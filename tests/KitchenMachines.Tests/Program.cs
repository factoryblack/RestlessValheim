using RestlessCook;
KitchenRun.TestMachines();

namespace RestlessCook
{
    // Game-boundary substitutes can reject a load or change capacity between
    // calls. The ingredient commit and receipt implementations are production.
    internal static class Hash { internal static int GetStableHashCode(this string s)=>s.GetHashCode(); }
    internal class View { internal bool Owner=true,Valid=true; internal bool IsOwner()=>Owner; internal bool IsValid()=>Valid; }
    internal class Info { internal int Free=99; }
    internal class Rack
    {
        internal string Slot=""; internal bool Reject,m_requireFire,m_useFuel,Fire=true; internal float Fuel=1;
        internal int GetFreeSlot()=>Slot==""?0:-1; internal bool IsFireLit()=>Fire; internal float GetFuel()=>Fuel;
        internal void RPC_AddItem(long sender,string raw,bool cheat) { if(!Reject&&Slot=="") Slot=raw; }
        internal void GetSlot(int slot,out string raw,out float time,out int status,out bool cheat) { raw=Slot;time=0;status=0;cheat=false; }
    }
    internal class Oven
    {
        internal int Count,m_maxOre=1,m_maxFuel=1; internal bool Reject; internal float Fuel=1;
        internal int GetQueueSize()=>Count; internal float GetFuel()=>Fuel;
        internal void QueueOre(string raw,bool cheat) { if(!Reject&&Count<m_maxOre)Count++; }
    }
    internal class Fermenter
    {
        internal enum Status { Empty,Fermenting }
        internal bool m_exposed,m_hasRoof=true,Reject; internal int Content;
        internal Status GetStatus()=>Content==0?Status.Empty:Status.Fermenting;
        internal void RPC_AddItem(long sender,int raw,bool cheat) { if(!Reject&&Content==0) Content=raw; }
        internal int GetContent()=>Content;
    }
    internal static partial class KitchenRun
    {
        private class Hit { internal View? View=new();internal Rack? Rack;internal Oven? Oven;internal Fermenter? Fermenter;internal Info Info=new();internal string Id="machine"; }
        private class Ledger { internal Dictionary<string,int> Pantry=new();internal List<TapReceipt> Taps=new(); }
        private static string StationId(Hit hit)=>hit.Id;
        private static void CreditFinished(Ledger ledger,string item,int amount) { ledger.Pantry.TryGetValue(item,out var held);ledger.Pantry[item]=held+amount; }
        internal static void TestMachines()
        {
            var checks=0; void Check(bool ok,string message) { checks++;if(!ok)throw new Exception(message); }
            var ledger=new Ledger();ledger.Pantry["Meat"]=3;var rack=new Rack();var hit=new Hit{Rack=rack};
            Check(TryLoadRaw(hit,"Meat",ledger)&&ledger.Pantry["Meat"]==2,"Accepted rack load must spend exactly one ingredient");
            Check(!TryLoadRaw(hit,"Meat",ledger)&&ledger.Pantry["Meat"]==2,"Full rack must not spend again despite stale free-slot snapshot");
            rack.Slot="";rack.Reject=true;Check(!TryLoadRaw(hit,"Meat",ledger)&&ledger.Pantry["Meat"]==2,"Rejected native RPC must retain paid input");
            rack.Reject=false;rack.m_requireFire=true;rack.Fire=false;Check(!TryLoadRaw(hit,"Meat",ledger),"Unlit rack must not load");
            rack.Fire=true;rack.m_useFuel=true;rack.Fuel=0;Check(!TryLoadRaw(hit,"Meat",ledger),"Unfuelled rack must not load");
            rack.Fuel=1;hit.View!.Owner=false;Check(!TryLoadRaw(hit,"Meat",ledger)&&rack.Slot=="","Non-owner must never mutate a native slot or debit ingredients");
            ledger.Pantry["Dough"]=2;var oven=new Oven();hit=new Hit{Oven=oven};
            Check(TryLoadRaw(hit,"Dough",ledger)&&ledger.Pantry["Dough"]==1,"Accepted oven load must commit once");
            Check(!TryLoadRaw(hit,"Dough",ledger)&&ledger.Pantry["Dough"]==1,"Full oven must retain remaining dough");
            oven.Count=0;oven.Reject=true;Check(!TryLoadRaw(hit,"Dough",ledger)&&ledger.Pantry["Dough"]==1,"Rejected oven insertion must not pay");
            ledger.Pantry["Base"]=2;var fermenter=new Fermenter();hit=new Hit{Fermenter=fermenter};
            Check(TryLoadRaw(hit,"Base",ledger)&&ledger.Pantry["Base"]==1,"Fermenter must consume one base after native acceptance");
            Check(!TryLoadRaw(hit,"Base",ledger)&&ledger.Pantry["Base"]==1,"Occupied fermenter must not consume a second base");
            fermenter.Content=0;fermenter.m_exposed=true;Check(!TryLoadRaw(hit,"Base",ledger),"Exposed fermenter must wait");
            fermenter.m_exposed=false;fermenter.m_hasRoof=false;Check(!TryLoadRaw(hit,"Base",ledger),"Unroofed fermenter must wait");
            fermenter.m_hasRoof=true;var receipt=new TapReceipt{StationId=hit.Id,Output="Mead",Amount=6,Due=100,Committed=true};ledger.Taps.Add(receipt);
            Check(!TryLoadRaw(hit,"Base",ledger),"A barrel being tapped must not start a second batch");
            var saved=ReadTap(TapLine(receipt).TrimEnd('\n').Split('\t'));
            Check(saved!=null&&saved.StationId==hit.Id&&saved.Amount==6&&saved.Due==100&&saved.Committed,"Tap handoff must retain machine, yield and native delay across saves");
            ledger.Taps.Clear();ledger.Taps.Add(saved!);
            Check(FinishTapReceipt(ledger,saved!)&&ledger.Pantry["Mead"]==6,"Persisted tap must finish into pantry");
            Check(!FinishTapReceipt(ledger,saved!)&&ledger.Pantry["Mead"]==6,"Native callback and recovery must not both award the batch");
            Check(ReadTap(new[]{"T","machine","Mead","-1","100"})==null,"Corrupt negative batch must not enter ledger");
            Console.WriteLine($"{checks} physical kitchen machine regressions passed.");
        }
    }
}
