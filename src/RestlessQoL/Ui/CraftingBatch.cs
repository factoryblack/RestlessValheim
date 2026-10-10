using System;
namespace RestlessQoL.Ui;

// Queue accounting only. Native InventoryGui performs each craft and owns all item mutations.
internal sealed class CraftingBatch
{
    internal const int Limit=99;
    internal int Total { get; private set; }
    internal int Completed { get; private set; }
    internal bool Active { get; private set; }
    internal int Remaining=>Active?Total-Completed:0;
    internal void Start(int count){Total=Math.Max(1,Math.Min(Limit,count));Completed=0;Active=true;}
    internal bool Complete(bool success)
    {
        if(!Active)return false;
        if(!success){Active=false;return false;}
        Completed++;
        Active=Completed<Total;return Active;
    }
    internal void Cancel()=>Active=false;
    internal static int Maximum(Func<int,bool> allows)
    {
        var low=0;var high=Limit;
        while(low<high){var mid=low+(high-low+1)/2;if(allows(mid))low=mid;else high=mid-1;}
        return low;
    }
}
