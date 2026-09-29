# Thunderstore 0.1.23

Prepared 29 Sep 2026. Not tagged yet. This drop: Core 0.2.0, Cook 0.2.0, Plant 0.2.0, Piles 0.2.0, Drawers 0.1.2, Storage 0.2.0, pack 0.1.23. BepInExPack 5.4.2351. Jötunn stays 2.30.2.

Pins live in `versions.yaml`. A package bump is `python scripts/bump-versions.py core=new cook=fix`. Changelog sentences stay written by hand.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.18 | **0.2.0** | Area seal, empty death pins, quiet extra-slot keys, storage page icon |
| RestlessCook | 0.1.9 | **0.2.0** | Cookbook and kitchen orders on the preparation table |
| RestlessPlant | 0.1.7 | **0.2.0** | Sprouts, field snap, paid grid cells, farm harvest, nests |
| RestlessPiles | 0.1.7 | **0.2.0** | Piles on the Storekeeper's Table |
| RestlessDrawers | 0.1.1 | **0.1.2** | One shared 1.1 m cell |
| RestlessStorage | 0.1.0 | **0.2.0** | Card wrap, piles on the sheet, handled materials |
| Restless Valheim pack | 0.1.22 | **0.1.23** | All six |

## What to tell players

**Restless Valheim 0.1.23**  
Core 0.2.0, Cook 0.2.0, Plant 0.2.0, Piles 0.2.0, Drawers 0.1.2, and Storage 0.2.0.

**RestlessCore 0.2.0**  
Hold Alt with the repair tool to seal wood against rain. A death with nothing left to recover drops the pin.

**RestlessCook 0.2.0**  
The food preparation table opens the Cookbook. Order a feast and the kitchen cooks it. Needs Core 0.2.0.

**RestlessPlant 0.2.0**  
A picked mushroom grows back from a sprout. On a bush, only the berries do.

**RestlessPiles 0.2.0**  
The Storekeeper's Table lists nearby piles. Needs Core 0.2.0.

**RestlessDrawers 0.1.2**  
Every cabinet is the same 1.1 m cube.

**RestlessStorage 0.2.0**  
Card names wrap, and piles show on the sheet. Needs Core 0.2.0.

## Tag order

Core first. Addons after that listing exists. Pack last. Do not run these until the drop is signed off.

```
git tag v0.2.0
git push origin v0.2.0
git tag cook-v0.2.0
git push origin cook-v0.2.0
git tag plant-v0.2.0
git push origin plant-v0.2.0
git tag piles-v0.2.0
git push origin piles-v0.2.0
git tag drawers-v0.1.2
git push origin drawers-v0.1.2
git tag storage-v0.2.0
git push origin storage-v0.2.0
git tag pack-v0.1.23
git push origin pack-v0.1.23
```

Pack waits up to 600s for Core 0.2.0, Cook 0.2.0, Plant 0.2.0, Piles 0.2.0, Drawers 0.1.2, and Storage 0.2.0.
