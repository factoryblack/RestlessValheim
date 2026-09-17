using System;
using System.Collections.Generic;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.Storage;

public static class NearbyStorage
{
    [ThreadStatic]
    private static int _skipPatches;

    [ThreadStatic]
    private static int _directTake;

    private static readonly List<Container> Cache = new();
    private static int _cacheFrame = -1;
    private static int _cachePlayer;
    private static float _cacheRange;

    internal static bool SkipPatches => _skipPatches > 0;
    internal static bool DirectTake => _directTake > 0;

    internal static IDisposable SuppressPatches()
    {
        _skipPatches++;
        return new Pop(() => _skipPatches--);
    }

    // Station / future pantry: consume from chests even when LeaveOne is on.
    internal static IDisposable BeginDirectTake()
    {
        _directTake++;
        return new Pop(() => _directTake--);
    }

    // Placement ghosts are chest prefabs with a ZNetView and no ZDO. Awake
    // bails before m_inventory / m_piece exist. Load() then NRE on GetZDO()
    // and CheckAccess NRE on a private chest's null m_piece — every frame
    // the ghost is out, which is only when the selected piece is a container.
    private static bool Ready(Container container)
    {
        if (container == null || !container.isActiveAndEnabled)
            return false;
        var view = container.m_nview;
        return view != null && view.IsValid();
    }

    // Chest OnContainerChanged only Save()s when IsOwner(). A client
    // RemoveItem/AddItem is local theatre; vacuum then Destroy()s the pile
    // and the host ZDO never saw it. Only mutate chests we already own.
    internal static bool CanWrite(Container container) =>
        Ready(container) && container.IsOwner();

    public static bool IsLocalPlayerInventory(Inventory inventory)
    {
        var player = Player.m_localPlayer;
        return player != null && inventory != null && player.GetInventory() == inventory;
    }

    public static int CountVanilla(Inventory inventory, string sharedName, int quality = -1, bool worldLevel = false)
    {
        using (SuppressPatches())
            return inventory.CountItems(sharedName, quality, worldLevel);
    }

    public static IEnumerable<Container> ForPlayer(Player player, float range)
    {
        if (player == null || range <= 0f)
            return Array.Empty<Container>();

        var frame = Time.frameCount;
        var id = player.GetInstanceID();
        if (frame == _cacheFrame && id == _cachePlayer && Mathf.Approximately(_cacheRange, range))
            return Cache;

        Cache.Clear();
        _cacheFrame = frame;
        _cachePlayer = id;
        _cacheRange = range;

        foreach (var container in NearbyQuery.UniqueInSphere<Container>(player.transform.position, range))
        {
            if (!Ready(container) || !container.CheckAccess(player.GetPlayerID()))
                continue;
            if (container.GetInventory() == null)
                container.Load();
            if (container.GetInventory() == null)
                continue;
            Cache.Add(container);
        }

        return Cache;
    }

    public static IEnumerable<Container> ForLocalPlayer()
    {
        var player = Player.m_localPlayer;
        if (player == null || !ModConfig.StorageEnabled.Value)
            return Array.Empty<Container>();
        return ForPlayer(player, ModConfig.StorageRange.Value);
    }

    // Chests around a world point (tames, stations). playerId 0 skips access on dedicated.
    public static IEnumerable<Container> Around(Vector3 origin, float range, long playerId)
    {
        if (range <= 0f)
            return Array.Empty<Container>();
        var found = new List<Container>();
        foreach (var container in NearbyQuery.UniqueInSphere<Container>(origin, range))
        {
            if (!Ready(container))
                continue;
            if (playerId != 0 && !container.CheckAccess(playerId))
                continue;
            if (container.GetInventory() == null)
                container.Load();
            if (container.GetInventory() == null)
                continue;
            found.Add(container);
        }

        return found;
    }

