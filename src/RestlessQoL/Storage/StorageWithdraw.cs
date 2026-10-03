using System;
using System.Collections;
using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

// The chest owner removes the stacks, then sends those exact items back.
// Until the taker acknowledges, the owner still holds them and puts them
// back on timeout. A repeated acknowledgement does nothing.
public static class StorageWithdraw
{
    internal const string RequestRpc = "RestlessWithdraw";
    internal const string ReplyRpc = "RestlessWithdrawn";
    internal const string AckRpc = "RestlessWithdrawAck";
    internal const string CancelRpc = "RestlessWithdrawCancel";

    public sealed class Batch
    {
        public int Id { get; }
        public List<ItemDrop.ItemData> Items { get; }
        public bool TimedOut { get; }
        public Batch(int id, List<ItemDrop.ItemData> items, bool timedOut)
        {
            Id = id;
            Items = items;
            TimedOut = timedOut;
        }
    }

    private static int _nextId = 1;
    private static readonly Dictionary<int, Action<Batch>> Waiting = new();
    private static readonly Dictionary<string, Hold> Holds = new();

    public static void Request(Container container, long playerId, string identity, int amount, Action<Batch> done)
    {
        if (done == null)
            return;
        if (container == null || amount <= 0 || string.IsNullOrEmpty(identity))
        {
            done(new Batch(0, new List<ItemDrop.ItemData>(), false));
            return;
        }

        if (NearbyStorage.CanWrite(container))
        {
            done(new Batch(0, Extract(container, identity, amount), false));
            return;
        }

        var view = container.m_nview;
        if (view == null || !view.IsValid())
        {
            done(new Batch(0, new List<ItemDrop.ItemData>(), true));
            return;
        }

        var id = NextId();
        Waiting[id] = done;
        Plugin.Instance.StartCoroutine(ExpireRequest(id));
        var pkg = new ZPackage();
        pkg.Write(playerId);
        pkg.Write(id);
        pkg.Write(identity);
        pkg.Write(amount);
        view.InvokeRPC(RequestRpc, pkg);
    }

    public static void Acknowledge(Container container, int id, List<ItemDrop.ItemData> overflow)
    {
        if (id == 0 || container?.m_nview == null || !container.m_nview.IsValid())
            return;
        SendAck(container, id, overflow);
        Plugin.Instance.StartCoroutine(RepeatAck(container, id, overflow));
    }

    public static void Cancel(Container container, int id)
    {
        if (id == 0 || container?.m_nview == null || !container.m_nview.IsValid())
            return;
        var pkg = new ZPackage();
        pkg.Write(id);
        container.m_nview.InvokeRPC(CancelRpc, pkg);
    }

    public static int Give(Inventory dest, List<ItemDrop.ItemData> items, List<ItemDrop.ItemData> overflow)
    {
        var added = 0;
        if (dest == null || items == null)
            return 0;
        foreach (var item in items)
        {
            if (item == null || item.m_stack <= 0)
                continue;
            var fit = Fit(dest, item);
            var landed = 0;
            if (fit > 0)
            {
                var clone = Copy(item, fit);
                var before = clone.m_stack;
                dest.AddItem(clone);
                landed = before - Math.Max(0, clone.m_stack);
            }

            added += landed;
            if (landed < item.m_stack)
                overflow.Add(Copy(item, item.m_stack - landed));
        }

        return added;
    }

    public static void ReturnTo(Container container, List<ItemDrop.ItemData> items)
    {
        if (items == null || items.Count == 0 || container == null)
            return;
        var inventory = container.GetInventory();
        foreach (var item in items)
        {
            if (item == null || item.m_stack <= 0)
                continue;
            var clone = Copy(item, item.m_stack);
            if (inventory != null && inventory.AddItem(clone))
                continue;
            ItemDrop.DropItem(clone, clone.m_stack, container.transform.position + Vector3.up, Quaternion.identity);
            Plugin.Log.LogWarning("Restless withdraw put a stack on the ground; the chest had no room to take it back.");
        }

        inventory?.Changed(true, false);
    }

