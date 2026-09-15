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
        image.sprite = sprite;
        image.color = Color.white; // Material already contains its charcoal colour.
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = 1f;
        image.raycastTarget = false;
        Rim(target, on: false);

        var rim = target.transform.Find("paperAccent")?.gameObject;
        if (rim == null) rim = Graphic(target.transform, "paperAccent", Color.white, false);
        Stretch(rim, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var overlay = rim.GetComponent<Image>();
        overlay.sprite = Kit.Sprite(name + "-rim", border);
        overlay.type = Image.Type.Tiled;
        overlay.pixelsPerUnitMultiplier = 1f;
        // Cover the baked gold edge on idle surfaces; reserve amber for emphasis.
        overlay.color = accent ?? new Color(0.18f, 0.16f, 0.13f, 0.88f);
        overlay.raycastTarget = false;
    }

    public static void PaperCorner(GameObject panel)
    {
        var corner = Graphic(panel.transform, "paperCorner", Color.white, false);
        var image = corner.GetComponent<Image>();
        image.sprite = Kit.Sprite("paper-corner");
        if (image.sprite == null) { corner.SetActive(false); return; }
        Pin(corner, Vector2.one, Vector2.one, new Vector2(4f, 5f), new Vector2(126f, 150f));
        // Decoration behind all text; title layout reserves space on the right.
        corner.transform.SetAsFirstSibling();
        var tree = Graphic(corner.transform, "emblem", new Color(0.58f, 0.53f, 0.44f, 0.55f), false);
        tree.GetComponent<Image>().sprite = Kit.Sprite("paper-tree");
        Pin(tree, Vector2.one, Vector2.one, new Vector2(-18f, -24f), new Vector2(24f, 48f));
    }

    // Geometric diamonds do not depend on a font glyph or optional sprite asset.
    public static void PaperQuality(Transform parent, int quality, string caption, float width)
    {
        var face = Label(parent, caption + " " + quality, HudMeta, Accent, TextAnchor.MiddleLeft);
        var labelWidth = Mathf.Min(width * 0.6f, face.preferredWidth + 12f);
        Pin(face.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            Vector2.zero, new Vector2(labelWidth, 24f));
        var count = Mathf.Min(Mathf.Max(quality, 0), Mathf.Max(0, Mathf.FloorToInt((width - labelWidth) / 13f)));
        for (var i = 0; i < count; i++)
        {
            var diamond = Graphic(parent, "qualityDiamond-" + i, Accent, false);
            Pin(diamond, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(labelWidth + 5f + i * 13f, 0f), new Vector2(6f, 6f));
            diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }
    }

    public static void PaperDivider(GameObject row)
    {
        var tint = new Color(0.58f, 0.46f, 0.30f, 0.55f);
        var left = Graphic(row.transform, "left", tint, false);
        Stretch(left, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -0.5f), new Vector2(-14f, 0.5f));
        var right = Graphic(row.transform, "right", tint, false);
        Stretch(right, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(14f, -0.5f), new Vector2(0f, 0.5f));
        var knot = Graphic(row.transform, "knot", tint, false);
        knot.GetComponent<Image>().sprite = Kit.Sprite("paper-knot");
        Pin(knot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 14f));
    }
}
