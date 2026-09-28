using System;
using System.Collections.Generic;
using System.Text;
using RestlessQoL.Api;
using RestlessQoL.Core;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessStorage;

// Nearby containers the player can already open. Take asks the chest owner
// for the real stacks. This is not a claim on unloaded storage.
internal sealed class StorageSource : IStorageWindowSource
{
    private readonly Storekeeper _table;
    private readonly Player _player;
    private string _signature = "";
    private long _revision;
    private bool _busy;
    internal StorageSource(Storekeeper table, Player player) { _table = table; _player = player; }
    public bool IsAvailable => Plugin.WindowEnabled.Value && _table != null && _player != null
        && Player.m_localPlayer == _player && !_player.IsDead()
        && (_table.transform.position - _player.transform.position).sqrMagnitude < 25f;

    public StorageSnapshot Capture()
    {
        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
        var stores = 0;
        void Add(ItemDrop.ItemData item, int count, string location)
        {
            if (item?.m_shared == null || count <= 0) return;
            var id = ItemKey.Of(item);
            if (!groups.TryGetValue(id, out var group)) groups.Add(id, group = new Group(item));
            group.Count += count;
            group.Pending.TryGetValue(location, out var have);
            group.Pending[location] = have + count;
        }
        foreach (var container in NearbyStorage.ForLocalPlayer())
        {
            var inventory = container.GetInventory();
            if (inventory == null || inventory == _player.GetInventory()) continue;
            stores++;
            var location = Local(inventory.GetName()) + " · "
                + Mathf.RoundToInt(Vector3.Distance(_player.transform.position, container.transform.position)) + " m";
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_stack <= 0) continue;
                Add(item, item.m_stack, location);
            }
        }
        var lots = new List<NearbyLot>();
        NearbyLots.Collect(_player, ModConfig.StorageRange.Value, lots);
        foreach (var lot in lots)
        {
            if (lot.Item?.m_shared == null || lot.Count <= 0) continue;
            stores++;
            Add(lot.Item, lot.Count, lot.Place);
        }
        foreach (var group in groups.Values)
        {
            foreach (var pair in group.Pending)
                group.Locations.Add(new StorageLocation(pair.Key, pair.Value));
            group.Pending.Clear();
        }
        var keys = new List<string>(groups.Keys);
        keys.Sort(StringComparer.Ordinal);
        var resources = new List<StorageResource>();
        var signature = new StringBuilder().Append(stores).Append('|').Append(ModConfig.StorageEnabled.Value);
        foreach (var id in keys)
        {
            var group = groups[id]; var item = group.Item;
            group.Locations.Sort((a, b) => StringComparer.CurrentCulture.Compare(a.Name, b.Name));
            var name = Local(item.m_shared.m_name);
            if (item.m_quality > 1) name += " · " + item.m_quality;
            var resource = new StorageResource(id, name, Category(item), Local(item.m_shared.m_description),
                item.GetIcon(), group.Count, item.m_shared.m_maxStackSize, item.GetWeight() / Math.Max(1, item.m_stack),
                group.Locations.ToArray());
            resources.Add(resource);
            signature.Append(id).Append(':').Append(group.Count).Append(':').Append(name).Append(resource.Description).Append(resource.Category)
                .Append(resource.UnitWeight).Append(resource.StackSize);
            foreach (var source in group.Locations) signature.Append(source.Name).Append(':').Append(source.Count);
        }
        Touch(_player, groups.Values);
        var stamp = signature.ToString();
        if (stamp != _signature) { _signature = stamp; _revision++; }
        var open = ModConfig.StorageEnabled.Value;
        return new StorageSnapshot(_revision, "Storage network", "Storekeeper's Table · nearby storage",
            stores, _player.GetInventory().GetTotalWeight(), _player.GetMaxCarryWeight(), open,
            open ? "Take from nearby stores" : "Nearby storage is disabled by the host",
            resources.ToArray());
    }

    public void Withdraw(string resourceId, int amount, Action<string> completed)
    {
        if (completed == null)
            return;
        if (_busy)
            return;
        if (!IsAvailable)
        {
            completed("The table is out of reach.");
            return;
        }

        if (!ModConfig.StorageEnabled.Value)
        {
            completed("Nearby storage is disabled by the host.");
            return;
        }

        if (amount < 1 || string.IsNullOrEmpty(resourceId))
        {
            completed("Nothing nearby.");
            return;
        }

        var chests = new List<Container>(NearbyStorage.ForLocalPlayer());
        var origin = _player.transform.position;
        chests.Sort((a, b) =>
            (a.transform.position - origin).sqrMagnitude.CompareTo((b.transform.position - origin).sqrMagnitude));
        var label = LabelFor(chests, resourceId);
        _busy = true;
        Walk(chests, resourceId, 0, amount, 0, false, false, label, message =>
        {
            _busy = false;
            completed(message);
        });
    }

    private void Walk(List<Container> chests, string resourceId, int index, int left, int moved, bool blocked,
        bool timedOut, string label, Action<string> completed)
    {
        if (blocked || timedOut || left <= 0 || index > chests.Count)
        {
            completed(Result(label, moved, blocked, timedOut));
            return;
        }

        if (index == chests.Count)
        {
            NearbyLots.Take(_player, resourceId, left, taken =>
            {
                var rest = left - taken;
                var full = rest > 0 && _player.GetInventory().GetEmptySlots() <= 0;
                Walk(chests, resourceId, index + 1, rest, moved + taken, full, false, label, completed);
            });
            return;
        }

        var chest = chests[index];
        StorageWithdraw.Request(chest, _player.GetPlayerID(), resourceId, left, batch =>
        {
            if (batch.TimedOut)
            {
                Walk(chests, resourceId, index + 1, left, moved, blocked, true, label, completed);
                return;
            }

            var bag = _player != null ? _player.GetInventory() : null;
            if (bag == null)
            {
                if (batch.Id == 0)
                    StorageWithdraw.ReturnTo(chest, batch.Items);
                else if (batch.Items.Count > 0)
                    StorageWithdraw.Acknowledge(chest, batch.Id, batch.Items);
                completed("The table is out of reach.");
                return;
            }

            var overflow = new List<ItemDrop.ItemData>();
            var added = StorageWithdraw.Give(bag, batch.Items, overflow);
            if (batch.Id == 0)
                StorageWithdraw.ReturnTo(chest, overflow);
            else if (batch.Items.Count > 0)
                StorageWithdraw.Acknowledge(chest, batch.Id, overflow);
            Walk(chests, resourceId, index + 1, left - added, moved + added, overflow.Count > 0, false, label, completed);
        });
    }

    private string LabelFor(List<Container> chests, string resourceId)
    {
        foreach (var chest in chests)
        {
            var inventory = chest.GetInventory();
            if (inventory == null)
                continue;
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || ItemKey.Of(item) != resourceId)
                    continue;
                var name = Local(item.m_shared.m_name);
                return item.m_quality > 1 ? name + " " + item.m_quality : name;
            }
        }

        var lots = new List<NearbyLot>();
        NearbyLots.Collect(_player, ModConfig.StorageRange.Value, lots);
        foreach (var lot in lots)
        {
            if (lot.Item?.m_shared == null || ItemKey.Of(lot.Item) != resourceId)
                continue;
            var name = Local(lot.Item.m_shared.m_name);
            return lot.Item.m_quality > 1 ? name + " " + lot.Item.m_quality : name;
        }

        return "items";
    }

    private static string Result(string label, int moved, bool blocked, bool timedOut)
    {
        if (timedOut && moved <= 0)
            return "The chest did not answer. Nothing was taken.";
        if (timedOut)
            return "Took " + moved.ToString("N0") + " " + label + ". A later chest did not answer.";
        if (moved <= 0 && blocked)
            return "No room in your inventory.";
        if (moved <= 0)
            return "Nothing nearby.";
        if (blocked)
            return "Took " + moved.ToString("N0") + " " + label + ". No room for the rest.";
        return "Took " + moved.ToString("N0") + " " + label + ".";
    }

    // A stack on the sheet counts as handled. Recipes missed because someone
    // else picked the materials up can unlock. The crafting station level is
    // still required. Trophies are not marked collected, and pickup stats stay put.
    private static void Touch(Player player, Dictionary<string, Group>.ValueCollection groups)
    {
        if (player != Player.m_localPlayer)
            return;
        var fresh = false;
        foreach (var group in groups)
        {
            var name = group.Item?.m_shared?.m_name;
            if (string.IsNullOrEmpty(name) || player.m_knownMaterial.Contains(name))
                continue;
            player.m_knownMaterial.Add(name);
            fresh = true;
        }

        if (!fresh)
            return;
        player.UpdateKnownRecipesList();
        player.UpdateEvents();
    }

    private static string Local(string value) => Localization.instance != null ? Localization.instance.Localize(value) : value;
    private static string Category(ItemDrop.ItemData item)
    {
        if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f) return "Food";
        // Use game types, not an incomplete hard-coded prefab list. Modded items
        // remain discoverable, including unknown types in Other.
        return item.m_shared.m_itemType.ToString() switch
        {
            "Material" => "Materials", "Consumable" => "Food", "Ammo" or "AmmoNonEquipable" => "Equipment",
            "OneHandedWeapon" or "TwoHandedWeapon" or "TwoHandedWeaponLeft" or "Bow" or "Shield"
                or "Helmet" or "Chest" or "Legs" or "Shoulder" or "Utility" or "Tool" => "Equipment",
            _ => "Other"
        };
    }
    private sealed class Group
    {
        internal readonly ItemDrop.ItemData Item;
        internal long Count;
        internal readonly List<StorageLocation> Locations = new();
        internal readonly Dictionary<string, long> Pending = new(StringComparer.Ordinal);
        internal Group(ItemDrop.ItemData item) => Item = item;
    }
}
