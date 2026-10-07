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

A raw fish plate takes any one fish. A storage take counts what landed in the bag. A Make order counts each unit as it drops at the machine, and then stops. One tag per push. Core first, then Cook, then Workshop, and the pack last. These tags are not pushed.

| Package | Was | Now | Why |
|---|---|---|---|
| RestlessCore | 0.2.4 | **0.2.5** | A take counts the stacks that landed in the bag |
| RestlessCook | 0.5.0 | **0.5.1** | A raw fish plate takes any one fish |
| RestlessWorkshop | 0.1.3 | **0.1.4** | A Make order counts each drop and then stops |
| Restless Valheim pack | 0.1.30 | **0.1.31** | Joins Core 0.2.5, Cook 0.5.1, and Workshop 0.1.4 |

Plant 0.2.2, Storage 0.2.2, Piles 0.2.1 and Drawers 0.2.0 stay. Jötunn stays 2.30.2.

## Tag order

```
git tag v0.2.5
git push origin v0.2.5
git tag cook-v0.5.1
git push origin cook-v0.5.1
git tag works-v0.1.4
git push origin works-v0.1.4
git tag pack-v0.1.31
git push origin pack-v0.1.31
```
