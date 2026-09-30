using Jotunn.Managers;
using UnityEngine;

namespace RestlessWorks;

internal static class BoardVisual
{
    public const string ChildName = "RestlessWorksMesh";

    public static void Apply(GameObject prefab)
    {
        var mesh = BoardArt.Mesh();
        if (mesh == null)
        {
            Plugin.Log.LogWarning("RestlessWorks: no custom model, leaving the chest look.");
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
        visual.AddComponent<MeshFilter>().sharedMesh = mesh;
        var rend = visual.AddComponent<MeshRenderer>();
        Paint(rend, BoardArt.Albedo(), mat);

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

    private static void Paint(Renderer renderer, Texture2D? albedo, Material? mat)
    {
        if (mat == null)
            return;
        mat.color = Color.white;
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.white);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.white);
        if (albedo != null)
        {
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
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
            return new Material(best.sharedMaterial);

        var chest = PrefabManager.Instance?.GetPrefab("piece_chest_wood");
        var fallback = chest != null ? chest.GetComponentInChildren<MeshRenderer>(true) : null;
        if (fallback?.sharedMaterial != null && fallback.sharedMaterial.shader.name != "Standard")
            return new Material(fallback.sharedMaterial);

        Plugin.Log.LogWarning("RestlessWorks shader missing");
        return null;
    }

    private static bool Has(string text, string token) =>
        text.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
}
