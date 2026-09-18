using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

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
        PrefabManager.OnVanillaPrefabsAvailable += AddCustom;
        ItemManager.OnItemsRegistered += RewriteRegistered;
        PieceManager.OnPiecesRegistered += WireFeasts;
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
        GrantTrayRecipe();

        foreach (var row in _rows.Where(r => r.IsAdd && r.IsMeal))
            RegisterItem(row);
        foreach (var row in _rows.Where(r => r.IsAdd && r.IsSideboard))
            RegisterItem(row);
        foreach (var row in _rows.Where(r => r.IsAdd && r.IsFeast))
            RegisterFeast(row);

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

        var source = CloneSource(row);
        var go = PrefabManager.Instance.CreateClonedPrefab(row.Prefab, source);
        CustomItem item;
        if (go != null)
            item = new CustomItem(go, true, cfg);
        else
            item = new CustomItem(row.Prefab, source, cfg);
        ItemManager.Instance.AddItem(item);
        CookVisual.Apply(go != null ? go : item.ItemPrefab, row);
        if (row.IsSideboard)
            StripFood(item);
        else if (row.IsMeal)
            ApplyFood(row, item);
    }

    private static void RegisterFeast(CookRow row)
    {
        var icon = CookIcons.Sprite(row.Id);
        var matName = row.Prefab + "_Material";
        var matSource = row.CloneFrom.EndsWith("_Material", StringComparison.Ordinal)
            ? row.CloneFrom
            : row.CloneFrom + "_Material";
        if (PrefabManager.Instance.GetPrefab(matSource) == null)
            matSource = row.CloneFrom;

        var matGo = PrefabManager.Instance.CreateClonedPrefab(matName, matSource);
        if (matGo != null)
        {
            var shared = matGo.GetComponent<ItemDrop>()?.m_itemData?.m_shared;
            if (shared != null)
            {
                shared.m_name = row.Name;
                shared.m_description = Description(row);
                if (icon != null)
                    shared.m_icons = new[] { icon };
            }

            ItemManager.Instance.AddItem(new CustomItem(matGo, true));
            CookVisual.Apply(matGo, row);
            ApplyFood(row, matGo.GetComponent<ItemDrop>());
        }
        else
            Plugin.Log.LogWarning("cook feast missing material " + matSource);

        var pieceSource = row.CloneFrom.EndsWith("_Material", StringComparison.Ordinal)
            ? row.CloneFrom.Substring(0, row.CloneFrom.Length - "_Material".Length)
            : row.CloneFrom;
        var pieceGo = PrefabManager.Instance.CreateClonedPrefab(row.Prefab, pieceSource);
        if (pieceGo == null)
        {
            Plugin.Log.LogWarning("cook feast missing piece " + pieceSource);
            return;
        }

        var cfg = new PieceConfig
        {
            Name = row.Name,
            Description = Description(row),
            PieceTable = PieceTables.ServingTray,
            CraftingStation = CraftingStations.None
        };
        if (icon != null)
            cfg.Icon = icon;
        foreach (var use in row.Uses)
            cfg.AddRequirement(use.Item, use.Amount, true);

        PieceManager.Instance.AddPiece(new CustomPiece(pieceGo, true, cfg));
        CookVisual.Apply(pieceGo, row);
        Plugin.Log.LogInfo("cook feast piece " + row.Prefab);
    }

    private static void GrantTrayRecipe()
    {
        var row = _kit.FirstOrDefault(r => r.IsTool && r.IsAdd);
        if (row == null)
            return;
        if (PrefabManager.Instance.GetPrefab(row.Prefab) == null)
        {
            Plugin.Log.LogWarning("cook kit missing item " + row.Prefab);
            return;
        }

        var cfg = new RecipeConfig
        {
            Name = "Recipe_Restless_" + row.Id,
            Item = row.Prefab,
            Amount = row.OutputAmount,
            CraftingStation = StationName(string.IsNullOrEmpty(row.Station) ? "piece_workbench" : row.Station),
            RepairStation = CraftingStations.Workbench,
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
        => ApplyFood(row, item.ItemDrop);

    private static void ApplyFood(CookRow row, ItemDrop? drop)
    {
        var shared = drop?.m_itemData?.m_shared;
        if (shared == null)
            return;
        shared.m_food = row.Food;
        shared.m_foodStamina = row.FoodStamina;
        shared.m_foodEitr = row.FoodEitr;
        shared.m_foodRegen = row.FoodRegen;
        shared.m_foodBurnTime = row.FoodMinutes * 60f;
    }

    private static void StripFood(CustomItem item)
        => StripFood(item.ItemDrop);

    private static void StripFood(ItemDrop? drop)
    {
        var shared = drop?.m_itemData?.m_shared;
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

    private static void RewriteRegistered()
    {
        DressRegistered();
        if (ObjectDB.instance != null)
            Rewrite(ObjectDB.instance);
    }

    private static void DressRegistered()
    {
        var n = 0;
        foreach (var row in _rows.Where(r => r.IsAdd))
        {
            foreach (var name in PrefabNames(row))
            {
                var go = PrefabManager.Instance.GetPrefab(name)
                    ?? ObjectDB.instance?.GetItemPrefab(name);
                if (go == null)
                    continue;
                CookVisual.Apply(go, row);
                var drop = go.GetComponent<ItemDrop>();
                if (row.IsSideboard)
                    StripFood(drop);
                else if (row.IsMeal || row.IsFeast)
                    ApplyFood(row, drop);
                n++;
            }
        }

        Plugin.Log.LogInfo($"cook dressed {n} prefabs");
    }

    private static IEnumerable<string> PrefabNames(CookRow row)
    {
        yield return row.Prefab;
        if (row.IsFeast)
            yield return row.Prefab + "_Material";
    }

    private static void WireFeasts()
    {
        foreach (var row in _rows.Where(r => r.IsAdd && r.IsFeast))
        {
            var feast = PrefabManager.Instance.GetPrefab(row.Prefab)?.GetComponent<Feast>();
            var mat = PrefabManager.Instance.GetPrefab(row.Prefab + "_Material")
                ?? ObjectDB.instance?.GetItemPrefab(row.Prefab + "_Material");
            var drop = mat?.GetComponent<ItemDrop>();
            if (feast == null || drop == null)
            {
                Plugin.Log.LogWarning("cook feast leftover missing " + row.Prefab);
                continue;
            }

            feast.m_foodItem = drop;
            CookVisual.Apply(feast.gameObject, row);
        }

        DressRegistered();

        if (ObjectDB.instance != null)
            Rewrite(ObjectDB.instance);
    }

    internal static void Rewrite(ObjectDB db)
    {
        if (db?.m_recipes == null)
            return;

        var tray = _kit.FirstOrDefault(r => r.IsTool && r.IsAdd);
        if (tray != null && !db.m_recipes.Any(r => r != null && r.name == "Recipe_Restless_" + tray.Id))
            Plugin.Log.LogWarning("cook kit recipe missing from ObjectDB: Recipe_Restless_" + tray.Id);

        foreach (var row in _rows.Where(r => r.IsRewrite))
        {
            var reqs = BuildReqs(db, row);
            if (reqs.Count == 0)
                continue;

            var recipe = db.m_recipes.FirstOrDefault(r => r != null && r.name == row.RecipeId);
            if (recipe != null)
            {
                recipe.m_resources = reqs.ToArray();
                recipe.m_amount = row.OutputAmount;
            }

            var pieceName = row.Prefab.EndsWith("_Material", StringComparison.Ordinal)
                ? row.Prefab.Substring(0, row.Prefab.Length - "_Material".Length)
                : row.Prefab;
            var piece = PrefabManager.Instance.GetPrefab(pieceName)?.GetComponent<Piece>();
            if (piece != null)
                piece.m_resources = reqs.ToArray();
            else if (recipe == null)
                Plugin.Log.LogWarning($"RestlessCook: missing {row.RecipeId} / {pieceName} for {row.Id}");
        }
    }

    private static List<Piece.Requirement> BuildReqs(ObjectDB db, CookRow row)
    {
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
                m_recover = row.IsFeast
            });
        }

        return reqs;
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

    [HarmonyPatch(typeof(Feast), nameof(Feast.UpdateVisual))]
    public static class FeastVisualPatch
    {
        public static void Postfix(Feast __instance)
        {
            if (__instance != null)
                CookVisual.KeepPlate(__instance.gameObject);
        }
    }
}