    public static int TryConsumeAround(Vector3 origin, float range, long playerId, string sharedName, int amount,
        bool honorLeaveOne = true, int quality = -1, bool worldLevel = false)
    {
        if (amount <= 0)
            return 0;
        var taken = 0;
        foreach (var container in Around(origin, range, playerId))
        {
            taken += StorageSync.RequestPull(container, playerId, sharedName, amount - taken, honorLeaveOne,
                quality, worldLevel, null);
            if (taken >= amount)
                break;
        }

        return taken;
    }

    // sharedName is ItemData.m_shared.m_name (e.g. $item_wood), not the prefab id.
    // LeaveOne reserves one per chest; Count must match PullOwned or a 1-cost
    // ingredient (tin in bronze) stays "enough" after it can no longer be spent.
    public static int Count(string sharedName, int quality = -1, bool worldLevel = false)
    {
        var total = 0;
        foreach (var container in ForLocalPlayer())
            total += Pullable(container.GetInventory().CountItems(sharedName, quality, worldLevel), true);
        return total;
    }

    internal static int Pullable(int have, bool honorLeaveOne)
    {
        if (have <= 0)
            return 0;
        if (honorLeaveOne && ModConfig.LeaveOne.Value)
            return Mathf.Max(0, have - 1);
        return have;
    }

    public static bool Has(string sharedName, int quality = -1, bool worldLevel = false) =>
        Count(sharedName, quality, worldLevel) > 0;

    public static bool TryCloneIfPresent(ItemDrop drop, out ItemDrop.ItemData item)
    {
        item = null!;
        if (drop?.m_itemData?.m_shared == null)
            return false;
        var name = drop.m_itemData.m_shared.m_name;
        ItemDrop.ItemData? source = null;
        foreach (var container in ForLocalPlayer())
        {
            source = container.GetInventory().GetItem(name, -1, false);
            if (source != null)
                break;
        }

        if (source == null)
            return false;
        item = source.Clone();
        item.m_stack = 1;
        return EnsureDropPrefab(item, drop);
    }

    // CookItem / OnAddOre / Fermenter.AddItem all read m_dropPrefab.name first.
    internal static bool EnsureDropPrefab(ItemDrop.ItemData? item, ItemDrop? from = null)
    {
        if (item == null)
            return false;
        if (item.m_dropPrefab != null)
            return true;
        if (from != null && from.gameObject != null)
        {
            item.m_dropPrefab = from.gameObject;
            return true;
        }

        var db = ObjectDB.instance;
        if (db == null || item.m_shared == null)
            return false;
        var prefab = db.GetItemPrefab(item.m_shared);
        if (prefab == null)
            return false;
        item.m_dropPrefab = prefab;
        return true;
    }

    public static int TryConsume(string sharedName, int amount, bool honorLeaveOne = true, int quality = -1,
        bool worldLevel = false)
    {
        if (amount <= 0)
            return 0;
        var playerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        var taken = 0;
        foreach (var container in ForLocalPlayer())
        {
            taken += StorageSync.RequestPull(container, playerId, sharedName, amount - taken, honorLeaveOne,
                quality, worldLevel, null);
            if (taken >= amount)
                break;
        }

        return taken;
    }

    public static void RequestConsume(string sharedName, int amount, Action<int> done, bool honorLeaveOne = true,
        int quality = -1, bool worldLevel = false)
    {
        if (amount <= 0)
        {
            done(0);
            return;
        }

        var playerId = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerID() : 0L;
        RequestConsumeFrom(new List<Container>(ForLocalPlayer()), playerId, sharedName, amount, honorLeaveOne,
            quality, worldLevel, 0, done);
    }

    public static bool HasRequirements(Player player, Piece.Requirement[] requirements, int qualityLevel, int amount)
    {
        if (player == null || requirements == null)
            return false;
        foreach (var req in requirements)
        {
            if (req.m_resItem == null)
                continue;
            var need = req.GetAmount(qualityLevel) * Mathf.Max(1, amount);
            var name = req.m_resItem.m_itemData.m_shared.m_name;
            var have = CountVanilla(player.GetInventory(), name) + Count(name);
            if (have < need)
                return false;
        }

        return true;
    }

