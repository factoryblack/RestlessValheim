namespace RestlessCook;

internal static partial class KitchenRun
{
    private static void CollectRack(CookingStation rack, Ledger ledger)
    {
        if (rack.m_nview == null || !rack.m_nview.IsValid() || !rack.m_nview.IsOwner())
            return;
        for (var i = 0; i < rack.m_slots.Length; i++)
        {
            rack.GetSlot(i, out var name, out _, out var status, out _);
            if (status != CookingStation.Status.Done || string.IsNullOrEmpty(name))
                continue;
            var product = Clean(name);
            if (!Wanted(ledger, product))
                continue;

            // Valheim 1.0.21 added sender. Clearing uses zero, as in the native
            // RPC_RemoveDoneItem path; this is not a new player insertion.
            rack.SetSlot(i, "", 0f, CookingStation.Status.NotDone, false, 0L);
            CreditFinished(ledger, product, 1);
            rack.SetSlotVisual(i, "", false, CookingStation.Status.NotDone);
        }
    }
}
