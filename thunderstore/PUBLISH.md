# Thunderstore 0.1.14

Signed off 22 Sep 2026.

## Proposed versions

Core build-menu paper and Plant grid extras. Pack moves once. Cook and piles stay.

| Package | Live now | Next | Why |
|---|---|---|---|
| RestlessCore | **0.1.10** | **0.1.11** | Hammer / hoe / tray paper menu and compact piece card |
| RestlessCook | **0.1.9** | 0.1.9 | No code this pass |
| RestlessPiles | **0.1.4** | 0.1.4 | No code this pass |
| RestlessPlant | **0.1.3** | **0.1.4** | Soil snap, red skip, rows×columns, F10, extras |
| Restless Valheim pack | **0.1.13** | **0.1.14** | One pack bump for Core + Plant |

`PluginVersion`, csproj, and `thunderstore/*/manifest.json` now match the **Next** column.

## What to tell players

**Restless Valheim 0.1.14**  
Hammer menu on paper, and planting that sits on the soil.

**RestlessCore 0.1.11**  
Hammer / hoe / serving-tray menu uses the shared paper wells. The selected piece gets a compact card under the grid (name, description, costs, station).

**RestlessPlant 0.1.4**  
A planting square sits on the dirt. Bad cells show red and are skipped. `[` `]` and `-` `=` set width and depth. F10 snaps to a nearby plant.

**RestlessCook / RestlessPiles**  
No new build this drop.

## Tag order

Core and Plant first so the pack can wait for them.

```
git tag v0.1.11 && git push origin v0.1.11
git tag plant-v0.1.4 && git push origin plant-v0.1.4
git tag pack-v0.1.14 && git push origin pack-v0.1.14
```

`v*` ships Core. `plant-v*` ships Plant. `pack-v*` waits for Restless-* deps on Thunderstore.
