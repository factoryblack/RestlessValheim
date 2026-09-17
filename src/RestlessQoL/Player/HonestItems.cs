using HarmonyLib;
using RestlessQoL.Core;

namespace RestlessQoL.PlayerTweaks;

// Valheim 1.0 stamps ItemData.m_cheated on spawned items and on some
// clone/add paths. Storage pulls, extra-slot moves, and Jötunn crafts
// were inheriting it. Clear the flag wherever an inventory changes.
public sealed class HonestItems : FeatureModule
{
    public override string Id => "inventory.honest";
    public override bool Enabled => true;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), "Changed")]
        private static void InventoryChanged(Inventory __instance)
        {
            if (!ModConfig.HonestItemsEnabled.Value || __instance == null)
                return;
            foreach (var item in __instance.GetAllItems())
                Honest(item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static void PlayerLoad(Player __instance)
        {
            if (!ModConfig.HonestItemsEnabled.Value || __instance?.GetInventory() == null)
                return;
            foreach (var item in __instance.GetInventory().GetAllItems())
                Honest(item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        private static void DropAwake(ItemDrop __instance)
        {
            if (!ModConfig.HonestItemsEnabled.Value)
                return;
            Honest(__instance?.m_itemData);
        }
    }

    private static void Honest(ItemDrop.ItemData? item)
    {
        if (item != null && item.m_cheated)
            item.m_cheated = false;
    }
}
