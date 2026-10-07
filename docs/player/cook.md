# RestlessCook guide

**Cook the haul, make meals, turn meals into feasts.**

Cook expands the food chain from Meadows through Ashlands and gives your kitchen a central order interface. Deep North dishes are not included in this expansion.

> (screenshot coming) — Cookbook with a feast selected and its ingredient chain visible.

## Your first kitchen

1. Gather ingredients and cook meat/fish into the cuts required by your recipes.
2. Build a cauldron and nearby cooking stations. Discover ingredients and add the station upgrades your dishes need.
3. Craft the **food preparation table** at a workbench: 10 wood, 8 resin and 6 leather scraps.
4. Use the table to open the **Cookbook**. Select a known dish, inspect what it needs and place an order.
5. Craft the **serving tray** at a workbench: 6 wood, 4 leather scraps and 2 resin. Use it to place the finished feast board, then eat from the placed feast.

The table and serving tray become available in Meadows. Early access to the furniture does not unlock every dish or bypass station levels.

## Recipes and food

Prepared meals feed into feast recipes rather than being isolated end products. Existing vanilla foods remain part of the chain, and selected vanilla recipes now require cooked protein instead of raw meat.

Custom feasts focus on health or stamina; Mistlands and Ashlands feasts also include eitr. Vanilla feasts remain available. Hidden Hills and Cinder sideboards bundle meals for later feast recipes; **sideboards are not edible food**.

Meadows and Black Forest custom feasts do not need Bog Witch spices. Later feasts retain their spice requirements.

[Browse recipes by biome](recipes/README.md) for ingredients, output amounts, station levels, food values and what each dish is used in.

## Using the Cookbook

Search and filter for **feasts, meals, prep, bait or meads**. Prep is dough, the uncooked dishes, and the sideboards. Bait is its own list. Mead bases stay under Meads. Select a dish to see its requirements and the preparation steps leading to it. Amounts on hand can exceed the requirement: 8/6 means you have eight and need six.

Orders draw from eligible nearby stock, including supported resource piles, load nearby cooking stations, collect prepared results and use those results in later recipe steps. The finished feast is handed to the player who ordered it.

> (screenshot coming) — Orders with an active multi-step preparation track.

The Orders view shows preparation progress for each step. Cauldron, mead-kettle and preparation crafts take their configured crafting time at a specific physical station. Existing dishes in your bag are not treated as the finished output of a new order.

## Stations still do the work

Each machine step uses the placed station and its native processing state:

- **Racks:** meat and fish occupy real hooks and use the rack's cooking time, fire and fuel requirements.
- **Ovens:** baked foods first become dough or an uncooked dish at the required preparation station, then enter a real oven queue. The oven consumes fuel and uses its normal baking time.
- **Meads:** the required kettle prepares a base, which occupies one real fermenter. Fermentation uses the barrel's normal world-time timer, roof/cover requirements and batch yield, including its short tapping delay.
- **Cauldron and preparation:** these stations have no native unattended queue. Cook gives each physical station one preparation job using the recipe's normal crafting duration and upgrade requirements. Losing fire or shelter pauses that job; it does not move to a different station.

Prepared results are collected into the table's pantry and feed the next recipe step. Existing cooked stock can satisfy an ingredient without cooking it again. A bigger order does not create extra hooks or remove station requirements.

The stone oven belongs to this kitchen. Kilns, smelters and other workshop machines belong to [Workshop](works.md).

If an order is waiting, inspect its ingredients and station requirements before placing another order. Keep the relevant stations in range and supplied.

## Cancellation and output

Cancelling an active timed cauldron/preparation craft returns its ingredients. Food already prepared can remain in the table's pantry, where it can be inspected and taken. An order's completed feast goes to its ordering player; cancellation is not a way to undo every earlier machine operation. Loaded rack, oven and fermenter ingredients continue their native process; cancellation does not refund those inputs.

Keep inventory space available and check the order/pantry if delivery cannot complete. In multiplayer, another player taking control of the table can affect which player its owned processing follows. Multiplayer ownership edge cases still need broader playtesting.

## Settings

Open **F8 → RestlessCook** for the local Cookbook toggle. Disabling the window is a presentation preference, not a promise that existing kitchen orders are cancelled. Core's host-controlled storage rules continue to apply.

> (screenshot coming) — Different finished feast boards placed on a well-lit dining table.

[Recipe reference](recipes/README.md) · [Install](install.md) · [Troubleshoot](troubleshooting.md) · [All guides](README.md)
