# Thunderstore 0.1.16

Signed off 22 Sep 2026.

## Proposed versions

Core idle-frame skip on hammer and Tab. Pack moves once. Cook, plant and piles stay.

| Package | Live now | Next | Why |
|---|---|---|---|
| RestlessCore | **0.1.11** | **0.1.12** | Hammer and Tab skip a full re-dress on idle frames |
| RestlessCook | **0.1.9** | 0.1.9 | No code this pass |
| RestlessPiles | **0.1.4** | 0.1.4 | No code this pass |
| RestlessPlant | **0.1.4** | 0.1.4 | No code this pass |
| Restless Valheim pack | **0.1.15** | **0.1.16** | One pack bump for Core |

`PluginVersion`, csproj, and `thunderstore/*/manifest.json` now match the **Next** column.

## What to tell players

**Restless Valheim 0.1.16**  
Hammer menu and Tab stay still when you are not changing anything.

**RestlessCore 0.1.12**  
The hammer menu no longer rebuilds every piece on idle frames. Tab paper (bag, chest, craft, skills, collections) only refreshes when a stack, recipe, or overlay actually changes.

**RestlessCook / RestlessPiles / RestlessPlant**  
No new build this drop.

## Tag order

Core first so the pack can wait for it.

```
git tag v0.1.12 && git push origin v0.1.12
git tag pack-v0.1.16 && git push origin pack-v0.1.16
```

`v*` ships Core. `pack-v*` waits for Restless-* deps on Thunderstore.
