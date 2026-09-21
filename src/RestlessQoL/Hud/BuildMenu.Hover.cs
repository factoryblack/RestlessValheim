using System.Collections.Generic;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed partial class BuildMenu
{
    private static GameObject? _hoverPaper;
    private static Text? _detailTitle, _detailBody;
    private static Image? _detailIcon;
    private static RectTransform? _bodyContent, _costContent;
    private static ScrollRect? _bodyScroll, _costScroll;
    private static readonly List<GameObject> CostRows = new();
    private static readonly Dictionary<Graphic, float> DetailAlpha = new();
    private static readonly Dictionary<RectTransform, (Vector3 scale, Vector3 position)> MenuGeometry = new();
    private static readonly Dictionary<RectTransform, (Vector2 min, Vector2 max, Vector2 pivot, Vector2 size, Vector3 position)> BarGeometry = new();
    private static string _detailKey = "";

    private static void DressHover(global::Hud hud)
    {
        if (!global::Hud.IsPieceSelectionVisible() || hud.m_buildHud == null
            || hud.m_buildSelection == null || !hud.m_buildSelection.gameObject.activeInHierarchy
            || string.IsNullOrWhiteSpace(hud.m_buildSelection.text))
        {
            ClearHover();
            return;
        }
        if (hud.m_rootObject == null) return;
        var host = hud.m_rootObject.transform as RectTransform;
        if (host == null || host.rect.height < 200f) return;
        EnsureDetails(host);
        _hoverPaper!.SetActive(true);
        var width = Mathf.Min(1400f, host.rect.width - 64f);
        var height = Mathf.Clamp(host.rect.height * 0.22f, 190f, 240f);
        var bottom = Mathf.Max(130f, host.rect.height * 0.12f);
        RestlessUi.Pin(_hoverPaper, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, bottom), new Vector2(width, height));
        FitBuildGrid(hud, host, bottom + height + 22f);

        var split = width * 0.58f;
        var title = hud.m_buildSelection.text;
        var description = hud.m_pieceDescription != null ? hud.m_pieceDescription.text : "";
        // Reset scrolling only when the selected piece/copy changes, not on count refresh.
        var key = title + "\n" + description;
        var changed = key != _detailKey;
        _detailKey = key;
        _detailTitle!.text = title;
        _detailBody!.text = description;
        _detailIcon!.sprite = hud.m_buildIcon != null ? hud.m_buildIcon.sprite : null;
        _detailIcon.enabled = _detailIcon.sprite != null;
        Place(_detailIcon.rectTransform, 22f, 20f, 62f, 62f);
        Place(_detailTitle.rectTransform, 102f, 18f, split - 126f, 62f);
        RestlessUi.BoundedLabel(_detailTitle, 28, 20);
        Place(_bodyScroll!.GetComponent<RectTransform>(), 24f, 94f, split - 48f, height - 116f);
        _detailBody.fontSize = 20;
        _detailBody.resizeTextForBestFit = false;
        _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailBody.verticalOverflow = VerticalWrapMode.Overflow;
        _bodyContent!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, split - 60f);
        _bodyContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(height - 116f, _detailBody.preferredHeight + 8f));
        Place(_costScroll!.GetComponent<RectTransform>(), split + 16f, 22f,
            width - split - 40f, height - 44f);
        PaintCosts(hud, width - split - 52f);
        if (changed)
        {
            _bodyScroll.verticalNormalizedPosition = 1f;
            _costScroll.verticalNormalizedPosition = 1f;
        }
        HideNativeDetails(hud);
    }

    private static void EnsureDetails(RectTransform host)
    {
        if (_hoverPaper != null) return;
        _hoverPaper = RestlessUi.Graphic(host, "RestlessBuildDetails", Color.white, false);
        RestlessUi.PaperSurface(_hoverPaper);
        _hoverPaper.AddComponent<LayoutElement>().ignoreLayout = true;
        _detailIcon = RestlessUi.Graphic(_hoverPaper.transform, "portrait", Color.white, false).GetComponent<Image>();
        _detailIcon.preserveAspect = true;
        _detailTitle = RestlessUi.Label(_hoverPaper.transform, "", 28, RestlessUi.Text, TextAnchor.MiddleLeft);
        _bodyScroll = DetailScroll(_hoverPaper.transform, "description", out _bodyContent);
        _detailBody = RestlessUi.Label(_bodyContent, "", 20, RestlessUi.Text, TextAnchor.UpperLeft);
        RestlessUi.Stretch(_detailBody.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _costScroll = DetailScroll(_hoverPaper.transform, "requirements", out _costContent);
        var divider = RestlessUi.Graphic(_hoverPaper.transform, "rule", new Color(0.55f, 0.46f, 0.31f, 0.45f), false);
        RestlessUi.Stretch(divider, new Vector2(0.58f, 0f), new Vector2(0.58f, 1f),
            new Vector2(0f, 22f), new Vector2(1f, -22f));
    }

    private static ScrollRect DetailScroll(Transform host, string name, out RectTransform content)
    {
        var viewport = RestlessUi.Graphic(host, name, Color.clear, true);
        viewport.AddComponent<RectMask2D>();
        content = RestlessUi.Node(viewport.transform, "content").GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var scroll = viewport.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 32f;
        return scroll;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
    }

    private static void PaintCosts(global::Hud hud, float width)
    {
        var slots = new List<Transform>();
        foreach (var text in hud.m_buildHud.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!text.gameObject.activeInHierarchy || text.name.ToLowerInvariant() != "res_name"
                || Owned(text.transform) || string.IsNullOrWhiteSpace(text.text)) continue;
            if (hud.m_buildUi != null && text.transform.IsChildOf(hud.m_buildUi.transform)) continue;
            if (text.transform.parent != null && !slots.Contains(text.transform.parent)) slots.Add(text.transform.parent);
        }
        var y = 0f;
        for (var i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            var name = RestlessUi.Deep<TMP_Text>(slot, "res_name");
            var amount = RestlessUi.Deep<TMP_Text>(slot, "res_amount");
            Image? icon = null;
            foreach (var image in slot.GetComponentsInChildren<Image>(true))
                if (image.name.ToLowerInvariant().Contains("icon") && image.sprite != null) { icon = image; break; }
            if (i == CostRows.Count)
            {
                var row = RestlessUi.Node(_costContent!, "requirement" + i);
                RestlessUi.Graphic(row.transform, "icon", Color.white, false);
                var label = RestlessUi.Label(row.transform, "", 18, RestlessUi.Text, TextAnchor.MiddleLeft);
                label.name = "name";
                var count = RestlessUi.Label(row.transform, "", 18, RestlessUi.Accent, TextAnchor.MiddleRight);
                count.name = "count";
                CostRows.Add(row);
            }
            var go = CostRows[i];
            go.SetActive(true);
            var labelFace = go.transform.Find("name").GetComponent<Text>();
            var countFace = go.transform.Find("count").GetComponent<Text>();
            var iconFace = go.transform.Find("icon").GetComponent<Image>();
            var hasCount = amount != null && amount.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(amount.text);
            labelFace.text = name != null ? name.text : "";
            labelFace.color = SourceColour(name, RestlessUi.Text);
            labelFace.fontSize = 18;
            labelFace.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelFace.verticalOverflow = VerticalWrapMode.Overflow;
            var countWidth = hasCount ? Mathf.Min(110f, width * 0.3f) : 0f;
            Place(labelFace.rectTransform, 50f, 0f, width - 58f - countWidth, 42f);
            var rowHeight = Mathf.Max(46f, labelFace.preferredHeight + 10f);
            Place(go.GetComponent<RectTransform>(), 0f, y, width, rowHeight);
            Place(labelFace.rectTransform, 50f, 0f, width - 58f - countWidth, rowHeight);
            Place(iconFace.rectTransform, 0f, (rowHeight - 36f) * 0.5f, 36f, 36f);
            iconFace.sprite = icon != null ? icon.sprite : null;
            iconFace.enabled = iconFace.sprite != null;
            iconFace.preserveAspect = true;
            countFace.text = hasCount ? amount!.text : "";
            countFace.color = SourceColour(amount, RestlessUi.Accent);
            Place(countFace.rectTransform, width - countWidth, 0f, Mathf.Max(1f, countWidth), rowHeight);
            RestlessUi.BoundedLabel(countFace, 18, 14);
            y += rowHeight + 4f;
        }
        for (var i = slots.Count; i < CostRows.Count; i++) CostRows[i].SetActive(false);
        _costContent!.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, y));
    }

    private static Color SourceColour(Graphic? source, Color neutral)
    {
        if (source == null) return neutral;
        var c = source.color;
        var chroma = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
        return chroma < 0.15f ? neutral : new Color(c.r, c.g, c.b, 1f);
    }

    private static void HideNativeDetails(global::Hud hud)
    {
        // BuildHud also owns BuildUi. Hide graphics individually, never that root
        // CanvasGroup; otherwise the grid and search input disappear with it.
        var detailRoot = hud.m_buildSelection != null ? hud.m_buildSelection.transform.parent : null;
        while (detailRoot != null && hud.m_pieceDescription != null
            && !hud.m_pieceDescription.transform.IsChildOf(detailRoot)) detailRoot = detailRoot.parent;
        if (detailRoot == hud.m_buildHud.transform
            || detailRoot != null && hud.m_buildUi != null && hud.m_buildUi.transform.IsChildOf(detailRoot)) detailRoot = null;
        foreach (var graphic in hud.m_buildHud.GetComponentsInChildren<Graphic>(true))
        {
            if (Owned(graphic.transform)) continue;
            if (hud.m_buildUi != null && graphic.transform.IsChildOf(hud.m_buildUi.transform)) continue;
            if (hud.m_pieceSelectionWindow != null && graphic.transform.IsChildOf(hud.m_pieceSelectionWindow.transform)
                && (detailRoot == null || !graphic.transform.IsChildOf(detailRoot))) continue;
            if (!DetailAlpha.ContainsKey(graphic)) DetailAlpha.Add(graphic, graphic.color.a);
            var c = graphic.color;
            c.a = 0f;
            graphic.color = c;
        }
    }

    private static void ClearHover()
    {
        foreach (var pair in DetailAlpha)
        {
            if (pair.Key == null) continue;
            var c = pair.Key.color;
            c.a = pair.Value;
            pair.Key.color = c;
        }
        DetailAlpha.Clear();
        foreach (var pair in MenuGeometry)
            if (pair.Key != null) { pair.Key.localScale = pair.Value.scale; pair.Key.localPosition = pair.Value.position; }
        MenuGeometry.Clear();
        foreach (var pair in BarGeometry)
        {
            if (pair.Key == null) continue;
            var r = pair.Key; var s = pair.Value;
            r.anchorMin = s.min; r.anchorMax = s.max; r.pivot = s.pivot;
            r.sizeDelta = s.size; r.anchoredPosition3D = s.position;
        }
        BarGeometry.Clear();
        if (_hoverPaper != null) Object.Destroy(_hoverPaper);
        _hoverPaper = null;
        CostRows.Clear();
        _detailKey = "";
    }
}
