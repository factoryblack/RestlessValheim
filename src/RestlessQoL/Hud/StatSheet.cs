using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

// Tab loadout totals. Sample while visible; reuse rows and show sources inside the panel.
public sealed partial class StatSheet : FeatureModule
{
    public override string Id => "ui.sheet";
    public override bool Enabled => true;
    public override bool TickInMenus => true;

    private const float SheetWidth = 780f;
    private const float SheetHeight = 570f;
    private const float RowH = 36f;

    private static GameObject? _root;
    private static GameObject? _body;
    private static Text? _originFace;
    private static Text? _originTitle;
    private static Text? _originValue;
    private static ScrollRect? _scroll;
    private static ScrollRect? _detailScroll;
    private static float _nextSample;
    private static Vector2 _parentSize;
    private static readonly List<RowView> Views = new();
    private static readonly List<StatRow> Rows = new();
    private static bool _open;
    private static string _detailCopy = "";
    private static string _hover = "";

    protected override void OnLoaded() => GUIManager.OnCustomGUIAvailable += TearDown;

    public override void Tick()
    {
        if (!ModConfig.SheetEnabled.Value || !InventoryGui.IsVisible())
        {
            Hide();
            return;
        }

        var gui = InventoryGui.instance;
        if (gui == null)
        {
            Hide();
            return;
        }

        if (OtherModal(gui))
            Hide();
        if (_open)
        {
            Fit();
            if (Time.unscaledTime >= _nextSample) Paint();
        }
    }

    internal static void Hook(Transform host)
    {
        if (host == null || !ModConfig.SheetEnabled.Value)
            return;
        var plate = host.Find("RestlessStat") ?? host;
        if (plate.GetComponent<SheetOpener>() != null)
            return;
        var image = plate.GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;
        var button = plate.gameObject.GetComponent<Button>() ?? plate.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(Toggle);
        var hint = plate.gameObject.GetComponent<RestlessHint>() ?? plate.gameObject.AddComponent<RestlessHint>();
        hint.Copy = "Open loadout totals";
        plate.gameObject.AddComponent<SheetOpener>();
    }

    private static void Toggle()
    {
        if (_open)
            Hide();
        else
            Show();
    }

