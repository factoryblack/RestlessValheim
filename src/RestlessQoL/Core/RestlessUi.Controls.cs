using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Core;

internal static partial class RestlessUi
{
    private static Sprite? _switchTrack;

    private static Sprite SwitchTrack()
    {
        if (_switchTrack != null) return _switchTrack;
        var circle = Circle();
        var border = (circle.rect.width - 2f) * 0.5f;
        _switchTrack = Sprite.Create(circle.texture, circle.rect, new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        return _switchTrack;
    }
    // Explicitly interactive counterpart to the non-blocking inspect material.
    public static void PaperControl(GameObject target, Color? accent = null)
    {
        PaperSurface(target, small: true, accent: accent);
        target.GetComponent<Image>().raycastTarget = true;
    }

    public static void PaperSelectable(Selectable control)
    {
        control.transition = Selectable.Transition.ColorTint;
        var colours = control.colors;
        colours.normalColor = Color.white;
        colours.highlightedColor = new Color(1.2f, 1.12f, 0.95f);
        colours.selectedColor = colours.highlightedColor;
        colours.pressedColor = new Color(0.72f, 0.66f, 0.56f);
        colours.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
        colours.fadeDuration = FadeSeconds;
        control.colors = colours;
    }

    public static void PaperSwitch(Button button, bool on)
    {
        // A capsule track has a softer silhouette than another torn rectangle.
        var track = button.GetComponent<Image>();
        track.sprite = SwitchTrack();
        track.type = Image.Type.Sliced;
        track.color = on ? new Color(0.38f, 0.28f, 0.13f) : new Color(0.13f, 0.13f, 0.12f);
        track.raycastTarget = true;
        Rim(button.gameObject, on: false);
        var edge = button.transform.Find("paperAccent");
        if (edge != null) edge.gameObject.SetActive(false);
        var knob = button.transform.Find("knob");
        if (knob != null)
        {
            var image = knob.GetComponent<Image>();
            image.sprite = Circle();
            image.color = on ? Accent : PaperMuted;
            image.raycastTarget = false;
            Pin(knob.gameObject, new Vector2(on ? 1f : 0f, 0.5f), new Vector2(on ? 1f : 0f, 0.5f),
                new Vector2(on ? -6f : 6f, 0f), new Vector2(18f, 18f));
            knob.SetAsLastSibling();
        }
        PaperSelectable(button);
    }

    public static GameObject PaperSettingsHeader(GameObject tray)
    {
        var bar = TrayHead(tray, "bar");
        PaperSurface(bar);
        bar.GetComponent<Image>().raycastTarget = true;
        Pin(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, 24f), new Vector2(1120f, 86f));
        for (var i = 0; i < 2; i++)
        {
            var tree = Graphic(bar.transform, "tree-" + i,
                new Color(0.58f, 0.53f, 0.44f, 0.55f), false);
            tree.GetComponent<Image>().sprite = Kit.Sprite("paper-tree");
            Pin(tree, new Vector2(i, 0.5f), new Vector2(i, 0.5f),
                new Vector2(i == 0 ? 20f : -20f, 0f), new Vector2(24f, 50f));
        }
        return bar;
    }

    public static Slider PaperSlider(Transform parent)
    {
        var track = Chip(parent, "slider");
        PaperControl(track);
        Pin(track, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-112f, 0f), new Vector2(200f, 28f));
        var fillArea = Node(track.transform, "fillArea");
        Stretch(fillArea, Vector2.zero, Vector2.one, new Vector2(8f, 11f), new Vector2(-8f, -11f));
        var fill = Graphic(fillArea.transform, "fill", Accent, false);
        Stretch(fill, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var slide = Node(track.transform, "slide");
        Stretch(slide, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        var knob = Picture(slide.transform, "knob", "diamond");
        Pin(knob, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20f, 20f));
        var slider = track.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = knob.GetComponent<RectTransform>();
        slider.targetGraphic = knob.GetComponent<Image>();
        PaperSelectable(slider);
        return slider;
    }
}
