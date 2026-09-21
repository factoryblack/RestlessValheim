using System.Collections.Generic;

namespace RestlessPlant;

internal enum CropGate
{
    Forage,
    Sapling,
    Flora
}

internal readonly struct CropRow
{
    public CropRow(string source, string item, string name, string description,
        CropGate gate = CropGate.Forage, bool till = true, bool attach = false)
    {
        Source = source;
        Item = item;
        Name = name;
        Description = description;
        Gate = gate;
        Till = till;
        Attach = attach;
    }

    public string Source { get; }
    public string Item { get; }
    public string Name { get; }
    public string Description { get; }
    public CropGate Gate { get; }
    public bool Till { get; }
    public bool Attach { get; }
    public string Prefab => "Restless_" + Source;
}

internal static class CropBook
{
    public static readonly IReadOnlyList<CropRow> Rows = new[]
    {
        new CropRow("RaspberryBush", "Raspberry", "Raspberry bush", "Plant a raspberry bush."),
        new CropRow("BlueberryBush", "Blueberries", "Blueberry bush", "Plant a blueberry bush."),
        new CropRow("CloudberryBush", "Cloudberry", "Cloudberry bush", "Plant a cloudberry bush."),
        new CropRow("LingonberryBush", "Lingonberry", "Lingonberry bush", "Plant a lingonberry bush."),
        new CropRow("Pickable_Mushroom", "Mushroom", "Mushroom", "Plant mushrooms."),
        new CropRow("Pickable_Mushroom_yellow", "MushroomYellow", "Yellow mushroom", "Plant yellow mushrooms."),
        new CropRow("Pickable_Mushroom_blue", "MushroomBlue", "Blue mushroom", "Plant blue mushrooms."),
        new CropRow("Pickable_Thistle", "Thistle", "Thistle", "Plant thistle."),
        new CropRow("Pickable_Dandelion", "Dandelion", "Dandelion", "Plant dandelion."),
        new CropRow("Pickable_Mushroom_Magecap", "MushroomMagecap", "Magecap", "Plant magecap."),
        new CropRow("Pickable_Mushroom_JotunPuffs", "MushroomJotunPuffs", "Jotun puffs", "Plant jotun puffs."),
        new CropRow("Pickable_SmokePuff", "MushroomSmokePuff", "Smoke puff", "Plant smoke puff."),
        new CropRow("Pickable_Fiddlehead", "Fiddleheadfern", "Fiddlehead", "Plant fiddlehead."),

        new CropRow("Ancient_Sapling", "ElderBark", "Ancient sapling", "Plant an ancient sapling.", CropGate.Sapling, false),
        new CropRow("Ygga_Sapling", "YggdrasilWood", "Ygga sapling", "Plant a ygga sapling.", CropGate.Sapling, false),
        new CropRow("Autumn_Birch_Sapling", "FineWood", "Autumn birch sapling", "Plant an autumn birch sapling.", CropGate.Sapling, false),
        new CropRow("Ashwood_Sapling", "Blackwood", "Ashwood sapling", "Plant an ashwood sapling.", CropGate.Sapling, false),

        new CropRow("Beech_small1", "Wood", "Small beech", "Plant a small beech.", CropGate.Flora, false),
        new CropRow("FirTree_small", "Wood", "Small fir", "Plant a small fir.", CropGate.Flora, false),
        new CropRow("FirTree_small_dead", "Wood", "Small dead fir", "Plant a small dead fir.", CropGate.Flora, false),
        new CropRow("Bush01", "Wood", "Small bush", "Plant a small bush.", CropGate.Flora, false),
        new CropRow("Bush01_heath", "Wood", "Heath bush", "Plant a heath bush.", CropGate.Flora, false),
        new CropRow("Bush02_en", "Wood", "Fruitless bush", "Plant a fruitless bush.", CropGate.Flora, false),
        new CropRow("shrub_2", "Wood", "Small shrub", "Plant a small shrub.", CropGate.Flora, false),
        new CropRow("shrub_2_heath", "Wood", "Heath shrub", "Plant a heath shrub.", CropGate.Flora, false),
        new CropRow("YggaShoot_small1", "YggdrasilWood", "Ygga shoot", "Plant a small ygga shoot.", CropGate.Flora, false),
        new CropRow("vines", "Wood", "Vines", "Plant wall vines.", CropGate.Flora, false, true),
        new CropRow("FernAshlands", "Wood", "Ashlands fern", "Plant an Ashlands fern.", CropGate.Flora, false),
        new CropRow("Pickable_Branch", "Wood", "Branch", "Place a pickable branch.", CropGate.Flora, false),
        new CropRow("Pickable_Stone", "Stone", "Stone", "Place a pickable stone.", CropGate.Flora, false),
        new CropRow("Pickable_Flint", "Flint", "Flint", "Place a pickable flint.", CropGate.Flora, false),
    };

    public static bool Allowed(CropRow row)
    {
        return row.Gate switch
        {
            CropGate.Sapling => PlantConfig.ExtraSaplings.Value,
            CropGate.Flora => PlantConfig.ExtraFlora.Value,
            _ => PlantConfig.ExtraCrops.Value
        };
    }

    public static bool IsOurs(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return false;
        return prefabName.StartsWith("Restless_");
    }
}
