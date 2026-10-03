using System;
using BepInEx.Configuration;
using RestlessQoL.Api;
using BepInEx;
using BepInEx.Logging;
using Jotunn;
using Jotunn.Utils;
using UnityEngine;

namespace RestlessCook;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.cook";
    public const string PluginName = "RestlessCook";
    public const string PluginVersion = "0.3.1";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private float _kitchen;
    internal static ConfigEntry<bool> CookbookEnabled = null!;
    private IDisposable? _cookbookSettings;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        var book = CookYaml.LoadEmbedded();
        RecipeEngine.Load(book);
        Kitchen.Load(book);
        CookbookEnabled = Config.Bind("Interface", "Cookbook", true, "Open the Cookbook at preparation tables. Local interface preference; does not stop kitchen orders.");
        _cookbookSettings = SettingsPageApi.RegisterSettings(PluginGuid,
            new SettingsSection("Cookbook", new SettingsOption("Open Cookbook", CookbookEnabled, false)));
        Kitchen.Opened += CookbookWindow.Open;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded ({book.Items.Count} cook.yaml rows, {book.Kit.Count} kit).");
    }

    private void OnDestroy()
    {
        Kitchen.Opened -= CookbookWindow.Open;
        CookbookWindow.Close();
        _cookbookSettings?.Dispose();
    }

    private void Update()
    {
        _kitchen += Time.deltaTime;
        if (_kitchen < 1f)
            return;
        var elapsed = _kitchen;
        _kitchen = 0f;
        KitchenHook.TickOwned(elapsed);
    }
}