    private static void Show()
    {
        if (!ModConfig.SheetEnabled.Value)
            return;
        var gui = InventoryGui.instance;
        var player = Player.m_localPlayer;
        if (gui == null || player == null)
            return;
        CloseOthers(gui);
        Ensure(gui);
        _open = true;
        _detailCopy = "";
        _hover = "";
        if (_root != null)
        {
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
        }
        Paint();
        if (ZInput.IsGamepadActive() && Views.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(Views[0].Back.gameObject);
    }

    internal static void Hide()
    {
        _open = false;
        _hover = "";
        if (_root != null)
            _root.SetActive(false);
    }

    private static void TearDown()
    {
        Hide();
        if (_root != null)
            Object.Destroy(_root);
        _root = _body = null;
        _originFace = _originTitle = _originValue = null;
        _scroll = _detailScroll = null;
        Views.Clear();
        Rows.Clear();
        _detailCopy = "";
    }

    private static bool OtherModal(InventoryGui gui) =>
        gui.m_textsDialog != null && gui.m_textsDialog.gameObject.activeInHierarchy
        || gui.m_skillsDialog != null && gui.m_skillsDialog.gameObject.activeInHierarchy
        || gui.m_trophiesPanel != null && gui.m_trophiesPanel.activeInHierarchy
        || gui.m_achievementsPanel != null && gui.m_achievementsPanel.gameObject.activeInHierarchy;

    private static void CloseOthers(InventoryGui gui)
    {
        if (gui.m_textsDialog != null)
            gui.m_textsDialog.gameObject.SetActive(false);
        if (gui.m_skillsDialog != null)
            gui.m_skillsDialog.gameObject.SetActive(false);
        if (gui.m_trophiesPanel != null)
            gui.m_trophiesPanel.SetActive(false);
        if (gui.m_achievementsPanel != null)
            gui.m_achievementsPanel.gameObject.SetActive(false);
    }

    private static void Ensure(InventoryGui gui)
    {
        if (_root != null) return;
        var parent = gui.m_inventoryRoot != null ? gui.m_inventoryRoot.parent : gui.transform;
        // PaperSurface skins an existing Image; Node alone silently had no backing.
        _root = RestlessUi.Graphic(parent, "RestlessSheet", RestlessUi.Ink);
        RestlessUi.Pin(_root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(SheetWidth, SheetHeight));
        RestlessUi.PaperSurface(_root);
        _root.GetComponent<Image>().raycastTarget = true;
        var title = RestlessUi.Label(_root.transform, "Loadout totals", 30, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(title.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(28f, -58f), new Vector2(-112f, -14f));
        var caption = RestlessUi.Label(_root.transform, "Equipped gear · food · active effects", 18,
            RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(caption.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(28f, -86f), new Vector2(-28f, -58f));
        var close = RestlessUi.Chip(_root.transform, "close");
        RestlessUi.PaperControl(close);
        RestlessUi.Pin(close, Vector2.one, Vector2.one, new Vector2(-28f, -26f), new Vector2(76f, 32f));
        var closeLabel = RestlessUi.Label(close.transform, "Close", 18, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(closeLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var button = close.AddComponent<Button>();
        button.targetGraphic = close.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(Hide);

        var rule = RestlessUi.Graphic(_root.transform, "rule", new Color(0.58f, 0.46f, 0.30f, 0.45f), false);
        RestlessUi.Stretch(rule, new Vector2(0f, 1f), Vector2.one, new Vector2(28f, -98f), new Vector2(-28f, -97f));
        var list = RestlessUi.Node(_root.transform, "totals");
        RestlessUi.Stretch(list, Vector2.zero, Vector2.one, new Vector2(24f, 32f), new Vector2(-326f, -110f));
        _scroll = Scroller(gui, list.transform, out var body);
        _body = body;
        var layout = body.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2f;
        body.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var divider = RestlessUi.Graphic(_root.transform, "columnRule", new Color(0.58f, 0.46f, 0.30f, 0.28f), false);
        RestlessUi.Stretch(divider, new Vector2(1f, 0f), Vector2.one,
            new Vector2(-309f, 32f), new Vector2(-308f, -112f));
        var detail = RestlessUi.Node(_root.transform, "sources");
        RestlessUi.Stretch(detail, new Vector2(1f, 0f), Vector2.one,
            new Vector2(-288f, 32f), new Vector2(-24f, -110f));
        _originTitle = RestlessUi.Label(detail.transform, "Sources", 22, RestlessUi.Text, TextAnchor.UpperLeft);
        RestlessUi.Stretch(_originTitle.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -56f), Vector2.zero);
        _originTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
        _originValue = RestlessUi.Label(detail.transform, "", 26, RestlessUi.Accent, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(_originValue.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -92f), new Vector2(0f, -56f));
        var detailView = RestlessUi.Node(detail.transform, "reader");
        RestlessUi.Stretch(detailView, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -108f));
        _detailScroll = Scroller(gui, detailView.transform, out var copy);
        _originFace = RestlessUi.Label(copy.transform, "", 18, RestlessUi.PaperMuted, TextAnchor.UpperLeft);
        RestlessUi.Stretch(_originFace.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        _originFace.horizontalOverflow = HorizontalWrapMode.Wrap;
        _originFace.verticalOverflow = VerticalWrapMode.Overflow;
        // The viewport receives wheel/drag events even between rows and paragraphs.
        _parentSize = Vector2.zero;
        Fit();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_root.GetComponent<RectTransform>());
    }

    private static ScrollRect Scroller(InventoryGui gui, Transform parent, out GameObject body)
    {
        var view = RestlessUi.Graphic(parent, "viewport", Color.clear);
        RestlessUi.Stretch(view, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-24f, 0f));
        view.AddComponent<RectMask2D>();
        var scroll = view.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = 36f;
        scroll.viewport = view.GetComponent<RectTransform>();
        body = RestlessUi.Node(view.transform, "content");
        var rt = body.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
        scroll.content = rt;
        if (gui.m_recipeListScroll != null)
        {
            var bar = Object.Instantiate(gui.m_recipeListScroll, parent, false);
            bar.gameObject.name = "scrollbar";
            bar.onValueChanged = new Scrollbar.ScrollEvent();
            RestlessUi.Stretch(bar.gameObject, new Vector2(1f, 0f), Vector2.one,
                new Vector2(-10f, 0f), Vector2.zero);
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            bar.gameObject.SetActive(true);
        }
        return scroll;
    }

    private static void Fit()
    {
        if (_root == null || _root.transform.parent is not RectTransform parent) return;
        var size = parent.rect.size;
        if (size == _parentSize) return;
        _parentSize = size;
        var scale = Mathf.Min(1f, (size.x - 40f) / SheetWidth, (size.y - 40f) / SheetHeight);
        _root.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
    }

    private static void Paint()
    {
        _nextSample = Time.unscaledTime + 0.25f;
        var player = Player.m_localPlayer;
        if (player == null || _body == null || _root == null) return;
        var rows = Collect(player);
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var restoreFocus = selected != null && selected.transform.IsChildOf(_body.transform);
        var rebuild = rows.Count != Views.Count;
        if (!rebuild)
            for (var i = 0; i < rows.Count; i++)
                if (rows[i].Key != Views[i].Key) { rebuild = true; break; }
        Rows.Clear();
        Rows.AddRange(rows);
        if (rebuild)
        {
            var position = _scroll != null ? _scroll.content.anchoredPosition : Vector2.zero;
            foreach (Transform child in _body.transform)
            {
                child.gameObject.SetActive(false); // Destroy is deferred; keep old rows out of layout now.
                Object.Destroy(child.gameObject);
            }
            Views.Clear();
            string? group = null;
            foreach (var row in rows)
            {
                if (row.Group != group) { group = row.Group; Head(group); }
                Line(row);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(_body.GetComponent<RectTransform>());
            if (_scroll != null)
            {
                position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, _scroll.content.rect.height - _scroll.viewport.rect.height));
                _scroll.content.anchoredPosition = position;
            }
        }
        else
            for (var i = 0; i < rows.Count; i++)
                if (Views[i].Value.text != rows[i].Value) Views[i].Value.text = rows[i].Value;
        if (!rows.Exists(row => row.Key == _hover))
        {
            _hover = rows.Count > 0 ? rows[0].Key : "";
            if (_detailScroll != null) _detailScroll.content.anchoredPosition = Vector2.zero;
        }
        if (rebuild && restoreFocus && EventSystem.current != null)
        {
            var focus = Views.Find(view => view.Key == _hover);
            if (focus != null) EventSystem.current.SetSelectedGameObject(focus.Back.gameObject);
        }
        PlaceOrigin();
    }

    private static void Head(string title)
    {
        var go = RestlessUi.Node(_body!.transform, "section");
        go.AddComponent<LayoutElement>().preferredHeight = 38f;
        var face = RestlessUi.Label(go.transform, title, 18, RestlessUi.Accent, TextAnchor.LowerLeft);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(12f, 6f), new Vector2(-8f, -2f));
    }

