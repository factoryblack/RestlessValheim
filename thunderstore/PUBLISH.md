# Thunderstore 0.1.21

Signed off 28 Sep 2026. Live after this drop: Core 0.1.17, Plant 0.1.7, Piles 0.1.7, pack 0.1.21. Cook 0.1.9 and Drawers 0.1.1 unchanged. BepInExPack 5.4.2351. Jötunn stays 2.30.2.

Pins live in `versions.yaml`. After a Jötunn or package bump: `python scripts/sync-versions.py`.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.16 | **0.1.17** | Achievements stay available, lifetime character stats, UI consistency, dedicated tame pull, split-drop vacuum |
| RestlessPlant | 0.1.6 | **0.1.7** | Harvest radius, crop-only grid, spacing follows the crop, oversized grids skip untilled cells |
| RestlessPiles | 0.1.6 | **0.1.7** | A split stack vacuumed onto a pile keeps the thrown count |
| RestlessCook | 0.1.9 | 0.1.9 | No zip |
| RestlessDrawers | 0.1.1 | 0.1.1 | No zip |
| Restless Valheim pack | 0.1.20 | **0.1.21** | Core + Plant + Piles |

## What to tell players

**Restless Valheim 0.1.21**  
Core 0.1.17, Plant 0.1.7, Piles 0.1.7. Cook and Drawers unchanged.

**RestlessCore 0.1.17**  
Achievements are no longer blocked because the game is modded. The character page keeps the lifetime totals. A dedicated server can feed tames from a chest the player owns.

**RestlessPlant 0.1.7**  
Grid spacing follows the crop. An oversized grid plants only the cells on cultivated ground. Harvest takes every ripe player-grown crop in range. Needs Core 0.1.17.

**RestlessPiles 0.1.7**  
Throwing part of a stack back onto a pile adds the amount you threw. Needs Core 0.1.17.

## Tag order

Core first. Plant and Piles after that listing exists. Pack last.

```
git tag v0.1.17 && git push origin v0.1.17
git tag plant-v0.1.7 && git push origin plant-v0.1.7
git tag piles-v0.1.7 && git push origin piles-v0.1.7
git tag pack-v0.1.21 && git push origin pack-v0.1.21
```

Pack waits up to 600s for Core, Plant, and Piles. Cook 0.1.9 and Drawers 0.1.1 are already listed.
