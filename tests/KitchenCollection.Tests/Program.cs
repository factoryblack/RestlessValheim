using RestlessCook;
KitchenRun.TestCollection();

namespace RestlessCook
{
    // Native boundary substitutes. The collection loop is production source.
    internal sealed class View
    {
        internal bool Valid = true, Owner = true;
        internal bool IsValid() => Valid;
        internal bool IsOwner() => Owner;
    }
    internal sealed class CookingStation
    {
        internal enum Status { NotDone, Done, Burnt }
        internal View? m_nview = new();
        internal string[] m_slots = { "CookedMeat", "RawMeat", "Coal", "Unwanted", "" };
        internal Status[] States = { Status.Done, Status.NotDone, Status.Burnt, Status.Done, Status.Done };
        internal int Clears, Visuals;
        internal long Sender = -1;
        internal bool FailClear;
        internal void GetSlot(int slot, out string name, out float time, out Status status, out bool cheated)
        { name = m_slots[slot]; time = 10; status = States[slot]; cheated = false; }
        internal void SetSlot(int slot, string name, float time, Status status, bool cheated, long sender)
        {
            if (FailClear) throw new InvalidOperationException("Native clear failed");
            if (time != 0 || cheated) throw new Exception("Invalid clear arguments");
            m_slots[slot] = name; States[slot] = status; Sender = sender; Clears++;
        }
        internal void SetSlotVisual(int slot, string name, bool cheated, Status status) => Visuals++;
    }
    internal static partial class KitchenRun
    {
        private sealed class Ledger { internal int Credits; }
        private static string Clean(string name) => name;
        private static bool Wanted(Ledger ledger, string product) => product == "CookedMeat";
        private static void CreditFinished(Ledger ledger, string product, int count) => ledger.Credits += count;

        internal static void TestCollection()
        {
            var checks = 0;
            void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
            var rack = new CookingStation(); var ledger = new Ledger();
            CollectRack(rack, ledger);
            Check(ledger.Credits == 1 && rack.Clears == 1, "Collect exactly one wanted finished item");
            Check(rack.m_slots[0] == "" && rack.States[0] == CookingStation.Status.NotDone, "Clear the native slot");
            Check(rack.Sender == 0 && rack.Visuals == 1, "Use native clear sender and update visuals");
            Check(rack.m_slots[1] == "RawMeat" && rack.m_slots[2] == "Coal" && rack.m_slots[3] == "Unwanted", "Leave raw, burnt and unrelated output alone");
            CollectRack(rack, ledger);
            Check(ledger.Credits == 1 && rack.Clears == 1, "Repeated tick must not duplicate output");
            rack = new CookingStation(); rack.m_nview!.Owner = false;
            CollectRack(rack, ledger);
            Check(rack.Clears == 0 && ledger.Credits == 1, "Client must not collect a remote-owned rack");
            rack.m_nview.Owner = true; rack.m_nview.Valid = false;
            CollectRack(rack, ledger);
            Check(rack.Clears == 0, "Invalid rack must not be credited");
            rack.m_nview = null; CollectRack(rack, ledger);
            Check(rack.Clears == 0, "Missing view must be ignored");
            rack = new CookingStation { FailClear = true };
            try { CollectRack(rack, ledger); throw new Exception("Expected native failure"); }
            catch (InvalidOperationException) { }
            Check(ledger.Credits == 1 && rack.m_slots[0] == "CookedMeat", "Failed clear must not credit an item still on the rack");
            rack.FailClear = false; CollectRack(rack, ledger);
            Check(ledger.Credits == 2 && rack.Clears == 1, "Retry after a failed clear collects once");
            Console.WriteLine($"{checks} kitchen rack collection regressions passed.");
        }
    }
}
