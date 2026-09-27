using HarmonyLib;
using RestlessQoL.Core;

namespace RestlessQoL.PlayerTweaks;

// Valheim blocks achievements when an item, the character, or the world is
// marked cheated, and also when Game.isModded is set. Jötunn sets that so
// the menu can say the game is modded. Clear the marks. Leave the menu label.
public sealed class HonestItems : FeatureModule
{
    public override string Id => "inventory.honest";
    public override bool Enabled => true;

    private static bool On => ModConfig.HonestItemsEnabled.Value;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), "Changed")]
        private static void InventoryChanged(Inventory __instance)
        {
            if (!On || __instance == null)
                return;
            foreach (var item in __instance.GetAllItems())
                Honest(item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.Load))]
        private static void PlayerLoad(Player __instance)
        {
            if (!On || __instance?.GetInventory() == null)
                return;
            foreach (var item in __instance.GetInventory().GetAllItems())
                Honest(item);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
        private static void DropAwake(ItemDrop __instance)
        {
            if (!On)
                return;
            Honest(__instance?.m_itemData);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerProfile), nameof(PlayerProfile.Load))]
        private static void ProfileLoad(PlayerProfile __instance) => ClearCharacter(__instance);

        // A cheat command sets the character flag after it runs. Drop it again
        // so the character file does not stay marked.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Terminal.ConsoleCommand), nameof(Terminal.ConsoleCommand.RunAction))]
        private static void AfterCommand()
        {
            if (!On || Game.instance == null)
                return;
            ClearCharacter(Game.instance.GetPlayerProfile());
        }

        // Starting keys outside the world-modifier list mark the world. The
        // keys stay. They are not an achievement ban.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Achievements), nameof(Achievements.IsWorldCheated))]
        private static void WorldCheated(ref bool __result)
        {
            if (On)
                __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Achievements), nameof(Achievements.CanGetAchievements))]
        private static void AchievementsOpen(ref bool __result)
        {
            if (On)
                __result = true;
        }
    }

    private static void ClearCharacter(PlayerProfile? profile)
    {
        if (!On || profile == null || !profile.m_usedCheats)
            return;
        profile.m_usedCheats = false;
        profile.Save();
    }

    private static void Honest(ItemDrop.ItemData? item)
    {
        if (item != null && item.m_cheated)
            item.m_cheated = false;
    }
}
