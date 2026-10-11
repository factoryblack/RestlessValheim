using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static void Assign(CraftingStation table, List<KitchenStep> steps, Dictionary<string, int> free, Dictionary<string, int> cooking, long? playerId, int completedForOrder = 0, List<Hit>? hits = null)
    {
        hits ??= Scan(table.transform.position, false, View(table));
        var covered = new bool[steps.Count];
        // Preserve native per-craft costs before reducing downstream demand.
        foreach (var step in steps)
            foreach (var use in step.Uses)
                if (use.UnitAmount <= 0)
                    use.UnitAmount = Math.Max(1, use.Amount / Math.Max(1, CraftsFor(step.Need, OutputAmount(step))));
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            if (step.ParentIndex >= 0 && step.ParentIndex < covered.Length && covered[step.ParentIndex])
            {
                covered[i] = true;
                step.CoveredByParent = true;
                free.TryGetValue(step.Output, out var already);
                step.Available = Math.Max(0, already);
                step.Have = step.Need;
                step.Cooking = 0;
                step.State = KitchenStepState.Prepared;
                step.Note = "Prepared";
                continue;
            }

            if (step.ParentIndex >= 0 && step.ParentIndex < i)
            {
                var parent = steps[step.ParentIndex];
                var required = 0;
                foreach (var use in parent.Uses)
                    if (use.Item == step.Output) required += use.Amount;
                step.Need = required;
            }

            free.TryGetValue(step.Output, out var stock);
            var portion = KitchenStock.Split(stock, step.Need, i == 0, completedForOrder);
            var have = portion.Allocated;
            step.Have = have;
            step.Available = portion.OnHand;
            free[step.Output] = Math.Max(0, stock - have);
            cooking.TryGetValue(step.Output, out var onFire);
            var still = step.Need - have;
            step.Cooking = Math.Min(onFire, Math.Max(0, still));
            cooking[step.Output] = onFire - step.Cooking;
            var remainingCrafts = CraftsFor(step.Need - step.Have - step.Cooking, OutputAmount(step));
            foreach (var use in step.Uses)
                use.Amount = InputCost(step, use) * remainingCrafts;
            if (step.Have >= step.Need)
            {
                covered[i] = true;
                step.State = KitchenStepState.Prepared;
                step.Note = "Prepared";
                continue;
            }

            if (step.Have + step.Cooking >= step.Need)
            {
                covered[i] = true;
                step.State = KitchenStepState.Cooking;
                step.Note = step.Cooking + " cooking";
            }
        }

        for (var i = steps.Count - 1; i >= 0; i--)
        {
            if (covered[i])
                continue;
            var step = steps[i];
            if (step.Uses.Count == 0)
            {
                step.State = KitchenStepState.Missing;
                step.Note = "Missing " + step.Name;
                continue;
            }

            var inputs = true;
            string? missing = null;
            foreach (var use in step.Uses)
            {
                // Eligibility is one real craft, even when the rest of the
                // order is waiting. Payment still checks actual stock.
                var want = InputCost(step, use);
                free.TryGetValue(use.Item, out var got);
                var incoming = Incoming(steps, use.Item);
                use.Have = got + incoming;
                if (got + incoming < want)
                {
                    inputs = false;
                    missing = use.Name;
                }
                else
                    free[use.Item] = Math.Max(0, got - Math.Max(0, want - incoming));
            }

            if (!inputs)
            {
                step.State = KitchenStepState.Missing;
                step.Note = string.IsNullOrEmpty(missing) ? "Missing ingredients" : "Waiting for " + missing;
                continue;
            }

            var block = BlockReason(table, hits, step);
            if (!string.IsNullOrEmpty(block))
            {
                step.State = KitchenStepState.Blocked;
                step.Note = block;
                continue;
            }

            step.State = playerId.HasValue ? KitchenStepState.Queued : KitchenStepState.Ready;
            step.Note = step.State == KitchenStepState.Ready ? "Ingredients available" : "Queued";
        }
    }

    private static int Incoming(List<KitchenStep> steps, string item)
    {
        var n = 0;
        foreach (var step in steps)
        {
            if (step.Output != item || step.CoveredByParent)
                continue;
            // These quantities were removed from free during allocation. Include
            // partial stock regardless of whether further production is possible.
            n += step.Have + step.Cooking;
            if (step.State == KitchenStepState.Queued || step.State == KitchenStepState.Ready)
                n += Math.Max(0, step.Need - step.Have - step.Cooking);
        }

        return n;
    }

}
