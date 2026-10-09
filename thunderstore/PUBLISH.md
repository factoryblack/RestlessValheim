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

Core 0.2.7 adds invariant storage identifiers and the shared actor-aware pile/production policy. Publish Core first, then Cook 0.5.3, Workshop 0.1.5, Piles 0.2.2 and Plant 0.2.3. Publish pack 0.1.33 after all five dependencies are publicly available. Drawers 0.2.0 and Storage 0.2.2 remain pack members. One tag per push.

The source changes fix all six audit findings and add Plant preview caching and pickup-history cleanup. The new Audit regression suite runs in CI alongside the existing suites. Jötunn stays 2.30.2.

The user authorised publication after build/regression checks, with live multiplayer, planting, Drawers visuals and physics acceptance to follow. See docs/maintainers/reliability.md for the remaining runtime checks.

## Tag order

```
git tag v0.2.7
git push origin v0.2.7
git tag cook-v0.5.3
git push origin cook-v0.5.3
git tag works-v0.1.5
git push origin works-v0.1.5
git tag piles-v0.2.2
git push origin piles-v0.2.2
git tag plant-v0.2.3
git push origin plant-v0.2.3
git tag pack-v0.1.33
git push origin pack-v0.1.33
```
