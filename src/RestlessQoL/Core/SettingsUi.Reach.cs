using System;
using BepInEx.Configuration;
using UnityEngine.UI;

namespace RestlessQoL.Core;

public sealed partial class SettingsUi
{
    private static void WatchReachSettings()
    {
        Watch(ModConfig.GlobalReachEnabled); Watch(ModConfig.GlobalReachProfile);
        Watch(ModConfig.StorageEnabled); Watch(ModConfig.WorkbenchTweaksEnabled);
        Watch(ModConfig.AreaRepairEnabled); Watch(ModConfig.AreaSealEnabled);
        foreach (var guid in new[] { "restless.piles", "restless.plant" })
        {
            var sections = RestlessQoL.Api.SettingsPageApi.SettingsFor(guid);
            if (sections == null) continue;
            foreach (var section in sections)
                foreach (var option in section.Options)
                    if (option.Entry is ConfigEntry<bool> entry) Watch(entry);
        }
        void Watch<T>(ConfigEntry<T> entry)
        {
            EventHandler changed = (_, _) => _settingsDirty = true;
            entry.SettingChanged += changed;
            SettingUnsubscribe.Add(() => entry.SettingChanged -= changed);
        }
    }

    private static void PaintReachSettings()
    {
        Head("Global Reach");
        Bool("Use global reach", ModConfig.GlobalReachEnabled, !CoreEditable(), editable: CoreEditable);
        IntegerStep("Reach profile", "", ModConfig.GlobalReachProfile,
            () => CoreEditable() && ReachPolicy.Global, i => ReachProfile.At(i).Name);
        var copy = EcosystemCopy(ReachExplanation());
        SettingRefresh.Add(() =>
        {
            if (copy == null) return;
            var next = ReachExplanation();
            if (copy.text == next) return;
            copy.text = next;
            copy.GetComponent<LayoutElement>().preferredHeight = copy.preferredHeight + 12f;
            if (_body != null) LayoutRebuilder.MarkLayoutForRebuild(_body.GetComponent<UnityEngine.RectTransform>());
        });
    }

    private static string ReachExplanation()
    {
        var p = ReachPolicy.Profile;
        var prefix = ReachPolicy.Global ? p.Name + " is active." : "Custom distances are active. Turning Global Reach on applies " + p.Name + ":";
        var isolated = AddonFlag("restless.piles", "IsolateRange");
        var piles = !AddonFlag("restless.piles", "Enabled") ? " Piles is disabled or not installed."
            : isolated ? " Piles is isolated and keeps its own range." : " Piles follows the shared distance.";
        var harvest = !AddonFlag("restless.plant", "Enabled") ? " Harvesting is disabled or Plant is not installed."
            : " Crops and beehives share the smaller harvest radius; their switches still apply.";
        return prefix + "\nStorage and stations " + p.Shared + " m · repair " + p.Repair
            + " m · open-air seal " + p.Seal + " m · harvest " + p.Harvest + " m.\n"
            + "Storage searches for chests, drawers, supplies and production machines. Crafting, building and the storage browser search around you; kitchen orders around the preparation table; workshop orders around the board; pet feeding around the animal.\n"
            + "Station use and horizontal build coverage share the storage distance. Sealing uses the covering station's area, or the smaller open-air radius outside it. Repairs start at the repaired piece; harvest starts at the picked crop or hive.\n"
            + "Disabled features stay disabled: nearby chests " + State(ModConfig.StorageEnabled.Value)
            + ", station override " + State(ModConfig.WorkbenchTweaksEnabled.Value)
            + ", repair " + State(ModConfig.AreaRepairEnabled.Value) + ", seal " + State(ModConfig.AreaSealEnabled.Value) + "."
            + piles + harvest + "\nOnly loaded objects participate; permissions still apply. Crop spacing and field snapping stay personal. Cookbook, Workshop and Storage windows keep their local 5 m interaction guard; the pile window keeps 4 m. Profile changes apply immediately; turning Global Reach off restores your saved custom distances.";
    }

    private static string State(bool on) => on ? "on" : "off";
    private static bool AddonFlag(string guid, string key)
    {
        var sections = RestlessQoL.Api.SettingsPageApi.SettingsFor(guid);
        if (sections == null) return false;
        foreach (var section in sections)
            foreach (var option in section.Options)
                if (option.Entry.Definition.Key == key && option.Entry is ConfigEntry<bool> entry) return entry.Value;
        return false;
    }
}
