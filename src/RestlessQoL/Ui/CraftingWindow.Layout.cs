using System;
using RestlessQoL.Api;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Ui;

internal sealed partial class CraftingWindow
{
    private void Build()
    {
        _frame=new UiKitWindow(transform,"Crafting",new Vector2(1280,800),()=>Close(true));
        var names=new[]{"Craft","Upgrade","Stations"};
        for(var i=0;i<names.Length;i++){var tab=i;_tabs.Add(_frame.Tab(names[i],new Rect(650+i*144,30,132,38),()=>SelectTab(tab)));}
        _search=UiKitApi.Field(_frame.Sheet,"Search…",new Rect(28,130,252,40));
        _search.onValueChanged.AddListener(value=>{_query=value;Refresh();UiKitApi.SizeReader(_browser,_browser.content.rect.height,true);});
        var filters=new[]{"All","Equipment","Other"};
        for(var i=0;i<filters.Length;i++){var filter=filters[i];_filters.Add(UiKitApi.Button(_frame.Sheet,filter,new Rect(28+i*84,178,80,32),()=>{_category=filter;Refresh();UiKitApi.SizeReader(_browser,_browser.content.rect.height,true);}));}
        _browser=UiKitApi.Reader(_frame.Sheet,"craft-browser",new Rect(28,222,252,430),76);
        _heading=UiKitWindow.Label(_frame.Sheet,"",27,new Rect(310,132,560,48));RestlessUi.BoundedLabel(_heading,27,22);
        var portrait=RestlessUi.Graphic(_frame.Sheet,"craft-portrait",Color.white,false);_portrait=portrait.GetComponent<Image>();_portrait.preserveAspect=true;
        UiKitApi.Place(portrait,new Rect(430,188,240,130));
        _detail=UiKitApi.Reader(_frame.Sheet,"craft-details",new Rect(310,328,560,322),42);
        _copy=UiKitWindow.Label(_detail.content,"",18,new Rect(0,0,530,100));_copy.alignment=TextAnchor.UpperLeft;_copy.verticalOverflow=VerticalWrapMode.Overflow;
        _context=UiKitWindow.Label(_frame.Sheet,"Required materials",22,new Rect(906,132,326,36));
        _requirements=UiKitApi.Reader(_frame.Sheet,"craft-requirements",new Rect(906,180,326,268),50);
        _needs=UiKitWindow.Label(_requirements.content,"",18,new Rect(0,0,302,100));_needs.alignment=TextAnchor.UpperLeft;_needs.verticalOverflow=VerticalWrapMode.Overflow;
        _reason=UiKitWindow.Label(_frame.Sheet,"",18,new Rect(906,462,326,64));RestlessUi.BoundedLabel(_reason,18,16);
        _variant=UiKitApi.Button(_frame.Sheet,"Change style",new Rect(906,534,326,36),()=>{
            var item=_choice?.Recipe.m_item.m_itemData;var count=item?.m_shared.m_variants??0;
            if(count>0&&_gui.m_craftTimer<0){_gui.OnVariantSelected((_gui.m_selectedVariant+1)%count);Refresh();}
        });
        _action=UiKitApi.Button(_frame.Sheet,"Craft",new Rect(906,600,326,48),Act);UiKitApi.ActionSurface(_action.gameObject);
        _cancel=UiKitApi.Button(_frame.Sheet,"Your full inventory",new Rect(906,660,326,40),()=>{if(_gui.m_craftTimer>=0){_gui.OnCraftCancelPressed();Refresh();}else Close(false);});
        _progress=UiKitApi.Progress(_frame.Sheet);
        _bag=UiKitWindow.Label(_frame.Sheet,"",17,new Rect(28,670,842,34),true);
        for(var i=0;i<10;i++)
        {
            var slot=UiKitApi.Button(_frame.Sheet,"",new Rect(28+i*82,718,72,64),()=>{});
            var icon=RestlessUi.Graphic(slot.transform,"icon",Color.white,false);icon.GetComponent<Image>().preserveAspect=true;
            UiKitApi.Place(icon,new Rect(8,4,48,42));
            var text=slot.GetComponentInChildren<Text>();UiKitApi.Place(text.gameObject,new Rect(4,45,64,18));text.alignment=TextAnchor.MiddleRight;text.fontSize=14;
            _slots.Add(slot.gameObject);
        }
        Fit();
    }
    private void Fit()
    {
        var size=((RectTransform)transform).rect.size;
        if(size==_canvas)return;_canvas=size;
        _compact=size.x<1150||size.y<750;
        _frame.Resize(_compact?new Vector2(960,680):new Vector2(1280,800));
        for(var i=0;i<_tabs.Count;i++)UiKitApi.Place(_tabs[i].gameObject,new Rect((_compact?430:650)+i*(_compact?130:144),30,_compact?120:132,38));
        UiKitApi.Place(_search.gameObject,new Rect(28,130,_compact?210:252,40));
        for(var i=0;i<_filters.Count;i++)UiKitApi.Place(_filters[i].gameObject,new Rect(28+i*(_compact?70:84),178,_compact?66:80,32));
        UiKitApi.ResizeReader(_browser,new Rect(28,222,_compact?210:252,_compact?326:430));
        UiKitApi.Place(_heading.gameObject,new Rect(_compact?256:310,132,_compact?366:560,48));
        UiKitApi.Place(_portrait.gameObject,new Rect(_compact?340:430,188,_compact?160:240,_compact?92:130));
        UiKitApi.ResizeReader(_detail,new Rect(_compact?256:310,_compact?292:328,_compact?366:560,_compact?256:322));
        UiKitApi.Place(_context.gameObject,new Rect(_compact?638:906,132,_compact?294:326,36));
        UiKitApi.ResizeReader(_requirements,new Rect(_compact?638:906,180,_compact?294:326,_compact?184:268));
        UiKitApi.Place(_reason.gameObject,new Rect(_compact?638:906,_compact?374:462,_compact?294:326,60));
        UiKitApi.Place(_variant.gameObject,new Rect(_compact?638:906,_compact?442:534,_compact?294:326,34));
        UiKitApi.Place(_action.gameObject,new Rect(_compact?638:906,_compact?496:600,_compact?294:326,48));
        UiKitApi.Place(_cancel.gameObject,new Rect(_compact?638:906,_compact?556:660,_compact?294:326,38));
        UiKitApi.Place(_bag.gameObject,new Rect(28,_compact?560:670,_compact?592:842,34));
        for(var i=0;i<_slots.Count;i++)
        {
            UiKitApi.Place(_slots[i],new Rect(28+i*(_compact?58:82),_compact?610:718,_compact?54:72,_compact?58:64));
            UiKitApi.Place(_slots[i].transform.Find("icon").gameObject,new Rect(8,4,_compact?38:48,_compact?36:42));
            UiKitApi.Place(_slots[i].GetComponentInChildren<Text>().gameObject,new Rect(4,_compact?39:45,_compact?46:64,18));
        }
        ResizeCopy();_frame.Fit();
    }
    private void ResizeCopy()
    {
        if(_copy==null)return;
        UiKitApi.Place(_copy.gameObject,new Rect(0,0,_detail.content.rect.width,Mathf.Max(100,_copy.preferredHeight+16)));
        UiKitApi.SizeReader(_detail,_copy.preferredHeight+24);
    }
    private Button Row(int index)
    {
        if(index==_rows.Count)
        {
            var button=UiKitApi.Button(_browser.content,"",new Rect(0,index*76,228,70),()=>{});
            button.GetComponentInChildren<Text>().gameObject.SetActive(false);
            var icon=RestlessUi.Graphic(button.transform,"icon",Color.white,false).GetComponent<Image>();icon.preserveAspect=true;
            UiKitApi.Place(icon.gameObject,new Rect(8,14,38,38));
            _rowNames.Add(UiKitWindow.Label(button.transform,"",18,new Rect(54,6,158,32)));
            _rowStates.Add(UiKitWindow.Label(button.transform,"",14,new Rect(54,39,158,25),true));
            _rowIcons.Add(icon);_rows.Add(button);
        }
        var row=_rows[index];row.gameObject.SetActive(true);
        UiKitApi.Place(row.gameObject,new Rect(0,index*76,_browser.content.rect.width,70));
        UiKitApi.Place(_rowNames[index].gameObject,new Rect(54,6,_browser.content.rect.width-62,32));
        UiKitApi.Place(_rowStates[index].gameObject,new Rect(54,39,_browser.content.rect.width-62,25));
        RestlessUi.BoundedLabel(_rowNames[index],18,16);return row;
    }
    private void FinishRows(int count)
    {
        for(var i=count;i<_rows.Count;i++)_rows[i].gameObject.SetActive(false);
        UiKitApi.SizeReader(_browser,count*76);
        for(var i=0;i<count;i++)
        {
            var navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=i>0?_rows[i-1]:_tabs[_tab],selectOnDown=i+1<count?_rows[i+1]:(_action.interactable?_action:_cancel),selectOnRight=_action.interactable?_action:_cancel};
            _rows[i].navigation=navigation;
        }
    }
}
