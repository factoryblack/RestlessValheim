using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RestlessQoL.Core;
using RestlessQoL.HudTweaks;
using TMPro;
using UnityEngine;

namespace RestlessQoL.Storage;

public sealed class CraftFromStorage : FeatureModule
{
    public override string Id => "storage.crafting";
    public override bool Enabled => true;

    private static bool On =>
        ModConfig.StorageEnabled.Value && ModConfig.CraftFromStorageEnabled.Value;

    protected override void OnLoaded()
    {
        var original = FindSetupRequirement(typeof(InventoryGui)) ?? FindSetupRequirement(typeof(Hud));
        if (original == null)
        {
            Plugin.Log.LogError($"{Id}: SetupRequirement not found — recipe counts stay need-only");
            return;
        }

        Harmony!.Patch(original, postfix: new HarmonyMethod(typeof(CraftFromStorage), nameof(ShowHaveNeed)));
    }

    private static MethodBase? FindSetupRequirement(Type type)
    {
        var six = new[]
        {
            typeof(Transform), typeof(Piece.Requirement), typeof(Player), typeof(bool), typeof(int), typeof(int)
        };
        var five = new[]
        {
            typeof(Transform), typeof(Piece.Requirement), typeof(Player), typeof(bool), typeof(int)
        };
        return AccessTools.DeclaredMethod(type, "SetupRequirement", six)
               ?? AccessTools.Method(type, "SetupRequirement", six)
               ?? AccessTools.DeclaredMethod(type, "SetupRequirement", five)
               ?? AccessTools.Method(type, "SetupRequirement", five);
    }

    // Vanilla writes the need only. CountItems already includes nearby chests
    // (minus LeaveOne). Keep a foreign have/need string if another mod wrote one.
    private static void ShowHaveNeed(object[] __args)
    {
        if (__args == null || __args.Length < 3)
            return;
        var elementRoot = __args[0] as Transform;
        var req = __args[1] as Piece.Requirement;
        var player = __args[2] as Player;
        if (elementRoot == null || req?.m_resItem?.m_itemData?.m_shared == null || player == null)
            return;
        var amount = elementRoot.Find("res_amount")?.GetComponent<TMP_Text>();
        if (amount == null || !amount.gameObject.activeSelf)
            return;
        var raw = RestlessUi.Bare(amount.text);
        if (string.IsNullOrEmpty(raw) || raw.IndexOf('/') >= 0)
            return;
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var need) || need <= 0)
            return;
        var have = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name, -1, true);
        amount.text = have + "/" + need;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.CountItems), typeof(string), typeof(int), typeof(bool))]
        private static void CountItems(Inventory __instance, string name, int quality, bool matchWorldLevel,
            ref int __result)
        {
            if (!On || NearbyStorage.SkipPatches || !NearbyStorage.IsLocalPlayerInventory(__instance))
                return;
            __result += NearbyStorage.Count(name, quality, matchWorldLevel);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.HaveItem), typeof(string), typeof(bool))]
        private static void HaveItem(Inventory __instance, string name, ref bool __result)
        {
            if (__result || !On || NearbyStorage.SkipPatches || !NearbyStorage.IsLocalPlayerInventory(__instance))
                return;
            __result = NearbyStorage.Has(name);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
        private static void RemoveItem(Inventory __instance, string name, ref int amount, int itemQuality,
            bool worldLevelBased)
        {
            if (On)
                NearbyStorage.TakeShortfall(__instance, name, ref amount, itemQuality, worldLevelBased);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Recipe), typeof(bool), typeof(int), typeof(int))]
        private static void HaveRequirementsRecipe(Player __instance, Recipe recipe, bool discover, int qualityLevel, int amount, ref bool __result)
        {
            if (__result || !On || discover || recipe?.m_resources == null || __instance != Player.m_localPlayer)
                return;
            if (!StationReady(__instance, recipe, qualityLevel))
                return;
            if (NearbyStorage.HasRequirements(__instance, recipe.m_resources, qualityLevel, amount))
                __result = true;
        }

        // Chest stock only covers materials. Vanilla already returned false
        // when the bench is missing or too low for this quality.
        private static bool StationReady(Player player, Recipe recipe, int qualityLevel)
        {
            if (recipe.m_craftingStation == null)
                return true;
            var station = player.GetCurrentCraftingStation();
            if (station == null || station.m_name != recipe.m_craftingStation.m_name)
                return false;
            if (!station.CheckUsable(player, false))
                return false;
            return station.GetLevel() >= recipe.GetRequiredStationLevel(qualityLevel);
        }
    }
}

