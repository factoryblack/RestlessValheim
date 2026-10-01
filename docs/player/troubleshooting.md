# Troubleshooting Restless

## Start with the installed versions

Open F8 and check the loaded Restless modules. Compare your mod-manager profile with the server's versions and required dependencies. Use the same released plugin versions on both sides.

The guides follow current main. An older package may not include a feature shown here. A README update does not update your installed DLL.

## A recipe or piece is missing

- Discover the required ingredients through ordinary play.
- Check the required station and its upgrade level.
- Check whether the relevant expansion is installed and enabled.
- For Plant, confirm the crop prefab exists in your game version and the host has enabled its category.
- Do not expect Cook's future Deep North dishes, a Core skill tree, or Works forge recipes that are not implemented.

## Nearby storage is not contributing

Check range, container access, the host's storage toggles and Leave one. A final reserved chest item can intentionally make a requirement look short.

Quick Stack needs a matching chest stack; empty chests are ignored. Restock fills stacks you already carry. Hotbar, equipment/quick slots and locked cells are protected.

Piles normally follow Core range; check their isolated range only if enabled.

## Kitchen or workshop orders are waiting

Check required station, upgrades, fire/fuel, inputs and free capacity. The UI does not create missing stations.

Cook chains prepared ingredients through its order steps. Works currently requires separate orders for dependencies. Inspect the actual waiting requirement before creating duplicate jobs.

If another player used the station, close and reopen its window to check ownership. Make output on Works can only be collected by the ordering player.

## Taking items did not fill my inventory

Leave space and check stack/variant compatibility. Storage returns overflow to its source; Works leaves output that will not fit on the board. Check the actual remaining stock rather than assuming it was lost.

For a repeatable discrepancy, stop repeating the transaction and include the before/after quantities in your report.

## Custom models are missing

Keep all shipped assets with their plugin. A DLL-only manual install can omit meshes and textures. Reinstall the complete package through the mod manager, then check the log for missing-asset messages.

## Layout, input or overlapping UI

Check F8 screen/HUD preferences and reproduce with a clean Restless profile. Other inventory, equipment and HUD mods can overlap Core's systems. Include resolution, UI scale and a screenshot showing the full screen.

Some controller navigation remains incomplete. Describe the input device and exact action when reporting it.

## Report a problem

Use [GitHub Issues](https://github.com/factoryblack/RestlessValheim/issues/new/choose). Include:

- Package versions and the full mod list.
- Game version, single-player/listen-host/dedicated-server setup, and which player performed the action.
- Exact steps, expected result and actual result.
- Relevant host settings, item/recipe/station names and before/after quantities.
- Screenshot/video and relevant BepInEx log excerpt.

Remove server passwords, private addresses and personal information before attaching logs. A short reproducible example is more useful than “it doesn't work.”

[All guides](README.md)

