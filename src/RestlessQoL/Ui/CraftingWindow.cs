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
    private static CraftingWindow? _current,_cached;
    private CanvasGroup _visibility=null!;
    private bool _built;
    private string _rowStamp="",_bagStamp="",_paintedCategory="";
    private int _paintedTab=-1;
    private CraftingBridge.Choice? _previewChoice;
    private float _nextPreview;
    private int _previewQuality,_previewVariant;
    internal static bool IsOpen=>_current!=null;
    private static int _closedFrame=-1;
    internal static bool BlocksMenu=>IsOpen||Time.frameCount==_closedFrame;
    private InventoryGui _gui=null!;
    private CraftingStation? _station;
    private UiKitWindow _frame=null!;
    private RestlessScrollRect _browser=null!,_detail=null!,_requirements=null!;
    private Text _heading=null!,_copy=null!,_needs=null!,_reason=null!,_bag=null!,_context=null!;
    private Image _portrait=null!;
    private GameObject _feature=null!,_portraitFrame=null!;
    private Text _heroMeta=null!,_heroNote=null!,_statHeader=null!;
    private readonly List<UiKitDetailRow> _attributes=new();
    private readonly List<UiKitMaterialCard> _materials=new();
    private string _detailStamp="",_materialStamp="";
    private Button _sortButton=null!,_savedOnlyButton=null!,_saveButton=null!;
    private int _sort;
    private bool _savedOnly;
    private readonly HashSet<string> _saved=new(StringComparer.Ordinal);
    private Button _action=null!,_cancel=null!,_variant=null!,_repair=null!;
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
        var window=_cached;
        if(window==null)
        {
            var root=RestlessUi.Graphic(GUIManager.CustomGUIFront.transform,"RestlessCraftingWindow",new Color(0,0,0,.45f),true);
            RestlessUi.Stretch(root,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            window=root.AddComponent<CraftingWindow>();
            window._visibility=root.AddComponent<CanvasGroup>();_cached=window;
        }
        window._visibility.alpha=0;window._visibility.interactable=false;window._visibility.blocksRaycasts=false;
        _current=window;window._gui=gui;window._openedFrame=Time.frameCount;window._previousFocus=EventSystem.current?.currentSelectedGameObject;
        window.gameObject.SetActive(true);
        try
        {
            window._station=Player.m_localPlayer.GetCurrentCraftingStation();
            window._tab=gui.InUpradeTab()?1:0;
            gui.UpdateCraftingPanel(false);
            window._choice=null;window._extension=null;
            window._saved.Clear();foreach(var name in ModConfig.CraftingSavedRecipes.Value.Split('|'))if(name.Length>0)window._saved.Add(name);
            window._rowStamp="";window._bagStamp="";window._paintedTab=-1;window._previewChoice=null;window._nextPreview=0;window._nextStation=0;
            window._messageUntil=0;window._canvas=Vector2.zero;window._detailStamp="";window._materialStamp="";
            // Resolve stretched canvas bounds before creating/fitting the sheet. Reveal only the final layout.
            Canvas.ForceUpdateCanvases();
            if(!window._built){window.Build();window._built=true;}
            else{window._search.SetTextWithoutNotify(window._query);window.Fit();}
            window.HideNative();window._blocked=true;GUIManager.BlockInput(true);
            LookHints.SetSuppressed(window,true);window.Refresh();
            window._browser.content.anchoredPosition=new Vector2(0,window._scrollPositions[window._tab]);
            UiKitApi.SizeReader(window._browser,window._browser.content.rect.height);
            UiKitApi.SizeReader(window._detail,window._detail.content.rect.height,true);
            Canvas.ForceUpdateCanvases();
            window._visibility.alpha=1;window._visibility.interactable=true;window._visibility.blocksRaycasts=true;
            EventSystem.current?.SetSelectedGameObject(window._tabs[window._tab].gameObject);
        }
        catch(Exception error){Plugin.Log.LogError("Crafting window failed; restored inventory: "+error);Reset();}
    }
    internal static void Close(bool hideInventory)
    {
        if(_current==null)return;
        var window=_current;_current=null;_closedFrame=Time.frameCount;
        if(window._browser!=null)window._scrollPositions[window._tab]=window._browser.content.anchoredPosition.y;
        window.gameObject.SetActive(false);
        if(hideInventory&&window._gui!=null)window._gui.Hide();
    }
    internal static void Reset()
    {
        Close(false);
        if(_cached!=null)Destroy(_cached.gameObject);
        _cached=null;
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
        _tab=tab;_choice=null;_extension=null;_rowStamp="";_nextPreview=0;_detailStamp="";
        if(tab==0)_gui.OnTabCraftPressed();else if(tab==1)_gui.OnTabUpgradePressed();
        Refresh();_browser.content.anchoredPosition=new Vector2(0,_scrollPositions[tab]);
        UiKitApi.SizeReader(_browser,_browser.content.rect.height);
    }
    private void Refresh()
    {
        _next=Time.unscaledTime+.3f;
        _frame.Subtitle.text=_station==null?"Hand crafting":CraftingBridge.Local(_station.m_name)+" · Level "+_station.GetLevel();
        if(_paintedTab!=_tab||_paintedCategory!=_category)
        {
            for(var i=0;i<_tabs.Count;i++)UiKitApi.Tab(_tabs[i],i==_tab);
            for(var i=0;i<_filters.Count;i++){_filters[i].gameObject.SetActive(_tab!=2);UiKitApi.Tab(_filters[i],_filters[i].GetComponentInChildren<Text>().text==_category);}
            _paintedTab=_tab;_paintedCategory=_category;
        }
        _sortButton.gameObject.SetActive(_tab!=2);_savedOnlyButton.gameObject.SetActive(_tab!=2);
        Caption(_sortButton,_sort==0?"Sort: native":_sort==1?"Sort: available":"Sort: name");
        Caption(_savedOnlyButton,_savedOnly?"Saved only":"All recipes");
        for(var i=0;i<_tabs.Count;i++)_tabs[i].interactable=_gui.m_craftTimer<0;
        if(_tab==2)PaintStations();else PaintRecipes();
        PaintBag();
        _repair.gameObject.SetActive(_gui.m_repairButton!=null&&_gui.m_repairButton.gameObject.activeSelf);
        _repair.interactable=_gui.m_craftTimer<0&&_gui.m_repairButton!=null&&_gui.m_repairButton.interactable;
        Caption(_repair,_repair.interactable?"Repair equipment":"No repairable equipment");
        UpdateNavigation();
        if(Time.unscaledTime<_messageUntil)_reason.text=_message;
    }
    private void PaintRecipes()
    {
        _context.text="Required materials";
        var all=CraftingBridge.Choices(_gui);
        var filtered=all.Where(c=>c.Name.IndexOf(_query,StringComparison.OrdinalIgnoreCase)>=0&&Category(c)&&(!_savedOnly||_saved.Contains(c.Recipe.name)));
        var choices=(_sort==1?filtered.OrderByDescending(c=>c.Available).ThenBy(c=>c.Name):_sort==2?filtered.OrderBy(c=>c.Name):filtered).ToList();
        var nativeChoice=all.FirstOrDefault(c=>CraftingBridge.Selected(_gui,c));
        if(_gui.m_craftTimer>=0)_choice=nativeChoice??_choice;
        else _choice=choices.FirstOrDefault(c=>nativeChoice!=null&&c.Recipe==nativeChoice.Recipe&&ReferenceEquals(c.Item,nativeChoice.Item))
            ??choices.FirstOrDefault(c=>_choice!=null&&c.Recipe==_choice.Recipe&&ReferenceEquals(c.Item,_choice.Item))??choices.FirstOrDefault();
        if(_choice!=null&&!CraftingBridge.Selected(_gui,_choice)&&_gui.m_craftTimer<0)CraftingBridge.Select(_gui,_choice);
        var rowStamp=CraftingBridge.RowsStamp(choices,_choice,_tab,_browser.content.rect.width,_gui.m_craftTimer>=0);
        if(rowStamp!=_rowStamp)
        {
        for(var i=0;i<choices.Count;i++)
        {
            var choice=choices[i];var button=Row(i);
            _rowNames[i].text=choice.Name;_rowStates[i].text=(_saved.Contains(choice.Recipe.name)?"Saved · ":"")+choice.State;_rowIcons[i].sprite=choice.Icon;_rowIcons[i].enabled=choice.Icon!=null;
            button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>{if(CraftingBridge.Select(_gui,choice)){_choice=choice;Refresh();UiKitApi.SizeReader(_detail,_detail.content.rect.height,true);}});
            button.interactable=_gui.m_craftTimer<0;
            RestlessUi.PaperControl(button.gameObject,_choice!=null&&choice.Recipe==_choice.Recipe&&ReferenceEquals(choice.Item,_choice.Item)?UiKitApi.Accent:(Color?)null);
        }
        FinishRows(choices.Count);
            _rowStamp=rowStamp;
        }
        _variant.gameObject.SetActive(_gui.m_variantButton!=null&&_gui.m_variantButton.gameObject.activeSelf&&_tab==0);
        _variant.interactable=_gui.m_craftTimer<0;
        _action.gameObject.SetActive(true);_cancel.gameObject.SetActive(true);
        if(_choice==null)
        {
            _heading.text=choices.Count==0&&all.Count>0?"No matching items":"No available recipes";
            _copy.text=_tab==1?"Your inventory has no equipment eligible for this station's upgrade tab.":"Known recipes appear here when the current station supports them.";
            _previewChoice=null;_nextPreview=0;_saveButton.gameObject.SetActive(false);_portrait.enabled=false;_heroMeta.text="Choose an item";_heroNote.text="";HideDetailRows();PaintMaterials(new List<CraftingBridge.Material>());_needs.text="";_reason.text="Choose a recipe or item";_action.interactable=false;Caption(_action,"Craft");ResizeCopy();return;
        }
        _heading.text=_choice.Name;_portrait.sprite=(_tab==0?_gui.m_recipeIcon?.sprite:null)??_choice.Icon;
        _portrait.rectTransform.localScale=Vector3.one*(_choice.Category=="Ammo"?1.2f:1f);_portrait.enabled=_choice.Icon!=null;
        if(_previewChoice==null||_previewChoice.Recipe!=_choice.Recipe||!ReferenceEquals(_previewChoice.Item,_choice.Item)||_previewQuality!=(_choice.Item?.m_quality??1)||_previewVariant!=_gui.m_selectedVariant||Time.unscaledTime>=_nextPreview)
        {
            PaintDetails(CraftingBridge.Details(_choice));_previewChoice=_choice;_previewQuality=_choice.Item?.m_quality??1;_previewVariant=_gui.m_selectedVariant;_nextPreview=Time.unscaledTime+1f;
        }
        _heroMeta.text=System.Text.RegularExpressions.Regex.Replace(_choice.Category,"([a-z])([A-Z])","$1 $2");
        _heroNote.text=_tab==1?"Quality "+_choice.Item!.m_quality+" → "+(_choice.Item.m_quality+1):"Makes "+_choice.Recipe.m_amount+" · Quality 1";
        Caption(_saveButton,_saved.Contains(_choice.Recipe.name)?"Saved recipe":"Save recipe");_saveButton.gameObject.SetActive(true);
        PaintMaterials(CraftingBridge.Materials(_gui));
        var selected=CraftingBridge.Selected(_gui,_choice);
        _action.interactable=selected&&_gui.m_craftButton.interactable&&_gui.m_craftTimer<0;
        Caption(_action,_tab==1?"Upgrade to quality "+((_choice.Item?.m_quality??0)+1):"Craft "+_choice.Recipe.m_amount);
        var requirement=_gui.m_minStationLevelIcon!=null&&_gui.m_minStationLevelIcon.gameObject.activeSelf&&_gui.m_minStationLevelText!=null?CraftingBridge.Local(_gui.m_minStationLevelText.text):"";
        if(int.TryParse(RestlessUi.Bare(requirement),out var level))requirement="Station level "+level;
        _reason.text=_gui.m_craftTimer>=0?"Crafting in progress":_action.interactable?(requirement.Length>0?"Ready · "+requirement:"Ready"):CraftingBridge.BlockedReason(_gui,_choice);
    }
    private bool Category(CraftingBridge.Choice choice)=>_category=="All"||(_category=="Equipment"?choice.Recipe.m_item.m_itemData.m_shared.m_maxQuality>1:choice.Recipe.m_item.m_itemData.m_shared.m_maxQuality<=1);
    private void Act()
    {
        if(_tab==2){SelectBuildPiece();return;}
        if(_choice==null)return;
        if(!CraftingBridge.Execute(_gui,_choice))Message(CraftingBridge.BlockedReason(_gui,_choice));
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
        if(_tab==1){var eligible=CraftingBridge.Choices(_gui).Where(c=>c.Item!=null).Select(c=>c.Item!).ToList();items=items.Where(i=>eligible.Contains(i)).OrderByDescending(i=>i.m_equipped).ToList();}
        if(_tab==0&&_choice!=null)
        {
            var names=new HashSet<string>(_choice.Recipe.m_resources.Where(r=>r.m_resItem!=null).Select(r=>r.m_resItem.m_itemData.m_shared.m_name));
            items=items.Where(i=>names.Contains(i.m_shared.m_name)).ToList();
        }
        _bag.text=(_tab==1?"Upgradeable equipment · ":_tab==0?"Recipe ingredients in your bag · ":"Your inventory · ")+inventory.GetTotalWeight().ToString("0.0")+" / "+player.GetMaxCarryWeight().ToString("0")+" weight · "+inventory.GetEmptySlots()+" free slots";
        var stamp=new StringBuilder().Append(_tab);
        for(var i=0;i<Math.Min(items.Count,_slots.Count);i++){var item=items[i];stamp.Append('|').Append(item.GetHashCode()).Append(':').Append(item.m_stack).Append(':').Append(item.m_quality).Append(':').Append(ReferenceEquals(_choice?.Item,item));}
        var bagStamp=stamp.ToString();if(bagStamp==_bagStamp)return;_bagStamp=bagStamp;
        for(var i=0;i<_slots.Count;i++)
        {
            var item=i<items.Count?items[i]:null;
            _slots[i].SetActive(item!=null);
            if(item==null)continue;
            var icon=_slots[i].transform.Find("icon").GetComponent<Image>();icon.sprite=item.GetIcon();
            var count=_slots[i].GetComponentInChildren<Text>();count.text=item.m_stack>1?item.m_stack.ToString():"Q"+item.m_quality;
            var button=_slots[i].GetComponent<Button>();button.onClick.RemoveAllListeners();
            // Ingredients provide inspection; only eligible equipment navigates to upgrades.
            if(_tab==1)button.onClick.AddListener(()=>{if(_gui.m_craftTimer>=0)return;var choice=CraftingBridge.Choices(_gui).FirstOrDefault(c=>ReferenceEquals(c.Item,item));if(choice!=null&&CraftingBridge.Select(_gui,choice)){_choice=choice;Refresh();}});
            else button.onClick.AddListener(()=>{if(_gui.m_craftTimer>=0)return;if(_tab==2){Close(false);return;}PaintDetails(CraftingDetails.Read(CraftingBridge.Local(item.m_shared.m_name)+"\n"+CraftingBridge.Local(item.GetTooltip())));_nextPreview=Time.unscaledTime+6;UiKitApi.SizeReader(_detail,_detail.content.rect.height,true);Message("Inspecting "+CraftingBridge.Local(item.m_shared.m_name)+" · recipe selection retained");});
            var hint=_slots[i].GetComponent<RestlessHint>();
            hint.Copy=CraftingBridge.Local(item.m_shared.m_name)+" · "+(item.m_stack>1?item.m_stack+" in this stack":"Quality "+item.m_quality)+"\n"+(_tab==1?"Select to view this equipment's upgrade":_tab==2?"Select to open your full inventory":"Select to inspect this item for six seconds");
            RestlessUi.PaperControl(_slots[i],ReferenceEquals(_choice?.Item,item)?UiKitApi.Accent:(Color?)null);
        }
    }
    private void Message(string text){_message=text;_messageUntil=Time.unscaledTime+5;}
    internal sealed class CraftResult
    {
        internal string Name="";internal bool Upgrade;
        internal Dictionary<int,int> Before=new();
    }
    internal static CraftResult? BeforeCraft(InventoryGui gui)
    {
        if(_current==null||_current._gui!=gui||gui.m_craftRecipe==null||Player.m_localPlayer==null)return null;
        var name=gui.m_craftRecipe.m_item.m_itemData.m_shared.m_name;
        return new CraftResult{Name=name,Upgrade=gui.m_craftUpgradeItem!=null,Before=OwnedQualities(name)};
    }
    private static Dictionary<int,int> OwnedQualities(string name)=>Player.m_localPlayer.GetInventory().GetAllItems().Where(i=>i.m_shared.m_name==name).GroupBy(i=>i.m_quality).ToDictionary(g=>g.Key,g=>g.Sum(i=>i.m_stack));
    internal static void AfterCraft(InventoryGui gui,CraftResult? result)
    {
        if(result==null||_current==null||_current._gui!=gui||Player.m_localPlayer==null)return;
        var after=OwnedQualities(result.Name);
        _current.Message(CraftingBridge.ResultText(result.Name,result.Upgrade,result.Before,after));
        _current._next=0;_current._nextPreview=0;_current._bagStamp="";
    }
    private void Repair()
    {
        _gui.UpdateRepair();
        if(_gui.m_craftTimer>=0||!_gui.m_repairButton.gameObject.activeSelf||!_gui.m_repairButton.interactable)return;
        // Native action repairs one eligible item and supplies its own result message/effects.
        _gui.OnRepairPressed();_bagStamp="";_nextPreview=0;Refresh();
        EventSystem.current?.SetSelectedGameObject(_repair.interactable?_repair.gameObject:_tabs[_tab].gameObject);
    }
    private void UpdateNavigation()
    {
        // Recompute after eligibility changes so focus cannot follow a stale disabled action.
        var first=_rows.FirstOrDefault(r=>r.gameObject.activeSelf&&r.interactable);
        var slots=_slots.Where(s=>s.activeSelf).Select(s=>s.GetComponent<Button>()).ToList();
        var firstSlot=slots.FirstOrDefault();
        var action=_action.gameObject.activeSelf&&_action.interactable?_action:_cancel;
        for(var i=0;i<_tabs.Count;i++)_tabs[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i>0?_tabs[i-1]:null,selectOnRight=i+1<_tabs.Count?_tabs[i+1]:null,selectOnDown=first??action};
        var tabNav=_tabs[0].navigation;tabNav.selectOnDown=_search;_tabs[0].navigation=tabNav;
        _search.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_tabs[_tab],selectOnDown=_tab==2?first??action:_filters[0],selectOnRight=_saveButton.gameObject.activeSelf?_saveButton:action};
        for(var i=0;i<_filters.Count;i++)_filters[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_search,selectOnLeft=i>0?_filters[i-1]:null,selectOnRight=i+1<_filters.Count?_filters[i+1]:action,selectOnDown=_sortButton};
        _sortButton.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_filters[0],selectOnRight=_savedOnlyButton,selectOnDown=first??action};
        _savedOnlyButton.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_filters[2],selectOnLeft=_sortButton,selectOnRight=action,selectOnDown=first??action};
        _saveButton.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_tabs[_tab],selectOnLeft=(Selectable?)first??_search,selectOnRight=action,selectOnDown=action};
        _variant.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_saveButton.gameObject.activeSelf?_saveButton:_tabs[_tab],selectOnDown=action,selectOnLeft=(Selectable?)first??_search};
        var active=_rows.Where(r=>r.gameObject.activeSelf&&r.interactable).ToList();
        for(var i=0;i<active.Count;i++)active[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=i>0?active[i-1]:_tabs[_tab],selectOnDown=i+1<active.Count?active[i+1]:action,selectOnRight=action};
        _action.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=first??_tabs[_tab],selectOnUp=_variant.gameObject.activeSelf&&_variant.interactable?_variant:_tabs[_tab],selectOnDown=_cancel};
        _cancel.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=first??_tabs[_tab],selectOnUp=_action.interactable?_action:_tabs[_tab],selectOnDown=_repair.gameObject.activeSelf&&_repair.interactable?_repair:firstSlot??_tabs[_tab]};
        _repair.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=first??_tabs[_tab],selectOnUp=_cancel,selectOnDown=firstSlot??_tabs[_tab]};
        for(var i=0;i<slots.Count;i++)slots[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=_cancel,selectOnDown=_tabs[_tab],selectOnLeft=i>0?slots[i-1]:_tabs[_tab],selectOnRight=i+1<slots.Count?slots[i+1]:_cancel};
        var focused=EventSystem.current?.currentSelectedGameObject;
        if(focused!=null&&focused.transform.IsChildOf(transform)&&(!focused.activeInHierarchy||focused.GetComponent<Selectable>() is {interactable:false}))EventSystem.current?.SetSelectedGameObject(_tabs[_tab].gameObject);
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
