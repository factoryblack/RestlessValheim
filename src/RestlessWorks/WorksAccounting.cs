using System;
using System.Collections.Generic;

namespace RestlessWorks;

// Shared finite-order reservations take precedence over standing stock targets.
// Pure accounting: no scene scans, network writes or local-player inventory.
internal static class WorksAccounting
{
    internal static void Describe(IReadOnlyList<WorksOrder> orders, IReadOnlyDictionary<string,int> stock,
        IReadOnlyDictionary<string,int> machines)
    {
        var pool = new Dictionary<string,int>(StringComparer.Ordinal);
        foreach (var pair in machines) pool[pair.Key] = Math.Max(0,pair.Value);
        foreach (var order in orders)
        {
            if (order.Mode != WorksOrderMode.Make) continue;
            pool.TryGetValue(order.Output,out var running);
            var still = Math.Max(0,order.Count-order.Ready-order.Collected);
            var cover = Math.Min(still,running);
            pool[order.Output] = running-cover;
            order.InProduction = cover;
            order.Available = order.Ready+order.Collected;
            order.Remaining = still-cover;
        }
        foreach (var order in orders)
        {
            if (order.Mode != WorksOrderMode.Keep) continue;
            pool.TryGetValue(order.Output,out var running);
            stock.TryGetValue(order.Output,out var held);
            held = Math.Max(0,held-Reserved(orders,order.Output));
            var gap = Math.Max(0,order.Count-held);
            var filling = Math.Min(gap,running);
            pool[order.Output] = running-filling;
            order.InProduction = filling;
            order.Available = held;
            order.Remaining = gap-filling;
        }
    }
    internal static int Reserved(IReadOnlyList<WorksOrder> orders,string output)
    {
        var reserved = 0;
        foreach (var order in orders)
            if (order.Mode == WorksOrderMode.Make && order.Output == output) reserved += Math.Max(0,order.Ready);
        return reserved;
    }
    internal static void Credit(IReadOnlyList<WorksOrder> orders,string output,int amount)
    {
        var left = Math.Max(0,amount);
        foreach (var order in orders)
        {
            if (left == 0) break;
            if (order.Mode != WorksOrderMode.Make || order.Output != output) continue;
            var give = Math.Min(left,Math.Max(0,order.Count-order.Ready-order.Collected));
            order.Ready += give; left -= give;
        }
    }
}
