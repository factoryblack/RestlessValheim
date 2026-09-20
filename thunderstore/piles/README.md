# RestlessPiles

The wood stack and the stone pile finally hold what you actually cut.

**E** opens a tray. **Take stack** puts one bag-sized bite in your inventory. **`** dumps every matching stack you are carrying. Smash or hammer-remove the pile and the extras spill as bag-sized drops, along with the wood or stone you spent to place it.

Needs **RestlessCore**, plus BepInEx and Jötunn. Everyone on the server should be on the same minor version.

This is still the hammer piece you already know — not a chest, not a ground drop. Existing piles in a save keep working; you do not have to tear them down.

## Using one

A stone pile only takes stone. A wood stack only takes wood. Empty is fine: the piece still knows what it is, so vacuum and `` ` `` will fill it.

- **Take stack** fills one matching partial in the bag, or starts one new max stack. It never dumps the whole pile into every free slot.
- **Stack** on the tray (or `` ` `` nearby) puts matching bag stacks into the pile. Hotbar, extra slots, and locked cells stay put.
- **Vacuum** prefers a matching pile, then a chest that already has that item.
- Crafting and building can pull from a nearby pile after they have checked chests.

Wood, finewood, core wood, yggdrasil, ashwood, stone, coal, black marble, grausten, bones, skulls, and the coin piles are included — anything that is a `_stack` or `_pile` and costs one stackable resource.

Host-locked toggle and range live in the BepInEx config.

Install the **Restless Valheim** modpack, or Core then this. This package does not replace RestlessCore.
