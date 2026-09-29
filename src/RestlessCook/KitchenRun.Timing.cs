using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    // Ingredients are paid once, then persisted with the job for cancellation recovery.
    private sealed class Work
    {
        internal int Order, Made, Level;
        internal string Output = "";
        internal KitchenStationKind Station;
        internal bool Root;
        internal float Elapsed, Duration;
        internal readonly Dictionary<string, int> Inputs = new(StringComparer.Ordinal);
    }
    private static float PreparationDuration(KitchenStep step)
    {
        if (ByOutput.TryGetValue(step.Output, out var row) && row.PreparationSeconds > 0)
            return row.PreparationSeconds;
        // Same duration as native player crafting; rack/oven timers are never replaced.
        var field = AccessTools.Field(typeof(InventoryGui), "m_craftDuration");
        var value = InventoryGui.instance != null ? field?.GetValue(InventoryGui.instance) : null;
        return value is float seconds && seconds > 0 && !float.IsInfinity(seconds) ? seconds : 2f;
    }
    private static bool Busy(Ledger ledger, KitchenStationKind kind)
        => ledger.Work.Exists(w => w.Station == kind);

    private static void StartWork(Ledger ledger, KitchenOrder order, KitchenStep step)
    {
        var work = new Work { Order = order.Id, Output = step.Output, Made = Math.Max(1, OutputAmount(step)),
            Station = step.Station, Level = step.StationLevel, Root = step.Depth == 0, Duration = PreparationDuration(step) };
        foreach (var use in step.Uses)
        {
            var amount = Math.Max(1, use.Amount / Math.Max(1, CraftsFor(step.Need, OutputAmount(step))));
            work.Inputs.TryGetValue(use.Item, out var old); work.Inputs[use.Item] = old + amount;
        }
        ledger.Work.Add(work);
    }
    private static void AdvanceWork(CraftingStation table, List<Hit> hits, Ledger ledger, float elapsed)
    {
        for (var i = ledger.Work.Count - 1; i >= 0; i--)
        {
            var w = ledger.Work[i];
            var order = ledger.Orders.Find(o => o.Id == w.Order);
            if (order == null)
            {
                foreach (var input in w.Inputs) Add(ledger, input.Key, input.Value);
                ledger.Work.RemoveAt(i); continue;
            }
            var requirement = new KitchenStep { Station = w.Station, StationLevel = w.Level };
            if (!StationReady(table, hits, requirement)) continue;
            w.Elapsed = PreparationClock.Advance(w.Elapsed, w.Duration, elapsed, true);
            if (w.Elapsed < w.Duration) continue;
            Add(ledger, w.Output, w.Made);
            if (w.Root) order.Ready += Math.Min(w.Made, Math.Max(0, order.Count - order.Collected - order.Ready));
            ledger.Work.RemoveAt(i);
        }
    }
    private static void IncludeWork(Ledger ledger, Dictionary<string,int> cooking)
    {
        foreach (var w in ledger.Work)
        { cooking.TryGetValue(w.Output, out var count); cooking[w.Output] = count + w.Made; }
    }
    private static void PaintTiming(CraftingStation table, List<KitchenStep> steps, Ledger ledger, int orderId)
    {
        var hits = Scan(table.transform.position);
        foreach (var step in steps)
        {
            // Have/Cooking counts are allocated by Assign; don't attach another branch's timer.
            if (step.Cooking <= 0) continue;
            var w = ledger.Work.Find(job => job.Output == step.Output && (orderId == 0 || job.Order == orderId));
            if (w != null)
            {
                step.ElapsedSeconds = w.Elapsed; step.DurationSeconds = w.Duration;
                var valid = StationReady(table, hits, step);
                step.State = valid ? KitchenStepState.Cooking : KitchenStepState.Blocked;
                step.Note = valid ? "Preparing" : "Paused · required station unavailable";
                continue;
            }
            foreach (var hit in hits)
            {
                if (hit.Rack == null) continue;
                for (var i = 0; i < hit.Rack.m_slots.Length; i++)
                {
                    hit.Rack.GetSlot(i, out var raw, out var time, out var status, out _);
                    if (status != CookingStation.Status.NotDone || ProductOf(hit.Rack, raw) != step.Output) continue;
                    var conversion = hit.Rack.GetItemConversion(raw);
                    if (conversion == null || conversion.m_cookTime <= 0) continue;
                    // Multiple portions can finish at different times: display the next one.
                    if (step.DurationSeconds <= 0 || conversion.m_cookTime - time < step.DurationSeconds - step.ElapsedSeconds)
                    { step.DurationSeconds = conversion.m_cookTime; step.ElapsedSeconds = Mathf.Clamp(time, 0, conversion.m_cookTime); }
                }
            }
        }
    }
    private static string WorkLine(Work w)
    {
        var inputs = new List<string>();
        foreach (var input in w.Inputs) inputs.Add(input.Key + "=" + input.Value);
        return "W\t" + w.Order + "\t" + w.Output + "\t" + (int)w.Station + "\t" + w.Level + "\t" + w.Made + "\t"
            + (w.Root ? 1 : 0) + "\t" + w.Elapsed.ToString("R", CultureInfo.InvariantCulture) + "\t"
            + w.Duration.ToString("R", CultureInfo.InvariantCulture) + "\t" + string.Join(";", inputs) + "\n";
    }
    private static Work? ReadWork(string[] p)
    {
        if (p.Length < 10 || !int.TryParse(p[1], out var order) || !int.TryParse(p[3], out var kind)
            || !int.TryParse(p[4], out var level) || !int.TryParse(p[5], out var made)
            || !float.TryParse(p[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var elapsed)
            || !float.TryParse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var duration)
            || duration <= 0 || float.IsNaN(duration) || float.IsInfinity(duration) || float.IsNaN(elapsed) || float.IsInfinity(elapsed)) return null;
        var w = new Work { Order = order, Output = p[2], Station = (KitchenStationKind)kind, Level = level,
            Made = made, Root = p[6] == "1", Elapsed = Math.Max(0, elapsed), Duration = duration };
        foreach (var item in p[9].Split(';'))
        { var pair = item.Split('='); if (pair.Length == 2 && int.TryParse(pair[1], out var n) && n > 0) w.Inputs[pair[0]] = n; }
        return w;
    }
}
