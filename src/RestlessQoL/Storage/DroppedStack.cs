using System.Collections.Generic;
using UnityEngine;

namespace RestlessQoL.Storage;

// A split drop lowers the bag stack before the world item is cloned. A later
// load can put the prefab stack (often 1, sometimes 0) back on that drop.
// Remember the amount that was actually thrown so vacuum can credit it.
public static class DroppedStack
{
    private static readonly Dictionary<int, int> Amounts = new();

    public static void Remember(ItemDrop? drop, int amount)
    {
        if (drop == null || amount <= 0)
            return;
        Amounts[drop.GetInstanceID()] = amount;
    }

    public static int Count(ItemDrop? drop)
    {
        if (drop?.m_itemData == null)
            return 0;
        var live = drop.m_itemData.m_stack;
        if (!Amounts.TryGetValue(drop.GetInstanceID(), out var thrown))
            return live;
        // The bag still holds this object when a partial drop did not clone.
        // The thrown amount is the one that left the bag, not the remainder.
        if (InBag(drop))
            return thrown;
        return Mathf.Max(live, thrown);
    }

    public static void Apply(ItemDrop? drop)
    {
        if (drop?.m_itemData == null || InBag(drop))
            return;
        var n = Count(drop);
        if (n <= 0 || drop.m_itemData.m_stack == n)
            return;
        drop.m_itemData.m_stack = n;
        if (drop.m_nview != null && drop.m_nview.IsValid() && drop.m_nview.IsOwner())
            drop.Save();
    }

    public static void Forget(ItemDrop? drop)
    {
        if (drop != null)
            Amounts.Remove(drop.GetInstanceID());
    }

    private static bool InBag(ItemDrop drop)
    {
        var inv = Player.m_localPlayer?.GetInventory();
        return inv != null && inv.ContainsItem(drop.m_itemData);
    }
}
