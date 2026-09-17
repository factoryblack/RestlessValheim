using System;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Jotunn;
using Jotunn.Configs;
using UnityEngine;

namespace RestlessQoL.Core;

public static class ModConfig
{
    private static readonly ConfigurationManagerAttributes Admin = new() { IsAdminOnly = true };

    public static ConfigEntry<bool> LockConfiguration = null!;

    public static ConfigEntry<bool> StorageEnabled = null!;
    public static ConfigEntry<float> StorageRange = null!;
    public static ConfigEntry<bool> LeaveOne = null!;

    public static ConfigEntry<bool> CraftFromStorageEnabled = null!;
    public static ConfigEntry<bool> BuildFromStorageEnabled = null!;
    public static ConfigEntry<bool> QuickStackEnabled = null!;
    public static ConfigEntry<KeyboardShortcut> QuickStackHotkey = null!;
    public static ConfigEntry<bool> RestockEnabled = null!;
    public static ConfigEntry<KeyboardShortcut> RestockHotkey = null!;

    public static ConfigEntry<bool> StationPullEnabled = null!;
    public static ConfigEntry<bool> PetPantryEnabled = null!;
    public static ConfigEntry<bool> VacuumEnabled = null!;
    public static ConfigEntry<float> VacuumInterval = null!;
    public static ConfigEntry<bool> AreaRepairEnabled = null!;
    public static ConfigEntry<float> AreaRepairRadius = null!;
    public static ConfigEntry<bool> WorkbenchTweaksEnabled = null!;
    public static ConfigEntry<float> WorkbenchRange = null!;

    public static ConfigEntry<bool> DeathPinsEnabled = null!;
    public static ConfigEntry<bool> SwimWieldEnabled = null!;
    public static ConfigEntry<bool> CrossbowStateEnabled = null!;
    public static ConfigEntry<bool> FriendlyFireEnabled = null!;
    public static ConfigEntry<bool> AxeComboEnabled = null!;
    public static ConfigEntry<bool> AutoRepairEnabled = null!;
    public static ConfigEntry<float> StackSizeMultiplier = null!;
    public static ConfigEntry<bool> HonestItemsEnabled = null!;
    public static ConfigEntry<bool> EternalFireEnabled = null!;
    public static ConfigEntry<bool> DigDeeperEnabled = null!;
    public static ConfigEntry<float> DigMaxDelta = null!;
    public static ConfigEntry<bool> NetworkUncapEnabled = null!;
    public static ConfigEntry<bool> FloatingItemsEnabled = null!;
    public static ConfigEntry<bool> FloatingEverything = null!;
    public static ConfigEntry<string> FloatingSinkList = null!;
    public static ConfigEntry<string> FloatingExtraList = null!;
    public static ConfigEntry<KeyboardShortcut> SettingsHotkey = null!;
    public static ConfigEntry<bool> BuffListEnabled = null!;
    public static ConfigEntry<bool> BuffListFood = null!;
    public static ConfigEntry<bool> BuffListFoodEmpty = null!;
    public static ConfigEntry<bool> MapChromeEnabled = null!;
    public static ConfigEntry<bool> MapTearEnabled = null!;
    public static ConfigEntry<bool> MapBiomePlate = null!;
    public static ConfigEntry<bool> MapWindPlate = null!;
    public static ConfigEntry<bool> MapScreenEnabled = null!;
    public static ConfigEntry<bool> MapPlayerDots = null!;
    public static ConfigEntry<bool> NoticesEnabled = null!;
    public static ConfigEntry<bool> NoticesMerge = null!;
    public static ConfigEntry<bool> NoticesFill = null!;
    public static ConfigEntry<float> NoticesHold = null!;
    public static ConfigEntry<bool> HotbarEnabled = null!;
    public static ConfigEntry<bool> InventoryScreenEnabled = null!;
    public static ConfigEntry<bool> TooltipEnabled = null!;
    public static ConfigEntry<bool> LedgerEnabled = null!;
    public static ConfigEntry<bool> MenuScreenEnabled = null!;
    public static ConfigEntry<bool> ExtraSlotsEnabled = null!;
    public static ConfigEntry<bool> SlotLockEnabled = null!;
    public static ConfigEntry<KeyboardShortcut> QuickSlot1 = null!;
    public static ConfigEntry<KeyboardShortcut> QuickSlot2 = null!;
    public static ConfigEntry<KeyboardShortcut> QuickSlot3 = null!;
    public static ConfigEntry<bool> VitalsEnabled = null!;
    public static ConfigEntry<bool> VitalsStaminaAlways = null!;
    public static ConfigEntry<bool> VitalsAdrenaline = null!;
    public static ConfigEntry<bool> VitalsEitr = null!;
    public static ConfigEntry<bool> VitalsNumbers = null!;
    public static ConfigEntry<bool> EquipHintEnabled = null!;
    public static ConfigEntry<bool> BuildMenuEnabled = null!;
    public static ConfigEntry<bool> ActionHintsEnabled = null!;
    public static ConfigEntry<bool> LookHintsEnabled = null!;
    public static ConfigEntry<bool> PieceHealthEnabled = null!;
    public static ConfigEntry<bool> JotunnDebug = null!;

