using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static void IndexConfiguredRows()
    {
        _rows = new List<CookRow>(_configuredRows);
        ById.Clear(); ByOutput.Clear(); UsedBy.Clear();
        foreach (var row in _rows)
        {
            if (!string.IsNullOrEmpty(row.Id)) ById[row.Id] = row;
            var output = OutputOf(row);
            if (!string.IsNullOrEmpty(output)) ByOutput[output] = row;
            foreach (var use in row.Uses)
            {
                if (string.IsNullOrEmpty(use.Item)) continue;
                if (!UsedBy.TryGetValue(use.Item, out var list)) UsedBy[use.Item] = list = new List<CookRow>();
                list.Add(row);
            }
        }
    }

    private static ObjectDB? _meadDb;
    private static int _nativeRecipeCount;
    private static ZNetScene? _catalogueScene;
    // Discover mead bases and the native recipes required by the Cook graph.
    // Costs/outputs/station levels come from ObjectDB; Cook overrides remain authoritative.
    private static void EnsureMeads()
    {
        EnsureConversions();
        var db = ObjectDB.instance;
        if (db == null || db.m_recipes == null || db.m_recipes.Count == 0
            || (db == _meadDb && _nativeRecipeCount == db.m_recipes.Count && _catalogueScene == ZNetScene.instance)) return;
        _meadDb = db; _nativeRecipeCount = db.m_recipes.Count; _catalogueScene = ZNetScene.instance;
        IndexConfiguredRows();
        var native = new Dictionary<string,Recipe>(StringComparer.Ordinal);
        foreach (var recipe in db.m_recipes)
            if (recipe != null && recipe.m_enabled && recipe.m_item != null && !native.ContainsKey(recipe.m_item.name))
                native.Add(recipe.m_item.name,recipe);
        var pending = new Queue<string>();
        foreach (var row in _rows)
            foreach (var use in row.Uses) pending.Enqueue(use.Item);
        foreach (var output in native.Keys)
            if (output.StartsWith("Mead",StringComparison.OrdinalIgnoreCase)) pending.Enqueue(output);
        // Native processes always win over a direct finished-item recipe. Baking
        // prepares the raw conversion input using the Cook recipe's ingredients.
        foreach (var pair in Products)
        {
            var conversion = pair.Value;
            if (conversion.Kind == KitchenStationKind.Oven && ByOutput.TryGetValue(pair.Key, out var finished))
            {
                native.TryGetValue(conversion.From, out var preparation);
                var raw = Item(conversion.From);
                if (raw == null) continue;
                var row = new CookRow { Id = "prepare:" + conversion.From, Prefab = conversion.From,
                    Name = Label(conversion.From), Kind = "ingredient", Source = "vanilla", Operation = "reference",
                    Station = preparation?.m_craftingStation != null ? Clean(preparation.m_craftingStation.gameObject.name) : finished.Station,
                    StationLevel = Math.Max(preparation?.m_minStationLevel ?? 1, finished.StationLevel),
                    OutputAmount = Math.Max(1, finished.OutputAmount), PreparationSeconds = finished.PreparationSeconds };
                foreach (var use in finished.Uses) row.Uses.Add(new CookUse { Item = use.Item, Amount = use.Amount });
                ByOutput[conversion.From] = row; ById[row.Id] = row;
                if (!UsedBy.TryGetValue(conversion.From,out var bakedUses)) UsedBy[conversion.From] = bakedUses = new List<CookRow>();
                bakedUses.Add(finished);
                foreach (var use in row.Uses)
                {
                    pending.Enqueue(use.Item);
                    if (!UsedBy.TryGetValue(use.Item,out var list)) UsedBy[use.Item] = list = new List<CookRow>();
                    list.Add(row);
                }
            }
            if (conversion.Kind != KitchenStationKind.Fermenter) continue;
            pending.Enqueue(conversion.From);
            if (ByOutput.ContainsKey(pair.Key)) continue;
            var mead = new CookRow { Id = "ferment:" + pair.Key, Prefab = pair.Key, Name = Label(pair.Key),
                Kind = "mead", Source = "vanilla", Operation = "reference", Station = "fermenter", OutputAmount = conversion.Amount };
            mead.Uses.Add(new CookUse { Item = conversion.From, Amount = 1 });
            _rows.Add(mead); ById[mead.Id] = mead; ByOutput[pair.Key] = mead;
            if (!UsedBy.TryGetValue(conversion.From,out var parents)) UsedBy[conversion.From] = parents = new List<CookRow>();
            parents.Add(mead);
        }
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            var output = pending.Dequeue();
            if (!visited.Add(output) || ByOutput.ContainsKey(output) || !native.TryGetValue(output,out var recipe)) continue;
            var item = recipe.m_item;
            if (item == null) continue;
            var row = new CookRow { Id = "native:" + output, Prefab = output,
                Name = Localization.instance != null ? Localization.instance.Localize(item.m_itemData.m_shared.m_name) : item.m_itemData.m_shared.m_name,
                Kind = output.StartsWith("Mead",StringComparison.OrdinalIgnoreCase) ? "mead" : "ingredient",
                Source = "vanilla", Operation = "reference", RecipeId = recipe.name,
                Station = recipe.m_craftingStation != null ? Clean(recipe.m_craftingStation.gameObject.name) : "",
                StationLevel = recipe.m_minStationLevel, OutputAmount = recipe.m_amount };
            foreach (var req in recipe.m_resources)
                if (req?.m_resItem != null && req.m_amount > 0) row.Uses.Add(new CookUse { Item = req.m_resItem.name, Amount = req.m_amount });
            if (row.Uses.Count == 0) continue;
            _rows.Add(row); ById[row.Id] = row; ByOutput[output] = row;
            foreach (var use in row.Uses)
            {
                pending.Enqueue(use.Item);
                if (!UsedBy.TryGetValue(use.Item,out var list)) UsedBy[use.Item] = list = new List<CookRow>();
                list.Add(row);
            }
        }
    }
}

