using BepInEx.Configuration;
using Jotunn.Configs;
using UnityEngine;

namespace RestlessPlant;

internal static class PlantConfig
{
    private static readonly ConfigurationManagerAttributes Admin = new() { IsAdminOnly = true };
    private static readonly ConfigurationManagerAttributes Hidden = new() { Browsable = false };

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<bool> ExtraCrops = null!;
    public static ConfigEntry<bool> ExtraSaplings = null!;
    public static ConfigEntry<bool> ExtraFlora = null!;
    public static ConfigEntry<bool> GrowAnywhere = null!;
    public static ConfigEntry<bool> Grid = null!;
    public static ConfigEntry<int> GridColumns = null!;
    public static ConfigEntry<int> GridRows = null!;
    public static ConfigEntry<float> Spacing = null!;
    public static ConfigEntry<bool> SnapToField = null!;
    public static ConfigEntry<float> SnapRange = null!;
    public static ConfigEntry<bool> BulkHarvest = null!;
    public static ConfigEntry<bool> BulkBeehives = null!;
    public static ConfigEntry<float> HarvestRange = null!;
    public static ConfigEntry<bool> Replant = null!;
    public static ConfigEntry<bool> RegrowHint = null!;
    public static ConfigEntry<KeyboardShortcut> ColsUp = null!;
    public static ConfigEntry<KeyboardShortcut> ColsDown = null!;
    public static ConfigEntry<KeyboardShortcut> RowsUp = null!;
    public static ConfigEntry<KeyboardShortcut> RowsDown = null!;
    public static ConfigEntry<KeyboardShortcut> SnapToggle = null!;

    public static bool On => Enabled.Value;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Plant", "Enabled", true,
            new ConfigDescription("Cultivator extras, grid planting, and bulk harvest. Host-locked.",
                null, Admin));
        ExtraCrops = config.Bind("Plant", "BerryBushesAndForage", true,
            new ConfigDescription("Add berry bushes, mushrooms, thistle, and later-biome forage to the cultivator. Host-locked.",
                null, Admin));
        ExtraSaplings = config.Bind("Plant", "ExtraSaplings", true,
            new ConfigDescription("Add ancient, ygga, autumn birch, and ashwood saplings to the cultivator. Host-locked.",
                null, Admin));
        ExtraFlora = config.Bind("Plant", "DecorativeTrees", true,
            new ConfigDescription("Add small trees, shrubs, vines, and stick/stone/flint pickables to the cultivator. Host-locked.",
                null, Admin));
        GrowAnywhere = config.Bind("Plant", "GrowAnywhere", false,
            new ConfigDescription("Skip tilled ground, biome, sun, and space checks so plants can sit on floors and wild soil. Host-locked.",
                null, Admin));

        Grid = config.Bind("Grid", "Enabled", true,
            "Plant a rectangle of the selected cultivator piece. Client-local size.");
        var legacy = config.Bind("Plant", "GridSize", 1,
            new ConfigDescription("Obsolete. Copied once into GridColumns and GridRows.",
                new AcceptableValueRange<int>(1, 7), Hidden));
        GridColumns = config.Bind("Grid", "Columns", 1,
            new ConfigDescription("How many plants wide. [ ] resize.",
                new AcceptableValueRange<int>(1, 7)));
        GridRows = config.Bind("Grid", "Rows", 1,
            new ConfigDescription("How many plants deep. - = resize.",
                new AcceptableValueRange<int>(1, 7)));
        if (GridColumns.Value == 1 && GridRows.Value == 1 && legacy.Value > 1)
        {
            GridColumns.Value = legacy.Value;
            GridRows.Value = legacy.Value;
        }

        Spacing = config.Bind("Grid", "Spacing", 2f,
            new ConfigDescription("Metres between grid cells. Plant pieces use their grow radius when larger.",
                new AcceptableValueRange<float>(1f, 4f)));
        SnapToField = config.Bind("Grid", "SnapToField", true,
            "Snap the cultivator ghost onto a nearby plant's spacing. Client-local.");
        SnapRange = config.Bind("Grid", "SnapRange", 3f,
            new ConfigDescription("How far to look for a plant to snap to, in metres.",
                new AcceptableValueRange<float>(1f, 8f)));

        BulkHarvest = config.Bind("Harvest", "BulkHarvest", true,
            new ConfigDescription("Picking a player-grown plant also picks matching neighbours. Host-locked.",
                null, Admin));
        BulkBeehives = config.Bind("Harvest", "BulkBeehives", true,
            new ConfigDescription("Using a beehive also takes honey from matching neighbours. Host-locked.",
                null, Admin));
        HarvestRange = config.Bind("Harvest", "HarvestRange", 2.5f,
            new ConfigDescription("Bulk-harvest radius for plants and hives.",
                new AcceptableValueRange<float>(1f, 8f), Admin));
        Replant = config.Bind("Harvest", "ReplantOnHarvest", true,
            new ConfigDescription("One-shot crops (carrots and the like) replant the picked piece if you can pay for it. Host-locked.",
                null, Admin));

        RegrowHint = config.Bind("Client", "RegrowHint", true,
            "Show time until fruit or a sapling is ready. Client-local.");
        ColsUp = config.Bind("Client", "GridColumnsBigger", new KeyboardShortcut(KeyCode.RightBracket),
            "Add a column to the planting rectangle. Client-local.");
        ColsDown = config.Bind("Client", "GridColumnsSmaller", new KeyboardShortcut(KeyCode.LeftBracket),
            "Remove a column from the planting rectangle. Client-local.");
        RowsUp = config.Bind("Client", "GridRowsBigger", new KeyboardShortcut(KeyCode.Equals),
            "Add a row to the planting rectangle. Client-local.");
        RowsDown = config.Bind("Client", "GridRowsSmaller", new KeyboardShortcut(KeyCode.Minus),
            "Remove a row from the planting rectangle. Client-local.");
        SnapToggle = config.Bind("Client", "SnapToggle", new KeyboardShortcut(KeyCode.F10),
            "Toggle snap onto an existing field. Client-local.");
    }
}
