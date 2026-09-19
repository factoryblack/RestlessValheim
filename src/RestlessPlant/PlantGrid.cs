using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantGrid
{
    private static readonly List<GameObject> Ghosts = new();
    private static Piece? _lastPiece;
    private static int _lastTold;
    private static bool _placing;

    public static void Tick()
    {
        if (!PlantConfig.On || !PlantConfig.Grid.Value)
            return;
        var player = Player.m_localPlayer;
        if (player == null || !player.InPlaceMode())
            return;

        if (PlantConfig.SizeUp.Value.IsDown())
            Nudge(1);
        else if (PlantConfig.SizeDown.Value.IsDown())
            Nudge(-1);
    }

    private static void Nudge(int delta)
    {
        var next = Mathf.Clamp(PlantConfig.GridSize.Value + delta * 2, 1, 7);
        if (next == PlantConfig.GridSize.Value)
            return;
        PlantConfig.GridSize.Value = next;
        var player = Player.m_localPlayer;
        player?.Message(MessageHud.MessageType.TopLeft, "Plant " + next + "×" + next);
        _lastTold = next;
    }

    public static bool Active(Player player)
    {
        if (!PlantConfig.On || !PlantConfig.Grid.Value || player == null)
            return false;
        if (PlantConfig.GridSize.Value <= 1)
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

    public static IEnumerable<Vector3> Cells(Transform ghost)
    {
        var size = PlantConfig.GridSize.Value;
        if (size < 1)
            size = 1;
        if ((size & 1) == 0)
            size--;
        var mid = size / 2;
        var space = Spacing(ghost.GetComponent<Piece>() ?? ghost.GetComponentInChildren<Piece>());
        var origin = ghost.position;
        var right = ghost.right;
        var fwd = ghost.forward;
        for (var z = 0; z < size; z++)
        {
            for (var x = 0; x < size; x++)
            {
                if (x == mid && z == mid)
                    continue;
                var pos = origin + (x - mid) * space * right + (z - mid) * space * fwd;
                if (Physics.Raycast(pos + Vector3.up * 3f, Vector3.down, out var hit, 8f,
                        ~0, QueryTriggerInteraction.Ignore))
                    pos = hit.point;
                yield return pos;
            }
        }
    }

    private static float Spacing(Piece? piece)
    {
        var grow = piece != null ? piece.GetComponent<Plant>() : null;
        var radius = grow != null ? grow.m_growRadius * 2f : 0f;
        return Mathf.Max(PlantConfig.Spacing.Value, radius);
    }

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

            if (!Active(__instance) || __instance.m_placementStatus != Player.PlacementStatus.Valid)
            {
                Hide();
                return;
            }

            var ghost = __instance.m_placementGhost;
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
            foreach (var pos in Cells(ghost.transform))
            {
                if (i >= Ghosts.Count || Ghosts[i] == null)
                {
                    var extra = MakeGhost(ghost, pos, ghost.transform.rotation);
                    if (i < Ghosts.Count)
                        Ghosts[i] = extra;
                    else
                        Ghosts.Add(extra);
                }
                else
                {
                    Ghosts[i].transform.SetPositionAndRotation(pos, ghost.transform.rotation);
                    Ghosts[i].SetActive(true);
                }

                i++;
            }

            for (var n = Ghosts.Count - 1; n >= i; n--)
            {
                if (Ghosts[n] != null)
                    Object.Destroy(Ghosts[n]);
                Ghosts.RemoveAt(n);
            }

            if (_lastTold != PlantConfig.GridSize.Value)
            {
                _lastTold = PlantConfig.GridSize.Value;
                __instance.Message(MessageHud.MessageType.TopLeft,
                    "Plant " + _lastTold + "×" + _lastTold + "  [ ] resize");
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
                foreach (var pos in Cells(ghost.transform))
                {
                    if (!__instance.HaveRequirements(piece, Player.RequirementMode.CanBuild))
                        break;
                    __instance.PlacePiece(piece, pos, rot, false, false);
                }
            }
            finally
            {
                _placing = false;
            }
        }
    }
}
