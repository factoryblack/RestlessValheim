using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using RestlessQoL.Core;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessWorks;

internal static class WorksRun
{
    private const string Key = "RestlessWorks";
    private const int MaxOrders = 8;
    private const int LoadsPerTick = 4;

    private static List<WorksRecipe>? _recipes;
    private static readonly Dictionary<string, string> SharedNames = new(StringComparer.Ordinal);
    private static readonly List<Board> Boards = new();

    internal static void Watch(Board board)
    {
        if (board != null && !Boards.Contains(board))
            Boards.Add(board);
    }

    internal static void Forget(Board board)
    {
        if (board != null)
            Boards.Remove(board);
    }

    internal static void TickOwned(float elapsed)
    {
        if (ZNetScene.instance == null)
            return;
        for (var i = Boards.Count - 1; i >= 0; i--)
        {
            var board = Boards[i];
            if (board == null)
            {
                Boards.RemoveAt(i);
                continue;
            }

            var view = board.GetComponent<ZNetView>();
            var piece = board.GetComponent<Piece>();
            if (view == null || piece == null || !view.IsValid() || !view.IsOwner())
                continue;
            Tick(piece, view);
        }
    }

    internal static IReadOnlyList<WorksRecipe> Recipes()
    {
        Discover();
        return _recipes ?? new List<WorksRecipe>();
    }

    internal static WorksRecipe? Find(string output)
    {
        Discover();
        if (_recipes == null || string.IsNullOrEmpty(output))
            return null;
        foreach (var recipe in _recipes)
        {
            if (recipe.Output == output || recipe.OutputName == output)
                return recipe;
        }

        return null;
    }

    internal static IReadOnlyList<WorksStep> Plan(Piece board, string output, int count)
    {
        var steps = new List<WorksStep>();
        var recipe = Find(output);
        if (recipe == null || board == null)
            return steps;
        var origin = board.transform.position;
        var playerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        var stock = Stock(origin, playerId, Read(View(board)));
        AddStep(steps, recipe, Math.Max(1, count), "", 0, stock, new HashSet<string>(StringComparer.Ordinal));
        return steps;
    }

    internal static IReadOnlyList<WorksOrder> ReadOrders(Piece board)
    {
        var view = View(board);
        if (view == null)
            return Array.Empty<WorksOrder>();
        var ledger = Read(view);
        var origin = board.transform.position;
        var playerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        Describe(ledger, Stock(origin, playerId, ledger), Production(Scan(origin, false, view)));
        return ledger.Orders;
    }

    internal static IReadOnlyList<WorksMachine> Machines(Piece board)
    {
        if (board == null)
            return Array.Empty<WorksMachine>();
        var list = new List<WorksMachine>();
        foreach (var hit in Scan(board.transform.position, false, View(board)))
            list.Add(hit.Info);
        return list;
    }

    internal static IReadOnlyList<WorksStock> Pantry(Piece board)
    {
        var view = View(board);
        if (view == null)
            return Array.Empty<WorksStock>();
        var ledger = Read(view);
        var list = new List<WorksStock>();
        foreach (var pair in ledger.Pantry)
        {
            var reserved = 0;
            foreach (var order in ledger.Orders)
            {
                if (order.Mode == WorksOrderMode.Make && order.Output == pair.Key)
                    reserved += order.Ready;
            }

            list.Add(new WorksStock
            {
                Prefab = pair.Key,
                Name = Label(pair.Key),
                Count = pair.Value,
                Reserved = Math.Min(pair.Value, reserved)
            });
        }

        return list;
    }

    internal static bool Place(Piece board, string output, WorksOrderMode mode, int count)
    {
        var view = View(board);
        var recipe = Find(output);
        if (view == null || recipe == null || count < 1 || !Controlled(view))
            return false;
        var ledger = Read(view);
        if (ledger.Orders.Count >= MaxOrders)
            return false;
        var next = 1;
        foreach (var order in ledger.Orders)
            next = Math.Max(next, order.Id + 1);
        ledger.Orders.Add(new WorksOrder
        {
            Id = next,
            Output = recipe.Output,
            Name = recipe.OutputName,
            Mode = mode,
            Count = Math.Min(count, 999),
            PlayerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L
        });
        Write(view, ledger);
        return true;
    }

