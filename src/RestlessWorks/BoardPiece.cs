using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessWorks;

internal static class BoardPiece
{
    public const string PrefabName = "piece_restless_works";
    public const string DisplayName = "Work-order board";
    public const string Blurb = "Standing orders for the kilns and smelters nearby.";

    private const string Donor = "piece_chest_wood";
    private static bool _added;

    public static void Load() => PrefabManager.OnVanillaPrefabsAvailable += Add;

    private static void Add()
    {
        PrefabManager.OnVanillaPrefabsAvailable -= Add;
        if (_added)
            return;
        _added = true;

        if (PrefabManager.Instance.GetPrefab(Donor) == null)
        {
            Plugin.Log.LogWarning("RestlessWorks skipped the board (no " + Donor + ").");
            return;
        }

        var go = PrefabManager.Instance.CreateClonedPrefab(PrefabName, Donor);
        if (go == null)
            return;

        var container = go.GetComponent<Container>();
        if (container != null)
            Object.DestroyImmediate(container);

        BoardVisual.Apply(go);

        var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
        var icon = BoardArt.Icon() ?? piece.m_icon;
        piece.m_icon = icon;
        piece.m_name = DisplayName;
        piece.m_description = Blurb;
        piece.m_groundOnly = false;
        piece.m_groundPiece = false;
        piece.m_clipGround = false;
        piece.m_category = Piece.PieceCategory.Furniture;
        SitAsFurniture(go);

        if (go.GetComponent<Board>() == null)
            go.AddComponent<Board>();

        var cfg = new PieceConfig
        {
            Name = DisplayName,
            Description = Blurb,
            PieceTable = PieceTables.Hammer,
            Category = PieceCategories.Furniture,
            CraftingStation = "piece_workbench",
            Icon = icon
        };
        cfg.AddRequirement("Wood", 10, true);
        cfg.AddRequirement("Coal", 4, true);
        cfg.AddRequirement("BronzeNails", 4, true);

        PieceManager.Instance.AddPiece(new CustomPiece(go, true, cfg));
        Plugin.Log.LogInfo("RestlessWorks added the work-order board.");
    }

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
