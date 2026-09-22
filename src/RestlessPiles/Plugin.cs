using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Managers;
using Jotunn.Utils;

namespace RestlessPiles;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.piles";
    public const string PluginName = "RestlessPiles";
    public const string PluginVersion = "0.1.5";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;
    private System.IDisposable? _settings;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        PileConfig.Bind(Config);
        _settings = PileSettings.Register();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        GUIManager.OnCustomGUIAvailable += PileUi.TearDown;
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        if (Console.IsVisible())
            return;
        PileUi.Tick();
    }

    private void OnDestroy()
    {
        _settings?.Dispose();
        GUIManager.OnCustomGUIAvailable -= PileUi.TearDown;
        _harmony?.UnpatchSelf();
    }
}
