using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static ObjectDB? _meadDb;
    private static int _nativeRecipeCount;
    // Discover mead bases and the native recipes required by the Cook graph.
    // Costs/outputs/station levels come from ObjectDB; Cook overrides remain authoritative.
    private static void EnsureMeads()
    {
        var db = ObjectDB.instance;
        if (db == null || db.m_recipes == null || db.m_recipes.Count == 0
            || (db == _meadDb && _nativeRecipeCount == db.m_recipes.Count)) return;
        _meadDb = db; _nativeRecipeCount = db.m_recipes.Count;
        var native = new Dictionary<string,Recipe>(StringComparer.Ordinal);
        foreach (var recipe in db.m_recipes)
            if (recipe != null && recipe.m_enabled && recipe.m_item != null && !native.ContainsKey(recipe.m_item.name))
                native.Add(recipe.m_item.name,recipe);
        var pending = new Queue<string>();
        foreach (var row in _rows)
            foreach (var use in row.Uses) pending.Enqueue(use.Item);
        foreach (var output in native.Keys)
            if (output.StartsWith("Mead",StringComparison.OrdinalIgnoreCase)) pending.Enqueue(output);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (pending.Count > 0)
        {
            var output = pending.Dequeue();
            if (!visited.Add(output) || ByOutput.ContainsKey(output) || !native.TryGetValue(output,out var recipe)) continue;
            var item = recipe.m_item;
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
