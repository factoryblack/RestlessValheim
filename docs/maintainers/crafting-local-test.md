# Crafting window local test

Branch: `feature/crafting-window-local`. This is a local acceptance build, with
the existing version pins. Do not publish its package archives. The prior release
candidate remains on `feature/global-reach-120` in the original source checkout.

## Open it

Use the direct Steam Valheim installation containing this test build. An r2modman
profile may load different plugins. Open a workbench, forge or another crafting
station to enter Crafting automatically. For hand crafting, open your inventory
and select **Open Crafting**.

The personal switch is **F8 → Interface → Crafting window (local test)**. It defaults
off for new installations; the local test installer enables it on this machine.
Turning it off restores the existing crafting panel. It is independent of the
inventory styling switch and does not change host range settings.

## What is implemented

- **Craft:** native known recipes, search and category filters, item preview,
  native material counts and eligibility, variant cycling, crafting progress and
  cancellation. The game owns resource consumption and item creation.
- **Upgrade:** individual owned equipment targets, including separate copies of
  the same item; current and next-quality tooltips; exact-target revalidation
  before native execution. Previews clone item data.
- **Stations:** actual connected extensions and station level; nearby extensions
  which are not contributing; known extensions available to build, with costs.
  With an appropriate building tool equipped, select an extension and return to
  normal game placement. The game checks build-menu membership and placement.
  A build choice does not promise an extra level or list future recipe unlocks.
- **Inventory:** the first ten items as a compact quick-access strip. Selecting
  one opens its upgrade entry where eligible. Full inventory returns to ordinary
  inventory management; the strip does not support dragging or transfers.
- **Shared presentation:** Crafting, Cookbook and Workshop use one paper window
  frame, header, close control, fitting behaviour and selected tab treatment.
  Crafting has a smaller layout for small canvases. Cookbook's Kitchen tab now
  has selectable stations, capacity, fuel, fire, blocker and distance details.

Escape closes the window and inventory. During a craft the secondary action
cancels it. F8 closes Crafting before opening settings. Leaving station reach,
losing access, dying, or disabling the feature restores the native presentation.

## Acceptance checks in game

1. Open inventory without a station, then Crafting. Search and switch categories.
   Craft an item, watch progress, then cancel another craft. Confirm resources and
   results agree with native behaviour.
2. Carry two copies of the same equipment at different qualities. Upgrade the
   chosen copy; confirm the other is unchanged and previews consume nothing.
   Test missing resources, full inventory and a maximum-quality item.
3. Open several station types. Check connected levels, an extension outside its
   connection distance, a duplicate extension, and a known unbuilt extension.
   Equip its building tool, select it, and place it normally.
4. Use full inventory, reopen Crafting, open a chest, press Escape, open F8,
   toggle the feature off/on, and move out of station reach. Confirm no hidden
   input capture, overlapping windows or unwanted menu opening.
5. Test a small resolution, a large display, UI scaling, long item names, long
   tooltips, a long recipe list and controller navigation/scrolling.
6. Open Cookbook's Kitchen tab, change selected station, search, then return to
   recipes/orders/pantry. Verify stale ingredient cards and quantity controls
   disappear in station view. Exercise Workshop's machines and queues too.
7. Multiplayer ownership and second-player behaviour remain pending until
   another player is available. No multiplayer validation is claimed.

## Verification and rollback

Automated checks pass: ten existing suites, twelve production crafting bridge
checks, nine added native crafting API contracts, five release tooling tests,
seven native mod builds, the external UI kit consumer, documentation/material
checks and eight package archives. Builds have existing nullable/reference and
unavailable NuGet audit warnings. In-game visual and interaction acceptance is
still pending; successful compilation is not a gameplay test.

Installation records, file hashes and previous files are under
`artifacts/install-backups/`. The local installation receipt records the exact
backup directory. Disable the F8 switch for immediate UI rollback. For a complete
binary rollback, close Valheim and restore the backed-up plugin files and Core
configuration according to that receipt; keep the saved worlds and characters.
The original source branch and release archives are separate from this test.
