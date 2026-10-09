using UnityEngine;
public class Component { public GameObject gameObject = new(); public Transform transform => gameObject.transform; public T? GetComponent<T>() where T:class => gameObject.GetComponent<T>(); }
public class GameObject { public string name=""; public Transform transform=new(); public Dictionary<Type,object> Components=new(); public T? GetComponent<T>() where T:class => Components.TryGetValue(typeof(T),out var c)?(T)c:null; public T Add<T>(T c) where T:Component {c.gameObject=this;Components[typeof(T)]=c;return c;} }
public class Piece:Component {public long Creator; public object m_resources=new(); public long GetCreator()=>Creator; public string FreeBuildKey()=>"free";}
public class Plant:Component {public GameObject[] m_grownPrefabs=Array.Empty<GameObject>();}
public class Pickable:Component {public ZNetView m_nview=new(); public float m_respawnTimeMinutes;}
public class ZDO {public Dictionary<string,string> Data=new(); public string GetString(string key)=>Data.GetValueOrDefault(key,"");}
public class ZNetView {public bool Valid=true,Owner=true; public ZDO Data=new(); public bool IsValid()=>Valid; public bool IsOwner()=>Owner;public void ClaimOwnership(){Owner=true;} public ZDO GetZDO()=>Data;}
public class ZNetScene {public static ZNetScene? instance; public List<GameObject> m_prefabs=new(); public GameObject? GetPrefab(string name)=>m_prefabs.Find(p=>p.name==name);}
public static class Utils {public static string GetPrefabName(GameObject go)=>go.name;}
public class Humanoid:Component {}
public class Player:Humanoid {public static Player? m_localPlayer;public long Id;public bool m_noPlacementCost;public bool CanPay=true;public int Paid;public Piece? Planted;public Inventory Bag=new();public Inventory GetInventory()=>Bag;public bool IsDead()=>false;public void Message(MessageHud.MessageType t,string s){}public long GetPlayerID()=>Id;public bool HaveRequirements(Piece p,RequirementMode m)=>CanPay;public void ConsumeResources(object o,int n)=>Paid++;public void PlacePiece(Piece p,Vector3 v,Quaternion q,bool a,bool b)=>Planted=p;public enum RequirementMode{CanBuild}}
public class ZoneSystem {public static ZoneSystem? instance;public bool GetGlobalKey(string key)=>false;}
public class PrivateArea {public static List<PrivateArea> m_allAreas=new();public bool Enabled=true,Inside=true;public Piece m_piece=new();public HashSet<long> Permitted=new();public bool IsEnabled()=>Enabled;public bool IsInside(Vector3 p,float r)=>Inside;public bool IsPermitted(long actor)=>Permitted.Contains(actor);}
public class ItemDrop:Component { public ZNetView m_nview=new(); public class SharedData{public string m_name="material";public int m_maxStackSize=50;} public class ItemData{public SharedData m_shared=new();public GameObject? m_dropPrefab;public int m_quality=1,m_variant,m_worldLevel,m_stack=1;public bool m_cheated;public float m_durability;public long m_crafterID;public Dictionary<string,string> m_customData=new();public ItemData Clone()=>(ItemData)MemberwiseClone();}}
public class Inventory{public List<ItemDrop.ItemData> Items=new();public List<ItemDrop.ItemData> GetAllItems()=>Items;public bool HaveEmptySlot()=>true;public bool AddItem(ItemDrop.ItemData i){Items.Add(i);return true;}public void Changed(bool a,bool b){}}
public class MessageHud{public enum MessageType{Center}}
public class PileBox:Component{public static List<PileBox> All=new();public bool Live=true;public int Stored=10;public ItemDrop.ItemData Item=new();public ZNetView View=new();public string GetHoverName()=>"pile";}
public static class PileConfig{public static bool On=true;public static float Nearby=5;}
public static class ModConfig{public static Config<float> StorageRange=new(20);}
public static class Pile{public static bool Delay;public static List<Action> Pending=new();public static int Claims;public static void Write(ZNetView v,int count){var box=PileBox.All.Find(b=>b.View==v);if(box!=null)box.Stored=count;}public static int Count(ZNetView v)=>PileBox.All.Find(b=>b.View==v)?.Stored??0;public static void OwnThen(ZNetView v,Action<ZNetView> act,Action? fail=null){Claims++;if(Delay)Pending.Add(()=>act(v));else act(v);}public static string Title(ItemDrop.ItemData i)=>i.m_shared.m_name;}
partial class PileHarness{static void Tell(Player p,string s,int n,ItemDrop.ItemData i){}}
public class Config<T>{public T Value;public Config(T value)=>Value=value;}
public static class PlantConfig {public static bool On=true;public static Config<bool> Replant=new(true),BulkHarvest=new(false);}
partial class PlantHarness {
    static Piece? _remembered;static bool _busy;static ZNetScene? _scene;static float _nextCleanup;
    static readonly Dictionary<string,Piece> Seeds=new(StringComparer.Ordinal);const string SeedKey="RestlessReplant";
    internal static readonly Dictionary<ZDOID,ItemDrop> PickedDrops=new();
    static bool Ours(Pickable p)=>true;static void Remember(Piece p)=>_remembered=p;static void Bulk(Pickable p,Humanoid h){}
}
public enum WorksOrderMode{Make,Keep}
public class WorksOrder{public int Id,Count,Ready,Collected,Remaining,InProduction,Available;public long PlayerId;public string Output="ore";public WorksOrderMode Mode;}
public class WorksRecipe{}
partial class WorksHarness {
    internal class Ledger{public List<WorksOrder> Orders=new();}
    internal static Ledger Current=new();internal static List<long> Planned=new(),Paid=new();const int LoadsPerTick=4;
    static Ledger Read(ZNetView v)=>Current;static void Write(ZNetView v,Ledger l){}
    static List<int> Scan(Vector3 o,bool b,ZNetView v)=>new();static Dictionary<string,int> Production(List<int> hits)=>new();
    static Dictionary<string,int> Stock(Vector3 o,long actor,Ledger l){Planned.Add(actor);return new();}
    static int Reserved(Ledger l,string output)=>0;static WorksRecipe Find(string s)=>new();
    static bool LoadOne(WorksRecipe r,List<int> h,Ledger l,Vector3 o,long actor,Dictionary<string,int> s){Paid.Add(actor);return true;}
    static void FuelQueued(List<int> h,Ledger l,Vector3 o){}
}
public enum KitchenStepState{Ready,Queued,Missing}
public enum KitchenStationKind{Rack,Oven,Fermenter,Cauldron,PrepTable,MeadKettle}
public class KitchenStep{public KitchenStepState State=KitchenStepState.Ready;public KitchenStationKind Station=KitchenStationKind.Cauldron;}
public class KitchenOrder{public int Count=1,Collected,Ready;public long PlayerId;public string Feast="food";}
public class CraftingStation:Component {public ZNetView View=new();}
partial class CookHarness {
    internal class Ledger{public List<KitchenOrder> Orders=new();public List<int> Work=new(),Taps=new();}
    internal static Ledger Current=new();internal static List<long> Planned=new(),Paid=new();const int LoadsPerTick=4,CraftsPerTick=4;
    static ZNetView View(CraftingStation t)=>t.View;static Ledger Read(ZNetView v)=>Current;static void Write(ZNetView v,Ledger l){}
    static int Outstanding(KitchenOrder o)=>o.Count-o.Collected;static void Deliver(KitchenOrder o,Ledger l){}
    static void EnsureConversions(){}static List<int> Scan(Vector3 o,bool b,ZNetView v)=>new();
    static void AdvanceWork(CraftingStation t,List<int> h,Ledger l,float e){}static void RecoverTaps(CraftingStation t,List<int> h,Ledger l){}static void CollectFinished(CraftingStation t,List<int> h,Ledger l){}
    static void Fuel(List<int> h,Ledger l,Vector3 o,long a){}
    static Dictionary<string,int> Stock(Vector3 o,long actor,Ledger l){Planned.Add(actor);return new();}
    static Dictionary<string,int> OnStations(Vector3 o,List<int> h)=>new();static void IncludeWork(Ledger l,Dictionary<string,int> c){}
    static List<KitchenStep> Expand(string s,int n,int d,string p,Dictionary<string,int> stock)=>new(){new()};
    static void Assign(CraftingStation t,List<KitchenStep>s,Dictionary<string,int>f,Dictionary<string,int>c,long a,int r,List<int>h){}
    static bool Load(List<int>h,KitchenStep s,Ledger l,Vector3 o,long a){Paid.Add(a);return true;}
    static object PreparationStation(List<int>h,KitchenStep s,Ledger l)=>new();
    static bool PullOne(KitchenStep s,Ledger l,Vector3 o,long a){Paid.Add(a);return true;}
    static bool Pay(KitchenStep s,Ledger l)=>true;static void StartWork(Ledger l,KitchenOrder o,KitchenStep s,object station){}
}
namespace RestlessQoL.Storage {
    public static class ProductionLease{public static void SetActive(ZNetView v,bool b){}}
    public class NearbyLot{public int Count;public NearbyLot(string place,ItemDrop.ItemData item,int count){Count=count;}}
    public static class NearbyStorage{public static bool EnsureDropPrefab(ItemDrop.ItemData i){i.m_dropPrefab??=new(){name="Wood"};return true;}}
}
public record struct ZDOID(long Id);
namespace UnityEngine {
    public struct Vector3{public float x,y,z;public Vector3(float x,float y=0,float z=0){this.x=x;this.y=y;this.z=z;}public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);public static float Distance(Vector3 a,Vector3 b)=>MathF.Sqrt((a-b).sqrMagnitude);}
    public struct Quaternion{}
    public class Transform{public Vector3 position;public Quaternion rotation;}
    public static class Time{public static float unscaledTime;}
    public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int RoundToInt(float x)=>(int)MathF.Round(x);}
}

