using System;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn;
using Jotunn.Utils;
using RestlessQoL.Api;
using UnityEngine;

namespace RestlessWorks;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.works";
    public const string PluginName = "RestlessWorks";
    public const string PluginVersion = "0.1.0";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static ConfigEntry<bool> BoardEnabled = null!;

    private float _clock;
    private Harmony? _uiPatches;
    private IDisposable? _page;
    private IDisposable? _settings;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        BoardEnabled = Config.Bind("Interface", "Work orders", true,
            "Open the work-order board. Local preference; does not stop orders already placed.");
        _page = SettingsPageApi.Register(new SettingsPage(PluginGuid, PluginName,
            "Standing orders for the kilns and smelters.",
            "A work-order board queues the production machines nearby. The Cookbook stays the kitchen."));
        _settings = SettingsPageApi.RegisterSettings(PluginGuid,
            new SettingsSection("Work orders", new SettingsOption("Open the board", BoardEnabled, false)));
        BoardPiece.Load();
        Works.Opened += WorkshopWindow.Open;
        _uiPatches = new Harmony(PluginGuid + ".ui");
        _uiPatches.PatchAll(typeof(WorkshopWindow).Assembly);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        Works.Opened -= WorkshopWindow.Open;
        WorkshopWindow.Close();
        _uiPatches?.UnpatchSelf();
        _settings?.Dispose();
        _page?.Dispose();
    }

    private void Update()
    {
        _clock += Time.deltaTime;
        if (_clock < 1f)
            return;
        var elapsed = _clock;
        _clock = 0f;
        WorksRun.TickOwned(elapsed);
    }
}

