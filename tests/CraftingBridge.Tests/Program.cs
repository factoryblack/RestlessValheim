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
UnityEngine.GameObject Material(string name,string count,bool amountActive=true,bool iconActive=true)
{
    var slot=new UnityEngine.GameObject{activeSelf=true};
    slot.transform.Children["res_name"]=new TMPro.TMP_Text{text=name};
    slot.transform.Children["res_amount"]=new TMPro.TMP_Text{text=count,gameObject=new UnityEngine.GameObject{activeSelf=amountActive}};
    slot.transform.Children["res_icon"]=new UnityEngine.UI.Image{gameObject=new UnityEngine.GameObject{activeSelf=iconActive}};
    return slot;
}
gui.m_recipeRequirementList=new[]{Material("Wood","1438/8"),Material("Flint","12/2"),Material("Feathers","1260/2"),Material("Mushroom","25",false,false)};
var materials=CraftingBridge.Requirements(gui);
Check(materials.Contains("Wood  1438/8")&&materials.Contains("Feathers  1260/2"),"Live native material counts must be preserved");
Check(!materials.Contains("Mushroom"),"Inactive native children must exclude a stale pooled material slot");
gui.m_recipeRequirementList=new[]{Material("Hidden icon","3",true,false)};
Check(CraftingBridge.Requirements(gui)=="No materials required","Hidden resource icon must exclude its old count");
gui.m_recipeRequirementList=new[]{Material("Hidden count","3",false,true)};
Check(CraftingBridge.Requirements(gui)=="No materials required","Hidden resource count must exclude its old name");
var details=CraftingDetails.Read("A hide-breaker.\nWeight: <color=orange>0.1</color>\nPierce: <color=orange>27</color>","A hide-breaker.\nWeight: <color=orange>0.1</color>\nPierce: <color=orange>31</color>");
Check(details.Description=="A hide-breaker."&&details.Attributes.Count==2,"Descriptions and native attributes must be separated");
Check(!details.Attributes[0].Changed&&details.Attributes[1].Changed,"Upgrade comparisons must highlight only changed native values");
Check(details.Attributes[1].Current.Contains("27")&&details.Attributes[1].Next!.Contains("31"),"Native current and next stats must be retained");
var extended=CraftingDetails.Read("Native prose without stat separators", "Native prose\nNew effect: 5");
Check(extended.Description.Contains("Native prose without stat separators")&&extended.Description.Contains("AFTER UPGRADE\nNative prose")&&extended.Attributes.Single().Current=="—","Both current and changed upgrade prose must remain visible");
Check(CraftingDetails.Plain("<color=orange>27</color>")=="27","Comparison must ignore formatting tags");
var materialRows=CraftingBridge.Materials(gui);
Check(materialRows.Count==0,"Card data must obey the same native hidden-slot contract");
gui.m_recipeRequirementList=new[]{Material("Wood","3/8")};
Check(CraftingBridge.Materials(gui).Single().Missing,"Shortage cards must use native have/needed counts even between blink frames");
gui.m_recipeRequirementList=new[]{Material("Wood","8/8")};
Check(!CraftingBridge.Materials(gui).Single().Missing,"Exact material coverage must not show a shortage");
gui.m_craftButton.Tip=new UITooltip{m_text="$msg_missingrequirement"};
gui.m_recipeRequirementList=new[]{Material("Wood","3/8")};
Check(CraftingBridge.BlockedReason(gui,craft)=="Missing: Wood","Blocked craft must identify the missing material");
recipe.m_requireOnlyOneIngredient=true;
Check(CraftingBridge.BlockedReason(gui,craft)=="$msg_missingrequirement","Alternative ingredients must not all be reported as required");
recipe.m_requireOnlyOneIngredient=false;gui.m_craftButton.Tip.m_text="Inventory full";
Check(CraftingBridge.BlockedReason(gui,craft)=="Inventory full","Specific native failures must take precedence over material hints");
gui.m_craftButton.Tip.m_text="$msg_missingrequirement";recipe.Station=new CraftingStation{Level=1};recipe.RequiredLevel=3;player.Station=recipe.Station;
Check(CraftingBridge.BlockedReason(gui,craft)=="Requires station level 3 · current 1","Station shortage must name required and actual levels");
recipe.Station.m_upgrader=true;gui.m_recipeRequirementList=Array.Empty<UnityEngine.GameObject>();
Check(!CraftingBridge.BlockedReason(gui,craft).Contains("level 3"),"Native upgrader stations use level one instead of ordinary recipe level");
var sameProse=CraftingDetails.Read("Same prose\nDamage: 1","Same prose\nDamage: 2");
Check(sameProse.Description=="Same prose","Unchanged upgrade descriptions must not be duplicated");
Check(CraftingBridge.ResultText("Arrow",false,new(){{1,20}},new(){{1,40}})=="Crafted Arrow × 20","Feedback must use actual inventory gain, including stacked outputs");
Check(CraftingBridge.ResultText("Axe",true,new(){{2,1},{5,1}},new(){{3,1},{5,1}})=="Result: Axe · quality 3","Upgrade feedback must identify the changed copy rather than existing higher-quality equipment");
Check(CraftingBridge.ResultText("Axe",true,new(){{3,1}},new(){{2,1}})=="Result: Axe · quality 2","Native failed upgrades that reduce quality must not be announced as a quality increase");
Check(CraftingBridge.ResultText("Arrow",false,new(){{1,20}},new(){{1,20}})=="Craft ended · no item gain","Rejected crafts must never announce success");

