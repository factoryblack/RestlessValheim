using System;
using System.Collections.Generic;
using UnityEngine;

namespace RestlessWorks;

// The work-order board is the workshop, in the same way the preparation table
// is the kitchen. This is the board the workshop window binds to. The screen
// itself is not in here.
//
// Make produces that many more, then stops. Keep refills until usable stock,
// after reservations, reaches the target. Finished stock and ore already in a
// machine both count, so several kilns do not each start the same shortage.

public sealed class WorksRecipe
{
    public string Output = "";
    public string OutputName = "";
    public string Input = "";
    public string InputName = "";
    public string Station = "";
    public string StationName = "";
    public string Fuel = "";
    public string FuelName = "";
    public int FuelEach = 1;
}

public sealed class WorksUse
{
    public string Item = "";
    public string Name = "";
    public int Amount;
    public int Available;
}

public sealed class WorksStep
{
    public string Output = "";
    public string Name = "";
    public string Parent = "";
    public int Depth;
    public int Need;
    public int Available;
    public string Station = "";
    public string Note = "";
    public readonly List<WorksUse> Uses = new();
}

public sealed class WorksMachine
{
    public string Name = "";
    public string Prefab = "";
    public Vector3 Position;
    public int Free;
    public int Cooking;
    public int Ready;
    public int Fuel;
    public int FuelMax;
    public string FuelItem = "";
    public string Block = "";
}

public sealed class WorksStock
{
    public string Prefab = "";
    public string Name = "";
    public int Count;
    public int Reserved;
}

public static class Works
{
    public static event Action<Piece>? Opened;

    public static IReadOnlyList<WorksRecipe> Recipes() => WorksRun.Recipes();

    public static WorksRecipe? Find(string output) => WorksRun.Find(output);

    public static IReadOnlyList<WorksStep> Plan(Piece board, string output, int count) =>
        WorksRun.Plan(board, output, count);

    public static IReadOnlyList<WorksOrder> Orders(Piece board) => WorksRun.ReadOrders(board);

    public static IReadOnlyList<WorksMachine> Machines(Piece board) => WorksRun.Machines(board);

    public static IReadOnlyList<WorksStock> Pantry(Piece board) => WorksRun.Pantry(board);

    public static bool Place(Piece board, string output, WorksOrderMode mode, int count) =>
        WorksRun.Place(board, output, mode, count);

    public static void Cancel(Piece board, int orderId) => WorksRun.Cancel(board, orderId);

    // Hands that Make order's finished output to the player who placed it.
    public static int Collect(Piece board, int orderId) => WorksRun.Collect(board, orderId);

    // Takes pantry stock that is not reserved for a Make order.
    public static int Take(Piece board, string prefab, int count) => WorksRun.Take(board, prefab, count);

    internal static void Raise(Piece board)
    {
        if (!Plugin.BoardEnabled.Value)
        {
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Enable the work-order board in F8 · RestlessWorkshop.");
            return;
        }

        if (Opened != null)
        {
            Opened(board);
            return;
        }

        Player.m_localPlayer?.Message(MessageHud.MessageType.Center, "Work-order board is not in yet.");
    }
}


