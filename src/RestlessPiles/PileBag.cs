using System.Collections;
using System.Linq;
using RestlessQoL.Core;
using RestlessQoL.HudTweaks;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessPiles;

internal static class PileBag
{
    private static float _nextVacuum;

    public static bool Matches(ItemDrop.ItemData? a, ItemDrop.ItemData? b)
    {
        if (a?.m_shared == null || b?.m_shared == null)
            return false;
        return a.m_shared.m_name == b.m_shared.m_name;
    }

    public static int TakeStack(Player player, PileBox box)
    {
        var taken = 0;
        Pile.OwnThen(box.View, _ => taken = TakeOwned(player, box, 0));
        return taken;
    }

    public static int CountNearby(string sharedName)
    {
        var player = Player.m_localPlayer;
        if (player == null || string.IsNullOrEmpty(sharedName) || !PileConfig.On)
            return 0;
        var range = Mathf.Max(PileConfig.Range.Value, ModConfig.StorageRange.Value);
        var total = 0;
        foreach (var box in PileBox.All)
        {
            if (box == null || !box.Live || box.Item?.m_shared == null)
                continue;
            if (box.Item.m_shared.m_name != sharedName)
                continue;
            if ((box.transform.position - player.transform.position).sqrMagnitude > range * range)
                continue;
            total += box.Stored;
        }

        return total;
    }

    public static int ConsumeNearby(string sharedName, int amount)
    {
        var player = Player.m_localPlayer;
        if (player == null || amount <= 0 || !PileConfig.On)
            return 0;
        var range = Mathf.Max(PileConfig.Range.Value, ModConfig.StorageRange.Value);
        var taken = 0;
        foreach (var box in PileBox.All)
        {
            if (taken >= amount)
                break;
            if (box == null || !box.Live || box.Item?.m_shared == null)
                continue;
            if (box.Item.m_shared.m_name != sharedName)
                continue;
            if ((box.transform.position - player.transform.position).sqrMagnitude > range * range)
                continue;
            if (!box.View.IsOwner())
                box.View.ClaimOwnership();
            if (!box.View.IsOwner())
                continue;
            taken += PullFromPile(box, amount - taken);
        }

        return taken;
    }

    // Craft/build consume — drop pile count only, never route through the bag.
    private static int PullFromPile(PileBox box, int amount)
    {
        if (box == null || amount <= 0 || box.Stored <= 0)
            return 0;
        var take = Mathf.Min(amount, box.Stored);
        Pile.Write(box.View, box.Stored - take);
        return take;
    }

    private static int TakeOwned(Player player, PileBox box, int limit)
    {
        if (player == null || box == null || box.Stored <= 0)
            return 0;
        var inv = player.GetInventory();
        var item = box.Item;
        if (inv == null || item?.m_shared == null)
            return 0;

        var max = item.m_shared.m_maxStackSize;
        var want = limit > 0 ? Mathf.Min(limit, box.Stored) : box.Stored;
        var partial = RoomInBag(inv, item);
        int take;
        if (partial != null)
            take = Mathf.Min(max - partial.m_stack, want);
        else if (inv.HaveEmptySlot())
            take = Mathf.Min(max, want);
        else
        {
            if (limit <= 0)
                player.Message(MessageHud.MessageType.Center, "Bag is full");
            return 0;
        }

        if (take <= 0)
            return 0;

        if (partial != null)
            partial.m_stack += take;
        else
        {
            var clone = item.Clone();
            clone.m_stack = take;
            clone.m_cheated = false;
            if (!inv.AddItem(clone))
            {
                if (limit <= 0)
                    player.Message(MessageHud.MessageType.Center, "Bag is full");
                return 0;
            }
        }

        inv.Changed(true, false);
        Pile.Write(box.View, box.Stored - take);
        if (limit <= 0)
            Tell(player, "Took " + take + " " + Pile.Title(item), take, item);
        return take;
    }

    private static ItemDrop.ItemData? RoomInBag(Inventory inv, ItemDrop.ItemData item)
    {
        foreach (var slot in inv.GetAllItems())
        {
            if (slot?.m_shared == null || slot.m_shared.m_name != item.m_shared.m_name)
                continue;
            if (slot.m_stack < slot.m_shared.m_maxStackSize)
                return slot;
        }

        return null;
    }

    public static int DumpInto(Player player, PileBox box)
    {
        var moved = 0;
        Pile.OwnThen(box.View, _ => moved = DumpOwned(player, box));
        return moved;
    }

    private static int DumpOwned(Player player, PileBox box)
    {
        if (player == null || box == null)
            return 0;
        var inv = player.GetInventory();
        if (inv == null)
            return 0;

        var moved = 0;
        foreach (var item in inv.GetAllItems().ToArray())
        {
            if (!CanDump(item) || !Matches(item, box.Item))
                continue;
            moved += item.m_stack;
            inv.RemoveItem(item);
        }

        if (moved <= 0)
            return 0;

        inv.Changed(true, false);
        Pile.Write(box.View, box.Stored + moved);
        Tell(player, "Stacked " + moved + " " + Pile.Title(box.Item), moved, box.Item);
        return moved;
    }

