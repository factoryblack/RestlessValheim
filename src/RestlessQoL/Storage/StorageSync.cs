using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

// Chest Save() only runs on the ZDO owner. Clients ask that peer to
// pull/push; they do not write a closed chest and then delete the source.
public sealed class StorageSync : FeatureModule
{
    public override string Id => "storage.sync";
    public override bool Enabled => true;

    private const string PullRpc = "RestlessPull";
    private const string PushRpc = "RestlessPush";
    private const string PulledRpc = "RestlessPulled";
    private const string PushedRpc = "RestlessPushed";
    private const string AckRpc = "RestlessTransferAck";
    private const string CancelRpc = "RestlessTransferCancel";

    private static int _nextId = 1;
    private static readonly Dictionary<int, Pending> Waiting = new();
    private static readonly Dictionary<string, int> Applied = new();
    private static readonly Dictionary<string, Hold> Holds = new();

    internal static int RequestPull(Container container, long playerId, string sharedName, int amount,
        bool honorLeaveOne, int quality, bool worldLevel, Action<int>? done)
    {
        if (container == null || amount <= 0 || string.IsNullOrEmpty(sharedName))
        {
            done?.Invoke(0);
            return 0;
        }

        // A synchronous caller can only be told about items this peer removed.
        // Claiming makes the chest writable so the craft payment is real.
        if (done == null || NearbyStorage.CanWrite(container))
        {
            var taken = TakeNow(container, sharedName, amount, honorLeaveOne, quality, worldLevel);
            done?.Invoke(taken);
            return taken;
        }

        Begin(container, PullRpc, (id, pkg) =>
        {
            pkg.Write(playerId);
            pkg.Write(id);
            pkg.Write(sharedName);
            pkg.Write(amount);
            pkg.Write(quality);
            pkg.Write(worldLevel);
            pkg.Write(honorLeaveOne);
        }, done);
        return 0;
    }

    internal static void RequestPush(Container container, long playerId, ItemDrop.ItemData item, ItemDrop? drop,
        Action<int> done)
    {
        if (done == null)
            return;
        if (container == null || item?.m_shared == null || item.m_stack <= 0)
        {
            done(0);
            return;
        }

        if (NearbyStorage.CanWrite(container))
        {
            done(NearbyStorage.PushOwned(container, item, drop));
            return;
        }

        var dropId = drop != null && drop.m_nview != null && drop.m_nview.IsValid()
            ? drop.m_nview.GetZDO().m_uid
            : ZDOID.None;
        var payload = WriteItem(item);
        Begin(container, PushRpc, (id, pkg) =>
        {
            pkg.Write(playerId);
            pkg.Write(id);
            pkg.Write(dropId);
            pkg.Write(payload);
        }, done);
    }

    private static int TakeNow(Container container, string sharedName, int amount, bool honorLeaveOne, int quality,
        bool worldLevel)
    {
        if (!NearbyStorage.CanWrite(container))
        {
            var view = container.m_nview;
            if (view != null && view.IsValid() && !view.IsOwner())
                view.ClaimOwnership();
        }

        if (!NearbyStorage.CanWrite(container))
            return 0;
        return NearbyStorage.PullOwned(container, sharedName, amount, honorLeaveOne, quality, worldLevel);
    }

    private static void Begin(Container container, string rpc, Action<int, ZPackage> fill, Action<int> done)
    {
        var view = container.m_nview;
        if (view == null || !view.IsValid())
        {
            done(0);
            return;
        }

        var id = NextId();
        var pkg = new ZPackage();
        fill(id, pkg);
        Waiting[id] = new Pending(container, rpc, pkg, done);
        Send(container, rpc, pkg);
        Plugin.Instance.StartCoroutine(Retry(id));
    }

    private static int NextId()
    {
        var id = _nextId++;
        if (_nextId <= 0)
            _nextId = 1;
        return id;
    }

    private static void Finish(int id, int taken)
    {
        if (id == 0 || !Waiting.TryGetValue(id, out var pending))
            return;
        Waiting.Remove(id);
        pending.Done(taken);
        if (taken > 0)
            Plugin.Instance.StartCoroutine(RepeatAck(pending.Container, id));
        else
            Cancel(pending.Container, id);
    }

