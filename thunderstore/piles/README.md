# RestlessPiles

Vanilla **wood stacks, stone piles, and the other resource piles** become uncapped single-item stores. **E opens them. Take stack takes one bag stack. ` dumps matching items in.**

Hard-depends on **RestlessCore**, **BepInExPack 5.4.2350**, and **Jötunn 2.30.1**. Everyone on the server needs this mod (minor version match).

This is not a chest and not a ground drop. The hammer piece stays where you built it. The visual stack is not a bag you can drag.

Existing piles in a world keep working. The mod dresses the vanilla prefabs (`stone_pile`, `wood_stack`, and the rest). You do not have to tear them down and rebuild.

## How it works

Each pile only holds the item it is made of (a stone pile only takes stone). Storage sits on the piece ZDO, so craft-from-chests still ignores it. Vacuum and ` treat the pile as that item even when stored is 0.

- **Take stack** moves one bag-shaped bite: fill one matching partial, or start one new max stack. Never a second cell. Never fill the bag.
- **Stack** dumps every matching bag stack into this pile (hotbar, extra slots, and locked cells stay).
- **`** (Core quick stack) dumps matching bag stacks into nearby piles first, then into chests the way Core already does.
- **Vacuum** pulls matching ground drops into nearby piles (including empty / 0 stored), then into chests that already hold that item.
- Hammer-remove or smash spills stored extras and the piece's build cost as bag-sized drops.

Wood, finewood, core wood, yggdrasil, ashwood, stone, coal, black marble, grausten, bones, skulls, and the coin stacks/piles are included — anything whose prefab name ends in `_stack` or `_pile` and costs one stackable resource.

Host-locked toggle and range live in the BepInEx config.

This package does not replace RestlessCore. Install the **Restless Valheim** modpack, or Core then this.
