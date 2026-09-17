using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RestlessCook;

internal static class CookIcons
{
    private static readonly Dictionary<string, Sprite> Cache = new();
    private static MethodInfo? _loadImage;
    private static bool _searched;

    public static Sprite? Sprite(string id)
    {
        var key = id.Replace('_', '-');
        if (Cache.TryGetValue(key, out var sprite) && sprite != null)
            return sprite;

        var tex = Texture(key);
        if (tex == null)
            return null;

        sprite = UnityEngine.Sprite.Create(
            tex,
            new Rect(0f, 0f, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = key;
        Cache[key] = sprite;
        return sprite;
    }

    private static Texture2D? Texture(string key)
    {
        var stream = typeof(CookIcons).Assembly.GetManifestResourceStream("RestlessCook.Assets." + key + ".png");
        if (stream == null)
        {
            Plugin.Log.LogWarning("cook icon missing " + key);
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

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var load = LoadImage();
            if (load == null || !(bool)load.Invoke(null, new object[] { tex, bytes }))
            {
                UnityEngine.Object.Destroy(tex);
                Plugin.Log.LogWarning("cook icon decode failed " + key);
                return null;
            }

            tex.name = key;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }
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