    public static int DumpNearby(Player player)
    {
        if (player == null)
            return 0;
        var inv = player.GetInventory();
        if (inv == null)
            return 0;

        var origin = player.transform.position;
        var range = Mathf.Max(PileConfig.Range.Value, ModConfig.StorageRange.Value);
        var moved = 0;
        ItemDrop.ItemData? last = null;
        var credit = new System.Collections.Generic.Dictionary<PileBox, (int N, ItemDrop.ItemData Sample)>();
        foreach (var item in inv.GetAllItems().ToArray())
        {
            if (!CanDump(item))
                continue;
            var box = Nearest(origin, range, item);
            if (box == null)
                continue;
            var n = item.m_stack;
            var sample = item.Clone();
            sample.m_stack = n;
            inv.RemoveItem(item);
            moved += n;
            last = sample;
            if (credit.TryGetValue(box, out var have))
                credit[box] = (have.N + n, have.Sample);
            else
                credit[box] = (n, sample);
        }

        foreach (var pair in credit)
        {
            var dest = pair.Key;
            var n = pair.Value.N;
            var sample = pair.Value.Sample;
            Pile.OwnThen(dest.View, view => Pile.Write(view, Pile.Count(view) + n),
                () => dest.DropStacks(sample, n));
        }

        if (moved <= 0)
            return 0;

        inv.Changed(true, false);
        if (last != null)
            Tell(player, "Stacked " + moved + " into piles", moved, last);
        return moved;
    }

    public static void VacuumNearby(Player player)
    {
        if (player == null || !PileConfig.On)
            return;
        if (Time.time < _nextVacuum)
            return;
        _nextVacuum = Time.time + ModConfig.VacuumInterval.Value;
        var origin = player.transform.position;
        var range = Mathf.Max(PileConfig.Range.Value, ModConfig.StorageRange.Value);
        foreach (var drop in ItemDrop.s_instances.ToArray())
        {
            if (drop?.m_itemData?.m_shared == null || drop.m_nview == null || !drop.m_nview.IsValid())
                continue;
            if (drop.m_itemData.m_shared.m_questItem || !drop.CanPickup(false))
                continue;
            if ((drop.transform.position - origin).sqrMagnitude > range * range)
                continue;
            var box = Nearest(origin, range, drop.m_itemData);
            if (box == null)
                continue;
            var id = drop.m_nview.GetZDO().m_uid;
            if (!NearbyStorage.BeginDeposit(id))
                continue;
            Plugin.Instance.StartCoroutine(AbsorbDrop(drop, box, id));
        }
    }

    private static IEnumerator AbsorbDrop(ItemDrop drop, PileBox box, ZDOID id)
    {
        try
        {
            if (box == null || !box.Live)
                yield break;
            if (!box.View.IsOwner())
            {
                box.View.ClaimOwnership();
                var until = Time.time + 1.5f;
                while (box != null && box.Live && !box.View.IsOwner() && Time.time < until)
                    yield return null;
            }

            if (box == null || !box.Live || !box.View.IsOwner())
                yield break;
            if (drop == null || drop.m_nview == null || !drop.m_nview.IsValid())
                yield break;
            if (!drop.m_nview.IsOwner())
            {
                drop.m_nview.ClaimOwnership();
                var until = Time.time + 1.5f;
                while (drop != null && drop.m_nview != null && drop.m_nview.IsValid()
                       && !drop.m_nview.IsOwner() && Time.time < until)
                    yield return null;
            }

            if (drop == null || drop.m_nview == null || !drop.m_nview.IsValid() || !drop.m_nview.IsOwner())
                yield break;
            if (drop.m_itemData == null || drop.m_itemData.m_stack <= 0)
                yield break;
            if (!Matches(drop.m_itemData, box.Item))
                yield break;

            var n = drop.m_itemData.m_stack;
            Pile.Write(box.View, box.Stored + n);
            drop.m_nview.Destroy();
        }
        finally
        {
            NearbyStorage.EndDeposit(id);
        }
    }

    private static bool CanDump(ItemDrop.ItemData? item)
    {
        if (item?.m_shared == null || item.m_equipped || item.m_stack <= 0)
            return false;
        if (item.m_shared.m_maxStackSize <= 1 || item.m_shared.m_questItem)
            return false;
        return !ExtraSlots.SkipDeposit(item) && !SlotLock.Held(item);
    }

    private static PileBox? Nearest(Vector3 origin, float range, ItemDrop.ItemData item)
    {
        PileBox? best = null;
        var bestSqr = range * range;
        foreach (var box in PileBox.All)
        {
            if (box == null || !box.Live || !Matches(box.Item, item))
                continue;
            var sqr = (box.transform.position - origin).sqrMagnitude;
            if (sqr > bestSqr)
                continue;
            bestSqr = sqr;
            best = box;
        }

        return best;
    }

    private static void Tell(Player player, string text, int amount, ItemDrop.ItemData item)
    {
        if (ModConfig.NoticesEnabled.Value)
            Notices.Post(text, amount, RestlessUi.IconOf(item));
        else
            player.Message(MessageHud.MessageType.Center, text);
    }
}
