using Jotunn.Entities;
using UnityEngine;

namespace RestlessCook;

internal static class CookVisual
{
    private const string ChildName = "RestlessCookMesh";

    // Meshy exports come in noticeably smaller than the vanilla food/feast
    // meshes they replace, so custom plates need an upscale to read at the
    // same size on the board/table.
    private static readonly Vector3 ModelScale = Vector3.one * 2.5f;

    public static void Apply(CustomItem item, CookRow row)
    {
        if (item?.ItemPrefab != null)
            Apply(item.ItemPrefab, row);
    }

    public static void Apply(GameObject prefab, CookRow row)
    {
        if (prefab == null)
            return;

        var mesh = CookMesh.Mesh(row.Id);
        var albedo = CookMesh.Albedo(row.Id);
        if (mesh == null)
        {
            // No custom model available (e.g. a dev build missing the mesh/
            // folder that ships alongside the packaged DLL). Fall back to the
            // vanilla clone_from appearance instead of leaving the prefab in a
            // half-configured state, and skip the plate/collider setup below
            // that assumes a custom mesh exists.
            Plugin.Log.LogWarning($"RestlessCook: no custom model for '{row.Id}', using vanilla appearance.");
            return;
        }

        var visual = FindOrCreate(prefab);
        var filter = visual.GetComponent<MeshFilter>() ?? visual.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var rend = visual.GetComponent<MeshRenderer>() ?? visual.AddComponent<MeshRenderer>();
        rend.enabled = true;
        Paint(rend, albedo);
        KeepPlate(prefab);
        EnsureHit(prefab);
    }

    public static void KeepPlate(GameObject prefab)
    {
        if (prefab == null)
            return;
        var keep = prefab.transform.Find(ChildName)?.GetComponent<Renderer>();
        if (keep == null)
            return;
        keep.enabled = true;
        keep.gameObject.SetActive(true);
        foreach (var rend in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (rend == null || rend == keep)
                continue;
            if (rend is not MeshRenderer && rend is not SkinnedMeshRenderer)
                continue;
            rend.enabled = false;
        }

        EnsureHit(prefab);
    }

    // Placed feast boards only. Ground meals carry ItemDrop physics; a convex
    // plate collider on those prefabs skews the rigidbody and they pop upward.
    public static void EnsureHit(GameObject prefab)
    {
        if (prefab == null)
            return;
        var keep = prefab.transform.Find(ChildName);
        if (keep == null)
            return;
        if (prefab.GetComponent<Feast>() == null)
        {
            var stray = keep.GetComponent<MeshCollider>();
            if (stray != null)
                Object.Destroy(stray);
            return;
        }

        var filter = keep.GetComponent<MeshFilter>();
        if (filter?.sharedMesh == null)
            return;
        var col = keep.GetComponent<MeshCollider>() ?? keep.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = filter.sharedMesh;
        col.convex = true;
        col.enabled = true;
    }

    private static GameObject FindOrCreate(GameObject prefab)
    {
        var t = prefab.transform.Find(ChildName);
        if (t != null)
        {
            t.SetParent(prefab.transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = ModelScale;
            t.gameObject.layer = prefab.layer;
            t.gameObject.SetActive(true);
            return t.gameObject;
        }

        var visual = new GameObject(ChildName);
        visual.layer = prefab.layer;
        visual.transform.SetParent(prefab.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = ModelScale;
        return visual;
    }

    private static void Paint(Renderer renderer, Texture2D? albedo)
    {
        var mat = Fallback();
        if (mat == null)
            return;
        mat.color = Color.white;
        SetColor(mat, "_Color", Color.white);
        SetColor(mat, "_BaseColor", Color.white);
        SetColor(mat, "_Tint", Color.white);
        SetFloat(mat, "_Metallic", 0f);
        SetFloat(mat, "_Glossiness", 0.12f);
        SetFloat(mat, "_Smoothness", 0.12f);
        SetFloat(mat, "_GlossMapScale", 0.12f);
        if (albedo != null)
        {
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
        }

        renderer.sharedMaterial = mat;
    }

    private static Material? Fallback()
    {
        var shader = Shader.Find("Standard")
                     ?? Shader.Find("Diffuse")
                     ?? Shader.Find("Legacy Shaders/Diffuse");
        return shader != null ? new Material(shader) : null;
    }

    private static void SetColor(Material mat, string prop, Color color)
    {
        if (mat.HasProperty(prop))
            mat.SetColor(prop, color);
    }

    private static void SetFloat(Material mat, string prop, float value)
    {
        if (mat.HasProperty(prop))
            mat.SetFloat(prop, value);
    }
}
