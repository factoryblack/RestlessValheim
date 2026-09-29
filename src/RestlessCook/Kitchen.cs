using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessCook;

// The food preparation table is the kitchen. This is the board the cookbook UI
// binds to. Planning reads the cook.yaml graph. Placing an order is what starts
// the racks, ovens and cauldron. The screen itself is not in here.
//
// Ready means the ingredients are on hand and the meal is not prepared yet.
// Queued means an order is waiting on a station. Cooking means it is on one.

public enum KitchenStationKind
{
    None,
    Cauldron,
    PrepTable,
    Rack,
    Oven,
    MeadKettle
}

public enum KitchenStepState
{
    Prepared,
    Ready,
    Queued,
    Cooking,
    Missing,
    Blocked
}

public sealed class KitchenUse
{
    public string Item = "";
    public string Name = "";
    public int Amount;
    public int Have;
}

public sealed class KitchenStep
{
    public string Id = "";
    public string Name = "";
    public string Output = "";
    public string Parent = "";
    public int Depth;
    public int Need;
    public int Have;
    public int Cooking;
    public KitchenStepState State;
    public KitchenStationKind Station;
    public string StationPrefab = "";
    public int StationLevel = 1;
    public string Note = "";
    public int ParentIndex = -1;
    // Zero duration means no reliable native timer is available; UI must not invent one.
    public float ElapsedSeconds;
    public float DurationSeconds;
    public readonly List<KitchenUse> Uses = new();
}

public sealed class KitchenOrder
{
    public int Id;
    public string Feast = "";
    public string Name = "";
    public int Count;
    public long PlayerId;
    public int Ready;
    public int Collected;
}

public sealed class KitchenPantryItem
{
    public string Prefab = "";
    public string Name = "";
    public int Count;
    public int Reserved;
}

public sealed class KitchenStationInfo
{
    public string Name = "";
    public KitchenStationKind Kind;
    public int Free;
    public int Cooking;
    public int Ready;
    public int Fuel;
    public int FuelMax;
    public bool NeedsFire;
    public bool FireLit = true;
    public int Level;
    public string Block = "";
}

public static class Kitchen
{
    public static event Action<CraftingStation>? Opened;

    public static IReadOnlyList<CookRow> Rows => KitchenRun.Rows;

    public static void Load(CookBook book) => KitchenRun.Load(book);

    public static CookRow? Find(string idOrPrefab) => KitchenRun.Find(idOrPrefab);

    public static IReadOnlyList<CookRow> OtherUses(string prefab) => KitchenRun.OtherUses(prefab);

    public static IReadOnlyList<KitchenStep> Plan(CraftingStation table, string idOrPrefab, int count) =>
        KitchenRun.Plan(table, idOrPrefab, count);

    public static IReadOnlyList<KitchenOrder> Orders(CraftingStation table) => KitchenRun.ReadOrders(table);

    public static IReadOnlyList<KitchenStationInfo> Stations(CraftingStation table) => KitchenRun.Describe(table);

    public static IReadOnlyList<KitchenStep> Live(CraftingStation table, int orderId) =>
        KitchenRun.Live(table, orderId);

    public static bool Place(CraftingStation table, string idOrPrefab, int count) =>
        KitchenRun.Place(table, idOrPrefab, count);

    public static void Cancel(CraftingStation table, int orderId) => KitchenRun.Cancel(table, orderId);

    // Moves that order's finished feasts into the player's inventory.
    // Those feasts count as delivered. A fully delivered order is removed.
    public static int Collect(CraftingStation table, int orderId) => KitchenRun.Collect(table, orderId);

    // Everything stored on the table. Reserved is ready food an active order is waiting to collect.
    public static IReadOnlyList<KitchenPantryItem> Pantry(CraftingStation table) => KitchenRun.Pantry(table);

    // Takes unreserved pantry food. Ready feasts stay until Collect.
    public static int Take(CraftingStation table, string prefab, int count) =>
        KitchenRun.Take(table, prefab, count);

    internal static void Raise(CraftingStation table)
    {
        if (Opened != null)
        {
            Opened(table);
            return;
        }

        var player = Player.m_localPlayer;
        player?.Message(MessageHud.MessageType.Center, "Kitchen board is not in yet.");
    }
}

