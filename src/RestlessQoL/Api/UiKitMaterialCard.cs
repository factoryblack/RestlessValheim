using System;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;
namespace RestlessQoL.Api;

/// <summary>Shared ingredient/input card. Callers supply authoritative counts and actions.</summary>
public sealed class UiKitMaterialCard
{
    public GameObject Root { get; }
    private readonly Button _button;
    private readonly Image _icon;
    private readonly Text _name,_counts,_state;
    private Action? _clicked;
    public UiKitMaterialCard(Transform parent)
    {
        _button=UiKitApi.Button(parent,"",new Rect(0,0,280,76),()=>_clicked?.Invoke());Root=_button.gameObject;
        _button.GetComponentInChildren<Text>().gameObject.SetActive(false);
        _icon=RestlessUi.Graphic(Root.transform,"material-icon",Color.white,false).GetComponent<Image>();_icon.preserveAspect=true;
        _name=UiKitWindow.Label(Root.transform,"",18,new Rect());RestlessUi.BoundedLabel(_name,18,15);
        _counts=UiKitWindow.Label(Root.transform,"",17,new Rect());_counts.supportRichText=true;
        _state=UiKitWindow.Label(Root.transform,"",13,new Rect(),true);
    }
    public void Set(Rect bounds,string name,string counts,string state,Sprite? icon,bool missing,Action? clicked=null)
    {
        Root.SetActive(true);UiKitApi.Place(Root,bounds);_clicked=clicked;_button.interactable=clicked!=null;
        // Passive cards retain full contrast; their image never captures pointer input.
        var colours=_button.colors;colours.disabledColor=Color.white;_button.colors=colours;
        _button.targetGraphic.raycastTarget=clicked!=null;
        _icon.sprite=icon;_icon.enabled=icon!=null;
        UiKitApi.Place(_icon.gameObject,new Rect(10,14,44,44));
        var left=icon!=null?66f:14f;var width=bounds.width-left-12;
        UiKitApi.Place(_name.gameObject,new Rect(left,5,width,25));
        UiKitApi.Place(_counts.gameObject,new Rect(left,30,width,23));
        UiKitApi.Place(_state.gameObject,new Rect(left,53,width,18));
        _name.text=name;_counts.text=counts;_counts.color=missing?RestlessUi.HealthTint:UiKitApi.Accent;
        _state.text=state;_state.color=missing?RestlessUi.HealthTint:UiKitApi.Muted;
    }
}

/// <summary>Native attribute values, optionally comparing an upgrade; no stat calculations.</summary>
public sealed class UiKitDetailRow
{
    public GameObject Root { get; }
    private readonly Text _name,_current,_next;
    public UiKitDetailRow(Transform parent)
    {
        Root=RestlessUi.Chip(parent,"attribute-row");UiKitApi.Surface(Root,true);
        _name=UiKitWindow.Label(Root.transform,"",16,new Rect(),true);
        _current=UiKitWindow.Label(Root.transform,"",17,new Rect());_current.supportRichText=true;
        _next=UiKitWindow.Label(Root.transform,"",17,new Rect());_next.supportRichText=true;
    }
    public float Set(Rect bounds,string name,string current,string? next,bool changed)
    {
        Root.SetActive(true);UiKitApi.Place(Root,bounds);
        var labelWidth=bounds.width*.34f;var valueWidth=next==null?bounds.width-labelWidth-24:(bounds.width-labelWidth-32)/2;
        UiKitApi.Place(_name.gameObject,new Rect(12,5,labelWidth-16,bounds.height-10));
        UiKitApi.Place(_current.gameObject,new Rect(labelWidth,5,valueWidth,bounds.height-10));
        UiKitApi.Place(_next.gameObject,new Rect(labelWidth+valueWidth+10,5,valueWidth,bounds.height-10));
        _name.text=name;_name.color=changed?UiKitApi.Accent:UiKitApi.Muted;_current.text=current;_next.text=next==null?"":(changed?"→ ":"")+next;_next.gameObject.SetActive(next!=null);
        _next.color=changed?UiKitApi.Accent:UiKitApi.Muted;
        var height=Mathf.Max(bounds.height,Mathf.Max(_name.preferredHeight,Mathf.Max(_current.preferredHeight,next==null?0:_next.preferredHeight))+12);
        if(height>bounds.height)
        {
            bounds.height=height;UiKitApi.Place(Root,bounds);
            foreach(var text in new[]{_name,_current,_next})text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height-10);
        }
        return height;
    }
}
