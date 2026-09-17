using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace RestlessCook;

internal static class CookMesh
{
    private static readonly Dictionary<string, Mesh> Meshes = new();
    private static readonly Dictionary<string, Texture2D> Albedos = new();
    private static string? _folder;
    private static MethodInfo? _loadImage;
    private static bool _searched;

    public static string Folder()
    {
        if (_folder != null)
            return _folder;
        var root = Path.GetDirectoryName(typeof(CookMesh).Assembly.Location) ?? ".";
        _folder = Path.Combine(root, "mesh");
        return _folder;
    }

    public static Mesh? Mesh(string id)
    {
        var key = id.Replace('_', '-');
        if (Meshes.TryGetValue(key, out var mesh) && mesh != null)
            return mesh;

        var path = Path.Combine(Folder(), key + ".rcm");
        if (!File.Exists(path))
        {
            Plugin.Log.LogWarning("cook mesh missing " + key);
            return null;
        }

        mesh = ReadRcm(File.ReadAllBytes(path), key);
        if (mesh != null)
            Meshes[key] = mesh;
        return mesh;
    }

    public static Texture2D? Albedo(string id)
    {
        var key = id.Replace('_', '-');
        if (Albedos.TryGetValue(key, out var tex) && tex != null)
            return tex;

        var path = Path.Combine(Folder(), key + ".png");
        if (!File.Exists(path))
        {
            Plugin.Log.LogWarning("cook albedo missing " + key);
            return null;
        }

        tex = DecodePng(File.ReadAllBytes(path), key);
        if (tex != null)
            Albedos[key] = tex;
        return tex;
    }

    private static Mesh? ReadRcm(byte[] data, string name)
    {
        if (data.Length < 12 || data[0] != (byte)'R' || data[1] != (byte)'C' || data[2] != (byte)'M' || (data[3] != (byte)'1' && data[3] != (byte)'2'))
        {
            Plugin.Log.LogWarning("cook mesh bad header " + name);
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
            Plugin.Log.LogWarning("cook mesh truncated " + name);
            return null;
        }

        var verts = new Vector3[vertCount];
        var norms = new Vector3[vertCount];
        var uvs = new Vector2[vertCount];
        for (var i = 0; i < vertCount; i++)
        {
            verts[i] = new Vector3(BitConverter.ToSingle(data, pos), BitConverter.ToSingle(data, pos + 4), BitConverter.ToSingle(data, pos + 8));
            norms[i] = new Vector3(BitConverter.ToSingle(data, pos + 12), BitConverter.ToSingle(data, pos + 16), BitConverter.ToSingle(data, pos + 20));
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

        var mesh = new Mesh { name = name, indexFormat = vertCount > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Texture2D? DecodePng(byte[] bytes, string name)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        var load = LoadImage();
        if (load == null || !(bool)load.Invoke(null, new object[] { tex, bytes }))
        {
            UnityEngine.Object.Destroy(tex);
            Plugin.Log.LogWarning("cook albedo decode failed " + name);
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
