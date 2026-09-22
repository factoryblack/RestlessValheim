using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;

namespace RestlessPlant;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.plant";
    public const string PluginName = "RestlessPlant";
    public const string PluginVersion = "0.1.5";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;
    private System.IDisposable? _settings;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        PlantConfig.Bind(Config);
        _settings = PlantSettings.Register();
        CropPieces.Load();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void Update()
    {
        if (Console.IsVisible())
            return;
        PlantGrid.Tick();
    }

    private void OnDestroy()
    {
        _settings?.Dispose();
        PlantGrid.Clear();
        _harmony?.UnpatchSelf();
    }
}
