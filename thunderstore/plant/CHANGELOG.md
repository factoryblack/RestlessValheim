# Changelog

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
