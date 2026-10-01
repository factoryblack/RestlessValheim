# Restless for Valheim

**Less friction. Clearer information. Still Valheim.**

Restless is a collection of mods for Valheim 1.0: a shared interface and quality-of-life foundation, with optional additions for cooking, farming, storage and workshop production. Gathering, station upgrades and progression still matter; the collection makes everyday work easier to understand and manage.

> (screenshot coming) — Restless HUD at a finished base.

[Install the collection](https://thunderstore.io/c/valheim/p/Restless/Restless_Valheim/) · [Player guides](docs/player/README.md) · [Report a problem](https://github.com/factoryblack/RestlessValheim/issues/new/choose)

## Choose your Restless

| Mod | What it adds | Guide |
| --- | --- | --- |
| [RestlessCore](https://thunderstore.io/c/valheim/p/Restless/RestlessCore/) | Shared interface, nearby-storage crafting, equipment slots and configurable QoL | [Core](docs/player/core.md) |
| [RestlessCook](https://thunderstore.io/c/valheim/p/Restless/RestlessCook/) | Meals, feasts and kitchen orders at the food preparation table | [Cook](docs/player/cook.md) |
| [RestlessPlant](https://thunderstore.io/c/valheim/p/Restless/RestlessPlant/) | Cultivator forage, grid planting, area harvest and replanting | [Plant](docs/player/plant.md) |
| [RestlessPiles](https://thunderstore.io/c/valheim/p/Restless/RestlessPiles/) | Familiar resource piles become bulk stores | [Piles](docs/player/piles.md) |
| [RestlessDrawers](https://thunderstore.io/c/valheim/p/Restless/RestlessDrawers/) | Modular cabinets with matching chest capacity and visible contents | [Drawers](docs/player/drawers.md) |
| [RestlessStorage](https://thunderstore.io/c/valheim/p/Restless/RestlessStorage/) | Search and withdraw from nearby storage at the Storekeeper's Table | [Storage](docs/player/storage.md) |
| [RestlessWorkshop](https://thunderstore.io/c/valheim/p/Restless/RestlessWorks/) | Make batches or maintain stock targets with nearby production machines | [Workshop](docs/player/works.md) |

Core works on its own. Each expansion needs Core, but does not require the other expansions. The **Restless Valheim** pack installs Core, Cook, Plant, Piles, Drawers and Storage with their dependencies. Workshop is currently a separate install.

## Start playing

1. Create a Valheim profile in r2modman or the Thunderstore App.
2. Install **Restless Valheim**, or choose **RestlessCore** and the expansions you want. The manager installs dependencies.
3. Launch modded and open **F8**, or **Restless** from the pause menu.

Install the same released plugin versions on every participating client and the server. Gameplay settings are controlled by the host/server; personal controls and interface preferences remain local.

[Installation and updates](docs/player/install.md) · [Controls and settings](docs/player/core.md) · [Troubleshooting](docs/player/troubleshooting.md)

The older **RestlessValheim** and **RestlessQOL** Thunderstore listings are deprecated. Use **Restless_Valheim** for this collection and **RestlessCore** for its foundation.

## Development

This repository contains all seven plugins, the package sources and the player guides. `catalogue.yaml` records Core feature decisions; `cook.yaml` defines Cook's recipe graph; `versions.yaml` owns dependency and package version pins.

[Build and release instructions](docs/maintainers/building.md) · [Public documentation workflow](docs/maintainers/public-docs.md) · [Screenshot brief](docs/maintainers/screenshots.md)

Public documentation is maintained here on GitHub and linked from Thunderstore. The guides describe current main; use the documentation shipped with an older package when checking its historical behaviour.
