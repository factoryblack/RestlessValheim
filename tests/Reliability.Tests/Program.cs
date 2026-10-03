using RestlessQoL.Storage;

static void Equal<T>(T expected, T actual, string name)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"{name}: expected {expected}, got {actual}");
}

// Run real history methods through delayed, reordered and duplicate delivery schedules.
var book = new TransferBook();
var chest = 20;
var player = 0;
TransferBook.Entry Reserve()
{
    chest -= 4;
    return new TransferBook.Entry { Count = 4, Payload = "exact-stack-payload", State = TransferBook.Phase.Held };
}
var row = book.Prepare(42, 1, "W", Reserve);
book.Prepare(42, 1, "W", Reserve);
Equal(16, chest, "duplicate request removes once");
Equal(4, row.Count, "duplicate request repeats original count");
// Ownership hand-off reconstructs from the serialized chest, not static dictionaries.
book = TransferBook.Load(book.Save());
Equal("exact-stack-payload", book.Find(42, 1, "W")!.Payload, "reservation survives hand-off");
player += 4;
book.Resolve(42, 1, "W", true, _ => { });
book = TransferBook.Load(book.Save());
book.Resolve(42, 1, "W", true, _ => throw new Exception("duplicate ack applied"));
book.Resolve(42, 1, "W", false, _ => throw new Exception("late cancellation refunded delivered items"));
Equal(20, chest + player, "late cancellation conserves stock");

// Cancellation may beat a request to its owner. A late packet cannot reopen it.
book.Resolve(42, 2, "W", false, _ => throw new Exception("unknown cancel has no stock"));
book.Prepare(42, 2, "W", () => throw new Exception("request after cancellation removed stock"));
Equal(TransferBook.Phase.Cancelled, book.Find(42, 2, "W")!.State, "early cancellation stays closed");

// Same character/sequence on different chests has independent histories.
var secondChest = new TransferBook();
secondChest.Prepare(42, 1, "W", Reserve);
Equal(12, chest, "transaction scope includes chest");
secondChest.Resolve(42, 1, "W", false, e => chest += e.Count);
Equal(20, chest + player, "cancellation restores once");
secondChest.Resolve(42, 1, "W", false, _ => throw new Exception("duplicate cancellation restored twice"));

// History compaction retains unresolved payloads and rejects evicted old requests.
var history = new TransferBook();
history.Prepare(7, 1, "P", () => new TransferBook.Entry { State = TransferBook.Phase.Held, Count = 1, Payload = "held" });
for (var id = 2; id < 400; id++) history.Resolve(7, id, "P", false, _ => { });
history = TransferBook.Load(history.Save());
Equal("held", history.Find(7, 1, "P")!.Payload, "compaction retains unresolved payload");
history.Prepare(7, 2, "P", () => throw new Exception("evicted request reopened"));
Equal(TransferBook.Phase.Cancelled, history.Find(7, 2, "P")!.State, "retired request refused");

// Exercise the real lease adapter against ZDO stubs, including another peer taking ownership.
ZDOMan.instance = new ZDOMan();
var a = new ZNetView(10, true); var b = new ZNetView(20, true); var machine = new ZNetView(30, false);
ZDOMan.instance.Add(a.GetZDO()); ZDOMan.instance.Add(b.GetZDO());
ProductionLease.SetActive(a, true); ProductionLease.SetActive(b, true);
Equal(true, ProductionLease.Acquire(machine, a), "first controller acquires");
Equal(false, ProductionLease.Acquire(machine, b), "active overlapping controller refused");
Equal(1, machine.Claims, "refused controller does not claim machine");
machine.Owner = false;
Equal(true, ProductionLease.Acquire(machine, a), "original controller resumes after peer hand-off");
ProductionLease.SetActive(a, false);
Equal(true, ProductionLease.Acquire(machine, b), "idle controller releases machine");
ProductionLease.SetActive(b, true);
ZDOMan.instance.Remove(b.GetZDO().m_uid);
Equal(true, ProductionLease.Acquire(machine, a), "destroyed controller no longer blocks");
Console.WriteLine("Reliability regressions passed.");
