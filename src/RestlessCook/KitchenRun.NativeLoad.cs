using System;

namespace RestlessCook;

internal static partial class KitchenRun
{
    // Verify native acceptance before committing the paid ingredient. Capacity
    // is read again for every load; a scan snapshot is never a reservation.
    private static bool TryLoadRaw(Hit hit, string raw, Ledger ledger)
    {
        if (hit.View == null || !hit.View.IsValid() || !hit.View.IsOwner()
            || !ledger.Pantry.TryGetValue(raw, out var held) || held < 1) return false;
        var accepted = false;
        if (hit.Rack != null && hit.Rack.GetFreeSlot() >= 0
            && (!hit.Rack.m_requireFire || hit.Rack.IsFireLit())
            && (!hit.Rack.m_useFuel || hit.Rack.GetFuel() > 0))
        {
            var slot = hit.Rack.GetFreeSlot();
            hit.Rack.RPC_AddItem(0L, raw, false);
            hit.Rack.GetSlot(slot, out var actual, out _, out _, out _);
            accepted = actual == raw;
            hit.Info.Free = Math.Max(0, hit.Info.Free - (accepted ? 1 : 0));
        }
        if (hit.Oven != null && hit.Oven.GetQueueSize() < hit.Oven.m_maxOre
            && (hit.Oven.m_maxFuel <= 0 || hit.Oven.GetFuel() > 0))
        {
            var before = hit.Oven.GetQueueSize();
            hit.Oven.QueueOre(raw, false);
            accepted = hit.Oven.GetQueueSize() == before + 1;
            hit.Info.Free = Math.Max(0, hit.Oven.m_maxOre - hit.Oven.GetQueueSize());
        }
        if (hit.Fermenter != null && !ledger.Taps.Exists(t => t.StationId == StationId(hit))
            && hit.Fermenter.GetStatus() == Fermenter.Status.Empty && !hit.Fermenter.m_exposed && hit.Fermenter.m_hasRoof)
        {
            var hash = raw.GetStableHashCode();
            hit.Fermenter.RPC_AddItem(0L, hash, false);
            accepted = hit.Fermenter.GetContent() == hash;
            hit.Info.Free = accepted ? 0 : 1;
        }
        if (!accepted) return false;
        if (held == 1) ledger.Pantry.Remove(raw); else ledger.Pantry[raw] = held - 1;
        return true;
    }
}
