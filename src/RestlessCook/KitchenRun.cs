using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RestlessQoL.Core;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private const string Key = "RestlessKitchen";
    private const int MaxOrders = 8;
    private const int CraftsPerTick = 3;
    private const int LoadsPerTick = 4;

    private static readonly List<CookRow> EmptyRows = new();
    private static readonly Dictionary<string, CookRow> ById = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, CookRow> ByOutput = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, List<CookRow>> UsedBy = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, Conversion> Products = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> SharedNames = new(StringComparer.Ordinal);

    private static bool _conversions;

    private sealed class Conversion
    {
        public string From = "";
        public KitchenStationKind Kind;
    }

    private sealed class Ledger
    {
        public readonly Dictionary<string, int> Pantry = new(StringComparer.Ordinal);
        public readonly List<KitchenOrder> Orders = new();
        public readonly List<Work> Work = new();
    }

    private static List<CookRow> _rows = new();
    internal static IReadOnlyList<CookRow> Rows { get { EnsureMeads(); return _rows; } }

    internal static void Load(CookBook book)
    {
        _rows = new List<CookRow>(book.Items);
        _meadDb = null;
        ById.Clear();
        ByOutput.Clear();
        UsedBy.Clear();
        foreach (var row in book.Items)
        {
            if (!string.IsNullOrEmpty(row.Id))
                ById[row.Id] = row;
            var output = OutputOf(row);
            if (!string.IsNullOrEmpty(output))
                ByOutput[output] = row;
            foreach (var use in row.Uses)
            {
                if (string.IsNullOrEmpty(use.Item))
                    continue;
                if (!UsedBy.TryGetValue(use.Item, out var list))
                {
                    list = new List<CookRow>();
                    UsedBy[use.Item] = list;
                }

                list.Add(row);
            }
        }
    }

    internal static CookRow? Find(string idOrPrefab)
    {
        EnsureMeads();
        if (string.IsNullOrEmpty(idOrPrefab))
            return null;
        if (ById.TryGetValue(idOrPrefab, out var byId))
            return byId;
        if (ByOutput.TryGetValue(idOrPrefab, out var byOutput))
            return byOutput;
        return null;
    }

    internal static IReadOnlyList<CookRow> OtherUses(string prefab)
    {
        if (string.IsNullOrEmpty(prefab) || !UsedBy.TryGetValue(prefab, out var list))
            return EmptyRows;
        return list;
    }

    internal static IReadOnlyList<KitchenOrder> ReadOrders(CraftingStation table)
    {
        var view = View(table);
        return view == null ? Array.Empty<KitchenOrder>() : Read(view).Orders;
    }

    internal static IReadOnlyList<KitchenStationInfo> Describe(CraftingStation table)
    {
        var list = new List<KitchenStationInfo>();
        if (table == null)
            return list;
        foreach (var hit in Scan(table.transform.position))
            list.Add(hit.Info);
        return list;
    }

    internal static IReadOnlyList<KitchenStep> Plan(CraftingStation table, string idOrPrefab, int count)
    {
        var row = Find(idOrPrefab);
        if (row == null || count < 1 || table == null)
            return Array.Empty<KitchenStep>();
        var ledger = Read(table.m_nview);
        var player = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        var free = Stock(table.transform.position, player, ledger);
        var cooking = OnStations(table.transform.position);
        IncludeWork(ledger, cooking);
        foreach (var order in ledger.Orders)
        {
            var prior = Expand(order.Feast, Outstanding(order), 0, "");
            Assign(table, prior, free, cooking, order.PlayerId, order.Ready);
        }

        var steps = Expand(OutputOf(row), count, 0, "");
        Assign(table, steps, free, cooking, null);
        PaintTiming(table, steps, ledger, 0);
        return steps;
    }

    internal static IReadOnlyList<KitchenStep> Live(CraftingStation table, int orderId)
    {
        var ledger = Read(table != null ? table.m_nview : null);
        KitchenOrder? order = null;
        foreach (var row in ledger.Orders)
        {
            if (row.Id == orderId)
                order = row;
        }

        if (order == null || table == null)
            return Array.Empty<KitchenStep>();
        var player = order.PlayerId;
        var free = Stock(table.transform.position, player, ledger);
        var cooking = OnStations(table.transform.position);
        IncludeWork(ledger, cooking);
        var steps = Expand(order.Feast, Outstanding(order), 0, "");
        Assign(table, steps, free, cooking, player, order.Ready);
        PaintTiming(table, steps, ledger, orderId);
        return steps;
    }

    internal static bool Place(CraftingStation table, string idOrPrefab, int count)
    {
        var view = View(table);
        var row = Find(idOrPrefab);
        if (view == null || row == null || count < 1)
            return false;
        if (!view.IsOwner())
            view.ClaimOwnership();
        if (!view.IsOwner())
            return false;

        var ledger = Read(view);
        if (ledger.Orders.Count >= MaxOrders)
            return false;
        var next = 1;
        foreach (var order in ledger.Orders)
            next = Math.Max(next, order.Id + 1);
        var player = Player.m_localPlayer;
        ledger.Orders.Add(new KitchenOrder
        {
            Id = next,
            Feast = OutputOf(row),
            Name = row.Name,
            Count = count,
            PlayerId = player != null ? player.GetPlayerID() : 0L
        });
        Write(view, ledger);
        return true;
    }

    internal static void Cancel(CraftingStation table, int orderId)
    {
        var view = View(table);
        if (view == null)
            return;
        if (!view.IsOwner())
            view.ClaimOwnership();
        if (!view.IsOwner())
            return;
        var ledger = Read(view);
        foreach (var work in ledger.Work)
            if (work.Order == orderId)
                foreach (var input in work.Inputs) Add(ledger, input.Key, input.Value);
        ledger.Work.RemoveAll(work => work.Order == orderId);
        ledger.Orders.RemoveAll(order => order.Id == orderId);
        Write(view, ledger);
    }

    internal static int Collect(CraftingStation table, int orderId)
    {
        var player = Player.m_localPlayer;
        var view = View(table);
        if (player == null || view == null)
            return 0;
        if (!view.IsOwner())
            view.ClaimOwnership();
        if (!view.IsOwner())
            return 0;

        var ledger = Read(view);
        KitchenOrder? order = null;
        foreach (var row in ledger.Orders)
        {
            if (row.Id == orderId)
                order = row;
        }

        if (order == null || order.Ready <= 0)
            return 0;
        var taken = Give(player, ledger, order.Feast, order.Ready);
        if (taken <= 0)
            return 0;
        order.Ready -= taken;
        order.Collected += taken;
        if (order.Collected >= order.Count && order.Ready <= 0)
            ledger.Orders.Remove(order);
        Write(view, ledger);
        return taken;
    }

    internal static IReadOnlyList<KitchenPantryItem> Pantry(CraftingStation table)
    {
        var ledger = Read(table != null ? table.m_nview : null);
        var reserved = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var order in ledger.Orders)
        {
            if (order.Ready <= 0 || string.IsNullOrEmpty(order.Feast))
                continue;
            reserved.TryGetValue(order.Feast, out var held);
            reserved[order.Feast] = held + order.Ready;
        }

        var list = new List<KitchenPantryItem>();
        foreach (var pair in ledger.Pantry)
        {
            if (pair.Value <= 0)
                continue;
            reserved.TryGetValue(pair.Key, out var held);
            list.Add(new KitchenPantryItem
            {
                Prefab = pair.Key,
                Name = Label(pair.Key),
                Count = pair.Value,
                Reserved = Math.Min(Math.Max(0, held), pair.Value)
            });
        }

        return list;
    }

    internal static int Take(CraftingStation table, string prefab, int count)
    {
        var player = Player.m_localPlayer;
        var view = View(table);
        if (player == null || view == null || string.IsNullOrEmpty(prefab) || count < 1)
            return 0;
        if (!view.IsOwner())
            view.ClaimOwnership();
        if (!view.IsOwner())
            return 0;

        var ledger = Read(view);
        if (!ledger.Pantry.TryGetValue(prefab, out var have) || have <= 0)
            return 0;
        var reserved = 0;
        foreach (var order in ledger.Orders)
        {
            if (order.Feast == prefab)
                reserved += order.Ready;
        }

        var free = Math.Max(0, have - reserved);
        var given = Give(player, ledger, prefab, Math.Min(count, free));
        if (given <= 0)
            return 0;
        Write(view, ledger);
        return given;
    }

    private static int Outstanding(KitchenOrder order) => Math.Max(0, order.Count - order.Collected);

    private static void Deliver(KitchenOrder order, Ledger ledger)
    {
        if (order.Ready <= 0)
            return;
        var player = Player.m_localPlayer;
        if (player == null || player.GetPlayerID() != order.PlayerId)
            return;
        var taken = Give(player, ledger, order.Feast, order.Ready);
        if (taken <= 0)
            return;
        order.Ready -= taken;
        order.Collected += taken;
        player.Message(MessageHud.MessageType.Center, order.Name);
    }

    private static int Give(Player player, Ledger ledger, string prefab, int count)
    {
        if (!ledger.Pantry.TryGetValue(prefab, out var have) || have <= 0 || count < 1)
            return 0;
        var drop = Item(prefab);
        if (drop?.m_itemData == null)
            return 0;
        var take = Math.Min(count, have);
        var given = 0;
        var inv = player.GetInventory();
        while (given < take)
        {
            var one = drop.m_itemData.Clone();
            one.m_stack = 1;
            one.m_dropPrefab = drop.gameObject;
            if (!inv.AddItem(one))
                break;
            given++;
        }

        if (given <= 0)
            return 0;
        ledger.Pantry[prefab] = have - given;
        if (ledger.Pantry[prefab] <= 0)
            ledger.Pantry.Remove(prefab);
        return given;
    }

    internal static void Tick(CraftingStation table, float elapsed)
    {
        var view = View(table);
        if (view == null || !view.IsOwner())
            return;
        var ledger = Read(view);
        if (ledger.Orders.Count == 0)
            return;

        var origin = table.transform.position;
        var hits = Scan(origin);
        EnsureConversions();
        AdvanceWork(table, hits, ledger, elapsed);
        CollectFinished(hits, ledger);
        Fuel(hits, ledger, origin);
        var crafts = 0;
        var loads = 0;
        var shared = Stock(origin, 0L, ledger);
        var cooking = OnStations(origin);
        IncludeWork(ledger, cooking);
        foreach (var order in ledger.Orders)
        {
            var outstanding = Outstanding(order);
            if (outstanding < 1)
                continue;
            var steps = Expand(order.Feast, outstanding, 0, "");
            Assign(table, steps, shared, cooking, order.PlayerId, order.Ready);
            for (var i = steps.Count - 1; i >= 0; i--)
            {
                var step = steps[i];
                if (step.State != KitchenStepState.Ready && step.State != KitchenStepState.Queued)
                    continue;
                if (step.Station == KitchenStationKind.Rack || step.Station == KitchenStationKind.Oven)
                {
                    if (loads >= LoadsPerTick)
                        continue;
                    if (Load(hits, step, ledger, origin, order.PlayerId))
                        loads++;
                    continue;
                }

                if (step.Station != KitchenStationKind.Cauldron && step.Station != KitchenStationKind.PrepTable && step.Station != KitchenStationKind.MeadKettle)
                    continue;
                if (crafts >= CraftsPerTick || Busy(ledger, step.Station))
                    continue;
                if (!StationReady(table, hits, step))
                    continue;
                if (!PullOne(step, ledger, origin, order.PlayerId))
                    continue;
                if (!Pay(step, ledger))
                    continue;
                StartWork(ledger, order, step);

                crafts++;
            }

            Deliver(order, ledger);
        }

        ledger.Orders.RemoveAll(order => order.Collected >= order.Count && order.Ready <= 0);
        Write(view, ledger);
    }

    private static string OutputOf(CookRow row)
    {
        if (row.IsFeast && row.IsAdd && !row.Prefab.EndsWith("_Material", StringComparison.Ordinal))
            return row.Prefab + "_Material";
        return row.Prefab;
    }

    private static List<KitchenStep> Expand(string output, int need, int depth, string parent)
    {
        var steps = new List<KitchenStep>();
        var guard = new HashSet<string>(StringComparer.Ordinal);
        Walk(output, need, depth, parent, -1, steps, guard);
        return steps;
    }

    private static void Walk(string output, int need, int depth, string parent, int parentIndex, List<KitchenStep> steps, HashSet<string> guard)
    {
        if (need < 1 || depth > 12 || string.IsNullOrEmpty(output) || !guard.Add(output + "#" + parent))
            return;
        EnsureConversions();
        var row = ByOutput.TryGetValue(output, out var found) ? found : null;
        Products.TryGetValue(output, out var conversion);
        var step = new KitchenStep
        {
            Id = row != null ? row.Id : output,
            Name = row != null ? row.Name : Label(output),
            Output = output,
            Parent = parent,
            Depth = depth,
            Need = need,
            Station = row != null ? KindOf(row.Station) : conversion != null ? conversion.Kind : KitchenStationKind.None,
            StationPrefab = row != null ? row.Station : "",
            StationLevel = row != null && row.StationLevel > 0 ? row.StationLevel : 1,
            ParentIndex = parentIndex
        };
        var index = steps.Count;
        steps.Add(step);
        if (row != null)
        {
            var crafts = CraftsFor(need, row.OutputAmount);
            foreach (var use in row.Uses)
            {
                if (use.Amount < 1 || string.IsNullOrEmpty(use.Item))
                    continue;
                var want = crafts * use.Amount;
                step.Uses.Add(new KitchenUse { Item = use.Item, Amount = want, Name = Label(use.Item) });
                Walk(use.Item, want, depth + 1, output, index, steps, guard);
            }

            return;
        }

        if (conversion == null || string.IsNullOrEmpty(conversion.From))
            return;
        step.Uses.Add(new KitchenUse { Item = conversion.From, Amount = need, Name = Label(conversion.From) });
        Walk(conversion.From, need, depth + 1, output, index, steps, guard);
    }

    private static void Assign(CraftingStation table, List<KitchenStep> steps, Dictionary<string, int> free, Dictionary<string, int> cooking, long? playerId, int completedForOrder = 0)
    {
        var hits = Scan(table.transform.position);
        var covered = new bool[steps.Count];
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.ParentIndex >= 0 && step.ParentIndex < covered.Length && covered[step.ParentIndex])
            {
                covered[i] = true;
                free.TryGetValue(step.Output, out var already);
                step.Available = Math.Max(0, already);
                step.Have = step.Need;
                step.Cooking = 0;
                step.State = KitchenStepState.Prepared;
                step.Note = "Prepared";
                continue;
            }

            free.TryGetValue(step.Output, out var stock);
            var portion = KitchenStock.Split(stock, step.Need, i == 0, completedForOrder);
            var have = portion.Allocated;
            step.Have = have;
            step.Available = portion.OnHand;
            free[step.Output] = Math.Max(0, stock - have);
            cooking.TryGetValue(step.Output, out var onFire);
            var still = step.Need - have;
            step.Cooking = Math.Min(onFire, Math.Max(0, still));
            cooking[step.Output] = onFire - step.Cooking;
            if (step.Have >= step.Need)
            {
                covered[i] = true;
                step.State = KitchenStepState.Prepared;
                step.Note = "Prepared";
                continue;
            }

            if (step.Have + step.Cooking >= step.Need)
            {
                covered[i] = true;
                step.State = KitchenStepState.Cooking;
                step.Note = step.Cooking + " cooking";
            }
        }

        for (var i = steps.Count - 1; i >= 0; i--)
        {
            if (covered[i])
                continue;
            var step = steps[i];
            if (step.Uses.Count == 0)
            {
                step.State = KitchenStepState.Missing;
                step.Note = "Missing " + step.Name;
                continue;
            }

            var crafts = CraftsFor(step.Need - step.Have - step.Cooking, OutputAmount(step));
            var inputs = true;
            string? missing = null;
            foreach (var use in step.Uses)
            {
                var want = use.Amount * crafts / Math.Max(1, CraftsFor(step.Need, OutputAmount(step)));
                if (want < 1)
                    want = use.Amount;
                free.TryGetValue(use.Item, out var got);
                var incoming = Incoming(steps, use.Item);
                use.Have = got + incoming;
                if (got + incoming < want)
                {
                    inputs = false;
                    missing = use.Name;
                }
                else
                    free[use.Item] = Math.Max(0, got - Math.Max(0, want - incoming));
            }

            if (!inputs)
            {
                step.State = KitchenStepState.Missing;
                step.Note = string.IsNullOrEmpty(missing) ? "Missing ingredients" : "Waiting for " + missing;
                continue;
            }

            var block = BlockReason(table, hits, step);
            if (!string.IsNullOrEmpty(block))
            {
                step.State = KitchenStepState.Blocked;
                step.Note = block;
                continue;
            }

            step.State = playerId.HasValue ? KitchenStepState.Queued : KitchenStepState.Ready;
            step.Note = step.State == KitchenStepState.Ready ? "Ingredients available" : "Queued";
        }
    }

    private static int Incoming(List<KitchenStep> steps, string item)
    {
        var n = 0;
        foreach (var step in steps)
        {
        if (step.Output != item)
            continue;
        if (step.Have >= step.Need)
        {
            n += step.Have;
            continue;
        }

        if (step.State == KitchenStepState.Cooking || step.State == KitchenStepState.Queued || step.State == KitchenStepState.Ready)
            n += Math.Max(0, step.Need - step.Have);
        }

        return n;
    }

    private static int OutputAmount(KitchenStep step)
    {
        if (ByOutput.TryGetValue(step.Output, out var row) && row.OutputAmount > 0)
            return row.OutputAmount;
        return 1;
    }

    private static int CraftsFor(int need, int output)
    {
        if (need < 1)
            return 0;
        var made = Math.Max(1, output);
        return (need + made - 1) / made;
    }

    private static Dictionary<string, int> Stock(Vector3 origin, long playerId, Ledger ledger)
    {
        var free = new Dictionary<string, int>(ledger.Pantry, StringComparer.Ordinal);
        void Add(string prefab, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(prefab))
                return;
            free.TryGetValue(prefab, out var have);
            free[prefab] = have + count;
        }

        var player = Player.m_localPlayer;
        if (player != null && (playerId == 0 || player.GetPlayerID() == playerId))
        {
            foreach (var item in player.GetInventory().GetAllItems())
            {
                if (item?.m_dropPrefab == null)
                    continue;
                Add(Clean(item.m_dropPrefab.name), item.m_stack);
            }
        }

        foreach (var container in NearbyStorage.Around(origin, Range(), playerId))
        {
            var view = container.m_nview;
            if (view == null || !view.IsOwner() || container.GetInventory() == null)
                continue;
            foreach (var item in container.GetInventory().GetAllItems())
            {
                if (item?.m_dropPrefab == null)
                    continue;
                var count = item.m_stack;
                if (ModConfig.LeaveOne.Value && count > 0)
                    count -= 1;
                Add(Clean(item.m_dropPrefab.name), count);
            }
        }

        var lots = new List<NearbyLot>();
        NearbyLots.CollectAround(origin, Range(), lots);
        foreach (var lot in lots)
        {
            if (lot.Item?.m_dropPrefab == null)
                continue;
            Add(Clean(lot.Item.m_dropPrefab.name), lot.Count);
        }

        return free;
    }

    private static Dictionary<string, int> OnStations(Vector3 origin)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var hit in Scan(origin))
        {
            if (hit.Rack == null)
                continue;
            var station = hit.Rack;
            for (var i = 0; i < station.m_slots.Length; i++)
            {
                station.GetSlot(i, out var name, out _, out var status, out _);
                if (string.IsNullOrEmpty(name) || status == CookingStation.Status.Burnt)
                    continue;
                var product = status == CookingStation.Status.Done
                    ? Clean(name)
                    : ProductOf(station, name);
                if (string.IsNullOrEmpty(product))
                    continue;
                map.TryGetValue(product, out var have);
                map[product] = have + 1;
            }
        }

        return map;
    }

    private static void CollectFinished(List<Hit> hits, Ledger ledger)
    {
        foreach (var hit in hits)
        {
            var rack = hit.Rack;
            if (rack != null && rack.m_nview != null && rack.m_nview.IsOwner())
            {
                for (var i = 0; i < rack.m_slots.Length; i++)
                {
                    rack.GetSlot(i, out var name, out _, out var status, out _);
                    if (status != CookingStation.Status.Done || string.IsNullOrEmpty(name))
                        continue;
                    var product = Clean(name);
                    if (!Wanted(ledger, product))
                        continue;
                    Add(ledger, product, 1);
                    rack.SetSlot(i, "", 0f, CookingStation.Status.NotDone, false);
                    rack.SetSlotVisual(i, "", false, CookingStation.Status.NotDone);
                }
            }

            var oven = hit.Oven;
            if (oven == null || oven.m_nview == null || !oven.m_nview.IsOwner() || oven.GetProcessedQueueSize() <= 0)
                continue;
            var source = oven.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre);
            var conversion = oven.GetItemConversion(source);
            if (conversion?.m_to == null)
                continue;
            var made = Clean(conversion.m_to.gameObject.name);
            if (!Wanted(ledger, made))
                continue;
            var amount = oven.GetProcessedQueueSize();
            Add(ledger, made, amount);
            oven.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, "");
            oven.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 0);
        }
    }

    private static void Fuel(List<Hit> hits, Ledger ledger, Vector3 origin)
    {
        foreach (var hit in hits)
        {
            if (hit.Rack != null && hit.Rack.m_useFuel && hit.Rack.m_fuelItem != null && hit.Rack.m_nview != null && hit.Rack.m_nview.IsOwner())
            {
                if (hit.Rack.GetFuel() < 1f && SpendFuel(hit.Rack.m_fuelItem, ledger, origin))
                    hit.Rack.SetFuel(Math.Min(hit.Rack.m_maxFuel, hit.Rack.GetFuel() + 1f));
            }

            if (hit.Oven == null || hit.Oven.m_fuelItem == null || hit.Oven.m_maxFuel <= 0 || hit.Oven.m_nview == null || !hit.Oven.m_nview.IsOwner())
                continue;
            if (hit.Oven.GetFuel() < 1f && SpendFuel(hit.Oven.m_fuelItem, ledger, origin))
                hit.Oven.SetFuel(Math.Min(hit.Oven.m_maxFuel, hit.Oven.GetFuel() + 1f));
        }
    }

    private static bool Load(List<Hit> hits, KitchenStep step, Ledger ledger, Vector3 origin, long playerId)
    {
        Products.TryGetValue(step.Output, out var conversion);
        var raw = conversion != null ? conversion.From : step.Uses.Count > 0 ? step.Uses[0].Item : "";
        if (string.IsNullOrEmpty(raw))
            return false;
        if (!ledger.Pantry.TryGetValue(raw, out var held) || held < 1)
        {
            PullInto(ledger, raw, 1, origin, playerId);
            if (!ledger.Pantry.TryGetValue(raw, out held) || held < 1)
                return false;
        }

        foreach (var hit in hits)
        {
            if (hit.Info.Kind != step.Station || !string.IsNullOrEmpty(hit.Info.Block) || hit.Info.Free < 1)
                continue;
            if (hit.Rack != null && hit.Rack.m_nview != null && hit.Rack.m_nview.IsOwner() && hit.Rack.IsItemAllowed(raw))
            {
                ledger.Pantry[raw] = held - 1;
                if (ledger.Pantry[raw] <= 0)
                    ledger.Pantry.Remove(raw);
                hit.Rack.m_nview.InvokeRPC("RPC_AddItem", raw, false);
                return true;
            }

            if (hit.Oven != null && hit.Oven.m_nview != null && hit.Oven.m_nview.IsOwner()
                && hit.Oven.GetQueueSize() < hit.Oven.m_maxOre && hit.Oven.IsItemAllowed(raw))
            {
                ledger.Pantry[raw] = held - 1;
                if (ledger.Pantry[raw] <= 0)
                    ledger.Pantry.Remove(raw);
                hit.Oven.QueueOre(raw, false);
                return true;
            }
        }

        return false;
    }

    private static bool PullOne(KitchenStep step, Ledger ledger, Vector3 origin, long playerId)
    {
        var crafts = 1;
        foreach (var use in step.Uses)
        {
            var one = Math.Max(1, use.Amount / Math.Max(1, CraftsFor(step.Need, OutputAmount(step))));
            var want = one * crafts;
            ledger.Pantry.TryGetValue(use.Item, out var have);
            if (have >= want)
                continue;
            PullInto(ledger, use.Item, want - have, origin, playerId);
            ledger.Pantry.TryGetValue(use.Item, out have);
            if (have < want)
                return false;
        }

        return true;
    }

    private static bool Pay(KitchenStep step, Ledger ledger)
    {
        var crafts = 1;
        foreach (var use in step.Uses)
        {
            var one = Math.Max(1, use.Amount / Math.Max(1, CraftsFor(step.Need, OutputAmount(step))));
            ledger.Pantry.TryGetValue(use.Item, out var have);
            if (have < one * crafts)
                return false;
        }

        foreach (var use in step.Uses)
        {
            var one = Math.Max(1, use.Amount / Math.Max(1, CraftsFor(step.Need, OutputAmount(step))));
            ledger.Pantry[use.Item] = ledger.Pantry[use.Item] - one;
            if (ledger.Pantry[use.Item] <= 0)
                ledger.Pantry.Remove(use.Item);
        }

        return true;
    }

    private static void PullInto(Ledger ledger, string prefab, int count, Vector3 origin, long playerId)
    {
        if (count < 1)
            return;
        var player = Player.m_localPlayer;
        if (player != null && (playerId == 0 || player.GetPlayerID() == playerId))
        {
            var shared = Shared(prefab);
            var inv = player.GetInventory();
            var have = string.IsNullOrEmpty(shared) ? 0 : inv.CountItems(shared);
            var take = Math.Min(have, count);
            if (take > 0)
            {
                inv.RemoveItem(shared, take);
                Add(ledger, prefab, take);
                count -= take;
            }
        }

        if (count < 1)
            return;
        var sharedName = Shared(prefab);
        if (string.IsNullOrEmpty(sharedName))
            return;
        foreach (var container in NearbyStorage.Around(origin, Range(), playerId))
        {
            var view = container.m_nview;
            var inv = container.GetInventory();
            if (view == null || !view.IsOwner() || inv == null)
                continue;
            var have = inv.CountItems(sharedName);
            if (ModConfig.LeaveOne.Value && have > 0)
                have -= 1;
            var take = Math.Min(have, count);
            if (take < 1)
                continue;
            inv.RemoveItem(sharedName, take);
            container.Save();
            Add(ledger, prefab, take);
            count -= take;
            if (count < 1)
                return;
        }

        var fromPiles = NearbyLots.Drain(origin, Range(), sharedName, count);
        if (fromPiles > 0)
            Add(ledger, prefab, fromPiles);
    }

    private static bool SpendFuel(ItemDrop fuel, Ledger ledger, Vector3 origin)
    {
        var prefab = Clean(fuel.gameObject.name);
        if (ledger.Pantry.TryGetValue(prefab, out var have) && have > 0)
        {
            ledger.Pantry[prefab] = have - 1;
            if (ledger.Pantry[prefab] <= 0)
                ledger.Pantry.Remove(prefab);
            return true;
        }

        var player = Player.m_localPlayer;
        if (player == null)
            return false;
        PullInto(ledger, prefab, 1, origin, player.GetPlayerID());
        if (!ledger.Pantry.TryGetValue(prefab, out have) || have < 1)
            return false;
        ledger.Pantry[prefab] = have - 1;
        if (ledger.Pantry[prefab] <= 0)
            ledger.Pantry.Remove(prefab);
        return true;
    }

    private static bool StationReady(CraftingStation table, List<Hit> hits, KitchenStep step)
    {
        if (step.Station == KitchenStationKind.PrepTable)
            return table.GetLevel() >= step.StationLevel;
        if (step.Station != KitchenStationKind.Cauldron && step.Station != KitchenStationKind.MeadKettle)
            return false;
        foreach (var hit in hits)
        {
            if (hit.Info.Kind == step.Station && hit.Info.Level >= step.StationLevel)
                return true;
        }

        return false;
    }

    private static string? BlockReason(CraftingStation table, List<Hit> hits, KitchenStep step)
    {
        if (step.Station == KitchenStationKind.None)
            return null;
        if (step.Station == KitchenStationKind.PrepTable)
            return table.GetLevel() >= step.StationLevel ? null : "Requires preparation table level " + step.StationLevel;
        if (step.Station == KitchenStationKind.Cauldron || step.Station == KitchenStationKind.MeadKettle)
        {
            var best = 0;
            var found = false;
            foreach (var hit in hits)
            {
                if (hit.Info.Kind != step.Station)
                    continue;
                found = true;
                best = Math.Max(best, hit.Info.Level);
            }

            if (!found)
                return "No " + step.Station + " in range";
            return best >= step.StationLevel ? null : "Requires " + step.Station + " level " + step.StationLevel;
        }

        var any = false;
        string? block = null;
        foreach (var hit in hits)
        {
            if (hit.Info.Kind != step.Station)
                continue;
            any = true;
            if (string.IsNullOrEmpty(hit.Info.Block) && hit.Info.Free > 0)
                return null;
            if (!string.IsNullOrEmpty(hit.Info.Block))
                block = hit.Info.Block;
        }

        if (!any)
            return step.Station == KitchenStationKind.Oven ? "No oven in range" : "No cooking rack in range";
        return block ?? "Stations are full";
    }

    private static bool Wanted(Ledger ledger, string product)
    {
        foreach (var order in ledger.Orders)
        {
            if (Needs(order.Feast, product, new HashSet<string>(StringComparer.Ordinal)))
                return true;
        }

        return false;
    }

    private static bool Needs(string output, string product, HashSet<string> guard)
    {
        if (output == product)
            return true;
        if (!guard.Add(output))
            return false;
        if (ByOutput.TryGetValue(output, out var row))
        {
            foreach (var use in row.Uses)
            {
                if (Needs(use.Item, product, guard))
                    return true;
            }
        }

        if (Products.TryGetValue(output, out var conversion) && Needs(conversion.From, product, guard))
            return true;
        return false;
    }

    private sealed class Hit
    {
        public CookingStation? Rack;
        public Smelter? Oven;
        public KitchenStationInfo Info = new();
    }

    private static List<Hit> Scan(Vector3 origin)
    {
        var list = new List<Hit>();
        var seen = new HashSet<int>();
        var hits = Physics.OverlapSphere(origin, Range(), ~0, QueryTriggerInteraction.Collide);
        foreach (var col in hits)
        {
            if (col == null)
                continue;
            var rack = col.GetComponentInParent<CookingStation>();
            if (rack != null && seen.Add(rack.GetInstanceID()))
                list.Add(DescribeRack(rack));
            var oven = col.GetComponentInParent<Smelter>();
            // Kilns, smelters and the other production machines belong to the work-order board.
            // The stone oven stays here; food recipes still load it.
            if (oven != null && seen.Add(oven.GetInstanceID()) && FoodOven(Utils.GetPrefabName(oven.gameObject)))
                list.Add(DescribeOven(oven));
            var craft = col.GetComponentInParent<CraftingStation>();
            if (craft != null && seen.Add(craft.GetInstanceID()) && (KindOf(Utils.GetPrefabName(craft.gameObject)) == KitchenStationKind.Cauldron || KindOf(Utils.GetPrefabName(craft.gameObject)) == KitchenStationKind.MeadKettle))
                list.Add(DescribeCauldron(craft));
        }

        return list;
    }

    private static Hit DescribeRack(CookingStation station)
    {
        var info = new KitchenStationInfo
        {
            Name = station.m_name,
            Kind = KitchenStationKind.Rack,
            FuelMax = station.m_useFuel ? station.m_maxFuel : 0,
            NeedsFire = station.m_requireFire
        };
        if (station.m_nview != null && station.m_nview.IsValid())
        {
            info.Fuel = station.m_useFuel ? Mathf.FloorToInt(station.GetFuel()) : 0;
            info.FireLit = !station.m_requireFire || station.IsFireLit();
            var free = 0;
            var busy = 0;
            var ready = 0;
            for (var i = 0; i < station.m_slots.Length; i++)
            {
                station.GetSlot(i, out var name, out _, out var status, out _);
                if (string.IsNullOrEmpty(name))
                    free++;
                else if (status == CookingStation.Status.Done)
                    ready++;
                else if (status == CookingStation.Status.NotDone)
                    busy++;
            }

            info.Free = free;
            info.Cooking = busy;
            info.Ready = ready;
        }

        if (info.NeedsFire && !info.FireLit)
            info.Block = "Needs a fire";
        else if (station.m_useFuel && info.Fuel < 1)
            info.Block = "Needs fuel";
        if (station.m_nview != null && !station.m_nview.IsOwner())
            station.m_nview.ClaimOwnership();
        return new Hit { Rack = station, Info = info };
    }

    private static Hit DescribeOven(Smelter station)
    {
        var info = new KitchenStationInfo
        {
            Name = station.m_name,
            Kind = KitchenStationKind.Oven,
            FuelMax = station.m_maxFuel,
            FireLit = true
        };
        if (station.m_nview != null && station.m_nview.IsValid())
        {
            info.Fuel = Mathf.FloorToInt(station.GetFuel());
            info.Cooking = station.GetQueueSize();
            info.Ready = station.GetProcessedQueueSize();
            info.Free = Math.Max(0, station.m_maxOre - station.GetQueueSize());
        }

        if (station.m_maxFuel > 0 && info.Fuel < 1)
            info.Block = "Needs fuel";
        if (station.m_nview != null && !station.m_nview.IsOwner())
            station.m_nview.ClaimOwnership();
        return new Hit { Oven = station, Info = info };
    }

    private static Hit DescribeCauldron(CraftingStation station)
    {
        var info = new KitchenStationInfo
        {
            Name = station.m_name,
            Kind = KindOf(Utils.GetPrefabName(station.gameObject)),
            Level = station.GetLevel(),
            FireLit = true
        };
        return new Hit { Info = info };
    }

    private static string ProductOf(CookingStation station, string raw)
    {
        var conversion = station.GetItemConversion(Clean(raw));
        if (conversion?.m_to == null)
            conversion = station.GetItemConversion(raw);
        return conversion?.m_to == null ? "" : Clean(conversion.m_to.gameObject.name);
    }

    private static void EnsureConversions()
    {
        if (_conversions || ZNetScene.instance == null)
            return;
        _conversions = true;
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab == null)
                continue;
            var oven = prefab.GetComponent<Smelter>();
            if (oven != null && FoodOven(Utils.GetPrefabName(prefab)))
            {
                foreach (var conversion in oven.m_conversion)
                    Remember(conversion?.m_from, conversion?.m_to, KitchenStationKind.Oven);
            }

            var rack = prefab.GetComponent<CookingStation>();
            if (rack == null)
                continue;
            foreach (var conversion in rack.m_conversion)
                Remember(conversion?.m_from, conversion?.m_to, KitchenStationKind.Rack);
        }
    }

    private static void Remember(ItemDrop? from, ItemDrop? to, KitchenStationKind kind)
    {
        if (from == null || to == null)
            return;
        var product = Clean(to.gameObject.name);
        var raw = Clean(from.gameObject.name);
        if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(raw))
            return;
        if (!Products.TryGetValue(product, out var existing) || (existing.Kind != KitchenStationKind.Rack && kind == KitchenStationKind.Rack))
            Products[product] = new Conversion { From = raw, Kind = kind };
    }

    private static bool FoodOven(string prefab) =>
        prefab.IndexOf("oven", StringComparison.OrdinalIgnoreCase) >= 0;

    private static KitchenStationKind KindOf(string station)
    {
        if (string.IsNullOrEmpty(station))
            return KitchenStationKind.None;
        if (station.IndexOf("meadcauldron", StringComparison.OrdinalIgnoreCase) >= 0 || station.IndexOf("meadketill", StringComparison.OrdinalIgnoreCase) >= 0 || station.IndexOf("meadkettle", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.MeadKettle;
        if (station.IndexOf("cauldron", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Cauldron;
        if (station.IndexOf("preptable", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.PrepTable;
        if (station.IndexOf("oven", StringComparison.OrdinalIgnoreCase) >= 0 || station.IndexOf("smelter", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Oven;
        if (station.IndexOf("cooking", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Rack;
        return KitchenStationKind.None;
    }

    private static void Add(Ledger ledger, string prefab, int count)
    {
        if (count < 1 || string.IsNullOrEmpty(prefab))
            return;
        ledger.Pantry.TryGetValue(prefab, out var have);
        ledger.Pantry[prefab] = have + count;
    }

    private static ItemDrop? Item(string prefab)
    {
        var go = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(prefab) : null;
        return go != null ? go.GetComponent<ItemDrop>() : null;
    }

    private static string Shared(string prefab)
    {
        if (SharedNames.TryGetValue(prefab, out var cached))
            return cached;
        var drop = Item(prefab);
        var name = drop?.m_itemData?.m_shared?.m_name ?? "";
        SharedNames[prefab] = name;
        return name;
    }

    private static string Label(string prefab)
    {
        if (ByOutput.TryGetValue(prefab, out var row) && !string.IsNullOrEmpty(row.Name))
            return row.Name;
        if (ById.TryGetValue(prefab, out row) && !string.IsNullOrEmpty(row.Name))
            return row.Name;
        var shared = Shared(prefab);
        if (string.IsNullOrEmpty(shared) || Localization.instance == null)
            return prefab;
        return Localization.instance.Localize(shared);
    }

    private static string Clean(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";
        const string suffix = "(Clone)";
        return name.EndsWith(suffix, StringComparison.Ordinal) ? name.Substring(0, name.Length - suffix.Length) : name;
    }

    private static float Range()
    {
        var range = ModConfig.StorageRange != null ? ModConfig.StorageRange.Value : 20f;
        return range < 4f ? 20f : range;
    }

    private static ZNetView? View(CraftingStation? table)
    {
        if (table == null || Utils.GetPrefabName(table.gameObject) != "piece_preptable")
            return null;
        var view = table.GetComponent<ZNetView>();
        return view != null && view.IsValid() ? view : null;
    }

    private static Ledger Read(ZNetView? view)
    {
        var ledger = new Ledger();
        if (view == null || !view.IsValid())
            return ledger;
        var text = view.GetZDO().GetString(Key);
        if (string.IsNullOrEmpty(text))
            return ledger;
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            if (line.Length < 2)
                continue;
            var parts = line.Split('\t');
            if (parts[0] == "W") { var work = ReadWork(parts); if (work != null) ledger.Work.Add(work); continue; }
            if (parts[0] == "P" && parts.Length >= 3 && int.TryParse(parts[2], out var count) && count > 0)
                ledger.Pantry[parts[1]] = count;
            if (parts[0] != "O" || parts.Length < 6)
                continue;
            if (!int.TryParse(parts[1], out var id) || !int.TryParse(parts[3], out var orderCount) || !long.TryParse(parts[4], out var player) || !int.TryParse(parts[5], out var ready))
                continue;
            var collected = 0;
            if (parts.Length >= 7)
                int.TryParse(parts[6], out collected);
            ledger.Orders.Add(new KitchenOrder
            {
                Id = id,
                Feast = parts[2],
                Name = Find(parts[2])?.Name ?? parts[2],
                Count = orderCount,
                PlayerId = player,
                Ready = ready,
                Collected = Math.Max(0, collected)
            });
        }

        return ledger;
    }

    private static void Write(ZNetView view, Ledger ledger)
    {
        var text = new StringBuilder();
        foreach (var pair in ledger.Pantry)
        {
            if (pair.Value > 0)
                text.Append("P\t").Append(pair.Key).Append('\t').Append(pair.Value).Append('\n');
        }

        foreach (var order in ledger.Orders)
            text.Append("O\t").Append(order.Id).Append('\t').Append(order.Feast).Append('\t').Append(order.Count).Append('\t').Append(order.PlayerId).Append('\t').Append(order.Ready).Append('\t').Append(order.Collected).Append('\n');
        foreach (var work in ledger.Work) text.Append(WorkLine(work));
        view.GetZDO().Set(Key, text.ToString());
    }
}

[HarmonyPatch]
internal static class KitchenHook
{
    internal static void TickOwned(float elapsed)
    {
        if (ZNetScene.instance == null)
            return;
        foreach (var station in UnityEngine.Object.FindObjectsByType<CraftingStation>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (station == null || Utils.GetPrefabName(station.gameObject) != "piece_preptable")
                continue;
            var view = station.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
                continue;
            KitchenRun.Tick(station, elapsed);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.Interact))]
    private static bool OpenKitchen(CraftingStation __instance, Humanoid user, bool repeat, ref bool __result)
    {
        if (repeat || Utils.GetPrefabName(__instance.gameObject) != "piece_preptable")
            return true;
        if (user != Player.m_localPlayer)
            return true;
        __result = false;
        var view = __instance.GetComponent<ZNetView>();
        if (view != null && view.IsValid() && !view.IsOwner())
            view.ClaimOwnership();
        Kitchen.Raise(__instance);
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CraftingStation), nameof(CraftingStation.GetHoverText))]
    private static void Hover(CraftingStation __instance, ref string __result)
    {
        if (Utils.GetPrefabName(__instance.gameObject) != "piece_preptable")
            return;
        var name = Localization.instance != null ? Localization.instance.Localize(__instance.m_name) : __instance.m_name;
        __result = name + "\n[E] Kitchen";
    }
}

