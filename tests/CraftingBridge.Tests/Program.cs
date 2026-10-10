using RestlessQoL.Ui;

var checks=0;
void Check(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
var player=new Player();Player.m_localPlayer=player;
var recipe=new Recipe();var first=new ItemDrop.ItemData{m_quality=2};var second=new ItemDrop.ItemData{m_quality=3};
player.Inventory.Items.AddRange(new[]{first,second});
var gui=new InventoryGui();
gui.m_availableRecipes.Add(new(recipe,first,true));gui.m_availableRecipes.Add(new(recipe,second,true));
var choices=CraftingBridge.Choices(gui);
Check(choices.Count==2&&ReferenceEquals(choices[1].Item,second),"Same recipe must retain distinct upgrade targets");
gui.m_availableRecipes.Reverse();
Check(CraftingBridge.Select(gui,choices[1])&&ReferenceEquals(gui.m_selectedRecipe.ItemData,second),"Reordering cannot redirect upgrade");
Check(gui.SelectedIndex==0&&gui.Revalidations==1,"Selection must resolve the live index and revalidate");
Check(CraftingBridge.Selected(gui,choices[1])&&!CraftingBridge.Selected(gui,choices[0]),"Selected state must compare actual items");
var preview=CraftingBridge.Preview(choices[1]);
Check(second.m_quality==3&&preview.Contains("AFTER UPGRADE · QUALITY 4"),"Preview must clone without modifying the owned item");
gui.m_craftTimer=0;
Check(!CraftingBridge.Execute(gui,choices[0])&&gui.Executions==0,"Running craft must reject another action");
gui.m_craftTimer=-1;gui.m_craftButton.interactable=false;
Check(!CraftingBridge.Execute(gui,choices[1])&&gui.Executions==0,"Native eligibility must gate execution");
gui.m_craftButton.interactable=true;player.Inventory.Items.Remove(second);
Check(!CraftingBridge.Execute(gui,choices[1])&&gui.Executions==0,"Removed item must not be upgraded");
player.Inventory.Items.Add(second);gui.m_availableRecipes.RemoveAt(0);
Check(!CraftingBridge.Execute(gui,choices[1])&&gui.Executions==0,"Disappearing recipe must reject stale selection");
gui.m_availableRecipes.Add(new(recipe,second,true));
Check(CraftingBridge.Execute(gui,choices[1])&&gui.Executions==1&&ReferenceEquals(gui.ExecutedItem,second),"Native execution must receive the exact target");
gui.m_craftTimer=-1;gui.m_availableRecipes.Clear();gui.m_availableRecipes.Add(new(recipe,null,true));
var craft=CraftingBridge.Choices(gui).Single();
Check(craft.Item==null&&CraftingBridge.Preview(craft).Contains("quality 1"),"New craft preview must represent quality one");
Check(recipe.m_item.m_itemData.m_quality==1,"Craft preview must leave the recipe template unchanged");
var rows=CraftingBridge.Choices(gui);
var stamp=CraftingBridge.RowsStamp(rows,rows[0],0,228,false);
Check(stamp==CraftingBridge.RowsStamp(CraftingBridge.Choices(gui),rows[0],0,228,false),"Unchanged refresh must reuse its row presentation");
Check(stamp!=CraftingBridge.RowsStamp(rows,null,0,228,false),"Selection change must repaint");
Check(stamp!=CraftingBridge.RowsStamp(rows,rows[0],1,228,false),"Tab change must repaint");
Check(stamp!=CraftingBridge.RowsStamp(rows,rows[0],0,186,false),"Layout width change must repaint");
Check(stamp!=CraftingBridge.RowsStamp(rows,rows[0],0,228,true),"Craft start must update disabled controls");
rows[0].State="Requirements missing";
Check(stamp!=CraftingBridge.RowsStamp(rows,rows[0],0,228,false),"Changed native eligibility must repaint");
rows=choices;
Check(CraftingBridge.RowsStamp(rows,rows[0],1,228,false)!=CraftingBridge.RowsStamp(rows,rows[1],1,228,false),"Two copies of the same recipe must preserve distinct selected rows");
Console.WriteLine($"{checks} production crafting bridge checks passed.");

// Controlled native boundaries; the production bridge above is compiled directly.
public class Recipe{public ItemDrop m_item=new();public int GetInstanceID()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);}
public class ItemDrop
{
    public ItemData m_itemData=new();
    public class ItemData
    {
        public Shared m_shared=new();public int m_quality=1;public bool m_equipped;
        public ItemData Clone()=>(ItemData)MemberwiseClone();
        public string GetTooltip()=>"quality "+m_quality;
        private UnityEngine.Sprite _icon=new();public UnityEngine.Sprite GetIcon()=>_icon;
    }
    public class Shared{public string m_name="item";public int m_itemType;}
}
public class InventoryGui
{
    public readonly record struct Pair(Recipe Recipe,ItemDrop.ItemData? ItemData,bool CanCraft);
    public List<Pair> m_availableRecipes=new();public Pair m_selectedRecipe;
    public float m_craftTimer=-1;public UnityEngine.UI.Button m_craftButton=new();
    public UnityEngine.GameObject[] m_recipeRequirementList=Array.Empty<UnityEngine.GameObject>();
    public int SelectedIndex,Revalidations,Executions;public ItemDrop.ItemData? ExecutedItem;
    public void SetRecipe(int index,bool focus){SelectedIndex=index;m_selectedRecipe=m_availableRecipes[index];}
    public void UpdateRecipe(Player? player,float delta)=>Revalidations++;
    public void OnCraftPressed(){Executions++;ExecutedItem=m_selectedRecipe.ItemData;m_craftTimer=0;}
}
public class Player
{
    public static Player? m_localPlayer;public Inventory Inventory=new();public Inventory GetInventory()=>Inventory;
}
public class Inventory{public List<ItemDrop.ItemData> Items=new();public List<ItemDrop.ItemData> GetAllItems()=>Items;}
public class Localization{public static Localization? instance;public string Localize(string text)=>text;}
public class UITooltip{public string m_topic="",m_text="";}
namespace UnityEngine
{
    public class Sprite{public int GetInstanceID()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);}
    public struct Color{public float r,g;}
    public class Transform{}
    public class GameObject{public bool activeSelf;public Transform transform=new();public T? GetComponent<T>() where T:class=>null;}
}
namespace UnityEngine.UI
{
    public class Button{public bool interactable=true;}
    public class Image{public T? GetComponent<T>() where T:class=>null;}
}
namespace TMPro{public class TMP_Text{public string text="";public UnityEngine.Color color;}}
namespace RestlessQoL.Core{public static class RestlessUi{public static T? Deep<T>(UnityEngine.Transform transform,string name) where T:class=>null;}}
