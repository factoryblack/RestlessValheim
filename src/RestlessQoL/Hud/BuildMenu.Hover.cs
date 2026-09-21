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
        const float maxHeight = 280f;
        const float gap = 16f;
        var reserve = Mathf.Max(130f, host.rect.height * 0.12f);
        // Reserve the maximum once: hovering a different recipe must not resize
        // or move the grid. The actual card is measured separately below.
        var gridBottom = FitBuildGrid(hud, host, reserve + maxHeight + gap, out var gridWidth);
        // Match the piece grid — wide enough for resource names, never wider than BuildUi.
        var width = Mathf.Min(gridWidth > 1f ? gridWidth : 900f, host.rect.width - 64f);
        var split = width * 0.48f;
        var title = hud.m_buildSelection.text;
        var description = hud.m_pieceDescription != null ? hud.m_pieceDescription.text : "";
        var key = title + "\n" + description;
        var changed = key != _detailKey;
        _detailKey = key;
        _detailTitle!.text = title;
        _detailBody!.text = description;
        _detailIcon!.sprite = hud.m_buildIcon != null ? hud.m_buildIcon.sprite : null;
        _detailIcon.enabled = _detailIcon.sprite != null;
        Place(_detailIcon.rectTransform, 18f, 16f, 44f, 44f);
        Place(_detailTitle.rectTransform, 76f, 12f, split - 96f, 52f);
        RestlessUi.BoundedLabel(_detailTitle, 24, 20);
        Place(_bodyScroll!.GetComponent<RectTransform>(), 18f, 76f, split - 36f, 40f);
        _detailBody.fontSize = 18;
        _detailBody.resizeTextForBestFit = false;
        _detailBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        _detailBody.verticalOverflow = VerticalWrapMode.Overflow;
        var bodyWidth = split - 44f;
        _bodyContent!.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bodyWidth);
        var bodyHeight = string.IsNullOrWhiteSpace(description) ? 0f : _detailBody.preferredHeight + 4f;
        Place(_costScroll!.GetComponent<RectTransform>(), split + 14f, 14f,
            width - split - 32f, 100f);
        var costsHeight = PaintCosts(hud, width - split - 40f);
        var height = Mathf.Clamp(Mathf.Max(76f + bodyHeight + 16f, costsHeight + 28f), 128f, maxHeight);
        // Dock to the grid, not the hotbar. Short copy no longer leaves a large
        // empty paper band or an unrelated gap between grid and detail card.
        var bottom = Mathf.Max(reserve, gridBottom - gap - height);
        RestlessUi.Pin(_hoverPaper, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, bottom), new Vector2(width, height));
        Place(_bodyScroll.GetComponent<RectTransform>(), 18f, 76f, split - 36f, height - 92f);
        _bodyContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bodyWidth);
        _bodyContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(height - 92f, bodyHeight));
        Place(_costScroll.GetComponent<RectTransform>(), split + 14f, 14f,
            width - split - 32f, height - 28f);
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
        RestlessUi.Stretch(divider, new Vector2(0.48f, 0f), new Vector2(0.48f, 1f),
            new Vector2(0f, 16f), new Vector2(1f, -16f));
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

    private static float PaintCosts(global::Hud hud, float width)
    {
        var slots = new List<Transform>();
        foreach (var text in hud.m_buildHud.GetComponentsInChildren<TMP_Text>(true))
        {
            if (!text.gameObject.activeInHierarchy || text.name.ToLowerInvariant() != "res_name"
                || Owned(text.transform) || string.IsNullOrWhiteSpace(text.text)) continue;
            if (hud.m_buildUi != null && text.transform.IsChildOf(hud.m_buildUi.transform)) continue;
            if (text.transform.parent != null && !slots.Contains(text.transform.parent)) slots.Add(text.transform.parent);
        }
        var cols = slots.Count >= 2 ? 2 : 1;
        var gutter = cols > 1 ? 12f : 0f;
        var colW = (width - gutter * (cols - 1)) / cols;
        var itemH = new float[slots.Count];
        for (var i = 0; i < slots.Count; i++)
        {
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
            itemH[i] = BindCost(CostRows[i], slots[i], colW);
        }
        for (var i = slots.Count; i < CostRows.Count; i++) CostRows[i].SetActive(false);
        var rows = (slots.Count + cols - 1) / cols;
        var rowH = new float[rows];
        for (var i = 0; i < slots.Count; i++)
            rowH[i / cols] = Mathf.Max(rowH[i / cols], itemH[i]);
        var y = 0f;
        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                var i = r * cols + c;
                if (i >= slots.Count) break;
                PlaceCost(CostRows[i], c * (colW + gutter), y, colW, rowH[r]);
            }
            y += rowH[r] + 4f;
        }
        var height = Mathf.Max(0f, y - 4f);
        _costContent!.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(1f, height));
        return height;
    }

    private static float BindCost(GameObject go, Transform slot, float colW)
    {
        go.SetActive(true);
        var name = RestlessUi.Deep<TMP_Text>(slot, "res_name");
        var amount = RestlessUi.Deep<TMP_Text>(slot, "res_amount");
        Image? icon = null;
        foreach (var image in slot.GetComponentsInChildren<Image>(true))
            if (image.name.ToLowerInvariant().Contains("icon") && image.sprite != null) { icon = image; break; }
        var labelFace = go.transform.Find("name").GetComponent<Text>();
        var countFace = go.transform.Find("count").GetComponent<Text>();
        var iconFace = go.transform.Find("icon").GetComponent<Image>();
        var hasCount = amount != null && amount.gameObject.activeInHierarchy && !string.IsNullOrWhiteSpace(amount.text);
        labelFace.text = name != null ? name.text : "";
        labelFace.color = SourceColour(name, RestlessUi.Text);
        labelFace.fontSize = 16;
        labelFace.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelFace.verticalOverflow = VerticalWrapMode.Overflow;
        var countWidth = hasCount ? Mathf.Min(72f, colW * 0.28f) : 0f;
        Place(labelFace.rectTransform, 42f, 0f, colW - 50f - countWidth, 36f);
        iconFace.sprite = icon != null ? icon.sprite : null;
        iconFace.enabled = iconFace.sprite != null;
        iconFace.preserveAspect = true;
        countFace.text = hasCount ? amount!.text : "";
        countFace.color = SourceColour(amount, RestlessUi.Accent);
        return Mathf.Max(38f, labelFace.preferredHeight + 8f);
    }

    private static void PlaceCost(GameObject go, float x, float y, float colW, float rowH)
    {
        var labelFace = go.transform.Find("name").GetComponent<Text>();
        var countFace = go.transform.Find("count").GetComponent<Text>();
        var iconFace = go.transform.Find("icon").GetComponent<Image>();
        var hasCount = !string.IsNullOrWhiteSpace(countFace.text);
        var countWidth = hasCount ? Mathf.Min(72f, colW * 0.28f) : 0f;
        Place(go.GetComponent<RectTransform>(), x, y, colW, rowH);
        Place(labelFace.rectTransform, 42f, 0f, colW - 50f - countWidth, rowH);
        Place(iconFace.rectTransform, 0f, (rowH - 30f) * 0.5f, 30f, 30f);
        Place(countFace.rectTransform, colW - countWidth, 0f, Mathf.Max(1f, countWidth), rowH);
        RestlessUi.BoundedLabel(countFace, 16, 14);
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
