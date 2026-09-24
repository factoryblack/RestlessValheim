# Thunderstore 0.1.20

Signed off 25 Sep 2026. Live after this drop: Core 0.1.16, Plant 0.1.6, pack 0.1.20. Cook 0.1.9, Piles 0.1.6, and Drawers 0.1.1 unchanged. BepInExPack 5.4.2351. Jötunn stays 2.30.2.

Pins live in `versions.yaml`. After a Jötunn or package bump: `python scripts/sync-versions.py`.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.15 | **0.1.16** | Crafting sheet, station sockets, reader wheel, tame damage, station-gated building |
| RestlessPlant | 0.1.5 | **0.1.6** | Cultivator snap stays on the crop |
| RestlessCook | 0.1.9 | 0.1.9 | No zip |
| RestlessPiles | 0.1.6 | 0.1.6 | No zip |
| RestlessDrawers | 0.1.1 | 0.1.1 | No zip |
| Restless Valheim pack | 0.1.19 | **0.1.20** | Core + Plant. BepInExPack 5.4.2351 |

## What to tell players

**Restless Valheim 0.1.20**  
Core 0.1.16, Plant 0.1.6. Cook, Piles, and Drawers unchanged. BepInExPack 5.4.2351.

**RestlessCore 0.1.16**  
Crafting is one paper sheet. Station requirements sit in a material socket. You can kill your own tames. Chest building still needs the station.

**RestlessPlant 0.1.6**  
Cultivator snap stays on the crop. Needs Core 0.1.16.

## Tag order

Core first. Plant after that listing exists. Pack last.

```
git tag v0.1.16 && git push origin v0.1.16
git tag plant-v0.1.6 && git push origin plant-v0.1.6
git tag pack-v0.1.20 && git push origin pack-v0.1.20
```

Pack waits up to 600s for Core and Plant. Cook 0.1.9, Piles 0.1.6, and Drawers 0.1.1 are already listed.
