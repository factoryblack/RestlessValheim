using System;
using System.Collections.Generic;

namespace RestlessCook;

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

