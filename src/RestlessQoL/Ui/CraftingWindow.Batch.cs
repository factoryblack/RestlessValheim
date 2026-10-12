using System;
using System.Linq;
using RestlessQoL.Api;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;
namespace RestlessQoL.Ui;
internal sealed partial class CraftingWindow
{
    private readonly CraftingBatch _batch=new();
    private CraftingBridge.Choice? _batchChoice;
    private Recipe? _quantityRecipe;
    private int _quantity=1,_maximum;
    private bool _pendingCraft,_ownsQuantity;
    private bool _executingSingle;
    private int _previousAmount;
    private bool _previousTouch;
    private float _nextMaximum;
    private Button _less=null!,_more=null!,_max=null!;
    private InputField _amount=null!;
    private Text _batchLabel=null!;
    private bool Busy=>_batch.Active||_gui.m_craftTimer>=0;
    private int PreviewQuantity=>_tab==0?(_batch.Active?_batch.Remaining:_quantity):1;
    private void OwnQuantity()
    {
        _previousAmount=_gui.m_multiCraftAmount;_previousTouch=_gui.m_touchMultiCrafting;_ownsQuantity=true;
        _batch.Cancel();_pendingCraft=false;_batchChoice=null;_quantityRecipe=null;_quantity=1;_nextMaximum=0;
    }
    internal static void PreparePreview(InventoryGui gui)
    {
        if(_current==null||_current._gui!=gui||!_current._ownsQuantity||_current._executingSingle)return;
        gui.m_multiCraftAmount=_current.PreviewQuantity;gui.m_touchMultiCrafting=_current.PreviewQuantity>1;
    }
    private void ReleaseQuantity()
    {
        if(!_ownsQuantity)return;
        if(_batch.Active){_batch.Cancel();_pendingCraft=false;if(_gui.m_craftTimer>=0)_gui.OnCraftCancelPressed();}
        _batchChoice=null;_gui.m_multiCraftAmount=_previousAmount;_gui.m_touchMultiCrafting=_previousTouch;_ownsQuantity=false;
    }
    private void Quantity(int count)
    {
        if(Busy)return;
        _quantity=Mathf.Clamp(count,1,CraftingBatch.Limit);_nextPreview=0;_nextMaximum=0;_messageUntil=0;
        PreparePreview(_gui);_gui.UpdateRecipe(Player.m_localPlayer,0);Refresh();
    }
    private void PaintQuantity()
    {
        var visible=_tab==0&&_choice!=null;
        _batchLabel.gameObject.SetActive(visible);
        foreach(var control in new Selectable[]{_less,_amount,_more,_max})control.gameObject.SetActive(visible);
        if(!visible)return;
        if(_quantityRecipe!=_choice!.Recipe&&!Busy){_quantityRecipe=_choice.Recipe;_quantity=1;_nextMaximum=0;PreparePreview(_gui);_gui.UpdateRecipe(Player.m_localPlayer,0);}
        if(!Busy&&Time.unscaledTime>=_nextMaximum)
        {
            _nextMaximum=Time.unscaledTime+1;
            _maximum=CraftingBatch.Maximum(count=>CraftingBridge.CanCraftCount(_choice,count));
        }
        if(!_amount.isFocused)_amount.SetTextWithoutNotify((_batch.Active?_batch.Remaining:_quantity).ToString());
        _amount.interactable=!Busy;_less.interactable=!Busy&&_quantity>1;
        _more.interactable=!Busy&&_quantity<CraftingBatch.Limit;_max.interactable=!Busy&&_maximum>0;
        Caption(_max,"Max: "+_maximum);
        _context.text="Materials for "+PreviewQuantity+" batch"+(PreviewQuantity==1?"":"es");
        _heroNote.text="Produces "+CraftingBridge.Output(_choice,PreviewQuantity)+" item"+(CraftingBridge.Output(_choice,PreviewQuantity)==1?"":"s");
        Caption(_action,"Craft "+CraftingBridge.Output(_choice,PreviewQuantity));
    }
    private void StartBatch()
    {
        if(_choice==null||Busy)return;
        if(!CraftingBridge.CanCraftCount(_choice,_quantity)){Message("Batch unavailable · check materials, station and inventory space");return;}
        _batchChoice=_choice;_batch.Start(_quantity);_pendingCraft=true;_messageUntil=0;
        StartNextCraft();
    }
    private void StartNextCraft()
    {
        if(_batch.Active&&!_pendingCraft&&_gui.m_craftTimer<0)
        {
            // A native or another-mod cancellation can bypass our button.
            _batch.Cancel();_batchChoice=null;Message("Batch stopped · "+_batch.Completed+" / "+_batch.Total+" crafts completed");_nextMaximum=0;return;
        }
        if(!_batch.Active||!_pendingCraft||_gui.m_craftTimer>=0)return;
        _pendingCraft=false;
        bool started;
        _executingSingle=true;
        try{started=_batchChoice!=null&&CraftingBridge.ExecuteSingle(_gui,_batchChoice);}
        catch(Exception error){started=false;Plugin.Log.LogError("Queued craft failed; stopped remaining work: "+error);}
        finally{_executingSingle=false;}
        if(!started)
        {
            _batch.Complete(false);Message("Batch stopped · "+_batch.Completed+" / "+_batch.Total+" crafts completed. Check materials, station and space.");
            _batchChoice=null;
            _nextMaximum=0;
        }
    }
    private void CancelCraft()
    {
        var completed=_batch.Completed;var total=_batch.Total;var queued=_batch.Active;
        _batch.Cancel();_pendingCraft=false;_batchChoice=null;
        if(_gui.m_craftTimer>=0)_gui.OnCraftCancelPressed();
        Message(queued?"Batch cancelled · "+completed+" / "+total+" crafts completed":"Craft cancelled");_nextMaximum=0;Refresh();
    }
}
