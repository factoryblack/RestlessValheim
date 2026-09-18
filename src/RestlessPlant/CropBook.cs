using System.Collections.Generic;

namespace RestlessPlant;

internal readonly struct CropRow
{
    public CropRow(string source, string item, string name, string description)
    {
        Source = source;
        Item = item;
        Name = name;
        Description = description;
    }

    public string Source { get; }
    public string Item { get; }
    public string Name { get; }
    public string Description { get; }
    public string Prefab => "Restless_" + Source;
}

internal static class CropBook
{
    public static readonly IReadOnlyList<CropRow> Rows = new[]
    {
        new CropRow("RaspberryBush", "Raspberry", "Raspberry bush", "Plant a raspberry bush."),
        new CropRow("BlueberryBush", "Blueberries", "Blueberry bush", "Plant a blueberry bush."),
        new CropRow("CloudberryBush", "Cloudberry", "Cloudberry bush", "Plant a cloudberry bush."),
        new CropRow("Pickable_Mushroom", "Mushroom", "Mushroom", "Plant mushrooms."),
        new CropRow("Pickable_Mushroom_yellow", "MushroomYellow", "Yellow mushroom", "Plant yellow mushrooms."),
        new CropRow("Pickable_Thistle", "Thistle", "Thistle", "Plant thistle."),
        new CropRow("Pickable_Dandelion", "Dandelion", "Dandelion", "Plant dandelion."),
        new CropRow("Pickable_Mushroom_Magecap", "MushroomMagecap", "Magecap", "Plant magecap."),
        new CropRow("Pickable_Mushroom_JotunPuffs", "MushroomJotunPuffs", "Jotun puffs", "Plant jotun puffs."),
        new CropRow("Pickable_SmokePuff", "MushroomSmokePuff", "Smoke puff", "Plant smoke puff."),
        new CropRow("Pickable_Fiddlehead", "Fiddleheadfern", "Fiddlehead", "Plant fiddlehead."),
    };

    public static bool IsOurs(string prefabName)
    {
        if (string.IsNullOrEmpty(prefabName))
            return false;
        if (prefabName.StartsWith("Restless_"))
            return true;
        foreach (var row in Rows)
        {
            if (row.Source == prefabName || row.Prefab == prefabName)
                return true;
        }

        return false;
    }
}
