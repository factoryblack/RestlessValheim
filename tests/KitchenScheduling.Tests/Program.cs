using RestlessCook;
KitchenRun.TestScheduling();

// Production graph, planning, scheduling, payment and native acceptance code
// are linked directly. Only Unity storage/machine/time boundaries are replaced.
namespace UnityEngine { public struct Vector3 {} }
namespace RestlessCook
{
internal class Transform { internal UnityEngine.Vector3 position=new(); }
internal class CraftingStation { internal Transform transform=new(); }
internal class ViewStub { internal bool Owner=true,Valid=true; internal bool IsOwner()=>Owner; internal bool IsValid()=>Valid; }
internal class OvenStub
{
    internal List<string> Queue=new(); internal int m_maxOre=4,m_maxFuel=1; internal bool Reject; internal float Fuel=1;
    internal int GetQueueSize()=>Queue.Count; internal float GetFuel()=>Fuel;
    internal void QueueOre(string raw,bool cheat) { if(!Reject&&Queue.Count<m_maxOre) Queue.Add(raw); }
}
internal class RackStub
{
    internal string[] Slots={"","","",""}; internal bool m_requireFire=true,m_useFuel,Fire=true;
    internal int GetFreeSlot()=>Array.FindIndex(Slots,s=>s==""); internal bool IsFireLit()=>Fire; internal float GetFuel()=>1;
    internal void RPC_AddItem(long sender,string raw,bool cheat) { var slot=GetFreeSlot();if(slot>=0)Slots[slot]=raw; }
    internal void GetSlot(int slot,out string actual,out float time,out int status,out bool cheat) {actual=Slots[slot];time=0;status=0;cheat=false;}
}
internal class Fermenter
{
    internal enum Status { Empty,Fermenting }
    internal bool m_exposed,m_hasRoof=true; internal int Content;
    internal Status GetStatus()=>Content==0?Status.Empty:Status.Fermenting;
    internal void RPC_AddItem(long sender,int hash,bool cheat) {if(Content==0)Content=hash;} internal int GetContent()=>Content;
}
internal static class Hash { internal static int GetStableHashCode(this string value)=>value.GetHashCode(); }
internal static partial class KitchenRun
{
    private class Hit
    {
        internal string Id=Guid.NewGuid().ToString(); internal KitchenStationInfo Info=new();
        internal ViewStub View=new(); internal OvenStub? Oven; internal RackStub? Rack; internal Fermenter? Fermenter;
    }
    private class Tap { internal string StationId=""; }
    private class Work { internal string Output="",StationId="";internal int Made; }
    private class Ledger { internal int NextOrder;internal Dictionary<string,int> Pantry=new();internal List<KitchenOrder> Orders=new();internal List<Work> Work=new();internal List<Tap> Taps=new(); }
    private class Conversion { internal string From="";internal KitchenStationKind Kind;internal int Amount=1; }
    private static Dictionary<string,Conversion> Products=new();
    private static Dictionary<string,CookRow> ByOutput=new();
    private static Dictionary<string,int> Warehouse=new();
    private const int LoadsPerTick=4,CraftsPerTick=3;
    private static void EnsureConversions() {}
    private static string Label(string item)=>item;
    private static List<Hit> Scan(UnityEngine.Vector3 origin,bool claim,ViewStub view)=>new();
    private static ViewStub View(CraftingStation table)=>new();
    private static string? BlockReason(CraftingStation table,List<Hit> hits,KitchenStep step)=>null;
    private static string StationId(Hit hit)=>hit.Id;
    private static bool Accepts(Hit hit,string output)=>Products.TryGetValue(output,out var c)&&c.Kind==hit.Info.Kind;
    private static Dictionary<string,int> Stock(UnityEngine.Vector3 origin,long player,Ledger ledger)
    {
        var stock=new Dictionary<string,int>(Warehouse);foreach(var p in ledger.Pantry){stock.TryGetValue(p.Key,out var n);stock[p.Key]=n+p.Value;}return stock;
    }
    private static void PullInto(Ledger ledger,string raw,int amount,UnityEngine.Vector3 origin,long player)
    {
        Warehouse.TryGetValue(raw,out var have);var take=Math.Min(amount,have);Warehouse[raw]=have-take;
        ledger.Pantry.TryGetValue(raw,out var held);ledger.Pantry[raw]=held+take;
    }
    private static Hit? PreparationStation(List<Hit> hits,KitchenStep step,Ledger ledger)
        =>hits.Find(h=>h.Info.Kind==step.Station&&!ledger.Work.Exists(w=>w.StationId==h.Id));
    private static void StartWork(Ledger ledger,KitchenOrder order,KitchenStep step,Hit station)
        =>ledger.Work.Add(new(){Output=step.Output,Made=OutputAmount(step),StationId=station.Id});
    private static void IncludeWork(Ledger ledger,Dictionary<string,int> cooking)
    {foreach(var w in ledger.Work){cooking.TryGetValue(w.Output,out var n);cooking[w.Output]=n+w.Made;}}
    private static int Outstanding(KitchenOrder order)=>order.Count-order.Ready-order.Collected;
    private static void Deliver(KitchenOrder order,Ledger ledger) {}
    private static Dictionary<string,int> OnStations(UnityEngine.Vector3 origin,List<Hit> hits)
    {
        var cooking=new Dictionary<string,int>();
        foreach(var h in hits)
        {
            var raws=h.Oven!=null?h.Oven.Queue:h.Rack!=null?h.Rack.Slots.Where(s=>s!="").ToList():new List<string>();
            if(h.Fermenter!=null&&h.Fermenter.Content!=0)raws.Add(Products.Values.First(c=>c.From.GetStableHashCode()==h.Fermenter.Content).From);
            foreach(var raw in raws){var c=Products.First(p=>p.Value.From==raw&&p.Value.Kind==h.Info.Kind);cooking.TryGetValue(c.Key,out var n);cooking[c.Key]=n+c.Value.Amount;}
        }
        return cooking;
    }
    private static Hit Oven(int slots=4)=>new(){Info=new(){Kind=KitchenStationKind.Oven,Free=slots},Oven=new(){m_maxOre=slots}};
    private static Hit Prep()=>new(){Info=new(){Kind=KitchenStationKind.PrepTable,Free=1}};
    private static void ConversionOf(string output,string input,KitchenStationKind kind=KitchenStationKind.Oven,int amount=1)
        =>Products[output]=new(){From=input,Kind=kind,Amount=amount};
    private static void Recipe(string output,int amount,params (string,int)[] inputs)
    {
        var row=new CookRow{Id=output,Prefab=output,Name=output,Station="piece_preptable",OutputAmount=amount};
        foreach(var i in inputs)row.Uses.Add(new(){Item=i.Item1,Amount=i.Item2});ByOutput[output]=row;
    }
    private static Ledger Orders(params (string,int)[] orders)
    {var l=new Ledger();foreach(var o in orders)l.Orders.Add(new(){Id=l.Orders.Count+1,Feast=o.Item1,Count=o.Item2,PlayerId=1});return l;}
    private static void Reset(){Warehouse.Clear();Products.Clear();ByOutput.Clear();}
    internal static void TestScheduling()
    {
        var checks=0;void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
        var table=new CraftingStation();var origin=new UnityEngine.Vector3();
        Reset();ConversionOf("Pie","RawPie");Recipe("RawPie",1,("CookedMeat",2));Warehouse["RawPie"]=5;
        var hits=new List<Hit>{Oven(),Oven(),Oven()};var ledger=Orders(("Pie",20));
        Schedule(table,hits,ledger,origin);
        Check(hits.Sum(h=>h.Oven!.Queue.Count)==4,"Partial raw pie stock must load without waiting for unavailable meat");
        Check(hits.Select(h=>h.Oven!.Queue.Count).SequenceEqual(new[]{2,1,1}),"Four loads must spread across three available ovens");
        Check(Warehouse["RawPie"]==1&&!ledger.Pantry.ContainsKey("RawPie"),"Only accepted native loads may spend prepared inputs");
        Schedule(table,hits,ledger,origin);
        Check(hits.Sum(h=>h.Oven!.Queue.Count)==5,"Exhausted partial stock must stop cleanly");
        Reset();ConversionOf("Pie","RawPie");Recipe("RawPie",1,("CookedMeat",2));
        var steps=Expand("Pie",20,0,"");Assign(table,steps,new(){{"RawPie",5},{"CookedMeat",30}},new(),1,0,hits);
        Check(steps[0].State==KitchenStepState.Queued&&Incoming(steps,"RawPie")==20,"Prepared plus promised output must include both exactly once");
        Check(steps.Single(s=>s.Output=="CookedMeat").Need==30,"Five prepared pies must reduce cooked-meat demand from forty to thirty");
        Check(InputCost(steps[1],steps[1].Uses[0])==2,"Partial allocation must retain the native two-meat recipe cost");
        steps=Expand("Pie",20,0,"");Assign(table,steps,new(){{"RawPie",20}},new(),1,0,hits);
        Check(Incoming(steps,"CookedMeat")==0,"Ingredients covered by prepared pies are not real stock for another branch");
        Reset();ConversionOf("Pie","RawPie");Warehouse["RawPie"]=20;hits=new(){Oven(),Oven(),Oven()};ledger=Orders(("Pie",2));
        Schedule(table,hits,ledger,origin);Schedule(table,hits,ledger,origin);
        Check(hits.Sum(h=>h.Oven!.Queue.Count)==2&&Warehouse["RawPie"]==18,"Repeated rounds and ticks must not exceed remaining dish demand");
        Reset();ConversionOf("Mead","Base",KitchenStationKind.Fermenter,6);Warehouse["Base"]=10;
        hits=new(){new(){Info=new(){Kind=KitchenStationKind.Fermenter,Free=1},Fermenter=new()},new(){Info=new(){Kind=KitchenStationKind.Fermenter,Free=1},Fermenter=new()},new(){Info=new(){Kind=KitchenStationKind.Fermenter,Free=1},Fermenter=new()}};ledger=Orders(("Mead",7));
        Schedule(table,hits,ledger,origin);Schedule(table,hits,ledger,origin);
        Check(hits.Count(h=>h.Fermenter!.Content!=0)==2&&Warehouse["Base"]==8,"Seven meads need two six-output batches, not seven bases or a third batch");
        Reset();ConversionOf("Bread","Dough");Recipe("Dough",2,("Flour",10));Warehouse["Flour"]=100;
        hits=new(){Oven(),Prep(),Prep(),Prep()};ledger=Orders(("Bread",3));Schedule(table,hits,ledger,origin);
        Check(ledger.Work.Count==2&&ledger.Work.Sum(w=>w.Made)==4&&Warehouse["Flour"]==80,"Three bread must prepare exactly two native two-dough batches at ten flour each");
        Reset();ConversionOf("Pie","SharedRaw");ConversionOf("Bread","SharedRaw");Warehouse["SharedRaw"]=1;
        hits=new(){Oven(16)};ledger=Orders(("Pie",1),("Bread",1));Schedule(table,hits,ledger,origin);
        Check(hits[0].Oven!.Queue.Count==1&&Warehouse["SharedRaw"]==0,"Competing orders must not both spend a single shared input");
        Reset();hits=new(){Oven(32)};ledger=new();for(var i=0;i<8;i++){ConversionOf("Dish"+i,"Raw"+i);Warehouse["Raw"+i]=20;ledger.Orders.Add(new(){Id=i+1,Feast="Dish"+i,Count=20,PlayerId=1});}
        Schedule(table,hits,ledger,origin);Schedule(table,hits,ledger,origin);
        Check(hits[0].Oven!.Queue.Distinct().Count()==8,"All eight competing orders must get a turn across two bounded ticks");
        Reset();ConversionOf("CookedMeat","Meat",KitchenStationKind.Rack);Warehouse["Meat"]=20;
        var rack=new Hit{Info=new(){Kind=KitchenStationKind.Rack,Free=3,Cooking=1},Rack=new()};rack.Rack.Slots[0]="Meat";hits=new(){rack};ledger=Orders(("CookedMeat",2));Schedule(table,hits,ledger,origin);
        Check(rack.Rack.Slots.Count(s=>s!="")==2&&Warehouse["Meat"]==19,"Existing cooking meat must reduce new loads without counting it twice");
        Reset();ConversionOf("Supreme","RawSupreme",KitchenStationKind.Rack);ConversionOf("CookedHare","Hare",KitchenStationKind.Rack);Recipe("RawSupreme",1,("CookedHare",1),("Puffs",3));Warehouse["RawSupreme"]=5;Warehouse["Hare"]=20;Warehouse["Puffs"]=60;
        rack=new(){Info=new(){Kind=KitchenStationKind.Rack,Free=4},Rack=new()};hits=new(){rack,Prep()};ledger=Orders(("Supreme",20));Schedule(table,hits,ledger,origin);
        Check(rack.Rack!.Slots.Count(s=>s=="RawSupreme")==4,"Prepared hare dishes must start before consuming more raw meat");
        rack.Rack.Slots=new[]{"","","",""};rack.Info.Free=4;rack.Info.Cooking=0;Warehouse["RawSupreme"]=0;Schedule(table,hits,ledger,origin);
        Check(rack.Rack.Slots.Count(s=>s=="Hare")==4,"Hare dependencies must use all four hooks when no prepared dish can start");
        steps=Expand("Supreme",20,0,"");Assign(table,steps,new(){{"RawSupreme",5},{"Hare",20},{"Puffs",60}},new(){{"CookedHare",4}},1,0,hits);
        Check(steps.Single(s=>s.Output=="CookedHare").Need==15&&steps.Single(s=>s.Output=="Hare").Need==11,"Prepared hare dishes and in-flight meat must both reduce upstream demand");
        Reset();Recipe("RawPie",1,("Meat",2),("Berry",1));Warehouse["Meat"]=1;Warehouse["Berry"]=10;hits=new(){Prep()};ledger=Orders(("RawPie",10));Schedule(table,hits,ledger,origin);
        Check(ledger.Work.Count==0&&Warehouse["Meat"]==1&&Warehouse["Berry"]==10,"Less than one recipe's cost must not start or debit preparation");
        Reset();ConversionOf("Pie","RawPie");Warehouse["RawPie"]=1;var bad=Oven();bad.Oven!.Reject=true;var good=Oven();hits=new(){bad,good};ledger=Orders(("Pie",1));Schedule(table,hits,ledger,origin);
        Check(bad.Oven.Queue.Count==0&&good.Oven!.Queue.Count==1&&Warehouse["RawPie"]==0,"Rejected first machine must fall through and commit only the accepted load");
        Reset();ConversionOf("Pie","RawPie");Warehouse["RawPie"]=3;var blocked=Oven();blocked.Info.Block="Smoke is blocked";good=Oven();hits=new(){blocked,good};ledger=Orders(("Pie",1));Schedule(table,hits,ledger,origin);
        Check(blocked.Oven!.Queue.Count==0&&good.Oven!.Queue.Count==1,"Dispatch must not load a blocked oven when a usable one exists");
        Console.WriteLine($"{checks} production kitchen scheduling regressions passed.");
    }
}
}
