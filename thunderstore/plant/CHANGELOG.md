# Changelog

Changes are listed newest first. **Unreleased** describes current source changes that are not assigned to a new tagged release yet.

## 0.2.3

- Normal grown crops resolve their original sapling for replanting, including fields planted before this update; saved sapling identity supports custom crops.
- Replanting pays the matching seed cost and leaves regrowing forage on its normal cycle.
- Grid previews cache renderer and collider lookups. Pickup history prunes vanished drops and clears between worlds.

## 0.2.2

- Ivy and ashvine use the game's own plant check before the seed is spent. A failed check leaves the seed in the bag, and every player sees the owner's sapling.
- Switching cultivator crops no longer logs a renderer error for each planting ghost.
- Extra saplings plant beech, pine, fir, birch, and oak.
- Requires Core 0.2.4.

## 0.2.1

- Berry-bush hover names now identify their own crop instead of displaying generic Pickable text.
- The cultivator can remove a planted crop, including mushrooms.
- Refreshed planting, harvest and regrowth guidance.
- Updated the package icon to the shared Restless ecosystem artwork, with a matching ring size, centred artwork and transparent background.
- Requires Core 0.2.2.

## 0.2.0

- Picked mushrooms shrink and regrow; bushes retain their foliage while berries regrow.
- Improved field alignment and made every additional grid cell pay its own planting cost.
- Area harvest includes ripe planted farm crops.
- Fixed grow-anywhere plants incorrectly retaining a blocked-biome state.
- Bird nests retain their own feather interaction and do not trigger nearby hive harvesting.

## 0.1.7

- Grid spacing follows each crop's needs; zero minimum spacing uses its growth spacing.
- Large previews plant valid cells and skip invalid ones.
- Area harvest picks ripe player-grown crops of different types within its radius.
- Crop-grid size no longer expands the till tool.
- Improved failed-placement feedback and stability.

## 0.1.6

- Fixed field alignment being offset by a plant's parent location.

## 0.1.5

- Added Plant settings and personal controls to its F8 page.

## 0.1.4

- Extra grid cells follow terrain height and show red when they cannot grow.
- Added independent row/column controls and F10 field alignment.
- Added configurable saplings, decorative plants and grow-anywhere, plus bulk beehive harvest.
- Added remaining-time/replant hints and available lingonberry/blue-mushroom entries.

## 0.1.3

- Fixed invisible forage placement previews.
- Bushes and forage regrow their resources and show remaining-time hints.
- Restricted replanting to one-shot crops rather than bushes.

## 0.1.2

- Fixed cultivator forage appearing invisible after placement.

## 0.1.1

- Area harvest distinguishes player-grown crops from wild ones.
- Improved one-shot replanting and remembered cultivator selection.
- Shrinking the planting grid removes unused previews.

## 0.1.0

- Initial cultivator berries, mushrooms and available forage.
- Added affordable grid planting, player-grown area harvest and replanting.
- Used native item icons and local planting previews.
