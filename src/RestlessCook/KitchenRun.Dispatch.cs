using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static bool Load(List<Hit> hits, KitchenStep step, Ledger ledger, Vector3 origin, long playerId)
    {
        Products.TryGetValue(step.Output, out var conversion);
        var raw = conversion != null ? conversion.From : step.Uses.Count > 0 ? step.Uses[0].Item : "";
        if (string.IsNullOrEmpty(raw))
            return false;
        if (!ledger.Pantry.TryGetValue(raw, out var held) || held < 1)
        {
            PullInto(ledger, raw, 1, origin, playerId);
            if (!ledger.Pantry.TryGetValue(raw, out held) || held < 1)
                return false;
        }

        // Least active compatible machine first. Free capacity breaks ties;
        // stable scan order is only a final tie-breaker. Rejected native loads
        // try the next machine without debiting the ingredient.
        var candidates = new List<Hit>();
        foreach (var hit in hits)
        {
            if (hit.Info.Kind != step.Station || !Accepts(hit, step.Output)
                || hit.Info.Free <= 0 || !string.IsNullOrEmpty(hit.Info.Block)
                || hit.View == null || !hit.View.IsValid() || !hit.View.IsOwner()) continue;
            candidates.Add(hit);
        }
        while (candidates.Count > 0)
        {
            var best = 0;
            for (var i = 1; i < candidates.Count; i++)
            {
                var a = candidates[i].Info; var b = candidates[best].Info;
                if (a.Cooking < b.Cooking || (a.Cooking == b.Cooking && a.Free > b.Free)) best = i;
            }
            var hit = candidates[best]; candidates.RemoveAt(best);
            if (!TryLoadRaw(hit, raw, ledger)) continue;
            hit.Info.Cooking++;
            return true;
        }

        return false;
    }

    private static bool PullOne(KitchenStep step, Ledger ledger, Vector3 origin, long playerId)
    {
        var crafts = 1;
        foreach (var use in step.Uses)
        {
            var one = InputCost(step, use);
            var want = one * crafts;
            ledger.Pantry.TryGetValue(use.Item, out var have);
            if (have >= want)
                continue;
            PullInto(ledger, use.Item, want - have, origin, playerId);
            ledger.Pantry.TryGetValue(use.Item, out have);
            if (have < want)
                return false;
        }

        return true;
    }

    private static bool Pay(KitchenStep step, Ledger ledger)
    {
        var crafts = 1;
        foreach (var use in step.Uses)
        {
            var one = InputCost(step, use);
            ledger.Pantry.TryGetValue(use.Item, out var have);
            if (have < one * crafts)
                return false;
        }

        foreach (var use in step.Uses)
        {
            var one = InputCost(step, use);
            ledger.Pantry[use.Item] = ledger.Pantry[use.Item] - one;
            if (ledger.Pantry[use.Item] <= 0)
                ledger.Pantry.Remove(use.Item);
        }

        return true;
    }

}