    internal static void OnRequest(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner())
            return;
        pkg.SetPos(0);
        var playerId = pkg.ReadLong();
        var id = pkg.ReadInt();
        var identity = pkg.ReadString();
        var amount = pkg.ReadInt();
        var items = new List<ItemDrop.ItemData>();
        if (ModConfig.StorageEnabled.Value && MayUse(container, playerId, sender))
            items = Extract(container, identity, amount);
        if (items.Count > 0)
        {
            var key = Key(sender, id);
            var hold = new Hold(container, items);
            Holds[key] = hold;
            hold.Timer = Plugin.Instance.StartCoroutine(ExpireHold(key));
        }

        Reply(container, sender, id, items);
    }

    internal static void OnReply(long sender, ZPackage pkg)
    {
        pkg.SetPos(0);
        var id = pkg.ReadInt();
        var items = ReadItems(pkg.ReadPackage());
        if (!Waiting.TryGetValue(id, out var done))
            return;
        Waiting.Remove(id);
        done(new Batch(id, items, false));
    }

    internal static void OnAck(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner())
            return;
        pkg.SetPos(0);
        var id = pkg.ReadInt();
        var key = Key(sender, id);
        if (!Holds.TryGetValue(key, out var hold))
            return;
        Holds.Remove(key);
        if (hold.Timer != null)
            Plugin.Instance.StopCoroutine(hold.Timer);
        ReturnTo(container, Within(hold.Items, ReadItems(pkg.ReadPackage())));
    }

    internal static void OnCancel(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner())
            return;
        pkg.SetPos(0);
        var id = pkg.ReadInt();
        var key = Key(sender, id);
        if (!Holds.TryGetValue(key, out var hold))
            return;
        Holds.Remove(key);
        if (hold.Timer != null)
            Plugin.Instance.StopCoroutine(hold.Timer);
        ReturnTo(container, hold.Items);
    }

    private static List<ItemDrop.ItemData> Extract(Container container, string identity, int amount)
    {
        var taken = new List<ItemDrop.ItemData>();
        if (!NearbyStorage.CanWrite(container) || amount <= 0 || string.IsNullOrEmpty(identity))
            return taken;
        var inventory = container.GetInventory();
        if (inventory == null)
        {
            container.Load();
            inventory = container.GetInventory();
        }

        if (inventory == null)
            return taken;

        using (NearbyStorage.SuppressPatches())
        {
            var left = amount;
            foreach (var item in inventory.GetAllItems().ToArray())
            {
                if (left <= 0)
                    break;
                if (item == null || item.m_stack <= 0 || ItemKey.Of(item) != identity)
                    continue;
                var count = Math.Min(item.m_stack, left);
                var clone = Copy(item, count);
                if (!inventory.RemoveItem(item, count))
                    continue;
                taken.Add(clone);
                left -= count;
            }

            if (taken.Count > 0)
                inventory.Changed(true, false);
        }

        return taken;
    }

    private static int Fit(Inventory dest, ItemDrop.ItemData item)
    {
        var lo = 0;
        var hi = item.m_stack;
        while (lo < hi)
        {
            var mid = (lo + hi + 1) / 2;
            var probe = Copy(item, mid);
            if (dest.CanAddItem(probe, mid))
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo;
    }

    private static ItemDrop.ItemData Copy(ItemDrop.ItemData item, int stack)
    {
        var clone = item.Clone();
        clone.m_stack = stack;
        clone.m_equipped = false;
        return clone;
    }

    private static void SendAck(Container container, int id, List<ItemDrop.ItemData> overflow)
    {
        var pkg = new ZPackage();
        pkg.Write(id);
        pkg.Write(WriteItems(overflow ?? new List<ItemDrop.ItemData>()));
        container.m_nview.InvokeRPC(AckRpc, pkg);
    }

    private static IEnumerator RepeatAck(Container container, int id, List<ItemDrop.ItemData> overflow)
    {
        yield return new WaitForSeconds(0.6f);
        if (container != null && container.m_nview != null && container.m_nview.IsValid())
            SendAck(container, id, overflow);
        yield return new WaitForSeconds(1.4f);
        if (container != null && container.m_nview != null && container.m_nview.IsValid())
            SendAck(container, id, overflow);
    }

    private static IEnumerator ExpireRequest(int id)
    {
        yield return new WaitForSeconds(8f);
        if (!Waiting.TryGetValue(id, out var done))
            yield break;
        Waiting.Remove(id);
        done(new Batch(id, new List<ItemDrop.ItemData>(), true));
    }

    private static IEnumerator ExpireHold(string key)
    {
        yield return new WaitForSeconds(10f);
        if (!Holds.TryGetValue(key, out var hold))
            yield break;
        Plugin.Log.LogWarning("Restless withdraw is still holding " + hold.Items.Count + " stack(s) for an acknowledgement.");
    }

    private static void Reply(Container container, long sender, int id, List<ItemDrop.ItemData> items)
    {
        var view = container.m_nview;
        if (view == null || !view.IsValid())
            return;
        var pkg = new ZPackage();
        pkg.Write(id);
        pkg.Write(WriteItems(items));
        if (items.Count > 0)
            ZDOMan.instance?.ForceSendZDO(sender, view.GetZDO().m_uid);
        view.InvokeRPC(sender, ReplyRpc, pkg);
    }

    private static bool MayUse(Container container, long playerId, long sender)
    {
        if (playerId != 0)
            return playerId == sender && container.CheckAccess(playerId);
        var server = ZNet.instance?.GetServerPeer();
        return server != null && sender == server.m_uid;
    }

    private static List<ItemDrop.ItemData> Within(List<ItemDrop.ItemData> held, List<ItemDrop.ItemData> claimed)
    {
        var left = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in held)
        {
            if (item == null || item.m_stack <= 0)
                continue;
            var id = ItemKey.Of(item);
            left.TryGetValue(id, out var have);
            left[id] = have + item.m_stack;
        }

        var ok = new List<ItemDrop.ItemData>();
        foreach (var item in claimed)
        {
            if (item == null || item.m_stack <= 0)
                continue;
            var id = ItemKey.Of(item);
            if (!left.TryGetValue(id, out var have) || have <= 0)
                continue;
            var count = Math.Min(have, item.m_stack);
            left[id] = have - count;
            ok.Add(Copy(item, count));
        }

        return ok;
    }

    private static ZPackage WriteItems(List<ItemDrop.ItemData> items)
    {
        var width = Math.Max(1, items.Count);
        var bag = new Inventory("RestlessWithdraw", null, width, 1);
        for (var i = 0; i < items.Count; i++)
        {
            var clone = Copy(items[i], items[i].m_stack);
            bag.AddItem(clone, clone.m_stack, i, 0, false);
        }

        var pkg = new ZPackage();
        bag.Save(pkg);
        return pkg;
    }

    private static List<ItemDrop.ItemData> ReadItems(ZPackage pkg)
    {
        var items = new List<ItemDrop.ItemData>();
        if (pkg == null)
            return items;
        pkg.SetPos(0);
        var bag = new Inventory("RestlessWithdraw", null, 8, 4);
        bag.Load(pkg);
        foreach (var item in bag.GetAllItems())
        {
            if (item != null && item.m_stack > 0)
                items.Add(item);
        }

        return items;
    }

    private static int NextId()
    {
        var id = _nextId++;
        if (_nextId <= 0)
            _nextId = 1;
        return id;
    }

    private static string Key(long sender, int id) => sender + ":" + id;

    private sealed class Hold
    {
        public readonly Container Container;
        public readonly List<ItemDrop.ItemData> Items;
        public Coroutine? Timer;
        public Hold(Container container, List<ItemDrop.ItemData> items)
        {
            Container = container;
            Items = items;
        }
    }
}
