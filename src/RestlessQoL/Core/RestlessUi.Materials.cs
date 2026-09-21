using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// Opt-in material vocabulary. Tooltip is the first consumer, not a separate kit.
internal static partial class RestlessUi
{
    private static readonly Vector4 PaperPanelBorder = new(32f, 32f, 32f, 32f);
    private static readonly Vector4 PaperChipBorder = new(32f, 16f, 32f, 16f);
    public static readonly Color PaperMuted = Hex(0xB4A99A);

    public static void PaperSurface(GameObject target, bool small = false, Color? accent = null)
    {
        var name = small ? "paper-chip" : "paper-panel";
        var border = small ? PaperChipBorder : PaperPanelBorder;
        var sprite = Kit.Sprite(name, border);
        if (sprite == null) return; // Existing kit remains the missing-asset fallback.
        var image = target.GetComponent<Image>();
        if (image == null) return;
        var rim = target.transform.Find("paperAccent")?.gameObject;
        var overlayColor = accent ?? new Color(0.18f, 0.16f, 0.13f, 0.88f);
        if (image.sprite == sprite && image.type == Image.Type.Tiled && rim != null)
        {
            image.color = Color.white;
            var live = rim.GetComponent<Image>();
            if (live != null) live.color = overlayColor;
            return;
        }

        image.sprite = sprite;
        image.color = Color.white; // Material already contains its charcoal colour.
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = 1f;
        image.raycastTarget = false;
        Rim(target, on: false);

        if (rim == null) rim = Graphic(target.transform, "paperAccent", Color.white, false);
        Stretch(rim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var overlay = rim.GetComponent<Image>();
        overlay.sprite = Kit.Sprite(name + "-rim", border);
        overlay.type = Image.Type.Tiled;
        overlay.pixelsPerUnitMultiplier = 1f;
        // Cover the baked gold edge on idle surfaces; reserve amber for emphasis.
        overlay.color = overlayColor;
        overlay.raycastTarget = false;
    }

    public static void PaperCorner(GameObject panel)
    {
        var corner = panel.transform.Find("paperCorner")?.gameObject
            ?? Graphic(panel.transform, "paperCorner", Color.white, false);
        var image = corner.GetComponent<Image>();
        image.sprite = Kit.Sprite("corner-overlay");
        if (image.sprite == null) { corner.SetActive(false); return; }
        image.preserveAspect = true;
        Pin(corner, Vector2.one, Vector2.one, new Vector2(4f, 5f), new Vector2(112f, 112f));
        // Decoration behind all text; title layout reserves space on the right.
        corner.transform.SetAsFirstSibling();
        var tree = corner.transform.Find("emblem")?.gameObject
            ?? Graphic(corner.transform, "emblem", new Color(1f, 1f, 1f, 0.8f), false);
        tree.GetComponent<Image>().sprite = Kit.Sprite("pine-emblem");
        tree.GetComponent<Image>().preserveAspect = true;
        Pin(tree, Vector2.one, Vector2.one, new Vector2(-13f, -14f), new Vector2(16f, 48f));
    }

    // Compatibility entry point; the forged mark asset is shared by both families.
    public static void PaperQuality(Transform parent, int quality, string caption, float width)
        => QualityMarks(parent, quality, width);

    public static void PaperDivider(GameObject row)
    {
        var tint = new Color(0.58f, 0.46f, 0.30f, 0.55f);
        var left = row.transform.Find("left")?.gameObject
            ?? Graphic(row.transform, "left", tint, false);
        Stretch(left, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -0.5f), new Vector2(-14f, 0.5f));
        var right = row.transform.Find("right")?.gameObject
            ?? Graphic(row.transform, "right", tint, false);
        Stretch(right, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(14f, -0.5f), new Vector2(0f, 0.5f));
        var knot = row.transform.Find("knot")?.gameObject
            ?? Graphic(row.transform, "knot", tint, false);
        knot.GetComponent<Image>().sprite = CastKnot();
        knot.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.75f);
        knot.GetComponent<Image>().preserveAspect = true;
        Pin(knot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 20f));
    }

    private static Sprite? _castKnot;
    private static Sprite? CastKnot()
    {
        if (_castKnot != null) return _castKnot;
        var source = Kit.Sprite("knot-divider");
        if (source == null) return Kit.Sprite("paper-knot");
        // Keep the knot proportional while the separate rules follow the row width.
        var width = source.rect.width * 0.20f;
        _castKnot = Sprite.Create(source.texture,
            new Rect((source.rect.width - width) * 0.5f, 0f, width, source.rect.height),
            new Vector2(0.5f, 0.5f), 100f);
        return _castKnot;
    }
}
