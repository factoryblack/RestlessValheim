using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;
using RestlessQoL.Api;

namespace RestlessStorage;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.storage";
    public const string PluginName = "RestlessStorage";
    public const string PluginVersion = "0.2.1";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;
    private IDisposable? _page;
    private IDisposable? _settings;
    internal static ConfigEntry<bool> WindowEnabled = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        WindowEnabled = Config.Bind("Interface", "StorageWindow", true,
            "Open the shared storage browser when using a Storekeeper's Table. Local visual preference.");
        _settings = SettingsPageApi.RegisterSettings(PluginGuid,
            new SettingsSection("Storage window", new SettingsOption("Open storage browser", WindowEnabled, false)));
        _page = SettingsPageApi.Register(new SettingsPage(
            PluginGuid,
            PluginName,
            "A table for the stores around you.",
            "Build a Storekeeper's Table at the workbench. Browse nearby stores, then take a stack into your inventory. The chest owner moves the items. Whatever does not fit goes back.",
            packageUrl: "https://thunderstore.io/c/valheim/p/Restless/RestlessStorage/"));
        StorekeeperPiece.Load();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        Storekeeper.CloseWindow();
        _settings?.Dispose();
        _page?.Dispose();
        _harmony?.UnpatchSelf();
    }
}

