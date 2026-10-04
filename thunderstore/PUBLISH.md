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

Chest transfers, kitchen stock and workshop queues. One tag per push. Core first, the pack last. These tags are not pushed.

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.2.2 | **0.2.3** | A chest ingredient counts only after it is removed, and an open transfer is not taken twice |
| RestlessCook | 0.3.1 | **0.3.2** | Kitchen chests count in full, a finished rack meal stays reserved, and a cold cauldron pauses |
| RestlessStorage | 0.2.1 | **0.2.2** | A withdrawal stays reserved until it settles |
| RestlessWorkshop | 0.1.1 | **0.1.2** | A smelter gets the coal a bar burns, and the finished unit drops at the machine |
| Restless Valheim pack | 0.1.26 | **0.1.27** | Joins the versions above |

Plant 0.2.1, Piles 0.2.1 and Drawers 0.2.0 stay. Jötunn stays 2.30.2.

## Tag order

```
git tag v0.2.3
git push origin v0.2.3
git tag cook-v0.3.2
git push origin cook-v0.3.2
git tag storage-v0.2.2
git push origin storage-v0.2.2
git tag works-v0.1.2
git push origin works-v0.1.2
git tag pack-v0.1.27
git push origin pack-v0.1.27
```
