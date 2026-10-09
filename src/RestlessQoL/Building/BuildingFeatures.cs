using System.Collections.Generic;
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
                             __instance.transform.position, ReachPolicy.RepairReach))
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
        // Alt+click while the hammer is out. Skipping the placement update
        // keeps a normal piece from being built and a repair from restoring health.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
        private static bool UpdatePlacement(Player __instance, bool takeInput)
        {
            if (!takeInput || !ModConfig.AreaSealEnabled.Value || ZInput.IsGamepadActive() || !HoldingAlt())
                return true;
            if (!HammerOut(__instance))
                return true;
            if (Hud.IsPieceSelectionVisible() || Hud.InRadial() || !ZInput.GetButtonDown("Attack"))
                return true;

            var tool = __instance.GetRightItem();
            var attack = tool?.m_shared?.m_attack;
            if (attack == null || !__instance.HaveStamina(attack.m_attackStamina))
                return true;

            var hovering = __instance.GetHoveringPiece();
            if (hovering != null && __instance.CheckCanRemovePiece(hovering)
                && PrivateArea.CheckAccess(hovering.transform.position))
            {
                var sealedCount = Apply(__instance, hovering);
                if (sealedCount > 0)
                    Swing(__instance, tool, hovering);
            }
            else
                __instance.Message(MessageHud.MessageType.TopLeft, "Nothing to seal");

            __instance.m_placePressedTime = -9999f;
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

    internal static bool HammerOut(Player? player) =>
        player?.GetRightItem()?.m_shared?.m_name == "$item_hammer";

    private readonly struct Region
    {
        public readonly Vector3 Origin;
        public readonly float Radius;
        public readonly bool Station;
        public Region(Vector3 origin, float radius, bool station = false) { Origin = origin; Radius = radius; Station = station; }
    }

    // A piece inside a crafting station seals that station's build area, the same
    // distance the workbench range override grants. The seal radius is only for
    // wood that is not inside any station.
    private static List<Region> Coverage(Piece hovering)
    {
        var list = new List<Region>();
        var point = hovering.transform.position;
        foreach (var station in CraftingStation.m_allStations)
        {
            if (station == null) continue;
            var range = station.GetStationBuildRange();
            if (!ReachGeometry.Station(point, station.transform.position, range))
                continue;
            list.Add(new Region(station.transform.position, range, true));
        }

        if (list.Count == 0)
            list.Add(new Region(point, ReachPolicy.SealReach));
        return list;
    }

    private static IEnumerable<WearNTear> Covered(List<Region> regions)
    {
        // One pass over loaded pieces, even when many station areas overlap.
        foreach (var wear in WearNTear.GetAllInstances())
        {
            if (wear == null) continue;
            foreach (var region in regions)
            {
                var inside = region.Station
                    ? ReachGeometry.Station(wear.transform.position, region.Origin, region.Radius)
                    : ReachGeometry.Sphere(wear.transform.position, region.Origin, region.Radius);
                if (!inside) continue;
                yield return wear;
                break;
            }
        }
    }

    private static int Apply(Player player, Piece hovering)
    {
        var seen = new HashSet<int>();
        var fresh = 0;
        var done = 0;
        var sealedCount = 0;
        var ranOut = false;
        foreach (var wear in Covered(Coverage(hovering)))
        {
            if (!seen.Add(wear.GetInstanceID()))
                continue;
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
        foreach (var container in NearbyStorage.Around(player.transform.position, ReachPolicy.ResourceReach, player.GetPlayerID()))
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
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CraftingStation, StationReachState> Native = new();

    protected override void OnLoaded()
    {
        ModConfig.WorkbenchTweaksEnabled.SettingChanged += Changed;
        ModConfig.WorkbenchRange.SettingChanged += Changed;
        ModConfig.GlobalReachEnabled.SettingChanged += Changed;
        ModConfig.GlobalReachProfile.SettingChanged += Changed;
    }

    private static void Changed(object sender, System.EventArgs args)
    {
        foreach (var station in CraftingStation.m_allStations)
        {
            if (station == null) continue;
            Apply(station);
            station.GetExtensions(); // Refresh the native marker and effect collider immediately.
        }
    }

    private static void Apply(CraftingStation station)
    {
        var original = Native.GetValue(station, s => new StationReachState(s.m_useDistance, s.m_rangeBuild, s.m_extraRangePerLevel));
        original.Resolve(ModConfig.WorkbenchTweaksEnabled.Value, ReachPolicy.StationReach, out var use, out var build, out var extra);
        if (station.m_useDistance == use && station.m_rangeBuild == build && station.m_extraRangePerLevel == extra) return;
        station.m_useDistance = use;
        station.m_rangeBuild = build;
        station.m_extraRangePerLevel = extra;
        station.m_updateExtensionTimer = 2f;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Start))]
        private static void Start(CraftingStation __instance)
        {
            if (__instance.m_nview == null || !__instance.m_nview.IsValid()) return;
            Apply(__instance);
            __instance.GetExtensions();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetExtensions))]
        private static void GetExtensions(CraftingStation __instance) => Apply(__instance);
    }
}
