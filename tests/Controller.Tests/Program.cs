using RestlessQoL.HudTweaks;
using RestlessQoL.Core;
var checks=0;
void Check(bool ok,string why){checks++;if(!ok)throw new Exception(why);}
foreach(var width in new[]{5,8,10}) foreach(var rows in new[]{1,4,6})
{
    var cells=new List<Vector2i>();
    for(var y=0;y<rows;y++) for(var x=0;x<width;x++) cells.Add(new(x,y));
    for(var i=0;i<9;i++) cells.Add(new(i%width,rows+i/width));
    foreach(var cell in cells)
    {
        Check(ControllerNavigation.Stored(ControllerNavigation.Visual(cell,width,rows),width,rows)==cell,"Visual/storage round trip must preserve every item address");
        foreach(var d in new[]{new Vector2i(-1,0),new(1,0),new(0,-1),new(0,1)})
            Check(cells.Contains(ControllerNavigation.Move(cell,width,rows,d.x,d.y)),"Navigation must never enter a hidden reserved cell");
    }
    var east=ControllerNavigation.Move(new(width-1,0),width,rows,1,0);
    Check(east==new Vector2i(0,rows),"Right from bag edge must reach head equipment");
    Check(ControllerNavigation.Move(east,width,rows,-1,0)==new Vector2i(width-1,0),"Left from head must return to bag");
    Check(ControllerNavigation.Move(new(0,rows),width,rows,1,0)==new Vector2i(1,rows),"Horizontal equipment traversal follows visible columns");
}
Check(ControllerNavigation.Modifier(false,false)==InventoryGrid.Modifier.Select,"A selects");
Check(ControllerNavigation.Modifier(true,false)==InventoryGrid.Modifier.Split,"LT+A splits");
Check(ControllerNavigation.Modifier(false,true)==InventoryGrid.Modifier.Drop,"RT+A drops");
Check(ControllerNavigation.Modifier(true,true)==InventoryGrid.Modifier.Drop,"Native right-trigger priority retained");

var home=new Inventory(); var grid=new InventoryGrid(home); var gui=new InventoryGui{m_playerGrid=grid};
InventoryGui.instance=gui; Player.m_localPlayer=new Player(home); ExtraSlots.Rows=4;
void Reset()
{
    ZInput.Down.Clear();ZInput.Held.Clear();ZInput.Gamepad=true;ZInput.Touch=false;Console.Visible=false;
    ModConfig.ExtraSlotsEnabled.Value=true;grid.m_uiGroup.IsActive=true;grid.jumpToNextContainer=false;
    grid.SetGamepadSelection(new(7,0)); grid.OnMoveToUpperInventoryGrid=null;grid.OnMoveToLowerInventoryGrid=null;
}
Reset();
Vector2i acted=new(-1,-1); InventoryGrid.Modifier action=InventoryGrid.Modifier.Drop; ItemDrop.ItemData? item=null;
grid.m_onSelected=(g,i,p,m)=>{acted=p;action=m;item=i;};
ZInput.Down.UnionWith(new[]{"JoyDPadRight","JoyButtonA"});
Check(!ExtraSlots.Handle(grid),"Custom extra grid must own its input");
Check(acted==new Vector2i(0,4)&&item?.Position==acted,"Move+A in one frame must act on the newly focused visible equipment cell");
Check(action==InventoryGrid.Modifier.Select,"Native select callback retained");
Check(grid.LastSelectable.Selected,"Visible equipment selection must update EventSystem target");
Check(grid.m_ensureVisible.Calls==0,"Relocated equipment must not scroll the bag away");
Check(ControllerUi.Focused(grid.GetElement(0,4,8)),"Focus must resolve a relocated native element by identity");
var other=new InventoryGrid(new Inventory());gui.ContainerGrid=other;
Check(!ControllerUi.Focused(other.GetElement(0,4,8)),"Same coordinates in another grid must not receive focus");
foreach(var source in new[]{"JoyDPad","JoyLStick"})
{
    Reset();ZInput.Down.Add(source+"Right");ZInput.Down.Add("JoyButtonX");ZInput.Held.Add("JoyLTrigger");
    ExtraSlots.Handle(grid);Check(acted==new Vector2i(0,4)&&action==InventoryGrid.Modifier.Move,"Controller chest transfer callback uses visible target for dpad and stick");
}
Reset();var used=0;grid.m_onRightClick=(g,i,p)=>{used++;acted=p;};
ZInput.Down.Add("JoyButtonX");ExtraSlots.Handle(grid);Check(used==1&&acted==new Vector2i(7,0),"X uses/equips through native callback");
Reset();ZInput.Down.Add("JoyButtonA");ZInput.Held.Add("JoyLTrigger");ExtraSlots.Handle(grid);
Check(action==InventoryGrid.Modifier.Split,"Split modifier passes unchanged");
ZInput.Held.Add("JoyRTrigger");ExtraSlots.Handle(grid);Check(action==InventoryGrid.Modifier.Drop,"Drop takes precedence over split");
Reset();grid.SetGamepadSelection(new(7,5));ZInput.Down.Add("JoyButtonA");ExtraSlots.Handle(grid);
Check(acted==new Vector2i(7,3),"Invalid hidden selection must recover to visible bag before taking action");
Reset();grid.SetGamepadSelection(new(3,0));var handoffs=0;
grid.OnMoveToUpperInventoryGrid=p=>handoffs++;
ZInput.Down.Add("JoyDPadUp");ZInput.Held.Add("JoyDPadUp");ExtraSlots.Handle(grid);
Check(handoffs==0,"Arriving at boundary must not immediately switch inventories");
ZInput.Down.Clear();ZInput.Held.Clear();ExtraSlots.Handle(grid);
ZInput.Down.UnionWith(new[]{"JoyDPadUp","JoyButtonA"});acted=new(-1,-1);ExtraSlots.Handle(grid);
Check(handoffs==1&&acted==new Vector2i(-1,-1),"Explicit boundary handoff must not also act on the old grid");

