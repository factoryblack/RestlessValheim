using BepInEx.Configuration;
using Jotunn.Configs;
using RestlessQoL.Core;

namespace RestlessPiles;

internal static class PileConfig
{
    private static readonly ConfigurationManagerAttributes Admin = new() { IsAdminOnly = true };

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<bool> Isolate = null!;
    public static ConfigEntry<float> Range = null!;

    public static bool On => Enabled.Value;

    // One nearby radius: Core Search range, unless this host isolates piles.
    public static float Nearby => Isolate.Value ? Range.Value : ModConfig.StorageRange.Value;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Piles", "Enabled", true,
            new ConfigDescription(
                "Vanilla wood stacks / stone piles (and the other resource piles) open as uncapped single-item stores. Host-locked.",
                null, Admin));
        Isolate = config.Bind("Piles", "IsolateRange", false,
            new ConfigDescription(
                "Use Pile range below instead of Core Search range for ` , vacuum, craft, and station pull from piles. Host-locked.",
                null, Admin));
        Range = config.Bind("Piles", "Range", 10f,
            new ConfigDescription(
                "How far piles reach when Isolate pile range is on. Ignored otherwise — piles use Core Search range.",
                new AcceptableValueRange<float>(2f, 30f), Admin));
    }
}
