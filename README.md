# RestlessCore

Valheim 1.0 quality-of-life core for Restless. One plugin, isolated Harmony modules, host-locked config.

The master list of what is in the DLL, planned, optional, or skipped is [`catalogue.yaml`](catalogue.yaml).

## Layout

```
catalogue.yaml            Master QoL behaviour list (built / planned / skip)
cook.yaml                 RestlessCook recipe graph (not part of the catalogue)
src/RestlessQoL/          RestlessCore BepInEx plugin
src/RestlessCook/         RestlessCook BepInEx plugin (hard-depends on Core)
src/RestlessPiles/        RestlessPiles BepInEx plugin (hard-depends on Core)
src/RestlessPlant/        RestlessPlant BepInEx plugin (hard-depends on Core)
thunderstore/plugin/      Thunderstore package for RestlessCore
thunderstore/pack/        Valheim modpack: BepInEx + Jötunn + Core + Cook + Piles + Plant
thunderstore/cook/        Thunderstore package for RestlessCook
thunderstore/piles/       Thunderstore package for RestlessPiles
thunderstore/plant/       Thunderstore package for RestlessPlant
```

## Plugin v0.1

Enabled by default; each feature can fail without taking the others down. The live list is [`catalogue.yaml`](catalogue.yaml). Player-facing copy lives in [`thunderstore/plugin/README.md`](thunderstore/plugin/README.md).

HUD, inventory, tooltips, crafting, and settings chrome are **in this DLL** and still landing — vanilla and Restless currently mix.

## Cook v0.1

Second plugin, hard-depends on Core. Recipe graph is [`cook.yaml`](cook.yaml). The Thunderstore page is the cooking wiki and matrix. Tag `cook-v*` to ship it. A Core `v*` tag does not republish Cook.

## Piles / Plant v0.1

Sibling plugins, hard-depend on Core. They ship in the Restless Valheim pack. Tag `piles-v*` or `plant-v*` to ship each plugin on its own.

## Pack v0.1

A Thunderstore **modpack** (not a plugin) listed as **Restless Valheim**. One-click install of BepInEx + Jötunn + RestlessCore + RestlessCook + RestlessPiles + RestlessPlant. The listing uses the original stone rune R.

## Build

Valheim is expected at `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. Copy `Environment.props.example` to `Environment.props` if yours is elsewhere.

```
dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release
dotnet build src/RestlessCook/RestlessCook.csproj -c Release
dotnet build src/RestlessPiles/RestlessPiles.csproj -c Release
dotnet build src/RestlessPlant/RestlessPlant.csproj -c Release
```

Output: `dist/RestlessCore.dll`, `dist/RestlessCook.dll`, `dist/RestlessPiles.dll`, and `dist/RestlessPlant.dll`. Drop Core into a r2modman profile `BepInEx/plugins/RestlessCore/` along with Jötunn. Cook, Piles, and Plant go beside it and need Core.

```
powershell -File scripts/test-local.ps1          # build + install into Steam Valheim
powershell -File scripts/test-local.ps1 -Launch  # same, then start the game
powershell -File scripts/pack.ps1                # zip artifacts/ for Thunderstore / r2modman import
```

This Steam folder is already BepInEx-patched. Launch from Steam, not a vanilla shortcut.

r2modman: install the **Restless Valheim** modpack, or BepInEx + Jötunn then Import local `artifacts/Restless-RestlessCore-0.1.5.zip` from `scripts/pack.ps1`.

## GitHub Actions

Pushes and pull requests compile Release `RestlessCore.dll`, `RestlessCook.dll`, `RestlessPiles.dll`, and `RestlessPlant.dll` on Ubuntu. Runners have no Steam client, so the job downloads the **dedicated server** (Steam app `896660`) plus BepInEx `5.4.2350`, then caches that tree by Valheim buildid. That is the same approach Jötunn uses.

A tag named `v*` ships **RestlessCore** and the **Restless Valheim** modpack (Core + Cook + Piles + Plant). A tag named `pack-v*` ships the pack only. `cook-v*`, `piles-v*`, and `plant-v*` ship those plugins only.

```
git tag v0.1.5
git push origin v0.1.5

git tag pack-v0.1.7
git push origin pack-v0.1.7

git tag cook-v0.1.3
git push origin cook-v0.1.3

git tag piles-v0.1.1
git push origin piles-v0.1.1

git tag plant-v0.1.0
git push origin plant-v0.1.0
```

### Thunderstore token

Do **not** put the API key in git, chat, or the workflow file.

1. On Thunderstore, create a team named exactly `Restless` (the pack depends on Core, Cook, Piles, and Plant).
2. Team → **Service Accounts** → add one (name it `github` or similar). Copy the token once.
3. In this GitHub repo: **Settings → Secrets and variables → Actions → New repository secret**.
4. Name: `TCLI_AUTH_TOKEN`. Value: that token.

You can also run the compile job by hand from the Actions tab (`workflow_dispatch`). Publish still only happens on a `v*`, `pack-v*`, `cook-v*`, `piles-v*`, or `plant-v*` tag.