    public static void Bind(ConfigFile config)
    {
        SettingsHotkey = config.Bind("Client", "SettingsShortcut",
            new KeyboardShortcut(KeyCode.F8),
            "Open Restless settings. Client-local. Pause menu also has a Restless button.");
        BuffListEnabled = config.Bind("Hud.Buffs", "Enabled", true,
            "Status effects as a vertical list under the minimap. Client-local.");
        BuffListFood = config.Bind("Hud.Buffs", "ShowFood", true,
            "Also list food on the buff strip. Vanilla does not. Client-local.");
        BuffListFoodEmpty = config.Bind("Hud.Buffs", "EmptyFoodSlots", true,
            "Keep empty food rows under the map so a free meal slot reads Eat. Client-local.");
        MapChromeEnabled = config.Bind("Hud.Minimap", "Enabled", true,
            "Restless HUD minimap chrome: hide the square box, torn edge, biome and wind plates. Client-local.");
        MapTearEnabled = config.Bind("Hud.Minimap", "TornEdge", true,
            "Torn paper edge on the HUD minimap. Client-local.");
        MapBiomePlate = config.Bind("Hud.Minimap", "BiomePlate", true,
            "Biome name on a dark plate like the buff rows. Client-local.");
        MapWindPlate = config.Bind("Hud.Minimap", "WindPlate", true,
            "Wind arrow on the right of the biome bar, same cream as the title. Client-local.");
        MapScreenEnabled = config.Bind("Hud.Map", "Enabled", true,
            "Restless chrome on the M map: torn mat, biome and pin-name plates, hint plate. Terrain stays vanilla. Client-local.");
        MapPlayerDots = config.Bind("Hud.Map", "PlayerDots", true,
            "Show every other player on the map and stay visible to them. Cream diamond instead of the vanilla person. Client-local.");
        NoticesEnabled = config.Bind("Hud.Notices", "Enabled", true,
            "Every MessageHud toast stacks top-left, including first-item and recipe unlocks. Biome-found stays vanilla. Client-local.");
        NoticesMerge = config.Bind("Hud.Notices", "MergeMatching", true,
            "Same line refreshes the sitting row and counts up (×N) instead of stacking a duplicate. Client-local.");
        NoticesFill = config.Bind("Hud.Notices", "DurationFill", true,
            "Gold sit-clock on each toast, same fill language as buffs. Client-local.");
        NoticesHold = config.Bind("Hud.Notices", "HoldSeconds", 7.2f,
            new ConfigDescription("How long a toast sits before it fades. Client-local.",
                new AcceptableValueRange<float>(1.5f, 16f)));
        if (Mathf.Approximately(NoticesHold.Value, 3.6f))
            NoticesHold.Value = 7.2f;
        HotbarEnabled = config.Bind("Hud.Hotbar", "Enabled", true,
            "Torn-edge squares and Averia key chips on the item hotbar, pinned to the bottom center. Client-local.");
        InventoryScreenEnabled = config.Bind("Hud.Inventory", "Enabled", true,
            "Torn plates and Averia on the Tab inventory. Vanilla grids, drag, and craft stay. Client-local.");
        TooltipEnabled = config.Bind("Hud.Tooltip", "Enabled", true,
            "Restless item inspect on Tab and chests, with expansion badges and sections. PgUp/PgDn scroll long details. Client-local.");
        LedgerEnabled = config.Bind("Hud.Character", "Enabled", true,
            "Show the F8 Character tab (vanilla PlayerStats). The ledger API stays on. Client-local.");
        MenuScreenEnabled = config.Bind("Hud.Menu", "Enabled", true,
            "Torn chips and Averia on the ESC pause menu, logout, and exit confirms. Client-local.");
        ExtraSlotsEnabled = config.Bind("Hud.ExtraSlots", "Enabled", true,
            "Dedicated armor well to the right of Tab, plus three quick slots on the hotbar (Z / X / C). Client-local.");
        SlotLockEnabled = config.Bind("Hud.Inventory", "SlotLock", true,
            "Middle-click a player bag cell to lock it. Stack, quick stack, and restock skip it. Client-local.");
        QuickSlot1 = config.Bind("Hud.ExtraSlots", "QuickSlot1", new KeyboardShortcut(KeyCode.Z),
            "Use the first extra quick slot. Client-local.");
        QuickSlot2 = config.Bind("Hud.ExtraSlots", "QuickSlot2", new KeyboardShortcut(KeyCode.X),
            "Use the second extra quick slot. Client-local.");
        QuickSlot3 = config.Bind("Hud.ExtraSlots", "QuickSlot3", new KeyboardShortcut(KeyCode.C),
            "Use the third extra quick slot. Client-local.");
        VitalsEnabled = config.Bind("Hud.Vitals", "Enabled", true,
            "Health, stamina, adrenaline, and eitr as torn plates flanking the hotbar. Health and adrenaline grow left; stamina and eitr grow right. Client-local.");
        VitalsStaminaAlways = config.Bind("Hud.Vitals", "StaminaAlways", true,
            "Keep the stamina plate visible even when full. Vanilla hides it. Client-local.");
        VitalsAdrenaline = config.Bind("Hud.Vitals", "Adrenaline", true,
            "Adrenaline plate stacked above health when a trinket gives you a pool. Client-local.");
        VitalsEitr = config.Bind("Hud.Vitals", "Eitr", true,
            "Eitr plate stacked above stamina when you have an eitr pool. Client-local.");
        VitalsNumbers = config.Bind("Hud.Vitals", "Numbers", false,
            "Averia current on the vital plates. Off while we play with the look. Client-local.");
        EquipHintEnabled = config.Bind("Hud.EquipHint", "Enabled", true,
            "Selected piece helper when the hammer or cultivator is out. Client-local.");
        BuildMenuEnabled = config.Bind("Hud.Build", "Enabled", true,
            "Torn plates on the hammer / hoe piece menu. Vanilla search and grid stay. Client-local.");
        ActionHintsEnabled = config.Bind("Hud.Hints", "Enabled", true,
            "Replace the bottom-right keybind cluster (combat, fishing, hammer) with KeyChips. Client-local.");
        LookHintsEnabled = config.Bind("Hud.Look", "Enabled", true,
            "Replace world hover prompts ([E] Cook item, pick up, etc.) with KeyChips. One Hud dresser — every CookingStation and every other interactable. Client-local.");
        PieceHealthEnabled = config.Bind("Hud.Debug", "PieceHealth", false,
            "Hammer hover: Jötunn piece panel (health, stability, rotation) and the vanilla health bar. Off by default. Client-local.");
        JotunnDebug = config.Bind("Hud.Debug", "JotunnDebugInfo", false,
            "Jötunn corner overlay (version, FPS, position). Client-local.");
        JotunnDebug.SettingChanged += (_, _) => ApplyJotunnDebug();
        PieceHealthEnabled.SettingChanged += (_, _) => ApplyJotunnDebug();
        ApplyJotunnDebug();

        LockConfiguration = AdminBool(config, "Server", "LockConfiguration", true,
            "If on, gameplay settings are admin-only and sync from the server.");

        StorageEnabled = AdminBool(config, "Storage", "Enabled", true,
            "Master switch for nearby-container access.");
        StorageRange = AdminFloat(config, "Storage", "Range", 20f, 4f, 50f,
            "How far to search for containers, in metres.");
        LeaveOne = AdminBool(config, "Storage", "LeaveOne", true,
            "Leave one item in a stack when pulling, so the chest stays a valid restock target.");

        CraftFromStorageEnabled = AdminBool(config, "Storage.Crafting", "Enabled", true,
            "Spend materials in nearby chests when crafting. Recipe ingredient counts are have/need including those chests.");
        BuildFromStorageEnabled = AdminBool(config, "Storage.Building", "Enabled", true,
            "Spend materials in nearby chests when placing pieces.");
        QuickStackEnabled = AdminBool(config, "Storage.QuickStack", "Enabled", true,
            "Deposit matching stacks into nearby chests.");
        QuickStackHotkey = config.Bind("Storage.QuickStack", "Shortcut",
            new KeyboardShortcut(KeyCode.BackQuote),
            "Dump matching stacks. Client-local. BackQuote is the ` key left of 1 — G is vanilla OpenRadial.");
        RestockEnabled = AdminBool(config, "Storage.Restock", "Enabled", true,
            "Fill existing player stacks from nearby chests.");
        RestockHotkey = config.Bind("Storage.Restock", "Shortcut",
            new KeyboardShortcut(KeyCode.BackQuote, KeyCode.LeftShift),
            "Fill existing stacks from chests. Client-local.");

        StationPullEnabled = AdminBool(config, "Production", "PullFromStorage", true,
            "Smelters, kilns, and fires pull ore/fuel from nearby chests.");
        PetPantryEnabled = AdminBool(config, "Storage.PetPantry", "Enabled", true,
            "Tamed animals eat matching food from nearby chests when hungry.");
        VacuumEnabled = AdminBool(config, "Storage.Vacuum", "Enabled", true,
            "Ground piles go into nearby chests that already have that item. Empty chests are ignored.");
        VacuumInterval = AdminFloat(config, "Storage.Vacuum", "Interval", 0.75f, 0.2f, 5f,
            "Seconds between vacuum scans.");
        AreaRepairEnabled = AdminBool(config, "Building.AreaRepair", "Enabled", true,
            "Hammer repair also repairs pieces in a radius.");
        AreaRepairRadius = AdminFloat(config, "Building.AreaRepair", "Radius", 15f, 2f, 40f,
            "Area repair radius in metres.");
        WorkbenchTweaksEnabled = AdminBool(config, "Building.Workbench", "Enabled", true,
            "Override crafting-station use range.");
        WorkbenchRange = AdminFloat(config, "Building.Workbench", "Range", 20f, 4f, 40f,
            "Crafting station range in metres.");

        DeathPinsEnabled = AdminBool(config, "Player.DeathPins", "Enabled", true,
            "Remove the death map pin when the tombstone is emptied.");
        SwimWieldEnabled = AdminBool(config, "Player.SwimWield", "Enabled", true,
            "Keep weapons and tools equipped while swimming.");
        CrossbowStateEnabled = AdminBool(config, "Player.Crossbow", "Enabled", true,
            "Keep a loaded crossbow loaded when you swap off it.");
        FriendlyFireEnabled = AdminBool(config, "Player.FriendlyFire", "Enabled", true,
            "Ballistae ignore tames. Tames do not hurt each other or players. Players do not hurt tames.");
        AxeComboEnabled = AdminBool(config, "Player.AxeCombo", "Enabled", true,
            "Do not interrupt the axe combo when chopping trees.");
        AutoRepairEnabled = AdminBool(config, "Player.AutoRepair", "Enabled", true,
            "Repair worn gear when you use a station that can repair it. Hides the hammer button.");
        StackSizeMultiplier = AdminFloat(config, "Inventory.StackSize", "Multiplier", 2f, 1f, 10f,
            "Multiply vanilla max stacks. 1 is vanilla. Gear that does not stack stays at 1. The host value applies to everyone.");
        HonestItemsEnabled = AdminBool(config, "Inventory.Honest", "Enabled", true,
            "Clear Valheim 1.0 cheated marks on items. Storage clones, extra-slot moves, and custom crafts were stamping them. Does not un-flag a character that used devcommands.");

        NetworkUncapEnabled = AdminBool(config, "Server", "NetworkUncap", true,
            "Raise the ZDO send window (10 KB → 32 KB) and Steam send max (150 KB/s → 512 KB/s). Stops players/mobs freezing then snapping. Host-locked.");

        EternalFireEnabled = AdminBool(config, "Production.EternalFire", "Enabled", true,
            "Campfires, hearths, and held torches stay lit. Smelter/kiln fuel is unchanged.");
        DigDeeperEnabled = AdminBool(config, "World.DigDeeper", "Enabled", true,
            "Allow digging and raising terrain past the vanilla depth cap.");
        DigMaxDelta = AdminFloat(config, "World.DigDeeper", "MaxDelta", 20f, Heightmap.c_LevelMaxDelta, 40f,
            $"Max height change from the original terrain, in metres. Vanilla is {Heightmap.c_LevelMaxDelta}.");

        FloatingItemsEnabled = AdminBool(config, "World.FloatingItems", "Enabled", true,
            "Add or remove water buoyancy on dropped items.");
        FloatingEverything = AdminBool(config, "World.FloatingItems", "FloatEverything", true,
            "Every drop floats except the sink list. Turn off to float only ExtraFloat.");
        FloatingSinkList = AdminString(config, "World.FloatingItems", "Sink", "BronzeNails,IronNails",
            "Prefab names that always sink. Comma-separated.");
        FloatingExtraList = AdminString(config, "World.FloatingItems", "ExtraFloat", "SerpentScale",
            "Prefab names that float when FloatEverything is off. Comma-separated.");
    }

