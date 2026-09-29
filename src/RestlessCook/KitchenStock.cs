using System;

namespace RestlessCook;

internal static class KitchenStock
{
    // A requested dish means additional output. Existing finished dishes do not
    // fulfil a new order; only that order's completed, uncollected output does.
    internal static int Allocate(int stock,int need,bool root,int completedForOrder)
        => Math.Min(Math.Max(0,need),Math.Max(0,root ? completedForOrder : stock));
}
