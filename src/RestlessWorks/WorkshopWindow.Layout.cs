using System;
using System.Collections.Generic;
using RestlessQoL.Core;
using RestlessQoL.Api;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessWorks;

internal sealed partial class WorkshopWindow
{
    private void Build()
    {
        var paper=RestlessUi.Tray(transform,"Workshop"); RestlessUi.PaperSurface(paper);
        paper.GetComponent<Image>().raycastTarget=true;
        _sheet=RestlessUi.Pin(paper,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1280,800));
        RestlessUi.PaperCorner(paper);
        Place(RestlessUi.Picture(_sheet,"workshop-mark","craft-hammer"),26,22,52,62);
        Label(_sheet,"Workshop",36,92,22,430,50);
        Label(_sheet,"Set the work. Keep the stores supplied.",17,30,78,530,26);
        var names=new[]{"Production","Orders","Machines","Output"};
        for(var i=0;i<names.Length;i++) { var tab=i; _tabs.Add(Button(_sheet,names[i],590+i*142,34,132,38,()=>SelectTab(tab))); }
        Button(_sheet,"ESC",1174,34,70,38,Close);
        Rule(_sheet,28,112,1220,1); Rule(_sheet,298,132,1,602); Rule(_sheet,894,132,1,602);
        _search=Field(_sheet,"Search…",28,132,252,40,false);
        _search.onValueChanged.AddListener(value=>{_query=value;_stamp="";Refresh(true);Reset(_list);});
        _controls=RestlessUi.Node(_sheet,"production-controls");
        RestlessUi.Stretch(_controls,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        _make=Button(_controls.transform,"Make",28,186,122,36,()=>SetMode(WorksOrderMode.Make));
        _keep=Button(_controls.transform,"Keep",158,186,122,36,()=>SetMode(WorksOrderMode.Keep));
        Button(_controls.transform,"−",28,236,52,38,()=>Quantity(-1));
        _amount=Field(_controls.transform,"30",90,236,128,38,true); _amount.SetTextWithoutNotify("30");
        _amount.onEndEdit.AddListener(value=>{if(int.TryParse(value,out var n))_count=Mathf.Clamp(n,1,999);_amount.SetTextWithoutNotify(_count.ToString());Refresh(true);});
        Button(_controls.transform,"+",228,236,52,38,()=>Quantity(1));
        _modeHelp=Label(_controls.transform,"",17,30,286,246,62);
        _stationFilter=Button(_controls.transform,"All machines",28,350,252,34,CycleStation);
        _list=Reader(_sheet,"selection",28,396,252,338,78);
        _body=Reader(_sheet,"work-plan",318,132,560,602,112);
        _details=Reader(_sheet,"inspector",916,132,328,450,40);
        _detailText=Label(_details.content,"",18,0,0,304,100); _detailText.alignment=TextAnchor.UpperLeft;
        _detailText.verticalOverflow=VerticalWrapMode.Overflow;
        _hint=Label(_sheet,"",17,918,596,324,72); RestlessUi.BoundedLabel(_hint,17,14);
        _secondary=Button(_sheet,"",916,636,328,40,Secondary);
        _action=Button(_sheet,"",916,684,328,50,Act); RestlessUi.ForgedSurface(_action.gameObject,action:true,interactive:true);
        _status=Label(_sheet,"",16,28,752,1216,30); RestlessUi.BoundedLabel(_status,16,13);
        Fit();
    }
    private static void Place(GameObject go,float x,float y,float w,float h)=>RestlessUi.Pin(go,new Vector2(0,1),new Vector2(0,1),new Vector2(x,-y),new Vector2(w,h));
    private static void Portrait(GameObject go,float x,float y,float w,float h)=>RestlessUi.Pin(go,new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(x+w/2,-y-h/2),new Vector2(w,h));
    private static Text Label(Transform p,string value,int size,float x,float y,float w,float h)
    { var t=RestlessUi.Label(p,value,size,RestlessUi.Text,TextAnchor.MiddleLeft); Place(t.gameObject,x,y,w,h);t.horizontalOverflow=HorizontalWrapMode.Wrap;return t; }
    private static void Rule(Transform p,float x,float y,float w,float h)=>Place(RestlessUi.Graphic(p,"rule",new Color(.5f,.43f,.32f,.5f),false),x,y,w,h);
    private static Button Button(Transform p,string title,float x,float y,float w,float h,Action action)
        => UiKitApi.Button(p,title,new Rect(x,y,w,h),action);
    private static InputField Field(Transform p,string placeholder,float x,float y,float w,float h,bool number)
        => UiKitApi.Field(p,placeholder,new Rect(x,y,w,h),number,number ? 3 : 80);
    private static RestlessScrollRect Reader(Transform p,string name,float x,float y,float w,float h,float row)
        => UiKitApi.Reader(p,name,new Rect(x,y,w,h),row);
    private static void ResizeReader(RestlessScrollRect s,float x,float y,float w,float h)
        => UiKitApi.ResizeReader(s,new Rect(x,y,w,h));
    private static void Size(RestlessScrollRect s,float height) => UiKitApi.SizeReader(s,height);
    private static void Reset(RestlessScrollRect s){s.CancelWheel();s.content.anchoredPosition=Vector2.zero;}
    private void Fit()
    {var size=((RectTransform)transform).rect.size;if(size==_canvas)return;_canvas=size;_sheet.localScale=Vector3.one*Mathf.Min(1,Mathf.Max(.1f,(size.x-24)/1280),Mathf.Max(.1f,(size.y-24)/800));}
    private static void Caption(Button b,string caption)=>b.GetComponentInChildren<Text>().text=caption;
    private sealed class Card
    {
        internal readonly GameObject Root;
        private readonly Image _icon;
        private readonly Text _name,_summary;
        private readonly UiKitProgress _progress;
        private Action? _click;
        internal Card(Transform parent)
        {
            Root=RestlessUi.Chip(parent,"work-card");RestlessUi.PaperControl(Root);
            var b=Root.AddComponent<Button>();b.targetGraphic=Root.GetComponent<Image>();RestlessUi.PaperSelectable(b);b.onClick.AddListener(()=>_click?.Invoke());
            _icon=RestlessUi.Graphic(Root.transform,"item",Color.white,false).GetComponent<Image>();_icon.preserveAspect=true;
            _name=Label(Root.transform,"",21,0,0,100,50);RestlessUi.BoundedLabel(_name,21,17);
            _summary=Label(Root.transform,"",17,0,0,100,40);RestlessUi.BoundedLabel(_summary,17,14);
            _progress=UiKitApi.Progress(Root.transform,"coverage-track");
        }
        internal void Set(string name,string summary,Sprite? icon,Action click,bool selected=false,bool compact=false)
        {
            var z=((RectTransform)Root.transform).rect.size;_click=click;
            Portrait(_icon.gameObject,12,compact?12:18,compact?40:62,compact?40:62);_icon.sprite=icon;_icon.enabled=icon!=null;
            var titleX=icon==null?14:compact?62:88;
            Place(_name.gameObject,titleX,8,z.x-titleX-18,compact?46:52);
            if(compact) Place(_summary.gameObject,12,58,z.x-24,26);
            else if(z.y<=116) Place(_summary.gameObject,titleX,62,z.x-titleX-18,z.y-72);
            else Place(_summary.gameObject,12,80,z.x-24,z.y-96);
            _name.text=name;_summary.text=summary;_summary.color=RestlessUi.PaperMuted;_summary.gameObject.SetActive(!string.IsNullOrEmpty(summary));
            _progress.Hide();RestlessUi.PaperControl(Root,selected?RestlessUi.Accent:new Color(.32f,.29f,.24f));
        }
        internal void Coverage(int completed,int running,int target)
        {
            var z=((RectTransform)Root.transform).rect.size;
            var denominator=Math.Max(1,target);var done=Mathf.Clamp01((float)completed/denominator);var total=Mathf.Clamp01(((float)completed+running)/denominator);
            _progress.Set(new Rect(14,z.y-12,z.x-28,5),done,total,UiKitApi.Accent,new Color(.58f,.76f,.56f));
        }
    }
    private static Card Get(List<Card> pool,int index,Transform parent){if(index==pool.Count)pool.Add(new Card(parent));pool[index].Root.SetActive(true);return pool[index];}
    private static void Hide(List<Card> pool,int from){for(var i=from;i<pool.Count;i++)pool[i].Root.SetActive(false);}
}
