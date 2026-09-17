using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

// Textures, semantic glyphs and state layers stay separate for future consumers.
internal static partial class RestlessUi
{
    public static void ForgedSurface(GameObject target, bool action = false, bool interactive = false)
    {
        var image = target.GetComponent<Image>();
        if (image == null) return;
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
        if (image == null) return;
        image.sprite = Kit.Sprite("category-strip", new Vector4(64f, 12f, 64f, 12f));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3f;
        image.color = selected ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.45f);
        image.raycastTarget = true;
        Rim(target, on: false);
        var old = target.transform.Find("paperAccent");
        if (old != null) old.gameObject.SetActive(false);
        var marker = target.transform.Find("RestlessTabMarker")?.gameObject;
        if (marker == null) marker = Picture(target.transform, "RestlessTabMarker", "selection-marker");
        Pin(marker, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(42f, 20f));
        marker.GetComponent<Image>().raycastTarget = false;
        marker.SetActive(selected);
    }

    public static float QualityMarks(Transform parent, int quality, float width)
    {
        var space = quality * 18f > width ? Mathf.Max(0f, width - 32f) : width;
        var count = Mathf.Min(Mathf.Max(0, quality), Mathf.Max(0, Mathf.FloorToInt(space / 18f)));
        for (var i = 0; i < count; i++)
        {
            var name = "qualityGem-" + i;
            var gem = parent.Find(name)?.gameObject ?? Picture(parent, name, "quality-lozenge");
            gem.SetActive(true);
            gem.GetComponent<Image>().sprite = QualityLozenge();
            Pin(gem, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(i * 18f, 0f), new Vector2(14f, 20f));
            gem.GetComponent<Image>().raycastTarget = false;
        }
        for (var i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (!child.name.StartsWith("qualityGem-")) continue;
            if (!int.TryParse(child.name.Substring("qualityGem-".Length), out var index) || index >= count)
                UnityEngine.Object.Destroy(child.gameObject);
        }
        var extra = parent.Find("qualityOverflow");
        if (quality > count)
        {
            var overflow = extra != null ? extra.GetComponent<Text>() : null;
            if (overflow == null)
                overflow = Label(parent, "", HudMeta, Accent, TextAnchor.MiddleLeft);
            overflow.gameObject.name = "qualityOverflow";
            overflow.gameObject.SetActive(true);
            overflow.text = "+" + (quality - count);
            Pin(overflow.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(count * 18f, 0f), new Vector2(32f, 20f));
        }
        else if (extra != null)
            extra.gameObject.SetActive(false);
        return count * 18f + (quality > count ? 32f : 0f);
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
