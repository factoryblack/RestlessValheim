using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessPlant;

internal static class CropPieces
{
    private const float DefaultRespawnMinutes = 240f;
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

        var template = TemplatePiece();
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

            // Some vanilla forage pickables (e.g. mushrooms) ship with an LODGroup
            // or a mesh nested under a child tuned for how they're scattered in the
            // world; that setup doesn't always yield a visible model once cloned
            // into a standalone placeable piece. Force every renderer visible and
            // drop any LODGroup so the placed/ghost model always shows.
            foreach (var lod in go.GetComponentsInChildren<LODGroup>(true))
                Object.Destroy(lod);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
            }

            var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
            piece.m_groundOnly = true;
            piece.m_groundPiece = true;
            piece.m_allowAltGroundPlacement = false;
            piece.m_noInWater = true;
            piece.m_cultivatedGroundOnly = true;
            piece.m_icon = icon;
            if (template != null)
                CopyPlacement(piece, template);

            FlattenLod(go);
            TunePickable(source, go);

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

    private static Piece? TemplatePiece()
    {
        foreach (var name in new[] { "sapling_carrot", "Plant_Carrot", "CarrotSeeds" })
        {
            var piece = PrefabManager.Instance.GetPrefab(name)?.GetComponent<Piece>();
            if (piece != null)
                return piece;
        }

        return null;
    }

    private static void CopyPlacement(Piece dest, Piece src)
    {
        dest.m_placeEffect = src.m_placeEffect;
        dest.m_category = src.m_category;
    }

    // Placement ghosts use the prefab mesh. LODGroup often hides every renderer at preview distance.
    private static void FlattenLod(GameObject go)
    {
        foreach (var lod in go.GetComponentsInChildren<LODGroup>(true))
        {
            var keep = lod.GetLODs();
            var show = keep.Length > 0 ? keep[0].renderers : null;
            foreach (var rend in lod.GetComponentsInChildren<Renderer>(true))
            {
                if (rend == null)
                    continue;
                rend.enabled = show == null || System.Array.IndexOf(show, rend) >= 0;
            }

            lod.enabled = false;
            Object.DestroyImmediate(lod);
        }
    }

    // Bushes and forage regrow fruit through Pickable — not ReplantOnHarvest.
    private static void TunePickable(GameObject source, GameObject clone)
    {
        var src = source.GetComponentInChildren<Pickable>(true);
        var dst = clone.GetComponentInChildren<Pickable>(true);
        if (src == null || dst == null)
            return;

        dst.m_respawnTimeMinutes = src.m_respawnTimeMinutes;
        dst.m_amount = src.m_amount;
        if (dst.m_respawnTimeMinutes <= 0f)
            dst.m_respawnTimeMinutes = DefaultRespawnMinutes;
    }

    private static string? ItemOf(CropRow row, GameObject source)
    {
        if (PrefabManager.Instance.GetPrefab(row.Item) != null)
            return row.Item;

        var pickable = source.GetComponentInChildren<Pickable>(true);
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
