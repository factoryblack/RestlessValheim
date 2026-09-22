# Expansion settings in F8

Plant and Piles register their existing BepInEx entries with Core. The module icon,
summary and package links remain; grouped controls appear directly on the module
page. There are no duplicate configuration files or new gameplay rules.

- Plant: 21 current entries, grouped into Planting, Harvesting, Your planting grid,
  and Your controls and hints. The obsolete GridSize migration entry is excluded.
- Piles: Enabled and deposit range.
- Cook and Drawers: explicit `No configurable settings.` when loaded.
- An older Plant/Piles build without registration prompts the player to update
  that expansion alongside Core, rather than falsely claiming it has no settings.

## Ownership and interaction

Gameplay entries use the expansion's existing Jotunn AdminOnly/synchronisation
policy. The renderer gates edits to the host or an admin, independently of Core's
optional LockConfiguration toggle. Personal grid sizes, spacing, snapping,
keybindings and regrowth hints remain editable locally.

Buttons, sliders and key capture recheck edit permission at write time. Values
received through SettingChanged queue a UI refresh on the next open-menu tick;
callbacks never manipulate Unity objects from a configuration event thread.
The page is not rebuilt for ordinary value changes or while dragging sliders.
Only registration/disposal rebuilds its structure. Permission changes refresh
interactability without rebuilding. Subscriptions are released on page changes,
close and GUI reset. Plant grid shortcuts do not run underneath F8/key capture.

Plant's prefab availability and placement flags are established during startup.
Those five controls are marked `(restart)` to avoid promising complete live
application. Other controls retain their existing runtime behaviour. No attempt
is made here to rebuild world prefabs or change the sync protocol.

## Reusable registration

Call after Config.Bind, on Unity's main thread. Retain/dispose the handle at plugin
shutdown. RegisterSettings supplements an existing SettingsPage descriptor; an
external mod should also register its own page if Core does not know its identity.
The original SettingsPage constructor and OpenSettings action remain compatible.

```csharp
_settings = SettingsPageApi.RegisterSettings(PluginGuid,
    new SettingsSection("Gameplay",
        new SettingsOption("Enabled", MyConfig.Enabled, hostControlled: true),
        new SettingsOption("Range", MyConfig.Range, hostControlled: true, unit: "m")),
    new SettingsSection("Your controls",
        new SettingsOption("Action key", MyConfig.Hotkey, hostControlled: false)));
```

Supported entries: bool, KeyboardShortcut, and ranged int/float. Numeric limits
come from the existing AcceptableValueRange. The optional requiresRestart flag
adds visible restart guidance. Section/option collections are copied; config
entries remain the original references. Duplicate registrations are rejected;
disposal is idempotent and invalidates the open page. Unsupported entry types are
rejected at registration rather than silently omitted.

## Validation and deployment

Install the matching Core, Plant and Piles DLLs from this build together: the new
addon registrations depend on the new Core API. No release versions are bumped
by this PR. Existing config values, defaults, paths and Jotunn metadata are unchanged.

Local checks cover C# parsing, all 23 current settings registered exactly once,
and host/local classifications matching their existing config metadata. The
repository PR workflow is the compilation gate against real Valheim assemblies.
Unity and host/client playtests are still required:

1. Open each expansion page with it loaded and absent. Check Cook/Drawers empty
   state and older Plant/Piles registration fallback.
2. As host, change pile range and Plant settings, reopen F8 and restart to verify
   persistence. Verify startup-marked settings after restarting game/server.
3. As non-admin client, gameplay controls must be disabled; personal controls
   remain editable. Core's LockConfiguration off must not unlock addon gameplay.
4. Change a setting from the host/config system while a client views its page;
   value and permission updates must appear without a page jump.
5. Drag float sliders; try integer grid limits 1/7; rebind grid shortcuts, cancel
   with Escape and switch pages during capture. No grid resizing behind F8.
6. Repeatedly open/close F8 and change module pages; confirm no duplicate callbacks,
   stale capture, growing subscriptions or gameplay hotkeys captured by the menu.
