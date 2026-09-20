# RestlessPlant

Berry bushes and forage on the cultivator, the way carrots already work.

Plant a raspberry bush. Plant thistle. Plant a square of mushrooms if you have the stock. Pick one player-grown plant and its neighbours come with it. Hover a bare bush and the look prompt tells you when the fruit is coming back.

Needs **RestlessCore**, plus BepInEx and Jötunn. Everyone on the server should be on the same minor version.

Vanilla carrots, flax, and the rest stay as they are. The square and the harvest pass apply to them too.

## In the field

The cultivator gains raspberry, blueberry, cloudberry, mushrooms, yellow mushrooms, thistle, dandelion, magecap, jotun puffs, smoke puff, and fiddlehead. Each costs one of the thing it grows. If a prefab is missing in your build, that crop is skipped so the rest still load.

- **`[` / `]`** shrinks or grows the planting square (1×1 up to 7×7, odd sizes). Place once; every cell you can afford goes down.
- Picking a **player-grown** plant also picks matching neighbours. Wild meadow raspberries stay wild.
- Carrots and other one-shot crops **replant** if you still have the seed.
- Bushes and mushrooms keep their fruit cycle. They are not ripped up on pick. Hover the bare plant for the wait.

Host-locked extras, harvest, and replant live in `restless.plant.cfg`. Grid size is yours.

Install the **Restless Valheim** modpack, or Core then this. This package does not replace RestlessCore.
