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
        return Cultivating(player) && player.m_placementGhost != null;
    }

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

    private static float Spacing(Piece? piece)
    {
        var grow = piece != null ? piece.GetComponent<Plant>() : null;
        var radius = grow != null ? grow.m_growRadius * 2f : 0f;
        return Mathf.Max(PlantConfig.Spacing.Value, radius);
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
        if (plant != null)
            return Probe(ghost, plant, pos) == Plant.Status.Healthy;

        var map = Heightmap.FindHeightmap(pos);
        if (map == null)
            return false;
        if ((piece == null || piece.m_cultivatedGroundOnly) && !map.IsCultivated(pos))
            return false;
        if (piece != null && piece.m_onlyInBiome != 0 && (map.GetBiome(pos) & piece.m_onlyInBiome) == 0)
            return false;
        return !Physics.CheckSphere(pos, 0.4f, Plant.m_spaceMask, QueryTriggerInteraction.Ignore);
    }

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

        t.position = pos;
        var status = plant.GetStatus();
        t.position = saved;
        for (var i = 0; i < cols.Length; i++)
            cols[i].enabled = on[i];
        return status;
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

    private static Transform? Field(Vector3 from, float space, GameObject ghost)
    {
        var range = Mathf.Max(PlantConfig.SnapRange.Value, space + 0.25f);
        var hits = Physics.OverlapSphere(from, range, Plant.m_pieceMask, QueryTriggerInteraction.Ignore);
        Transform? best = null;
        var bestD = range * range;
        foreach (var hit in hits)
        {
            if (hit == null)
                continue;
            var root = hit.transform.root;
            if (root == ghost.transform || IsGhost(root.gameObject))
                continue;
            if (root.GetComponentInChildren<Plant>(true) == null &&
                root.GetComponentInChildren<Pickable>(true) == null)
                continue;
            var d = (root.position - from).sqrMagnitude;
            if (d < 0.01f || d > bestD)
                continue;
            best = root;
            bestD = d;
        }

        return best;
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
            col.enabled = false;
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
            if (ghost != null && Cultivating(__instance) && PlantConfig.On && PlantConfig.Grid.Value)
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
            _placing = true;
            try
            {
                foreach (var cell in Cells(__instance, ghost.transform))
                {
                    if (!cell.Ok)
                        continue;
                    if (!__instance.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                        break;
                    __instance.PlacePiece(piece, cell.Pos, rot, false, false);
                }
            }
            finally
            {
                _placing = false;
            }
        }
    }
}
