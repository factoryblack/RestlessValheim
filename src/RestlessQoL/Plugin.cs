using BepInEx;
using BepInEx.Logging;
using Jotunn;
using Jotunn.Utils;
using RestlessQoL.Building;
using RestlessQoL.Core;
using RestlessQoL.PlayerTweaks;
using RestlessQoL.Production;
using RestlessQoL.Storage;
using RestlessQoL.HudTweaks;
using RestlessQoL.World;

namespace RestlessQoL;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency(Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
[SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "restless.core";
    public const string PluginName = "RestlessCore";
    public const string PluginVersion = "0.1.0";

    internal static Plugin Instance { get; private set; } = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    private readonly FeatureModule[] _modules =
    {
        new StorageSync(),
        new CraftFromStorage(),
        new BuildFromStorage(),
        new QuickStack(),
        new Restock(),
        new PetPantry(),
        new GroundVacuum(),
        new StationPull(),
        new AreaRepair(),
        new WorkbenchTweaks(),
        new DeathPins(),
        new SwimWield(),
        new CrossbowState(),
        new FriendlyFire(),
        new AxeCombo(),
        new AutoRepair(),
        new StackSizes(),
        new EternalFire(),
        new DigDeeper(),
        new NetworkUncap(),
        new FloatingItems(),
        new Ledger(),
        new SettingsUi(),
        new MenuScreen(),
        new BuffList(),
        new MapChrome(),
        new MapScreen(),
        new MapDots(),
        new Notices(),
        new Hotbar(),
        new InventoryScreen(),
        new ItemTooltip(),
        new ExtraSlots(),
        new SlotLock(),
        new Vitals(),
        new EquipHint(),
        new BuildMenu(),
        new ActionHints(),
        new LookHints(),
    };

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        ModConfig.Bind(Config);
        Kit.Warm();

        foreach (var module in _modules)
            module.TryLoad();

        Log.LogInfo($"{PluginName} {PluginVersion} loaded ({_modules.Length} modules registered).");
    }

    private void Update()
    {
        if (Console.IsVisible())
            return;
        if (SettingsUi.IsOpen)
        {
            foreach (var module in _modules)
            {
                if (module is SettingsUi)
                    module.Tick();
            }
            return;
        }

        if (Player.m_localPlayer == null)
            return;
        var menu = InventoryGui.IsVisible();
        foreach (var module in _modules)
        {
            if (menu && !module.TickInMenus)
                continue;
            module.Tick();
        }
    }
}
