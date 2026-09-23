# Thunderstore 0.1.19

Signed off 23 Sep 2026. Live after this drop: Core 0.1.15, Piles 0.1.6, Drawers 0.1.1, pack 0.1.19. Cook 0.1.9 and Plant 0.1.5 unchanged.

Pins live in `versions.yaml`. After a Jötunn or package bump: `python scripts/sync-versions.py`.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.14 | **0.1.15** | Loadout totals, debug-only layout dumps, hideable expansion rows |
| RestlessPiles | 0.1.5 | **0.1.6** | Pile range stays hidden until isolate. Needs this Core. |
| RestlessDrawers | 0.1.0 | **0.1.1** | Metal/roughness on the chest shader. Hammer icons lose the black plate. |
| RestlessCook | 0.1.9 | 0.1.9 | No zip |
| RestlessPlant | 0.1.5 | 0.1.5 | No zip |
| Restless Valheim pack | 0.1.18 | **0.1.19** | Core + Piles + Drawers |

## What to tell players

**Restless Valheim 0.1.19**  
Core 0.1.15, Piles 0.1.6, Drawers 0.1.1. Cook and Plant unchanged.

**RestlessCore 0.1.15**  
Loadout totals on the character sheet. Layout dumps only while the Jötunn debug overlay is on. Expansion rows can hide. Plant 0.1.5 and Piles 0.1.5 still start.

**RestlessPiles 0.1.6**  
The pile range slider stays off F8 until Isolate pile range is on. Needs Core 0.1.15.

**RestlessDrawers 0.1.1**  
Cabinet bands can catch light. Hammer icons are no longer sitting on a black square.

## Tag order

Core first. Piles and Drawers after that listing exists. Pack last.

```
git tag v0.1.15 && git push origin v0.1.15
git tag piles-v0.1.6 && git push origin piles-v0.1.6
git tag drawers-v0.1.1 && git push origin drawers-v0.1.1
git tag pack-v0.1.19 && git push origin pack-v0.1.19
```

Pack waits up to 600s for Core, Piles, and Drawers. Cook 0.1.9 and Plant 0.1.5 are already listed.
