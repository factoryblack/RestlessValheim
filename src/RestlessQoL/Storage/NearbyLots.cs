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
    private static readonly List<Action<Player, float, List<NearbyLot>>> Collectors = new();
    private static readonly List<Action<Player, string, int, Action<int>>> Takers = new();

    internal static void Listen(
        Action<Player, float, List<NearbyLot>> collect,
        Action<Player, string, int, Action<int>> take)
    {
        Collectors.Add(collect);
        Takers.Add(take);
    }

    internal static void Collect(Player player, float range, List<NearbyLot> into)
    {
        if (player == null || range <= 0f)
            return;
        foreach (var collect in Collectors)
            collect(player, range, into);
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
