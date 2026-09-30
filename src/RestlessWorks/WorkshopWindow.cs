using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using RestlessQoL.HudTweaks;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessWorks;

internal sealed partial class WorkshopWindow : MonoBehaviour
{
    private static WorkshopWindow? _current;
    private static int _closedFrame=-1;
    private Piece _board=null!;
    private RectTransform _sheet=null!;
    private RestlessScrollRect _list=null!,_body=null!,_details=null!;
    private readonly List<Card> _choices=new(),_cards=new();
    private readonly List<Button> _tabs=new();
    private readonly Dictionary<string,Sprite?> _icons=new();
    private GameObject _controls=null!;
    private InputField _search=null!,_amount=null!;
    private Text _modeHelp=null!,_detailText=null!,_hint=null!,_status=null!;
    private Button _make=null!,_keep=null!,_stationFilter=null!,_action=null!,_secondary=null!;
    private IReadOnlyList<WorksRecipe> _recipes=Array.Empty<WorksRecipe>();
    private IReadOnlyList<WorksOrder> _orders=Array.Empty<WorksOrder>();
    private IReadOnlyList<WorksMachine> _machines=Array.Empty<WorksMachine>();
    private IReadOnlyList<WorksStock> _stock=Array.Empty<WorksStock>();
    private IReadOnlyList<WorksStep> _plan=Array.Empty<WorksStep>();
    private WorksOrderMode _mode=WorksOrderMode.Make;
    private int _tab,_count=30,_order,_machine;
    private string _output="",_stockItem="",_query="",_station="",_stamp="",_lastError="";
    private bool _blocked,_confirmCancel;
    private float _next,_messageUntil;
    private Vector2 _canvas;
    internal static void Open(Piece board)
    {
        if(!Plugin.BoardEnabled.Value||board==null||GUIManager.CustomGUIFront==null||SettingsUi.IsOpen||InventoryGui.IsVisible())return;
        Close();
        var go=RestlessUi.Graphic(GUIManager.CustomGUIFront.transform,"RestlessWorkshop",new Color(0,0,0,.28f),true);
        RestlessUi.Stretch(go,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var w=go.AddComponent<WorkshopWindow>();_current=w;w._board=board;
        try {w.Build();LookHints.SetSuppressed(w,true);w._blocked=true;GUIManager.BlockInput(true);w.Refresh(true);}
        catch(Exception e){Plugin.Log.LogError(e);Close();}
    }
    internal static void Close()
    {
        if(_current==null)return;
        var w=_current;_current=null;_closedFrame=Time.frameCount;w.gameObject.SetActive(false);Destroy(w.gameObject);
    }
    private void OnDisable()
    {LookHints.SetSuppressed(this,false);if(_current==this)_current=null;if(_blocked&&!SettingsUi.IsOpen)GUIManager.BlockInput(false);_blocked=false;}
    private void Update()
    {
        var player=Player.m_localPlayer;
        if(Input.GetKeyDown(KeyCode.Escape)||!Plugin.BoardEnabled.Value||_board==null||player==null||player.IsDead()
            ||SettingsUi.IsOpen||InventoryGui.IsVisible()||(_board.transform.position-player.transform.position).sqrMagnitude>25f){Close();return;}
        Fit();if(Time.unscaledTime>=_next)Refresh(false);
    }
    private void SelectTab(int tab)
    {_tab=tab;_confirmCancel=false;_query="";_search.SetTextWithoutNotify("");Refresh(true);Reset(_list);Reset(_body);Reset(_details);}
    private void SetMode(WorksOrderMode mode){_mode=mode;Refresh(true);}
    private void Quantity(int delta){_count=Mathf.Clamp(_count+delta,1,999);_amount.SetTextWithoutNotify(_count.ToString());Refresh(true);}
    private void CycleStation()
    {
        var stations=_recipes.GroupBy(r=>r.Station).Select(g=>g.First()).OrderBy(r=>r.StationName).ToList();
        var i=stations.FindIndex(r=>r.Station==_station);_station=i+1<stations.Count?stations[i+1].Station:"";Refresh(true);Reset(_list);
    }
    private bool Control()=>_board!=null&&_board.GetComponent<ZNetView>() is { } v&&v.IsValid()&&v.IsOwner();
    private bool Matches(string name)=>string.IsNullOrWhiteSpace(_query)||Local(name).IndexOf(_query.Trim(),StringComparison.CurrentCultureIgnoreCase)>=0;
    private void Refresh(bool force)
    {
        _next=Time.unscaledTime+1f;
        try
        {
            _recipes=Works.Recipes();_orders=Works.Orders(_board);_machines=Works.Machines(_board);
            if(_tab==3)_stock=Works.Pantry(_board);
            if(_tab==0)
            {
                var visible=_recipes.Where(r=>Matches(r.OutputName)&&(_station.Length==0||r.Station==_station)).ToList();
                if(!visible.Any(r=>r.Output==_output))_output=visible.FirstOrDefault()?.Output??"";
            }
            if(_tab==1&&!_orders.Any(o=>o.Id==_order&&Matches(o.Name))){_order=_orders.FirstOrDefault(o=>Matches(o.Name))?.Id??0;_confirmCancel=false;}
            if(_tab==2)_machine=Mathf.Clamp(_machine,0,Math.Max(0,_machines.Count-1));
            if(_tab==3&&!_stock.Any(s=>s.Prefab==_stockItem&&Matches(s.Name)))_stockItem=_stock.FirstOrDefault(s=>Matches(s.Name))?.Prefab??"";
            if(_tab==0)_plan=_output.Length==0?Array.Empty<WorksStep>():Works.Plan(_board,_output,_count);
            var stamp=new StringBuilder().Append(_tab).Append(_mode).Append(_count).Append(_query).Append(_station).Append(_output).Append(_order).Append(_machine).Append(_stockItem).Append(Control()).Append(_confirmCancel);
            foreach(var r in _recipes)stamp.Append('|').Append(r.Output);
            foreach(var o in _orders)stamp.Append('|').Append(o.Id).Append(':').Append(o.Count).Append(':').Append(o.Mode).Append(':').Append(o.Ready).Append(':').Append(o.Collected).Append(':').Append(o.Available).Append(':').Append(o.InProduction).Append(':').Append(o.Remaining);
            foreach(var m in _machines)stamp.Append('|').Append(m.Prefab).Append(m.Position).Append(':').Append(m.Fuel).Append(':').Append(m.Free).Append(':').Append(m.Cooking).Append(':').Append(m.Ready).Append(m.Block);
            if(_tab==0)foreach(var s in _plan){stamp.Append('|').Append(s.Output).Append(':').Append(s.Available).Append(':').Append(s.Need);foreach(var u in s.Uses)stamp.Append(u.Item).Append(':').Append(u.Available).Append(':').Append(u.Amount);}
            if(_tab==3)foreach(var s in _stock)stamp.Append('|').Append(s.Prefab).Append(':').Append(s.Count).Append(':').Append(s.Reserved);
            var key=stamp.ToString();
            if(force||key!=_stamp)
            {
                _stamp=key;
                for(var i=0;i<_tabs.Count;i++)RestlessUi.PaperControl(_tabs[i].gameObject,i==_tab?RestlessUi.Accent:(Color?)null);
                _controls.SetActive(_tab==0);ResizeReader(_list,28,_tab==0?396:190,252,_tab==0?338:544);
                _secondary.gameObject.SetActive(false);_action.gameObject.SetActive(true);
                Place(_hint.gameObject,918,596,324,_tab==1?36:72);
                if(_tab==0)PaintProduction();else if(_tab==1)PaintOrders();else if(_tab==2)PaintMachines();else PaintOutput();
                if(!Control()){_action.interactable=false;_secondary.interactable=false;_hint.text="Board control changed. Reopen it to manage orders.";}
            }
            if(Time.unscaledTime>=_messageUntil)_status.text="Finished output stays on this board · "+_orders.Count+" / 8 orders · "+_machines.Count+" machines in range";
            _lastError="";
        }
        catch(Exception e)
        {
            if(_lastError!=e.Message){Plugin.Log.LogError(e);_lastError=e.Message;}
            _action.interactable=false;_secondary.interactable=false;Message("Workshop data unavailable. Close and reopen the board.");
        }
    }
    private void Detail(string text)
    {_detailText.text=text;var height=Mathf.Max(60,_detailText.preferredHeight);Place(_detailText.gameObject,0,0,304,height);Size(_details,height+16);}
    private void Message(string text){_status.text=text;_messageUntil=Time.unscaledTime+5f;}
    private static string Local(string text)=>Localization.instance==null?text:Localization.instance.Localize(text);
    private Sprite? Icon(string prefab)
    {
        if(_icons.TryGetValue(prefab,out var icon))return icon;
        var icons=ObjectDB.instance?.GetItemPrefab(prefab)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_icons;
        icon=icons!=null&&icons.Length>0?icons[0]:null;_icons[prefab]=icon;return icon;
    }
    private void PickOutput(string output)
    {_output=output;_station="";_query="";_search.SetTextWithoutNotify("");_tab=0;Refresh(true);Reset(_body);Reset(_details);}
    private void Act()
    {
        if(!_action.interactable||!Control())return;
        try
        {
            if(_tab==0)
            {
                if(_amount.isFocused&&int.TryParse(_amount.text,out var n)){_count=Mathf.Clamp(n,1,999);_amount.SetTextWithoutNotify(_count.ToString());}
                var before=Works.Orders(_board);
                if(_mode==WorksOrderMode.Keep&&before.Any(o=>o.Output==_output&&o.Mode==WorksOrderMode.Keep)){Message("A Keep target already exists for this item. Manage it in Orders.");Refresh(true);return;}
                var ids=new HashSet<int>(before.Select(o=>o.Id));
                if(!Works.Place(_board,_output,_mode,_count)){Message("Order could not be placed. Check the board and queue capacity.");return;}
                _order=Works.Orders(_board).FirstOrDefault(o=>!ids.Contains(o.Id))?.Id??0;SelectTab(1);Message("Order placed. Machines keep working after this window closes.");
            }
            else if(_tab==1)
            {
                if(!_confirmCancel){_confirmCancel=true;Refresh(true);Message("Remove this order? Material already in machines keeps processing.");return;}
                Works.Cancel(_board,_order);_confirmCancel=false;Refresh(true);Message("Order removed. Finished stock can be taken from Output.");
            }
            else if(_tab==3)
            {
                var item=Works.Pantry(_board).FirstOrDefault(s=>s.Prefab==_stockItem);if(item==null)return;
                var stack=ObjectDB.instance?.GetItemPrefab(item.Prefab)?.GetComponent<ItemDrop>()?.m_itemData?.m_shared?.m_maxStackSize??1;
                var taken=Works.Take(_board,item.Prefab,Math.Min(stack,Math.Max(0,item.Count-item.Reserved)));Refresh(true);Message(taken>0?"Collected "+taken+" "+Local(item.Name):"Nothing collected. Check inventory space and reservations.");
            }
        }
        catch(Exception e){Plugin.Log.LogError(e);Message("Action failed. Check Orders before retrying.");}
    }
    private void Secondary()
    {
        if(!_secondary.interactable||!Control())return;
        try {var n=Works.Collect(_board,_order);Refresh(true);Message(n>0?"Collected "+n+". Any remaining output stays on the board.":"Nothing collected. Check inventory space and order ownership.");}
        catch(Exception e){Plugin.Log.LogError(e);Message("Collection failed. Check your inventory.");}
    }
    [HarmonyPatch(typeof(Menu),"Update")]
    private static class MenuInput { private static bool Prefix()=>_current==null&&Time.frameCount!=_closedFrame; }
}
