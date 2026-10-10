using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RestlessQoL.Core;

namespace RestlessQoL.Ui;

// Native InventoryGui remains the owner of recipes, requirements and craft execution.
internal static class CraftingBridge
{
    internal sealed class Choice
    {
        internal Recipe Recipe=null!;
        internal ItemDrop.ItemData? Item;
        internal string Name="", Category="", State="";
        internal Sprite? Icon;
    }
    internal static List<Choice> Choices(InventoryGui gui)
    {
        var result=new List<Choice>();
        foreach(var pair in gui.m_availableRecipes)
        {
            var item=pair.ItemData??pair.Recipe?.m_item?.m_itemData;
            if(pair.Recipe==null||item==null)continue;
            result.Add(new Choice{Recipe=pair.Recipe,Item=pair.ItemData,Name=Local(item.m_shared.m_name),
                Category=item.m_shared.m_itemType.ToString(),Icon=item.GetIcon(),
                State=pair.ItemData!=null ? "Quality "+item.m_quality+(item.m_equipped?" · Equipped":"") : pair.CanCraft?"Materials available":"Requirements missing"});
        }
        return result;
    }
    // Only repaint rows when their visible data, selection or layout actually changed.
    internal static string RowsStamp(IEnumerable<Choice> choices,Choice? selected,int tab,float width,bool running)
    {
        var stamp=new StringBuilder().Append(tab).Append('|').Append(width).Append('|').Append(running);
        foreach(var c in choices)stamp.Append('|').Append(c.Recipe.GetInstanceID()).Append(':').Append(c.Item?.GetHashCode()??0).Append(':').Append(c.Name).Append(':').Append(c.State).Append(':').Append(c.Icon?.GetInstanceID()??0).Append(':').Append(selected!=null&&c.Recipe==selected.Recipe&&ReferenceEquals(c.Item,selected.Item));
        return stamp.ToString();
    }
    internal static bool Select(InventoryGui gui,Choice choice)
    {
        if(gui.m_craftTimer>=0)return false;
        for(var i=0;i<gui.m_availableRecipes.Count;i++)
        {
            var pair=gui.m_availableRecipes[i];
            if(pair.Recipe==choice.Recipe&&ReferenceEquals(pair.ItemData,choice.Item))
            { gui.SetRecipe(i,true); gui.UpdateRecipe(Player.m_localPlayer,0f); return true; }
        }
        return false;
    }
    internal static bool Selected(InventoryGui gui,Choice choice) =>
        gui.m_selectedRecipe.Recipe==choice.Recipe&&ReferenceEquals(gui.m_selectedRecipe.ItemData,choice.Item);
    internal static string Requirements(InventoryGui gui)
    {
        var lines=new List<string>();
        foreach(var slot in gui.m_recipeRequirementList)
        {
            if(slot==null||!slot.activeSelf)continue;
            var name=RestlessUi.Deep<TMP_Text>(slot.transform,"res_name");
            var amount=RestlessUi.Deep<TMP_Text>(slot.transform,"res_amount");
            var text=Local(name?.text??"");
            if(text.Length==0)
            {
                var icon=RestlessUi.Deep<Image>(slot.transform,"res_icon");
                var tip=icon?.GetComponent<UITooltip>()??slot.GetComponent<UITooltip>();
                text=Local(!string.IsNullOrEmpty(tip?.m_topic)?tip!.m_topic:tip?.m_text??"Material");
            }
            var counts=amount?.text??"";
            if(amount!=null&&amount.color.r>amount.color.g*1.4f)counts="<color=#DB7770>"+counts+"</color>";
            lines.Add(text+"  "+counts);
        }
        return lines.Count==0?"No materials required":string.Join("\n\n",lines);
    }
    internal static string Preview(Choice choice)
    {
        var item=choice.Item??choice.Recipe.m_item.m_itemData;
        var next=item.Clone();next.m_quality=choice.Item!=null?item.m_quality+1:1;
        if(choice.Item==null)return Local(next.GetTooltip());
        return "CURRENT · QUALITY "+item.m_quality+"\n"+Local(item.GetTooltip())
            +"\n\nAFTER UPGRADE · QUALITY "+next.m_quality+"\n"+Local(next.GetTooltip());
    }
    internal static bool Execute(InventoryGui gui,Choice choice)
    {
        if(!Select(gui,choice)||Player.m_localPlayer==null||!gui.m_craftButton.interactable)return false;
        if(choice.Item!=null&&!Player.m_localPlayer.GetInventory().GetAllItems().Contains(choice.Item))return false;
        gui.OnCraftPressed();return gui.m_craftTimer>=0;
    }
    internal static string Local(string text)=>Localization.instance!=null?Localization.instance.Localize(text):text;
}