    // Vanilla ConsumeResources always ends in RemoveItem(string). One prefix
    // pulls the bag shortfall from chests; a second prefix (craft + build both
    // loaded) sees the reduced amount and no-ops.
    public static void TakeShortfall(Inventory inventory, string sharedName, ref int amount, int quality = -1,
        bool worldLevel = false)
    {
        if (SkipPatches || !IsLocalPlayerInventory(inventory) || amount <= 0)
            return;
        var inPlayer = CountVanilla(inventory, sharedName, quality, worldLevel);
        if (inPlayer >= amount)
            return;
        amount -= TryConsume(sharedName, amount - inPlayer, true, quality, worldLevel);
    }

    public static int TryDeposit(Player player, ItemDrop.ItemData item)
    {
        if (player == null || item?.m_shared == null)
            return 0;
        var playerInv = player.GetInventory();
        var moved = 0;
        foreach (var container in ForPlayer(player, ModConfig.StorageRange.Value))
        {
            if (!CanWrite(container))
                continue;
            var dest = container.GetInventory();
            if (dest == null || dest == playerInv)
                continue;
            if (!dest.HaveItem(item.m_shared.m_name))
                continue;

            var taken = MergeInto(dest, item, item.m_stack);
            if (item.m_stack - taken > 0)
                taken += PlaceNewStacks(dest, item, item.m_stack - taken);
            if (taken <= 0)
                continue;

            dest.Changed(true, false);
            item.m_stack -= taken;
            moved += taken;
            if (item.m_stack <= 0)
                break;
        }

        return moved;
    }

    // Fill stacks that already exist. Never hands vanilla a clone sitting at grid (-1,-1).
    private static int MergeInto(Inventory dest, ItemDrop.ItemData source, int amount)
    {
        var taken = 0;
        foreach (var slot in dest.GetAllItems())
        {
            if (taken >= amount || slot?.m_shared == null)
                continue;
            if (slot.m_shared.m_name != source.m_shared.m_name)
                continue;
            if (slot.m_quality != source.m_quality || slot.m_worldLevel != source.m_worldLevel)
                continue;
            var room = slot.m_shared.m_maxStackSize - slot.m_stack;
            if (room <= 0)
                continue;
            var n = Mathf.Min(room, amount - taken);
            slot.m_stack += n;
            taken += n;
        }

        return taken;
    }

    private static int PlaceNewStacks(Inventory dest, ItemDrop.ItemData source, int amount)
    {
        var taken = 0;
        while (taken < amount)
        {
            var pos = dest.FindEmptySlot(true);
            if (pos.x < 0)
                break;
            var n = Mathf.Min(source.m_shared.m_maxStackSize, amount - taken);
            var payload = source.Clone();
            payload.m_stack = n;
            payload.m_equipped = false;
            payload.m_gridPos = pos;
            var before = dest.CountItems(source.m_shared.m_name);
            dest.AddItem(payload);
            var got = dest.CountItems(source.m_shared.m_name) - before;
            if (got <= 0)
                break;
            taken += got;
        }

        return taken;
    }

    public static int TryDepositDrop(Player player, ItemDrop drop)
    {
        if (player == null || drop?.m_itemData?.m_shared == null || drop.m_nview == null || !drop.m_nview.IsValid())
            return 0;
        if (!drop.CanPickup(true))
            return 0;
        var id = drop.m_nview.GetZDO().m_uid;
        if (!BeginDeposit(id))
            return 0;
        var item = drop.m_itemData;
        var moved = TryDeposit(player, item);
        if (item.m_stack <= 0)
        {
            FinishDrop(drop, item);
            EndDeposit(id);
            return moved;
        }

        RequestDeposit(player, item, drop, extra =>
        {
            if (item.m_stack <= 0 || extra > 0)
                FinishDrop(drop, item);
            EndDeposit(id);
        });
        return moved;
    }

