using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessQoL.Storage;

// A loaded nearby store that is not a Container. Chests stay on ForLocalPlayer.
// Piles register here so the browser, the count, and a take share one search.
internal sealed class NearbyLot
{
    internal readonly string Place;
    internal readonly ItemDrop.ItemData Item;
    internal readonly int Count;

    internal NearbyLot(string place, ItemDrop.ItemData item, int count)
    {
        Place = place;
        Item = item;
        Count = count;
    }
}

internal static class NearbyLots
{
    private static readonly List<Action<Vector3, float, long, List<NearbyLot>>> Collectors = new();
    private static readonly List<Action<Player, string, int, Action<int>>> Takers = new();
    private static readonly List<Func<Vector3, float, long, string, int, int>> Drains = new();

    internal static void Listen(
        Action<Vector3, float, long, List<NearbyLot>> collect,
        Action<Player, string, int, Action<int>> take,
        Func<Vector3, float, long, string, int, int>? drain = null)
    {
        Collectors.Add(collect);
        Takers.Add(take);
        if (drain != null)
            Drains.Add(drain);
    }

    // Keep the previous internal ABI for already released addons.
    internal static void Listen(Action<Vector3, float, List<NearbyLot>> collect,
        Action<Player, string, int, Action<int>> take,
        Func<Vector3, float, string, int, int>? drain = null) =>
        Listen((origin, range, actor, into) => collect(origin, range, into), take,
            drain == null ? null : (origin, range, actor, name, amount) => drain(origin, range, name, amount));

    internal static void CollectAround(Vector3 origin, float range, List<NearbyLot> into) =>
        CollectAround(origin, range, Player.m_localPlayer?.GetPlayerID() ?? 0L, into);

    internal static int Drain(Vector3 origin, float range, string sharedName, int amount) =>
        Drain(origin, range, Player.m_localPlayer?.GetPlayerID() ?? 0L, sharedName, amount);

    internal static void Collect(Player player, float range, List<NearbyLot> into)
    {
        if (player == null)
            return;
        CollectAround(player.transform.position, range, player.GetPlayerID(), into);
    }

    internal static void CollectAround(Vector3 origin, float range, long actor, List<NearbyLot> into)
    {
        if (range <= 0f)
            return;
        foreach (var collect in Collectors)
            collect(origin, range, actor, into);
    }

    // Reduce a nearby lot without putting the items in a bag. Callers account for them.
    internal static int Drain(Vector3 origin, float range, long actor, string sharedName, int amount)
    {
        if (amount <= 0 || range <= 0f || string.IsNullOrEmpty(sharedName))
            return 0;
        var taken = 0;
        foreach (var drain in Drains)
        {
            taken += Math.Max(0, drain(origin, range, actor, sharedName, amount - taken));
            if (taken >= amount)
                break;
        }

        return taken;
    }

    internal static void Take(Player player, string key, int amount, Action<int> done)
    {
        if (done == null)
            return;
        if (player == null || amount <= 0 || string.IsNullOrEmpty(key) || Takers.Count == 0)
        {
            done(0);
            return;
        }

        void Next(int index, int got)
        {
            if (index >= Takers.Count || got >= amount)
            {
                done(got);
                return;
            }

            Takers[index](player, key, amount - got, n => Next(index + 1, got + Math.Max(0, n)));
        }

        Next(0, 0);
    }
}
