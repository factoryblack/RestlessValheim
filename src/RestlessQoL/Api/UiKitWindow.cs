using System;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Api;

/// <summary>Shared window chrome. The caller owns input, content and lifetime.</summary>
public sealed class UiKitWindow
{
    public RectTransform Sheet { get; }
    public Text Title { get; }
    public Text Subtitle { get; }
    private readonly RectTransform _parent;
    private readonly Button _close;
    private Vector2 _size;
    public UiKitWindow(Transform parent, string title, Vector2 size, Action close)
    {
        _parent = (RectTransform)parent;
        var paper = RestlessUi.Tray(parent, "RestlessWindowSheet");
        RestlessUi.PaperSurface(paper); paper.GetComponent<Image>().raycastTarget = true;
        Sheet = RestlessUi.Pin(paper, new Vector2(.5f,.5f), new Vector2(.5f,.5f), Vector2.zero, size);
        RestlessUi.PaperCorner(paper);
        Title = Label(Sheet,title,34,new Rect(28,20,480,48));
        Subtitle = Label(Sheet,"",17,new Rect(30,72,size.x-160,28), true);
        _close = UiKitApi.Button(Sheet,"ESC",new Rect(size.x-100,30,64,38),close);
        var line=RestlessUi.Graphic(Sheet,"header-divider",new Color(.5f,.43f,.32f,.5f),false);
        UiKitApi.Place(line,new Rect(28,108,size.x-56,1));
        _size=size; Fit();
    }
    public Button Tab(string title, Rect bounds, Action select) => UiKitApi.Button(Sheet,title,bounds,select);
    public void Resize(Vector2 size)
    {
        if (_size==size) return;
        _size=size; Sheet.sizeDelta=size;
        UiKitApi.Place(_close.gameObject,new Rect(size.x-100,30,64,38));
        UiKitApi.Place(Subtitle.gameObject,new Rect(30,72,size.x-160,28));
        var line=Sheet.Find("header-divider");
        if(line!=null) UiKitApi.Place(line.gameObject,new Rect(28,108,size.x-56,1));
        Fit();
    }
    public void Fit() => Sheet.localScale=Vector3.one*Mathf.Min(1f,
        Mathf.Max(.1f,(_parent.rect.width-32)/_size.x),Mathf.Max(.1f,(_parent.rect.height-32)/_size.y));
    public static Text Label(Transform parent,string copy,int size,Rect bounds,bool muted=false)
    {
        var label=RestlessUi.Label(parent,copy,size,muted?UiKitApi.Muted:UiKitApi.Text,TextAnchor.MiddleLeft);
        UiKitApi.Place(label.gameObject,bounds);
        label.horizontalOverflow=HorizontalWrapMode.Wrap;
        return label;
    }
}
