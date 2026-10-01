# RestlessCore guide

Core brings everyday Valheim work and its interface into one configurable system.

> (screenshot coming) — HUD, food/status column and populated hotbar.

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

