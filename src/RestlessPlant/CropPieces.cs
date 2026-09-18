using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessPlant;

internal static class CropPieces
{
    private static bool _added;

    public static void Load() => PrefabManager.OnVanillaPrefabsAvailable += Add;

    private static void Add()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Add;
        if (_added)
            return;
        _added = true;
        if (!PlantConfig.On || !PlantConfig.ExtraCrops.Value)
            return;

        var n = 0;
        foreach (var row in CropBook.Rows)
        {
            var source = PrefabManager.Instance.GetPrefab(row.Source);
            if (source == null)
            {
                Plugin.Log.LogInfo($"RestlessPlant skipped {row.Source} (no vanilla prefab).");
                continue;
            }

            var item = ItemOf(row, source);
            if (item == null)
            {
                Plugin.Log.LogInfo($"RestlessPlant skipped {row.Source} (no item {row.Item}).");
                continue;
            }

            var icon = IconOf(item);
            if (icon == null)
            {
                Plugin.Log.LogInfo($"RestlessPlant skipped {row.Source} (no icon on {item}).");
                continue;
            }

            var go = PrefabManager.Instance.CreateClonedPrefab(row.Prefab, row.Source);
            if (go == null)
                continue;

            var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
            piece.m_groundOnly = true;
            piece.m_allowAltGroundPlacement = false;
            piece.m_noInWater = true;
            piece.m_icon = icon;

            var cfg = new PieceConfig
            {
                Name = row.Name,
                Description = row.Description,
                PieceTable = PieceTables.Cultivator,
                CraftingStation = CraftingStations.None,
                Icon = icon
            };
            cfg.AddRequirement(item, 1, true);
            PieceManager.Instance.AddPiece(new CustomPiece(go, true, cfg));
            n++;
        }

        Plugin.Log.LogInfo($"RestlessPlant added {n} cultivator crops.");
    }

    private static string? ItemOf(CropRow row, GameObject source)
    {
        if (PrefabManager.Instance.GetPrefab(row.Item) != null)
            return row.Item;

        var pickable = source.GetComponent<Pickable>();
        if (pickable?.m_itemPrefab == null)
            return null;
        return Utils.GetPrefabName(pickable.m_itemPrefab);
    }

    private static Sprite? IconOf(string item)
    {
        var drop = PrefabManager.Instance.GetPrefab(item)?.GetComponent<ItemDrop>();
        if (drop == null)
            return null;
        try
        {
            return drop.m_itemData.GetIcon();
        }
        catch
        {
            var icons = drop.m_itemData?.m_shared?.m_icons;
            return icons is { Length: > 0 } ? icons[0] : null;
        }
    }
}
