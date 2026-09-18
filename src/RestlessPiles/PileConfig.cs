using BepInEx.Configuration;
using Jotunn.Configs;

namespace RestlessPiles;

internal static class PileConfig
{
    private static readonly ConfigurationManagerAttributes Admin = new() { IsAdminOnly = true };

    public static ConfigEntry<bool> Enabled = null!;
    public static ConfigEntry<float> Range = null!;

    public static bool On => Enabled.Value;

    public static void Bind(ConfigFile config)
    {
        Enabled = config.Bind("Piles", "Enabled", true,
            new ConfigDescription(
                "Vanilla wood stacks / stone piles (and the other resource piles) open as uncapped single-item stores. Host-locked.",
                null, Admin));
        Range = config.Bind("Piles", "Range", 10f,
            new ConfigDescription(
                "How far ` and vacuum send matching items into nearby resource piles. A stone pile takes stone even at 0 stored.",
                new AcceptableValueRange<float>(2f, 30f), Admin));
    }
}
