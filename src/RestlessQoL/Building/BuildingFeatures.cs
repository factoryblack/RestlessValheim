using HarmonyLib;
using RestlessQoL.Core;

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
