# Thunderstore 0.1.18

Signed off 23 Sep 2026. Live after this drop: Core 0.1.14, Plant 0.1.5, Piles 0.1.5, pack 0.1.18. Cook 0.1.9 and Drawers 0.1.0 unchanged.

Pins live in `versions.yaml`. After a Jötunn or package bump: `python scripts/sync-versions.py`.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.13 | **0.1.14** | Plant/Piles F8 API, search range covers piles, dedicated pet pantry |
| RestlessPlant | 0.1.4 | **0.1.5** | F8 Plant page (PR 19). Needs this Core. |
| RestlessPiles | 0.1.4 | **0.1.5** | F8 Piles page, isolate range. Needs this Core. |
| RestlessCook | 0.1.9 | 0.1.9 | No zip |
| RestlessDrawers | 0.1.0 | 0.1.0 | No zip |
| Restless Valheim pack | 0.1.17 | **0.1.18** | Core + Plant + Piles |

## What to tell players

**Restless Valheim 0.1.18**  
Core 0.1.14, Plant 0.1.5, Piles 0.1.5. Cook and Drawers unchanged.

**RestlessCore 0.1.14**  
Plant and Piles settings sit on their F8 pages. Search range covers piles unless Piles isolates. Dedicated-host tames eat from nearby chests.

**RestlessPlant 0.1.5**  
Planting, harvest, grid and personal controls on the F8 Plant page. Needs Core 0.1.14.

**RestlessPiles 0.1.5**  
Enabled and isolate range on the F8 Piles page. Piles follow Core Search range unless isolated. Needs Core 0.1.14.

## Tag order

```
git tag v0.1.14 && git push origin v0.1.14
git tag plant-v0.1.5 && git push origin plant-v0.1.5
git tag piles-v0.1.5 && git push origin piles-v0.1.5
git tag pack-v0.1.18 && git push origin pack-v0.1.18
```

Pack waits up to 600s for the three listings. Do not tag Core alone.
