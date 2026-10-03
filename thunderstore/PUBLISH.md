# Thunderstore 0.1.24

Prepared 29 Sep 2026. Not tagged yet. This drop: Core 0.2.1, Cook 0.3.0. Plant 0.2.0, Piles 0.2.0, Drawers 0.1.2, and Storage 0.2.0 stay. Pack 0.1.24. BepInExPack 5.4.2351. Jötunn stays 2.30.2.

Pins live in `versions.yaml`. A package bump is `python scripts/bump-versions.py core=fix cook=new`. Changelog sentences stay written by hand.

## This drop

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.2.0 | **0.2.1** | Seal bind on the hammer, Alt-click seals, shared cooking portrait frame |
| RestlessCook | 0.2.0 | **0.3.0** | Cookbook portraits, filters, timed prep, step progress, real native ingredients |
| RestlessPlant | 0.2.0 | **0.2.0** | Stays |
| RestlessPiles | 0.2.0 | **0.2.0** | Stays |
| RestlessDrawers | 0.1.2 | **0.1.2** | Stays |
| RestlessStorage | 0.2.0 | **0.2.0** | Stays |
| Restless Valheim pack | 0.1.23 | **0.1.24** | Core and Cook |

## What to tell players

**Restless Valheim 0.1.24**  
Core 0.2.1 and Cook 0.3.0. Plant, Piles, Drawers, and Storage stay.

**RestlessCore 0.2.1**  
Hold Alt and click with the hammer out to seal wood. The Seal bind is on the hammer key stack.

**RestlessCook 0.3.0**  
The Cookbook shows portraits, filters, and each step of an order. Cauldron and prep crafts take their time. Needs Core 0.2.1.

## Tag order

Core first. Cook after that listing exists. Pack last. Plant, Piles, Drawers, and Storage are already listed. Do not run these until the drop is signed off.

```
git tag v0.2.1
git push origin v0.2.1
git tag cook-v0.3.0
git push origin cook-v0.3.0
git tag pack-v0.1.24
git push origin pack-v0.1.24
```

Pack waits up to 600s for Core 0.2.1 and Cook 0.3.0. The other four are already published.

## Pending ecosystem icon refresh

The aligned icon refresh is recorded under **Unreleased** in all eight package changelogs. These entries are not a published release and do not change the version pins or tag plan above.

- Core embeds the package icons for the F8 module cards, module headings and modpack link. Releasing Core updates the in-game collection artwork.
- Each of the eight Thunderstore listings needs a new package version to receive its own updated icon. Publishing Core alone does not refresh the expansion listings.
- When preparing the icon release, assign each package a new version with the version-bump script, move its Unreleased entry into that version's changelog section, and update this publish plan. Publish the plugins before a pack release that depends on their new versions.
- All package icons remain 256 × 256 PNGs with transparent exterior; no gameplay changes accompany the icon refresh.
