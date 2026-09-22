using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessDrawers;

internal static class DrawerPieces
{
    private static bool _added;

    public static void Load() => PrefabManager.OnVanillaPrefabsAvailable += Add;

    private static void Add()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Add;
        if (_added)
            return;
        _added = true;

        var n = 0;
        foreach (var row in DrawerBook.Rows)
        {
            var source = PrefabManager.Instance.GetPrefab(row.Source);
            if (source == null)
            {
                Plugin.Log.LogInfo("RestlessDrawers skipped " + row.Source + " (no vanilla prefab).");
                continue;
            }

            var go = PrefabManager.Instance.CreateClonedPrefab(row.Prefab, row.Source);
            if (go == null)
                continue;

            DrawerVisual.Apply(go, row);

            var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
            var icon = DrawerMesh.Icon(row.Id) ?? piece.m_icon;
            piece.m_icon = icon;
            piece.m_name = row.Name;
            piece.m_description = row.Description;
            piece.m_groundOnly = false;
            piece.m_groundPiece = false;
            piece.m_clipGround = false;
            SitAsFurniture(go);

            var cfg = new PieceConfig
            {
                Name = row.Name,
                Description = row.Description,
                PieceTable = PieceTables.Hammer,
                Icon = icon
            };
            if (piece.m_craftingStation != null)
                cfg.CraftingStation = piece.m_craftingStation.name;
            if (piece.m_resources != null)
            {
                foreach (var req in piece.m_resources)
                {
                    if (req?.m_resItem == null || req.m_amount <= 0)
                        continue;
                    cfg.AddRequirement(req.m_resItem.name, req.m_amount, req.m_recover);
                }
            }

            PieceManager.Instance.AddPiece(new CustomPiece(go, true, cfg));
            n++;
        }

        Plugin.Log.LogInfo("RestlessDrawers added " + n + " drawers.");
    }

    // Cabinets are furniture, not studs. They can hold a neighbour, but
    // they do not run the chest AABB support graph (see DrawerGrid).
    private static void SitAsFurniture(GameObject prefab)
    {
        var wear = prefab.GetComponent<WearNTear>();
        if (wear == null)
            return;
        wear.m_noSupportWear = false;
        wear.m_supports = true;
        AccessTools.Method(typeof(WearNTear), "SetupColliders")?.Invoke(wear, null);
    }
}
