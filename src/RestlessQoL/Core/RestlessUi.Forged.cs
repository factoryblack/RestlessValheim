using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// Textures, semantic glyphs and state layers stay separate for future consumers.
internal static partial class RestlessUi
{
    public static void ForgedSurface(GameObject target, bool action = false, bool interactive = false)
    {
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite(action ? "forged-action" : "forged-badge",
            action ? new Vector4(80f, 20f, 80f, 20f) : new Vector4(52f, 14f, 52f, 14f));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 2f;
        image.color = Color.white;
        image.raycastTarget = interactive;
        Rim(target, on: false);
        var old = target.transform.Find("paperAccent");
        if (old != null) old.gameObject.SetActive(false);
    }

    public static void ForgedTab(GameObject target, bool selected)
    {
        var image = target.GetComponent<Image>();
        image.sprite = Kit.Sprite("tab-inset", new Vector4(12f, 8f, 12f, 8f));
        image.type = Image.Type.Sliced;
        image.color = selected ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.45f);
        image.raycastTarget = true;
        Rim(target, on: false);
        var old = target.transform.Find("paperAccent");
        if (old != null) old.gameObject.SetActive(false);
        var marker = target.transform.Find("RestlessTabMarker")?.gameObject;
        if (marker == null) marker = Picture(target.transform, "RestlessTabMarker", "tab-marker");
        Pin(marker, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 8f));
        marker.GetComponent<Image>().raycastTarget = false;
        marker.SetActive(selected);
    }

    public static float QualityMarks(Transform parent, int quality, float width)
    {
        var space = quality * 15f > width ? width - 32f : width;
        var count = Mathf.Min(Mathf.Max(0, quality), Mathf.Max(1, Mathf.FloorToInt(space / 15f)));
        for (var i = 0; i < count; i++)
        {
            var gem = Picture(parent, "qualityGem-" + i, "quality-gem");
            Pin(gem, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(i * 15f, 0f), new Vector2(14f, 17f));
            gem.GetComponent<Image>().raycastTarget = false;
        }
        // Large mod levels must not silently look like a lower level.
        if (quality > count)
        {
            var overflow = Label(parent, "+" + (quality - count), HudMeta, Accent, TextAnchor.MiddleLeft);
            Pin(overflow.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(count * 15f, 0f), new Vector2(32f, 20f));
        }
        return count * 15f;
    }

    public static Scrollbar ForgedScrollbar(Transform parent)
    {
        var track = Graphic(parent, "RestlessScrollTrack", new Color(0.06f, 0.06f, 0.05f, 0.7f));
        var thumb = Picture(track.transform, "RestlessScrollThumb", "scroll-thumb");
        var scroll = track.AddComponent<Scrollbar>();
        scroll.direction = Scrollbar.Direction.BottomToTop;
        scroll.handleRect = thumb.GetComponent<RectTransform>();
        scroll.targetGraphic = thumb.GetComponent<Image>();
        PaperSelectable(scroll);
        return scroll;
    }
}
