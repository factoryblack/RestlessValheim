using UnityEngine;
using System.Collections.Generic;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static void Schedule(CraftingStation table, List<Hit> hits, Ledger ledger, Vector3 origin)
    {
        var crafts = 0;
        var loads = 0;
        if (ledger.Orders.Count == 0) return;
        var start = ledger.NextOrder % ledger.Orders.Count;
        // One accepted action per order per round. Replan after every round:
        // newly loaded food and paid preparation jobs now cover real demand.
        // Four native loads and three preparation jobs remain the tick limits.
        for (var round = 0; round < LoadsPerTick + CraftsPerTick; round++)
        {
            var progressed = false;
            var cooking = OnStations(origin, hits);
            IncludeWork(ledger, cooking);
            for (var offset = 0; offset < ledger.Orders.Count; offset++)
            {
                var index = (start + offset) % ledger.Orders.Count;
                var order = ledger.Orders[index];
                var outstanding = Outstanding(order);
                if (outstanding < 1) continue;
                var stock = Stock(origin, order.PlayerId, ledger);
                var steps = Expand(order.Feast, outstanding, 0, "", stock);
                Assign(table, steps, stock, cooking, order.PlayerId, order.Ready, hits);
                // Use prepared inputs closest to the finished dish first;
                // otherwise descend until a real ingredient can be produced.
                for (var i = 0; i < steps.Count; i++)
                {
                    var step = steps[i];
                    if (step.State != KitchenStepState.Ready && step.State != KitchenStepState.Queued) continue;
                    if (step.Station == KitchenStationKind.Rack || step.Station == KitchenStationKind.Oven || step.Station == KitchenStationKind.Fermenter)
                    {
                        if (loads >= LoadsPerTick || !Load(hits, step, ledger, origin, order.PlayerId)) continue;
                        loads++;
                    }
                    else
                    {
                        if (step.Station != KitchenStationKind.Cauldron && step.Station != KitchenStationKind.PrepTable && step.Station != KitchenStationKind.MeadKettle) continue;
                        if (crafts >= CraftsPerTick) continue;
                        var station = PreparationStation(hits, step, ledger);
                        if (station == null || !PullOne(step, ledger, origin, order.PlayerId) || !Pay(step, ledger)) continue;
                        StartWork(ledger, order, step, station);
                        crafts++;
                    }
                    progressed = true;
                    ledger.NextOrder = (index + 1) % ledger.Orders.Count;
                    break;
                }
            }
            if (!progressed || (loads >= LoadsPerTick && crafts >= CraftsPerTick)) break;
        }
        foreach (var order in ledger.Orders) Deliver(order, ledger);
    }
}