recipe.Station=null;player.Station=null;player.Requirements=true;player.AvailableCrafts=3;player.Inventory.OutputSpace=40;
Check(CraftingBatch.Maximum(n=>CraftingBridge.CanCraftCount(craft,n))==2,"Max must combine material and actual output-space limits");
player.Inventory.OutputSpace=0;
Check(CraftingBatch.Maximum(n=>CraftingBridge.CanCraftCount(craft,n))==0,"A full inventory must yield no batch capacity");
Check(CraftingBridge.BlockedReason(gui,craft).Contains("inventory space"),"Full-output blocker must be explicit even when native button misses capacity");
player.Inventory.OutputSpace=9999;player.AvailableCrafts=999;
Check(CraftingBatch.Maximum(n=>CraftingBridge.CanCraftCount(craft,n))==99,"Max must respect the bounded queue limit");
Check(CraftingBridge.Output(craft,3)==60,"Craft counts must remain distinct from native output quantities");
recipe.Station=new CraftingStation{Usable=false};player.Station=recipe.Station;
Check(!CraftingBridge.CanCraftCount(craft,1),"Unusable stations must reject queued execution");
recipe.Station=null;player.Station=null;
gui.m_availableRecipes.Clear();gui.m_availableRecipes.Add(new(recipe,null,true));gui.m_craftTimer=-1;gui.m_multiCraftAmount=17;gui.m_touchMultiCrafting=true;
Check(CraftingBridge.ExecuteSingle(gui,craft)&&!gui.m_multiCrafting,"Queue execution must use normal native craft timing");
Check(gui.m_multiCraftAmount==17&&gui.m_touchMultiCrafting,"Scoped single execution must restore preview fields");
gui.m_craftTimer=-1;player.Inventory.OutputSpace=0;var executions=gui.Executions;
Check(!CraftingBridge.ExecuteSingle(gui,craft)&&gui.Executions==executions,"Capacity loss must stop without executing a queued craft");
Check(gui.m_multiCraftAmount==17&&gui.m_touchMultiCrafting,"Rejected crafts must also restore native fields");
var batch=new CraftingBatch();batch.Start(3);
Check(batch.Remaining==3&&batch.Complete(true)&&batch.Completed==1&&batch.Remaining==2,"Only confirmed completion advances the queue");
Check(!batch.Complete(false)&&batch.Completed==1&&!batch.Active,"Failure stops remaining crafts without counting the failed craft");
batch.Start(2);batch.Complete(true);batch.Cancel();
Check(batch.Completed==1&&!batch.Active&&!batch.Complete(true)&&batch.Completed==1,"Cancellation keeps completed work and ignores late completion callbacks");
batch.Start(1);Check(!batch.Complete(true)&&batch.Completed==1,"A single-craft queue ends exactly once");
batch.Start(999);Check(batch.Total==99,"Queue input must be capped");
batch.Start(-4);Check(batch.Total==1,"Invalid queue input must be clamped");

