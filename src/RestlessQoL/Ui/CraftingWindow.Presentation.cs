using System;
using System.Collections.Generic;
using System.Linq;
using RestlessQoL.Api;
using UnityEngine;
namespace RestlessQoL.Ui;
internal sealed partial class CraftingWindow
{
    private void HideDetailRows()
    {
        foreach(var row in _attributes)row.Root.SetActive(false);_statHeader.gameObject.SetActive(false);_detailStamp="";
    }
    private void PaintDetails(CraftingDetails.Details details)
    {
        var stamp=_detail.content.rect.width+"|"+details.Description+"|"+string.Join("|",details.Attributes.Select(a=>a.Name+":"+a.Current+":"+a.Next));
        if(stamp==_detailStamp)return;_detailStamp=stamp;
        _copy.text=details.Description;_copy.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,_detail.content.rect.width);
        var proseHeight=string.IsNullOrEmpty(details.Description)?0:_copy.preferredHeight+12;
        UiKitApi.Place(_copy.gameObject,new Rect(0,0,_detail.content.rect.width,proseHeight));
        var compare=details.Attributes.Any(a=>a.Next!=null);
        _statHeader.gameObject.SetActive(details.Attributes.Count>0);_statHeader.text=compare?"ATTRIBUTE          CURRENT → AFTER UPGRADE":"ITEM ATTRIBUTES";
        UiKitApi.Place(_statHeader.gameObject,new Rect(0,proseHeight,_detail.content.rect.width,24));
        var y=proseHeight+(details.Attributes.Count>0?32:0);
        for(var i=0;i<details.Attributes.Count;i++)
        {
            if(i==_attributes.Count)_attributes.Add(new UiKitDetailRow(_detail.content));
            var a=details.Attributes[i];var height=_compact&&compare?76:58;
            var measured=_attributes[i].Set(new Rect(0,y,_detail.content.rect.width,height),a.Name,a.Current,a.Next,a.Changed);y+=measured+6;
        }
        for(var i=details.Attributes.Count;i<_attributes.Count;i++)_attributes[i].Root.SetActive(false);
        UiKitApi.SizeReader(_detail,y+8);
    }
    private void PaintMaterials(List<CraftingBridge.Material> materials)
    {
        var stamp=_tab+"|"+_requirements.content.rect.width+"|"+string.Join("|",materials.Select(m=>m.Name+":"+m.Counts+":"+m.Missing+":"+(m.Icon?.GetInstanceID()??0)));
        if(stamp==_materialStamp)return;_materialStamp=stamp;
        _needs.gameObject.SetActive(materials.Count==0);_needs.text=_tab==2?"Select an extension to inspect its build costs.":"No materials required.";
        for(var i=0;i<materials.Count;i++)
        {
            if(i==_materials.Count)_materials.Add(new UiKitMaterialCard(_requirements.content));
            var m=materials[i];_materials[i].Set(new Rect(0,i*84,_requirements.content.rect.width,76),m.Name,m.Counts,m.Missing?"Missing materials":_tab==2?"Build cost":m.Counts.Contains("/")?"Available / needed":"Required amount",m.Icon,m.Missing);
        }
        for(var i=materials.Count;i<_materials.Count;i++)_materials[i].Root.SetActive(false);
        UiKitApi.SizeReader(_requirements,materials.Count*84);
    }
}
