# RestlessCook

Valheim 1.0 cooking graph for Restless. **Cook the haul, make meals, turn meals into feasts.**

Hard-depends on **RestlessCore**, **BepInExPack 5.4.2350**, and **Jötunn 2.30.0**. Everyone on the server needs this mod (minor version match).

v0.1 covers Meadows through Ashlands. Deep North waits.

## What it does

- Adds prepared meals that use cooked meat and fish, then feast boards assembled from those meals
- Rewrites existing protein recipes in place so they ask for the cooked cut instead of the raw one
- Reroutes Mushrooms Galore and the Ashlands Gourmet Bowl through Hidden Hills / Cinder sideboards so leftover vanilla produce still reaches a feast

Every prepared meal, custom or vanilla, still reaches at least one feast. Existing vanilla feasts stay valid sinks.

Recipes live in `cook.yaml` in the RestlessQoL repo. This package does not replace RestlessCore.
