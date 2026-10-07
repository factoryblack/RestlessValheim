# Changelog

Changes are listed newest first. **Unreleased** describes current source changes that are not assigned to a new tagged release yet.

## 0.2.5

- A storage take counts the stacks that landed in the bag, including a new stack and gear.

## 0.2.4

- A kitchen collects oven output only from an oven that kitchen is running.

## 0.2.3

- Crafting and building count a chest ingredient only after it has been removed.
- An open chest transfer stays reserved when the chest changes owner, and the same request is not taken twice.
- Restock skips the hotbar, equipment, quick slots and locked cells, and adds items only after the chest confirms the pull.
- F8 still lists Storage and Workshop when those plugins are not installed.
- Shared screens use one UI kit, with checked material sizes and a shared texture cache.

## 0.2.2

- Refreshed package information and player guides, with screenshot areas ready for new captures.
- Wood sealing now covers the build range of stations covering the piece, with the configured radius used outside station coverage.
- The Seal hint and Alt-click appear only while the hammer is out.
- Updated the Restless ecosystem icons in the F8 menu and package listing, with matching ring size, centred artwork and transparent backgrounds.

## 0.2.1

- Alt-click with the hammer seals wood while ordinary clicks still place or repair.
- Improved shared cooking-card frames so wider layouts retain their corner details.

## 0.2.0

- Added resin-based rain protection for wooden pieces, with a host-controlled toggle and radius.
- Deaths with nothing to recover no longer leave a recovery pin.
- Quick-slot hotkeys no longer activate while typing.
- Updated the F8 Storage page emblem.

## 0.1.18

- Added the shared storage browser for the Storekeeper's Table, with source-owned withdrawal and rollback for unanswered transfers.

## 0.1.17

- Fixed dedicated-server tame feeding from eligible player-owned chests.
- Fixed split-stack ground-item routing losing the original thrown amount.
- Improved compendium scrolling, live settings refresh and shared reader scrolling.
- Improved narrow tooltips, hover text sizing and scrolling crafting requirements.
- Updated achievement handling so modded/cheated marks do not block achievements when the host setting is enabled.
- Expanded the Character page with lifetime records, activity, streaks and building records.

## 0.1.16

- Reworked crafting into a coherent paper layout with a station header, recipe list, scrolling details and readable material counts.
- Station requirements now use the same presentation as materials, including level and unmet-state hints.
- Fixed station, category and preview information following the wrong selected recipe.
- Added shared scrolling for recipe and loadout readers.
- Preserved selected Craft/Upgrade colours and handcraft station information.
- Restored player attacks on owned tames, including the butcher knife; tame and ballista protection remains.
- Building from storage still requires the correct nearby station.
- Updated BepInExPack to 5.4.2351; Jötunn remains 2.30.2.

## 0.1.15

- Improved loadout totals with a paper panel, actual worn-armour sources, combined attack bonuses and scrollable explanations.
- Reduced unnecessary layout diagnostics outside the debug overlay.
- Expansion settings can hide dependent controls until their parent option is enabled.
- Maintained compatibility with the published Plant 0.1.5 and Piles 0.1.5 builds.

## 0.1.14

- Added Plant and Piles controls to their F8 pages; Cook and Drawers have no configurable settings in this release.
- Piles use Core's search range unless their separate range is enabled.
- Fixed nearby-chest tame feeding on dedicated hosts.

## 0.1.13

- Turning inventory styling off restores native graphics, text and visibility, including recipe and requirement rows.
- Crafting shortage colours refresh correctly, and inspect shares tooltip sections while retaining scroll position.
- Split-stack and variant dialogs use Restless styling with native input behaviour.
- Added an F8 ecosystem overview with installed module/version information.
- Added a loadout totals overlay with stat sources.
- Added optional day/time information and station timers.
- Jötunn is pinned to 2.30.2, with package/dependency versions managed centrally.

## 0.1.12

- Reduced unnecessary idle refreshes in the hammer menu and inventory while preserving live selection and material updates.

## 0.1.11

- Styled hammer, hoe and serving-tray menus with shared piece cards and native availability feedback.
- Added a compact selected-piece detail card with description, materials and station requirements.
- Improved fit and scrolling for larger build recipes.
- Kept native scrollbars, search caret, placeholder and selection behaviour.

## 0.1.10

- Fixed pause-menu labels disappearing from the Restless paper layout.

## 0.1.9

- Improved wind information placement around the minimap.
- Fixed crafting panels becoming too dark during refresh.
- Updated compendium, trophy and achievement screens, pause/confirmation menus and map controls to the shared interface style.

## 0.1.8

- Added a clearer hover health meter for placed pieces.

## 0.1.7

- Moved the wind arrow to a separate plate beside the minimap.

## 0.1.6

- Improved resource-meter fill colours.
- Building from storage no longer unlocks unknown pieces or feast boards.
- Fixed partial ground-item deposits, quality-aware restocking and unanswered storage requests.

## 0.1.5

- Aligned resource bars around the hotbar.
- Allowed Piles deposits to protect extra slots.

## 0.1.4

- Added host-controlled cheated-item mark clearing.
- Kept native crafting backgrounds when replacement art is unavailable.
- Improved minimap player markers.
- Crafting from storage respects station upgrade levels.
- Updated Jötunn to 2.30.1; BepInExPack remains 5.4.2350.

## 0.1.3

- Reduced repeated resizing of inventory, container and skills frames.
- Improved native layout restoration and reduced idle slot refreshes.

## 0.1.2

- Recipe quantities include nearby storage while respecting Leave one.
- Adjusted adrenaline/health and eitr/stamina layout.

## 0.1.1

- Updated the listing to describe Core itself.

## 0.1.0

- Initial nearby-storage crafting, building, station inputs, tame feeding and ground-item routing.
- Quick Stack, Restock, cell locks, worn slots and three quick slots.
- Area repair, wider station range, equipment repair, stack settings and host-controlled configuration.
- Swimming equipment, crossbow state, tame protection, axe combo and death-pin improvements.
- Persistent fires, extended terrain limits, floating drops and network settings.
- Initial Restless HUD, inventory and map presentation.
