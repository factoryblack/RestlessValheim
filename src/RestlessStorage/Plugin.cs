using System;
using BepInEx;
using BepInEx.Logging;
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
    public const string PluginVersion = "0.1.0";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;
    private IDisposable? _page;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        _page = SettingsPageApi.Register(new SettingsPage(
            PluginGuid,
            PluginName,
            "A table for the stores around you.",
            "Build a Storekeeper's Table at the workbench. The storage network window is the next piece of this plugin."));
        StorekeeperPiece.Load();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        _page?.Dispose();
        _harmony?.UnpatchSelf();
    }
}
