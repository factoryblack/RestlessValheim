using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantConfig
{
    private static readonly ConfigurationManagerAttributes Admin = new() { IsAdminOnly = true };

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<bool> ExtraCrops = null!;
    public static ConfigEntry<bool> Grid = null!;
    public static ConfigEntry<int> GridSize = null!;
    public static ConfigEntry<float> Spacing = null!;
    public static ConfigEntry<bool> BulkHarvest = null!;
    public static ConfigEntry<float> HarvestRange = null!;
    public static ConfigEntry<bool> Replant = null!;
    public static ConfigEntry<KeyboardShortcut> SizeUp = null!;
    public static ConfigEntry<KeyboardShortcut> SizeDown = null!;

    public static bool On => Enabled.Value;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Plant", "Enabled", true,
            new ConfigDescription("Cultivator extras, grid planting, and bulk harvest. Host-locked.",
                null, Admin));
        ExtraCrops = config.Bind("Plant", "BerryBushesAndForage", true,
            new ConfigDescription("Add berry bushes, mushrooms, thistle, and later-biome forage to the cultivator. Host-locked.",
                null, Admin));
        Grid = config.Bind("Plant", "Grid", true,
            "Plant a square of the selected cultivator piece. Client-local size.");
        GridSize = config.Bind("Plant", "GridSize", 1,
            new ConfigDescription("Odd width of the planting square (1 is vanilla single). [ ] resize.",
                new AcceptableValueRange<int>(1, 7)));
        Spacing = config.Bind("Plant", "Spacing", 2f,
            new ConfigDescription("Metres between grid cells. Plant pieces use their grow radius when larger.",
                new AcceptableValueRange<float>(1f, 4f)));
        BulkHarvest = config.Bind("Plant", "BulkHarvest", true,
            new ConfigDescription("Picking a player-grown plant also picks matching neighbours. Host-locked.",
                null, Admin));
        HarvestRange = config.Bind("Plant", "HarvestRange", 2.5f,
            new ConfigDescription("Bulk-harvest radius.",
                new AcceptableValueRange<float>(1f, 8f), Admin));
        Replant = config.Bind("Plant", "ReplantOnHarvest", true,
            new ConfigDescription("One-shot crops (carrots and the like) replant the picked piece if you can pay for it. Host-locked.",
                null, Admin));
        SizeUp = config.Bind("Client", "GridBigger", new KeyboardShortcut(KeyCode.RightBracket),
            "Grow the planting square. Client-local.");
        SizeDown = config.Bind("Client", "GridSmaller", new KeyboardShortcut(KeyCode.LeftBracket),
            "Shrink the planting square. Client-local.");
    }
}
