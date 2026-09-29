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
        var names = new[] { "Recipes", "Orders", "Kitchen", "Pantry" };
        for (var i = 0; i < names.Length; i++)
        { var tab = i; _tabs.Add(Button(_sheet, names[i], 570 + i * 124, 34, 114, 38, () => Tab(tab))); }
        Button(_sheet, "ESC", 1090, 34, 64, 38, Close);
        Rule(_sheet, 28, 112, 1176, 1);
        var search = Field(_sheet, 28, 132, 252, 40);
        search.onValueChanged.AddListener(value => { _query = value; if (_tab == 0) { RecipeList(); Refresh(true); Size(_recipes,_recipes.content.rect.height,true); } });
        var categories = new[] { "All", "Feasts", "Meals", "Ingredients", "Meads" };
        for (var i = 0; i < categories.Length; i++)
        {
            var category = categories[i];
            var button = Button(_sheet, category, 28 + (i == 4 ? 168 : i % 3 * 84), 182 + i / 3 * 36, i == 3 ? 164 : 80, 30, () =>
            {
                _category = category;
                for (var j = 0; j < _filters.Count; j++) RestlessUi.PaperControl(_filters[j].gameObject, categories[j] == category ? RestlessUi.Accent : (Color?)null);
                if (_tab == 0) { RecipeList(); Refresh(true); Size(_recipes,_recipes.content.rect.height,true); }
            });
            _filters.Add(button);
            if (category == _category) RestlessUi.PaperControl(button.gameObject, RestlessUi.Accent);
        }
        _recipes = Reader(_sheet, "recipes", 28, 260, 252, 422, 96);
        Label(_sheet, "Order quantity", 18, 28, 696, 252, 26);
        Button(_sheet, "−", 28, 726, 52, 36, () => Quantity(-1));
        _quantity = Label(_sheet, "1", 22, 88, 726, 132, 36); _quantity.alignment = TextAnchor.MiddleCenter;
        Button(_sheet, "+", 228, 726, 52, 36, () => Quantity(1));
        Rule(_sheet, 296, 132, 1, 630);
        _back = Button(_sheet, "‹ Back", 316, 132, 100, 34, () =>
        {
            if (_trail.Count == 0) return;
            _focus = _trail[_trail.Count - 1]; _trail.RemoveAt(_trail.Count - 1); PaintTree(); PaintDetails();
            Size(_tree, _tree.content.rect.height, true); Size(_details,_details.content.rect.height,true);
        });
        _heading = Label(_sheet, "Choose a recipe", 24, 428, 130, 424, 62);
        RestlessUi.BoundedLabel(_heading, 24, 19);
        _tree = Reader(_sheet, "recipe-tree", 316, 206, 552, 552, 112);
        Rule(_sheet, 884, 132, 1, 630);
        Label(_sheet, "SELECTED STEP", 18, 904, 136, 300, 30);
        _dish = RestlessUi.Graphic(_sheet,"featured-dish",Color.white,false).GetComponent<Image>();
        _dish.preserveAspect = true; Portrait(_dish.gameObject,916,194,68,52);
        _dishFrame = RestlessUi.Picture(_sheet,"dish-frame","cook-portrait",false);
        Portrait(_dishFrame,904,186,96,64);
        _dishName = Label(_sheet,"",22,1012,180,192,78);
        RestlessUi.BoundedLabel(_dishName,22,18);
        var ribbon = RestlessUi.Node(_sheet,"dish-state"); Position(ribbon,904,264,304,30);
        _dishKind = Label(ribbon.transform,"",17,0,0,304,30); RestlessUi.BoundedLabel(_dishKind,17,14);
        _station = Label(_sheet,"",18,904,302,300,48);
        Rule(_sheet,904,352,300,1);
        _details = Reader(_sheet, "details", 904, 358, 304, 262, 64);
        _requirementsLabel = Label(_details.content,"",15,0,0,280,24);
        _copy = Label(_details.content, "", 17, 0, 0, 280, 100);
        _copy.alignment = TextAnchor.UpperLeft; _copy.verticalOverflow = VerticalWrapMode.Overflow;
        _collect = Button(_sheet, "Collect ready food", 904, 636, 304, 42, Collect);
        _action = Button(_sheet, "Queue 1", 904, 694, 304, 52, Act);
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
        { var line = RestlessUi.Graphic(_tree.content, "branch", new Color(.64f,.52f,.32f,.85f), false); line.transform.SetAsFirstSibling(); _lines.Add(line); }
        var go = _lines[index]; go.SetActive(true); Position(go, x, y, Mathf.Max(1,width), height);
    }
    private static void Position(GameObject go, float x, float y, float w, float h) =>
        RestlessUi.Pin(go, new Vector2(0,1), new Vector2(0,1), new Vector2(x,-y), new Vector2(w,h));
    // Image.preserveAspect uses the RectTransform pivot: centre it explicitly.
    private static void Portrait(GameObject go,float x,float y,float w,float h) =>
        RestlessUi.Pin(go,new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(x+w/2,-y-h/2),new Vector2(w,h));
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
        var view = RestlessUi.Graphic(root.transform,"viewport",Color.clear,true); Position(view,0,0,w-24,h); view.AddComponent<RectMask2D>();
        scroll.viewport = (RectTransform)view.transform;
        var content = RestlessUi.Node(view.transform,"content"); Position(content,0,0,w-24,h); scroll.content = (RectTransform)content.transform;
        if (InventoryGui.instance != null && InventoryGui.instance.m_recipeListScroll != null)
        {
            var bar = Instantiate(InventoryGui.instance.m_recipeListScroll,root.transform,false);
            bar.onValueChanged = new Scrollbar.ScrollEvent(); Position(bar.gameObject,w-6,0,6,h);
            bar.transform.localScale = Vector3.one;
            // The native prefab can retain a wide sliding area after its root is resized.
            if (bar.handleRect != null)
            {
                var area = bar.handleRect.parent as RectTransform;
                if (area != null && area != bar.transform)
                    RestlessUi.Stretch(area.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                var handle = bar.handleRect;
                handle.sizeDelta = new Vector2(0,handle.sizeDelta.y);
                handle.anchoredPosition = new Vector2(0,handle.anchoredPosition.y);
            }
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
        private readonly Image _bar;
        private readonly GameObject _track;
        private readonly Image _statusDiamond;
        private Action? _clicked;
        internal Card(Transform parent)
        {
            Root = RestlessUi.Chip(parent,"recipe"); RestlessUi.PaperControl(Root);
            _button = Root.AddComponent<Button>(); _button.targetGraphic = Root.GetComponent<Image>(); RestlessUi.PaperSelectable(_button);
            _icon = RestlessUi.Graphic(Root.transform,"icon",Color.white,false).GetComponent<Image>(); _icon.preserveAspect = true;
            _name = Label(Root.transform,"",18,0,0,100,40); _name.alignment = TextAnchor.MiddleLeft; RestlessUi.BoundedLabel(_name,18,15);
            _statusDiamond = RestlessUi.Graphic(Root.transform,"status-diamond",Color.white,false).GetComponent<Image>();
            _statusDiamond.rectTransform.localRotation = Quaternion.Euler(0,0,45);
            _track = RestlessUi.Graphic(Root.transform,"preparation-track",RestlessUi.Ink,false);
            _bar = RestlessUi.Graphic(_track.transform,"progress",RestlessUi.Accent,false).GetComponent<Image>();
            _state = Label(Root.transform,"",15,0,0,100,30); _state.alignment = TextAnchor.MiddleLeft;
            RestlessUi.BoundedLabel(_state,15,13); _button.onClick.AddListener(() => _clicked?.Invoke());
        }
        internal void Progress(KitchenStep step)
        {
            var size = ((RectTransform)Root.transform).rect.size;
            _track.SetActive(step.State == KitchenStepState.Cooking && step.DurationSeconds > 0); Position(_track,12,size.y-10,size.x-24,4);
            float fraction = step.State == KitchenStepState.Prepared ? 1f : step.DurationSeconds > 0
                ? Mathf.Clamp01(step.ElapsedSeconds/step.DurationSeconds) : 0f;
            Position(_bar.gameObject,0,0,(size.x-24)*fraction,4);
            _bar.color = Tint(step.State);
        }
        internal void Set(string name,string state,Sprite? icon,Color tint,Action action,bool selected = false,bool featured = false)
        {
            _clicked = action; var size = ((RectTransform)Root.transform).rect.size;
            _statusDiamond.gameObject.SetActive(featured);
            if (featured)
            {
                Portrait(_icon.gameObject,26,(size.y-72)/2,72,72);
                Position(_name.gameObject,114,18,size.x-178,52);
                Portrait(_statusDiamond.gameObject,116,81,7,7);
                Position(_state.gameObject,134,72,size.x-172,26);
            }
            else if (size.y <= 64)
            {
                Portrait(_icon.gameObject,6,(size.y-36)/2,36,36);
                Position(_name.gameObject,50,4,size.x-124,size.y-8);
                Position(_state.gameObject,size.x-70,4,64,size.y-8);
            }
            else
            {
                Portrait(_icon.gameObject,10,12,48,48);
                Position(_name.gameObject,68,6,size.x-80,50);
                Position(_state.gameObject,12,62,size.x-24,size.y-76);
            }
            _name.alignment = TextAnchor.MiddleLeft;
            _state.alignment = size.y <= 64 ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            _icon.sprite = icon; _icon.enabled = icon != null; _name.text = name; _state.text = state; _state.color = tint;
            _track.SetActive(false);
            _statusDiamond.color = tint;
            if (featured) RestlessUi.CookHeroSurface(Root);
            else
            {
                var hero = Root.transform.Find("cookHeroFrame");
                if (hero != null) hero.gameObject.SetActive(false);
                RestlessUi.PaperControl(Root,selected ? RestlessUi.Accent : new Color(.37f,.33f,.27f));
                // Cards are pooled between the recipe tree, orders and pantry.
                var rim = Root.transform.Find("paperAccent");
                if (rim != null) rim.gameObject.SetActive(true);
            }
        }
    }
}

