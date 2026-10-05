# Publish Restless packages

Use [the build and release guide](../docs/maintainers/building.md) for tag prefixes and build commands, and [the documentation workflow](../docs/maintainers/public-docs.md) for package copy and screenshot updates.

1. Check the current source and assign Unreleased changelog notes to the version actually shipping.
2. Bump selected packages with `scripts/bump-versions.py`; `versions.yaml` owns version and dependency pins.
3. Run the documented generation checks. Review the package README, bio, dependency list and version notes together.
4. Build and playtest complete packages, including runtime assets. `scripts/pack.ps1` stages local archives from package TOMLs.
5. Publish dependencies before their consumers. Publish the collection pack last, after its pinned dependencies are available.
6. Check each live Thunderstore page after publication: description, guide link, screenshots, changelog and dependencies.

A Core tag publishes Core only. Workshop is a pack member. Do not reactivate the deprecated legacy packages.

Documentation merges do not update an existing Thunderstore release automatically. The revised package pages ship in the next package release. Screenshot placeholders are intentional until captures are available; see [the capture brief](../docs/maintainers/screenshots.md).

## This drop

An iron order takes iron scraps. Iron ore is used only when no scraps are on hand. One tag per push. Workshop first, the pack last. These tags are not pushed.

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessWorkshop | 0.1.2 | **0.1.3** | An iron order takes iron scraps |
| Restless Valheim pack | 0.1.28 | **0.1.29** | Joins Workshop 0.1.3 |

Core 0.2.4, Cook 0.4.0, Plant 0.2.2, Storage 0.2.2, Piles 0.2.1 and Drawers 0.2.0 stay. Jötunn stays 2.30.2.

## Tag order

```
git tag works-v0.1.3
git push origin works-v0.1.3
git tag pack-v0.1.29
git push origin pack-v0.1.29
```
