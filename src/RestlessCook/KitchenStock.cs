using System;

namespace RestlessCook;

internal static class KitchenStock
{
    internal readonly struct Portion
    {
        internal readonly int Allocated;
        internal readonly int OnHand;
        internal Portion(int allocated, int onHand) { Allocated = allocated; OnHand = onHand; }
    }

    // A requested dish means additional output. Existing finished dishes do not
    // fulfil a new order; only that order's completed, uncollected output does.
    // OnHand stays uncapped so the cookbook can show 8/6 when more than enough is nearby.
    internal static Portion Split(int stock, int need, bool root, int completedForOrder)
    {
        var onHand = Math.Max(0, root ? completedForOrder : stock);
        var allocated = Math.Min(Math.Max(0, need), onHand);
        return new Portion(allocated, onHand);
    }

    internal static int Allocate(int stock, int need, bool root, int completedForOrder)
        => Split(stock, need, root, completedForOrder).Allocated;

    internal static string Count(int onHand, int need) => Math.Max(0, onHand) + " / " + Math.Max(0, need);
}
