using System;
using System.Linq;
using System.Text;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessCook;

internal sealed partial class CookbookWindow
{
    private string _pantrySelection = "";
    private bool Matches(CookRow row)
    {
        var mead = row.Prefab.StartsWith("Mead", StringComparison.OrdinalIgnoreCase)
            || row.Kind.Equals("mead", StringComparison.OrdinalIgnoreCase);
        return _category switch
        {
            "Feasts" => row.IsFeast,
            "Meals" => row.IsMeal && !mead,
            "Ingredients" => row.IsSideboard || row.Kind == "ingredient",
            "Meads" => mead,
            _ => row.IsFeast || row.IsMeal || row.IsSideboard || mead || row.Kind == "ingredient"
        };
    }
    private void HideDish()
    {
        _station.text = ""; _requirementsLabel.text = ""; HideAfter(_requirementCards,0);
        _dish.enabled = false; _dishFrame.SetActive(false); _dishName.text = ""; _dishKind.text = "";
        _dishKind.transform.parent.gameObject.SetActive(false);
        Position(_details.gameObject,904,184,304,436);
        _details.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,436);
        if (_details.verticalScrollbar != null) Position(_details.verticalScrollbar.gameObject,298,0,6,436);
    }
    private void ShowDish()
    {
        _dishKind.transform.parent.gameObject.SetActive(true);
        Position(_details.gameObject,904,358,304,262);
        _details.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,262);
        if (_details.verticalScrollbar != null) Position(_details.verticalScrollbar.gameObject,298,0,6,262);
    }
    private void PaintOrderSteps()
    {
        foreach (var line in _lines) line.SetActive(false);
        _back.interactable = false;
        var order = _orders.FirstOrDefault(o => o.Id == _order);
        _heading.text = order == null ? "Select an order" : order.Name;
        // Each requirement gets a separate row; no fictitious single ETA for parallel work.
        for (var i = 0; i < _steps.Count; i++)
        {
            var index = i; var step = _steps[i];
            var card = GetCard(_nodes, i, _tree.content);
            var indent = Mathf.Min(step.Depth, 4) * 12;
            Position(card.Root,indent,i*114,528-indent,104);
            card.Set(Local(step.Name), State(step), Icon(step.Output), Tint(step.State), () => { _focus = index; PaintOrderSteps(); PaintDetails(); Size(_details,_details.content.rect.height,true); }, selected: i == _focus);
            card.Progress(step);
        }
        HideAfter(_nodes,_steps.Count); Size(_tree,_steps.Count*114,false);
    }
    private void PaintPantry(bool force)
    {
        var pantry = Kitchen.Pantry(_table);
        var stamp = "pantry|" + string.Join("|",pantry.Select(p => p.Prefab+":"+p.Count+":"+p.Reserved));
        if (!force && _stamp == stamp) return;
        _stamp = stamp; _heading.text = "Kitchen pantry";
        _back.interactable = false;
        foreach (var line in _lines) line.SetActive(false);
        HideAfter(_recipeCards,0);
        if (!pantry.Any(p => p.Prefab == _pantrySelection)) _pantrySelection = pantry.FirstOrDefault()?.Prefab ?? "";
        for (var i=0;i<pantry.Count;i++)
        {
            var item=pantry[i]; var card=GetCard(_nodes,i,_tree.content);
            Position(card.Root,i%2*276,i/2*128,252,116);
            var available=Math.Max(0,item.Count-item.Reserved);
            card.Set(Local(item.Name),available+" available · "+item.Reserved+" reserved",Icon(item.Prefab),RestlessUi.Accent,
                () => { _pantrySelection=item.Prefab; PaintPantry(true); }, selected: item.Prefab == _pantrySelection);
        }
        HideAfter(_nodes,pantry.Count); Size(_tree,((pantry.Count+1)/2)*128,false);
        var selected=pantry.FirstOrDefault(p=>p.Prefab==_pantrySelection);
        HideAfter(_requirementCards,0); _requirementsLabel.text=""; _station.text="";
        ShowDish(); _dish.sprite=selected==null?null:Icon(selected.Prefab); _dish.enabled=_dish.sprite!=null;
        _dishFrame.SetActive(selected!=null); _dishName.text=selected==null?"Pantry is empty":Local(selected.Name);
        _dishKind.text="Stored preparation"; _dishKind.color=RestlessUi.PaperMuted;
        _copy.text="Unreserved food can be collected here. Ready food reserved for active orders stays with its order.";
        Position(_copy.gameObject,0,0,280,Mathf.Max(60,_copy.preferredHeight)); Size(_details,_copy.preferredHeight+16,false);
        _collect.gameObject.SetActive(false); _action.gameObject.SetActive(true);
        _action.interactable=selected!=null&&selected.Count>selected.Reserved; _actionText.text="Take a stack";
        if(Time.unscaledTime>=_messageUntil) _status.text="Prepared food and returned ingredients · order reservations are protected";
    }
    private void TakePantry()
    {
        var item=Kitchen.Pantry(_table).FirstOrDefault(p=>p.Prefab==_pantrySelection);
        if(item==null)return;
        var drop=ObjectDB.instance?.GetItemPrefab(item.Prefab)?.GetComponent<ItemDrop>();
        var stack=drop?.m_itemData?.m_shared?.m_maxStackSize??1;
        try { Message("Collected "+Kitchen.Take(_table,item.Prefab,Math.Min(stack,Math.Max(0,item.Count-item.Reserved)))+"."); PaintPantry(true); }
        catch(Exception e){ Debug.LogException(e); Message("Could not collect. Check your inventory before retrying."); }
    }
}