foreach(var guard in Enumerable.Range(0,5))
{
    Reset(); if(guard==0)ZInput.Gamepad=false;if(guard==1)ZInput.Touch=true;if(guard==2)Console.Visible=true;
    if(guard==3)grid.m_uiGroup.IsActive=false;if(guard==4)ModConfig.ExtraSlotsEnabled.Value=false;
    Check(ExtraSlots.Handle(grid),"Disabled/inactive/non-controller contexts must retain native handler");
}
Reset();Check(ExtraSlots.Handle(other),"Container grids retain native navigation");
Check(ControllerNavigation.NearbyStackAllowed(true,true,false,false,false,true),"Nearby stack is available in player inventory");
foreach(var guard in Enumerable.Range(0,6))
    Check(!ControllerNavigation.NearbyStackAllowed(guard!=0,guard!=1,guard==2,guard==3,guard==4,guard!=5),"Nearby stack must not conflict with chest, drag, modal or disabled/input contexts");
System.Console.WriteLine($"{checks} controller navigation, focus, native-action and input-isolation regressions passed.");

public struct Vector2i : IEquatable<Vector2i>
{
    public int x,y;public Vector2i(int x,int y){this.x=x;this.y=y;}
    public bool Equals(Vector2i p)=>x==p.x&&y==p.y;public override bool Equals(object? p)=>p is Vector2i v&&Equals(v);
    public override int GetHashCode()=>HashCode.Combine(x,y);
    public static bool operator ==(Vector2i a,Vector2i b)=>a.Equals(b);public static bool operator !=(Vector2i a,Vector2i b)=>!a.Equals(b);
}
public static class Console { public static bool Visible;public static bool IsVisible()=>Visible; }
public static class ZInput
{
    public static bool Gamepad=true,Touch;public static HashSet<string> Down=new(),Held=new();
    public static bool IsExclusiveGamepadActive()=>Gamepad;public static bool IsTouchActive()=>Touch;
    public static bool GetButtonDown(string s)=>Down.Contains(s);public static bool GetButton(string s)=>Held.Contains(s);
}
public class Inventory {public int GetWidth()=>8;public Dictionary<Vector2i,ItemDrop.ItemData> Items=new();}
public class ItemDrop {public class ItemData{public Vector2i Position;}}
public class Player {public static Player? m_localPlayer;private Inventory inv;public Player(Inventory i){inv=i;}public Inventory GetInventory()=>inv;}
public class UIGroupHandler {public bool IsActive=true;}
public class InventoryElement {public Vector2i Position;}
public class Scroller {public int Calls;public void CenterOnItem(UnityEngine.RectTransform r)=>Calls++;}
public class InventoryGui {public static InventoryGui? instance;public InventoryGrid m_playerGrid=null!;public InventoryGrid? ContainerGrid;}
public class InventoryGrid
{
    public enum Modifier{Select,Split,Move,Drop}
    public UIGroupHandler m_uiGroup=new();public bool jumpToNextContainer;public Scroller m_ensureVisible=new();
    public UnityEngine.GameObject gameObject=new();public UnityEngine.UI.Selectable LastSelectable=new();
    public Action<Vector2i>? OnMoveToUpperInventoryGrid,OnMoveToLowerInventoryGrid;
    public Action<InventoryGrid,ItemDrop.ItemData?,Vector2i,Modifier>? m_onSelected;
    public Action<InventoryGrid,ItemDrop.ItemData?,Vector2i>? m_onRightClick;
    private Inventory inv;private Vector2i selected;private Dictionary<Vector2i,InventoryElement> elements=new();
    public InventoryGrid(Inventory i){inv=i;}
    public Inventory GetInventory()=>inv;public Vector2i SelectionGridPosition=>selected;
    public void SetGamepadSelection(Vector2i p)=>selected=p;
    public UnityEngine.RectTransform GetGamepadSelectedElement()=>new(LastSelectable);
    public ItemDrop.ItemData GetGamepadSelectedItem(){if(!inv.Items.TryGetValue(selected,out var item)){item=new(){Position=selected};inv.Items[selected]=item;}return item;}
    public InventoryElement GetElement(int x,int y,int width){var p=new Vector2i(x,y);if(!elements.TryGetValue(p,out var e)){e=new(){Position=p};elements[p]=e;}return e;}
}
namespace UnityEngine
{
    public class GameObject{public bool activeInHierarchy=true;}
    public class RectTransform{private UI.Selectable selected;public RectTransform(UI.Selectable s){selected=s;}public T? GetComponent<T>() where T:class=>selected as T;}
    public static class Mathf{public static int Min(int a,int b)=>Math.Min(a,b);}
}
namespace UnityEngine.UI{public class Selectable{public bool Selected;public void Select()=>Selected=true;}}
namespace RestlessQoL.Core{public class Setting{public bool Value=true;}public static class ModConfig{public static Setting ExtraSlotsEnabled=new();}}
namespace RestlessQoL.HudTweaks
{
    public sealed partial class ExtraSlots
    {
        private static int _visibleHeight;public static int Rows{set=>_visibleHeight=value;}
        public static bool Handle(InventoryGrid g)=>UpdateControllerGrid(g);
    }
}
