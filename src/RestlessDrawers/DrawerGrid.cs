using HarmonyLib;
using UnityEngine;

namespace RestlessDrawers;

// Shared cell for every tier. Wood snaps to black metal the same as wood to wood.
internal static class DrawerGrid
{
    public const float Cell = 1.1f;
    public const float Pull = 0.55f;
    public const float Overlap = 0.04f;
    public const float YawSlop = 8f;

    private static readonly Collider[] Near = new Collider[32];
    private static readonly Collider[] Occupied = new Collider[16];

    public static float Fit(Mesh mesh)
    {
        var size = mesh.bounds.size;
        var longest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        return Cell / Mathf.Max(0.05f, longest);
    }

    public static void Dress(GameObject prefab)
    {
        StripOldSnaps(prefab);
        var size = Extent(prefab.transform);
        var mid = new Vector3(size.x * 0.5f, size.y * 0.5f, size.z * 0.5f);
        AddSnap(prefab, "snap_r", new Vector3(mid.x, mid.y, 0f));
        AddSnap(prefab, "snap_l", new Vector3(-mid.x, mid.y, 0f));
        AddSnap(prefab, "snap_f", new Vector3(0f, mid.y, mid.z));
        AddSnap(prefab, "snap_b", new Vector3(0f, mid.y, -mid.z));
        AddSnap(prefab, "snap_u", new Vector3(0f, size.y, 0f));
        AddSnap(prefab, "snap_d", new Vector3(0f, 0f, 0f));
    }

    public static void Align(Player player)
    {
        if (player == null || !player.InPlaceMode())
            return;
        var ghost = player.m_placementGhost;
        if (ghost == null || ghost.GetComponent<DrawerFace>() == null)
            return;
        if (!TrySnap(ghost.transform, out var pos, out var rot))
            return;
        ghost.transform.SetPositionAndRotation(pos, rot);
        player.m_placementStatus = Player.PlacementStatus.Valid;
    }

    private static bool TrySnap(Transform ghost, out Vector3 pos, out Quaternion rot)
    {
        pos = ghost.position;
        rot = ghost.rotation;
        var range = Cell + Pull;
        var n = Physics.OverlapSphereNonAlloc(ghost.position, range, Near, ~0, QueryTriggerInteraction.Ignore);
        var bestD = Pull * Pull;
        var found = false;
        for (var i = 0; i < n; i++)
        {
            var col = Near[i];
            if (col == null)
                continue;
            var face = col.GetComponentInParent<DrawerFace>();
            if (face == null)
                continue;
            var other = face.transform;
            if (other == ghost || other.IsChildOf(ghost) || ghost.IsChildOf(other))
                continue;
            if (Mathf.Abs(Mathf.DeltaAngle(ghost.eulerAngles.y, other.eulerAngles.y)) > YawSlop)
                continue;

            var step = Step(other);
            var local = other.InverseTransformPoint(ghost.position);
            float gx, gy, gz;
            if (Mathf.Abs(local.x) < Pull && Mathf.Abs(local.z) < Pull)
            {
                // Same footprint: the lid, or the floor under the cabinet.
                gx = 0f;
                gz = 0f;
                gy = local.y < -step.y * 0.4f ? -1f : 1f;
            }
            else
            {
                gx = Mathf.Round(local.x / step.x);
                gy = Mathf.Round(local.y / step.y);
                gz = Mathf.Round(local.z / step.z);
                if (Mathf.Abs(gx) < 0.01f && Mathf.Abs(gy) < 0.01f && Mathf.Abs(gz) < 0.01f)
                {
                    if (Mathf.Abs(local.x) >= Mathf.Abs(local.z))
                        gx = local.x >= 0f ? 1f : -1f;
                    else
                        gz = local.z >= 0f ? 1f : -1f;
                }

                if (Mathf.Abs(gx) + Mathf.Abs(gy) + Mathf.Abs(gz) > 1.01f)
                    continue;
            }

            var target = other.TransformPoint(new Vector3(gx * step.x, gy * step.y, gz * step.z));
            if (Taken(target, ghost))
                continue;
            var d = (ghost.position - target).sqrMagnitude;
            if (d > bestD)
                continue;
            bestD = d;
            pos = target;
            rot = other.rotation;
            found = true;
        }

        return found;
    }

    private static bool Taken(Vector3 pos, Transform ghost)
    {
        var limit = 0.12f;
        var n = Physics.OverlapSphereNonAlloc(pos, Cell, Occupied, ~0, QueryTriggerInteraction.Ignore);
        for (var i = 0; i < n; i++)
        {
            var col = Occupied[i];
            if (col == null)
                continue;
            var face = col.GetComponentInParent<DrawerFace>();
            if (face == null)
                continue;
            var other = face.transform;
            if (other == ghost || other.IsChildOf(ghost) || ghost.IsChildOf(other))
                continue;
            if ((other.position - pos).sqrMagnitude < limit)
                return true;
        }

        return false;
    }

    // Visual size after fit — shorter cabinets snap a shorter step so the
    // next one sits on the lid instead of floating a cube-cell above it.
    private static Vector3 Extent(Transform drawer)
    {
        var visual = drawer.Find(DrawerVisual.ChildName);
        var filter = visual != null ? visual.GetComponent<MeshFilter>() : null;
        var mesh = filter != null ? filter.sharedMesh : null;
        if (mesh == null)
            return Vector3.one * Cell;
        var size = mesh.bounds.size;
        var scale = visual!.lossyScale;
        return new Vector3(
            Mathf.Max(0.2f, size.x * Mathf.Abs(scale.x)),
            Mathf.Max(0.2f, size.y * Mathf.Abs(scale.y)),
            Mathf.Max(0.2f, size.z * Mathf.Abs(scale.z)));
    }

    private static Vector3 Step(Transform drawer)
    {
        var size = Extent(drawer);
        return new Vector3(
            Mathf.Max(0.2f, size.x - Overlap),
            Mathf.Max(0.2f, size.y - Overlap),
            Mathf.Max(0.2f, size.z - Overlap));
    }

    private static void StripOldSnaps(GameObject prefab)
    {
        for (var i = prefab.transform.childCount - 1; i >= 0; i--)
        {
            var child = prefab.transform.GetChild(i);
            if (child.name.StartsWith("snap", System.StringComparison.OrdinalIgnoreCase))
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void AddSnap(GameObject prefab, string name, Vector3 local)
    {
        var go = new GameObject(name);
        go.transform.SetParent(prefab.transform, false);
        go.transform.localPosition = local;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
        private static void AfterGhost(Player __instance) => Align(__instance);

        // Skip the vanilla support graph. Chests do not support, and the
        // leftover chest AABB is a world-aligned box the next cabinet
        // tries to sit on. Drawers just stay up; hammer still removes them.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WearNTear), "UpdateSupport")]
        private static bool SkipSupport(WearNTear __instance)
        {
            if (__instance.GetComponent<DrawerFace>() == null)
                return true;
            __instance.m_support = 1000f;
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WearNTear), "HaveSupport")]
        private static void AlwaysSupported(WearNTear __instance, ref bool __result)
        {
            if (__instance.GetComponent<DrawerFace>() != null)
                __result = true;
        }
    }
}
