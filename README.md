# RestlessCore

Valheim 1.0 quality-of-life core for Restless. One plugin, isolated Harmony modules, host-locked config.

This is **not** the old 60-mod `RestlessQOL` 1.0.7 pack. The master list of what is in the DLL, planned, optional, or skipped is [`catalogue.yaml`](catalogue.yaml).

## Layout

```
catalogue.yaml            Master behaviour list (built / planned / skip)
src/RestlessQoL/          C# BepInEx plugin
thunderstore/plugin/      Thunderstore package for the DLL
thunderstore/pack/        Optional one-click install of BepInEx + Jötunn + this DLL
```

## Plugin v0.1

Enabled by default; each feature can fail without taking the others down. The live list is [`catalogue.yaml`](catalogue.yaml). Player-facing copy lives in [`thunderstore/plugin/README.md`](thunderstore/plugin/README.md).

HUD, inventory, tooltips, crafting, and settings chrome are **in this DLL** and mid-migration — vanilla and Restless currently mix. Extra worn + Z/X/C slots are in the DLL; do not install EquipmentAndQuickSlots. MyLittleUI is still the only optional sidecar (timers / multicraft). PlanBuild, Auga, and ValheimPlus are skip.

## Pack v0.1

BepInEx + Jötunn + this DLL. Not a 60-mod stack.

Do **not** also install ValheimPlus, BetterUI, MinimalUI, or AzuCraftyBoxes next to this. They patch the same inventory/build methods.

## Build

Valheim is expected at `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. Copy `Environment.props.example` to `Environment.props` if yours is elsewhere.

```
dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release
```

Output: `dist/RestlessCore.dll`. Drop it into a r2modman profile `BepInEx/plugins/RestlessCore/` along with Jötunn.

```
powershell -File scripts/test-local.ps1          # build + install into Steam Valheim
powershell -File scripts/test-local.ps1 -Launch  # same, then start the game
powershell -File scripts/pack.ps1                # zip artifacts/ for Thunderstore / r2modman import
```

This Steam folder is already BepInEx-patched. Launch from Steam, not a vanilla shortcut.

Until the first tag ships, r2modman: BepInEx + Jötunn online, then Import local `artifacts/Restless-RestlessCore-0.1.0.zip`. The pack zip only works after the plugin itself is on Thunderstore.

## GitHub Actions

Pushes and pull requests compile a Release `RestlessCore.dll` on Ubuntu. Runners have no Steam client, so the job downloads the **dedicated server** (Steam app `896660`) plus BepInEx `5.4.2350`, then caches that tree by Valheim buildid. That is the same approach Jötunn uses.

A tag named `v*` is the ship button. It builds the DLL, opens a GitHub Release, then publishes **RestlessCore** and **RestlessCorePack** to Thunderstore under the `Restless` team.

```
git tag v0.1.0
git push origin v0.1.0
```

### Thunderstore token

Do **not** put the API key in git, chat, or the workflow file.

1. On Thunderstore, create a team named exactly `Restless` (the pack already depends on `Restless-RestlessCore`).
2. Team → **Service Accounts** → add one (name it `github` or similar). Copy the token once.
3. In this GitHub repo: **Settings → Secrets and variables → Actions → New repository secret**.
4. Name: `TCLI_AUTH_TOKEN`. Value: that token.

You can also run the compile job by hand from the Actions tab (`workflow_dispatch`). Publish still only happens on a `v*` tag.
