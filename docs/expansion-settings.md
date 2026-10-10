# Expansion settings in F8

Plant and Piles register their existing BepInEx entries with Core. Cook, Workshop and Storage expose their personal interface toggles. Drawers has no configurable settings. Module pages retain their summary and package links; an older expansion without its expected registration prompts the player to update it alongside Core.

## Ownership and interaction

Gameplay entries retain the expansion's Jötunn AdminOnly/synchronisation policy. The renderer gates host-controlled edits to the host or an admin, independently of Core's optional LockConfiguration toggle. Personal planting grid sizes, spacing, snapping, keybindings, hints and interface preferences remain local.

Global Reach is opt-in and defaults off. It supplies effective reach values without overwriting saved custom ranges. Dependent sliders show their effective value and status, and become uneditable while their feature is disabled or Global Reach controls them. Turning Global Reach off restores use of the saved custom ranges. Personal planting snap distance stays independent. See the [player settings guide](player/core.md) for the preset table and feature scope.

Piles inherits Core resource reach unless isolation is enabled. An isolated pile radius applies to the browser, crafting and production pulls. Its custom range control is available when isolation is on and Piles is enabled. An isolated radius remains independent of Global Reach.

Buttons, sliders and key capture recheck permission and editability at write time. Configuration events queue refreshes on the next menu tick. Ordinary changes and slider dragging do not rebuild the page; registration/disposal changes its structure. Permission and dependency changes refresh interactability. Subscriptions are released on page changes, close and GUI reset. Plant grid shortcuts do not run underneath F8/key capture.

Plant's startup prefab/placement options carry restart guidance. Other entries retain their runtime behaviour. The obsolete GridSize migration entry is excluded from the UI but kept for config compatibility.

## Reusable registration

Call after Config.Bind on Unity's main thread, retain the handle, and dispose it at plugin shutdown. RegisterSettings supplements a SettingsPage descriptor; an external mod should register its page if Core does not know its identity.

```csharp
_settings = SettingsPageApi.RegisterSettings(PluginGuid,
    new SettingsSection("Gameplay",
        new SettingsOption("Enabled", MyConfig.Enabled, hostControlled: true),
        new SettingsOption("Range", MyConfig.Range, hostControlled: true, unit: "m")),
    new SettingsSection("Your controls",
        new SettingsOption("Action key", MyConfig.Hotkey, hostControlled: false)));
```

Supported entries are bool, KeyboardShortcut and ranged int/float. Numeric limits come from AcceptableValueRange. SettingsOption retains its original five- and six-argument constructor signatures for compiled consumers. The additive nine-argument overload supplies visibility, editability, effective-float and status callbacks. Sections/options are copied; config entries remain their original references. Duplicate registrations and unsupported types are rejected. Disposal is idempotent and invalidates the open page. The original SettingsPage constructor and OpenSettings action remain available.

## Validation and deployment

Use matching package versions from `versions.yaml`; updated reach-aware expansions require Core 0.3.0. Current build, regression and package checks are described in [building](maintainers/building.md). Native UI and host/client checks still need an in-game session:

1. Open each module page with its expansion loaded and absent. Check Drawers' empty state, interface toggles and older-registration guidance.
2. As host, change custom ranges, toggle Global Reach and cycle its presets. Effective values/status must update; custom values must survive toggling, reopening and restarting. Test startup-marked controls after restart.
3. As a non-admin client, gameplay controls stay disabled while personal controls remain editable. Turning Core LockConfiguration off must not unlock expansion gameplay.
4. Receive host/config changes while viewing a page. Values, status and permissions must update without a page jump; disabling a feature must disable its dependent controls.
5. Drag sliders, test fractional harvest/snap values and integer grid limits, then rebind/cancel shortcuts and switch pages during capture. No planting grid resizing should occur behind F8.
6. Repeatedly open/close F8 and change pages; check for duplicate callbacks, stale capture, growing subscriptions and gameplay hotkeys captured by the menu.
