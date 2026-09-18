using System.Collections;
using System.Linq;
using RestlessQoL.Core;
using RestlessQoL.HudTweaks;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessPiles;

internal static class PileBag
{
    public static bool Matches(ItemDrop.ItemData? a, ItemDrop.ItemData? b)
    {
        if (a?.m_shared == null || b?.m_shared == null)
            return false;
        return a.m_shared.m_name == b.m_shared.m_name;
    }

    public static int TakeStack(Player player, PileBox box)
    {
        var taken = 0;
        Pile.OwnThen(box.View, _ => taken = TakeOwned(player, box));
        return taken;
    }

    private static int TakeOwned(Player player, PileBox box)
    {
        if (player == null || box == null || box.Stored <= 0)
            return 0;
        var inv = player.GetInventory();
        var item = box.Item;
        if (inv == null || item?.m_shared == null)
            return 0;

        var max = item.m_shared.m_maxStackSize;
        var partial = inv.FindFreeStackItem(item.m_shared.m_name, item.m_quality, item.m_worldLevel, item.m_cheated);
        int take;
        if (partial != null)
            take = Mathf.Min(max - partial.m_stack, box.Stored);
        else if (inv.HaveEmptySlot())
            take = Mathf.Min(max, box.Stored);
        else
        {
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
            if (!inv.AddItem(clone))
            {
                player.Message(MessageHud.MessageType.Center, "Bag is full");
                return 0;
            }
        }

        inv.Changed(true, false);
        Pile.Write(box.View, box.Stored - take);
        Tell(player, "Took " + take + " " + Pile.Title(item), take, item);
        return take;
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
        var range = PileConfig.Range.Value;
        foreach (var item in inv.GetAllItems().ToArray())
        {
            if (!CanDump(item))
                continue;
            var box = Nearest(origin, range, item);
            if (box == null)
                continue;
            var payload = item;
            Pile.OwnThen(box.View, _ =>
            {
                if (payload.m_stack <= 0 || !inv.ContainsItem(payload))
                    return;
                var n = payload.m_stack;
                inv.RemoveItem(payload);
                Pile.Write(box.View, box.Stored + n);
                inv.Changed(true, false);
            });
        }

        return 0;
    }

    public static void VacuumNearby(Player player)
    {
        if (player == null || !PileConfig.On)
            return;
        var origin = player.transform.position;
        var range = PileConfig.Range.Value;
        foreach (var drop in ItemDrop.s_instances.ToArray())
        {
            if (drop?.m_itemData?.m_shared == null || drop.m_nview == null || !drop.m_nview.IsValid())
                continue;
            if (drop.m_itemData.m_shared.m_questItem || !drop.CanPickup(true))
                continue;
            if ((drop.transform.position - origin).sqrMagnitude > range * range)
                continue;
            var box = Nearest(origin, range, drop.m_itemData);
            if (box == null)
                continue;
            Plugin.Instance.StartCoroutine(AbsorbDrop(drop, box));
        }
    }

    private static IEnumerator AbsorbDrop(ItemDrop drop, PileBox box)
    {
        if (drop?.m_nview == null || !drop.m_nview.IsValid() || box == null)
            yield break;
        var id = drop.m_nview.GetZDO().m_uid;
        if (!NearbyStorage.BeginDeposit(id))
            yield break;

        try
        {
            if (!box.View.IsOwner())
            {
                box.View.ClaimOwnership();
                var until = Time.time + 1.5f;
                while (box != null && box.View.IsValid() && !box.View.IsOwner() && Time.time < until)
                    yield return null;
            }

            if (box == null || !box.View.IsValid() || !box.View.IsOwner())
                yield break;
            if (drop == null || drop.m_itemData == null || drop.m_itemData.m_stack <= 0)
                yield break;
            if (!Matches(drop.m_itemData, box.Item))
                yield break;

            var n = drop.m_itemData.m_stack;
            Pile.Write(box.View, box.Stored + n);
            drop.m_itemData.m_stack = 0;
            if (!drop.m_nview.IsOwner())
            {
                drop.m_nview.ClaimOwnership();
                var until = Time.time + 1.5f;
                while (drop != null && drop.m_nview != null && drop.m_nview.IsValid()
                       && !drop.m_nview.IsOwner() && Time.time < until)
                    yield return null;
            }

            if (drop != null && drop.m_nview != null && drop.m_nview.IsValid() && drop.m_nview.IsOwner())
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
            if (box == null || !Matches(box.Item, item))
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
