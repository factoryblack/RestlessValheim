using System.Linq;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

public sealed class GroundVacuum : FeatureModule
{
    public override string Id => "storage.vacuum";
    public override bool Enabled => ModConfig.StorageEnabled.Value && ModConfig.VacuumEnabled.Value;
    public override bool TickInMenus => true;

    private float _next;

    public override void Tick()
    {
        if (!Enabled)
            return;
        if (Time.time < _next)
            return;
        _next = Time.time + ModConfig.VacuumInterval.Value;

        var player = Player.m_localPlayer;
        if (player == null)
            return;

        var range = ModConfig.StorageRange.Value;
        var origin = player.transform.position;
        foreach (var drop in ItemDrop.s_instances.ToArray())
        {
            if (drop == null || drop.m_itemData?.m_shared == null)
                continue;
            if (drop.m_itemData.m_shared.m_questItem)
                continue;
            // A placed feast is an item drop and a piece. It is not a pile.
            if (drop.IsPiece())
                continue;
            if ((drop.transform.position - origin).sqrMagnitude > range * range)
                continue;
            NearbyStorage.TryDepositDrop(player, drop);
        }
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.DropItem))]
        private static void AfterDrop(int amount, ItemDrop __result)
        {
            DroppedStack.Remember(__result, amount);
            DroppedStack.Apply(__result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), "Start")]
        private static void AfterStart(ItemDrop __instance) => DroppedStack.Apply(__instance);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), "OnDestroy")]
        private static void AfterDestroy(ItemDrop __instance) => DroppedStack.Forget(__instance);
    }
}
