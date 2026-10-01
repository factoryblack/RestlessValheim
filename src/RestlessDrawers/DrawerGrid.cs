using HarmonyLib;
using UnityEngine;

namespace RestlessDrawers;

// Shared cell for every tier. Wood snaps to black metal the same as wood to wood.
internal static class DrawerGrid
{
    public const float Cell = 0.94f;
    public const float Overlap = 0.01f;

    // Every tier fills the same cube. A long mesh used to keep its short axes,
    // so wood and black metal did not share a pitch.
    public static void Fit(Mesh mesh, out Vector3 scale, out Vector3 offset)
    {
        var bounds = mesh.bounds;
        var size = bounds.size;
        var body = Cell + Overlap;
        scale = new Vector3(
            body / Mathf.Max(0.05f, size.x),
            body / Mathf.Max(0.05f, size.y),
            body / Mathf.Max(0.05f, size.z));
        var center = Vector3.Scale(bounds.center, scale);
        offset = new Vector3(-center.x, body * 0.5f - center.y, -center.z);
    }

    // Vanilla joins tagged snappoints within half a metre and leaves yaw alone.
    // Corners of the 94cm cell, not the 95cm mesh, so neighbours overlap by 1cm
    // and a bottom corner can catch a floor corner.
    public static void Dress(GameObject prefab)
    {
        StripOldSnaps(prefab);
        var half = Cell * 0.5f;
        foreach (var y in new[] { 0f, Cell })
        foreach (var x in new[] { -half, half })
        foreach (var z in new[] { -half, half })
        {
            var name = "snap_" + (y > 0f ? "t" : "b") + (x < 0f ? "l" : "r") + (z < 0f ? "b" : "f");
            AddSnap(prefab, name, new Vector3(x, y, z));
        }
    }

    private static void StripOldSnaps(GameObject prefab)
    {
        for (var i = prefab.transform.childCount - 1; i >= 0; i--)
        {
            var child = prefab.transform.GetChild(i);
            var named = child.name.StartsWith("snap", System.StringComparison.OrdinalIgnoreCase);
            var tagged = child.tag == "snappoint";
            if (named || tagged)
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void AddSnap(GameObject prefab, string name, Vector3 local)
    {
        var go = new GameObject(name);
        go.tag = "snappoint";
        go.transform.SetParent(prefab.transform, false);
        go.transform.localPosition = local;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
    }

    [HarmonyPatch]
    private static class Patches
    {
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
