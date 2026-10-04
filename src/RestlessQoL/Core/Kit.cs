using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RestlessQoL.Core;

internal static class Kit
{
    private const string ResourcePrefix = "RestlessQoL.Assets.";
    private static readonly Dictionary<(string Name, Vector4 Border), Sprite> Cache = new();
    private static readonly Dictionary<string, Texture2D> Textures = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Failed = new(StringComparer.Ordinal);
    private static readonly HashSet<string> Warnings = new(StringComparer.Ordinal);
    private static MethodInfo? _loadImage;
    private static bool _searched;

    public static void Warm()
    {
        var ok = 0;
        var total = 0;
        // The embedded resources are the catalogue. New assets cannot silently
        // miss validation because someone forgot a second handwritten list.
        foreach (var resource in typeof(Kit).Assembly.GetManifestResourceNames())
        {
            if (!resource.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !resource.EndsWith(".png", StringComparison.Ordinal)) continue;
            var name = resource.Substring(ResourcePrefix.Length, resource.Length - ResourcePrefix.Length - 4);
            total++;
            if (Texture(name) != null) ok++;
        }
        Plugin.Log.LogInfo("kit: " + ok + "/" + total + " textures ready");
    }

    public static Sprite? Sprite(string name, Vector4 border = default)
    {
        if (name == "wear-slit")
            return WearSlit();
        if (name == "row-fill")
            return FillIdle();

        var key = (name, border);
        if (Cache.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        var tex = Texture(name);
        if (tex == null)
            return null;

        sprite = UnityEngine.Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            border);
        sprite.name = name;
        Cache[key] = sprite;
        return sprite;
    }

    // Thin vertical ribbon from row-idle's torn left and right so a slit along
    // the slot's right edge is ragged on every side. Top/bottom still 9-slice.
    private static Sprite? WearSlit()
    {
        var key = ("wear-slit-w", Vector4.zero);
        if (Cache.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        var src = Sprite("row-idle")?.texture;
        if (src == null)
            return null;

        var edge = Mathf.Max(28, src.width / 40);
        var tall = src.height;
        var tex = new Texture2D(edge * 2, tall, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = key.Item1
        };
        tex.SetPixels(0, 0, edge, tall, src.GetPixels(0, 0, edge, tall));
        tex.SetPixels(edge, 0, edge, tall, src.GetPixels(src.width - edge, 0, edge, tall));
        var pix = tex.GetPixels();
        for (var i = 0; i < pix.Length; i++)
        {
            var a = pix[i].a;
            if (a < 0.02f)
                continue;
            pix[i] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pix);
        tex.Apply();
        sprite = UnityEngine.Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(0f, 28f, 0f, 28f));
        sprite.name = key.Item1;
        Cache[key] = sprite;
        return sprite;
    }

    // row-idle bleached to white so a pool tint is the colour you see.
    private static Sprite? FillIdle()
    {
        var key = ("row-fill", Vector4.zero);
        if (Cache.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        var src = Sprite("row-idle")?.texture;
        if (src == null)
            return null;

        var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
        {
            wrapMode = src.wrapMode,
            filterMode = src.filterMode,
            name = key.Item1
        };
        var pix = src.GetPixels();
        for (var i = 0; i < pix.Length; i++)
        {
            var a = pix[i].a;
            if (a < 0.02f)
                continue;
            pix[i] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pix);
        tex.Apply();
        sprite = UnityEngine.Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(28f, 14f, 28f, 14f));
        sprite.name = key.Item1;
        Cache[key] = sprite;
        return sprite;
    }

    public static Texture2D? Texture(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (Textures.TryGetValue(name, out var cached) && cached != null) return cached;
        if (Failed.Contains(name)) return null;
        var stream = typeof(Kit).Assembly.GetManifestResourceStream(ResourcePrefix + name + ".png");
        if (stream == null)
        {
            Failed.Add(name);
            Warn("missing " + name);
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

            var tex = LoadBytes(bytes);
            if (tex == null)
            {
                Failed.Add(name);
                Warn("decode failed " + name);
                return null;
            }

            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Textures[name] = tex;
            return tex;
        }
    }

    private static Texture2D? LoadBytes(byte[] bytes)
    {
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var load = LoadImage();
        if (load != null)
        {
            try
            {
                if ((bool)load.Invoke(null, new object[] { tex, bytes }))
                    return tex;
            }
            catch
            {
                // fall through to the local decoder
            }
        }

        UnityEngine.Object.Destroy(tex);
        return Png.Load(bytes);
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

    private static void Warn(string message)
    {
        if (!Warnings.Add(message)) return;
        Plugin.Log.LogWarning("kit: " + message);
    }
}
