using System;
using System.Collections.Generic;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static KitchenStationKind KindOf(string station)
    {
        if (string.IsNullOrEmpty(station))
            return KitchenStationKind.None;
        if (station.IndexOf("meadcauldron", StringComparison.OrdinalIgnoreCase) >= 0 || station.IndexOf("meadketill", StringComparison.OrdinalIgnoreCase) >= 0 || station.IndexOf("meadkettle", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.MeadKettle;
        if (station.IndexOf("cauldron", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Cauldron;
        if (station.IndexOf("preptable", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.PrepTable;
        if (station.IndexOf("oven", StringComparison.OrdinalIgnoreCase) >= 0 || station.IndexOf("smelter", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Oven;
        if (station.IndexOf("fermenter", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Fermenter;
        if (station.IndexOf("cooking", StringComparison.OrdinalIgnoreCase) >= 0)
            return KitchenStationKind.Rack;
        return KitchenStationKind.None;
    }

    private static List<KitchenStep> Expand(string output, int need, int depth, string parent)
    {
        var steps = new List<KitchenStep>();
        var guard = new HashSet<string>(StringComparer.Ordinal);
        Walk(output, need, depth, parent, -1, steps, guard);
        return steps;
    }

    private static void Walk(string output, int need, int depth, string parent, int parentIndex, List<KitchenStep> steps, HashSet<string> guard)
    {
        if (need < 1 || depth > 12 || string.IsNullOrEmpty(output) || !guard.Add(output + "#" + parent))
            return;
        EnsureConversions();
        var row = ByOutput.TryGetValue(output, out var found) ? found : null;
        Products.TryGetValue(output, out var conversion);
        var step = new KitchenStep
        {
            Id = row != null ? row.Id : output,
            Name = row != null ? row.Name : Label(output),
            Output = output,
            Parent = parent,
            Depth = depth,
            Need = need,
            Station = conversion != null ? conversion.Kind : row != null ? KindOf(row.Station) : KitchenStationKind.None,
            StationPrefab = row != null ? row.Station : "",
            StationLevel = row != null && row.StationLevel > 0 ? row.StationLevel : 1,
            ParentIndex = parentIndex
        };
        var index = steps.Count;
        steps.Add(step);
        if (conversion == null && row != null)
        {
            var crafts = CraftsFor(need, row.OutputAmount);
            foreach (var use in row.Uses)
            {
                if (use.Amount < 1 || string.IsNullOrEmpty(use.Item))
                    continue;
                var want = crafts * use.Amount;
                step.Uses.Add(new KitchenUse { Item = use.Item, Amount = want, Name = Label(use.Item) });
                Walk(use.Item, want, depth + 1, output, index, steps, guard);
            }

            return;
        }

        if (conversion == null || string.IsNullOrEmpty(conversion.From))
            return;
        var inputs = CraftsFor(need, conversion.Amount);
        step.Uses.Add(new KitchenUse { Item = conversion.From, Amount = inputs, Name = Label(conversion.From) });
        Walk(conversion.From, inputs, depth + 1, output, index, steps, guard);
    }

    private static int OutputAmount(KitchenStep step)
    {
        if (Products.TryGetValue(step.Output, out var conversion)) return Math.Max(1, conversion.Amount);
        if (ByOutput.TryGetValue(step.Output, out var row) && row.OutputAmount > 0)
            return row.OutputAmount;
        return 1;
    }

    private static int CraftsFor(int need, int output)
    {
        if (need < 1)
            return 0;
        var made = Math.Max(1, output);
        return (need + made - 1) / made;
    }

}
