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

Workshop joins the pack. Publish Workshop first, then the pack. One tag per push.

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessWorkshop | 0.1.0 | **0.1.1** | The listing now belongs to the pack |
| Restless Valheim pack | 0.1.25 | **0.1.26** | Workshop is a required member |

The other packages stay on the versions published in 0.1.25. Jötunn stays 2.30.2.

## Tag order

```
git tag works-v0.1.1
git push origin works-v0.1.1
git tag pack-v0.1.26
git push origin pack-v0.1.26
```
