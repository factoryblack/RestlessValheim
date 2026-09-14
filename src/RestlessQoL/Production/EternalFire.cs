using System;
using HarmonyLib;
using RestlessQoL.Core;

namespace RestlessQoL.Production;

public sealed class EternalFire : FeatureModule
{
    public override string Id => "production.eternalfire";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Awake))]
        private static void Awake(Fireplace __instance)
        {
            if (!ModConfig.EternalFireEnabled.Value)
                return;
            __instance.m_infiniteFuel = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.DrainEquipedItemDurability))]
        private static bool KeepTorch(ItemDrop.ItemData item)
        {
            if (!ModConfig.EternalFireEnabled.Value || item?.m_shared == null)
                return true;
            return item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch;
        }
    }
}
