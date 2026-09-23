using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RestlessQoL.Core;

internal static class Kit
{
    private static readonly string[] Library =
    {
        "craft-station-corner", "craft-material-socket", "craft-tab-ribbon", "craft-selection-clasp",
        "row-idle", "btn-small", "diamond", "map-player", "eat-fork",
        "inventory-slot", "empty-head", "empty-chest", "empty-legs", "empty-cape",
        "empty-utility", "empty-trinket", "carry-weight",
        "mouse-left", "mouse-right", "mouse-middle",
        "tab-glow", "panel-back",
        "paper-panel", "paper-panel-rim", "paper-chip", "paper-chip-rim",
        "paper-corner", "paper-tree", "paper-knot",
        "forged-badge", "forged-action", "quality-gem", "category-ribbon",
        "station-medallion", "tab-inset", "tab-marker", "scroll-thumb",
        "glyph-blunt", "glyph-slash", "glyph-pierce", "glyph-fire", "glyph-frost",
        "glyph-poison", "glyph-lightning", "glyph-spirit", "glyph-chop", "glyph-pickaxe", "glyph-station",
        "corner-overlay", "craft-hammer", "station-socket", "portrait-frame", "knot-divider", "category-strip", "pine-emblem",
        "nav-raven", "nav-knot", "nav-shield", "nav-trophy", "nav-swords",
        "utility-lock", "utility-equipped", "utility-repair", "utility-missing",
        "utility-expand", "utility-collapse", "utility-close", "quality-lozenge", "selection-marker", "focus-corners",
        "loadout-corner", "loadout-crest", "loadout-stat-plaque", "equipment-set-seal",
    };

    private static readonly Dictionary<string, Sprite> Cache = new();
    private static MethodInfo? _loadImage;
    private static bool _searched;
    private static bool _logged;

    public static void Warm()
    {
        var ok = 0;
        foreach (var name in Library)
        {
            if (Sprite(name) != null)
                ok++;
            else
                Plugin.Log.LogWarning("kit: missing " + name);
        }

        Plugin.Log.LogInfo("kit: " + ok + "/" + Library.Length + " sprites ready");
    }

    public static Sprite? Sprite(string name, Vector4 border = default)
    {
        if (name == "wear-slit")
            return WearSlit();
        if (name == "row-fill")
            return FillIdle();

        var key = name + border;
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
        const string key = "wear-slit-w";
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
            name = key
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
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    // row-idle bleached to white so a pool tint is the colour you see.
    private static Sprite? FillIdle()
    {
        const string key = "row-fill";
        if (Cache.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        var src = Sprite("row-idle")?.texture;
        if (src == null)
            return null;

        var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
        {
            wrapMode = src.wrapMode,
            filterMode = src.filterMode,
            name = key
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
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    public static Texture2D? Texture(string name)
    {
        var stream = typeof(Kit).Assembly.GetManifestResourceStream("RestlessQoL.Assets." + name + ".png");
        if (stream == null)
        {
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
                Warn("decode failed " + name);
                return null;
            }

            tex.name = name;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
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
        if (_logged)
            return;
        _logged = true;
        Plugin.Log.LogWarning("kit: " + message);
    }
}
