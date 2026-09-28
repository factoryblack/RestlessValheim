using HarmonyLib;
using RestlessQoL.Core;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessQoL.Building;

public sealed class AreaRepair : FeatureModule
{
    public override string Id => "building.arearepair";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        private static bool _busy;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Repair))]
        private static void Repair(WearNTear __instance)
        {
            if (_busy || !ModConfig.AreaRepairEnabled.Value)
                return;
            var player = Player.m_localPlayer;
            if (player == null)
                return;

            _busy = true;
            try
            {
                foreach (var other in NearbyQuery.UniqueInSphere<WearNTear>(
                             __instance.transform.position, ModConfig.AreaRepairRadius.Value))
                {
                    if (other == __instance)
                        continue;
                    var piece = other.GetComponent<Piece>();
                    if (piece == null || !piece.IsPlacedByPlayer())
                        continue;
                    other.Repair();
                }
            }
            finally
            {
                _busy = false;
            }
        }
    }
}

public sealed class AreaSeal : FeatureModule
{
    public override string Id => "building.area_seal";
    public override bool Enabled => true;

    private const string Flag = "RestlessSeal";
    private const string Resin = "$item_resin";

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.Repair))]
        private static bool Repair(Player __instance, ItemDrop.ItemData toolItem)
        {
            if (!ModConfig.AreaSealEnabled.Value || !HoldingAlt())
                return true;
            var hovering = __instance.GetHoveringPiece();
            if (hovering == null || !__instance.CheckCanRemovePiece(hovering)
                || !PrivateArea.CheckAccess(hovering.transform.position))
                return false;

            var sealedCount = Apply(__instance, hovering);
            if (sealedCount > 0)
                Swing(__instance, toolItem, hovering);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.UpdateWear))]
        private static void UpdateWear(WearNTear __instance, float time)
        {
            if (Sealed(__instance))
                __instance.m_rainTimer = time;
        }
    }

    private static bool HoldingAlt() =>
        Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

    private static int Apply(Player player, Piece hovering)
    {
        var origin = hovering.transform.position;
        var radius = ModConfig.AreaSealRadius.Value;
        var fresh = 0;
        var done = 0;
        var sealedCount = 0;
        var ranOut = false;
        foreach (var wear in NearbyQuery.UniqueInSphere<WearNTear>(origin, radius))
        {
            if (!Wooden(wear))
                continue;
            if (!Owned(wear))
                continue;
            if (Sealed(wear))
            {
                done++;
                continue;
            }

            fresh++;
            if (ranOut)
                continue;
            if (!SpendResin(player))
            {
                ranOut = true;
                continue;
            }

            wear.m_nview.GetZDO().Set(Flag, 1);
            fresh--;
            sealedCount++;
        }

        if (sealedCount > 0)
            player.Message(MessageHud.MessageType.TopLeft, "Sealed " + sealedCount);
        else if (fresh > 0)
            player.Message(MessageHud.MessageType.TopLeft, "No resin");
        else if (done > 0)
            player.Message(MessageHud.MessageType.TopLeft, "Already sealed");
        else
            player.Message(MessageHud.MessageType.TopLeft, "Nothing to seal");
        return sealedCount;
    }

    private static void Swing(Player player, ItemDrop.ItemData? tool, Piece hovering)
    {
        player.UseStamina(player.GetBuildStamina());
        var attack = tool?.m_shared?.m_attack;
        if (attack != null)
            Traverse.Create(player).Field<ZSyncAnimation>("m_zanim").Value?.SetTrigger(attack.m_attackAnimation);
        if (tool?.m_shared != null && tool.m_shared.m_useDurability)
            tool.m_durability -= tool.m_shared.m_useDurabilityDrain * Game.m_durabilityRate;
        hovering.m_placeEffect?.Create(hovering.transform.position, hovering.transform.rotation, null, 1f, -1, player.GetZDOID());
    }

    private static bool Wooden(WearNTear wear)
    {
        if (wear == null || !wear.m_noRoofWear)
            return false;
        var piece = wear.GetComponent<Piece>();
        return piece != null && piece.IsPlacedByPlayer();
    }

    private static bool Sealed(WearNTear wear)
    {
        var view = wear.m_nview;
        return view != null && view.IsValid() && view.GetZDO() != null && view.GetZDO().GetInt(Flag) == 1;
    }

    private static bool Owned(WearNTear wear)
    {
        var view = wear.m_nview;
        if (view == null || !view.IsValid() || view.GetZDO() == null)
            return false;
        if (!view.IsOwner())
            view.ClaimOwnership();
        return view.IsOwner();
    }

    private static bool SpendResin(Player player)
    {
        var inventory = player.GetInventory();
        using (NearbyStorage.SuppressPatches())
        {
            if (NearbyStorage.CountVanilla(inventory, Resin) > 0)
            {
                inventory.RemoveItem(Resin, 1);
                return true;
            }
        }

        if (!ModConfig.StorageEnabled.Value)
            return false;
        foreach (var container in NearbyStorage.Around(player.transform.position, ModConfig.StorageRange.Value, player.GetPlayerID()))
        {
            if (NearbyStorage.PullOwned(container, Resin, 1, ModConfig.LeaveOne.Value, -1, false) < 1)
                continue;
            container.Save();
            return true;
        }

        return false;
    }
}

public sealed class WorkbenchTweaks : FeatureModule
{
    public override string Id => "building.workbench";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        private static void Start(CraftingStation __instance)
        {
            if (!ModConfig.WorkbenchTweaksEnabled.Value)
                return;
            var range = ModConfig.WorkbenchRange.Value;
            __instance.m_useDistance = range;
            __instance.m_rangeBuild = range;
            __instance.m_buildRange = range;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetStationBuildRange))]
        private static void GetStationBuildRange(ref float __result)
        {
            if (ModConfig.WorkbenchTweaksEnabled.Value)
                __result = ModConfig.WorkbenchRange.Value;
        }
    }
}