public sealed class BuildFromStorage : FeatureModule
{
    public override string Id => "storage.building";
    public override bool Enabled => true;

    private static bool On =>
        ModConfig.StorageEnabled.Value && ModConfig.BuildFromStorageEnabled.Value;

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
        private static void HaveRequirements(Player __instance, Piece piece, ref bool __result)
        {
            if (__result || !On || piece?.m_resources == null || __instance != Player.m_localPlayer)
                return;
            if (NearbyStorage.HasRequirements(__instance, piece.m_resources, 1, 1))
                __result = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.RemoveItem), typeof(string), typeof(int), typeof(int), typeof(bool))]
        private static void RemoveItem(Inventory __instance, string name, ref int amount, int itemQuality,
            bool worldLevelBased)
        {
            if (On)
                NearbyStorage.TakeShortfall(__instance, name, ref amount, itemQuality, worldLevelBased);
        }
    }
}

public sealed class QuickStack : FeatureModule
{
    public override string Id => "storage.quickstack";
    public override bool Enabled => ModConfig.StorageEnabled.Value && ModConfig.QuickStackEnabled.Value;

    public override void Tick()
    {
        if (!Enabled || !ModConfig.QuickStackHotkey.Value.IsDown())
            return;
        var player = Player.m_localPlayer;
        if (player == null)
            return;
        Run(player);
    }

    public static void Run(Player player)
    {
        var inventory = player.GetInventory();
        var items = inventory.GetAllItems().ToArray();
        var moved = 0;
        void Next(int index)
        {
            while (index < items.Length)
            {
                var item = items[index++];
                if (item == null || item.m_equipped || ExtraSlots.SkipDeposit(item) || SlotLock.Held(item))
                    continue;
                moved += NearbyStorage.TryDeposit(player, item);
                if (item.m_stack <= 0)
                {
                    inventory.RemoveItem(item);
                    continue;
                }

                NearbyStorage.RequestDeposit(player, item, null, extra =>
                {
                    moved += extra;
                    if (item.m_stack <= 0)
                        inventory.RemoveItem(item);
                    Next(index);
                });
                return;
            }

            if (moved > 0)
                inventory.Changed(true, false);
            player.Message(MessageHud.MessageType.Center, moved > 0 ? $"Stacked {moved}" : "Nothing to stack");
        }

        Next(0);
    }
}

public sealed class Restock : FeatureModule
{
    public override string Id => "storage.restock";
    public override bool Enabled => ModConfig.StorageEnabled.Value && ModConfig.RestockEnabled.Value;

    public override void Tick()
    {
        if (!Enabled || !ModConfig.RestockHotkey.Value.IsDown())
            return;
        var player = Player.m_localPlayer;
        if (player == null)
            return;
        Run(player);
    }

    public static void Run(Player player)
    {
        var inventory = player.GetInventory();
        var items = inventory.GetAllItems().ToArray();
        var filled = 0;
        void Next(int index)
        {
            while (index < items.Length)
            {
                var item = items[index++];
                if (item == null || SlotLock.Held(item))
                    continue;
                var max = item.m_shared.m_maxStackSize;
                if (item.m_stack >= max)
                    continue;
                NearbyStorage.RequestConsume(item.m_shared.m_name, max - item.m_stack, taken =>
                {
                    if (taken > 0)
                    {
                        item.m_stack += taken;
                        filled += taken;
                    }

                    Next(index);
                });
                return;
            }

            if (filled > 0)
                inventory.Changed(true, false);
            player.Message(MessageHud.MessageType.Center, filled > 0 ? $"Restocked {filled}" : "Nothing to restock");
        }

        Next(0);
    }
}
