using System;

namespace RestlessCook;

internal static partial class KitchenRun
{
    // Planning reduces order totals, never the cost paid for a native craft.
    private static int InputCost(KitchenStep step, KitchenUse use)
        => use.UnitAmount > 0 ? use.UnitAmount
            : Math.Max(1, use.Amount / Math.Max(1, CraftsFor(step.Need, OutputAmount(step))));
}
