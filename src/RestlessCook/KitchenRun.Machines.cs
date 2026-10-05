using System;
using System.Collections.Generic;
using HarmonyLib;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessCook;

internal static partial class KitchenRun
{
    private static bool WorkReady(CraftingStation table, List<Hit> hits, Work work, bool requireOwner = true)
    {
        foreach (var hit in hits)
            if (StationId(hit) == work.StationId && hit.Craft != null && hit.View != null
                && (!requireOwner || hit.View.IsOwner()) && ProductionLease.Controls(hit.View, table.m_nview)
                && Usable(hit.Craft, work.Level))
            { if (requireOwner) hit.Craft.PokeInUse(); return true; }
        return false;
    }

    private static bool Accepts(Hit hit, string output)
    {
        if (!Products.TryGetValue(output, out var conversion)) return false;
        if (hit.Rack != null) return ProductOf(hit.Rack, conversion.From) == output;
        if (hit.Oven != null) return Clean(hit.Oven.GetItemConversion(conversion.From)?.m_to?.gameObject.name ?? "") == output;
        if (hit.Fermenter != null) return Clean(hit.Fermenter.GetItemConversion(conversion.From.GetStableHashCode())?.m_to?.gameObject.name ?? "") == output;
        return false;
    }

    private static Hit DescribeFermenter(Fermenter station)
    {
        station.UpdateCover(0f, true);
        var status = station.GetStatus();
        var info = new KitchenStationInfo { Name = station.m_name, Kind = KitchenStationKind.Fermenter,
            Free = status == Fermenter.Status.Empty ? 1 : 0,
            Cooking = status == Fermenter.Status.Fermenting ? 1 : 0,
            Ready = status == Fermenter.Status.Ready ? 1 : 0 };
        if (!station.m_hasRoof) info.Block = "Needs a roof";
        else if (station.m_exposed) info.Block = "Too exposed";
        return new Hit { Fermenter = station, Info = info };
    }

    private static void PaintNativeTiming(List<Hit> hits, KitchenStep step)
    {
        foreach (var hit in hits)
        {
            if (hit.Rack != null)
                for (var i = 0; i < hit.Rack.m_slots.Length; i++)
                {
                    hit.Rack.GetSlot(i, out var raw, out var time, out var status, out _);
                    if (status != CookingStation.Status.NotDone || ProductOf(hit.Rack, raw) != step.Output) continue;
                    var conversion = hit.Rack.GetItemConversion(raw);
                    if (conversion == null || conversion.m_cookTime <= 0) continue;
                    SetNativeTiming(step, hit.Info.Name, time, conversion.m_cookTime,
                        (hit.Rack.m_requireFire && !hit.Rack.IsFireLit()) || (hit.Rack.m_useFuel && hit.Rack.GetFuel() <= 0));
                }
            if (hit.Oven != null && hit.Oven.GetQueueSize() > 0)
            {
                var oven = hit.Oven;
                var raw = ProductionQueue.Input(oven, 0);
                var conversion = oven.GetItemConversion(raw);
                if (conversion?.m_to != null && Clean(conversion.m_to.gameObject.name) == step.Output)
                    SetNativeTiming(step, hit.Info.Name, oven.GetBakeTimer(), oven.m_secPerProduct,
                        (oven.m_maxFuel > 0 && oven.GetFuel() <= 0) || (oven.m_requiresRoof && !oven.m_haveRoof) || oven.m_blockedSmoke);
            }
            if (hit.Fermenter != null && hit.Fermenter.GetContent() != 0)
            {
                var fermenter = hit.Fermenter;
                var conversion = fermenter.GetItemConversion(fermenter.GetContent());
                if (conversion?.m_to != null && Clean(conversion.m_to.gameObject.name) == step.Output)
                    SetNativeTiming(step, hit.Info.Name, (float)fermenter.GetFermentationTime(), fermenter.m_fermentationDuration,
                        fermenter.m_exposed || !fermenter.m_hasRoof);
            }
        }
    }

    private static void SetNativeTiming(KitchenStep step, string station, float elapsed, float duration, bool paused)
    {
        if (duration <= 0 || (step.DurationSeconds > 0 && duration - elapsed >= step.DurationSeconds - step.ElapsedSeconds)) return;
        step.ActiveStation = station;
        step.DurationSeconds = duration; step.ElapsedSeconds = Mathf.Clamp(elapsed, 0, duration);
        step.State = paused ? KitchenStepState.Blocked : KitchenStepState.Cooking;
        step.Note = (paused ? "Paused · " : "Cooking · ") + station;
    }
}
