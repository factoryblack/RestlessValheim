using Jotunn.Entities;
using UnityEngine;

namespace RestlessCook;

internal static class CookVisual
{
    public static void Apply(CustomItem item, CookRow row)
    {
        var prefab = item.ItemPrefab;
        if (prefab == null)
            return;

        var mesh = CookMesh.Mesh(row.Id);
        var albedo = CookMesh.Albedo(row.Id);
        if (mesh == null)
            return;

        var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
        var host = Pick(filters);
        if (host != null)
        {
            host.sharedMesh = mesh;
            Paint(host.GetComponent<Renderer>(), albedo);
            foreach (var filter in filters)
            {
                if (filter != host && filter.GetComponent<Renderer>() is { } extra)
                    extra.enabled = false;
            }
        }

        foreach (var skin in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (host != null)
            {
                skin.enabled = false;
                continue;
            }

            var filter = skin.gameObject.GetComponent<MeshFilter>() ?? skin.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var rend = skin.gameObject.GetComponent<MeshRenderer>() ?? skin.gameObject.AddComponent<MeshRenderer>();
            rend.sharedMaterials = skin.sharedMaterials;
            Paint(rend, albedo);
            skin.enabled = false;
            host = filter;
        }

        if (host == null)
        {
            var visual = new GameObject("RestlessCookMesh");
            visual.transform.SetParent(prefab.transform, false);
            var filter = visual.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            Paint(visual.AddComponent<MeshRenderer>(), albedo);
        }

        Plugin.Log.LogInfo("cook mesh " + row.Id);
    }

    private static MeshFilter? Pick(MeshFilter[] filters)
    {
        MeshFilter? best = null;
        var bestVol = -1f;
        foreach (var filter in filters)
        {
            if (filter == null)
                continue;
            var size = filter.sharedMesh != null ? filter.sharedMesh.bounds.size : Vector3.one * 0.01f;
            var vol = size.x * size.y * size.z;
            if (vol > bestVol)
            {
                best = filter;
                bestVol = vol;
            }
        }

        return best;
    }

    private static void Paint(Renderer? renderer, Texture2D? albedo)
    {
        if (renderer == null)
            return;
        var source = renderer.sharedMaterial;
        if (source == null)
            return;
        var mat = new Material(source);
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
