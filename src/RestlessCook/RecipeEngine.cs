using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace RestlessCook;

internal static class RecipeEngine
{
    private static IReadOnlyList<CookRow> _rows = Array.Empty<CookRow>();
    private static IReadOnlyList<CookRow> _kit = Array.Empty<CookRow>();
    private static bool _added;
    private static Harmony? _harmony;

    public static void Load(CookBook book)
    {
        _rows = book.Items;
        _kit = book.Kit;
        GrantTrayRecipe();
        PrefabManager.OnVanillaPrefabsAvailable += AddCustom;
        ItemManager.OnItemsRegistered += RewriteRegistered;
        _harmony = new Harmony(Plugin.PluginGuid + ".recipes");
        _harmony.PatchAll(typeof(RecipeEngine).Assembly);
    }

    private static string StationName(string station)
    {
        if (station == "piece_preptable")
            return CraftingStations.FoodPreparationTable;
        if (station == "piece_cauldron")
            return CraftingStations.Cauldron;
        if (station == "piece_workbench")
            return CraftingStations.Workbench;
        return station;
    }

    internal static string CloneSource(CookRow row)
    {
        var src = row.CloneFrom;
        if (row.IsFeast && src.StartsWith("Feast", StringComparison.Ordinal) && !src.EndsWith("_Material", StringComparison.Ordinal))
            return src + "_Material";
        return src;
    }

    private static void AddCustom()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= AddCustom;
        if (_added)
            return;
        _added = true;

        RewritePrepTable();

        foreach (var row in _rows.Where(r => r.IsAdd && r.IsMeal))
            RegisterItem(row);
        foreach (var row in _rows.Where(r => r.IsAdd && r.IsSideboard))
            RegisterItem(row);
        foreach (var row in _rows.Where(r => r.IsAdd && r.IsFeast))
            RegisterItem(row);

