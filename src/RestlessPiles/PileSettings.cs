using System;
using RestlessQoL.Api;

namespace RestlessPiles;

internal static class PileSettings
{
    internal static IDisposable Register() => SettingsPageApi.RegisterSettings(Plugin.PluginGuid,
        new SettingsSection("Resource piles",
            new SettingsOption("Enable RestlessPiles", PileConfig.Enabled, true),
            new SettingsOption("Deposit range", PileConfig.Range, true, "m")));
}
