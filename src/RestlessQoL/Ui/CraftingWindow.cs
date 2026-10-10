using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jotunn.Managers;
using RestlessQoL.Api;
using RestlessQoL.Core;
using RestlessQoL.HudTweaks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RestlessQoL.Ui;

internal sealed partial class CraftingWindow : MonoBehaviour
{
    private static CraftingWindow? _current;
    internal static bool IsOpen=>_current!=null;
    private static int _closedFrame=-1;
    internal static bool BlocksMenu=>IsOpen||Time.frameCount==_closedFrame;
    private InventoryGui _gui=null!;
    private CraftingStation? _station;
    private UiKitWindow _frame=null!;
    private RestlessScrollRect _browser=null!,_detail=null!,_requirements=null!;
    private Text _heading=null!,_copy=null!,_needs=null!,_reason=null!,_bag=null!,_context=null!;
    private Image _portrait=null!;
    private Button _action=null!,_cancel=null!,_variant=null!;
    private readonly List<Button> _tabs=new(),_rows=new(),_filters=new();
    private InputField _search=null!;
    private readonly List<GameObject> _slots=new();
    private readonly List<Text> _rowNames=new(),_rowStates=new();
    private readonly List<Image> _rowIcons=new();
    private CanvasGroup? _native;
    private bool _ownsNative,_blocked;
    private float _nativeAlpha,_next;
    private bool _nativeInteractable,_nativeRaycasts;
    private int _tab;
    private string _query="",_category="All";
    private CraftingBridge.Choice? _choice;
    private StationChoice? _extension;
    private GameObject? _previousFocus;
    private int _openedFrame;
    private string _message="";
    private float _messageUntil;
    private Vector2 _canvas;
    private bool _compact;
    private readonly float[] _scrollPositions=new float[3];
    private UiKitProgress _progress=null!;
    internal static void Open(InventoryGui gui)
    {
        if(!ModConfig.CraftingWindowEnabled.Value||GUIManager.CustomGUIFront==null||SettingsUi.IsOpen||Player.m_localPlayer==null)return;
        if(_current!=null)return;
        var root=RestlessUi.Graphic(GUIManager.CustomGUIFront.transform,"RestlessCraftingWindow",new Color(0,0,0,.45f),true);
        RestlessUi.Stretch(root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        var window=root.AddComponent<CraftingWindow>();_current=window;window._gui=gui;window._openedFrame=Time.frameCount;window._previousFocus=EventSystem.current?.currentSelectedGameObject;
        try
        {
            window._station=Player.m_localPlayer.GetCurrentCraftingStation();
            window._tab=gui.InUpradeTab()?1:0;
            gui.UpdateCraftingPanel(true);
            window.Build();window.HideNative();window._blocked=true;GUIManager.BlockInput(true);
            LookHints.SetSuppressed(window,true);window.Refresh();
            EventSystem.current?.SetSelectedGameObject(window._tabs[window._tab].gameObject);
        }
        catch(Exception error){Plugin.Log.LogError("Crafting window failed; restored inventory: "+error);Close(false);}
    }
    internal static void Close(bool hideInventory)
    {
        if(_current==null)return;
        var window=_current;_current=null;_closedFrame=Time.frameCount;
        window.gameObject.SetActive(false);Destroy(window.gameObject);
        if(hideInventory&&window._gui!=null)window._gui.Hide();
    }
    private void HideNative()
    {
        _native=_gui.m_inventoryRoot.GetComponent<CanvasGroup>();_ownsNative=_native==null;
        if(_native==null)_native=_gui.m_inventoryRoot.gameObject.AddComponent<CanvasGroup>();
        _nativeAlpha=_native.alpha;_nativeInteractable=_native.interactable;_nativeRaycasts=_native.blocksRaycasts;
        _native.alpha=0;_native.interactable=false;_native.blocksRaycasts=false;
    }
    private void OnDisable()
    {
        LookHints.SetSuppressed(this,false);
        if(_native!=null)
        {
            if(_ownsNative)Destroy(_native);
            else{_native.alpha=_nativeAlpha;_native.interactable=_nativeInteractable;_native.blocksRaycasts=_nativeRaycasts;}
        }
        if(_blocked&&!SettingsUi.IsOpen)GUIManager.BlockInput(false);_blocked=false;
        if(_previousFocus!=null&&_previousFocus.activeInHierarchy)EventSystem.current?.SetSelectedGameObject(_previousFocus);
        if(_current==this)_current=null;
    }
    private void LateUpdate()
    {
        var player=Player.m_localPlayer;
        if(Time.frameCount-_openedFrame<2)return;
        if(!ModConfig.CraftingWindowEnabled.Value||_gui==null||!InventoryGui.IsVisible()||player==null||player.IsDead()||SettingsUi.IsOpen||Menu.IsVisible())
        {Close(false);return;}
        if(Input.GetKeyDown(KeyCode.Escape)||ZInput.GetButtonDown("JoyButtonB")){Close(true);return;}
        var station=player.GetCurrentCraftingStation();
        if(_station!=null&&(station!=_station||!Storage.StorageAccess.Allows(_station.transform.position,player.GetPlayerID())
            ||Vector3.Distance(player.transform.position,_station.transform.position)>_station.m_useDistance))
        {Close(true);return;}
        Fit();
        if(Time.unscaledTime>=_next)
        {
            try{Refresh();}catch(Exception e){Plugin.Log.LogError("Crafting refresh failed; restoring inventory: "+e);Close(false);}
        }
        PaintProgress();EnsureFocusedVisible();
    }
    private void SelectTab(int tab)
    {
        if(_gui.m_craftTimer>=0)return;
        _scrollPositions[_tab]=_browser.content.anchoredPosition.y;
        _tab=tab;_choice=null;_extension=null;
        if(tab==0)_gui.OnTabCraftPressed();else if(tab==1)_gui.OnTabUpgradePressed();
        Refresh();_browser.content.anchoredPosition=new Vector2(0,_scrollPositions[tab]);
        UiKitApi.SizeReader(_browser,_browser.content.rect.height);
    }
    private void Refresh()
    {
        _next=Time.unscaledTime+.3f;
        _frame.Subtitle.text=_station==null?"Hand crafting":CraftingBridge.Local(_station.m_name)+" · Level "+_station.GetLevel();
        for(var i=0;i<_tabs.Count;i++){UiKitApi.Tab(_tabs[i],i==_tab);_tabs[i].interactable=_gui.m_craftTimer<0;}
        for(var i=0;i<_filters.Count;i++){_filters[i].gameObject.SetActive(_tab!=2);UiKitApi.Tab(_filters[i],_filters[i].GetComponentInChildren<Text>().text==_category);}
        if(_tab==2)PaintStations();else PaintRecipes();
        PaintBag();
        if(Time.unscaledTime<_messageUntil)_reason.text=_message;
    }
    private void PaintRecipes()
    {
        _context.text="Required materials";
        var all=CraftingBridge.Choices(_gui);
        var choices=all.Where(c=>c.Name.IndexOf(_query,StringComparison.OrdinalIgnoreCase)>=0&&Category(c)).ToList();
        var nativeChoice=all.FirstOrDefault(c=>CraftingBridge.Selected(_gui,c));
        if(nativeChoice!=null)_choice=nativeChoice;
        else if(_choice==null||!all.Any(c=>c.Recipe==_choice.Recipe&&ReferenceEquals(c.Item,_choice.Item)))_choice=choices.FirstOrDefault();
        if(_choice!=null&&!CraftingBridge.Selected(_gui,_choice)&&_gui.m_craftTimer<0)CraftingBridge.Select(_gui,_choice);
        for(var i=0;i<choices.Count;i++)
        {
            var choice=choices[i];var button=Row(i);
            _rowNames[i].text=choice.Name;_rowStates[i].text=choice.State;_rowIcons[i].sprite=choice.Icon;_rowIcons[i].enabled=choice.Icon!=null;
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(CraftingBridge.Select(_gui,choice)){_choice=choice;Refresh();UiKitApi.SizeReader(_detail,_detail.content.rect.height,true);}});
            button.interactable=_gui.m_craftTimer<0;
            RestlessUi.PaperControl(button.gameObject,_choice!=null&&choice.Recipe==_choice.Recipe&&ReferenceEquals(choice.Item,_choice.Item)?UiKitApi.Accent:(Color?)null);
        }
        FinishRows(choices.Count);
        _variant.gameObject.SetActive(_gui.m_variantButton!=null&&_gui.m_variantButton.gameObject.activeSelf&&_tab==0);
        _variant.interactable=_gui.m_craftTimer<0;
        _action.gameObject.SetActive(true);_cancel.gameObject.SetActive(true);
        if(_choice==null)
        {
            _heading.text=choices.Count==0&&all.Count>0?"No matching items":"No available recipes";
            _copy.text=_tab==1?"Your inventory has no equipment eligible for this station's upgrade tab.":"Known recipes appear here when the current station supports them.";
            _portrait.enabled=false;_needs.text="";_reason.text="Choose a recipe or item";_action.interactable=false;Caption(_action,"Craft");ResizeCopy();return;
        }
        _heading.text=_choice.Name;_portrait.sprite=_choice.Icon;_portrait.enabled=_choice.Icon!=null;
        var preview=CraftingBridge.Preview(_choice);
        if(_copy.text!=preview){_copy.text=preview;ResizeCopy();}
        _needs.text=CraftingBridge.Requirements(_gui);
        UiKitApi.Place(_needs.gameObject,new Rect(0,0,_requirements.content.rect.width,Mathf.Max(60,_needs.preferredHeight+16)));
        UiKitApi.SizeReader(_requirements,_needs.preferredHeight+24);
        var selected=CraftingBridge.Selected(_gui,_choice);
        _action.interactable=selected&&_gui.m_craftButton.interactable&&_gui.m_craftTimer<0;
        Caption(_action,_tab==1?"Upgrade to quality "+((_choice.Item?.m_quality??0)+1):"Craft "+_choice.Recipe.m_amount);
        var requirement=_gui.m_minStationLevelText!=null&&_gui.m_minStationLevelText.gameObject.activeSelf?CraftingBridge.Local(_gui.m_minStationLevelText.text):"";
        _reason.text=_gui.m_craftTimer>=0?"Crafting in progress":_action.interactable?"Ready · "+requirement:"Requirements missing · "+requirement+"\nCheck materials, station access and inventory space.";
    }
    private bool Category(CraftingBridge.Choice choice)=>_category=="All"||(_category=="Equipment"?choice.Recipe.m_item.m_itemData.m_shared.m_maxQuality>1:choice.Recipe.m_item.m_itemData.m_shared.m_maxQuality<=1);
    private void Act()
    {
        if(_tab==2){SelectBuildPiece();return;}
        if(_choice==null)return;
        if(!CraftingBridge.Execute(_gui,_choice)){_message="Craft could not start. Check requirements and inventory space.";_messageUntil=Time.unscaledTime+5;}
        Refresh();
    }
    private void PaintProgress()
    {
        var running=_gui.m_craftTimer>=0;
        _cancel.interactable=true;Caption(_cancel,running?"Cancel craft":"Your full inventory");
        if(running)
        {
            var fraction=_gui.m_craftProgressBar!=null?Mathf.Clamp01(_gui.m_craftProgressBar.m_value / Mathf.Max(.001f,_gui.m_craftProgressBar.m_maxValue)):0;
            _progress.Set(new Rect(_compact?638:906,_compact?478:578,_compact?298:326,8),0,fraction,UiKitApi.Accent,UiKitApi.Accent);
        }
        else _progress.Hide();
    }
    private void PaintBag()
    {
        var player=Player.m_localPlayer;if(player==null)return;
        var inventory=player.GetInventory();var items=inventory.GetAllItems();
        _bag.text="Your inventory · "+inventory.GetTotalWeight().ToString("0.0")+" / "+player.GetMaxCarryWeight().ToString("0")+" weight · "+inventory.GetEmptySlots()+" free slots";
        for(var i=0;i<_slots.Count;i++)
        {
            var item=i<items.Count?items[i]:null;
            _slots[i].SetActive(item!=null);
            if(item==null)continue;
            var icon=_slots[i].transform.Find("icon").GetComponent<Image>();icon.sprite=item.GetIcon();
            var count=_slots[i].GetComponentInChildren<Text>();count.text=item.m_stack>1?item.m_stack.ToString():"Q"+item.m_quality;
            var button=_slots[i].GetComponent<Button>();button.onClick.RemoveAllListeners();
            button.onClick.AddListener(()=>{if(_gui.m_craftTimer>=0)return;SelectTab(1);var choice=CraftingBridge.Choices(_gui).FirstOrDefault(c=>ReferenceEquals(c.Item,item));if(choice!=null&&CraftingBridge.Select(_gui,choice)){_choice=choice;Refresh();}});
            RestlessUi.PaperControl(_slots[i],ReferenceEquals(_choice?.Item,item)?UiKitApi.Accent:(Color?)null);
        }
    }
    private void EnsureFocusedVisible()
    {
        var current=EventSystem.current?.currentSelectedGameObject;
        if(current==null)return;
        var index=_rows.FindIndex(b=>b.gameObject==current);
        if(index<0)return;
        if(!_rows[index].gameObject.activeInHierarchy){EventSystem.current?.SetSelectedGameObject(_tabs[_tab].gameObject);return;}
        var y=index*76f;var position=_browser.content.anchoredPosition.y;
        if(y<position)position=y;else if(y+72>position+_browser.viewport.rect.height)position=y+72-_browser.viewport.rect.height;
        if(!Mathf.Approximately(position,_browser.content.anchoredPosition.y)){_browser.CancelWheel();_browser.content.anchoredPosition=new Vector2(0,position);}
    }
    private static void Caption(Button button,string caption)=>button.GetComponentInChildren<Text>().text=caption;
}
