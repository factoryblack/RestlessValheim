using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn;
using Jotunn.Utils;

namespace RestlessDrawers;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.drawers";
    public const string PluginName = "RestlessDrawers";
    public const string PluginVersion = "0.1.1";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        DrawerPieces.Load();
        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();
        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    private void OnDestroy() => _harmony?.UnpatchSelf();
}
