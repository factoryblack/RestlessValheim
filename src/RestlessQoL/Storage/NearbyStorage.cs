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

    public static bool IsLocalPlayerInventory(Inventory inventory)
    {
        var player = Player.m_localPlayer;
        return player != null && inventory != null && player.GetInventory() == inventory;
    }

    public static int CountVanilla(Inventory inventory, string sharedName)
    {
        using (SuppressPatches())
            return inventory.CountItems(sharedName);
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

    public static int TryConsumeAround(Vector3 origin, float range, long playerId, string sharedName, int amount, bool honorLeaveOne = true)
    {
        if (amount <= 0)
            return 0;
        var taken = 0;
        foreach (var container in Around(origin, range, playerId))
        {
            var inventory = container.GetInventory();
            var have = inventory.CountItems(sharedName);
            if (have <= 0)
                continue;
            var leave = honorLeaveOne && ModConfig.LeaveOne.Value ? 1 : 0;
            var available = Mathf.Max(0, have - leave);
            if (available <= 0)
                continue;
            var pull = Mathf.Min(available, amount - taken);
            inventory.RemoveItem(sharedName, pull);
            taken += pull;
            if (taken >= amount)
                break;
        }

        return taken;
    }

    // sharedName is ItemData.m_shared.m_name (e.g. $item_wood), not the prefab id.
    public static int Count(string sharedName)
    {
        var total = 0;
        foreach (var container in ForLocalPlayer())
            total += container.GetInventory().CountItems(sharedName);
        return total;
    }

    public static bool Has(string sharedName) => Count(sharedName) > 0;

    public static bool TryCloneIfPresent(ItemDrop drop, out ItemDrop.ItemData item)
    {
        item = null!;
        if (drop?.m_itemData?.m_shared == null)
            return false;
        if (!Has(drop.m_itemData.m_shared.m_name))
            return false;
        item = drop.m_itemData.Clone();
        item.m_stack = 1;
        return true;
    }

    public static int TryConsume(string sharedName, int amount, bool honorLeaveOne = true)
    {
        if (amount <= 0)
            return 0;
        var taken = 0;
        foreach (var container in ForLocalPlayer())
        {
            var inventory = container.GetInventory();
            var have = inventory.CountItems(sharedName);
            if (have <= 0)
                continue;
            var leave = honorLeaveOne && ModConfig.LeaveOne.Value ? 1 : 0;
            var available = Mathf.Max(0, have - leave);
            if (available <= 0)
                continue;
            var pull = Mathf.Min(available, amount - taken);
            inventory.RemoveItem(sharedName, pull);
            taken += pull;
            if (taken >= amount)
                break;
        }

        return taken;
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
    public static void TakeShortfall(Inventory inventory, string sharedName, ref int amount)
    {
        if (SkipPatches || !IsLocalPlayerInventory(inventory) || amount <= 0)
            return;
        var inPlayer = CountVanilla(inventory, sharedName);
        if (inPlayer >= amount)
            return;
        amount -= TryConsume(sharedName, amount - inPlayer);
    }

    public static int TryDeposit(Player player, ItemDrop.ItemData item)
    {
        if (player == null || item?.m_shared == null)
            return 0;
        var playerInv = player.GetInventory();
        var moved = 0;
        foreach (var container in ForPlayer(player, ModConfig.StorageRange.Value))
        {
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
        if (player == null || drop?.m_itemData?.m_shared == null)
            return 0;
        if (!drop.CanPickup(true))
            return 0;
        var item = drop.m_itemData;
        var moved = TryDeposit(player, item);
        if (moved <= 0)
            return 0;
        if (item.m_stack <= 0)
            drop.m_nview.Destroy();
        else
            drop.SetStack(item.m_stack);
        return moved;
    }

    private sealed class Pop : IDisposable
    {
        private readonly Action _pop;
        public Pop(Action pop) => _pop = pop;
        public void Dispose() => _pop();
    }
}
