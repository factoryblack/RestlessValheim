using System;
using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessCook;

internal sealed partial class CookbookWindow
{
    private void Build()
    {
        var paper = RestlessUi.Tray(transform, "Cookbook"); RestlessUi.PaperSurface(paper);
        paper.GetComponent<Image>().raycastTarget = true;
        _sheet = RestlessUi.Pin(paper, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, new Vector2(1240,800));
        RestlessUi.PaperCorner(paper);
        Label(_sheet, "Cookbook", 36, 28, 20, 340, 52);
        Label(_sheet, "Cook the haul. Prepare meals. Share a feast.", 16, 30, 75, 620, 28);
        var names = new[] { "Recipes", "Orders", "Kitchen" };
        for (var i = 0; i < names.Length; i++)
        { var tab = i; _tabs.Add(Button(_sheet, names[i], 660 + i * 138, 34, 128, 38, () => Tab(tab))); }
        Button(_sheet, "ESC", 1090, 34, 64, 38, Close);
        Rule(_sheet, 28, 112, 1176, 1);
        var search = Field(_sheet, 28, 132, 252, 40);
        search.onValueChanged.AddListener(value => { _query = value; if (_tab == 0) RecipeList(); });
        var filter = Button(_sheet, "Feasts", 28, 182, 252, 34, () => { });
        filter.onClick.AddListener(() => { _feasts = !_feasts; filter.GetComponentInChildren<Text>().text = _feasts ? "Feasts" : "All food"; if (_tab == 0) RecipeList(); });
        _recipes = Reader(_sheet, "recipes", 28, 230, 252, 452, 90);
        Label(_sheet, "Order quantity", 18, 28, 696, 252, 26);
        Button(_sheet, "−", 28, 726, 52, 36, () => Quantity(-1));
        _quantity = Label(_sheet, "1", 22, 88, 726, 132, 36); _quantity.alignment = TextAnchor.MiddleCenter;
        Button(_sheet, "+", 228, 726, 52, 36, () => Quantity(1));
        Rule(_sheet, 296, 132, 1, 630);
        _back = Button(_sheet, "‹ Back", 316, 132, 100, 34, () =>
        {
            if (_trail.Count == 0) return;
            _focus = _trail[_trail.Count - 1]; _trail.RemoveAt(_trail.Count - 1); PaintTree(); PaintDetails();
            Size(_tree, _tree.content.rect.height, true);
        });
        _heading = Label(_sheet, "Choose a recipe", 24, 428, 130, 424, 62);
        RestlessUi.BoundedLabel(_heading, 24, 19);
        _tree = Reader(_sheet, "recipe-tree", 316, 206, 552, 552, 112);
        Rule(_sheet, 884, 132, 1, 630);
        Label(_sheet, "DETAILS", 20, 904, 136, 300, 30);
        _details = Reader(_sheet, "details", 904, 184, 304, 436, 36);
        _copy = Label(_details.content, "", 18, 0, 0, 280, 100);
        _copy.alignment = TextAnchor.UpperLeft; _copy.verticalOverflow = VerticalWrapMode.Overflow;
        _collect = Button(_sheet, "Collect ready food", 904, 636, 304, 42, Collect);
        _action = Button(_sheet, "Prepare 1", 904, 694, 304, 52, Act);
        RestlessUi.ForgedSurface(_action.gameObject, action: true, interactive: true);
        _actionText = _action.GetComponentInChildren<Text>();
        _status = Label(_sheet, "", 16, 316, 766, 888, 26);
        RestlessUi.BoundedLabel(_status, 16, 13);
        RestlessUi.PaperControl(_tabs[0].gameObject, RestlessUi.Accent); Fit();
    }
    private void Quantity(int delta)
    {
        if (_tab != 0) return;
        _count = Mathf.Clamp(_count + delta, 1, 100); Refresh(true);
    }
    private void Fit()
    {
        var rect = (RectTransform)transform;
        if (_canvas == rect.rect.size) return;
        _canvas = rect.rect.size;
        _sheet.localScale = Vector3.one * Mathf.Min(1, Mathf.Max(.1f, (_canvas.x - 24) / 1240), Mathf.Max(.1f, (_canvas.y - 24) / 800));
    }
    private void Line(int index, float x, float y, float width, float height)
    {
        if (index == _lines.Count)
        { var line = RestlessUi.Graphic(_tree.content, "branch", new Color(.55f,.45f,.28f,.7f), false); line.transform.SetAsFirstSibling(); _lines.Add(line); }
        var go = _lines[index]; go.SetActive(true); Position(go, x, y, Mathf.Max(1,width), height);
    }
    private static void Position(GameObject go, float x, float y, float w, float h) =>
        RestlessUi.Pin(go, new Vector2(0,1), new Vector2(0,1), new Vector2(x,-y), new Vector2(w,h));
    private static Text Label(Transform parent, string text, int size, float x, float y, float w, float h)
    {
        var label = RestlessUi.Label(parent, text, size, RestlessUi.Text, TextAnchor.MiddleLeft);
        Position(label.gameObject,x,y,w,h); label.horizontalOverflow = HorizontalWrapMode.Wrap; return label;
    }
    private static Button Button(Transform parent, string text, float x, float y, float w, float h, Action action)
    {
        var go = RestlessUi.Chip(parent,"control"); RestlessUi.PaperControl(go); Position(go,x,y,w,h);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); RestlessUi.PaperSelectable(button);
        var label = Label(go.transform,text,18,8,3,w-16,h-6); label.alignment = TextAnchor.MiddleCenter;
        RestlessUi.BoundedLabel(label,18,14); button.onClick.AddListener(() => action()); return button;
    }
    private static InputField Field(Transform parent, float x, float y, float w, float h)
    {
        var go = RestlessUi.Chip(parent,"search"); RestlessUi.PaperControl(go); Position(go,x,y,w,h);
        var field = go.AddComponent<InputField>(); field.targetGraphic = go.GetComponent<Image>();
        field.textComponent = Label(go.transform,"",18,10,3,w-20,h-6);
        field.placeholder = Label(go.transform,"Search recipes…",18,10,3,w-20,h-6);
        field.placeholder.color = RestlessUi.PaperMuted; field.characterLimit = 128;
        field.lineType = InputField.LineType.SingleLine; RestlessUi.PaperSelectable(field); return field;
    }
    private static void Rule(Transform parent,float x,float y,float w,float h)
    { Position(RestlessUi.Graphic(parent,"rule",new Color(.5f,.43f,.32f,.5f),false),x,y,w,h); }
    private static RestlessScrollRect Reader(Transform parent,string name,float x,float y,float w,float h,float row)
    {
        var root = RestlessUi.Node(parent,name); Position(root,x,y,w,h);
        var scroll = root.AddComponent<RestlessScrollRect>(); scroll.horizontal = false; scroll.RowHeight = row; scroll.RowsPerNotch = 2;
        var view = RestlessUi.Graphic(root.transform,"viewport",Color.clear,true); Position(view,0,0,w-12,h); view.AddComponent<RectMask2D>();
        scroll.viewport = (RectTransform)view.transform;
        var content = RestlessUi.Node(view.transform,"content"); Position(content,0,0,w-12,h); scroll.content = (RectTransform)content.transform;
        if (InventoryGui.instance != null && InventoryGui.instance.m_recipeListScroll != null)
        {
            var bar = Instantiate(InventoryGui.instance.m_recipeListScroll,root.transform,false);
            bar.onValueChanged = new Scrollbar.ScrollEvent(); Position(bar.gameObject,w-8,0,8,h);
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        return scroll;
    }
    private static void Size(RestlessScrollRect scroll,float height,bool reset)
    {
        height = Mathf.Max(scroll.viewport.rect.height,height);
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        var y = reset ? 0 : Mathf.Clamp(scroll.content.anchoredPosition.y,0,height-scroll.viewport.rect.height);
        if (reset || !Mathf.Approximately(y,scroll.content.anchoredPosition.y))
        { scroll.CancelWheel(); scroll.content.anchoredPosition = new Vector2(0,y); }
    }
    private static Card GetCard(List<Card> cards,int i,Transform parent)
    {
        if (i == cards.Count) cards.Add(new Card(parent));
        cards[i].Root.SetActive(true); return cards[i];
    }
    private static void HideAfter(List<Card> cards,int count)
    { for(var i=count;i<cards.Count;i++) cards[i].Root.SetActive(false); }
    private sealed class Card
    {
        internal readonly GameObject Root;
        private readonly Button _button;
        private readonly Image _icon;
        private readonly Text _name, _state;
        private Action? _clicked;
        internal Card(Transform parent)
        {
            Root = RestlessUi.Chip(parent,"recipe"); RestlessUi.PaperControl(Root);
            _button = Root.AddComponent<Button>(); _button.targetGraphic = Root.GetComponent<Image>(); RestlessUi.PaperSelectable(_button);
            _icon = RestlessUi.Graphic(Root.transform,"icon",Color.white,false).GetComponent<Image>(); _icon.preserveAspect = true;
            _name = Label(Root.transform,"",18,0,0,100,40); _name.alignment = TextAnchor.MiddleLeft; RestlessUi.BoundedLabel(_name,18,15);
            _state = Label(Root.transform,"",15,0,0,100,30); _state.alignment = TextAnchor.MiddleLeft;
            RestlessUi.BoundedLabel(_state,15,13); _button.onClick.AddListener(() => _clicked?.Invoke());
        }
        internal void Set(string name,string state,Sprite? icon,Color tint,Action action)
        {
            _clicked = action; var size = ((RectTransform)Root.transform).rect.size;
            Position(_icon.gameObject,10,10,52,52); _icon.sprite = icon; _icon.enabled = icon != null;
            Position(_name.gameObject,72,8,size.x-84,48); _name.text = name;
            Position(_state.gameObject,12,60,size.x-24,size.y-64); _state.text = state; _state.color = tint;
            RestlessUi.PaperControl(Root,tint);
        }
    }
}
