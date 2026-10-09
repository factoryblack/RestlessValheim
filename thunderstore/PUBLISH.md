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

Core 0.3.0 introduces opt-in Global Reach through 120 m, with smaller repair/seal/harvest sets, effective-value controls and reversible native station coverage. Cook 0.5.4, Workshop 0.1.6, Piles 0.2.3, Plant 0.2.4 and Storage 0.2.3 consume the shared policy; their plugin dependencies require Core 0.3.0. Drawers remains 0.2.0. Pack 0.1.34 pins the updated set.

Publish Core first, then the five updated addons, then the pack after every dependency is publicly available. One tag per push. This change is prepared for review; no new release has been published. Global Reach stays off on upgrade. Automated profile, boundary, native restoration and pile range-change regressions run in CI. Live settings UI, multiplayer and dense-base performance at 120 m remain pending. JÃ¶tunn stays 2.30.2.

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
