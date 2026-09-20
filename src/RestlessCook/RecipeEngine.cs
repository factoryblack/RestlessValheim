using System;
using System.Collections;
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
        ItemManager.OnItemsRegistered += RewriteSoon;
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
            GrantItemRecipe(row, matName);
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
        cfg.AddRequirement(matName, 1, true);

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

    private static void GrantItemRecipe(CookRow row, string itemName)
    {
        if (PrefabManager.Instance.GetPrefab(itemName) == null)
        {
            Plugin.Log.LogWarning("cook feast leftover missing " + itemName);
            return;
        }

        var cfg = new RecipeConfig
        {
            Name = "Recipe_Restless_" + row.Id,
            Item = itemName,
            Amount = row.OutputAmount,
            CraftingStation = StationName(string.IsNullOrEmpty(row.Station) ? "piece_preptable" : row.Station),
            MinStationLevel = row.StationLevel < 1 ? 1 : row.StationLevel
        };
        foreach (var use in row.Uses)
            cfg.AddRequirement(use.Item, use.Amount, 0);
        ItemManager.Instance.AddRecipe(new CustomRecipe(cfg));
        Plugin.Log.LogInfo("cook feast leftover recipe " + itemName);
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
        shared.m_itemType = ItemDrop.ItemData.ItemType.Consumable;
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

    private static void RewriteSoon()
    {
        if (Plugin.Instance != null)
            Plugin.Instance.StartCoroutine(RewriteAfterRecipes());
    }

    private static IEnumerator RewriteAfterRecipes()
    {
        yield return null;
        yield return null;
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
            ApplyFood(row, drop);
            if (feast.m_eatStacks < 1)
                feast.m_eatStacks = 10;
            CookVisual.Apply(feast.gameObject, row);
            BindFeastPiece(row, feast.GetComponent<Piece>(), ObjectDB.instance);
        }

        // RewriteRegistered (on ItemManager.OnItemsRegistered) already ran
        // DressRegistered once by the time PieceManager.OnPiecesRegistered
        // fires -- every custom prefab is registered with PrefabManager back
        // in AddCustom, well before either event, so calling it again here
        // was pure duplicate work that doubled every "cook mesh/albedo
        // missing" warning on every load.
        if (ObjectDB.instance != null)
            Rewrite(ObjectDB.instance);
    }

    private static void BindFeastPiece(CookRow row, Piece? piece, ObjectDB? db)
    {
        if (piece == null)
            return;

        piece.m_craftingStation = null;
        piece.m_enabled = true;
        EnsureOnTray(piece.gameObject);
        DetachFromOtherTables(piece.gameObject);

        var matName = row.Prefab + "_Material";
        var prefab = db != null ? db.GetItemPrefab(matName) : null;
        if (prefab == null)
            prefab = PrefabManager.Instance.GetPrefab(matName);
        var drop = prefab?.GetComponent<ItemDrop>();
        if (drop?.m_itemData?.m_shared == null)
        {
            Plugin.Log.LogWarning($"cook feast {row.Id} missing leftover {matName}");
            return;
        }

        piece.m_resources = new[]
        {
            new Piece.Requirement
            {
                m_resItem = drop,
                m_amount = 1,
                m_amountPerLevel = 0,
                m_recover = true
            }
        };
    }

    private static void EnsureOnTray(GameObject go)
    {
        foreach (var table in PieceTablesInPlay())
        {
            if (!IsServingTray(table) || table.m_pieces == null)
                continue;
            if (table.m_pieces.Contains(go))
                continue;
            table.m_pieces.Add(go);
            Plugin.Log.LogInfo("cook feast added to " + table.name);
        }
    }

    private static void DetachFromOtherTables(GameObject go)
    {
        foreach (var table in PieceTablesInPlay())
        {
            if (IsServingTray(table) || table.m_pieces == null)
                continue;
            if (table.m_pieces.Remove(go))
                Plugin.Log.LogInfo("cook feast removed from " + table.name);
        }
    }

    private static bool IsServingTray(PieceTable table)
    {
        var name = table != null ? table.name : "";
        if (string.IsNullOrEmpty(name))
            return false;
        if (name.IndexOf("Feaster", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (name.IndexOf("ServingTray", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var token = PieceTables.ServingTray ?? "";
        return token.Length > 0 && name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static IEnumerable<PieceTable> PieceTablesInPlay()
    {
        var seen = new HashSet<PieceTable>();
        foreach (var table in Resources.FindObjectsOfTypeAll<PieceTable>())
        {
            if (table != null && seen.Add(table))
                yield return table;
        }
    }

    internal static void Rewrite(ObjectDB db)
    {
        if (db?.m_recipes == null || FindItem(db, "Wood") == null)
            return;

        var tray = _kit.FirstOrDefault(r => r.IsTool && r.IsAdd);
        if (tray != null && !db.m_recipes.Any(r => r != null && MatchesKitRecipe(r, tray)))
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

        foreach (var row in _rows.Where(r => r.IsAdd && r.IsFeast))
        {
            var piece = PrefabManager.Instance.GetPrefab(row.Prefab)?.GetComponent<Piece>();
            BindFeastPiece(row, piece, db);
        }

        foreach (var recipe in db.m_recipes)
        {
            var row = RowForRecipe(recipe);
            if (row == null)
                continue;
            var reqs = BuildReqs(db, row);
            if (reqs.Count == 0)
                continue;
            recipe.m_resources = reqs.ToArray();
            recipe.m_amount = row.OutputAmount;
        }
    }

    private static bool MatchesKitRecipe(Recipe recipe, CookRow tray)
    {
        if (recipe == null)
            return false;
        if (recipe.name == "Recipe_Restless_" + tray.Id)
            return true;
        var item = recipe.m_item != null ? recipe.m_item.name : "";
        return item == tray.Prefab || item.IndexOf(tray.Prefab, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static CookRow? RowForRecipe(Recipe? recipe)
    {
        if (recipe == null)
            return null;
        foreach (var row in _rows)
        {
            if (!row.IsAdd || !(row.IsMeal || row.IsSideboard || row.IsFeast))
                continue;
            if (MatchesAddRecipe(recipe, row))
                return row;
        }

        return null;
    }

    private static bool MatchesAddRecipe(Recipe? recipe, CookRow row)
    {
        if (recipe == null)
            return false;
        var itemName = row.IsFeast ? row.Prefab + "_Material" : row.Prefab;
        var crafted = recipe.m_item != null ? recipe.m_item.name : "";
        if (crafted == itemName || crafted == row.Prefab)
            return true;
        if (crafted.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        var name = recipe.name ?? "";
        if (name == "Recipe_Restless_" + row.Id)
            return true;
        return name.IndexOf(itemName, StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf(row.Prefab, StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf(row.Id, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static List<Piece.Requirement> BuildReqs(ObjectDB db, CookRow row)
    {
        var reqs = new List<Piece.Requirement>();
        foreach (var use in row.Uses)
        {
            var prefab = FindItem(db, use.Item);
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

    private static GameObject? FindItem(ObjectDB db, string name)
    {
        var go = db.GetItemPrefab(name);
        if (go != null)
            return go;
        go = PrefabManager.Instance.GetPrefab(name);
        if (go != null)
            return go;
        if (db.m_items == null)
            return null;
        foreach (var item in db.m_items)
        {
            if (item != null && item.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return item;
        }

        return null;
    }

    private static bool Ours(Recipe? recipe)
    {
        if (recipe?.m_item == null)
            return false;
        var crafted = recipe.m_item.name;
        if (crafted.StartsWith("Restless", StringComparison.Ordinal))
            return true;
        if ((recipe.name ?? "").IndexOf("Restless", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return _rows.Any(r => r.IsAdd && (crafted == r.Prefab || crafted == r.Prefab + "_Material"));
    }

    private static bool ReqsComplete(Recipe? recipe)
    {
        if (recipe?.m_resources == null || recipe.m_resources.Length == 0)
            return false;
        var needed = 0;
        foreach (var req in recipe.m_resources)
        {
            if (req == null || req.m_amount <= 0)
                continue;
            needed++;
            var drop = req.m_resItem;
            if (drop == null || drop.name.IndexOf("Mock", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (drop.m_itemData?.m_shared == null || string.IsNullOrEmpty(drop.m_itemData.m_shared.m_name))
                return false;
        }

        return needed > 0;
    }

    // Data-validity gate only: a recipe becomes knowable as soon as its
    // resource list is fully loaded (real items, not "Mock" placeholders
    // left over from a startup load-order race) -- same as vanilla cooking
    // recipes, which stay visible/known regardless of what you currently
    // hold. This used to also require the player to presently have the
    // ingredients or be near a station, which meant custom recipes were
    // un-learned and re-learned (re-firing the "learned recipe" toast) every
    // time UpdateKnownRecipesList ran and inventory/proximity had changed --
    // i.e. constantly during normal play.
    private static bool ShouldShow(Recipe recipe) => ReqsComplete(recipe);

    private static void TeachOurs(Player player)
    {
        var db = ObjectDB.instance;
        if (db?.m_recipes == null || player.m_knownRecipes == null)
            return;

        // Once granted, a recipe stays known forever, same as vanilla -- we
        // never remove from m_knownRecipes here.
        foreach (var recipe in db.m_recipes)
        {
            if (!Ours(recipe) || !ShouldShow(recipe))
                continue;
            var token = recipe.m_item.m_itemData?.m_shared?.m_name;
            if (string.IsNullOrEmpty(token) || player.IsRecipeKnown(token))
                continue;
            player.AddKnownRecipe(recipe);
        }
    }

    private static Recipe? FindRecipeByKnownName(ObjectDB db, string name)
    {
        foreach (var recipe in db.m_recipes)
        {
            var token = recipe?.m_item?.m_itemData?.m_shared?.m_name;
            if (token == name)
                return recipe;
        }

        return null;
    }

    private static void HideUnknown(object? list)
    {
        var player = Player.m_localPlayer;
        if (player == null || list == null)
            return;
        if (list is List<Recipe> recipes)
        {
            recipes.RemoveAll(recipe => Ours(recipe) && !ShouldShow(recipe));
            return;
        }

        if (list is not IList entries)
            return;
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var recipe = RecipeOf(entries[i]);
            if (recipe != null && Ours(recipe) && !ShouldShow(recipe))
                entries.RemoveAt(i);
        }
    }

    private static Recipe? RecipeOf(object? entry)
    {
        if (entry is Recipe recipe)
            return recipe;
        if (entry == null)
            return null;
        var field = AccessTools.Field(entry.GetType(), "Recipe")
            ?? AccessTools.Field(entry.GetType(), "m_recipe")
            ?? AccessTools.Field(entry.GetType(), "recipe");
        return field?.GetValue(entry) as Recipe;
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
            {
                ReadyFeast(__instance);
                CookVisual.KeepPlate(__instance.gameObject);
            }
        }
    }



    [HarmonyPatch(typeof(Feast), nameof(Feast.Interact))]
    public static class FeastInteractPatch
    {
        public static void Prefix(Feast __instance) => ReadyFeast(__instance);
    }

    private static void ReadyFeast(Feast? feast)
    {
        if (feast == null)
            return;
        var name = Utils.GetPrefabName(feast.gameObject);
        var row = _rows.FirstOrDefault(r => r.IsAdd && r.IsFeast && r.Prefab == name);
        if (row == null)
            return;
        var mat = ObjectDB.instance != null
            ? ObjectDB.instance.GetItemPrefab(row.Prefab + "_Material")
            : null;
        if (mat == null)
            mat = PrefabManager.Instance.GetPrefab(row.Prefab + "_Material");
        var drop = mat?.GetComponent<ItemDrop>();
        if (drop != null)
        {
            feast.m_foodItem = drop;
            ApplyFood(row, drop);
        }

        if (feast.m_eatStacks < 1)
            feast.m_eatStacks = 10;
        CookVisual.EnsureHit(feast.gameObject);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))]
    public static class KnownFeastPatch
    {
        public static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
                return;
            TeachOurs(__instance);
            foreach (var row in _rows.Where(r => r.IsAdd && r.IsFeast))
            {
                var piece = PrefabManager.Instance.GetPrefab(row.Prefab)?.GetComponent<Piece>();
                if (piece == null || string.IsNullOrEmpty(piece.m_name))
                    continue;
                if (__instance.m_knownRecipes.Contains(piece.m_name))
                    continue;
                if (!__instance.HaveRequirements(piece, Player.RequirementMode.IsKnown))
                    continue;
                __instance.AddKnownPiece(piece);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.IsRecipeKnown))]
    public static class IsRecipeKnownPatch
    {
        public static void Postfix(Player __instance, string name, ref bool __result)
        {
            if (!__result)
                return;
            var db = ObjectDB.instance;
            if (db?.m_recipes == null)
                return;
            var recipe = FindRecipeByKnownName(db, name);
            if (recipe == null || !Ours(recipe))
                return;
            if (!ShouldShow(recipe))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipeList))]
    public static class RecipeListPatch
    {
        public static void Prefix(object __0) => HideUnknown(__0);
    }
}