    internal static void Cancel(Piece board, int orderId)
    {
        var view = View(board);
        if (view == null || !Controlled(view))
            return;
        var ledger = Read(view);
        ledger.Orders.RemoveAll(order => order.Id == orderId);
        Write(view, ledger);
        if (ledger.Orders.Count == 0) ProductionLease.SetActive(view, false);
    }

    internal static int Collect(Piece board, int orderId)
    {
        var view = View(board);
        var player = Player.m_localPlayer;
        if (view == null || player == null || !Controlled(view))
            return 0;
        var ledger = Read(view);
        WorksOrder? order = null;
        foreach (var row in ledger.Orders)
        {
            if (row.Id == orderId)
                order = row;
        }

        if (order == null || order.Mode != WorksOrderMode.Make || order.Ready < 1 || player.GetPlayerID() != order.PlayerId)
            return 0;
        var given = Give(player, ledger, order.Output, order.Ready);
        if (given < 1)
            return 0;
        order.Ready -= given;
        order.Collected += given;
        if (order.Collected >= order.Count && order.Ready < 1)
            ledger.Orders.Remove(order);
        Write(view, ledger);
        return given;
    }

    internal static int Take(Piece board, string prefab, int count)
    {
        var view = View(board);
        var player = Player.m_localPlayer;
        if (view == null || player == null || count < 1 || string.IsNullOrEmpty(prefab) || !Controlled(view))
            return 0;
        var ledger = Read(view);
        if (!ledger.Pantry.TryGetValue(prefab, out var have) || have < 1)
            return 0;
        var reserved = 0;
        foreach (var order in ledger.Orders)
        {
            if (order.Mode == WorksOrderMode.Make && order.Output == prefab)
                reserved += order.Ready;
        }

        var free = Math.Max(0, have - reserved);
        var given = Give(player, ledger, prefab, Math.Min(count, free));
        if (given > 0)
            Write(view, ledger);
        return given;
    }

    private static void Tick(Piece board, ZNetView view)
    {
        var ledger = Read(view);
        if (ledger.Orders.Count == 0)
        {
            ProductionLease.SetActive(view, false);
            return;
        }
        var origin = board.transform.position;
        var playerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        if (!ledger.Orders.Exists(order => order.Mode == WorksOrderMode.Keep || order.Count > order.Ready + order.Collected))
        { ProductionLease.SetActive(view, false); return; }
        ProductionLease.SetActive(view, true);
        var hits = Scan(origin, true, view);
        CollectFinished(hits, ledger);
        var loads = 0;
        Describe(ledger, Stock(origin, playerId, ledger), Production(hits));
        foreach (var order in ledger.Orders)
        {
            if (loads >= LoadsPerTick || order.Remaining < 1)
                continue;
            var recipe = Find(order.Output);
            if (recipe == null)
                continue;
            while (loads < LoadsPerTick && order.Remaining > 0)
            {
                if (!LoadOne(recipe, hits, ledger, origin, playerId))
                    break;
                loads++;
                order.Remaining--;
                order.InProduction++;
            }
        }

        FuelQueued(hits, ledger, origin, playerId);
        Write(view, ledger);
        ProductionLease.SetActive(view, ledger.Orders.Exists(order => order.Mode == WorksOrderMode.Keep || order.Remaining > 0 || order.InProduction > 0));
    }

    private static void Describe(Ledger ledger, Dictionary<string, int> stock, Dictionary<string, int> machines)
    {
        var pool = new Dictionary<string, int>(machines, StringComparer.Ordinal);
        foreach (var order in ledger.Orders)
        {
            pool.TryGetValue(order.Output, out var running);
            if (order.Mode == WorksOrderMode.Make)
            {
                var still = Math.Max(0, order.Count - order.Ready - order.Collected);
                var cover = Math.Min(still, running);
                pool[order.Output] = running - cover;
                order.InProduction = cover;
                order.Available = order.Ready + order.Collected;
                order.Remaining = still - cover;
                continue;
            }

            stock.TryGetValue(order.Output, out var held);
            held = Math.Max(0, held - Reserved(ledger, order.Output));
            var gap = Math.Max(0, order.Count - held);
            var filling = Math.Min(gap, running);
            pool[order.Output] = running - filling;
            order.InProduction = filling;
            order.Available = held;
            order.Remaining = gap - filling;
        }
    }

