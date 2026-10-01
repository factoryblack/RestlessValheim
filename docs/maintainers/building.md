# Build and release Restless

## Repository map

| Path | Purpose |
| --- | --- |
| `src/RestlessQoL/` | RestlessCore; historical source-directory name |
| `src/RestlessCook/` | Food expansion and Cookbook |
| `src/RestlessPlant/` | Cultivator, planting and harvest |
| `src/RestlessPiles/` | Bulk resource-pile storage |
| `src/RestlessDrawers/` | Modular cabinets |
| `src/RestlessStorage/` | Storekeeper's Table |
| `src/RestlessWorks/` | Production board and Workshop UI |
| `thunderstore/` | Package manifests, TOMLs, pages, changelogs and icons |
| `docs/player/` | Player guides and generated food reference |
| `catalogue.yaml` | Core feature decisions/status |
| `cook.yaml` | Cook recipe graph |
| `versions.yaml` | Package/dependency version pins |

## Local build

Copy `Environment.props.example` to `Environment.props` and point it at your Valheim installation. Do not commit local installation paths.

```sh
dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release
dotnet build src/RestlessCook/RestlessCook.csproj -c Release
dotnet build src/RestlessPlant/RestlessPlant.csproj -c Release
dotnet build src/RestlessPiles/RestlessPiles.csproj -c Release
dotnet build src/RestlessDrawers/RestlessDrawers.csproj -c Release
dotnet build src/RestlessStorage/RestlessStorage.csproj -c Release
dotnet build src/RestlessWorks/RestlessWorks.csproj -c Release
```

DLLs are written to `dist/`. Install complete packages with their shipped assets. The local scripts `test-local.ps1` and `install-local.ps1` help install/playtest; check their parameters before use.

`powershell -File scripts/pack.ps1` builds all registered plugins and stages the package files/assets specified by their TOMLs. Archive names use current manifest versions.

## Versioning and release

Edit/bump versions through the existing tooling, then run `python scripts/sync-versions.py`. Public metadata is separate: `python scripts/sync-public-docs.py`.

| Tag prefix | Published package |
| --- | --- |
| `v*` | Core only |
| `pack-v*` | Restless Valheim pack |
| `cook-v*` | Cook |
| `plant-v*` | Plant |
| `piles-v*` | Piles |
| `drawers-v*` | Drawers |
| `storage-v*` | Storage |
| `works-v*` | Works |

A Core tag compiles siblings but **does not republish the pack or expansions**. Publish required dependencies before consumers, then the pack last. Keep Unreleased notes out of a tagged release until assigned to its actual version.

The build workflow runs on PRs/pushes and uses a cached dedicated-server installation for compilation. Compilation and regression checks do not replace in-game UI/multiplayer playtests.

Thunderstore publication uses the existing `TCLI_AUTH_TOKEN` repository secret and the Restless namespace. Never put tokens in documentation or source.

[Public documentation workflow](public-docs.md) · [Player guides](../player/README.md)

