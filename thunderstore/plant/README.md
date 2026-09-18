# RestlessPlant

**Berry bushes and forage on the cultivator**, plus a planting square, bulk harvest, and replant. PlantEverything content and PlantEasily feel in one Restless addon.

Hard-depends on **RestlessCore**, **BepInExPack 5.4.2350**, and **Jötunn 2.30.1**. Everyone on the server needs this mod (minor version match).

## How it works

The cultivator gains raspberry, blueberry, cloudberry, mushrooms, thistle, dandelion, magecap, jotun puffs, smoke puff, and fiddlehead. Each costs one of the item it grows. Missing prefabs are skipped so Deep North / Ashlands holes do not break load.

Vanilla carrots, flax, and the rest stay as they are. The grid and harvest also apply to them.

- **`[` / `]`** shrinks or grows the planting square (starts at 1×1, up to 7×7, odd sizes).
- Place once to plant the whole square if you can pay for each cell.
- Picking a **player-grown** plant also picks matching neighbours in range. Wild meadow raspberries stay wild.
- One-shot crops (carrots and the like) **replant** the last cultivator piece you had selected, if you still have the seed.
- Bushes and mushrooms keep their vanilla respawn; they are not destroyed on pick, so they are not replanted.

Host-locked extras / harvest / replant live in `restless.plant.cfg`. Grid size is client-local.

This package does not replace RestlessCore. Install the **Restless Valheim** modpack, or Core then this.
