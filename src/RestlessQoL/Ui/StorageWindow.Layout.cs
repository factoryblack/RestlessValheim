using System;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Ui;

internal sealed partial class StorageWindow
{
    private void Build()
    {
        var paper = RestlessUi.Tray(transform, "RestlessStorageSheet");
        RestlessUi.PaperSurface(paper);
        paper.GetComponent<Image>().raycastTarget = true;
        _sheet = RestlessUi.Pin(paper, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(1200f, 800f));
        RestlessUi.PaperCorner(paper);
        _title = Label(_sheet, "Storage network", 34, 28, 24, 650, 42);
        _scope = Label(_sheet, "", 18, 30, 70, 700, 24, true);
        _stores = Label(_sheet, "", 18, 808, 38, 228, 32, true);
        Button(_sheet, "ESC", 1048, 32, 64, 36, Dismiss);
        Rule(_sheet, 28, 100, 1120);

        var search = Field(_sheet, "Search resources…", 28, 118, 596, 40);
        search.onValueChanged.AddListener(value => { _query = value; Filter(true); });
        var sort = Button(_sheet, "Name A–Z", 638, 118, 182, 40, () => { });
        sort.onClick.RemoveAllListeners();
        sort.onClick.AddListener(() =>
        {
            _sort = (_sort + 1) % 3;
            sort.GetComponentInChildren<Text>().text = new[] { "Name A–Z", "Quantity ↓", "Unit weight ↓" }[_sort];
            Filter(true);
        });
        for (var i = 0; i < _categories.Length; i++)
        {
            var category = _categories[i];
            _tabs.Add(Button(_sheet, category, 28 + i * 160, 172, 150, 32,
                () => { _category = category; Filter(true); }));
        }
        _grid = Reader(_sheet, "resources", 28, 220, 806, 516, 140f);
        _grid.RowsPerNotch = 2f;
        for (var i = 0; i < Columns * PoolRows; i++)
        {
            var cell = new Cell();
            cell.Button = Button(_grid.content, "", 0, 0, 122, 128, () => Select(cell));
            var oldLabel = cell.Button.GetComponentInChildren<Text>();
            Destroy(oldLabel.gameObject);
            cell.Icon = RestlessUi.Graphic(cell.Button.transform, "icon", Color.white, false).GetComponent<Image>();
            cell.Icon.preserveAspect = true; At(cell.Icon.gameObject, 24, 6, 74, 58);
            cell.Name = Label(cell.Button.transform, "", 16, 8, 66, 106, 36);
            cell.Name.alignment = TextAnchor.UpperCenter;
            cell.Name.verticalOverflow = VerticalWrapMode.Truncate;
            cell.Name.resizeTextForBestFit = false;
            cell.Count = Label(cell.Button.transform, "", 16, 8, 104, 106, 18);
            cell.Count.alignment = TextAnchor.MiddleRight; cell.Count.color = RestlessUi.Accent;
            _cells.Add(cell);
        }
        _grid.onValueChanged.AddListener(_ => BindCells(false));
        _empty = Label(_sheet, "", 22, 80, 380, 700, 84, true);
        _empty.alignment = TextAnchor.MiddleCenter;
        var divider = RestlessUi.Graphic(_sheet, "detail-divider", new Color(0.5f, 0.43f, 0.32f, 0.5f), false);
        At(divider, 850, 118, 1, 618);

        _detail = Reader(_sheet, "detail", 872, 118, 300, 376, 36f);
        _portrait = RestlessUi.Graphic(_detail.content, "portrait", Color.white, false).GetComponent<Image>();
        _portrait.preserveAspect = true; At(_portrait.gameObject, 8, 8, 72, 72);
        var portraitFrame = RestlessUi.Graphic(_detail.content, "frame", Color.white, false);
        RestlessUi.PortraitFrame(portraitFrame); At(portraitFrame, 0, 0, 88, 88); portraitFrame.transform.SetAsFirstSibling();
        _name = Label(_detail.content, "", 24, 100, 0, 184, 60);
        RestlessUi.BoundedLabel(_name, 24, 18);
        _categoryLabel = Label(_detail.content, "", 16, 100, 64, 184, 24, true);
        _description = Label(_detail.content, "", 18, 0, 108, 284, 48, true);
        _description.horizontalOverflow = HorizontalWrapMode.Wrap;
        _description.verticalOverflow = VerticalWrapMode.Overflow;
        _total = Label(_detail.content, "", 24, 0, 180, 284, 30);
        _total.color = RestlessUi.Accent;
        _sourcesHeading = Label(_detail.content, "", 18, 0, 220, 284, 26, true);
        Rule(_sheet, 872, 510, 300);
        Label(_sheet, "Take amount", 22, 872, 528, 300, 28);
        _minus = Button(_sheet, "−", 872, 568, 48, 42, () => SetAmount(_amount - 1));
        _quantity = Field(_sheet, "1", 930, 568, 180, 42);
        _quantity.contentType = InputField.ContentType.IntegerNumber;
        _quantity.characterLimit = 10;
        _quantity.textComponent.alignment = TextAnchor.MiddleCenter;
        _quantity.textComponent.fontSize = 24;
        _quantity.onEndEdit.AddListener(value =>
        {
            if (!_paintingAmount) SetAmount(long.TryParse(value, out var count)
                ? (int)Math.Max(1L, Math.Min(int.MaxValue, count)) : 1);
        });
        _plus = Button(_sheet, "+", 1120, 568, 48, 42, () => SetAmount(_amount == int.MaxValue ? int.MaxValue : _amount + 1));
        _presets.Add(Button(_sheet, "1", 872, 620, 90, 30, () => SetAmount(1)));
        _presets.Add(Button(_sheet, "Stack", 974, 620, 90, 30, () => SetAmount(Selected()?.StackSize ?? 1)));
        _presets.Add(Button(_sheet, "All", 1076, 620, 92, 30, () => SetAmount((int)Math.Min(int.MaxValue, Selected()?.Count ?? 1))));
        _addedWeight = Label(_sheet, "", 16, 872, 662, 300, 24, true);
        _take = Button(_sheet, "Take 1", 872, 698, 300, 42, Withdraw);
        RestlessUi.ForgedSurface(_take.gameObject, action: true, interactive: true);
        _takeLabel = _take.GetComponentInChildren<Text>();
        Rule(_sheet, 28, 754, 1144);
        _footer = Label(_sheet, "", 16, 28, 766, 230, 24, true);
        _status = Label(_sheet, "", 15, 268, 766, 570, 24, true);
        _weight = Label(_sheet, "", 16, 872, 766, 300, 24);
        _weight.alignment = TextAnchor.MiddleRight;
        Fit();
    }