        var ops = _rows.Count(r => r.IsAdd || r.IsRewrite);
        Plugin.Log.LogInfo($"RestlessCook registered {_rows.Count(r => r.IsAdd)} custom items from cook.yaml ({ops} recipe operations).");
    }

    private static void RegisterItem(CookRow row)
    {
        var cfg = new ItemConfig
        {
            Name = row.Name,
            Description = Description(row),
            CraftingStation = StationName(row.Station),
            MinStationLevel = row.StationLevel,
            Amount = row.OutputAmount
        };
        var icon = CookIcons.Sprite(row.Id);
        if (icon != null)
            cfg.Icon = icon;
        foreach (var use in row.Uses)
            cfg.AddRequirement(use.Item, use.Amount);

        var item = new CustomItem(row.Prefab, CloneSource(row), cfg);
        ItemManager.Instance.AddItem(item);
        CookVisual.Apply(item, row);
        if (row.IsSideboard)
            StripFood(item);
        else if (row.IsMeal || row.IsFeast)
            ApplyFood(row, item);
    }

    private static void GrantTrayRecipe()
    {
        var row = _kit.FirstOrDefault(r => r.IsTool && r.IsAdd);
        if (row == null)
            return;

        var cfg = new RecipeConfig
        {
            Name = "Recipe_Restless_" + row.Id,
            Item = row.Prefab,
            Amount = row.OutputAmount,
            CraftingStation = StationName(string.IsNullOrEmpty(row.Station) ? "piece_workbench" : row.Station),
            RepairStation = "piece_workbench",
            MinStationLevel = row.StationLevel < 1 ? 1 : row.StationLevel
        };
        foreach (var use in row.Uses)
            cfg.AddRequirement(use.Item, use.Amount, 0);
        ItemManager.Instance.AddRecipe(new CustomRecipe(cfg));
        Plugin.Log.LogInfo("cook kit recipe " + row.Prefab);
    }

    private static void RewritePrepTable()
    {
        var row = _kit.FirstOrDefault(r => r.IsStation && r.IsRewrite);
        if (row == null)
            return;

        var go = PrefabManager.Instance.GetPrefab(row.Prefab);
        var piece = go?.GetComponent<Piece>();
        if (piece == null)
        {
            Plugin.Log.LogWarning("cook kit missing " + row.Prefab);
            return;
        }

        var reqs = new List<Piece.Requirement>();
        foreach (var use in row.Uses)
        {
            var prefab = PrefabManager.Instance.GetPrefab(use.Item);
            var drop = prefab?.GetComponent<ItemDrop>();
            if (drop == null)
            {
                Plugin.Log.LogWarning("cook kit missing ingredient " + use.Item);
                continue;
            }

            reqs.Add(new Piece.Requirement
            {
                m_resItem = drop,
                m_amount = use.Amount,
                m_amountPerLevel = 0,
                m_recover = true
            });
        }

        if (reqs.Count == 0)
            return;
        piece.m_resources = reqs.ToArray();
        Plugin.Log.LogInfo("cook kit piece " + row.Prefab);
    }

    private static string Description(CookRow row)
    {
        if (row.IsSideboard)
            return "A prepared board for feast assembly. Not edible on its own.";
        if (row.IsFeast)
            return "A Restless feast assembled from prepared meals.";
        return "A Restless prepared meal.";
    }

    private static void ApplyFood(CookRow row, CustomItem item)
    {
        var shared = Shared(item);
        if (shared == null)
            return;
        shared.m_food = row.Food;
        shared.m_foodStamina = row.FoodStamina;
        shared.m_foodEitr = row.FoodEitr;
        shared.m_foodRegen = row.FoodRegen;
        shared.m_foodBurnTime = row.FoodMinutes * 60f;
    }

    private static void StripFood(CustomItem item)
    {
        var shared = Shared(item);
        if (shared == null)
            return;
        shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
        shared.m_food = 0f;
        shared.m_foodStamina = 0f;
        shared.m_foodEitr = 0f;
        shared.m_foodRegen = 0f;
        shared.m_foodBurnTime = 0f;
        if (shared.m_maxStackSize < 20)
            shared.m_maxStackSize = 20;
    }

    private static ItemDrop.ItemData.SharedData? Shared(CustomItem item)
        => item.ItemDrop?.m_itemData?.m_shared;

    private static void RewriteRegistered()
    {
        if (ObjectDB.instance != null)
            Rewrite(ObjectDB.instance);
    }

    internal static void Rewrite(ObjectDB db)
    {
        if (db?.m_recipes == null)
            return;

        foreach (var row in _rows.Where(r => r.IsRewrite))
        {
            var recipe = db.m_recipes.FirstOrDefault(r => r != null && r.name == row.RecipeId);
            if (recipe == null)
            {
                Plugin.Log.LogWarning($"RestlessCook: missing {row.RecipeId} for {row.Id}");
                continue;
            }

            var reqs = new List<Piece.Requirement>();
            foreach (var use in row.Uses)
            {
                var prefab = db.GetItemPrefab(use.Item);
                if (prefab == null)
                {
                    Plugin.Log.LogWarning($"RestlessCook: {row.Id} missing ingredient {use.Item}");
                    continue;
                }

                var drop = prefab.GetComponent<ItemDrop>();
                if (drop == null)
                {
                    Plugin.Log.LogWarning($"RestlessCook: {use.Item} has no ItemDrop");
                    continue;
                }

                reqs.Add(new Piece.Requirement
                {
                    m_resItem = drop,
                    m_amount = use.Amount,
                    m_amountPerLevel = 0,
                    m_recover = false
                });
            }

            if (reqs.Count == 0)
                continue;

            recipe.m_resources = reqs.ToArray();
            recipe.m_amount = row.OutputAmount;
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    public static class CopyOtherDbPatch
    {
        public static void Postfix(ObjectDB __instance) => Rewrite(__instance);
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class AwakePatch
    {
        public static void Postfix(ObjectDB __instance) => Rewrite(__instance);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))]
    public static class KnownRecipesPatch
    {
        public static void Postfix(Player __instance) => Teach(__instance);
    }

    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    public static class PrepTablePatch
    {
        public static void Postfix(CraftingStation __instance, Humanoid user)
        {
            if (__instance == null || user is not Player player)
                return;
            if (__instance.name.Replace("(Clone)", "") != "piece_preptable")
                return;
            Teach(player);
        }
    }

    private static void Teach(Player player)
    {
        if (player == null || ObjectDB.instance == null)
            return;

        foreach (var row in _rows.Where(r => r.IsAdd))
        {
            var prefab = ObjectDB.instance.GetItemPrefab(row.Prefab);
            var data = prefab?.GetComponent<ItemDrop>()?.m_itemData;
            if (data == null)
                continue;
            player.AddKnownItem(data);
        }

        var ours = new HashSet<string>(_rows.Where(r => r.IsAdd).Select(r => r.Prefab), StringComparer.Ordinal);
        foreach (var row in _kit.Where(r => r.IsAdd))
            ours.Add(row.Prefab);

        if (ObjectDB.instance.m_recipes == null)
            return;
        foreach (var recipe in ObjectDB.instance.m_recipes)
        {
            if (recipe?.m_item == null)
                continue;
            if (!ours.Contains(recipe.m_item.name))
                continue;
            player.AddKnownRecipe(recipe);
        }
    }
}
