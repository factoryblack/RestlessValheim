# Thunderstore 0.1.17

Signed off 23 Sep 2026. Live: Core 0.1.13, Drawers 0.1.0, pack 0.1.17.

Next drop is not signed off. PR 19 (Plant/Piles F8 controls) is on main — Core + Plant + Piles must ship together. Do not tag.

Pins live in `versions.yaml`. After a Jötunn or package bump: `python scripts/sync-versions.py`.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.12 | **0.1.13** | F8 collection, PR 17/18 restore, clock, station time, Jötunn 2.30.2 |
| RestlessDrawers | — | **0.1.0** | First ship. Furniture cabinets. |
| RestlessCook | 0.1.9 | 0.1.9 | No zip |
| RestlessPiles | 0.1.4 | 0.1.4 | No zip |
| RestlessPlant | 0.1.4 | 0.1.4 | No zip |
| Restless Valheim pack | 0.1.16 | **0.1.17** | Core + Drawers |

## What to tell players

**Restless Valheim 0.1.17**  
Core 0.1.13 and the first RestlessDrawers cabinets. Same Jötunn 2.30.2.

**RestlessCore 0.1.13**  
F8 shows the collection and the versions actually loaded, including Drawers. Tab and chests restore vanilla on close. Day/time on the biome bar. Station remaining time on look. Loadout totals from the armor chip.

**RestlessDrawers 0.1.0**  
Wood, personal, reinforced, and black metal cabinets. Same slots and recipes as the matching chest. They snap together and stay up.

## Tag order

```
git tag v0.1.13 && git push origin v0.1.13
git tag drawers-v0.1.0 && git push origin drawers-v0.1.0
git tag pack-v0.1.17 && git push origin pack-v0.1.17
```