    private static IEnumerator Retry(int id)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            yield return new WaitForSeconds(2f);
            if (!Waiting.TryGetValue(id, out var pending))
                yield break;
            Send(pending.Container, pending.Rpc, pending.Package);
        }

        yield return new WaitForSeconds(2f);
        if (!Waiting.TryGetValue(id, out var last))
            yield break;
        Waiting.Remove(id);
        Cancel(last.Container, id);
        last.Done(0);
    }

    private static void OnPull(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner())
            return;
        pkg.SetPos(0);
        var playerId = pkg.ReadLong();
        var id = pkg.ReadInt();
        var name = pkg.ReadString();
        var amount = pkg.ReadInt();
        var quality = pkg.ReadInt();
        var worldLevel = pkg.ReadBool();
        var honorLeave = pkg.ReadBool();
        if (id == 0 || !ModConfig.StorageEnabled.Value || !MayUse(container, playerId, sender))
        {
            Reply(container, sender, PulledRpc, id, 0);
            return;
        }

        var key = Key(sender, id);
        if (!Applied.TryGetValue(key, out var taken))
        {
            var items = NearbyStorage.Extract(container, name, amount, honorLeave, quality, worldLevel);
            taken = 0;
            foreach (var item in items)
                taken += item.m_stack;
            Applied[key] = taken;
            if (taken > 0)
                Holds[key] = new Hold(container, items, false);
        }

        Reply(container, sender, PulledRpc, id, taken);
    }

    private static void OnPush(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner())
            return;
        pkg.SetPos(0);
        var playerId = pkg.ReadLong();
        var id = pkg.ReadInt();
        pkg.ReadZDOID();
        var item = ReadItem(pkg.ReadPackage());
        if (id == 0 || !ModConfig.StorageEnabled.Value || item == null || !MayUse(container, playerId, sender))
        {
            Reply(container, sender, PushedRpc, id, 0);
            return;
        }

        var key = Key(sender, id);
        if (!Applied.TryGetValue(key, out var taken))
        {
            taken = NearbyStorage.PlanPush(container, item);
            Applied[key] = taken;
            if (taken > 0)
            {
                var clone = item.Clone();
                clone.m_stack = taken;
                clone.m_equipped = false;
                Holds[key] = new Hold(container, new List<ItemDrop.ItemData> { clone }, true);
            }
        }

        Reply(container, sender, PushedRpc, id, taken);
    }

    private static void OnAck(Container container, long sender, int id)
    {
        if (!container.IsOwner())
            return;
        var key = Key(sender, id);
        if (!Holds.TryGetValue(key, out var hold))
            return;
        Holds.Remove(key);
        if (!hold.Push)
            return;
        var item = hold.Items.Count > 0 ? hold.Items[0] : null;
        if (item == null)
            return;
        NearbyStorage.PushOwned(container, item, null);
        if (item.m_stack > 0)
            ItemDrop.DropItem(item, item.m_stack, container.transform.position + Vector3.up, Quaternion.identity);
    }

    private static void OnCancel(Container container, long sender, int id)
    {
        if (!container.IsOwner())
            return;
        var key = Key(sender, id);
        if (!Holds.TryGetValue(key, out var hold))
            return;
        Holds.Remove(key);
        if (!hold.Push)
            StorageWithdraw.ReturnTo(container, hold.Items);
    }

    // The player id in the package has to be the peer who sent it.
    // A dedicated tame has no player id. Accept that only from the server peer.
    private static bool MayUse(Container container, long playerId, long sender)
    {
        if (playerId != 0)
            return playerId == sender && container.CheckAccess(playerId);
        var server = ZNet.instance?.GetServerPeer();
        return server != null && sender == server.m_uid;
    }

    private static void Send(Container container, string rpc, ZPackage pkg)
    {
        var view = container != null ? container.m_nview : null;
        if (view == null || !view.IsValid())
            return;
        pkg.SetPos(0);
        view.InvokeRPC(rpc, pkg);
    }

    private static void Cancel(Container container, int id)
    {
        if (id == 0 || container?.m_nview == null || !container.m_nview.IsValid())
            return;
        container.m_nview.InvokeRPC(CancelRpc, id);
    }

    private static IEnumerator RepeatAck(Container container, int id)
    {
        yield return new WaitForSeconds(0.6f);
        if (container != null && container.m_nview != null && container.m_nview.IsValid())
            container.m_nview.InvokeRPC(AckRpc, id);
        yield return new WaitForSeconds(1.4f);
        if (container != null && container.m_nview != null && container.m_nview.IsValid())
            container.m_nview.InvokeRPC(AckRpc, id);
    }

    private static string Key(long sender, int id) => sender + ":" + id;

    private static void Reply(Container container, long sender, string rpc, int id, int taken)
    {
        if (id == 0)
            return;
        var view = container.m_nview;
        if (view == null || !view.IsValid())
            return;
        ZDOMan.instance?.ForceSendZDO(sender, view.GetZDO().m_uid);
        view.InvokeRPC(sender, rpc, id, taken);
    }

    private static ItemDrop? FindDrop(ZDOID id)
    {
        if (id == ZDOID.None || ZNetScene.instance == null)
            return null;
        var go = ZNetScene.instance.FindInstance(id);
        return go != null ? go.GetComponent<ItemDrop>() : null;
    }

    private static ZPackage WriteItem(ItemDrop.ItemData item)
    {
        var bag = new Inventory("RestlessSync", null, 1, 1);
        var clone = item.Clone();
        clone.m_equipped = false;
        bag.AddItem(clone, clone.m_stack, 0, 0, false);
        var pkg = new ZPackage();
        bag.Save(pkg);
        return pkg;
    }

    private static ItemDrop.ItemData? ReadItem(ZPackage pkg)
    {
        if (pkg == null)
            return null;
        pkg.SetPos(0);
        var bag = new Inventory("RestlessSync", null, 1, 1);
        bag.Load(pkg);
        foreach (var item in bag.GetAllItems())
        {
            if (item != null)
                return item;
        }

        return null;
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
        private static void AfterAwake(Container __instance)
        {
            var view = __instance.m_nview;
            if (view == null || !view.IsValid() || __instance.GetComponent<Hook>() != null)
                return;
            __instance.gameObject.AddComponent<Hook>();
            view.Register<ZPackage>(PullRpc, (long sender, ZPackage pkg) => OnPull(__instance, sender, pkg));
            view.Register<ZPackage>(PushRpc, (long sender, ZPackage pkg) => OnPush(__instance, sender, pkg));
            view.Register<int, int>(PulledRpc, (_, id, taken) => Finish(id, taken));
            view.Register<int, int>(PushedRpc, (_, id, taken) => Finish(id, taken));
            view.Register<int>(AckRpc, (long sender, int id) => OnAck(__instance, sender, id));
            view.Register<int>(CancelRpc, (long sender, int id) => OnCancel(__instance, sender, id));
            view.Register<ZPackage>(StorageWithdraw.RequestRpc,
                (long sender, ZPackage pkg) => StorageWithdraw.OnRequest(__instance, sender, pkg));
            view.Register<ZPackage>(StorageWithdraw.ReplyRpc,
                (long sender, ZPackage pkg) => StorageWithdraw.OnReply(sender, pkg));
            view.Register<ZPackage>(StorageWithdraw.AckRpc,
                (long sender, ZPackage pkg) => StorageWithdraw.OnAck(__instance, sender, pkg));
            view.Register<ZPackage>(StorageWithdraw.CancelRpc,
                (long sender, ZPackage pkg) => StorageWithdraw.OnCancel(__instance, sender, pkg));
        }
    }

    private sealed class Hook : MonoBehaviour
    {
    }

    private sealed class Pending
    {
        public readonly Container Container;
        public readonly string Rpc;
        public readonly ZPackage Package;
        public readonly Action<int> Done;
        public Pending(Container container, string rpc, ZPackage package, Action<int> done)
        {
            Container = container;
            Rpc = rpc;
            Package = package;
            Done = done;
        }
    }

    private sealed class Hold
    {
        public readonly Container Container;
        public readonly List<ItemDrop.ItemData> Items;
        public readonly bool Push;
        public Hold(Container container, List<ItemDrop.ItemData> items, bool push)
        {
            Container = container;
            Items = items;
            Push = push;
        }
    }
}
