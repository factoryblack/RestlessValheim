using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RestlessQoL.Api;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Ui;

internal sealed partial class CraftingWindow
{
    private sealed class StationChoice
    {
        internal string Id="",Name="",State="";
        internal Piece? Piece;
        internal StationExtension? Extension;
        internal bool Connected;
    }
    private float _nextStation;
    private List<StationChoice> _stationChoices=new();
    private ZNetScene? _scene;
    private readonly List<StationExtension> _extensionPrefabs=new();
    private void DiscoverStations()
    {
        _nextStation=Time.unscaledTime+1;
        if(_station==null){_stationChoices.Clear();return;}
        var connected=_station.GetExtensions();var rows=new List<StationChoice>();
        foreach(var ext in StationExtension.m_allExtensions)
        {
            if(ext==null||ext.m_craftingStation==null||ext.m_craftingStation.m_name!=_station.m_name)continue;
            if(!connected.Contains(ext)&&Vector3.Distance(ext.transform.position,_station.transform.position)>Mathf.Max(ext.m_maxStationDistance,15f))continue;
            var piece=ext.GetComponent<Piece>();
            rows.Add(new StationChoice{Id="world:"+ext.GetInstanceID(),Name=CraftingBridge.Local(piece!=null?piece.m_name:ext.GetExtensionName()),Piece=piece,Extension=ext,Connected=connected.Contains(ext),
                State=connected.Contains(ext)?"Connected · contributes to station level":"Nearby · not counted by this station"});
        }
        if(_scene!=ZNetScene.instance)
        {
            _scene=ZNetScene.instance;_extensionPrefabs.Clear();
            if(_scene!=null)foreach(var prefab in _scene.m_prefabs){var extension=prefab!=null?prefab.GetComponent<StationExtension>():null;if(extension!=null)_extensionPrefabs.Add(extension);}
        }
        foreach(var ext in _extensionPrefabs)
        {
            if(ext.m_craftingStation==null||ext.m_craftingStation.m_name!=_station.m_name)continue;
            var piece=ext.GetComponent<Piece>();if(piece==null||!Player.m_localPlayer.HaveRequirements(piece,Player.RequirementMode.IsKnown))continue;
            if(rows.Any(r=>r.Connected&&r.Extension!=null&&Utils.GetPrefabName(r.Extension.gameObject)==Utils.GetPrefabName(ext.gameObject)))continue;
            rows.Add(new StationChoice{Id="build:"+Utils.GetPrefabName(ext.gameObject),Name=CraftingBridge.Local(piece.m_name),Piece=piece,State="Known extension · available to build"});
        }
        _stationChoices=rows;
    }
    private void PaintStations()
    {
        if(Time.unscaledTime>=_nextStation)DiscoverStations();
        _variant.gameObject.SetActive(false);_context.text="Extension requirements";
        var choices=_stationChoices.Where(c=>c.Name.IndexOf(_query,StringComparison.OrdinalIgnoreCase)>=0).ToList();
        if(_extension==null||!choices.Any(c=>c.Id==_extension.Id))_extension=choices.FirstOrDefault();
        else _extension=choices.First(c=>c.Id==_extension.Id);
        for(var i=0;i<choices.Count;i++)
        {
            var choice=choices[i];var row=Row(i);_rowNames[i].text=choice.Name;_rowStates[i].text=choice.State;
            _rowIcons[i].sprite=choice.Piece?.m_icon;_rowIcons[i].enabled=_rowIcons[i].sprite!=null;
            row.onClick.RemoveAllListeners();row.onClick.AddListener(()=>{_extension=choice;PaintStations();UiKitApi.SizeReader(_detail,_detail.content.rect.height,true);});
            row.interactable=true;RestlessUi.PaperControl(row.gameObject,_extension?.Id==choice.Id?UiKitApi.Accent:(Color?)null);
        }
        FinishRows(choices.Count);
        _action.gameObject.SetActive(true);_cancel.gameObject.SetActive(true);Caption(_action,"Select in build menu");
        _heading.text=_extension?.Name??(_station==null?"Hand crafting":"Station extensions");
        _portrait.sprite=_extension?.Piece?.m_icon;_portrait.enabled=_portrait.sprite!=null;
        var explanation=_station==null?"Hand crafting has no station extensions. Open a crafting station to inspect its connections.":
            CraftingBridge.Local(_station.m_name)+" · Level "+_station.GetLevel()+"\n\n"+_station.GetExtensions().Count+" connected extensions counted by the game.\n\n";
        if(_extension!=null)
        {
            explanation+=_extension.State+"\n\n"+CraftingBridge.Local(_extension.Piece?.m_description??"");
            if(_extension.Extension!=null)explanation+="\n\nConnection distance: "+_extension.Extension.m_maxStationDistance.ToString("0.0")+" m. The game's extension list determines whether it contributes.";
            if(!_extension.Connected)explanation+="\n\nPlacement and extension conflicts are checked by the game. Building this piece does not guarantee a new station level.";
        }
        _copy.text=explanation;ResizeCopy();
        var requirements=new List<string>();
        if(_extension?.Piece!=null)foreach(var requirement in _extension.Piece.m_resources)
        {if(requirement.m_resItem!=null)requirements.Add(CraftingBridge.Local(requirement.m_resItem.m_itemData.m_shared.m_name)+" · "+requirement.m_amount+" needed");}
        _needs.text=requirements.Count==0?"Select an extension to inspect its build requirements.":string.Join("\n\n",requirements);
        UiKitApi.Place(_needs.gameObject,new Rect(0,0,_requirements.content.rect.width,Mathf.Max(60,_needs.preferredHeight+16)));UiKitApi.SizeReader(_requirements,_needs.preferredHeight+24);
        var tool=Player.m_localPlayer.GetRightItem();
        _action.interactable=_extension?.Piece!=null&&_extension.Extension==null&&tool?.m_shared.m_buildPieces!=null;
        _reason.text=_extension?.Connected==true?"Connected to this station":_extension?.Extension!=null?"Nearby, but not contributing to this station":_action.interactable?"Select this extension for normal building":"Equip its building tool, then reopen Stations to select it.";
    }
    private void SelectBuildPiece()
    {
        var player=Player.m_localPlayer;var piece=_extension?.Piece;
        if(player==null||piece==null||player.GetRightItem()?.m_shared.m_buildPieces==null)return;
        if(!player.SetSelectedPiece(piece)){_reason.text="This extension is not available in your current build menu.";return;}
        Close(true);
        player.Message(MessageHud.MessageType.Center,"Selected "+CraftingBridge.Local(piece.m_name)+" for building.");
    }
}