    private static void At(GameObject go, float x, float y, float width, float height) =>
        RestlessUi.Pin(go, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height));

    private static Text Label(Transform parent, string copy, int size, float x, float y, float width, float height, bool muted = false)
    {
        var label = RestlessUi.Label(parent, copy, size, muted ? RestlessUi.PaperMuted : RestlessUi.Text, TextAnchor.MiddleLeft);
        At(label.gameObject, x, y, width, height);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        return label;
    }
    private static Button Button(Transform parent, string copy, float x, float y, float width, float height, Action clicked)
    {
        var go = RestlessUi.Chip(parent, "control"); RestlessUi.PaperControl(go);
        At(go, x, y, width, height);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        var label = RestlessUi.Label(go.transform, copy, 18, RestlessUi.Text, TextAnchor.MiddleCenter);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(8, 3), new Vector2(-8, -3));
        RestlessUi.BoundedLabel(label, 18, 14);
        button.onClick.AddListener(() => clicked());
        return button;
    }
    private static InputField Field(Transform parent, string placeholder, float x, float y, float width, float height)
    {
        var go = RestlessUi.Chip(parent, "input"); RestlessUi.PaperControl(go); At(go, x, y, width, height);
        var field = go.AddComponent<InputField>(); field.targetGraphic = go.GetComponent<Image>();
        var face = RestlessUi.Label(go.transform, "", 18, RestlessUi.Text, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(face.gameObject, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
        var hint = RestlessUi.Label(go.transform, placeholder, 18, RestlessUi.PaperMuted, TextAnchor.MiddleLeft);
        RestlessUi.Stretch(hint.gameObject, Vector2.zero, Vector2.one, new Vector2(12, 4), new Vector2(-12, -4));
        field.textComponent = face; field.placeholder = hint;
        field.lineType = InputField.LineType.SingleLine; field.characterLimit = 128;
        field.caretColor = RestlessUi.Accent;
        RestlessUi.PaperSelectable(field);
        return field;
    }
    private static void Rule(Transform parent, float x, float y, float width)
    {
        var line = RestlessUi.Graphic(parent, "rule", new Color(0.5f, 0.43f, 0.32f, 0.5f), false);
        At(line, x, y, width, 1);
    }
    private static RestlessScrollRect Reader(Transform parent, string name, float x, float y, float width, float height, float rowHeight)
    {
        var root = RestlessUi.Node(parent, name); At(root, x, y, width, height);
        var scroll = root.AddComponent<RestlessScrollRect>(); scroll.horizontal = false; scroll.RowHeight = rowHeight;
        var view = RestlessUi.Graphic(root.transform, "viewport", Color.clear, true);
        At(view, 0, 0, width - 16, height); view.AddComponent<RectMask2D>();
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = RestlessUi.Node(view.transform, "content"); At(content, 0, 0, width - 16, height);
        scroll.content = content.GetComponent<RectTransform>();
        var native = InventoryGui.instance != null ? InventoryGui.instance.m_recipeListScroll : null;
        if (native != null)
        {
            var bar = Instantiate(native, root.transform, false);
            // Borrow artwork, never the native control's event bindings.
            bar.onValueChanged = new Scrollbar.ScrollEvent();
            At(bar.gameObject, width - 10, 0, 10, height);
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        return scroll;
    }
}