    public static void RequestDeposit(Player player, ItemDrop.ItemData item, ItemDrop? drop, Action<int> done)
    {
        if (player == null || item?.m_shared == null || item.m_stack <= 0)
        {
            done(0);
            return;
        }

        var playerId = player.GetPlayerID();
        var left = new List<Container>();
        foreach (var container in ForPlayer(player, ModConfig.StorageRange.Value))
        {
            if (CanWrite(container))
                continue;
            var dest = container.GetInventory();
            if (dest == null || !dest.HaveItem(item.m_shared.m_name))
                continue;
            left.Add(container);
        }

        PushWalk(left, 0, playerId, item, drop, 0, done);
    }

    internal static int PullOwned(Container container, string sharedName, int amount, bool honorLeaveOne, int quality,
        bool worldLevel)
    {
        if (!CanWrite(container) || amount <= 0)
            return 0;
        var inventory = container.GetInventory();
        if (inventory == null)
            return 0;
        var have = inventory.CountItems(sharedName, quality, worldLevel);
        var pull = Mathf.Min(Pullable(have, honorLeaveOne), amount);
        if (pull <= 0)
            return 0;
        inventory.RemoveItem(sharedName, pull, quality, worldLevel);
        return pull;
    }

    internal static int PushOwned(Container container, ItemDrop.ItemData item, ItemDrop? drop)
    {
        if (!CanWrite(container) || item?.m_shared == null || item.m_stack <= 0)
            return 0;
        var dest = container.GetInventory();
        if (dest == null || !dest.HaveItem(item.m_shared.m_name))
            return 0;
        var taken = MergeInto(dest, item, item.m_stack);
        if (item.m_stack - taken > 0)
            taken += PlaceNewStacks(dest, item, item.m_stack - taken);
        if (taken <= 0)
            return 0;
        dest.Changed(true, false);
        item.m_stack -= taken;
        if (drop != null)
            FinishDrop(drop, item);
        return taken;
    }

    private static readonly HashSet<ZDOID> Depositing = new();

    private static void RequestConsumeFrom(List<Container> chests, long playerId, string sharedName, int amount,
        bool honorLeaveOne, int quality, bool worldLevel, int moved, Action<int> done)
    {
        if (amount <= 0 || chests.Count == 0)
        {
            done(moved);
            return;
        }

        var chest = chests[0];
        chests.RemoveAt(0);
        StorageSync.RequestPull(chest, playerId, sharedName, amount, honorLeaveOne, quality, worldLevel, taken =>
            RequestConsumeFrom(chests, playerId, sharedName, amount - taken, honorLeaveOne, quality, worldLevel,
                moved + taken, done));
    }

    private static void PushWalk(List<Container> chests, int index, long playerId, ItemDrop.ItemData item,
        ItemDrop? drop, int moved, Action<int> done)
    {
        if (item.m_stack <= 0 || index >= chests.Count)
        {
            done(moved);
            return;
        }

        var chest = chests[index];
        var local = CanWrite(chest);
        StorageSync.RequestPush(chest, playerId, item, drop, taken =>
        {
            if (taken > 0 && !local)
                item.m_stack = Mathf.Max(0, item.m_stack - taken);
            PushWalk(chests, index + 1, playerId, item, drop, moved + taken, done);
        });
    }

    private static void FinishDrop(ItemDrop drop, ItemDrop.ItemData item)
    {
        if (drop?.m_nview == null || !drop.m_nview.IsValid() || !drop.m_nview.IsOwner())
            return;
        if (item.m_stack <= 0)
            drop.m_nview.Destroy();
        else
            drop.SetStack(item.m_stack);
    }

    public static bool BeginDeposit(ZDOID id) => id != ZDOID.None && Depositing.Add(id);

    public static void EndDeposit(ZDOID id) => Depositing.Remove(id);

    private sealed class Pop : IDisposable
    {
        private readonly Action _pop;
        public Pop(Action pop) => _pop = pop;
        public void Dispose() => _pop();
    }
}
