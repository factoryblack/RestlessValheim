using BepInEx;
using BepInEx.Logging;
using Jotunn;
using Jotunn.Utils;

namespace RestlessCook;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("restless.core", BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.cook";
    public const string PluginName = "RestlessCook";
    public const string PluginVersion = "0.1.2";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        var book = CookYaml.LoadEmbedded();
        RecipeEngine.Load(book);
        Log.LogInfo($"{PluginName} {PluginVersion} loaded ({book.Items.Count} cook.yaml rows, {book.Kit.Count} kit).");
    }
}