    private static int Reserved(Ledger ledger, string output)
    {
        var n = 0;
        foreach (var order in ledger.Orders)
        {
            if (order.Mode == WorksOrderMode.Make && order.Output == output)
                n += order.Ready;
        }

        return n;
    }

    private static bool LoadOne(WorksRecipe recipe, List<Hit> hits, Ledger ledger, Vector3 origin, long playerId)
    {
        if (!ledger.Pantry.TryGetValue(recipe.Input, out var held) || held < 1)
        {
            PullInto(ledger, recipe.Input, 1, origin, playerId);
            if (!ledger.Pantry.TryGetValue(recipe.Input, out held) || held < 1)
                return false;
        }

        foreach (var hit in hits)
        {
            var station = hit.Station;
            if (station?.m_nview == null || !station.m_nview.IsOwner())
                continue;
            if (station.GetQueueSize() >= station.m_maxOre || !station.IsItemAllowed(recipe.Input))
                continue;
            var source = station.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre);
            if (!string.IsNullOrEmpty(source) && source != recipe.Input && Clean(source) != recipe.Input)
                continue;
            if (!Fuel(station, recipe, ledger, origin, playerId))
                continue;
            ledger.Pantry[recipe.Input] = held - 1;
            if (ledger.Pantry[recipe.Input] <= 0)
                ledger.Pantry.Remove(recipe.Input);
            station.QueueOre(recipe.Input, false);
            hit.Info.Free = Math.Max(0, hit.Info.Free - 1);
            return true;
        }

