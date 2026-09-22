using System;
using RestlessQoL.Api;

namespace RestlessPlant;

internal static class PlantSettings
{
    internal static IDisposable Register() => SettingsPageApi.RegisterSettings(Plugin.PluginGuid,
        new SettingsSection("Planting",
            new SettingsOption("Enable RestlessPlant", PlantConfig.Enabled, true, requiresRestart: true),
            new SettingsOption("Berry bushes and forage", PlantConfig.ExtraCrops, true, requiresRestart: true),
            new SettingsOption("Extra saplings", PlantConfig.ExtraSaplings, true, requiresRestart: true),
            new SettingsOption("Decorative plants", PlantConfig.ExtraFlora, true, requiresRestart: true),
            new SettingsOption("Grow anywhere", PlantConfig.GrowAnywhere, true, requiresRestart: true)),
        new SettingsSection("Harvesting",
            new SettingsOption("Harvest neighbouring crops", PlantConfig.BulkHarvest, true),
            new SettingsOption("Collect neighbouring beehives", PlantConfig.BulkBeehives, true),
            new SettingsOption("Harvest range", PlantConfig.HarvestRange, true, "m"),
            new SettingsOption("Replant on harvest", PlantConfig.Replant, true)),
        new SettingsSection("Your planting grid",
            new SettingsOption("Grid planting", PlantConfig.Grid, false),
            new SettingsOption("Columns", PlantConfig.GridColumns, false),
            new SettingsOption("Rows", PlantConfig.GridRows, false),
            new SettingsOption("Spacing", PlantConfig.Spacing, false, "m"),
            new SettingsOption("Snap to existing field", PlantConfig.SnapToField, false),
            new SettingsOption("Snap range", PlantConfig.SnapRange, false, "m")),
        new SettingsSection("Your controls and hints",
            new SettingsOption("Show regrowth time", PlantConfig.RegrowHint, false),
            new SettingsOption("Add column", PlantConfig.ColsUp, false),
            new SettingsOption("Remove column", PlantConfig.ColsDown, false),
            new SettingsOption("Add row", PlantConfig.RowsUp, false),
            new SettingsOption("Remove row", PlantConfig.RowsDown, false),
            new SettingsOption("Toggle field snapping", PlantConfig.SnapToggle, false)));
}
