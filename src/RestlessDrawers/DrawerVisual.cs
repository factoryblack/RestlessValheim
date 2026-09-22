using Jotunn.Managers;
using UnityEngine;

namespace RestlessDrawers;

internal static class DrawerVisual
{
    public const string ChildName = "RestlessDrawerMesh";

    public static void Apply(GameObject prefab, DrawerRow row)
    {
        var mesh = DrawerMesh.Mesh(row.Id);
        var albedo = DrawerMesh.Albedo(row.Id);
        if (mesh == null)
        {
            Plugin.Log.LogWarning("RestlessDrawers: no custom model for " + row.Id);
            return;
        }

        // Steal the chest shader before we strip LODs. Standard in Valheim
        // explodes the ghost the same way the early food plates did.
        var mat = CopyLit(prefab, row.Source);

        foreach (var lod in prefab.GetComponentsInChildren<LODGroup>(true))
            Object.DestroyImmediate(lod);

        var visual = FindOrCreate(prefab, mesh);
        var filter = visual.GetComponent<MeshFilter>() ?? visual.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        var rend = visual.GetComponent<MeshRenderer>() ?? visual.AddComponent<MeshRenderer>();
        rend.enabled = true;
        Paint(rend, albedo, mat);

        foreach (var other in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (other == null || other == rend)
                continue;
            if (other is MeshRenderer or SkinnedMeshRenderer)
                other.enabled = false;
        }

        // WearNTear.SetupColliders(includeInactive: true) still records
        // disabled vanilla chest boxes. Those AABBs are the grid-aligned
        // phantom the next cabinet tries to sit on. Remove them.
        foreach (var col in prefab.GetComponentsInChildren<Collider>(true))
        {
            if (col == null || col.transform == visual.transform)
                continue;
            Object.DestroyImmediate(col);
        }

        var hit = visual.GetComponent<BoxCollider>() ?? visual.AddComponent<BoxCollider>();
        var bounds = mesh.bounds;
        hit.center = bounds.center;
        hit.size = Vector3.Max(bounds.size, new Vector3(0.2f, 0.2f, 0.2f));
        hit.enabled = true;

        if (prefab.GetComponent<DrawerFace>() == null)
            prefab.AddComponent<DrawerFace>();
        DrawerGrid.Dress(prefab);
    }

    private static GameObject FindOrCreate(GameObject prefab, Mesh mesh)
    {
        var t = prefab.transform.Find(ChildName);
        var scale = DrawerGrid.Fit(mesh);
        if (t != null)
        {
            t.SetParent(prefab.transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * scale;
            t.gameObject.layer = prefab.layer;
            t.gameObject.SetActive(true);
            return t.gameObject;
        }

        var visual = new GameObject(ChildName);
        visual.layer = prefab.layer;
        visual.transform.SetParent(prefab.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * scale;
        return visual;
    }

    private static void Paint(Renderer renderer, Texture2D? albedo, Material? mat)
    {
        if (mat == null)
            return;
        mat.color = Color.white;
        SetColor(mat, "_Color", Color.white);
        SetColor(mat, "_BaseColor", Color.white);
        SetColor(mat, "_Tint", Color.white);
        SetFloat(mat, "_Metallic", 0f);
        SetFloat(mat, "_Glossiness", 0.12f);
        SetFloat(mat, "_Smoothness", 0.12f);
        MuteGlow(mat);
        if (albedo != null)
        {
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", albedo);
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", albedo);
            if (mat.HasProperty("_Diffuse"))
                mat.SetTexture("_Diffuse", albedo);
        }

        renderer.sharedMaterial = mat;
    }

    private static Material? CopyLit(GameObject prefab, string source)
    {
        var src = QuietMat(prefab)
                  ?? QuietNamed(source)
                  ?? QuietNamed("piece_chest_wood")
                  ?? QuietNamed("piece_chest");
        if (src != null)
            return new Material(src);

        foreach (var name in new[] { "Legacy Shaders/Diffuse", "Diffuse" })
        {
            var shader = Shader.Find(name);
            if (shader != null)
                return new Material(shader);
        }

        Plugin.Log.LogWarning("drawer shader missing — leave vanilla look");
        return null;
    }

    private static Material? QuietNamed(string name)
    {
        var go = PrefabManager.Instance?.GetPrefab(name);
        return go != null ? QuietMat(go) : null;
    }

    // First renderer on a 1.0 chest is the snow overlay (Valheim/Snow Mesh),
    // same class of steal as wood_stack — wind/displace shaders explode the ghost.
    private static Material? QuietMat(GameObject? prefab)
    {
        if (prefab == null)
            return null;
        Renderer? best = null;
        var bestVol = -1f;
        foreach (var rend in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (rend == null || rend.transform.name == ChildName)
                continue;
            if (rend is not MeshRenderer && rend is not SkinnedMeshRenderer)
                continue;
            if (SkipNode(rend.transform))
                continue;
            var mat = rend.sharedMaterial;
            if (mat?.shader == null || !Usable(mat))
                continue;
            var vol = rend.bounds.size.x * rend.bounds.size.y * rend.bounds.size.z;
            if (vol <= bestVol)
                continue;
            bestVol = vol;
            best = rend;
        }

        if (best?.sharedMaterial == null)
            return null;
        Plugin.Log.LogInfo("drawer lit " + best.name + " / " + best.sharedMaterial.name + " / " +
                           best.sharedMaterial.shader.name);
        return best.sharedMaterial;
    }

    private static bool SkipNode(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            var n = t.name;
            if (Has(n, "fx") || Has(n, "spark") || Has(n, "glow") || Has(n, "ghost")
                || Has(n, "particle") || Has(n, "wisp") || Has(n, "snow") || Has(n, "ice")
                || Has(n, "winter") || Has(n, "stack") || Has(n, "pile") || Has(n, "cover")
                || Has(n, "overlay"))
                return true;
        }

        return false;
    }

    private static bool Usable(Material mat)
    {
        var label = mat.name + " " + mat.shader.name;
        if (Has(label, "Hidden/") || Has(label, "Particle") || Has(label, "glow")
            || Has(label, "ghost") || Has(label, "snow") || Has(label, "stack")
            || Has(label, "pile") || Has(label, "wind") || Has(label, "vegetation")
            || Has(label, "tree"))
            return false;
        if (mat.shader.name == "Standard")
            return false;
        if (mat.IsKeywordEnabled("_EMISSION") || mat.IsKeywordEnabled("EMISSION"))
            return false;
        if (mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent > 0.02f)
            return false;
        return true;
    }

    private static void MuteGlow(Material mat)
    {
        SetColor(mat, "_EmissionColor", Color.black);
        SetColor(mat, "_EmissiveColor", Color.black);
        SetFloat(mat, "_EnableEmission", 0f);
        mat.DisableKeyword("_EMISSION");
        mat.DisableKeyword("EMISSION");
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
