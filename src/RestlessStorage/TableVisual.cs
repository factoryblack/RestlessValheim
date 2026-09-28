using Jotunn.Managers;
using UnityEngine;

namespace RestlessStorage;

internal static class TableVisual
{
    public const string ChildName = "RestlessStorekeeperMesh";

    // This Meshy export is already about 1.5m across and 1.3m tall. Leave it.

    public static void Apply(GameObject prefab)
    {
        var mesh = TableArt.Mesh();
        if (mesh == null)
        {
            Plugin.Log.LogWarning("RestlessStorage: no custom model, leaving the chest look.");
            return;
        }

        var mat = CopyLit(prefab);
        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true))
            Object.DestroyImmediate(lod);

        var layer = prefab.layer;
        var donor = prefab.GetComponentInChildren<Collider>(true);
        if (donor != null)
            layer = donor.gameObject.layer;

        foreach (var col in prefab.GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
                Object.DestroyImmediate(col);
        }

        var visual = new GameObject(ChildName);
        visual.layer = layer;
        visual.transform.SetParent(prefab.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        visual.transform.localScale = Vector3.one;
        var filter = visual.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var rend = visual.AddComponent<MeshRenderer>();
        Paint(rend, TableArt.Albedo(), TableArt.Surface(), mat);

        foreach (var other in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (other == null || other == rend)
                continue;
            if (other is MeshRenderer or SkinnedMeshRenderer)
                other.enabled = false;
        }

        var hit = visual.AddComponent<BoxCollider>();
        hit.center = mesh.bounds.center;
        hit.size = Vector3.Max(mesh.bounds.size, new Vector3(0.2f, 0.2f, 0.2f));
    }

    private static void Paint(Renderer renderer, Texture2D? albedo, Texture2D? surface, Material? mat)
    {
        if (mat == null)
            return;
        mat.color = Color.white;
        SetColor(mat, "_Color", Color.white);
        SetColor(mat, "_BaseColor", Color.white);
        if (albedo != null)
        {
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
        }

        if (surface != null)
        {
            if (mat.HasProperty("_MetallicTex"))
                mat.SetTexture("_MetallicTex", surface);
            if (mat.HasProperty("_MetallicGlossMap"))
                mat.SetTexture("_MetallicGlossMap", surface);
            SetFloat(mat, "_Metallic", 1f);
            SetFloat(mat, "_MetallicAlphaGloss", 1f);
            SetFloat(mat, "_Glossiness", 1f);
            SetFloat(mat, "_Smoothness", 1f);
            mat.EnableKeyword("_METALLICGLOSSMAP");
        }

        renderer.sharedMaterial = mat;
    }

    private static Material? CopyLit(GameObject prefab)
    {
        Renderer? best = null;
        var bestVol = -1f;
        foreach (var rend in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (rend == null || rend is not MeshRenderer and not SkinnedMeshRenderer)
                continue;
            var shared = rend.sharedMaterial;
            if (shared?.shader == null || shared.shader.name == "Standard")
                continue;
            var label = shared.name + " " + shared.shader.name;
            if (Has(label, "snow") || Has(label, "particle") || Has(label, "glow"))
                continue;
            var vol = rend.bounds.size.x * rend.bounds.size.y * rend.bounds.size.z;
            if (vol <= bestVol)
                continue;
            bestVol = vol;
            best = rend;
        }

        if (best?.sharedMaterial != null)
        {
            Plugin.Log.LogInfo("table lit " + best.name + " / " + best.sharedMaterial.shader.name);
            return new Material(best.sharedMaterial);
        }

        var chest = PrefabManager.Instance?.GetPrefab("piece_chest_wood");
        var fallback = chest != null ? chest.GetComponentInChildren<MeshRenderer>(true) : null;
        if (fallback?.sharedMaterial != null && fallback.sharedMaterial.shader.name != "Standard")
            return new Material(fallback.sharedMaterial);

        Plugin.Log.LogWarning("table shader missing");
        return null;
    }

    private static bool Has(string text, string token) =>
        text.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;

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
