using System;
using System.Collections;
using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

// The chest owner removes the stacks, then sends those exact items back.
// Holds persist on the chest until a confirmed character decision settles them.
public static class StorageWithdraw
{
    internal const string RequestRpc = "RestlessWithdrawV2";
    internal const string ReplyRpc = "RestlessWithdrawnV2";


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

    private sealed class Pending
    {
        internal Container Container = null!;
        internal long Actor;
        internal Action<Batch> Done = null!;
        internal ZPackage Package = null!;
    }
    private static readonly Dictionary<int, Pending> Waiting = new();
    internal static bool WaitingFor(int id) => Waiting.ContainsKey(id);

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

        var id = TransferDelivery.NextId();
        if (id == 0) { done(new Batch(0, new List<ItemDrop.ItemData>(), true)); return; }
        var pkg = new ZPackage();
        pkg.Write(playerId);
        pkg.Write(id);
        pkg.Write(identity);
        pkg.Write(amount);
        Waiting[id] = new Pending { Container = container, Actor = playerId, Done = done, Package = pkg };
        TransferDelivery.Remember(container, id, "W", 0);
        Send(container,pkg);
        Plugin.Instance.StartCoroutine(ExpireRequest(id));
    }

    public static void Acknowledge(Container container, int id, List<ItemDrop.ItemData> overflow)
    {
        if (id != 0) TransferDelivery.Remember(container, id, "W", 1, TransferDelivery.Pack(overflow));
    }

    public static void Cancel(Container container, int id)
    {
        if (id != 0) TransferDelivery.Remember(container, id, "W", -1);
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
        if (id <= 0 || !ModConfig.StorageEnabled.Value || !TransferDelivery.MayUse(container, playerId, sender))
        { Reply(container, sender, id, new List<ItemDrop.ItemData>()); return; }
        var book = TransferDelivery.Read(container);
        var row = book.Prepare(playerId, id, "W", () =>
        {
            var items = Extract(container, identity, amount);
            var count = 0; foreach (var item in items) count += item.m_stack;
            return new TransferBook.Entry { Count = count, Payload = TransferDelivery.Pack(items),
                State = count > 0 ? TransferBook.Phase.Held : TransferBook.Phase.Cancelled };
        });
        TransferDelivery.Write(container, book);
        Reply(container, sender, id, row.State == TransferBook.Phase.Held ? TransferDelivery.Unpack(row.Payload) : new List<ItemDrop.ItemData>());
    }

    internal static void OnReply(Container container, long sender, ZPackage pkg)
    {
        pkg.SetPos(0);
        var id = pkg.ReadInt(); var items = ReadItems(pkg.ReadPackage());
        if (!Waiting.TryGetValue(id, out var pending) || pending.Container != container
            || container.m_nview.GetZDO().GetOwner() != sender) return;
        Waiting.Remove(id);
        if (Player.m_localPlayer == null || Player.m_localPlayer.GetPlayerID() != pending.Actor) return;
        pending.Done(new Batch(id, items, false));
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

    // Exact identity (quality/world level/custom data included), not shared name.
    public static bool Contains(Inventory inventory, string identity)
    {
        if (inventory == null || string.IsNullOrEmpty(identity)) return false;
        return ItemKey.Contains(inventory.GetAllItems(),identity);
    }

    private static void Send(Container container, ZPackage pkg)
    {
        var view = container != null ? container.m_nview : null;
        if (view == null || !view.IsValid()) return;
        pkg.SetPos(0); view.InvokeRPC(RequestRpc,pkg);
    }

    private static IEnumerator ExpireRequest(int id)
    {
        // Lost requests/replies and owner hand-offs should not turn a single
        // packet loss into an eight-second wait. Replay the same reservation ID.
        for (var attempt = 0; attempt < 7; attempt++)
        {
            yield return new WaitForSecondsRealtime(1f);
            if (!Waiting.TryGetValue(id,out var pending)) yield break;
            Send(pending.Container,pending.Package);
        }
        yield return new WaitForSecondsRealtime(1f);
        if (!Waiting.TryGetValue(id, out var last)) yield break;
        Waiting.Remove(id);
        if (Player.m_localPlayer == null || Player.m_localPlayer.GetPlayerID() != last.Actor) yield break;
        Cancel(last.Container, id);
        last.Done(new Batch(id, new List<ItemDrop.ItemData>(), true));
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

    internal static List<ItemDrop.ItemData> Within(List<ItemDrop.ItemData> held, List<ItemDrop.ItemData> claimed)
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

    internal static ZPackage WriteItems(List<ItemDrop.ItemData> items)
    {
        var width = Math.Max(1, items.Count);
        var bag = new Inventory("RestlessWithdrawV2", null, width, 1);
        for (var i = 0; i < items.Count; i++)
        {
            var clone = Copy(items[i], items[i].m_stack);
            bag.AddItem(clone, clone.m_stack, i, 0, false);
        }

        var pkg = new ZPackage();
        bag.Save(pkg);
        return pkg;
    }

    internal static List<ItemDrop.ItemData> ReadItems(ZPackage pkg)
    {
        var items = new List<ItemDrop.ItemData>();
        if (pkg == null)
            return items;
        pkg.SetPos(0);
        var bag = new Inventory("RestlessWithdrawV2", null, 8, 4);
        bag.Load(pkg);
        foreach (var item in bag.GetAllItems())
        {
            if (item != null && item.m_stack > 0)
                items.Add(item);
        }

        return items;
    }

}

