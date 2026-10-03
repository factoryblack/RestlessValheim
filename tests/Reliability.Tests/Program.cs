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
Equal(true, history.Resolve(7, 2, "P", true, _ => throw new Exception("retired settlement applied")), "retired settlement can be confirmed");
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
// Exercise receipt persistence and the actual peer-binding/decision adapter.
var character = new Player { Id = 42, View = new ZNetView(40, true) };
character.View.GetZDO().Peer = 900;
Player.m_localPlayer = character; Player.All.Add(character);
Equal(true, TransferDelivery.ActorIsPeer(42, 900), "profile binds to owning peer");
Equal(false, TransferDelivery.ActorIsPeer(42, 42), "profile id is not a network peer id");
var target = new Container { m_nview = new ZNetView(50, true), Stock = 10 };
Equal(true, TransferDelivery.MayUse(target, 42, 900), "normal remote actor allowed");
target.Allowed = false;
Equal(false, TransferDelivery.MayUse(target, 42, 900), "ward access enforced");
target.Allowed = true;
var transferId = TransferDelivery.NextId();
TransferDelivery.Remember(target, transferId, "W", 0);
// Simulate reconnect with only the character's persisted custom data.
var restored = new Player { Id = 42, View = character.View };
foreach (var pair in character.m_customData) restored.m_customData.Add(pair.Key, pair.Value);
Player.m_localPlayer = restored; Player.All.Clear(); Player.All.Add(restored);
ZNetScene.instance = new ZNetScene(); ZNetScene.instance.Objects[target.m_nview.GetZDO().m_uid] = new GameObject { Container = target };
UnityEngine.Time.unscaledTime = 10;
TransferDelivery.Tick();
var cancellation = target.m_nview.Sent.Last().Data;
TransferDelivery.OnDecision(target, 900, cancellation);
var confirmed = target.m_nview.Sent.Last().Data;
TransferDelivery.OnClosed(target, 500, confirmed);
Equal(false, restored.m_customData.ContainsKey("restless.transfer.receipts.v2"), "confirmed decision clears saved receipt");
var restoredBook = TransferDelivery.Read(target);
restoredBook.Prepare(42, transferId, "W", () => throw new Exception("delayed request reopened after reconnect cancellation"));
Equal(10, target.Stock, "early reconnect cancellation moves no stock");

// A delivered batch stays committed through repeated decisions and lost confirmation.
var committedId = TransferDelivery.NextId();
var ownerBook = TransferDelivery.Read(target);
ownerBook.Prepare(42, committedId, "W", () => new TransferBook.Entry { State = TransferBook.Phase.Held, Count = 4,
    Payload = TransferDelivery.Pack(new() { new() { m_stack = 4 } }) });
TransferDelivery.Write(target, ownerBook);
TransferDelivery.Remember(target, committedId, "W", 1, TransferDelivery.Pack(new() { new() { m_stack = 1 } }));
var decision = target.m_nview.Sent.Last().Data;
TransferDelivery.OnDecision(target, 900, decision);
TransferDelivery.OnDecision(target, 900, decision);
Equal(11, target.Stock, "overflow returned once on duplicate decision");
UnityEngine.Time.unscaledTime = 20; TransferDelivery.Tick();
Equal(TransferDelivery.DecisionRpc, target.m_nview.Sent.Last().Rpc, "lost confirmation keeps retrying saved decision");
TransferDelivery.OnDecision(target, 900, target.m_nview.Sent.Last().Data);
TransferDelivery.OnClosed(target, 500, target.m_nview.Sent.Last().Data);
Equal(false, restored.m_customData.ContainsKey("restless.transfer.receipts.v2"), "retry confirmation clears receipt");
Equal(11, target.Stock, "retry does not return overflow twice");
// Mixed input queues are read individually even before any output is processed.
var smelter = new Smelter(); smelter.Queue("CopperOre"); smelter.Queue("TinOre");
smelter.m_nview.GetZDO().Set("spawnOre", "IronScrap");
Equal("CopperOre", ProductionQueue.Input(smelter, 0), "first input uses native getter");
Equal("TinOre", ProductionQueue.Input(smelter, 1), "second input is independent of first/output buffer");
Equal("", ProductionQueue.Input(smelter, 2), "outside queue is empty");
Console.WriteLine("Reliability regressions passed.");
