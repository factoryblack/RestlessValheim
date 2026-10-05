// Minimal game boundary substitutes. The job implementation itself is linked unchanged.
using System.Reflection;
namespace HarmonyLib { internal static class AccessTools { internal static FieldInfo? Field(Type t,string n)=>t.GetField(n); } }
namespace UnityEngine { internal static class Mathf { internal static float Clamp(float v,float a,float b)=>Math.Clamp(v,a,b); } }
namespace RestlessCook
{
    internal class InventoryGui { public static InventoryGui? instance; public float m_craftDuration=2; }
    internal class CraftingStation { public bool Available=true; public Transform transform=new(); }
    internal class Transform { public object position=new(); }
    internal class CookingStation
    {
        internal enum Status { NotDone,Done }
        internal int[] m_slots=Array.Empty<int>();
        internal void GetSlot(int i,out string raw,out float time,out Status state,out int unused)
        { raw="";time=0;state=Status.NotDone;unused=0; }
        internal Conversion? GetItemConversion(string raw)=>null;
    }
    internal class Conversion { internal float m_cookTime; }
    internal enum KitchenStationKind { None,Cauldron,PrepTable,Rack,Oven,MeadKettle }
    internal enum KitchenStepState { Prepared,Ready,Queued,Cooking,Missing,Blocked }
    internal class KitchenUse { internal string Item=""; internal int Amount; }
    internal class KitchenStep
    {
        internal string Output="",Note="",ActiveStation=""; internal int StationLevel=1,Depth,Need,Have,Cooking,OutputCount=1;
        internal float ElapsedSeconds,DurationSeconds; internal KitchenStationKind Station; internal KitchenStepState State;
        internal List<KitchenUse> Uses=new();
    }
    internal class KitchenOrder { internal int Id,Ready,Count,Collected; }
    internal class CookRow { internal float PreparationSeconds; }
    internal static partial class KitchenRun
    {
        private class Hit { internal CookingStation? Rack; internal string Id="table"; internal Info Info=new(); }
        private class Info { internal string Name="Preparation table"; }
        private class TapReceipt { internal string Output=""; internal int Amount; }
        private class Ledger
        {
            internal List<Work> Work=new(); internal List<KitchenOrder> Orders=new(); internal List<TapReceipt> Taps=new();
            internal Dictionary<string,int> Pantry=new();
        }
        private static Dictionary<string,CookRow> ByOutput=new();
        private static Hit? PreparationStation(List<Hit> hits,KitchenStep step,Ledger? ledger)=>hits.Find(h=>ledger==null||!Busy(ledger,h.Id));
        private static string StationId(Hit h)=>h.Id;
        private static bool WorkReady(CraftingStation table,List<Hit> hits,Work work,bool requireOwner=true)=>table.Available&&hits.Exists(h=>h.Id==work.StationId);
        private static void PaintNativeTiming(List<Hit> hits,KitchenStep step) { }
        private static List<Hit> Scan(object position,bool claim=false,object? controller=null)=>new();
        private static object View(CraftingStation table)=>table;
        private static string ProductOf(CookingStation rack,string raw)=>raw;
        private static int OutputAmount(KitchenStep s)=>s.OutputCount;
        private static int CraftsFor(int need,int amount)=>need<=0?0:(need+amount-1)/amount;
        private static void Add(Ledger l,string item,int amount) { l.Pantry.TryGetValue(item,out var n);l.Pantry[item]=n+amount; }
        internal static void TestJobs()
        {
            void Check(bool ok,string message) { if(!ok)throw new Exception(message); }
            var table=new CraftingStation(); var station=new Hit(); var hits=new List<Hit>{station}; var ledger=new Ledger();
            var order=new KitchenOrder{Id=1,Count=1}; ledger.Orders.Add(order);
            var step=new KitchenStep{Output="Feast",Need=1,Station=KitchenStationKind.PrepTable};
            step.Uses.Add(new KitchenUse{Item="Meal",Amount=3});
            StartWork(ledger,order,step,station);
            Check(ledger.Work.Count==1 && Busy(ledger,station.Id),"Job must occupy its station");
            Check(ledger.Pantry.Count==0,"Starting a job must not award instant food");
            AdvanceWork(table,hits,ledger,1);
            Check(ledger.Work[0].Elapsed==1 && order.Ready==0,"First tick must remain in progress");
            var persisted=ReadWork(WorkLine(ledger.Work[0]).TrimEnd('\n').Split('\t'));
            Check(persisted!=null && persisted.StationId==station.Id && persisted.Elapsed==1 && persisted.Inputs["Meal"]==3,"Reload must preserve time and paid inputs");
            ledger.Work.Clear();ledger.Work.Add(persisted!);
            table.Available=false;AdvanceWork(table,hits,ledger,1);
            Check(ledger.Work[0].Elapsed==1,"Unavailable station must pause");
            table.Available=true;AdvanceWork(table,hits,ledger,1);
            Check(ledger.Work.Count==0 && ledger.Pantry["Feast"]==1 && order.Ready==1,"Resumed job must complete and mark output ready");
            AdvanceWork(table,hits,ledger,1);
            Check(ledger.Pantry["Feast"]==1,"Finished job must not award twice");
            step.Depth=1;step.Output="Meal";step.Uses[0]=new KitchenUse{Item="Raw",Amount=2};
            StartWork(ledger,order,step,station);AdvanceWork(table,hits,ledger,2);
            Check(ledger.Pantry["Meal"]==1 && order.Ready==1,"Intermediate completion must stay in pantry without marking the feast ready");
            StartWork(ledger,order,step,station);ledger.Orders.Clear();AdvanceWork(table,hits,ledger,1);
            Check(ledger.Work.Count==0 && ledger.Pantry["Raw"]==2,"Orphaned job must refund paid ingredients once");
            ledger.Orders.Add(order); StartWork(ledger,order,step,station);
            hits.Clear(); hits.Add(new Hit{Id="different cauldron"}); AdvanceWork(table,hits,ledger,2);
            Check(ledger.Work.Count==1 && ledger.Work[0].Elapsed==0,"A replacement station must not finish a bound job");
            Check(!Busy(ledger,"different cauldron"),"Another physical station of the same kind must have its own capacity");
            var other=hits[0]; StartWork(ledger,order,step,other); AdvanceWork(table,hits,ledger,2);
            Check(ledger.Work.Count==1 && ledger.Work[0].StationId==station.Id,"Each station must advance only its own work");
            var old=WorkLine(ledger.Work[0]).TrimEnd('\n').Split('\t').Take(10).ToArray();
            var legacy=ReadWork(old); Check(legacy!=null && legacy.StationId=="","Old jobs must remain readable");
            ledger.Work.Clear();ledger.Work.Add(legacy!); AdvanceWork(table,hits,ledger,1);
            Check(ledger.Work[0].StationId==other.Id && ledger.Work[0].Elapsed==1,"Old jobs must bind once when resumed");
            Console.WriteLine("14 timed-job lifecycle regressions passed");
        }
    }
}

