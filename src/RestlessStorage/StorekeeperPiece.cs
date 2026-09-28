using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessStorage;

internal static class StorekeeperPiece
{
    public const string PrefabName = "piece_restless_storekeeper";
    public const string DisplayName = "Storekeeper's Table";
    public const string Blurb = "A ledger for the chests you can reach.";

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
            Plugin.Log.LogWarning("RestlessStorage skipped the table (no " + Donor + ").");
            return;
        }

        var go = PrefabManager.Instance.CreateClonedPrefab(PrefabName, Donor);
        if (go == null)
            return;

        var container = go.GetComponent<Container>();
        if (container != null)
            Object.DestroyImmediate(container);

        TableVisual.Apply(go);

        var piece = go.GetComponent<Piece>() ?? go.AddComponent<Piece>();
        var icon = TableArt.Icon() ?? piece.m_icon;
        piece.m_icon = icon;
        piece.m_name = DisplayName;
        piece.m_description = Blurb;
        piece.m_groundOnly = false;
        piece.m_groundPiece = false;
        piece.m_clipGround = false;
        piece.m_category = Piece.PieceCategory.Furniture;
        SitAsFurniture(go);

        if (go.GetComponent<Storekeeper>() == null)
            go.AddComponent<Storekeeper>();

        var cfg = new PieceConfig
        {
            Name = DisplayName,
            Description = Blurb,
            PieceTable = PieceTables.Hammer,
            Category = PieceCategories.Furniture,
            CraftingStation = "piece_workbench",
            Icon = icon
        };
        cfg.AddRequirement("Wood", 20, true);
        cfg.AddRequirement("FineWood", 10, true);
        cfg.AddRequirement("BronzeNails", 10, true);

        PieceManager.Instance.AddPiece(new CustomPiece(go, true, cfg));
        Plugin.Log.LogInfo("RestlessStorage added the Storekeeper's Table.");
    }

    // Furniture, not a stud. Same idea as the drawer cabinets.
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
