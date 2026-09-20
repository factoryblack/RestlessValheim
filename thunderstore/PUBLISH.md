# Thunderstore hotfix 0.1.12

Signed off 21 Sep 2026.

## Proposed versions

Core-only ESC label hotfix. Pack moves once. Cook, plant and piles stay.

| Package | Live now | Next | Why |
|---|---|---|---|
| RestlessCore | **0.1.9** | **0.1.10** | ESC pause labels were vanishing after a frame |
| RestlessCook | **0.1.8** | 0.1.8 | No code this pass |
| RestlessPiles | **0.1.4** | 0.1.4 | No code this pass |
| RestlessPlant | **0.1.3** | 0.1.3 | No code this pass |
| Restless Valheim pack | **0.1.11** | **0.1.12** | One pack bump for the Core hotfix |

`PluginVersion`, csproj, and `thunderstore/*/manifest.json` now match the **Next** column.

## What to tell players

**Restless Valheim 0.1.12**  
Hotfix. Pause menu labels stay on the paper column.

**RestlessCore 0.1.10**  
ESC pause labels stay visible. The paper plate was fine; the row layout was collapsing after a frame.

**RestlessCook / RestlessPlant / RestlessPiles**  
No new build this drop.

## Tag order

Core first so the pack can wait for 0.1.10.

```
git tag v0.1.10 && git push origin v0.1.10
git tag pack-v0.1.12 && git push origin pack-v0.1.12
```

`v*` ships Core. `pack-v*` waits for Restless-* deps on Thunderstore.
