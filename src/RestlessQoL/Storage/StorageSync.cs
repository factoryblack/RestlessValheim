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

    private const string PullRpc = "RestlessPullV2";
    private const string PushRpc = "RestlessPushV2";
    private const string PulledRpc = "RestlessPulledV2";
    private const string PushedRpc = "RestlessPushedV2";
    public override bool TickInMenus => true;
    public override void Tick() => TransferDelivery.Tick();

    private static readonly Dictionary<int, Pending> Waiting = new();
    internal static bool WaitingFor(int id) => Waiting.ContainsKey(id);

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
        }, done, amount);
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
        }, done, item.m_stack, taken =>
        {
            if (drop != null)
                return drop.m_nview != null && drop.m_nview.IsValid() && drop.m_nview.IsOwner()
                    && drop.m_itemData == item && item.m_stack >= taken;
            var player = Player.m_localPlayer;
            return player != null && player.GetInventory().ContainsItem(item) && NearbyStorage.Spendable(item) && item.m_stack >= taken;
        });
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

    private static void Begin(Container container, string rpc, Action<int, ZPackage> fill, Action<int> done, int limit = int.MaxValue, Func<int, bool>? accept = null)
    {
        var view = container.m_nview;
        if (view == null || !view.IsValid())
        {
            done(0);
            return;
        }

        var id = TransferDelivery.NextId();
        if (id == 0) { done(0); return; }
        var pkg = new ZPackage();
        fill(id, pkg);
        Waiting[id] = new Pending(container, rpc, pkg, done, Player.m_localPlayer.GetPlayerID(), limit, accept);
        TransferDelivery.Remember(container, id, rpc == PushRpc ? "S" : "P", 0);
        Send(container, rpc, pkg);
        Plugin.Instance.StartCoroutine(Retry(id));
    }

    private static void Finish(Container container, long sender, int id, int taken)
    {
        if (id == 0 || !Waiting.TryGetValue(id, out var pending) || pending.Container != container
            || container.m_nview.GetZDO().GetOwner() != sender) return;
        Waiting.Remove(id);
        var kind = pending.Rpc == PushRpc ? "S" : "P";
        if (Player.m_localPlayer == null || Player.m_localPlayer.GetPlayerID() != pending.Actor) return;
        if (taken < 0 || taken > pending.Limit || (taken > 0 && pending.Accept != null && !pending.Accept(taken))) taken = 0;
        // A single main-thread callback applies the source/destination before the decision is sent.
        pending.Done(taken);
        TransferDelivery.Remember(container, id, kind, taken > 0 ? 1 : -1);
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
        if (Player.m_localPlayer != null && Player.m_localPlayer.GetPlayerID() == last.Actor)
        {
            TransferDelivery.Remember(last.Container, id, last.Rpc == PushRpc ? "S" : "P", -1);
            last.Done(0);
        }
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
        if (id <= 0 || !ModConfig.StorageEnabled.Value || !MayUse(container, playerId, sender))
        {
            Reply(container, sender, PulledRpc, id, 0);
            return;
        }

        var book = TransferDelivery.Read(container);
        var row = book.Prepare(playerId, id, "P", () =>
        {
            var items = NearbyStorage.Extract(container, name, amount, honorLeave, quality, worldLevel);
            var count = 0;
            foreach (var item in items) count += item.m_stack;
            return new TransferBook.Entry { Count = count, Payload = TransferDelivery.Pack(items),
                State = count > 0 ? TransferBook.Phase.Held : TransferBook.Phase.Cancelled };
        });
        TransferDelivery.Write(container, book);
        var taken = row.State == TransferBook.Phase.Held ? row.Count : 0;

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
        if (id <= 0 || !ModConfig.StorageEnabled.Value || item == null || !MayUse(container, playerId, sender))
        {
            Reply(container, sender, PushedRpc, id, 0);
            return;
        }

        var book = TransferDelivery.Read(container);
        var row = book.Prepare(playerId, id, "S", () =>
        {
            var count = NearbyStorage.PlanPush(container, item);
            var clone = item.Clone(); clone.m_stack = count; clone.m_equipped = false;
            return new TransferBook.Entry { Count = count,
                Payload = count > 0 ? TransferDelivery.Pack(new List<ItemDrop.ItemData> { clone }) : "",
                State = count > 0 ? TransferBook.Phase.Held : TransferBook.Phase.Cancelled };
        });
        TransferDelivery.Write(container, book);
        Reply(container, sender, PushedRpc, id, row.State == TransferBook.Phase.Held ? row.Count : 0);
    }

    private static bool MayUse(Container container, long playerId, long sender) => TransferDelivery.MayUse(container, playerId, sender);

    private static void Send(Container container, string rpc, ZPackage pkg)
    {
        var view = container != null ? container.m_nview : null;
        if (view == null || !view.IsValid()) return;
        pkg.SetPos(0); view.InvokeRPC(rpc, pkg);
    }

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
            view.Register<int, int>(PulledRpc, (sender, id, taken) => Finish(__instance, sender, id, taken));
            view.Register<int, int>(PushedRpc, (sender, id, taken) => Finish(__instance, sender, id, taken));
            view.Register<ZPackage>(TransferDelivery.DecisionRpc, (sender, pkg) => TransferDelivery.OnDecision(__instance, sender, pkg));
            view.Register<ZPackage>(TransferDelivery.ClosedRpc, (sender, pkg) => TransferDelivery.OnClosed(__instance, sender, pkg));
            view.Register<ZPackage>(StorageWithdraw.RequestRpc,
                (long sender, ZPackage pkg) => StorageWithdraw.OnRequest(__instance, sender, pkg));
            view.Register<ZPackage>(StorageWithdraw.ReplyRpc,
                (long sender, ZPackage pkg) => StorageWithdraw.OnReply(__instance, sender, pkg));

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
        public readonly long Actor;
        public readonly int Limit;
        public readonly Func<int, bool>? Accept;
        public Pending(Container container, string rpc, ZPackage package, Action<int> done, long actor, int limit, Func<int, bool>? accept)
        { Container = container; Rpc = rpc; Package = package; Done = done; Actor = actor; Limit = limit; Accept = accept; }
    }
}
