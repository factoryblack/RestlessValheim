using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantGrid
{
    private readonly struct Cell
    {
        public Cell(Vector3 pos, bool ok)
        {
            Pos = pos;
            Ok = ok;
        }

        public Vector3 Pos { get; }
        public bool Ok { get; }
    }

    private static readonly List<GameObject> Ghosts = new();
    private static readonly MaterialPropertyBlock Block = new();
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color Bad = new(1f, 0.35f, 0.35f, 1f);
    private static Piece? _lastPiece;
    private static int _lastCols;
    private static int _lastRows;
    private static bool _lastSnap;
    private static bool _placing;

    public static void Tick()
    {
        if (RestlessQoL.Api.SettingsPageApi.IsMenuOpen) return;
        if (!PlantConfig.On || !PlantConfig.Grid.Value)
            return;
        var player = Player.m_localPlayer;
        if (player == null || !player.InPlaceMode() || !Cultivating(player))
            return;

        if (PlantConfig.SnapToggle.Value.IsDown())
        {
            PlantConfig.SnapToField.Value = !PlantConfig.SnapToField.Value;
            player.Message(MessageHud.MessageType.TopLeft,
                PlantConfig.SnapToField.Value ? "Plant snap on" : "Plant snap off");
            _lastSnap = PlantConfig.SnapToField.Value;
        }

        if (PlantConfig.ColsUp.Value.IsDown())
            Nudge(PlantConfig.GridColumns, 1, "wide");
        else if (PlantConfig.ColsDown.Value.IsDown())
            Nudge(PlantConfig.GridColumns, -1, "wide");
        else if (PlantConfig.RowsUp.Value.IsDown())
            Nudge(PlantConfig.GridRows, 1, "deep");
        else if (PlantConfig.RowsDown.Value.IsDown())
            Nudge(PlantConfig.GridRows, -1, "deep");
    }

    private static void Nudge(BepInEx.Configuration.ConfigEntry<int> entry, int delta, string axis)
    {
        var next = Mathf.Clamp(entry.Value + delta, 1, 7);
        if (next == entry.Value)
            return;
        entry.Value = next;
        var player = Player.m_localPlayer;
        player?.Message(MessageHud.MessageType.TopLeft,
            "Plant " + PlantConfig.GridColumns.Value + "×" + PlantConfig.GridRows.Value + " " + axis);
        _lastCols = PlantConfig.GridColumns.Value;
        _lastRows = PlantConfig.GridRows.Value;
    }

    public static bool Wide =>
        PlantConfig.GridColumns.Value > 1 || PlantConfig.GridRows.Value > 1;

    public static bool Active(Player player)
    {
        if (!PlantConfig.On || !PlantConfig.Grid.Value || player == null || !Wide)
            return false;
        return Cultivating(player) && Crop(player.GetSelectedPiece()) && player.m_placementGhost != null;
    }

    // Ground till is a cultivator piece with no plant. A wide grid must not stamp that.
    private static bool Crop(Piece? piece) =>
        piece != null && (piece.GetComponent<Plant>() != null || piece.GetComponentInChildren<Pickable>(true) != null);

    public static bool Cultivating(Player player)
    {
        var table = player.m_buildPieces;
        if (table == null)
            return false;
        var name = table.name ?? "";
        return name.IndexOf("Cultivator", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static IEnumerable<Cell> Cells(Player player, Transform ghost)
    {
        var cols = Mathf.Clamp(PlantConfig.GridColumns.Value, 1, 7);
        var rows = Mathf.Clamp(PlantConfig.GridRows.Value, 1, 7);
        var midX = (cols - 1) / 2;
        var midZ = (rows - 1) / 2;
        var piece = player.GetSelectedPiece();
        var space = Spacing(ghost.GetComponent<Piece>() ?? piece);
        var origin = ghost.position;
        var right = Flat(ghost.right);
        var fwd = Flat(ghost.forward);
        var plant = ghost.GetComponent<Plant>();
        for (var z = 0; z < rows; z++)
        {
            for (var x = 0; x < cols; x++)
            {
                if (x == midX && z == midZ)
                    continue;
                var pos = origin + (x - midX) * space * right + (z - midZ) * space * fwd;
                var grounded = Sit(pos, out pos);
                yield return new Cell(pos, grounded && CanGrow(ghost.gameObject, piece, plant, pos));
            }
        }
    }

    private static Vector3 Flat(Vector3 axis)
    {
        axis.y = 0f;
        return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
    }

    // A hair past the grow diameter so a neighbour's collider is outside the grow sphere.
    private const float GrowGap = 0.1f;

    private static float Spacing(Piece? piece)
    {
        var grow = piece != null ? piece.GetComponent<Plant>() : null;
        var natural = grow != null && grow.m_growRadius > 0.05f
            ? grow.m_growRadius * 2f + GrowGap
            : 1f;
        var chosen = PlantConfig.Spacing.Value;
        if (chosen <= 0.01f)
            return natural;
        return Mathf.Max(chosen, natural);
    }

    private static bool Sit(Vector3 pos, out Vector3 grounded)
    {
        grounded = pos;
        var zones = ZoneSystem.instance;
        if (zones == null)
            return false;
        if (PlantGrow.Free && zones.GetSolidHeight(pos, out var solid))
        {
            grounded = new Vector3(pos.x, solid, pos.z);
            return true;
        }

        if (zones.GetGroundHeight(pos, out var y))
        {
            grounded = new Vector3(pos.x, y, pos.z);
            return true;
        }

        return false;
    }

    private static bool CanGrow(GameObject ghost, Piece? piece, Plant? plant, Vector3 pos)
    {
        if (PlantGrow.Free)
            return true;
        var map = Heightmap.FindHeightmap(pos);
        if (map == null)
            return false;
        if (NeedsTill(piece, plant) && !map.IsCultivated(pos))
            return false;
        if (plant != null)
            return Probe(ghost, plant, pos) == Plant.Status.Healthy;
        if (piece != null && piece.m_onlyInBiome != 0 && (map.GetBiome(pos) & piece.m_onlyInBiome) == 0)
            return false;
        return !Physics.CheckSphere(pos, 0.4f, Plant.m_spaceMask, QueryTriggerInteraction.Ignore);
    }

    private static bool NeedsTill(Piece? piece, Plant? plant)
    {
        if (plant != null)
            return plant.m_needCultivatedGround || (piece != null && piece.m_cultivatedGroundOnly);
        return piece == null || piece.m_cultivatedGroundOnly;
    }

    private static bool _probeLogged;

    private static Plant.Status Probe(GameObject ghost, Plant plant, Vector3 pos)
    {
        var t = ghost.transform;
        var saved = t.position;
        var cols = ghost.GetComponentsInChildren<Collider>(true);
        var on = new bool[cols.Length];
        for (var i = 0; i < cols.Length; i++)
        {
            on[i] = cols[i].enabled;
            cols[i].enabled = false;
        }

        var status = plant.m_status;
        try
        {
            t.position = pos;
            // GetStatus is the last result. Ask again at this cell, or every
            // extra copies the centre and an oversized grid plants wild ground.
            plant.UpdateHealth(11.0);
            return plant.GetStatus();
        }
        catch (System.Exception ex)
        {
            if (!_probeLogged)
            {
                _probeLogged = true;
                Plugin.Log.LogWarning($"Plant grow check failed; later cells stay quiet. {ex}");
            }

            return Plant.Status.NoSpace;
        }
        finally
        {
            plant.m_status = status;
            t.position = saved;
            for (var i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = on[i];
            }
        }
    }

    private static void Align(Player player, GameObject ghost)
    {
        if (!PlantConfig.SnapToField.Value)
            return;
        var piece = player.GetSelectedPiece();
        var space = Spacing(ghost.GetComponent<Piece>() ?? piece);
        var field = Field(ghost.transform.position, space, ghost);
        if (field == null)
            return;

        var right = Flat(field.right);
        var fwd = Flat(field.forward);
        var delta = ghost.transform.position - field.position;
        var x = Mathf.Round(Vector3.Dot(delta, right) / space);
        var z = Mathf.Round(Vector3.Dot(delta, fwd) / space);
        if (Mathf.Abs(x) < 0.01f && Mathf.Abs(z) < 0.01f)
        {
            var alongR = Vector3.Dot(delta, right);
            var alongF = Vector3.Dot(delta, fwd);
            if (Mathf.Abs(alongR) >= Mathf.Abs(alongF))
                x = alongR >= 0f ? 1f : -1f;
            else
                z = alongF >= 0f ? 1f : -1f;
        }

        if (!Sit(field.position + right * (x * space) + fwd * (z * space), out var pos))
            return;
        ghost.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
    }

    // Plant.m_pieceMask stays 0 until a vine looks for a wall, and that runs on
    // the owner. A farmer who has never loaded one gets an empty search.
    // Ripe bushes sit on item, and a lot of crop colliders are triggers.
    private static int _fieldMask;

    private static int FieldMask
    {
        get
        {
            if (_fieldMask == 0)
                _fieldMask = LayerMask.GetMask("piece", "piece_nonsolid", "item");
            return _fieldMask;
        }
    }

    private static Transform? Field(Vector3 from, float space, GameObject ghost)
    {
        var range = Mathf.Max(PlantConfig.SnapRange.Value, space + 0.25f);
        var hits = Physics.OverlapSphere(from, range, FieldMask, QueryTriggerInteraction.Collide);
        Transform? best = null;
        var bestD = range * range;
        foreach (var hit in hits)
        {
            if (hit == null)
                continue;
            var anchor = Anchor(hit, ghost);
            if (anchor == null)
                continue;
            var d = (anchor.position - from).sqrMagnitude;
            if (d > bestD)
                continue;
            best = anchor;
            bestD = d;
        }

        return best;
    }

    // Snap to the crop itself. A pickable's transform.root is often the whole
    // location, and aligning to that flings the cultivator ghost across the field.
    private static Transform? Anchor(Collider hit, GameObject ghost)
    {
        var plant = hit.GetComponentInParent<Plant>();
        var pick = plant == null ? hit.GetComponentInParent<Pickable>() : null;
        var anchor = plant != null ? plant.transform : pick != null ? pick.transform : null;
        if (anchor == null)
            return null;
        if (anchor == ghost.transform || anchor.IsChildOf(ghost.transform))
            return null;
        if (IsGhost(anchor.gameObject) || IsGhost(anchor.root.gameObject))
            return null;
        return anchor;
    }

    private static bool IsGhost(GameObject go) =>
        go != null && go.name.IndexOf("RestlessPlantGhost", System.StringComparison.Ordinal) >= 0;

    public static void Hide()
    {
        foreach (var ghost in Ghosts)
        {
            if (ghost != null)
                ghost.SetActive(false);
        }
    }

    public static void Clear()
    {
        foreach (var ghost in Ghosts)
        {
            if (ghost == null)
                continue;
            ghost.SetActive(false);
            Object.Destroy(ghost);
        }

        Ghosts.Clear();
        _lastPiece = null;
    }

    private static GameObject MakeGhost(GameObject source, Vector3 pos, Quaternion rot)
    {
        var on = source.activeSelf;
        source.SetActive(false);
        var extra = Object.Instantiate(source);
        source.SetActive(on);
        extra.name = "RestlessPlantGhost";
        extra.transform.SetPositionAndRotation(pos, rot);
        Strip(extra);
        extra.SetActive(true);
        return extra;
    }

    private static void Strip(GameObject go)
    {
        foreach (var view in go.GetComponentsInChildren<ZNetView>(true))
            Object.DestroyImmediate(view);
        foreach (var piece in go.GetComponentsInChildren<Piece>(true))
            Object.DestroyImmediate(piece);
        foreach (var wear in go.GetComponentsInChildren<WearNTear>(true))
            Object.DestroyImmediate(wear);
        foreach (var pickable in go.GetComponentsInChildren<Pickable>(true))
            Object.DestroyImmediate(pickable);
        foreach (var plant in go.GetComponentsInChildren<Plant>(true))
            Object.DestroyImmediate(plant);
        foreach (var body in go.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);
        foreach (var col in go.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(col);
        foreach (var lod in go.GetComponentsInChildren<LODGroup>(true))
            lod.enabled = false;
    }

    private static void Paint(GameObject go, bool ok)
    {
        Block.Clear();
        Block.SetColor(ColorId, ok ? Color.white : Bad);
        foreach (var rend in go.GetComponentsInChildren<Renderer>(true))
        {
            if (rend != null)
                rend.SetPropertyBlock(Block);
        }
    }

    private static bool FreeBuild(Player player, Piece piece)
    {
        if (player.m_noPlacementCost)
            return true;
        return ZoneSystem.instance != null && ZoneSystem.instance.GetGlobalKey(piece.FreeBuildKey());
    }

    // Seeds still in the inventory include the centre, which the click pays
    // for after TryPlacePiece returns.
    private static int ExtraBudget(Player player, Piece piece)
    {
        if (FreeBuild(player, piece))
            return int.MaxValue;
        var spare = int.MaxValue;
        var any = false;
        foreach (var req in piece.m_resources)
        {
            if (req?.m_resItem?.m_itemData?.m_shared == null || req.m_amount <= 0)
                continue;
            any = true;
            var have = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
            var can = (have - req.m_amount) / req.m_amount;
            if (can < spare)
                spare = can;
        }

        if (!any)
            return int.MaxValue;
        return spare < 0 ? 0 : spare;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        private static void AfterGhost(Player __instance)
        {
            if (_placing)
                return;
            if (Cultivating(__instance))
            {
                var selected = __instance.GetSelectedPiece();
                if (selected != null)
                    PlantHarvest.Remember(selected);
            }

            var ghost = __instance.m_placementGhost;
            if (ghost != null && Cultivating(__instance) && PlantConfig.On && PlantConfig.Grid.Value
                && Crop(__instance.GetSelectedPiece()))
                Align(__instance, ghost);

            if (!Active(__instance) || (__instance.m_placementStatus != Player.PlacementStatus.Valid && !PlantGrow.Free))
            {
                Hide();
                return;
            }

            if (ghost == null)
            {
                Hide();
                return;
            }

            var piece = __instance.GetSelectedPiece();
            if (piece != _lastPiece)
            {
                Clear();
                _lastPiece = piece;
                PlantHarvest.Remember(piece);
            }

            var i = 0;
            foreach (var cell in Cells(__instance, ghost.transform))
            {
                if (i >= Ghosts.Count || Ghosts[i] == null)
                {
                    var extra = MakeGhost(ghost, cell.Pos, ghost.transform.rotation);
                    if (i < Ghosts.Count)
                        Ghosts[i] = extra;
                    else
                        Ghosts.Add(extra);
                }
                else
                {
                    Ghosts[i].transform.SetPositionAndRotation(cell.Pos, ghost.transform.rotation);
                    Ghosts[i].SetActive(true);
                }

                Paint(Ghosts[i], cell.Ok);
                i++;
            }

            for (var n = Ghosts.Count - 1; n >= i; n--)
            {
                if (Ghosts[n] != null)
                    Object.Destroy(Ghosts[n]);
                Ghosts.RemoveAt(n);
            }

            if (_lastCols != PlantConfig.GridColumns.Value || _lastRows != PlantConfig.GridRows.Value ||
                _lastSnap != PlantConfig.SnapToField.Value)
            {
                _lastCols = PlantConfig.GridColumns.Value;
                _lastRows = PlantConfig.GridRows.Value;
                _lastSnap = PlantConfig.SnapToField.Value;
                __instance.Message(MessageHud.MessageType.TopLeft,
                    "Plant " + _lastCols + "×" + _lastRows + "  [ ] wide  - = deep" +
                    (_lastSnap ? "  snap" : ""));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.TryPlacePiece))]
        private static void AfterPlace(Player __instance, Piece piece, bool __result)
        {
            if (_placing || !__result || !Active(__instance) || piece == null)
                return;
            var ghost = __instance.m_placementGhost;
            if (ghost == null)
                return;
            var rot = ghost.transform.rotation;
            // The click pays for the centre after this returns. Extras have to
            // pay now, or one seed still fills the whole grid.
            var budget = ExtraBudget(__instance, piece);
            var placed = 0;
            _placing = true;
            try
            {
                foreach (var cell in Cells(__instance, ghost.transform))
                {
                    if (!cell.Ok)
                        continue;
                    if (placed >= budget)
                        break;
                    __instance.PlacePiece(piece, cell.Pos, rot, false, false);
                    if (!FreeBuild(__instance, piece))
                        __instance.ConsumeResources(piece.m_resources, 0);
                    placed++;
                }
            }
            finally
            {
                _placing = false;
            }
        }
    }
}
