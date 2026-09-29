using System;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static ObjectDB? _meadDb;
    // Mead bases are vanilla recipes, not cook.yaml meals. Discover their real costs
    // and station requirements; this does not invent fermentation conversions.
    private static void EnsureMeads()
    {
        var db = ObjectDB.instance;
        if (db == null || db == _meadDb || db.m_recipes == null || db.m_recipes.Count == 0) return;
        _meadDb = db;
        foreach (var recipe in db.m_recipes)
        {
            var item = recipe?.m_item;
            if (item == null || !recipe!.m_enabled || !item.name.StartsWith("Mead", StringComparison.OrdinalIgnoreCase)
                || ByOutput.ContainsKey(item.name)) continue;
            var row = new CookRow { Id = "native:" + item.name, Prefab = item.name, Name = Localization.instance.Localize(item.m_itemData.m_shared.m_name),
                Kind = "mead", Source = "vanilla", Operation = "reference", RecipeId = recipe.name,
                Station = recipe.m_craftingStation != null ? Clean(recipe.m_craftingStation.gameObject.name) : "",
                StationLevel = recipe.m_minStationLevel, OutputAmount = recipe.m_amount };
            foreach (var req in recipe.m_resources)
                if (req?.m_resItem != null && req.m_amount > 0) row.Uses.Add(new CookUse { Item = req.m_resItem.name, Amount = req.m_amount });
            if (row.Uses.Count == 0) continue;
            _rows.Add(row); ById[row.Id] = row; ByOutput[row.Prefab] = row;
            foreach (var use in row.Uses)
            {
                if (!UsedBy.TryGetValue(use.Item, out var list)) UsedBy[use.Item] = list = new System.Collections.Generic.List<CookRow>();
                list.Add(row);
            }
        }
    }
}
