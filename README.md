# RestlessCore

Valheim 1.0 quality-of-life core for Restless. One plugin, isolated Harmony modules, host-locked config.

The master list of what is in the DLL, planned, optional, or skipped is [`catalogue.yaml`](catalogue.yaml).

## Layout

```
catalogue.yaml            Master QoL behaviour list (built / planned / skip)
cook.yaml                 RestlessCook recipe graph (not part of the catalogue)
src/RestlessQoL/          RestlessCore BepInEx plugin
src/RestlessCook/         RestlessCook BepInEx plugin (hard-depends on Core)
thunderstore/plugin/      Thunderstore package for RestlessCore
thunderstore/pack/        Valheim modpack: BepInEx + Jötunn + Core + Cook
thunderstore/cook/        Thunderstore package for RestlessCook
```

## Plugin v0.1

Enabled by default; each feature can fail without taking the others down. The live list is [`catalogue.yaml`](catalogue.yaml). Player-facing copy lives in [`thunderstore/plugin/README.md`](thunderstore/plugin/README.md).

HUD, inventory, tooltips, crafting, and settings chrome are **in this DLL** and still landing — vanilla and Restless currently mix.

## Cook v0.1

Second plugin, hard-depends on Core. Recipe graph is [`cook.yaml`](cook.yaml). Tag `cook-v*` to ship it. A Core `v*` tag does not republish Cook.

## Pack v0.1

A Thunderstore **modpack** (not a plugin) listed as **Restless Valheim**. One-click install of BepInEx + Jötunn + RestlessCore + RestlessCook. The listing uses the original stone rune R.

## Build

Valheim is expected at `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. Copy `Environment.props.example` to `Environment.props` if yours is elsewhere.

```
dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release
dotnet build src/RestlessCook/RestlessCook.csproj -c Release
```

Output: `dist/RestlessCore.dll` and `dist/RestlessCook.dll`. Drop Core into a r2modman profile `BepInEx/plugins/RestlessCore/` along with Jötunn. Cook goes beside it and needs Core.

```
powershell -File scripts/test-local.ps1          # build + install into Steam Valheim
powershell -File scripts/test-local.ps1 -Launch  # same, then start the game
powershell -File scripts/pack.ps1                # zip artifacts/ for Thunderstore / r2modman import
```

This Steam folder is already BepInEx-patched. Launch from Steam, not a vanilla shortcut.

r2modman: install the **Restless Valheim** modpack, or BepInEx + Jötunn then Import local `artifacts/Restless-RestlessCore-0.1.3.zip` from `scripts/pack.ps1`.

## GitHub Actions

Pushes and pull requests compile Release `RestlessCore.dll` and `RestlessCook.dll` on Ubuntu. Runners have no Steam client, so the job downloads the **dedicated server** (Steam app `896660`) plus BepInEx `5.4.2350`, then caches that tree by Valheim buildid. That is the same approach Jötunn uses.

A tag named `v*` ships **RestlessCore** and the **Restless Valheim** modpack. A tag named `pack-v*` ships the pack only. A tag named `cook-v*` ships **RestlessCook** only.

```
git tag v0.1.3
git push origin v0.1.3

git tag pack-v0.1.3
git push origin pack-v0.1.3

git tag cook-v0.1.0
git push origin cook-v0.1.0
```

### Thunderstore token

Do **not** put the API key in git, chat, or the workflow file.

1. On Thunderstore, create a team named exactly `Restless` (the pack depends on `Restless-RestlessCore` and `Restless-RestlessCook`).
2. Team → **Service Accounts** → add one (name it `github` or similar). Copy the token once.
3. In this GitHub repo: **Settings → Secrets and variables → Actions → New repository secret**.
4. Name: `TCLI_AUTH_TOKEN`. Value: that token.

You can also run the compile job by hand from the Actions tab (`workflow_dispatch`). Publish still only happens on a `v*`, `pack-v*`, or `cook-v*` tag.
