using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using RestlessQoL.Storage;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private const string AutoTapKey = "restless.kitchen.tap.v1";
    // Native Tap clears the barrel before its delayed spawn. Persist that handoff
    // so unloading the scene or changing peer ownership cannot lose the batch.
    private static long WorldTicks() => ZNet.instance != null ? ZNet.instance.GetTime().Ticks : DateTime.UtcNow.Ticks;
    private static void StartTap(CraftingStation table, Fermenter fermenter, ItemDrop output, int amount, Ledger ledger)
    {
        var receipt = new TapReceipt { StationId = fermenter.m_nview.GetZDO().m_uid.ToString(),
            Output = Clean(output.gameObject.name), Amount = Math.Max(1, amount),
            Due = WorldTicks() + (long)(Math.Max(0, fermenter.m_tapDelay) * TimeSpan.TicksPerSecond) };
        ledger.Taps.Add(receipt); Write(table.m_nview, ledger);
        fermenter.RPC_Tap(0L);
        if (fermenter.GetContent() != 0)
        { ledger.Taps.Remove(receipt); Write(table.m_nview, ledger); return; }
        receipt.Committed = true; Write(table.m_nview, ledger);
        fermenter.m_nview.GetZDO().Set(AutoTapKey, true);
    }
    private static void RecoverTaps(CraftingStation table, List<Hit> hits, Ledger ledger)
    {
        for (var i = ledger.Taps.Count - 1; i >= 0; i--)
        {
            var receipt = ledger.Taps[i];
            if (receipt.Due > WorldTicks()) continue;
            foreach (var hit in hits)
            {
                if (hit.Fermenter == null || StationId(hit) != receipt.StationId || !hit.Fermenter.m_nview.IsOwner()
                    || !ProductionLease.Controls(hit.Fermenter.m_nview, table.m_nview)) continue;
                if (!receipt.Committed && hit.Fermenter.GetContent() != 0)
                { ledger.Taps.RemoveAt(i); break; } // Tap had not cleared its source before unload.
                hit.Fermenter.m_nview.GetZDO().Set(AutoTapKey, true);
                hit.Fermenter.CancelInvoke(nameof(Fermenter.DelayedTap));
                FinishTapReceipt(ledger, receipt); break;
            }
        }
    }
    internal static bool CaptureOven(CraftingStation table, Smelter oven, string raw, int amount)
    {
        if (!FoodOven(Utils.GetPrefabName(oven.gameObject)) || table == null || !table.m_nview.IsOwner()
            || !oven.m_nview.IsOwner() || !ProductionLease.Controls(oven.m_nview, table.m_nview)) return false;
        var conversion = oven.GetItemConversion(raw);
        if (conversion?.m_to == null || amount <= 0) return false;
        var output = Clean(conversion.m_to.gameObject.name);
        var ledger = Read(table.m_nview);
        if (!Wanted(ledger, output)) return false;
        CreditFinished(ledger, output, amount); Write(table.m_nview, ledger);
        return true;
    }
    internal static bool CaptureTap(CraftingStation table, Fermenter fermenter)
    {
        if (!fermenter.m_nview.IsOwner() || !table.m_nview.IsOwner()
            || !ProductionLease.Controls(fermenter.m_nview, table.m_nview)) return false;
        var ledger = Read(table.m_nview);
        var receipt = ledger.Taps.Find(t => t.StationId == fermenter.m_nview.GetZDO().m_uid.ToString());
        if (receipt == null) return false;
        FinishTapReceipt(ledger, receipt);
        Write(table.m_nview, ledger); return true;
    }
    internal static void StationRemoved(ZNetView machine)
    {
        if (!machine.IsValid() || !machine.IsOwner()) return;
        var id = machine.GetZDO().m_uid.ToString();
        foreach (var table in KitchenHook.Tables)
        {
            if (table == null || table.m_nview == null || !table.m_nview.IsValid() || !table.m_nview.IsOwner()) continue;
            var ledger = Read(table.m_nview); var changed = false;
            for (var i = ledger.Work.Count - 1; i >= 0; i--)
                if (ledger.Work[i].StationId == id)
                {
                    foreach (var input in ledger.Work[i].Inputs) Add(ledger, input.Key, input.Value);
                    ledger.Work.RemoveAt(i); changed = true;
                }
            for (var i = ledger.Taps.Count - 1; i >= 0; i--)
                if (ledger.Taps[i].StationId == id)
                {
                    CreditFinished(ledger, ledger.Taps[i].Output, ledger.Taps[i].Amount);
                    ledger.Taps.RemoveAt(i); changed = true;
                }
            if (changed) Write(table.m_nview, ledger);
        }
    }
    internal static bool IsAutoTap(Fermenter fermenter) => fermenter.m_nview != null && fermenter.m_nview.IsValid()
        && fermenter.m_nview.GetZDO().GetBool(AutoTapKey);
    internal static void ResetTapMarker(Fermenter fermenter)
    { if (fermenter.m_nview != null && fermenter.m_nview.IsValid() && fermenter.m_nview.IsOwner()) fermenter.m_nview.GetZDO().Set(AutoTapKey, false); }
}

[HarmonyPatch]
internal static class KitchenMachineHook
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Smelter), nameof(Smelter.Spawn), typeof(string), typeof(int))]
    private static bool CollectOven(Smelter __instance, string __0, int __1)
    {
        foreach (var table in KitchenHook.Tables)
            if (table != null && KitchenRun.CaptureOven(table, __instance, __0, __1)) return false;
        return true;
    }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.RPC_Tap))]
    private static void BeforeTap(Fermenter __instance)
    { if (__instance.GetStatus() == Fermenter.Status.Ready) KitchenRun.ResetTapMarker(__instance); }
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Fermenter), nameof(Fermenter.DelayedTap))]
    private static bool CollectMead(Fermenter __instance)
    {
        if (!KitchenRun.IsAutoTap(__instance)) return true;
        foreach (var table in KitchenHook.Tables)
            if (table != null && KitchenRun.CaptureTap(table, __instance)) break;
        // The persisted receipt is authoritative even if its controller has moved
        // to another peer. That peer completes it; this callback must not spawn twice.
        return false;
    }
}
