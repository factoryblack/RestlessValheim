# Install and update Restless

## Install with a mod manager

1. Select Valheim in **r2modman** or the **Thunderstore App** and create a profile.
2. Install [Restless Valheim](https://thunderstore.io/c/valheim/p/Restless/Restless_Valheim/) for the collection, or [RestlessCore](https://thunderstore.io/c/valheim/p/Restless/RestlessCore/) plus individual expansions.
3. Let the manager install BepInExPack, Jötunn and the required Core version.
4. Start the game using the manager's modded launch option.
5. Open **F8** or **Restless** in the pause menu to check settings.

The pack includes Core, Cook, Plant, Piles, Drawers, Storage and Workshop. Expansions require Core; they do not require every other expansion.

The old RestlessValheim and RestlessQOL listings are deprecated. They are not the current collection.

## Multiplayer

Use the same released plugin versions on all clients and the server. Install the plugins on a dedicated server as well as on players' profiles; a client-only installation is not the supported setup.

With configuration locking enabled, gameplay settings come from the host/server. Keybinds, HUD choices and local window toggles remain personal where appropriate. A locked setting in F8 is intentional.

## Update

Back up the world and characters before changing a modded setup. Close the game, update the selected packages and their dependencies, and update the server before reconnecting. Read each package's changelog for required dependency changes.

Adding an expansion can add items or build pieces to a world. Removing it does not convert those pieces and items into vanilla equivalents. Test removal on a copy of the save.

## Manual installation

Install the required BepInExPack and Jötunn versions listed on the package page. Extract each complete package into its own folder under `BepInEx/plugins`. Keep shipped mesh and texture files with their plugin; copying only a DLL can leave custom models missing.

A mod manager handles this layout for you.

## Existing mod profiles

Core overlaps storage automation, inventory-slot and HUD mods. Avoid installing duplicate implementations such as AzuCraftyBoxes, AzuAutoStore, AzuAreaRepair, AzuWorkbenchTweaks, EquipmentAndQuickSlots, ValheimPlus, Auga, BetterUI or MinimalUI alongside those Core systems. This is an overlap warning, not a claim that every possible combination has been tested.

If something behaves unexpectedly, reproduce it in a clean Restless profile and attach the original profile's mod list to your report.

[Back to guides](README.md) · [Troubleshooting](troubleshooting.md)

