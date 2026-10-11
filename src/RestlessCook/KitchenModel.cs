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
    MeadKettle,
    Fermenter
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
    // Kept separately because Amount becomes the remaining order requirement.
    internal int UnitAmount;
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
    // On-hand stock this step can see, including amounts above Need.
    public int Available;
    public int Cooking;
    internal bool CoveredByParent;
    public KitchenStepState State;
    public KitchenStationKind Station;
    public string StationPrefab = "";
    public int StationLevel = 1;
    public string Note = "";
    public int ParentIndex = -1;
    // Zero duration means no reliable native timer is available; UI must not invent one.
    public float ElapsedSeconds;
    public float DurationSeconds;
    public string ActiveStation = "";
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
    public string Id = "";
    public float Distance;
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

