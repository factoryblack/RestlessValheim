using System;
using System.Collections.Generic;
using RestlessQoL.Ui;
using UnityEngine;

namespace RestlessQoL.Api;

/// <summary>Shared presentation for storage providers. Call on Unity's main thread.
/// Providers own discovery, authority and transfers; Core never mutates inventories.</summary>
public static class StorageWindowApi
{
    public static bool IsOpen => StorageWindow.IsOpen;
    public static bool BlocksMenuInput => StorageWindow.BlocksMenu;
    public static bool Open(IStorageWindowSource source) => StorageWindow.Open(source);
    public static void Close(IStorageWindowSource source) => StorageWindow.Close(source);
}

public interface IStorageWindowSource
{
    bool IsAvailable { get; }
    StorageSnapshot Capture();
    // Exactly one completion, on Unity's main thread. A closed window does not
    // cancel an accepted transfer. Provider must revalidate capacity/access/item
    // identity and use owner-authoritative, acknowledged transfer semantics.
    void Withdraw(string resourceId, int amount, Action<string> completed);
}

public sealed class StorageSnapshot
{
    public long Revision { get; }
    public string Title { get; }
    public string Scope { get; }
    public int StoreCount { get; }
    public float CarryWeight { get; }
    public float CarryLimit { get; }
    public bool CanWithdraw { get; }
    public string Status { get; }
    public IReadOnlyList<StorageResource> Resources { get; }
    public StorageSnapshot(long revision, string title, string scope, int storeCount,
        float carryWeight, float carryLimit, bool canWithdraw, string status, StorageResource[] resources)
    {
        Revision = revision; Title = title; Scope = scope; StoreCount = storeCount;
        CarryWeight = carryWeight; CarryLimit = carryLimit; CanWithdraw = canWithdraw;
        Status = status; Resources = Array.AsReadOnly((StorageResource[])resources.Clone());
    }
}

public sealed class StorageResource
{
    public string Id { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public Sprite? Icon { get; }
    public long Count { get; }
    public int StackSize { get; }
    public float UnitWeight { get; }
    public IReadOnlyList<StorageLocation> Locations { get; }
    public StorageResource(string id, string name, string category, string description, Sprite? icon,
        long count, int stackSize, float unitWeight, StorageLocation[] locations)
    {
        Id = id; Name = name; Category = category; Description = description; Icon = icon;
        Count = Math.Max(0, count); StackSize = Math.Max(1, stackSize); UnitWeight = Math.Max(0f, unitWeight);
        Locations = Array.AsReadOnly((StorageLocation[])locations.Clone());
    }
}

public sealed class StorageLocation
{
    public string Name { get; }
    public long Count { get; }
    public StorageLocation(string name, long count) { Name = name; Count = count; }
}
