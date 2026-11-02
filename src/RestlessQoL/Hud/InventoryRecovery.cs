using HarmonyLib;
using RestlessQoL.Core;

namespace RestlessQoL.HudTweaks;

// Data migration only: do not install ExtraSlots' UI, input, equipment or
// inventory-capacity patches when Core's optional features are disabled.
public sealed class InventoryRecovery : FeatureModule
{
    public override string Id => "inventory.recovery";
    public override bool Enabled => !CoreFeatures.OptionalEnabled;
    public override bool IsFoundation => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix, HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static void BeforeLoad(Player __instance) => ExtraSlots.BeginRecovery(__instance);

        [HarmonyPostfix, HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static void AfterLoad(Player __instance) => ExtraSlots.FinishRecovery(__instance);
    }
}
