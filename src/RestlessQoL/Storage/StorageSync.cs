using System;
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

    private static int _nextId = 1;
    private static readonly Dictionary<int, Action<int>> Waiting = new();

    internal static int RequestPull(Container container, long playerId, string sharedName, int amount,
        bool honorLeaveOne, int quality, bool worldLevel, Action<int>? done)
    {
        if (container == null || amount <= 0)
        {
            done?.Invoke(0);
            return 0;
        }

        if (NearbyStorage.CanWrite(container))
        {
            var taken = NearbyStorage.PullOwned(container, sharedName, amount, honorLeaveOne, quality, worldLevel);
            done?.Invoke(taken);
            return taken;
        }

        var predicted = PredictedPull(container, sharedName, amount, honorLeaveOne, quality, worldLevel);
        if (predicted <= 0)
        {
            done?.Invoke(0);
            return 0;
        }

        var id = done == null ? 0 : NextId();
        if (done != null)
            Waiting[id] = done;

        var pkg = new ZPackage();
        pkg.Write(playerId);
        pkg.Write(id);
        pkg.Write(sharedName);
        pkg.Write(predicted);
        pkg.Write(quality);
        pkg.Write(worldLevel);
        pkg.Write(honorLeaveOne);
        container.m_nview.InvokeRPC(PullRpc, pkg);
        return predicted;
    }

    internal static void RequestPush(Container container, long playerId, ItemDrop.ItemData item, ItemDrop? drop,
        Action<int> done)
    {
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

        var id = NextId();
        Waiting[id] = done;
        var pkg = new ZPackage();
        pkg.Write(playerId);
        pkg.Write(id);
        pkg.Write(drop != null && drop.m_nview != null && drop.m_nview.IsValid()
            ? drop.m_nview.GetZDO().m_uid
            : ZDOID.None);
        pkg.Write(WriteItem(item));
        container.m_nview.InvokeRPC(PushRpc, pkg);
    }

    private static int NextId()
    {
        var id = _nextId++;
        if (_nextId <= 0)
            _nextId = 1;
        return id;
    }

    private static int PredictedPull(Container container, string sharedName, int amount, bool honorLeaveOne,
        int quality, bool worldLevel)
    {
        var inventory = container.GetInventory();
        if (inventory == null)
            return 0;
        var have = inventory.CountItems(sharedName, quality, worldLevel);
        return Mathf.Min(amount, NearbyStorage.Pullable(have, honorLeaveOne));
    }

    private static void Finish(int id, int taken)
    {
        if (id == 0 || !Waiting.TryGetValue(id, out var done))
            return;
        Waiting.Remove(id);
        done(taken);
    }

    private static void OnPull(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner() || !ModConfig.StorageEnabled.Value)
            return;
        pkg.SetPos(0);
        var playerId = pkg.ReadLong();
        var id = pkg.ReadInt();
        var name = pkg.ReadString();
        var amount = pkg.ReadInt();
        var quality = pkg.ReadInt();
        var worldLevel = pkg.ReadBool();
        var honorLeave = pkg.ReadBool();
        var taken = 0;
        if (container.CheckAccess(playerId))
            taken = NearbyStorage.PullOwned(container, name, amount, honorLeave, quality, worldLevel);
        Reply(container, sender, PulledRpc, id, taken);
    }

    private static void OnPush(Container container, long sender, ZPackage pkg)
    {
        if (!container.IsOwner() || !ModConfig.StorageEnabled.Value)
            return;
        pkg.SetPos(0);
        var playerId = pkg.ReadLong();
        var id = pkg.ReadInt();
        var dropId = pkg.ReadZDOID();
        var item = ReadItem(pkg.ReadPackage());
        var taken = 0;
        if (item != null && container.CheckAccess(playerId))
            taken = NearbyStorage.PushOwned(container, item, FindDrop(dropId));
        Reply(container, sender, PushedRpc, id, taken);
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
            view.Register<int, int>(PulledRpc, (_, id, taken) => Finish(id, taken));
            view.Register<int, int>(PushedRpc, (_, id, taken) => Finish(id, taken));
        }
    }

    private sealed class Hook : MonoBehaviour
    {
    }
}
