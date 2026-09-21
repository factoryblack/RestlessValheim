using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace RestlessCook;

internal static class CookVisual
{
    private const string ChildName = "RestlessCookMesh";
    // Baked feast plates are dish-sized (~0.6m). Visual-only scale until a Blender re-bake.
    private const float FeastMeshScale = 3f;

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
        Paint(rend, albedo, prefab);
        KeepPlate(prefab);
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
            foreach (var col in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (col is MeshCollider mesh)
                    Object.DestroyImmediate(mesh);
            }

            if (prefab.GetComponentInChildren<Collider>(true) == null)
                SitOnGround(keep);
            return;
        }

        DressFeast(prefab, keep);
    }

    // Scale the plate, not the cloned feast root. Root scale also blows up
    // vanilla table colliders and the serving-tray ghost fails as invalid.
    private static void DressFeast(GameObject prefab, Transform keep)
    {
        keep.localScale = Vector3.one * FeastMeshScale;
        keep.localPosition = new Vector3(0f, 0.02f, 0f);

        foreach (var col in prefab.GetComponentsInChildren<Collider>(true))
        {
            if (col == null)
                continue;
            if (col.transform == keep || col.transform.IsChildOf(keep))
                continue;
            col.enabled = false;
        }

        var piece = prefab.GetComponent<Piece>();
        if (piece != null)
        {
            piece.m_clipGround = true;
            piece.m_noInWater = true;
            piece.m_cultivatedGroundOnly = false;
            piece.m_groundOnly = false;
        }

        var filter = keep.GetComponent<MeshFilter>();
        if (filter?.sharedMesh == null)
            return;
        var hit = keep.GetComponent<MeshCollider>() ?? keep.gameObject.AddComponent<MeshCollider>();
        hit.sharedMesh = filter.sharedMesh;
        hit.convex = true;
        hit.enabled = true;
    }

    private static void SitOnGround(Transform keep)
    {
        var filter = keep.GetComponent<MeshFilter>();
        if (filter?.sharedMesh == null)
            return;
        var box = keep.GetComponent<BoxCollider>() ?? keep.gameObject.AddComponent<BoxCollider>();
        var bounds = filter.sharedMesh.bounds;
        box.center = bounds.center;
        box.size = Vector3.Max(bounds.size, new Vector3(0.08f, 0.04f, 0.08f));
        box.enabled = true;
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

    private static void Paint(Renderer renderer, Texture2D? albedo, GameObject prefab)
    {
        var mat = CopyLit(prefab);
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

    // Same as the working food-shader steal: clone a vanilla MeshRenderer
    // on this prefab (or a feast board). Skip thistle / spice / eitr glow
    // garnish. Never wood_stack — that painted bark on our UVs.
    private static Material? CopyLit(GameObject prefab)
    {
        var src = QuietMat(prefab) ?? QuietFeast();
        if (src != null)
            return new Material(src);

        var shader = Shader.Find("Standard")
                     ?? Shader.Find("Diffuse")
                     ?? Shader.Find("Legacy Shaders/Diffuse");
        return shader != null ? new Material(shader) : null;
    }

    private static Material? QuietFeast()
    {
        foreach (var name in new[] { "FeastMeadows", "FeastBlackforest" })
        {
            var go = PrefabManager.Instance?.GetPrefab(name);
            var mat = QuietMat(go);
            if (mat != null)
                return mat;
        }

        return null;
    }

    private static Material? QuietMat(GameObject? prefab)
    {
        if (prefab == null)
            return null;
        foreach (var rend in prefab.GetComponentsInChildren<Renderer>(true))
        {
            if (rend == null || rend.transform.name == ChildName)
                continue;
            if (rend is not MeshRenderer)
                continue;
            if (Garnish(rend.transform))
                continue;
            var mat = rend.sharedMaterial;
            if (mat?.shader == null || Glows(mat))
                continue;
            return mat;
        }

        return null;
    }

    private static bool Garnish(Transform node)
    {
        for (var t = node; t != null; t = t.parent)
        {
            var n = t.name;
            if (Has(n, "thistle") || Has(n, "spice") || Has(n, "eitr")
                || Has(n, "glow") || Has(n, "garnish") || Has(n, "magecap")
                || Has(n, "jotun") || Has(n, "wisp") || Has(n, "mist"))
                return true;
        }

        return false;
    }

    private static bool Glows(Material mat)
    {
        var label = mat.name + " " + mat.shader.name;
        if (Has(label, "thistle") || Has(label, "spice") || Has(label, "eitr")
            || Has(label, "glow"))
            return true;
        if (mat.IsKeywordEnabled("_EMISSION") || mat.IsKeywordEnabled("EMISSION"))
            return true;
        if (mat.HasProperty("_EmissionMap") && mat.GetTexture("_EmissionMap") != null)
            return true;
        if (mat.HasProperty("_EmissionColorMap") && mat.GetTexture("_EmissionColorMap") != null)
            return true;
        if (mat.HasProperty("_EmissionColor") && mat.GetColor("_EmissionColor").maxColorComponent > 0.02f)
            return true;
        if (mat.HasProperty("_EmissiveColor") && mat.GetColor("_EmissiveColor").maxColorComponent > 0.02f)
            return true;
        if (mat.HasProperty("_EnableEmission") && mat.GetFloat("_EnableEmission") > 0.5f)
            return true;
        return false;
    }

    private static void MuteGlow(Material mat)
    {
        SetColor(mat, "_EmissionColor", Color.black);
        SetColor(mat, "_EmissiveColor", Color.black);
        SetColor(mat, "_GlowColor", Color.black);
        SetFloat(mat, "_EnableEmission", 0f);
        SetFloat(mat, "_EmissionIntensity", 0f);
        if (mat.HasProperty("_EmissionMap"))
            mat.SetTexture("_EmissionMap", null);
        if (mat.HasProperty("_EmissionColorMap"))
            mat.SetTexture("_EmissionColorMap", null);
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
