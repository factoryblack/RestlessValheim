# RestlessPlant

Berry bushes and forage on the cultivator, the way carrots already work.

Plant a raspberry bush. Plant thistle. Plant a square of mushrooms if you have the stock. Pick one player-grown plant and its neighbours come with it. Hover a bare bush and the look prompt tells you when the fruit is coming back.

Needs **RestlessCore**, plus BepInEx and Jötunn. Everyone on the server should be on the same minor version.

Vanilla carrots, flax, and the rest stay as they are. The square and the harvest pass apply to them too.

## In the field

The cultivator gains raspberry, blueberry, cloudberry, lingonberry, mushrooms (including yellow and blue), thistle, dandelion, magecap, jotun puffs, smoke puff, and fiddlehead. Each costs one of the thing it grows. Optional extras add ancient / ygga / autumn birch / ashwood saplings, plus small trees, shrubs, vines, and stick/stone/flint. If a prefab is missing in your build, that crop is skipped so the rest still load.

- **`[` / `]`** sets how many plants wide. **`-` / `=`** sets how deep. Place once; every cell that can grow and that you can afford goes down.
- **F10** snaps the ghost onto a nearby plant so a new row lines up with the field.
- Extra cells snap to the soil. A cell that cannot grow (untilled, wrong biome, no sun, no room) shows red and is left empty, unless **Grow anywhere** is on.
- Picking a **player-grown** plant also picks matching neighbours. Using a hive can take neighbouring hives too. Wild meadow raspberries stay wild.
- Carrots and other one-shot crops **replant** if you still have the seed. Hover a ripe one-shot to see what goes back in.
- Bushes and mushrooms keep their fruit cycle. They are not ripped up on pick. Hover the bare plant for the wait, or a sapling still growing.

Host-locked extras, grow-anywhere, harvest, and replant live in `restless.plant.cfg`. Grid size and snap are yours.

Install the **Restless Valheim** modpack, or Core then this. This package does not replace RestlessCore.