        return false;
    }

    private static bool Fuel(Smelter station, WorksRecipe recipe, Ledger ledger, Vector3 origin, long playerId)
    {
        if (station.m_fuelItem == null || station.m_maxFuel <= 0 || station.GetFuel() >= 1f)
            return true;
        var prefab = string.IsNullOrEmpty(recipe.Fuel) ? Clean(station.m_fuelItem.gameObject.name) : recipe.Fuel;
        return AddFuel(station, prefab, ledger, origin, playerId);
    }

    // Ore already in a machine still burns fuel after the order stops queueing more.
    private static void FuelQueued(List<Hit> hits, Ledger ledger, Vector3 origin, long playerId)
    {
        foreach (var hit in hits)
        {
            var station = hit.Station;
            if (station?.m_nview == null || !station.m_nview.IsOwner())
                continue;
            if (station.m_fuelItem == null || station.m_maxFuel <= 0 || station.GetFuel() >= 1f)
                continue;
            if (station.GetQueueSize() <= 0)
                continue;
            var relevant = false;
            for (var i = 0; i < station.GetQueueSize(); i++)
            {
                var conversion = station.GetItemConversion(ProductionQueue.Input(station, i));
                if (conversion?.m_to != null && Wanted(ledger, Clean(conversion.m_to.gameObject.name)))
                { relevant = true; break; }
            }
            if (relevant) AddFuel(station, Clean(station.m_fuelItem.gameObject.name), ledger, origin, playerId);
        }
    }

    private static bool AddFuel(Smelter station, string prefab, Ledger ledger, Vector3 origin, long playerId)
    {
        if (string.IsNullOrEmpty(prefab))
            return false;
        if (!ledger.Pantry.TryGetValue(prefab, out var have) || have < 1)
        {
            PullInto(ledger, prefab, 1, origin, playerId);
            if (!ledger.Pantry.TryGetValue(prefab, out have) || have < 1)
                return false;
        }

        ledger.Pantry[prefab] = have - 1;
        if (ledger.Pantry[prefab] <= 0)
            ledger.Pantry.Remove(prefab);
        station.SetFuel(Math.Min(station.m_maxFuel, station.GetFuel() + 1f));
        return station.GetFuel() >= 1f;
    }

    private static void CollectFinished(List<Hit> hits, Ledger ledger)
    {
        foreach (var hit in hits)
        {
            var station = hit.Station;
            if (station?.m_nview == null || !station.m_nview.IsOwner() || station.GetProcessedQueueSize() <= 0)
                continue;
            var source = station.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre);
            var conversion = station.GetItemConversion(source);
            if (conversion?.m_to == null)
                continue;
            var made = Clean(conversion.m_to.gameObject.name);
            if (!Wanted(ledger, made))
                continue;
            var amount = station.GetProcessedQueueSize();
            Add(ledger, made, amount);
            var left = amount;
            foreach (var order in ledger.Orders)
            {
                if (left < 1 || order.Mode != WorksOrderMode.Make || order.Output != made)
                    continue;
                var room = Math.Max(0, order.Count - order.Ready - order.Collected);
                var give = Math.Min(room, left);
                order.Ready += give;
                left -= give;
            }

            station.m_nview.GetZDO().Set(ZDOVars.s_spawnOre, "");
            station.m_nview.GetZDO().Set(ZDOVars.s_spawnAmount, 0);
        }
    }

    private static bool Wanted(Ledger ledger, string output)
    {
        foreach (var order in ledger.Orders)
        {
            if (order.Output != output)
                continue;
            if (order.Mode == WorksOrderMode.Keep)
                return true;
            if (order.Ready + order.Collected < order.Count)
                return true;
        }

        return false;
    }

    private static void AddStep(List<WorksStep> steps, WorksRecipe recipe, int count, string parent, int depth, Dictionary<string, int> stock, HashSet<string> guard)
    {
        if (!guard.Add(recipe.Output) || depth > 6)
            return;
        stock.TryGetValue(recipe.Output, out var have);
        var step = new WorksStep
        {
            Output = recipe.Output,
            Name = recipe.OutputName,
            Parent = parent,
            Depth = depth,
            Need = count,
            Available = have,
            Station = recipe.Station
        };
        stock.TryGetValue(recipe.Input, out var inputHave);
        step.Uses.Add(new WorksUse
        {
            Item = recipe.Input,
            Name = recipe.InputName,
            Amount = count,
            Available = inputHave
        });
        if (!string.IsNullOrEmpty(recipe.Fuel))
        {
            stock.TryGetValue(recipe.Fuel, out var fuelHave);
            step.Uses.Add(new WorksUse
            {
                Item = recipe.Fuel,
                Name = recipe.FuelName,
                Amount = count,
                Available = fuelHave
            });
            step.Note = "Fuel is a planning estimate of one per product.";
        }

        steps.Add(step);
        var child = Find(recipe.Input);
        if (child != null)
            AddStep(steps, child, count, recipe.Output, depth + 1, stock, guard);
        if (!string.IsNullOrEmpty(recipe.Fuel))
        {
            var fuel = Find(recipe.Fuel);
            if (fuel != null)
                AddStep(steps, fuel, count, recipe.Output, depth + 1, stock, guard);
        }
    }

    private static List<Hit> Scan(Vector3 origin, bool claim, ZNetView? controller = null)
    {
        var list = new List<Hit>();
        var seen = new HashSet<int>();
        HashSet<string>? demand = null;
        if (claim && controller != null)
        {
            demand = new HashSet<string>(StringComparer.Ordinal);
            foreach (var order in Read(controller).Orders)
                if (order.Mode == WorksOrderMode.Keep || order.Count > order.Ready + order.Collected) demand.Add(order.Output);
        }
        var hits = Physics.OverlapSphere(origin, Range(), ~0, QueryTriggerInteraction.Collide);
        foreach (var col in hits)
        {
            if (col == null)
                continue;
            var station = col.GetComponentInParent<Smelter>();
            if (station == null || !seen.Add(station.GetInstanceID()) || FoodOven(Utils.GetPrefabName(station.gameObject)))
                continue;
            if (demand != null)
            {
                var relevant = false;
                foreach (var conversion in station.m_conversion)
                    if (conversion?.m_to != null && demand.Contains(Clean(conversion.m_to.gameObject.name))) { relevant = true; break; }
                if (!relevant) continue;
            }
            if (controller != null && !ProductionLease.Available(station.m_nview, controller)) continue;
            if (claim && (controller == null || !ProductionLease.Acquire(station.m_nview, controller))) continue;
            list.Add(Describe(station, false));
        }

        return list;
    }

    private static Hit Describe(Smelter station, bool claim)
    {
        var info = new WorksMachine
        {
            Name = LabelToken(station.m_name),
            Prefab = Utils.GetPrefabName(station.gameObject),
            Position = station.transform.position,
            FuelMax = station.m_maxFuel,
            FuelItem = station.m_fuelItem != null ? Clean(station.m_fuelItem.gameObject.name) : ""
        };
        if (station.m_nview != null && station.m_nview.IsValid())
        {
            info.Fuel = station.m_maxFuel > 0 ? Mathf.FloorToInt(station.GetFuel()) : 0;
            info.Cooking = station.GetQueueSize();
            info.Ready = station.GetProcessedQueueSize();
            info.Free = Math.Max(0, station.m_maxOre - station.GetQueueSize());
            if (claim && !station.m_nview.IsOwner())
                station.m_nview.ClaimOwnership();
        }

        if (station.m_maxFuel > 0 && info.Fuel < 1)
            info.Block = "Needs fuel";
        return new Hit { Station = station, Info = info };
    }

    private static Dictionary<string, int> Production(List<Hit> hits)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var hit in hits)
        {
            var station = hit.Station;
            if (station?.m_nview == null || !station.m_nview.IsValid())
                continue;
            void AddSource(string source, int count)
            {
                var conversion = string.IsNullOrEmpty(source) ? null : station.GetItemConversion(source);
                if (conversion?.m_to == null || count <= 0) return;
                var product = Clean(conversion.m_to.gameObject.name);
                map.TryGetValue(product, out var have);
                map[product] = have + count;
            }
            for (var i = 0; i < station.GetQueueSize(); i++) AddSource(ProductionQueue.Input(station, i), 1);
            AddSource(station.m_nview.GetZDO().GetString(ZDOVars.s_spawnOre), station.GetProcessedQueueSize());
        }

        return map;
    }

    private static Dictionary<string, int> Stock(Vector3 origin, long playerId, Ledger ledger)
    {
        var free = new Dictionary<string, int>(ledger.Pantry, StringComparer.Ordinal);
        void AddStock(string prefab, int count)
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
                if (item?.m_dropPrefab == null || !NearbyStorage.Spendable(item))
                    continue;
                AddStock(Clean(item.m_dropPrefab.name), item.m_stack);
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
                AddStock(Clean(item.m_dropPrefab.name), count);
            }
        }

        var lots = new List<NearbyLot>();
        NearbyLots.CollectAround(origin, Range(), lots);
        foreach (var lot in lots)
        {
            if (lot.Item?.m_dropPrefab == null)
                continue;
            AddStock(Clean(lot.Item.m_dropPrefab.name), lot.Count);
        }

        return free;
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
            var have = string.IsNullOrEmpty(shared) ? 0 : NearbyStorage.CountSpendable(inv, shared);
            var take = Math.Min(have, count);
            if (take > 0)
            {
                var got = NearbyStorage.TakeSpendable(inv, shared, take);
                if (got > 0)
                {
                    Add(ledger, prefab, got);
                    count -= got;
                }
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

    private static void Discover()
    {
        if (_recipes != null || ZNetScene.instance == null)
            return;
        var list = new List<WorksRecipe>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var prefab in ZNetScene.instance.m_prefabs)
        {
            if (prefab == null)
                continue;
            var station = prefab.GetComponent<Smelter>();
            if (station == null || FoodOven(Utils.GetPrefabName(prefab)))
                continue;
            var stationName = Utils.GetPrefabName(prefab);
            foreach (var conversion in station.m_conversion)
            {
                if (conversion?.m_from == null || conversion.m_to == null)
                    continue;
                var output = Clean(conversion.m_to.gameObject.name);
                if (string.IsNullOrEmpty(output) || !seen.Add(output))
                    continue;
                var fuel = station.m_fuelItem != null ? Clean(station.m_fuelItem.gameObject.name) : "";
                list.Add(new WorksRecipe
                {
                    Output = output,
                    OutputName = Label(output),
                    Input = Clean(conversion.m_from.gameObject.name),
                    InputName = Label(Clean(conversion.m_from.gameObject.name)),
                    Station = stationName,
                    StationName = LabelToken(station.m_name),
                    Fuel = fuel,
                    FuelName = string.IsNullOrEmpty(fuel) ? "" : Label(fuel)
                });
            }
        }

        _recipes = list;
    }

    private static bool FoodOven(string prefab) =>
        !string.IsNullOrEmpty(prefab) && prefab.IndexOf("oven", StringComparison.OrdinalIgnoreCase) >= 0;

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
        var shared = Shared(prefab);
        if (string.IsNullOrEmpty(shared) || Localization.instance == null)
            return prefab;
        return Localization.instance.Localize(shared);
    }

    private static string LabelToken(string token)
    {
        if (string.IsNullOrEmpty(token) || Localization.instance == null)
            return token ?? "";
        return Localization.instance.Localize(token);
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

    private static ZNetView? View(Piece board)
    {
        if (board == null)
            return null;
        var view = board.GetComponent<ZNetView>();
        return view != null && view.IsValid() ? view : null;
    }

    private static Ledger Read(ZNetView? view)
    {
        var ledger = new Ledger();
        if (view == null)
            return ledger;
        var text = view.GetZDO().GetString(Key);
        if (string.IsNullOrEmpty(text))
            return ledger;
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith("O\t", StringComparison.Ordinal))
            {
                var parts = line.Split('\t');
                if (parts.Length < 8)
                    continue;
                if (!int.TryParse(parts[1], out var id) || !int.TryParse(parts[3], out var mode)
                    || !int.TryParse(parts[4], out var count) || !long.TryParse(parts[5], out var player)
                    || !int.TryParse(parts[6], out var ready) || !int.TryParse(parts[7], out var collected))
                    continue;
                ledger.Orders.Add(new WorksOrder
                {
                    Id = id,
                    Output = parts[2],
                    Name = Label(parts[2]),
                    Mode = mode == 1 ? WorksOrderMode.Keep : WorksOrderMode.Make,
                    Count = count,
                    PlayerId = player,
                    Ready = ready,
                    Collected = collected
                });
            }
            else if (line.StartsWith("P\t", StringComparison.Ordinal))
            {
                var parts = line.Split('\t');
                if (parts.Length < 3 || !int.TryParse(parts[2], out var count) || count < 1)
                    continue;
                ledger.Pantry[parts[1]] = count;
            }
        }

        return ledger;
    }

    private static void Write(ZNetView view, Ledger ledger)
    {
        var text = new StringBuilder();
        foreach (var order in ledger.Orders)
        {
            text.Append("O\t").Append(order.Id).Append('\t').Append(order.Output).Append('\t')
                .Append(order.Mode == WorksOrderMode.Keep ? 1 : 0).Append('\t').Append(order.Count).Append('\t')
                .Append(order.PlayerId).Append('\t').Append(order.Ready).Append('\t').Append(order.Collected)
                .Append('\n');
        }

        foreach (var pair in ledger.Pantry)
        {
            if (pair.Value > 0)
                text.Append("P\t").Append(pair.Key).Append('\t').Append(pair.Value).Append('\n');
        }

        var body = text.ToString();
        var zdo = view.GetZDO();
        if (zdo.GetString(Key) == body)
            return;
        zdo.Set(Key, body);
    }

    internal static void Spill(Piece board)
    {
        var view = View(board);
        if (view == null || !view.IsValid())
            return;
        if (!view.IsOwner())
            view.ClaimOwnership();
        if (!view.IsOwner())
            return;
        var ledger = Read(view);
        var origin = board.transform.position + Vector3.up;
        var drops = new List<KeyValuePair<string, int>>(ledger.Pantry);
        view.GetZDO().Set(Key, "");
        ProductionLease.SetActive(view, false);
        foreach (var drop in drops)
            Drop(drop.Key, drop.Value, origin);
    }

    private static bool Controlled(ZNetView? view) => view != null && view.IsValid() && view.IsOwner();

    private static void Drop(string prefab, int count, Vector3 origin)
    {
        var item = Item(prefab);
        if (item?.m_itemData == null || count < 1)
            return;
        var left = count;
        var size = Math.Max(1, item.m_itemData.m_shared.m_maxStackSize);
        while (left > 0)
        {
            var stack = Math.Min(size, left);
            var clone = item.m_itemData.Clone();
            clone.m_stack = stack;
            clone.m_dropPrefab = item.gameObject;
            ItemDrop.DropItem(clone, stack, origin, Quaternion.identity);
            left -= stack;
        }
    }

    private sealed class Hit
    {
        public Smelter? Station;
        public WorksMachine Info = new();
    }

    private sealed class Ledger
    {
        public readonly List<WorksOrder> Orders = new();
        public readonly Dictionary<string, int> Pantry = new(StringComparer.Ordinal);
    }
}

[HarmonyPatch]
internal static class WorksSpill
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Destroy), typeof(HitData), typeof(bool))]
    private static void BeforeDestroy(WearNTear __instance)
    {
        var piece = __instance != null ? __instance.GetComponent<Piece>() : null;
        if (piece != null && piece.GetComponent<Board>() != null)
            WorksRun.Spill(piece);
    }
}
