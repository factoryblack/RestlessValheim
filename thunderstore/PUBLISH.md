# Thunderstore drop 0.1.11

Signed off 21 Sep 2026.

## Proposed versions

Aligned to live Thunderstore (21 Sep) plus one bump on each changed plugin. Pack moves once.

| Package | Live now | Next | Why |
|---|---|---|---|
| RestlessCore | **0.1.8** | **0.1.9** | Wind split, craft stone paper, collection/ESC/map paper |
| RestlessCook | **0.1.7** | **0.1.8** | Feast boards place, leftover stats, recipe loop |
| RestlessPiles | **0.1.4** | 0.1.4 | No code this pass |
| RestlessPlant | **0.1.2** | **0.1.3** | Forage ghosts, fruit comes back, hover timer |
| Restless Valheim pack | **0.1.10** | **0.1.11** | One pack bump for the whole drop |

`PluginVersion`, csproj, and `thunderstore/*/manifest.json` now match the **Next** column.

Pack only moves +0.0.1 no matter how many plugins move.

## What to tell players

Paste these as the Thunderstore update text. Longer story is on each README.

**Restless Valheim 0.1.11**  
The full table, one click. Feast boards place. Leftover stats and the cook unlock loop are cleaned up. Bushes grow their fruit back. Wind at the helm sits left of the map. Tab collection screens, ESC, and the M map use the same paper kit.

**RestlessCore 0.1.9**  
World wind stays on the biome bar; the helm ring moves left of the minimap so it is not sitting on the buffs. Crafting paper stays grey stone. Compendium, trophies, achievements, ESC, and the M map use the shared paper kit.

**RestlessCook 0.1.8**  
Custom feast boards place with the serving tray. Leftovers no longer print the cloned Meadows stats under their own food. Incomplete leftover recipes stay disabled so they cannot unlock on a loop. Plates use a quiet vanilla food shader so they are not cave-dark.

**RestlessPlant 0.1.3**  
Mushrooms and thistle show a ghost when you plant them. Fruit comes back on bushes. Hover a bare plant to see how long it will take. Replant still only runs for carrots and the like.

**RestlessPiles**  
No new build this drop.

## Tag order

Cook and plant depend on Core 0.1.9, so Core goes first.

```
git tag v0.1.9 && git push origin v0.1.9
git tag plant-v0.1.3 && git push origin plant-v0.1.3
git tag cook-v0.1.8 && git push origin cook-v0.1.8
git tag pack-v0.1.11 && git push origin pack-v0.1.11
```

`v*` ships Core. `pack-v*` waits for those addon versions to show on Thunderstore.

## Still open (do not bury in the notes)

- Feast mesh size is a 3× visual scale until a Blender re-bake
- Authored plate albedo + emission to those UVs is a later art pass
- Collection / ESC / M map paper still needs Unity playtest (see docs/collection-screens.md, docs/menu-materials.md, docs/map-materials.md)
- HUD / Tab / crafting still mid-migration — READMEs already say so
- Temporary `restlesscook` dump stayed out of this zip
