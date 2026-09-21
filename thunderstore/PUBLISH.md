# Thunderstore hotfix 0.1.13

Signed off 21 Sep 2026.

## Proposed versions

Cook-only r2modman mesh hotfix. Pack moves once. Core, plant and piles stay.

| Package | Live now | Next | Why |
|---|---|---|---|
| RestlessCore | **0.1.10** | 0.1.10 | No code this pass |
| RestlessCook | **0.1.8** | **0.1.9** | r2modman flattens `mesh/`; plates were falling back to vanilla |
| RestlessPiles | **0.1.4** | 0.1.4 | No code this pass |
| RestlessPlant | **0.1.3** | 0.1.3 | No code this pass |
| Restless Valheim pack | **0.1.12** | **0.1.13** | One pack bump for the Cook hotfix |

`PluginVersion`, csproj, and `thunderstore/*/manifest.json` now match the **Next** column.

## What to tell players

**Restless Valheim 0.1.13**  
Hotfix. Custom cook plates show again in r2modman / Thunderstore installs.

**RestlessCook 0.1.9**  
Custom meal and feast plates load when the manager flattens the `mesh/` folder next to the DLL.

**RestlessCore / RestlessPlant / RestlessPiles**  
No new build this drop.

## Tag order

Cook first so the pack can wait for 0.1.9.

```
git tag cook-v0.1.9 && git push origin cook-v0.1.9
git tag pack-v0.1.13 && git push origin pack-v0.1.13
```

`cook-v*` ships Cook. `pack-v*` waits for Restless-* deps on Thunderstore.
