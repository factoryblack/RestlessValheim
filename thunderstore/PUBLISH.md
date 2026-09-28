# Thunderstore 0.1.22

Signed off 28 Sep 2026. Live after this drop: Core 0.1.18, Storage 0.1.0, pack 0.1.22. Cook 0.1.9, Plant 0.1.7, Piles 0.1.7, and Drawers 0.1.1 stay. BepInExPack 5.4.2351. Jötunn stays 2.30.2.

Pins live in `versions.yaml`. After a Jötunn or package bump: `python scripts/sync-versions.py`.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.1.17 | **0.1.18** | Storage browser, and Take answered by the chest owner |
| RestlessStorage | — | **0.1.0** | Storekeeper's Table. First listing |
| RestlessCook | 0.1.9 | 0.1.9 | No zip |
| RestlessPlant | 0.1.7 | 0.1.7 | No zip |
| RestlessPiles | 0.1.7 | 0.1.7 | No zip |
| RestlessDrawers | 0.1.1 | 0.1.1 | No zip |
| Restless Valheim pack | 0.1.21 | **0.1.22** | Core + Storage |

## What to tell players

**Restless Valheim 0.1.22**  
Core 0.1.18 and Storage 0.1.0. Cook, Plant, Piles, and Drawers unchanged.

**RestlessCore 0.1.18**  
The Storekeeper's Table can list nearby chests and take from them. The chest owner moves the stacks.

**RestlessStorage 0.1.0**  
Place the table at a workbench. Search what the nearby chests hold and take it. Needs Core 0.1.18.

## Tag order

Core first. Storage after that listing exists. Pack last.

```
git tag v0.1.18 && git push origin v0.1.18
git tag storage-v0.1.0 && git push origin storage-v0.1.0
git tag pack-v0.1.22 && git push origin pack-v0.1.22
```

Pack waits up to 600s for Core 0.1.18 and Storage 0.1.0. Cook, Plant, Piles, and Drawers are already listed.
