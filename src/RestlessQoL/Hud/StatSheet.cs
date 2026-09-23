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

    private const float SheetWidth = 920f;
    private const float SheetHeight = 640f;
    private const float RowH = 44f;

    private static GameObject? _root;
    private static GameObject? _body;
    private static GameObject? _sourceBody;
    private static bool _sourcesOpen = true;
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
        _sourcesOpen = true;
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
        _originTitle = _originValue = null;
        _sourceBody = null;
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
        RestlessUi.LoadoutCorner(_root.transform);
        var title = RestlessUi.Label(_root.transform, "Loadout totals", 30, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(title.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(28f, -58f), new Vector2(-228f, -14f));
        var caption = RestlessUi.Label(_root.transform, "Equipped gear · food · active effects", 18,
            RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(caption.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(28f, -86f), new Vector2(-132f, -58f));
        var close = RestlessUi.Chip(_root.transform, "close");
        RestlessUi.PaperControl(close);
        RestlessUi.Pin(close, Vector2.one, Vector2.one, new Vector2(-126f, -26f), new Vector2(76f, 32f));
        var closeLabel = RestlessUi.Label(close.transform, "Close", 18, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(closeLabel.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var button = close.AddComponent<Button>();
        button.targetGraphic = close.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(Hide);

        var rule = RestlessUi.Graphic(_root.transform, "rule", new Color(0.58f, 0.46f, 0.30f, 0.45f), false);
        RestlessUi.Stretch(rule, new Vector2(0f, 1f), Vector2.one, new Vector2(28f, -98f), new Vector2(-28f, -97f));
        var list = RestlessUi.Node(_root.transform, "totals");
        RestlessUi.Stretch(list, Vector2.zero, Vector2.one, new Vector2(24f, 32f), new Vector2(-484f, -110f));
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
            new Vector2(-469f, 32f), new Vector2(-468f, -112f));
        var detail = RestlessUi.Node(_root.transform, "sources");
        RestlessUi.Stretch(detail, new Vector2(1f, 0f), Vector2.one,
            new Vector2(-448f, 32f), new Vector2(-24f, -110f));
        var plaque = RestlessUi.Graphic(detail.transform, "selectedTotal", Color.white, false);
        RestlessUi.LoadoutPlaque(plaque);
        RestlessUi.Stretch(plaque, new Vector2(0f, 1f), Vector2.one,
            new Vector2(0f, -108f), new Vector2(-24f, 0f));
        _originTitle = RestlessUi.Label(plaque.transform, "Sources", 23, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(_originTitle.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(24f, -55f), new Vector2(-24f, -12f));
        RestlessUi.BoundedLabel(_originTitle, 23, 18);
        _originValue = RestlessUi.Label(plaque.transform, "", 32, RestlessUi.Accent, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(_originValue.gameObject, new Vector2(0f, 1f), Vector2.one,
            new Vector2(24f, -94f), new Vector2(-24f, -54f));
        RestlessUi.BoundedLabel(_originValue, 32, 20);
        var detailView = RestlessUi.Node(detail.transform, "reader");
        RestlessUi.Stretch(detailView, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -122f));
        _detailScroll = Scroller(gui, detailView.transform, out var copy);
        _sourceBody = copy;
        var sourceLayout = copy.AddComponent<VerticalLayoutGroup>();
        sourceLayout.spacing = 10f;
        sourceLayout.padding = new RectOffset(4, 8, 4, 8);
        sourceLayout.childControlWidth = sourceLayout.childControlHeight = true;
        sourceLayout.childForceExpandWidth = true;
        sourceLayout.childForceExpandHeight = false;
        copy.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
            {
                if (Views[i].Value.text != rows[i].Value) Views[i].Value.text = rows[i].Value;
                if (Views[i].SetState != null)
                {
                    Views[i].SetState!.text = rows[i].SetActive ? "Active set" : "Inactive set";
                    Views[i].SetState!.color = rows[i].SetActive ? RestlessUi.SetActiveTint : RestlessUi.PaperMuted;
                }
            }
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
        go.AddComponent<LayoutElement>().preferredHeight = row.SetRequired > 0 ? 68f : RowH;
        var metricHost = go.transform;
        Text? setState = null;
        if (row.SetRequired > 0)
        {
            var seal = RestlessUi.Picture(go.transform, "setSeal", "equipment-set-seal");
            RestlessUi.Pin(seal, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(12f, 0f), new Vector2(30f, 30f));
            seal.GetComponent<Image>().raycastTarget = false;
            var inset = RestlessUi.Node(go.transform, "setMetric");
            RestlessUi.Stretch(inset, Vector2.zero, Vector2.one, new Vector2(38f, 20f), Vector2.zero);
            metricHost = inset.transform;
            setState = RestlessUi.Label(go.transform, row.SetActive ? "Active set" : "Inactive set", 16,
                row.SetActive ? RestlessUi.SetActiveTint : RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
            RestlessUi.Stretch(setState.gameObject, Vector2.zero, new Vector2(1f, 0f),
                new Vector2(58f, 4f), new Vector2(-12f, 24f));
        }
        RestlessUi.Metric(metricHost, row.Label, row.Value);
        var texts = metricHost.Find("RestlessMetric").GetComponentsInChildren<Text>();
        var button = go.AddComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = colors.selectedColor = new Color(1f, 1f, 1f, 1f);
        button.colors = colors;
        var hover = go.AddComponent<SheetHover>();
        hover.Key = row.Key;
        button.onClick.AddListener(() => Select(row.Key));
        Views.Add(new RowView { Key = row.Key, Value = texts[texts.Length - 1], Back = go.GetComponent<Image>(), SetState = setState });
    }

    private static void Select(string key)
    {
        if (_hover == key) return;
        _hover = key;
        _sourcesOpen = true;
        _detailCopy = "";
        if (_detailScroll != null) _detailScroll.content.anchoredPosition = Vector2.zero;
        PlaceOrigin();
    }

    private static void PlaceOrigin()
    {
        if (_sourceBody == null || _originTitle == null || _originValue == null || _detailScroll == null) return;
        var row = Rows.Find(r => r.Key == _hover);
        if (row == null) return;
        foreach (var view in Views)
            view.Back.color = view.Key == _hover ? new Color(0.72f, 0.57f, 0.34f, 0.18f) : Color.clear;
        var sb = new StringBuilder(row.Key).Append('|').Append(row.SetEquipped).Append('|')
            .Append(row.SetRequired).Append('|').Append(row.SetActive).Append('|').Append(_sourcesOpen);
        foreach (var part in row.Parts) sb.Append('\n').Append(part.Name).Append('\n').Append(part.Value);
        sb.Append('\n').Append(row.Note);
        var stamp = sb.ToString();
        _originTitle.text = row.SetRequired > 0 ? "Equipment set" : row.Label;
        _originValue.text = row.Value;
        _originValue.color = row.SetRequired > 0 && row.SetActive ? RestlessUi.SetActiveTint : RestlessUi.Accent;
        if (_detailCopy == stamp) return;
        _detailCopy = stamp;
        RebuildSources(row);
    }

    private static void RebuildSources(StatRow row)
    {
        if (_sourceBody == null || _detailScroll == null) return;
        var position = _detailScroll.content.anchoredPosition;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        var restoreFocus = selected != null && selected.transform.IsChildOf(_sourceBody.transform);
        foreach (Transform child in _sourceBody.transform)
        {
            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }
        var width = Mathf.Max(100f, _detailScroll.viewport.rect.width - 12f);
        var parent = _sourceBody.transform;
        if (row.SetRequired > 0)
        {
            var well = RestlessUi.SetWell(parent);
            RestlessUi.SetIdentity(well.transform, row.Label, row.SetEquipped, row.SetRequired, row.SetActive, width - 28f);
            RestlessUi.LoadoutCopy(well.transform, "Applies once for the set.", width - 28f, 17, RestlessUi.PaperMuted);
        }
        var toggle = RestlessUi.Strip(parent, "sourceToggle");
        toggle.AddComponent<LayoutElement>().preferredHeight = 40f;
        RestlessUi.PaperControl(toggle);
        var label = RestlessUi.Label(toggle.transform,
            (row.SetRequired > 0 ? "Equipped pieces" : "Sources") + " (" + row.Parts.Count + ")",
            20, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-38f, -4f));
        var glyph = RestlessUi.Picture(toggle.transform, "expand", _sourcesOpen ? "utility-collapse" : "utility-expand");
        RestlessUi.Pin(glyph, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(16f, 16f));
        glyph.GetComponent<Image>().raycastTarget = false;
        var button = toggle.AddComponent<Button>();
        button.targetGraphic = toggle.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(() => { _sourcesOpen = !_sourcesOpen; _detailCopy = ""; PlaceOrigin(); });
        if (_sourcesOpen)
        {
            foreach (var part in row.Parts)
            {
                var entry = RestlessUi.Node(parent, "contribution");
                var layout = entry.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(12, 12, 4, 6);
                layout.spacing = 3f;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                RestlessUi.LoadoutCopy(entry.transform, part.Name, width - 24f, 20, RestlessUi.Text);
                RestlessUi.LoadoutCopy(entry.transform, part.Value, width - 24f, 20, RestlessUi.Accent);
            }
            if (row.Parts.Count == 0)
                RestlessUi.LoadoutCopy(parent, "No additional sources.", width, 18, RestlessUi.PaperMuted);
        }
        RestlessUi.LoadoutCopy(parent, row.Note, width, 18, RestlessUi.PaperMuted);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_sourceBody.GetComponent<RectTransform>());
        position.y = Mathf.Clamp(position.y, 0f, Mathf.Max(0f, _detailScroll.content.rect.height - _detailScroll.viewport.rect.height));
        _detailScroll.content.anchoredPosition = position;
        if (restoreFocus && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(toggle);
    }

    private sealed class RowView
    {
        public string Key = "";
        public Text Value = null!;
        public Image Back = null!;
        public Text? SetState;
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
