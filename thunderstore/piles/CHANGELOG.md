# Changelog

Changes are listed newest first. **Unreleased** describes current source changes that are not assigned to a new tagged release yet.

## 0.2.1

- Refreshed the package page and added a guide to deposits, withdrawal and shared storage actions.
- Updated the package icon to the shared Restless ecosystem artwork, with a matching ring size, centred artwork and transparent background.
- Requires Core 0.2.2.

## 0.2.0

- Nearby piles appear in the Storekeeper's Table and can be withdrawn from there.
- Requires Core 0.2.0.

## 0.1.7

- Fixed thrown split-stack amounts when routed back into a pile.
- Requires Core 0.1.17.

## 0.1.6

- The separate range slider appears only when pile-range isolation is enabled.
- Requires Core 0.1.15.

## 0.1.5

- Added Piles controls to F8.
- Piles follow Core's search range unless their separate range is enabled.

## 0.1.4

- Fixed supported station interactions failing to find inputs or fuel stored only in a pile.

## 0.1.3

- Quick Stack and ground-item routing now respect the configured storage search range.

## 0.1.2

- Ground-item routing and Quick Stack prefer placed piles before chests.
- Fixed deposit accounting, ownership timing and merging withdrawn resources into matching bag stacks.
- Crafting/building can consume nearby pile stock, and sneak-use takes a stack directly.

## 0.1.1

- Removing or destroying a pile returns stored extras and its original build materials.

## 0.1.0

- Initial bulk storage for vanilla single-resource stacks/piles, including existing placed pieces.
- Added Take stack, Stack and Quick Stack integration, with clickable tray controls.
- Stored counts are not capped by an item's normal stack size.
- Added matching ground-item routing even into empty piles. Craft/build integration was not included in this initial release.