    internal static void ApplyJotunnDebug()
    {
        SetJotunn("DebugInfo", "Enabled", JotunnDebug?.Value ?? false);
        SetJotunn("HoverInfo", "Enabled", PieceHealthEnabled?.Value ?? false);
    }

    private static void SetJotunn(string section, string key, bool value)
    {
        try
        {
            if (!Chainloader.PluginInfos.TryGetValue(Jotunn.Main.ModGuid, out var info) || info?.Instance == null)
                return;
            var def = new ConfigDefinition(section, key);
            if (!info.Instance.Config.TryGetEntry(def, out ConfigEntry<bool> entry))
                return;
            if (entry.Value != value)
                entry.Value = value;
        }
        catch (Exception e)
        {
            Plugin.Log.LogWarning("jotunn " + section + ": " + e.Message);
        }
    }

    private static ConfigEntry<bool> AdminBool(ConfigFile config, string section, string key, bool value, string desc) =>
        config.Bind(section, key, value, new ConfigDescription(desc, null, Admin));

    private static ConfigEntry<float> AdminFloat(ConfigFile config, string section, string key, float value, float min, float max, string desc) =>
        config.Bind(section, key, value, new ConfigDescription(desc, new AcceptableValueRange<float>(min, max), Admin));

    private static ConfigEntry<string> AdminString(ConfigFile config, string section, string key, string value, string desc) =>
        config.Bind(section, key, value, new ConfigDescription(desc, null, Admin));
}
