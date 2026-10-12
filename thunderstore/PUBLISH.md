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

Core 0.3.0 introduces opt-in Global Reach through 120 m, with smaller repair/seal/harvest sets, effective-value controls and reversible native station coverage. Cook 0.5.4, Workshop 0.1.6, Piles 0.2.3, Plant 0.2.4 and Storage 0.2.3 consume the shared policy; their plugin dependencies require Core 0.3.0. Drawers 0.2.1 updates its Core dependency. Pack 0.1.34 pins the updated set.

Publish Core first, then the six addons, then the pack after every dependency is publicly available. One tag per push. The user authorised publication of the complete drop on 2026-10-12, including the optional crafting window, batch queue, shared window polish and issue 44 kitchen scheduler fixes. Global Reach stays off on upgrade. Automated profile, boundary, native restoration and pile range-change regressions run in CI. Live settings UI, multiplayer and dense-base performance at 120 m remain pending. Jötunn stays 2.30.2.

## Pending tag order

Run `python scripts/release-plan.py` to print commands for pinned versions that do not already have a local tag. It reads `versions.yaml`, puts Core first and the collection pack last, and does not execute the commands. Fetch repository tags before planning against another checkout. Create and push one tag at a time, waiting for each dependency release before publishing consumers.

Do not paste commands from historical release notes. Published changelogs retain their original versions; the live pins and generated plan own the next drop.
