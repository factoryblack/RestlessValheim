# Changelog

## 0.1.8

- A picked mushroom shrinks to a sprout and grows back. On a bush only the berries do that; the bush stays full size
- Field snap lines up with the crop itself
- Each extra grid cell pays for its own seed
- Area harvest includes ripe farm crops you planted
- Grow-anywhere no longer leaves the plant unable to grow in its biome
- A bird nest keeps its own text and feathers, and does not pull nearby hives

## 0.1.7

- Grid spacing follows the crop. Minimum spacing 0 uses the room that plant needs to grow
- An oversized grid plants the cells that sit on cultivated ground and can grow. The rest stay red and are skipped
- Harvest range picks every ripe player-grown crop nearby, not only the same kind, and only searches that radius
- A wide cultivator grid stamps crops. The till tool still cultivates one patch
- A failed grow check logs once and still puts the ghost back

## 0.1.6

- Snap-to-field aligns to the crop itself. A location parent was throwing the cultivator ghost off the bed

## 0.1.5

- Planting, harvest, grid and personal controls sit on the F8 Plant page (PR 19)

## 0.1.4

- Grid extras sit on the heightmap instead of a catch-all ray, so a square no longer floats
- Extra cells that cannot grow (untilled, wrong biome, no sun, no space) paint red and are skipped
- `[` `]` set width and `-` `=` set depth, so a bed can be 1×5
- F10 snaps the ghost onto a nearby plant's spacing
- Grow-anywhere, decorative trees/shrubs/vines, extra saplings, and beehive bulk harvest are settings (off for grow-anywhere)
- Hover a growing sapling for the wait; hover a one-shot crop to see what will replant
- Lingonberry bushes and blue mushrooms join the cultivator when those prefabs exist

## 0.1.3

- Mushrooms, thistle, and the other forage crops show a placement ghost again
- Berry bushes and forage grow their fruit back (about four hours if vanilla left the timer empty)
- Hover a bare plant to see how long until it fruits
- Replant still only runs for carrots and other one-shot crops, not bushes

## 0.1.2

- Fix cultivator crops that placed with no visible model (mushrooms and other forage). Cloned pieces now force every renderer active and strip any LODGroup, instead of inheriting whatever visibility state the vanilla pickable prefab happened to ship with.

## 0.1.1

- Bulk harvest only treats Restless_* clones or pieces with a creator as player-grown (vanilla bushes stayed “ours”)
- One-shot crops replant the picked piece, including bulk neighbours; cultivator selection is remembered at 1×1
- Shrinking the plant grid destroys leftover ghosts instead of hiding them

## 0.1.0

- Cultivator plants berry bushes, mushrooms, thistle, dandelion, and later-biome forage (skips missing prefabs)
- Odd planting square (`[` / `]`) places every cell you can afford
- Bulk harvest of player-grown matching plants
- One-shot crops replant the last selected cultivator piece
- Cultivator crop clones now take the vanilla item icon so Jötunn accepts them
- Grid extras are visual-only (no ZNetView). Default size is 1×1; [ ] grows the square
