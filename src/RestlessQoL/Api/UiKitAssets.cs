using System;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Api;

/// <summary>Supported material roles. Raw asset filenames are implementation details.</summary>
public enum UiKitAsset { Panel, PanelRim, Chip, ChipRim, Action, Badge, Category, Portrait, Corner, Quality }

/// <summary>Immutable drawing and content rules for a supported kit material.</summary>
public sealed class UiKitAssetInfo
{
    internal string Name { get; }
    public Vector2 NativeSize { get; }
    public Vector4 Border { get; }
    public Image.Type Drawing { get; }
    public float PixelsPerUnitMultiplier { get; }
    /// <summary>Left, bottom, right, top padding in canvas units.</summary>
    public Vector4 ContentInsets { get; }
    public Vector2 MinimumSize { get; }
    public bool PreserveAspect { get; }

    internal UiKitAssetInfo(string name, int width, int height, Image.Type drawing,
        Vector4 border, float multiplier, Vector4 insets, Vector2 minimum, bool aspect = false)
    {
        Name = name; NativeSize = new Vector2(width, height); Drawing = drawing;
        Border = border; PixelsPerUnitMultiplier = multiplier; ContentInsets = insets;
        MinimumSize = minimum; PreserveAspect = aspect;
    }
}

internal static class UiKitAssets
{
    // Native pixel dimensions are checked against PNG headers in CI. Slice caps
    // are artwork pixels; minimum size and insets are canvas units, not pixels.
    private static readonly UiKitAssetInfo[] Entries = {
        new("paper-panel",516,514,Image.Type.Tiled,new Vector4(32,32,32,32),1,new Vector4(24,24,24,24),new Vector2(64,64)),
        new("paper-panel-rim",516,514,Image.Type.Tiled,new Vector4(32,32,32,32),1,Vector4.zero,new Vector2(64,64)),
        new("paper-chip",516,103,Image.Type.Tiled,new Vector4(32,16,32,16),1,new Vector4(10,3,10,3),new Vector2(64,32)),
        new("paper-chip-rim",516,103,Image.Type.Tiled,new Vector4(32,16,32,16),1,Vector4.zero,new Vector2(64,32)),
        new("forged-action",772,133,Image.Type.Sliced,new Vector4(80,20,80,20),2,new Vector4(40,10,40,10),new Vector2(80,20)),
        new("forged-badge",516,103,Image.Type.Sliced,new Vector4(52,14,52,14),2,new Vector4(26,7,26,7),new Vector2(52,14)),
        new("category-strip",516,81,Image.Type.Sliced,new Vector4(64,12,64,12),3,new Vector4(22,4,22,4),new Vector2(44,8)),
        new("portrait-frame",516,503,Image.Type.Simple,Vector4.zero,1,Vector4.zero,new Vector2(32,32),true),
        new("corner-overlay",516,509,Image.Type.Simple,Vector4.zero,1,Vector4.zero,new Vector2(32,32),true),
        new("quality-lozenge",64,64,Image.Type.Simple,Vector4.zero,1,Vector4.zero,new Vector2(14,20),true),
    };

    internal static UiKitAssetInfo Get(UiKitAsset role)
    {
        var index = (int)role;
        if (index < 0 || index >= Entries.Length) throw new ArgumentOutOfRangeException(nameof(role));
        return Entries[index];
    }

    internal static void Apply(Image image, UiKitAsset role)
    {
        var entry = Get(role);
        var sprite = Core.Kit.Sprite(entry.Name, entry.Border);
        if (sprite == null) return; // Keep the existing graphic as the fallback.
        image.sprite = sprite; image.type = entry.Drawing;
        image.pixelsPerUnitMultiplier = entry.PixelsPerUnitMultiplier;
        image.preserveAspect = entry.PreserveAspect;
    }
}
