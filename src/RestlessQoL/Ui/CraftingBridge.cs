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
        internal bool Available;
    }
    internal static List<Choice> Choices(InventoryGui gui)
    {
        var result=new List<Choice>();
        foreach(var pair in gui.m_availableRecipes)
        {
            var item=pair.ItemData??pair.Recipe?.m_item?.m_itemData;
            if(pair.Recipe==null||item==null)continue;
            result.Add(new Choice{Recipe=pair.Recipe,Item=pair.ItemData,Name=Local(item.m_shared.m_name),
                Category=item.m_shared.m_itemType.ToString(),Icon=item.GetIcon(),Available=pair.CanCraft,
                State=pair.ItemData!=null ? (pair.CanCraft?"Ready · ":"Requirements missing · ")+"Quality "+item.m_quality+(item.m_equipped?" · Equipped":"") : pair.CanCraft?"Ready to craft":"Requirements missing"});
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
    internal sealed class Material
    {
        internal string Name="",Counts="";internal Sprite? Icon;internal bool Missing;
    }
    internal static List<Material> Materials(InventoryGui gui)
    {
        var result=new List<Material>();
        foreach(var slot in gui.m_recipeRequirementList)
        {
            if(slot==null||!slot.activeSelf)continue;
            var amount=RestlessUi.Deep<TMP_Text>(slot.transform,"res_amount");
            var icon=RestlessUi.Deep<Image>(slot.transform,"res_icon");
            if(amount==null||!amount.gameObject.activeSelf||icon==null||!icon.gameObject.activeSelf)continue;
            var name=Local(RestlessUi.Deep<TMP_Text>(slot.transform,"res_name")?.text??"");
            if(name.Length==0){var tip=icon.GetComponent<UITooltip>()??slot.GetComponent<UITooltip>();name=Local(!string.IsNullOrEmpty(tip?.m_topic)?tip!.m_topic:tip?.m_text??"Material");}
            var counts=amount.text;var parts=CraftingDetails.Plain(counts).Split('/');
            var missing=parts.Length==2&&int.TryParse(parts[0].Trim(),out var have)&&int.TryParse(parts[1].Trim(),out var need)?have<need:amount.color.r>amount.color.g*1.4f;
            result.Add(new Material{Name=name,Counts=counts,Icon=icon.sprite,Missing=missing});
        }
        return result;
    }
    internal static CraftingDetails.Details Details(Choice choice)
    {
        var item=choice.Item??choice.Recipe.m_item.m_itemData;
        var next=item.Clone();next.m_quality=choice.Item!=null?item.m_quality+1:1;
        return choice.Item==null?CraftingDetails.Read(Local(next.GetTooltip())):CraftingDetails.Read(Local(item.GetTooltip()),Local(next.GetTooltip()));
    }
    internal static string Requirements(InventoryGui gui)
    {
        var lines=new List<string>();
        foreach(var slot in gui.m_recipeRequirementList)
        {
            if(slot==null||!slot.activeSelf)continue;
            var name=RestlessUi.Deep<TMP_Text>(slot.transform,"res_name");
            var amount=RestlessUi.Deep<TMP_Text>(slot.transform,"res_amount");
            var nativeIcon=RestlessUi.Deep<Image>(slot.transform,"res_icon");
            // HideRequirement disables these children, leaving the pooled slot itself active.
            // The inventory dresser hides TMP rendering, so use child activeSelf, not alpha/enabled.
            if(amount==null||!amount.gameObject.activeSelf||nativeIcon==null||!nativeIcon.gameObject.activeSelf)continue;
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
    internal static string BlockedReason(InventoryGui gui,Choice choice)
    {
        // Native tooltip owns the primary failure, including modded requirement checks.
        var native=Local(gui.m_craftButton.GetComponent<UITooltip>()?.m_text??"");
        if(native.Length>0&&native!=Local("$msg_missingrequirement")&&native!=Local("$msg_missingstation"))return native;
        var station=Player.m_localPlayer?.GetCurrentCraftingStation();
        var quality=choice.Item==null?1:choice.Item.m_quality+1;
        var required=station!=null&&station.m_upgrader?1:choice.Recipe.GetRequiredStationLevel(quality);
        if(choice.Recipe.GetRequiredStation(quality)!=null&&(station==null||station.GetLevel()<required))
            return "Requires station level "+required+" · current "+(station?.GetLevel()??0);
        // Alternative-ingredient recipes deliberately show unavailable alternatives.
        if(!choice.Recipe.m_requireOnlyOneIngredient)
        {
            var missing=Materials(gui).Where(m=>m.Missing).Select(m=>m.Name).ToList();
            if(missing.Count>0)return "Missing: "+string.Join(", ",missing.Take(2))+(missing.Count>2?" +"+(missing.Count-2):"");
        }
        if(native.Length>0)return native;
        return "Craft unavailable · check the station and recipe requirements";
    }
    internal static string ResultText(string name,bool upgrade,Dictionary<int,int> before,Dictionary<int,int> after)
    {
        var gains=after.Where(p=>p.Value-(before.TryGetValue(p.Key,out var count)?count:0)>0).ToList();
        var quantity=after.Values.Sum()-before.Values.Sum();
        return upgrade&&gains.Count>0?"Result: "+Local(name)+" · quality "+gains.Max(p=>p.Key)
            :!upgrade&&quantity>0?"Crafted "+Local(name)+" × "+quantity:"Craft ended · no item gain";
    }
    internal static string Local(string text)=>Localization.instance!=null?Localization.instance.Localize(text):text;
}
