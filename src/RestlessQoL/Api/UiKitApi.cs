using System;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Api;

/// <summary>Supported client-side UI components. Call on Unity's main thread after GUI creation.
/// The caller owns returned objects and listeners; destroy the screen root on shutdown.
/// Cached kit sprites/textures are shared and must not be destroyed or modified.</summary>
public static class UiKitApi
{
    public const int Version = 1;
    public const int ControlFontSize = 18;
    public const int MinimumControlFontSize = 14;
    public const float ControlInsetX = 8f;
    public const float ControlInsetY = 3f;
    public const float ScrollbarWidth = 6f;
    public const float ScrollContentGutter = 24f;
    public static Color Text => RestlessUi.Text;
    public static Color Muted => RestlessUi.PaperMuted;
    public static Color Accent => RestlessUi.Accent;
    public static UiKitAssetInfo Asset(UiKitAsset role) => UiKitAssets.Get(role);

    /// <summary>Top-left coordinates in canvas units; does not reparent the object.</summary>
    public static void Place(GameObject target, Rect bounds) => RestlessUi.Pin(target,
        new Vector2(0,1), new Vector2(0,1), new Vector2(bounds.x,-bounds.y), bounds.size);

    public static void Surface(GameObject target, bool compact = false, Color? accent = null)
        => RestlessUi.PaperSurface(target, compact, accent);
    public static void ActionSurface(GameObject target) => RestlessUi.ForgedSurface(target, true, true);
    public static void Tab(Button target, bool selected) => RestlessUi.ForgedTab(target.gameObject, selected);
    public static float Quality(Transform parent, int quality, float width) => RestlessUi.QualityMarks(parent, quality, width);
    public static void Selectable(Selectable target) => RestlessUi.PaperSelectable(target);

    public static UiKitProgress Progress(Transform parent, string name = "progress-track") => new(parent,name);

    public static Button Button(Transform parent, string caption, Rect bounds, Action clicked)
    {
        if (clicked == null) throw new ArgumentNullException(nameof(clicked));
        var go = RestlessUi.Chip(parent,"control"); RestlessUi.PaperControl(go); Place(go,bounds);
        var button = go.AddComponent<Button>(); button.targetGraphic = go.GetComponent<Image>(); Selectable(button);
        var label = RestlessUi.Label(go.transform,caption,ControlFontSize,Text,TextAnchor.MiddleCenter);
        RestlessUi.Stretch(label.gameObject,Vector2.zero,Vector2.one,
            new Vector2(ControlInsetX,ControlInsetY),new Vector2(-ControlInsetX,-ControlInsetY));
        RestlessUi.BoundedLabel(label,ControlFontSize,MinimumControlFontSize);
        button.onClick.AddListener(() => clicked()); return button;
    }

    public static InputField Field(Transform parent, string placeholder, Rect bounds, bool number = false, int characterLimit = 128)
    {
        if (characterLimit < 1) throw new ArgumentOutOfRangeException(nameof(characterLimit));
        var go = RestlessUi.Chip(parent,"input"); RestlessUi.PaperControl(go); Place(go,bounds);
        var field = go.AddComponent<InputField>(); field.targetGraphic = go.GetComponent<Image>();
        field.textComponent = RestlessUi.Label(go.transform,"",ControlFontSize,Text,
            number ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft);
        var hint = RestlessUi.Label(go.transform,placeholder,ControlFontSize,Muted,TextAnchor.MiddleLeft);
        foreach (var label in new[] { field.textComponent, hint })
        {
            RestlessUi.Stretch(label.gameObject,Vector2.zero,Vector2.one,new Vector2(10,3),new Vector2(-10,-3));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
        }
        field.placeholder = hint; field.characterLimit = characterLimit;
        field.lineType = InputField.LineType.SingleLine;
        if (number) field.contentType = InputField.ContentType.IntegerNumber;
        Selectable(field); return field;
    }

    /// <summary>Owned vertical reader with the game's scrollbar and Restless wheel motion.
    /// Pass a native prefab explicitly, or let Core use the current inventory prefab.</summary>
    public static RestlessScrollRect Reader(Transform parent, string name, Rect bounds, float rowHeight,
        float rowsPerNotch = 2f, Scrollbar? nativeScrollbar = null)
    {
        if (bounds.width <= ScrollContentGutter || bounds.height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds));
        if (rowHeight <= 0 || rowsPerNotch <= 0) throw new ArgumentOutOfRangeException(nameof(rowHeight));
        var root = RestlessUi.Node(parent,name); Place(root,bounds);
        var scroll = root.AddComponent<RestlessScrollRect>(); scroll.horizontal = false;
        scroll.RowHeight = rowHeight; scroll.RowsPerNotch = rowsPerNotch;
        var view = RestlessUi.Graphic(root.transform,"viewport",Color.clear,true);
        Place(view,new Rect(0,0,bounds.width-ScrollContentGutter,bounds.height)); view.AddComponent<RectMask2D>();
        scroll.viewport = (RectTransform)view.transform;
        var content = RestlessUi.Node(view.transform,"content");
        Place(content,new Rect(0,0,bounds.width-ScrollContentGutter,bounds.height)); scroll.content = (RectTransform)content.transform;
        var template = nativeScrollbar ?? InventoryGui.instance?.m_recipeListScroll;
        if (template != null)
        {
            var bar = UnityEngine.Object.Instantiate(template,root.transform,false);
            bar.onValueChanged = new Scrollbar.ScrollEvent(); bar.transform.localScale = Vector3.one;
            Place(bar.gameObject,new Rect(bounds.width-ScrollbarWidth,0,ScrollbarWidth,bounds.height));
            if (bar.handleRect != null)
            {
                var area = bar.handleRect.parent as RectTransform;
                if (area != null && area != bar.transform)
                    RestlessUi.Stretch(area.gameObject,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
                bar.handleRect.sizeDelta = new Vector2(0,bar.handleRect.sizeDelta.y);
                bar.handleRect.anchoredPosition = new Vector2(0,bar.handleRect.anchoredPosition.y);
            }
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }
        return scroll;
    }

    /// <summary>Resize without resetting position. Clamps only when content no longer fits.</summary>
    public static void ResizeReader(RestlessScrollRect scroll, Rect bounds)
    {
        if (bounds.width <= ScrollContentGutter || bounds.height <= 0) throw new ArgumentOutOfRangeException(nameof(bounds));
        Place(scroll.gameObject,bounds);
        scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,bounds.width-ScrollContentGutter);
        scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,bounds.height);
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,bounds.width-ScrollContentGutter);
        if (scroll.verticalScrollbar != null)
            Place(scroll.verticalScrollbar.gameObject,new Rect(bounds.width-ScrollbarWidth,0,ScrollbarWidth,bounds.height));
        SizeReader(scroll,scroll.content.rect.height);
    }

    public static void SizeReader(RestlessScrollRect scroll, float contentHeight, bool reset = false)
    {
        var height = Mathf.Max(scroll.viewport.rect.height,contentHeight);
        scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
        var y = reset ? 0 : Mathf.Clamp(scroll.content.anchoredPosition.y,0,height-scroll.viewport.rect.height);
        if (reset || !Mathf.Approximately(y,scroll.content.anchoredPosition.y))
        { scroll.CancelWheel(); scroll.content.anchoredPosition = new Vector2(0,y); }
    }
}
