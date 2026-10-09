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

Core 0.2.6 is already published and leaves placed feast boards out of ground collection. Cook 0.5.2 preserves native support colliders through visual refresh and reload, and reads kitchen collection state after obtaining ownership. Publish Cook first, verify it is available, then publish the pack. One tag per push.

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCook | 0.5.1 | **0.5.2** | Persistent feast support and collection snapshot correction |
| Restless Valheim pack | 0.1.31 | **0.1.32** | Joins Core 0.2.6 and Cook 0.5.2 |

Core 0.2.6, Workshop 0.1.4, Plant 0.2.2, Storage 0.2.2, Piles 0.2.1 and Drawers 0.2.0 stay. Jötunn stays 2.30.2.

In-game physics, save/reload and multiplayer acceptance remain pending. The user authorised publication after build/regression checks on 10 October 2026, with live testing and feedback to follow. See docs/cooking-lifecycle-audit.md and docs/maintainers/reliability.md for the outstanding checks.

## Tag order

```
git tag cook-v0.5.2
git push origin cook-v0.5.2
git tag pack-v0.1.32
git push origin pack-v0.1.32
```
