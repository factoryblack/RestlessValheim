# RestlessCook

Valheim 1.0 cooking for Restless. **Cook the haul, make meals, turn meals into feasts.**

Hard-depends on **RestlessCore**, **BepInExPack 5.4.2350**, and **Jötunn 2.30.1**. Everyone on the server needs this mod (minor version match).

v0.1 is Meadows through Ashlands. Deep North waits.

This page is the recipe wiki. Isolated plates are the full renders; the in-game slots use 256px copies of the same art. The graph itself is [`cook.yaml`](https://github.com/factoryblack/RestlessValheim/blob/main/cook.yaml).

## How it works

1. Cook the raw meat and fish first.
2. Turn those cuts (and forage) into prepared meals.
3. Assemble meals into feast boards. Two sideboards bundle leftover Mistlands and Ashlands plates so they still reach a vanilla feast.

Every prepared meal still reaches at least one feast. Existing vanilla feasts stay valid sinks. Protein recipes that already exist are rewritten in place so they ask for the cooked cut.

Vanilla feasts stay the balanced white fork. Custom feast A is health. Custom feast B is stamina. Mistlands and Ashlands custom feasts also carry eitr. Custom feasts consume finished meals, so they beat the vanilla board rather than matching it. Hidden Hills and Cinder sideboards are assembly pieces, not food.

The Food preparation table and Serving tray are the vanilla pieces, unlocked in Meadows: 10 wood / 8 resin / 6 leather scraps for the table, 6 wood / 4 leather scraps / 2 resin for the tray at a workbench. Meadows and Black Forest custom boards do not ask for Bog Witch spices. Later custom feasts still do.

## The matrix

All 81 graph rows. Custom dishes show a thumb; vanilla rewrites and references keep Iron Gate art.

|  | Dish | Kind | Biome | Op | H/S/E | Recipe | Goes into |
| --- | --- | --- | --- | --- | --- | --- | --- |
| ![Hunter's Skillet](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/hunters-skillet.png) | Hunter's Skillet | Meal | Meadows | add | 32/24 · 2 regen · 25m | 1 Cooked boar meat + 1 Grilled neck tail + 2 Mushroom | Hunter's Hearth Board |
| ![Herb-Roasted Venison](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/herb-roasted-venison.png) | Herb-Roasted Venison | Meal | Meadows | add | 40/18 · 3 regen · 25m | 1 Cooked deer meat + 2 Mushroom + 2 Dandelion | Hunter's Hearth Board |
| ![Honeyed Mushrooms](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/honeyed-mushrooms.png) | Honeyed Mushrooms | Meal | Meadows | add | 18/42 · 2 regen · 25m | 3 Mushroom + 2 Honey + 1 Dandelion | Honeyed Forager's Table |
| ![Bear & Mushroom Stew](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bear-mushroom-stew.png) | Bear & Mushroom Stew | Meal | Black Forest | add | 48/18 · 3 regen · 25m | 1 Cooked bear meat + 2 Yellow mushroom + 1 Carrot | Forester's Game Supper |
| ![Forest Root Medley](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/forest-root-medley.png) | Forest Root Medley | Meal | Black Forest | add | 18/50 · 2 regen · 25m | 2 Carrot + 2 Yellow mushroom + 1 Thistle | Forager's Breakfast Board |
| ![Sausage & Turnip Bake](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/sausage-turnip-bake.png) | Sausage & Turnip Bake | Meal | Swamp | add | 60/22 · 4 regen · 30m | 2 Sausages + 2 Turnip + 1 Thistle | Cryptkeeper's Supper |
| ![Bogroot Mash](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bogroot-mash.png) | Bogroot Mash | Meal | Swamp | add | 22/60 · 2 regen · 30m | 3 Turnip + 1 Yellow mushroom + 1 Honey | Bog Harvester's Table |
| ![Serpent Steak](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/serpent-steak.png) | Serpent Steak | Meal | Ocean | add | 75/28 · 4 regen · 30m | 1 Cooked serpent meat + 1 Thistle + 1 Honey | Longship Leviathan Spread |
| ![Fisherman's Chowder](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/fishermans-chowder.png) | Fisherman's Chowder | Meal | Ocean | add | 28/70 · 3 regen · 30m | 2 Cooked fish + 1 Turnip + 1 Fresh seaweed | Longship Leviathan Spread, Deckhand's Provision |
| ![Pickled Catch](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/pickled-catch.png) | Pickled Catch | Meal | Ocean | add | 25/65 · 3 regen · 30m | 2 Cooked fish + 1 Fresh seaweed + 2 Thistle | Deckhand's Provision |
| ![Wolf Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/wolf-roast.png) | Wolf Roast | Meal | Mountain | add | 72/24 · 4 regen · 30m | 1 Cooked wolf meat + 2 Onion + 1 Honey | Wolf King's Carving Board |
| ![Frostberry Preserve](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/frostberry-preserve.png) | Frostberry Preserve | Meal | Mountain | add | 24/72 · 2 regen · 30m | 3 Blueberries + 2 Honey + 1 Freeze gland | Peak Runner's Supper |
| ![Lox Rib Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/lox-rib-roast.png) | Lox Rib Roast | Meal | Plains | add | 82/27 · 5 regen · 30m | 1 Cooked lox meat + 2 Cloudberry + 1 Onion | Jarl's Lox Table |
| ![Barley Root Bake](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/barley-root-bake.png) | Barley Root Bake | Meal | Plains | add | 27/82 · 3 regen · 30m | 2 Barley flour + 2 Onion + 2 Carrot + 1 Cloudberry | Golden Harvest Board |
| ![Sap-Glazed Garden Medley](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/sap-glazed-garden-medley.png) | Sap-Glazed Garden Medley | Meal | Mistlands | add | 32/85/15 · 4 regen · 30m | 1 Salad + 2 Sap + 2 Jotun puffs | Mistwalker's Garden Table |
| ![Magecap Tart](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/magecap-tart.png) | Magecap Tart | Meal | Mistlands | add | 30/18/90 · 4 regen · 30m | 2 Magecap + 2 Barley flour + 1 Egg + 1 Royal jelly | Mistwalker's Garden Table |
| ![Bonemaw & Fiddlehead Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bonemaw-fiddlehead-roast.png) | Bonemaw & Fiddlehead Roast | Meal | Ashlands | add | 110/36 · 7 regen · 30m | 1 Cooked bonemaw meat + 2 Fiddlehead + 2 Vineberry | Emberlord's Carving Table |
| ![Hunter's Hearth Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/hunters-hearth-board.png) | Hunter's Hearth Board | Feast | Meadows | add | 55/30 · 3 regen · 50m | 2 Herb-Roasted Venison + 2 Hunter's Skillet + 2 Cooked boar meat | — |
| ![Honeyed Forager's Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/honeyed-foragers-table.png) | Honeyed Forager's Table | Feast | Meadows | add | 30/55 · 3 regen · 50m | 3 Honeyed Mushrooms + 6 Honey + 8 Raspberry | — |
| ![Forester's Game Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/foresters-game-supper.png) | Forester's Game Supper | Feast | Black Forest | add | 60/35 · 4 regen · 50m | 2 Bear & Mushroom Stew + 1 Pulled Bear + 1 Minced Meat Sauce + 2 Boar Jerky | — |
| ![Forager's Breakfast Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/foragers-breakfast-board.png) | Forager's Breakfast Board | Feast | Black Forest | add | 35/60 · 3 regen · 50m | 2 Forest Root Medley + 2 Carrot Soup + 4 Queen's Jam | — |
| ![Cryptkeeper's Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/cryptkeepers-supper.png) | Cryptkeeper's Supper | Feast | Swamp | add | 70/40 · 4 regen · 50m | 2 Sausage & Turnip Bake + 2 Black Soup + 4 Sausages + 1 Woodland Herb Blend | — |
| ![Bog Harvester's Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bog-harvesters-table.png) | Bog Harvester's Table | Feast | Swamp | add | 40/70 · 3 regen · 50m | 2 Bogroot Mash + 2 Turnip Stew + 2 Muckshake + 1 Woodland Herb Blend | — |
| ![Longship Leviathan Spread](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/longship-leviathan-spread.png) | Longship Leviathan Spread | Feast | Ocean | add | 80/45 · 5 regen · 50m | 2 Serpent Steak + 2 Serpent Stew + 2 Fisherman's Chowder + 1 Seafarer's Herbs | — |
| ![Deckhand's Provision](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/deckhands-provision.png) | Deckhand's Provision | Feast | Ocean | add | 45/80 · 4 regen · 50m | 2 Pickled Catch + 2 Fisherman's Chowder + 2 Queen's Jam + 1 Seafarer's Herbs | — |
| ![Wolf King's Carving Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/wolf-kings-carving-board.png) | Wolf King's Carving Board | Feast | Mountain | add | 85/50 · 5 regen · 50m | 2 Wolf Roast + 2 Wolf Skewer + 4 Wolf Jerky + 1 Mountain Peak Pepper Powder | — |
| ![Peak Runner's Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/peak-runners-supper.png) | Peak Runner's Supper | Feast | Mountain | add | 50/85 · 4 regen · 50m | 2 Frostberry Preserve + 2 Eyescream + 2 Onion Soup + 1 Mountain Peak Pepper Powder | — |
| ![Jarl's Lox Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/jarls-lox-table.png) | Jarl's Lox Table | Feast | Plains | add | 95/55 · 6 regen · 50m | 2 Lox Rib Roast + 2 Lox Meat Pie + 2 Fish Wraps + 1 Grasslands Herbalist Harvest | — |
| ![Golden Harvest Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/golden-harvest-board.png) | Golden Harvest Board | Feast | Plains | add | 55/95 · 5 regen · 50m | 2 Barley Root Bake + 2 Blood Pudding + 2 Bread + 1 Grasslands Herbalist Harvest | — |
| ![Queen's Hunting Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/queens-hunting-table.png) | Queen's Hunting Table | Feast | Mistlands | add | 100/55/30 · 6 regen · 50m | 2 Misthare Supreme + 2 Meat Platter + 2 Honey Glazed Chicken + 1 Herbs of the Hidden Hills | — |
| ![Mistwalker's Garden Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/mistwalkers-garden-table.png) | Mistwalker's Garden Table | Feast | Mistlands | add | 55/100/35 · 5 regen · 50m | 2 Sap-Glazed Garden Medley + 2 Magecap Tart + 2 Mushroom Omelette + 1 Herbs of the Hidden Hills | — |
| ![Emberlord's Carving Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/emberlords-carving-table.png) | Emberlord's Carving Table | Feast | Ashlands | add | 115/65/35 · 7 regen · 50m | 2 Bonemaw & Fiddlehead Roast + 2 Piquant Pie + 2 Mashed Meat + 2 Fiery Svinstew + 1 Fiery Spice Powder | — |
| ![Cinder Runner's Spread](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/cinder-runners-spread.png) | Cinder Runner's Spread | Feast | Ashlands | add | 65/115/40 · 6 regen · 50m | 2 Roasted Crust Pie + 3 Scorching Medley + 2 Sparkling Shroomshake + 1 Fiery Spice Powder | — |
| ![Hidden Hills Sideboard](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/hidden-hills-sideboard.png) | Hidden Hills Sideboard | Sideboard | Mistlands | add | not edible | 1 Fish 'n' Bread + 2 Seeker Aspic + 1 Stuffed Mushroom + 1 Yggdrasil Porridge + 1 Cooked Egg | Mushrooms Galore à la Mistlands |
| ![Cinder Sideboard](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/cinder-sideboard.png) | Cinder Sideboard | Sideboard | Ashlands | add | not edible | 1 Spicy Marmalade + 1 Sizzling Berry Broth + 1 Marinated Greens | Ashlands Gourmet Bowl |
|  | Minced Meat Sauce | Meal | Black Forest | rewrite | vanilla | 1 Cooked boar meat + 1 Grilled neck tail + 1 Carrot | Forester's Game Supper |
|  | Boar Jerky | Meal | Black Forest | rewrite | vanilla | 1 Cooked boar meat + 1 Honey → 2 | Forester's Game Supper |
|  | Sausages | Meal | Swamp | rewrite | vanilla | 4 Entrails + 1 Cooked boar meat + 1 Thistle → 4 | Cryptkeeper's Supper, Swamp Dweller's Delight |
|  | Turnip Stew | Meal | Swamp | rewrite | vanilla | 1 Cooked boar meat + 3 Turnip | Bog Harvester's Table, Swamp Dweller's Delight |
|  | Wolf Jerky | Meal | Mountain | rewrite | vanilla | 1 Cooked wolf meat + 1 Honey → 2 | Wolf King's Carving Board |
|  | Wolf Skewer | Meal | Mountain | rewrite | vanilla | 1 Cooked wolf meat + 2 Mushroom + 1 Onion | Wolf King's Carving Board, Hearty Mountain Logger's Stew |
|  | Lox Meat Pie | Meal | Plains | rewrite | vanilla | 2 Cloudberry + 2 Cooked lox meat + 4 Barley flour | Jarl's Lox Table, Plains Pie Picnic |
|  | Meat Platter | Meal | Mistlands | rewrite | vanilla | 1 Cooked seeker meat + 1 Cooked lox meat + 1 Cooked hare meat | Queen's Hunting Table |
|  | Honey Glazed Chicken | Meal | Mistlands | rewrite | vanilla | 1 Cooked chicken + 3 Honey + 2 Jotun puffs | Queen's Hunting Table |
|  | Misthare Supreme | Meal | Mistlands | rewrite | vanilla | 1 Cooked hare meat + 3 Jotun puffs + 2 Carrot | Queen's Hunting Table, Mushrooms Galore à la Mistlands |
|  | Seeker Aspic | Meal | Mistlands | rewrite | vanilla | 2 Cooked seeker meat + 2 Magecap + 2 Royal jelly → 2 | Hidden Hills Sideboard |
|  | Fiery Svinstew | Meal | Ashlands | rewrite | vanilla | 1 Cooked asksvin meat + 2 Vineberry + 1 Smoke puff | Emberlord's Carving Table |
|  | Mashed Meat | Meal | Ashlands | rewrite | vanilla | 1 Cooked asksvin meat + 1 Cooked volture meat + 1 Fiddlehead | Emberlord's Carving Table |
|  | Piquant Pie | Meal | Ashlands | rewrite | vanilla | 2 Vineberry + 2 Cooked asksvin meat + 4 Barley flour | Emberlord's Carving Table |
|  | Mushrooms Galore à la Mistlands | Feast | Mistlands | rewrite | vanilla | 1 Misthare Supreme + 3 Cooked seeker meat + 1 Hidden Hills Sideboard + 1 Herbs of the Hidden Hills | — |
|  | Ashlands Gourmet Bowl | Feast | Ashlands | rewrite | vanilla | 3 Cooked asksvin meat + 1 Cinder Sideboard + 2 Scorching Medley + 1 Fiery Spice Powder | — |
|  | Deer Stew | Meal | Black Forest | reference | vanilla | 1 Cooked deer meat + 1 Blueberries + 1 Carrot | Black Forest Buffet Platter |
|  | Pulled Bear | Meal | Black Forest | reference | vanilla | 1 Cooked bear meat + 2 Carrot + 1 Blueberries | Forester's Game Supper |
|  | Carrot Soup | Meal | Black Forest | reference | vanilla | 1 Mushroom + 3 Carrot | Forager's Breakfast Board |
|  | Queen's Jam | Meal | Black Forest | reference | vanilla | 8 Raspberry + 6 Blueberries → 4 | Forager's Breakfast Board, Deckhand's Provision |
|  | Black Soup | Meal | Swamp | reference | vanilla | 1 Bloodbag + 1 Honey + 1 Turnip | Cryptkeeper's Supper |
|  | Muckshake | Meal | Swamp | reference | vanilla | 1 Ooze + 2 Raspberry + 2 Blueberries | Bog Harvester's Table |
|  | Serpent Stew | Meal | Ocean | reference | vanilla | 1 Mushroom + 1 Cooked serpent meat + 2 Honey | Longship Leviathan Spread |
|  | Onion Soup | Meal | Mountain | reference | vanilla | 3 Onion | Peak Runner's Supper |
|  | Eyescream | Meal | Mountain | reference | vanilla | 3 Greydwarf Eye + 1 Freeze gland | Peak Runner's Supper |
|  | Fish Wraps | Meal | Plains | reference | vanilla | 2 Cooked fish + 4 Barley flour | Jarl's Lox Table |
|  | Blood Pudding | Meal | Plains | reference | vanilla | 2 Thistle + 2 Bloodbag + 4 Barley flour | Golden Harvest Board |
|  | Bread | Meal | Plains | reference | vanilla | 10 Barley flour → 2 | Golden Harvest Board |
|  | Mushroom Omelette | Meal | Mistlands | reference | vanilla | 3 Egg + 3 Jotun puffs | Mistwalker's Garden Table |
|  | Salad | Meal | Mistlands | reference | vanilla | 3 Jotun puffs + 3 Onion + 3 Cloudberry → 3 | Sap-Glazed Garden Medley |
|  | Fish 'n' Bread | Meal | Mistlands | reference | vanilla | 1 Fish9 + 2 Bread Dough | Hidden Hills Sideboard |
|  | Stuffed Mushroom | Meal | Mistlands | reference | vanilla | 3 Magecap + 1 Giant Blood Sack + 2 Turnip | Hidden Hills Sideboard |
|  | Yggdrasil Porridge | Meal | Mistlands | reference | vanilla | 4 Sap + 3 Barley + 2 Royal jelly | Hidden Hills Sideboard |
|  | Cooked Egg | Meal | Mistlands | reference | vanilla | 1 Egg | Hidden Hills Sideboard |
|  | Roasted Crust Pie | Meal | Ashlands | reference | vanilla | 2 Vineberry + 1 Volture Egg + 4 Barley flour | Cinder Runner's Spread |
|  | Scorching Medley | Meal | Ashlands | reference | vanilla | 3 Jotun puffs + 3 Onion + 3 Fiddlehead → 3 | Cinder Runner's Spread, Ashlands Gourmet Bowl |
|  | Sparkling Shroomshake | Meal | Ashlands | reference | vanilla | 4 Sap + 2 Vineberry + 2 Smoke puff + 2 Magecap | Cinder Runner's Spread |
|  | Spicy Marmalade | Meal | Ashlands | reference | vanilla | 3 Vineberry + 1 Honey + 1 Fiddlehead | Cinder Sideboard |
|  | Sizzling Berry Broth | Meal | Ashlands | reference | vanilla | 3 Sap + 2 Fiddlehead + 2 Vineberry | Cinder Sideboard |
|  | Marinated Greens | Meal | Ashlands | reference | vanilla | 3 Sap + 2 Magecap + 2 Fiddlehead + 2 Smoke puff | Cinder Sideboard |
|  | Whole Roasted Meadow Boar | Feast | Meadows | reference | vanilla | 2 Cooked deer meat + 5 Cooked boar meat + 4 Dandelion + 1 Woodland Herb Blend | — |
|  | Black Forest Buffet Platter | Feast | Black Forest | reference | vanilla | 3 Deer Stew + 5 Thistle + 4 Queen's Jam + 1 Woodland Herb Blend | — |
|  | Swamp Dweller's Delight | Feast | Swamp | reference | vanilla | 8 Sausages + 4 Bloodbag + 2 Turnip Stew + 1 Woodland Herb Blend | — |
|  | Sailor's Bounty | Feast | Ocean | reference | vanilla | 5 Cooked fish + 4 Thistle + 2 Cooked serpent meat + 1 Seafarer's Herbs | — |
|  | Hearty Mountain Logger's Stew | Feast | Mountain | reference | vanilla | 2 Wolf Skewer + 3 Onion Soup + 4 Carrot + 1 Mountain Peak Pepper Powder | — |
|  | Plains Pie Picnic | Feast | Plains | reference | vanilla | 3 Bread + 2 Lox Meat Pie + 5 Cloudberry + 1 Grasslands Herbalist Harvest | — |

## Plates

Full-size isolated renders, grouped by biome. Meals first, then feasts, then sideboards.

### Meadows

#### Meals

**Hunter's Skillet**

![Hunter's Skillet](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/hunters-skillet.png)

- Recipe: 1 Cooked boar meat + 1 Grilled neck tail + 2 Mushroom
- Station: Cauldron 1
- Stats: 32/24 · 2 regen · 25m
- Goes into: Hunter's Hearth Board

**Herb-Roasted Venison**

![Herb-Roasted Venison](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/herb-roasted-venison.png)

- Recipe: 1 Cooked deer meat + 2 Mushroom + 2 Dandelion
- Station: Cauldron 1
- Stats: 40/18 · 3 regen · 25m
- Goes into: Hunter's Hearth Board

**Honeyed Mushrooms**

![Honeyed Mushrooms](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/honeyed-mushrooms.png)

- Recipe: 3 Mushroom + 2 Honey + 1 Dandelion
- Station: Cauldron 1
- Stats: 18/42 · 2 regen · 25m
- Goes into: Honeyed Forager's Table

#### Feasts

**Hunter's Hearth Board**

![Hunter's Hearth Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/hunters-hearth-board.png)

- Recipe: 2 Herb-Roasted Venison + 2 Hunter's Skillet + 2 Cooked boar meat
- Station: Food preparation table 1
- Stats: 55/30 · 3 regen · 50m
- Goes into: —

**Honeyed Forager's Table**

![Honeyed Forager's Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/honeyed-foragers-table.png)

- Recipe: 3 Honeyed Mushrooms + 6 Honey + 8 Raspberry
- Station: Food preparation table 1
- Stats: 30/55 · 3 regen · 50m
- Goes into: —


### Black Forest

#### Meals

**Bear & Mushroom Stew**

![Bear & Mushroom Stew](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/bear-mushroom-stew.png)

- Recipe: 1 Cooked bear meat + 2 Yellow mushroom + 1 Carrot
- Station: Cauldron 2
- Stats: 48/18 · 3 regen · 25m
- Goes into: Forester's Game Supper

**Forest Root Medley**

![Forest Root Medley](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/forest-root-medley.png)

- Recipe: 2 Carrot + 2 Yellow mushroom + 1 Thistle
- Station: Cauldron 2
- Stats: 18/50 · 2 regen · 25m
- Goes into: Forager's Breakfast Board

#### Feasts

**Forester's Game Supper**

![Forester's Game Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/foresters-game-supper.png)

- Recipe: 2 Bear & Mushroom Stew + 1 Pulled Bear + 1 Minced Meat Sauce + 2 Boar Jerky
- Station: Food preparation table 1
- Stats: 60/35 · 4 regen · 50m
- Goes into: —

**Forager's Breakfast Board**

![Forager's Breakfast Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/foragers-breakfast-board.png)

- Recipe: 2 Forest Root Medley + 2 Carrot Soup + 4 Queen's Jam
- Station: Food preparation table 1
- Stats: 35/60 · 3 regen · 50m
- Goes into: —


### Swamp

#### Meals

**Sausage & Turnip Bake**

![Sausage & Turnip Bake](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/sausage-turnip-bake.png)

- Recipe: 2 Sausages + 2 Turnip + 1 Thistle
- Station: Cauldron 3
- Stats: 60/22 · 4 regen · 30m
- Goes into: Cryptkeeper's Supper

**Bogroot Mash**

![Bogroot Mash](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/bogroot-mash.png)

- Recipe: 3 Turnip + 1 Yellow mushroom + 1 Honey
- Station: Cauldron 3
- Stats: 22/60 · 2 regen · 30m
- Goes into: Bog Harvester's Table

#### Feasts

**Cryptkeeper's Supper**

![Cryptkeeper's Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/cryptkeepers-supper.png)

- Recipe: 2 Sausage & Turnip Bake + 2 Black Soup + 4 Sausages + 1 Woodland Herb Blend
- Station: Food preparation table 1
- Stats: 70/40 · 4 regen · 50m
- Goes into: —

**Bog Harvester's Table**

![Bog Harvester's Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/bog-harvesters-table.png)

- Recipe: 2 Bogroot Mash + 2 Turnip Stew + 2 Muckshake + 1 Woodland Herb Blend
- Station: Food preparation table 1
- Stats: 40/70 · 3 regen · 50m
- Goes into: —


### Ocean

#### Meals

**Serpent Steak**

![Serpent Steak](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/serpent-steak.png)

- Recipe: 1 Cooked serpent meat + 1 Thistle + 1 Honey
- Station: Cauldron 3
- Stats: 75/28 · 4 regen · 30m
- Goes into: Longship Leviathan Spread

**Fisherman's Chowder**

![Fisherman's Chowder](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/fishermans-chowder.png)

- Recipe: 2 Cooked fish + 1 Turnip + 1 Fresh seaweed
- Station: Cauldron 3
- Stats: 28/70 · 3 regen · 30m
- Goes into: Longship Leviathan Spread, Deckhand's Provision

**Pickled Catch**

![Pickled Catch](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/pickled-catch.png)

- Recipe: 2 Cooked fish + 1 Fresh seaweed + 2 Thistle
- Station: Cauldron 3
- Stats: 25/65 · 3 regen · 30m
- Goes into: Deckhand's Provision

#### Feasts

**Longship Leviathan Spread**

![Longship Leviathan Spread](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/longship-leviathan-spread.png)

- Recipe: 2 Serpent Steak + 2 Serpent Stew + 2 Fisherman's Chowder + 1 Seafarer's Herbs
- Station: Food preparation table 1
- Stats: 80/45 · 5 regen · 50m
- Goes into: —

**Deckhand's Provision**

![Deckhand's Provision](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/deckhands-provision.png)

- Recipe: 2 Pickled Catch + 2 Fisherman's Chowder + 2 Queen's Jam + 1 Seafarer's Herbs
- Station: Food preparation table 1
- Stats: 45/80 · 4 regen · 50m
- Goes into: —


### Mountain

#### Meals

**Wolf Roast**

![Wolf Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/wolf-roast.png)

- Recipe: 1 Cooked wolf meat + 2 Onion + 1 Honey
- Station: Cauldron 4
- Stats: 72/24 · 4 regen · 30m
- Goes into: Wolf King's Carving Board

**Frostberry Preserve**

![Frostberry Preserve](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/frostberry-preserve.png)

- Recipe: 3 Blueberries + 2 Honey + 1 Freeze gland
- Station: Cauldron 4
- Stats: 24/72 · 2 regen · 30m
- Goes into: Peak Runner's Supper

#### Feasts

**Wolf King's Carving Board**

![Wolf King's Carving Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/wolf-kings-carving-board.png)

- Recipe: 2 Wolf Roast + 2 Wolf Skewer + 4 Wolf Jerky + 1 Mountain Peak Pepper Powder
- Station: Food preparation table 1
- Stats: 85/50 · 5 regen · 50m
- Goes into: —

**Peak Runner's Supper**

![Peak Runner's Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/peak-runners-supper.png)

- Recipe: 2 Frostberry Preserve + 2 Eyescream + 2 Onion Soup + 1 Mountain Peak Pepper Powder
- Station: Food preparation table 1
- Stats: 50/85 · 4 regen · 50m
- Goes into: —


### Plains

#### Meals

**Lox Rib Roast**

![Lox Rib Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/lox-rib-roast.png)

- Recipe: 1 Cooked lox meat + 2 Cloudberry + 1 Onion
- Station: Cauldron 4
- Stats: 82/27 · 5 regen · 30m
- Goes into: Jarl's Lox Table

**Barley Root Bake**

![Barley Root Bake](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/barley-root-bake.png)

- Recipe: 2 Barley flour + 2 Onion + 2 Carrot + 1 Cloudberry
- Station: Cauldron 4
- Stats: 27/82 · 3 regen · 30m
- Goes into: Golden Harvest Board

#### Feasts

**Jarl's Lox Table**

![Jarl's Lox Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/jarls-lox-table.png)

- Recipe: 2 Lox Rib Roast + 2 Lox Meat Pie + 2 Fish Wraps + 1 Grasslands Herbalist Harvest
- Station: Food preparation table 1
- Stats: 95/55 · 6 regen · 50m
- Goes into: —

**Golden Harvest Board**

![Golden Harvest Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/golden-harvest-board.png)

- Recipe: 2 Barley Root Bake + 2 Blood Pudding + 2 Bread + 1 Grasslands Herbalist Harvest
- Station: Food preparation table 1
- Stats: 55/95 · 5 regen · 50m
- Goes into: —


### Mistlands

#### Meals

**Sap-Glazed Garden Medley**

![Sap-Glazed Garden Medley](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/sap-glazed-garden-medley.png)

- Recipe: 1 Salad + 2 Sap + 2 Jotun puffs
- Station: Cauldron 5
- Stats: 32/85/15 · 4 regen · 30m
- Goes into: Mistwalker's Garden Table

**Magecap Tart**

![Magecap Tart](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/magecap-tart.png)

- Recipe: 2 Magecap + 2 Barley flour + 1 Egg + 1 Royal jelly
- Station: Cauldron 5
- Stats: 30/18/90 · 4 regen · 30m
- Goes into: Mistwalker's Garden Table

#### Feasts

**Queen's Hunting Table**

![Queen's Hunting Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/queens-hunting-table.png)

- Recipe: 2 Misthare Supreme + 2 Meat Platter + 2 Honey Glazed Chicken + 1 Herbs of the Hidden Hills
- Station: Food preparation table 1
- Stats: 100/55/30 · 6 regen · 50m
- Goes into: —

**Mistwalker's Garden Table**

![Mistwalker's Garden Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/mistwalkers-garden-table.png)

- Recipe: 2 Sap-Glazed Garden Medley + 2 Magecap Tart + 2 Mushroom Omelette + 1 Herbs of the Hidden Hills
- Station: Food preparation table 1
- Stats: 55/100/35 · 5 regen · 50m
- Goes into: —

#### Sideboards

**Hidden Hills Sideboard**

![Hidden Hills Sideboard](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/hidden-hills-sideboard.png)

- Recipe: 1 Fish 'n' Bread + 2 Seeker Aspic + 1 Stuffed Mushroom + 1 Yggdrasil Porridge + 1 Cooked Egg
- Station: Food preparation table 1
- Stats: not edible
- Goes into: Mushrooms Galore à la Mistlands


### Ashlands

#### Meals

**Bonemaw & Fiddlehead Roast**

![Bonemaw & Fiddlehead Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/bonemaw-fiddlehead-roast.png)

- Recipe: 1 Cooked bonemaw meat + 2 Fiddlehead + 2 Vineberry
- Station: Cauldron 6
- Stats: 110/36 · 7 regen · 30m
- Goes into: Emberlord's Carving Table

#### Feasts

**Emberlord's Carving Table**

![Emberlord's Carving Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/emberlords-carving-table.png)

- Recipe: 2 Bonemaw & Fiddlehead Roast + 2 Piquant Pie + 2 Mashed Meat + 2 Fiery Svinstew + 1 Fiery Spice Powder
- Station: Food preparation table 1
- Stats: 115/65/35 · 7 regen · 50m
- Goes into: —

**Cinder Runner's Spread**

![Cinder Runner's Spread](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/cinder-runners-spread.png)

- Recipe: 2 Roasted Crust Pie + 3 Scorching Medley + 2 Sparkling Shroomshake + 1 Fiery Spice Powder
- Station: Food preparation table 1
- Stats: 65/115/40 · 6 regen · 50m
- Goes into: —

#### Sideboards

**Cinder Sideboard**

![Cinder Sideboard](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/wiki/cinder-sideboard.png)

- Recipe: 1 Spicy Marmalade + 1 Sizzling Berry Broth + 1 Marinated Greens
- Station: Food preparation table 1
- Stats: not edible
- Goes into: Ashlands Gourmet Bowl


## Vanilla rewrites

These keep their vanilla identity. Ingredients change in place; there is no second Meat Platter.

| Dish | Biome | Now asks for |
| --- | --- | --- |
| Minced Meat Sauce | Black Forest | 1 Cooked boar meat + 1 Grilled neck tail + 1 Carrot |
| Boar Jerky | Black Forest | 1 Cooked boar meat + 1 Honey → 2 |
| Sausages | Swamp | 4 Entrails + 1 Cooked boar meat + 1 Thistle → 4 |
| Turnip Stew | Swamp | 1 Cooked boar meat + 3 Turnip |
| Wolf Jerky | Mountain | 1 Cooked wolf meat + 1 Honey → 2 |
| Wolf Skewer | Mountain | 1 Cooked wolf meat + 2 Mushroom + 1 Onion |
| Lox Meat Pie | Plains | 2 Cloudberry + 2 Cooked lox meat + 4 Barley flour |
| Meat Platter | Mistlands | 1 Cooked seeker meat + 1 Cooked lox meat + 1 Cooked hare meat |
| Honey Glazed Chicken | Mistlands | 1 Cooked chicken + 3 Honey + 2 Jotun puffs |
| Misthare Supreme | Mistlands | 1 Cooked hare meat + 3 Jotun puffs + 2 Carrot |
| Seeker Aspic | Mistlands | 2 Cooked seeker meat + 2 Magecap + 2 Royal jelly → 2 |
| Fiery Svinstew | Ashlands | 1 Cooked asksvin meat + 2 Vineberry + 1 Smoke puff |
| Mashed Meat | Ashlands | 1 Cooked asksvin meat + 1 Cooked volture meat + 1 Fiddlehead |
| Piquant Pie | Ashlands | 2 Vineberry + 2 Cooked asksvin meat + 4 Barley flour |
| Mushrooms Galore à la Mistlands | Mistlands | 1 Misthare Supreme + 3 Cooked seeker meat + 1 Hidden Hills Sideboard + 1 Herbs of the Hidden Hills |
| Ashlands Gourmet Bowl | Ashlands | 3 Cooked asksvin meat + 1 Cinder Sideboard + 2 Scorching Medley + 1 Fiery Spice Powder |

## Vanilla spices

Not new items. They sit on feast boards.

- Woodland Herb Blend (`SpiceForests`)
- Seafarer's Herbs (`SpiceOceans`)
- Mountain Peak Pepper Powder (`SpiceMountains`)
- Grasslands Herbalist Harvest (`SpicePlains`)
- Herbs of the Hidden Hills (`SpiceMistlands`)
- Fiery Spice Powder (`SpiceAshlands`)

This package does not replace RestlessCore. Install the **Restless Valheim** modpack, or Core then this.
