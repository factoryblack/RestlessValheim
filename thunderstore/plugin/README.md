# RestlessCore

One Valheim 1.0 QoL plugin for Restless. Isolated Harmony modules. Gameplay config is **host-locked** (Jötunn). Open **F8** in-game, or Restless from the pause menu.

Requires **BepInExPack 5.4.2350** and **Jötunn 2.30.0**.

This is not the old 60-mod RestlessQOL pack.

## Storage and stations

- Nearby chests count toward craft and build
- Quick stack (`` ` ``) / restock (`Shift+`` ` ``) — matching stacks only
- Smelters, kilns, cooking stations, and fermenters pull from those chests
- Hungry tames eat matching food from those chests
- Ground piles vacuum into chests that already have that item
- Middle-click lock on bag slots (quick stack and restock skip them)

## Building and player

- Hammer repairs a radius; stations use a wider range
- Using a station repairs what that station can repair
- Death pins clear when the tomb is emptied
- Tools stay equipped while swimming
- No tame-on-tame / player-on-tame friendly fire (player PvP stays vanilla)
- Axe combo while chopping; a loaded crossbow survives unequip
- Campfires and hearths stay lit; held torches do not burn down
- Hoe dig/raise cap is 20 m
- Most drops float (nails still sink)
- Stack-size multiplier (default 2×; gear stays 1)
- Extra worn slots plus Z / X / C quick slots (do **not** also install EquipmentAndQuickSlots)

## Interface (work in progress)

HUD, inventory, tooltips, crafting, map chrome, and settings are **in this DLL**, not a sidecar. They are mid-migration: some surfaces use the Restless kit, others are still vanilla or a mix of the two. Expect unfinished edges, especially Tab, inspect, and crafting.

F8 toggles the Restless pieces. Vanilla Settings is unchanged.

## Do not install beside this

ValheimPlus, BetterUI, MinimalUI, Auga, AzuCraftyBoxes, AzuAutoStore, AzuAreaRepair, AzuWorkbenchTweaks, EquipmentAndQuickSlots.

Optional **MyLittleUI** is still a separate product if you want timers / multicraft / chest names. It is not this plugin.
