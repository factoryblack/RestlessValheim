# RestlessCore guide

Core brings everyday Valheim work and its interface into one configurable system.

> (screenshot coming) — HUD, food/status column and populated hotbar.

## Global Reach

Open **F8 → Core → Storage or Building**. **Use global reach** is off by default, preserving your existing settings. Turning it on selects one host-controlled profile:

| Profile | Storage and station use/build | Repair | Sealing outside a station | Crop/hive harvest |
| --- | --- | --- | --- | --- |
| Small | 15 m | 8 m | 8 m | 4 m |
| Standard | 25 m | 15 m | 15 m | 6 m |
| Far | 50 m | 25 m | 25 m | 8 m |
| Wide | 80 m | 35 m | 35 m | 10 m |
| Maximum | 120 m | 40 m | 40 m | 12 m |

The shared distance controls chest/drawer discovery, supplies, kitchen/workshop machine discovery and non-isolated piles. Crafting, building and Storage search around the player; Cook around its preparation table; Workshop around its board; pets around the animal. Station building uses horizontal distance. Repairs start at the repaired piece, and harvesting starts at the picked crop or hive. Sealing uses the covering station's horizontal build area, falling back to the smaller seal sphere outside it.

Features keep their own enabled switches. Their distance sliders are disabled while Global Reach controls them and show the effective value; disabled features also disable their distance controls. Turning Global Reach off restores saved custom values. Piles can explicitly isolate their range; that exception applies consistently to browsing, crafting and production. Crop spacing and field snapping remain personal. Host permissions still apply.

The Cookbook, Workshop and Storage windows retain their local 5 m interaction guard; pile windows retain 4 m. These interaction guards are independent of resource discovery and native station use/build coverage.

Only loaded objects participate. Resource and action searches check object centres after collider discovery. Station circles refresh with profile changes, and disabling the station override restores native use distances and upgrade scaling. Shrinking reach prevents subsequent withdrawals from stale discovery lists; transfers already accepted complete through the existing reservation/return flow. The 120 m setting still needs live dense-base performance and multiplayer acceptance.

## Nearby storage

Crafting and building use your bag first, then eligible nearby chests for missing materials. The default search range is **20 metres** and the host can change it. Access restrictions still apply; this is not global or unloaded-world storage.

**Leave one** is enabled by default. Material pulls preserve one item in a chest stack so it remains a matching destination. Disable that setting if the host wants fully empty stacks.

| Action | Default control | Rule |
| --- | --- | --- |
| Quick Stack | Backtick, the key left of 1 | Deposits matching bag stacks into chests already holding that item; empty chests are ignored |
| Restock | Shift + Backtick | Tops up stacks already carried; does not fetch a new assortment |
| Lock/unlock a bag cell | Middle mouse button | A cream diamond marks the locked cell |
| Settings | F8 | Host controls gameplay values; local preferences stay personal |

Hotbar, equipment/quick slots and locked bag cells are protected from automatic inventory actions. Locking a cell does not prevent you moving its item manually. There is no automatic bag-sort feature.

Ground-item routing uses matching player-built chests and ignores world loot chests and tombstones. Hungry tames can eat appropriate food from nearby storage. Station interactions can draw eligible inputs/fuel from storage, reducing trips back to the chest. Cook and Works add explicit order management on top of those shared systems.

With Piles installed, resource piles participate in supported storage actions. See [Piles](piles.md) for priority and range rules. Drawers behave as containers; Storage adds a browser.

## Crafting window

Enable the optional **Crafting window** in F8 to use the Craft, Upgrade and Stations views. Search/filter recipes, save favourites, inspect material counts and compare upgrades. Stations shows connected extensions, nearby extensions and known build options. Manual repair uses the game's normal repair action.

For ordinary crafting, **Batches** selects 1–99 crafts; the materials and produced-item total follow that selection. **Max** checks current materials and output space, including nearby storage enabled by your settings. Each queued craft uses normal timing and checks requirements again. Cancel or close stops the remaining queue and keeps completed items. Equipment upgrades remain one at a time. The full-inventory button returns to normal inventory management. Disabling the window restores the existing panel.

## Equipment and inventory

Dedicated worn slots sit beside the main bag. Three quick slots have default **Z / X / C** bindings. Extra slots have their own gravestone handling.

Stack sizes default to **2×** vanilla, configurable from **1× to 10×** by the host. Items that do not normally stack remain single items.

> (screenshot coming) — Inventory with occupied equipment/quick slots and a set-piece tooltip.

Tooltips separate item stats from set bonuses and show quality marks. The loadout view lets you inspect totals and their sources. The shared UI is designed to accept information from other Restless modules; it does not itself add rarity mechanics or a skill tree.

## Building

- Repair nearby pieces with an area repair action rather than clicking each one.
- Use wider configurable station ranges; required stations and upgrade levels still matter.
- Automatically repair equipment when using a station capable of repairing it.
- **Alt + hammer click** seals player-built wood against rain for one resin per eligible piece. Already sealed pieces are skipped. Current main uses the covering station's build range, or the configured fallback radius outside station coverage.
- Extend terrain dig/raise limits; the default maximum change is **20 metres** from the original terrain.

Normal hammer clicks retain their ordinary place/repair action.

## Player and world options

Tools can remain equipped while swimming. Loaded crossbows retain their bolt when swapped, and axes keep their combo while chopping.

Tames do not damage other tames or players, and ballistae ignore tames when the protection setting is enabled. **Players can still kill their own tames.** Player-versus-player behaviour remains vanilla.

Death pins clear when a tombstone is emptied; a death leaving nothing to recover does not keep a recovery pin.

Campfires, hearths and held torches can remain lit. This does not remove kiln or smelter fuel requirements. Most dropped items float by default; bronze and iron nails are on the default sink list.

The host can configure the network send-window setting and achievement/cheated-mark handling. These, stack sizes, terrain limits, persistent fires and storage automation affect gameplay: review the options together before starting a shared world.

## Make the interface yours

HUD elements, minimap presentation, inventory/crafting, item tooltips, build information, map and menu screens share the Restless visual language. Notices stack and repeated messages consolidate, so rapid interaction does not erase useful feedback.

Use F8 to choose HUD and screen treatments. The Character page shows recorded activity; it is not an XP or skill-tree system.

> (screenshot coming) — F8 overview and one expanded Core settings section.

[Install](install.md) · [Troubleshoot](troubleshooting.md) · [All guides](README.md)