Console.WriteLine($"{checks} production crafting bridge checks passed.");

// Controlled native boundaries; the production bridge above is compiled directly.
public class Recipe{public string name="recipe";public ItemDrop m_item=new();public bool m_requireOnlyOneIngredient;public CraftingStation? Station;public int RequiredLevel=1;public int m_amount=20;public int GetAmount(int quality,out int used,out ItemDrop.ItemData? ingredient,int count){used=count;ingredient=null;return m_amount*count;}public int GetRequiredStationLevel(int quality)=>RequiredLevel;public CraftingStation? GetRequiredStation(int quality)=>Station;public int GetInstanceID()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);}
public class CraftingStation{public bool m_upgrader;public int Level=1;public bool Usable=true;public bool CheckUsable(Player player,bool message)=>Usable;public int GetLevel()=>Level;}
public class ItemDrop
{
    public UnityEngine.GameObject gameObject=new();public ItemData m_itemData=new();
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
    public int m_multiCraftAmount=5;public bool m_touchMultiCrafting,m_multiCrafting;public float m_craftTimer=-1;public UnityEngine.UI.Button m_craftButton=new();
    public UnityEngine.GameObject[] m_recipeRequirementList=Array.Empty<UnityEngine.GameObject>();
    public int SelectedIndex,Revalidations,Executions;public ItemDrop.ItemData? ExecutedItem;
    public void SetRecipe(int index,bool focus){SelectedIndex=index;m_selectedRecipe=m_availableRecipes[index];}
    public void UpdateRecipe(Player? player,float delta)=>Revalidations++;
    public void OnCraftPressed(){Executions++;ExecutedItem=m_selectedRecipe.ItemData;m_craftTimer=0;}
}
public class Player
{
    public static Player? m_localPlayer;public CraftingStation? Station;public CraftingStation? GetCurrentCraftingStation()=>Station;public bool Requirements=true;public int AvailableCrafts=99;public bool NoCostCheat()=>false;public bool HaveRequirements(Recipe recipe,bool discover,int quality,int amount)=>Requirements&&amount<=AvailableCrafts;public Inventory Inventory=new();public Inventory GetInventory()=>Inventory;
}
public enum GlobalKeys{NoCraftCost}
public class ZoneSystem{public static ZoneSystem? instance;public bool GetGlobalKey(GlobalKeys key)=>false;}
public class Inventory{public int OutputSpace=9999;public bool CanAddItem(UnityEngine.GameObject item,int count)=>count<=OutputSpace;public List<ItemDrop.ItemData> Items=new();public List<ItemDrop.ItemData> GetAllItems()=>Items;}
public class Localization{public static Localization? instance;public string Localize(string text)=>text;}
public class UITooltip{public string m_topic="",m_text="";}
namespace UnityEngine
{
    public class Sprite{public int GetInstanceID()=>System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);}
    public struct Color{public float r,g;}
    public class Transform{public Dictionary<string,object> Children=new();}
    public class GameObject{public bool activeSelf;public Transform transform=new();public T? GetComponent<T>() where T:class=>null;}
}
namespace UnityEngine.UI
{
    public class Button{public bool interactable=true;public UITooltip? Tip;public T? GetComponent<T>() where T:class=>Tip as T;}
    public class Image{public UnityEngine.Sprite? sprite;public UnityEngine.GameObject gameObject=new(){activeSelf=true};public T? GetComponent<T>() where T:class=>null;}
}
namespace TMPro{public class TMP_Text{public UnityEngine.GameObject gameObject=new(){activeSelf=true};public string text="";public UnityEngine.Color color;}}
namespace RestlessQoL.Core{public static class RestlessUi{public static T? Deep<T>(UnityEngine.Transform transform,string name) where T:class=>transform.Children.TryGetValue(name,out var value)?value as T:null;}}
