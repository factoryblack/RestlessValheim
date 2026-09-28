using System;
using System.Collections.Generic;
using System.Text;
using RestlessQoL.Api;
using RestlessQoL.Core;
using RestlessQoL.Storage;
using UnityEngine;

namespace RestlessStorage;

// Read-only adapter. This is a view of accessible, loaded nearby containers,
// not a global network and not a client-side clone-and-consume transfer.
internal sealed class StorageSource : IStorageWindowSource
{
    private readonly Storekeeper _table;
    private readonly Player _player;
    private string _signature = "";
    private long _revision;
    internal StorageSource(Storekeeper table, Player player) { _table = table; _player = player; }
    public bool IsAvailable => Plugin.WindowEnabled.Value && _table != null && _player != null
        && Player.m_localPlayer == _player && !_player.IsDead()
        && (_table.transform.position - _player.transform.position).sqrMagnitude < 25f;

    public StorageSnapshot Capture()
    {
        var groups = new Dictionary<string, Group>(StringComparer.Ordinal);
        var stores = 0;
        foreach (var container in NearbyStorage.ForLocalPlayer())
        {
            var inventory = container.GetInventory();
            if (inventory == null || inventory == _player.GetInventory()) continue;
            stores++;
            var location = Local(inventory.GetName()) + " · "
                + Mathf.RoundToInt(Vector3.Distance(_player.transform.position, container.transform.position)) + " m";
            var quantities = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var item in inventory.GetAllItems())
            {
                if (item?.m_shared == null || item.m_stack <= 0) continue;
                var id = Identity(item);
                if (!groups.TryGetValue(id, out var group)) groups.Add(id, group = new Group(item));
                group.Count += item.m_stack;
                quantities.TryGetValue(id, out var count);
                quantities[id] = count + item.m_stack;
            }
            foreach (var pair in quantities) groups[pair.Key].Locations.Add(new StorageLocation(location, pair.Value));
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
        var stamp = signature.ToString();
        if (stamp != _signature) { _signature = stamp; _revision++; }
        return new StorageSnapshot(_revision, "Storage network", "Storekeeper's Table · nearby storage",
            stores, _player.GetInventory().GetTotalWeight(), _player.GetMaxCarryWeight(), false,
            ModConfig.StorageEnabled.Value ? "Browsing only · withdrawals unavailable" : "Nearby storage is disabled by the host",
            resources.ToArray());
    }

    public void Withdraw(string resourceId, int amount, Action<string> completed)
        => completed("Withdrawals are unavailable.");

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
    private static string Identity(ItemDrop.ItemData item)
    {
        // Preserve variants/custom data in the provider's opaque key. A later
        // transfer adapter must not collapse unlike equipment into one request.
        var b = new StringBuilder();
        void Part(string value) => b.Append(value.Length).Append(':').Append(value);
        Part(item.m_dropPrefab != null ? item.m_dropPrefab.name : item.m_shared.m_name);
        Part(item.m_quality.ToString()); Part(item.m_variant.ToString()); Part(item.m_worldLevel.ToString());
        Part(item.m_durability.ToString("R")); Part(item.m_crafterID.ToString());
        if (item.m_customData != null)
        {
            var keys = new List<string>(item.m_customData.Keys); keys.Sort(StringComparer.Ordinal);
            foreach (var key in keys) { Part(key); Part(item.m_customData[key]); }
        }
        return b.ToString();
    }
    private sealed class Group
    {
        internal readonly ItemDrop.ItemData Item;
        internal long Count;
        internal readonly List<StorageLocation> Locations = new();
        internal Group(ItemDrop.ItemData item) => Item = item;
    }
}