    private static void Line(StatRow row)
    {
        var go = RestlessUi.Graphic(_body!.transform, "row", Color.clear);
        go.AddComponent<LayoutElement>().preferredHeight = RowH;
        RestlessUi.Metric(go.transform, row.Label, row.Value);
        var texts = go.GetComponentsInChildren<Text>();
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        button.colors = colors;
        var hover = go.AddComponent<SheetHover>();
        hover.Key = row.Key;
        button.onClick.AddListener(() => Select(row.Key));
        Views.Add(new RowView { Key = row.Key, Value = texts[texts.Length - 1], Back = go.GetComponent<Image>() });
    }

    private static void Select(string key)
    {
        if (_hover == key) return;
        _hover = key;
        _detailCopy = "";
        if (_detailScroll != null) _detailScroll.content.anchoredPosition = Vector2.zero;
        PlaceOrigin();
    }

    private static void PlaceOrigin()
    {
        if (_originFace == null || _originTitle == null || _originValue == null || _detailScroll == null) return;
        var row = Rows.Find(r => r.Key == _hover);
        if (row == null) return;
        foreach (var view in Views)
            view.Back.color = view.Key == _hover ? new Color(0.72f, 0.57f, 0.34f, 0.12f) : Color.clear;
        var sb = new StringBuilder();
        foreach (var part in row.Parts)
            sb.Append(part.Name).Append('\n').Append(part.Value).Append("\n\n");
        if (row.Parts.Count == 0) sb.Append("No additional sources.\n\n");
        sb.Append(row.Note);
        var copy = sb.ToString();
        _originTitle.text = row.Label;
        _originValue.text = row.Value;
        if (_detailCopy == copy) return;
        _detailCopy = copy;
        _originFace.text = copy;
        var height = _originFace.preferredHeight + 8f;
        _detailScroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        var position = _detailScroll.content.anchoredPosition;
        position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, height - _detailScroll.viewport.rect.height));
        _detailScroll.content.anchoredPosition = position;
    }

    private sealed class RowView
    {
        public string Key = "";
        public Text Value = null!;
        public Image Back = null!;
    }
    private sealed class SheetOpener : MonoBehaviour { }
    private sealed class SheetHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public string Key = "";
        public void OnPointerEnter(PointerEventData eventData) => Select(Key);
        public void OnSelect(BaseEventData eventData)
        {
            Select(Key);
            if (_scroll == null) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_scroll.content, transform);
            var position = _scroll.content.anchoredPosition;
            var top = -bounds.max.y;
            var bottom = -bounds.min.y;
            if (top < position.y) position.y = top;
            else if (bottom > position.y + _scroll.viewport.rect.height) position.y = bottom - _scroll.viewport.rect.height;
            position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, _scroll.content.rect.height - _scroll.viewport.rect.height));
            _scroll.content.anchoredPosition = position;
        }
    }
    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
        private static void AfterHide() => Hide();
    }
}
