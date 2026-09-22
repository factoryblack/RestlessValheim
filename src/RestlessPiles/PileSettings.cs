using System;
using RestlessQoL.Api;

namespace RestlessPiles;

internal static class PileSettings
{
    internal static IDisposable Register() => SettingsPageApi.RegisterSettings(Plugin.PluginGuid,
        new SettingsSection("Resource piles",
            new SettingsOption("Enable RestlessPiles", PileConfig.Enabled, true),
            new SettingsOption("Isolate pile range", PileConfig.Isolate, true),
            new SettingsOption("Pile range", PileConfig.Range, true, "m")));
}
