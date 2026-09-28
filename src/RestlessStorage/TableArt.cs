using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RestlessStorage;

internal static class TableArt
{
    public const string Id = "table";

    private static Mesh? _mesh;
    private static Texture2D? _albedo;
    private static Texture2D? _surface;
    private static Sprite? _icon;
    private static string? _folder;
    private static MethodInfo? _loadImage;
    private static bool _searched;

    public static Mesh? Mesh()
    {
        if (_mesh != null)
            return _mesh;
        var path = Path.Combine(Folder(), Id + ".rcm");
        if (!File.Exists(path))
        {
            Plugin.Log.LogWarning("table mesh missing " + path);
            return null;
        }

        _mesh = ReadRcm(File.ReadAllBytes(path));
        return _mesh;
    }

    public static Texture2D? Albedo() => Picture(ref _albedo, Id + ".png", linear: false);

    public static Texture2D? Surface() => Picture(ref _surface, Id + ".metal.png", linear: true);

    public static Sprite? Icon()
    {
        if (_icon != null)
            return _icon;
        var stream = typeof(TableArt).Assembly.GetManifestResourceStream("RestlessStorage.Assets.table.png");
        if (stream == null)
        {
            Plugin.Log.LogWarning("table icon missing");
            return null;
        }

        using (stream)
        {
            var bytes = new byte[stream.Length];
            var read = 0;
            while (read < bytes.Length)
            {
                var n = stream.Read(bytes, read, bytes.Length - read);
                if (n <= 0)
                    break;
                read += n;
            }

            var tex = DecodePng(bytes, Id + ".icon");
            if (tex == null)
                return null;
            _icon = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _icon.name = Id;
            return _icon;
        }
    }

    private static Texture2D? Picture(ref Texture2D? slot, string file, bool linear)
    {
        if (slot != null)
            return slot;
        var path = Path.Combine(Folder(), file);
        if (!File.Exists(path))
        {
            Plugin.Log.LogWarning("table picture missing " + path);
            return null;
        }

        slot = DecodePng(File.ReadAllBytes(path), file, linear);
        return slot;
    }

    private static string Folder()
    {
        if (_folder != null)
            return _folder;

        var roots = new List<string>();
        AddRoot(roots, Plugin.Instance?.Info?.Location);
        AddRoot(roots, typeof(TableArt).Assembly.Location);
        foreach (var root in roots)
        {
            var nested = Path.Combine(root, "mesh");
            if (File.Exists(Path.Combine(nested, Id + ".rcm")))
                return Remember(nested);
            if (File.Exists(Path.Combine(root, Id + ".rcm")))
                return Remember(root);
        }

        _folder = Path.Combine(roots.Count > 0 ? roots[0] : ".", "mesh");
        Plugin.Log.LogWarning("table mesh folder missing, looking in " + _folder);
        return _folder;
    }

    private static void AddRoot(List<string> roots, string? file)
    {
        var path = string.IsNullOrEmpty(file) ? "" : Path.GetDirectoryName(file) ?? "";
        if (path.Length == 0)
            return;
        foreach (var existing in roots)
        {
            if (string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
                return;
        }

        roots.Add(path);
    }

    private static string Remember(string dir)
    {
        _folder = dir;
        Plugin.Log.LogInfo("table mesh folder " + dir);
        return dir;
    }

    private static Mesh? ReadRcm(byte[] data)
    {
        if (data.Length < 12 || data[0] != (byte)'R' || data[1] != (byte)'C' || data[2] != (byte)'M'
            || (data[3] != (byte)'1' && data[3] != (byte)'2'))
        {
            Plugin.Log.LogWarning("table mesh bad header");
            return null;
        }

        var unflipV = data[3] == (byte)'1';
        var pos = 4;
        var vertCount = BitConverter.ToInt32(data, pos);
        pos += 4;
        var indexCount = BitConverter.ToInt32(data, pos);
        pos += 4;
        if (vertCount <= 0 || indexCount <= 0 || pos + vertCount * 32 + indexCount * 4 > data.Length)
        {
            Plugin.Log.LogWarning("table mesh truncated");
            return null;
        }

        var verts = new Vector3[vertCount];
        var norms = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        for (var i = 0; i < vertCount; i++)
        {
            verts[i] = new Vector3(BitConverter.ToSingle(data, pos), BitConverter.ToSingle(data, pos + 4),
                BitConverter.ToSingle(data, pos + 8));
            norms[i] = new Vector3(BitConverter.ToSingle(data, pos + 12), BitConverter.ToSingle(data, pos + 16),
                BitConverter.ToSingle(data, pos + 20));
            var v = BitConverter.ToSingle(data, pos + 28);
            uvs[i] = new Vector2(BitConverter.ToSingle(data, pos + 24), unflipV ? 1f - v : v);
            pos += 32;
        }

        var tris = new int[indexCount];
        for (var i = 0; i < indexCount; i++)
        {
            tris[i] = BitConverter.ToInt32(data, pos);
            pos += 4;
        }

        var mesh = new Mesh
        {
            name = Id,
            indexFormat = vertCount > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16
        };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Texture2D? DecodePng(byte[] bytes, string name, bool linear = false)
    {
        var tex = linear
            ? new Texture2D(2, 2, TextureFormat.RGBA32, true, true)
            : new Texture2D(2, 2, TextureFormat.RGBA32, true);
        var load = LoadImage();
        if (load == null || !(bool)load.Invoke(null, new object[] { tex, bytes }))
        {
            UnityEngine.Object.Destroy(tex);
            Plugin.Log.LogWarning("table png decode failed " + name);
            return null;
        }

        tex.name = name;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.anisoLevel = 4;
        tex.Apply(true, false);
        return tex;
    }

    private static MethodInfo? LoadImage()
    {
        if (_searched)
            return _loadImage;
        _searched = true;
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("UnityEngine.ImageConversion");
            if (type == null)
                continue;
            _loadImage = type.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
            if (_loadImage != null)
                return _loadImage;
        }

        return null;
    }
}
