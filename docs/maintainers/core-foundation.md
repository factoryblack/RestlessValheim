# Core foundation configuration matrix

Core remains required by every addon. This policy does not copy services into addons or create a separate package. Core 0.3.2 adds one startup setting in `BepInEx/config/restless.core.cfg`:

```ini
[Core]
OptionalFeatures = false
```

Default is `true`, preserving existing feature defaults. The value is captured before module registration. Changing it in F8 or on disk requires a restart. It is deliberately local startup policy, not a server-synchronised live toggle: configure each client and server. Saved per-feature values are not overwritten. Addons retain their own independent feature settings.

| Configuration | Core optional hooks and ticks | Native presentation | Addons |
| --- | --- | --- | --- |
| Core, `true` | Existing individual settings apply | Existing selected Core treatments | Available |
| Core, `false` | All 40 optional modules skipped, including `OnLoaded` subscriptions/manual patches | Core does not replace native HUD, inventory, map, prompts, tooltips or menus | Shared foundation available |
| Core + Drawers, `false` | Same as above | Native container controls and presentation | Drawers cabinets enabled; no drawer-specific switch |
| Core + other addons, `false` | Same as above | Native Core presentation; explicitly installed addons may open their own UI | Addon settings govern addon behaviour |

## Complete module boundary

Every registered module inherits `FeatureModule`. `TryLoad` checks the startup policy before constructing Harmony, evaluating patch types or calling `OnLoaded`. Both plugin update paths call `TickConfigured`; a suppressed module cannot create UI or change behaviour from `Tick`. Normal mode preserves existing live per-feature tick checks, including enabling quick stack/restock after startup. New modules are optional unless explicitly marked as foundation.

| Optional modules, all suppressed when false | Effects excluded |
| --- | --- |
| CraftFromStorage, BuildFromStorage, QuickStack, Restock, PetPantry, GroundVacuum, StationPull | Nearby crafting/building, stacking, restocking, tame feeding, world-item vacuum and production/fuel pulls |
| AreaRepair, AreaSeal, WorkbenchTweaks | Area actions and station-radius changes |
| DeathPins, SwimWield, CrossbowState, FriendlyFire, AxeCombo, AutoRepair | Death, equipment, combat and repair changes |
| StackSizes, HonestItems, EternalFire, DigDeeper, NetworkUncap, FloatingItems | Stack multipliers, cheated/achievement changes, fires, terrain, network transpiler/Steam configuration and buoyancy |
| MenuScreen, BuffList, MapChrome, MapScreen, MapDots, Notices, Hotbar | Menu/HUD/map styling, forced map visibility, custom notices and hotbar controller decoration |
| InventoryScreen, CraftingInterface, StatSheet, ItemTooltip, ExtraSlots, SlotLock | Inventory dressing, replacement crafting, stat sheet, tooltips, extra equipment/quick slots, controller remapping and middle-click locks |
| Vitals, EquipHint, BuildMenu, ActionHints, LookHints | Vitals, build panels, prompts and station-time hover text |

## Retained foundation and side-effect audit

| Service | Foundation behaviour |
| --- | --- |
| StorageSync | Registers permission/owner-checked transfer RPCs on containers and settles existing transactions. Does not initiate nearby crafting, stacking, vacuum or other automation. Explicit addon requests can use the shared service. |
| Ledger | Reads native lifetime statistics and saved extra counters. Counters are written only when explicitly requested by a caller; no replacement character page is shown. |
| SettingsUi | F8 deliberately opens the shared settings/addon pages. No native pause-menu patches or added button. Optional Core tabs are hidden while the startup mode is off. |
| InventoryRecovery | Only in foundation mode, two `Player.Load` hooks temporarily make room for previously saved extra-slot items, reuse the existing legacy importer, then return items to normal bag cells and restore bag height. Overflow drops at the player when the bag is full. It installs none of ExtraSlots' UI/input/equipment/capacity patches. |
| Configuration/Jötunn | Bind preferences without overwriting them. `ApplyJotunnDebug` returns before writing Jötunn settings in foundation mode. Core does not reset independently configured Jötunn or other-mod overlays. |
| Shared APIs/assets | Remain available on request: UI kit, settings-page registration, tooltip contributions, storage window, item identity, transfer authority and ledger. Embedded images load lazily; no eager UI warm-up in foundation mode. |

`SlotLock.Held`, `ExtraSlots.HoldsPlayerStats` and `ReachPolicy.Global` also respect the startup policy so addon calls cannot revive optional Core state. Addon-owned storage ranges and permission-checked transfer services remain usable.

Native `Inventory.Load` reads saved item coordinates through `AddItem` before `Player.Load` reads custom data. The narrow recovery hooks therefore reserve the former extra rows before the load and shrink afterwards; a postfix-only repair could lose items before recovery. Empty extra slots before switching when possible. Disabling future features does not undo past world changes or split already enlarged stacks.

## Verification and acceptance

`CoreFoundation.Tests` executes production startup/lifecycle code with a substituted Harmony boundary. It asserts suppressed modules cannot evaluate patch types, construct Harmony, install patches, run `OnLoaded` or tick, and checks wiring of the complete foundation boundary, config, menu/debug guards and required Drawers dependency. Controller and native API regressions remain in CI. Release verification checks the actual downloaded DLL/config policy and loader/package dependencies, not only source manifests.

This proves code/configuration and package contracts. Unity-rendered vanilla parity, character migration/overflow, physical controller use and multiplayer behaviour remain untested in-game. BepInEx/Jötunn modded flags and network compatibility requirements remain; this is a vanilla gameplay/presentation configuration of a modded installation.
