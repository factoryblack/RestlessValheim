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

All 81 graph rows. Custom dishes use the isolated plate thumbs; vanilla rows use Iron Gate icons.

|  | Dish | Kind | Biome | Recipe | H/S/E | Goes into |
| --- | --- | --- | --- | --- | --- | --- |
| ![Hunter's Skillet](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/hunters-skillet.png) | Hunter's Skillet | Meal | Meadows | 1 Cooked boar meat + 1 Grilled neck tail + 2 Mushroom | 32/24 · 2 regen · 25m | Hunter's Hearth Board |
| ![Herb-Roasted Venison](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/herb-roasted-venison.png) | Herb-Roasted Venison | Meal | Meadows | 1 Cooked deer meat + 2 Mushroom + 2 Dandelion | 40/18 · 3 regen · 25m | Hunter's Hearth Board |
| ![Honeyed Mushrooms](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/honeyed-mushrooms.png) | Honeyed Mushrooms | Meal | Meadows | 3 Mushroom + 2 Honey + 1 Dandelion | 18/42 · 2 regen · 25m | Honeyed Forager's Table |
| ![Bear & Mushroom Stew](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bear-mushroom-stew.png) | Bear & Mushroom Stew | Meal | Black Forest | 1 Cooked bear meat + 2 Yellow mushroom + 1 Carrot | 48/18 · 3 regen · 25m | Forester's Game Supper |
| ![Forest Root Medley](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/forest-root-medley.png) | Forest Root Medley | Meal | Black Forest | 2 Carrot + 2 Yellow mushroom + 1 Thistle | 18/50 · 2 regen · 25m | Forager's Breakfast Board |
| ![Sausage & Turnip Bake](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/sausage-turnip-bake.png) | Sausage & Turnip Bake | Meal | Swamp | 2 Sausages + 2 Turnip + 1 Thistle | 60/22 · 4 regen · 30m | Cryptkeeper's Supper |
| ![Bogroot Mash](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bogroot-mash.png) | Bogroot Mash | Meal | Swamp | 3 Turnip + 1 Yellow mushroom + 1 Honey | 22/60 · 2 regen · 30m | Bog Harvester's Table |
| ![Serpent Steak](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/serpent-steak.png) | Serpent Steak | Meal | Ocean | 1 Cooked serpent meat + 1 Thistle + 1 Honey | 75/28 · 4 regen · 30m | Longship Leviathan Spread |
| ![Fisherman's Chowder](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/fishermans-chowder.png) | Fisherman's Chowder | Meal | Ocean | 2 Cooked fish + 1 Turnip + 1 Fresh seaweed | 28/70 · 3 regen · 30m | Longship Leviathan Spread, Deckhand's Provision |
| ![Pickled Catch](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/pickled-catch.png) | Pickled Catch | Meal | Ocean | 2 Cooked fish + 1 Fresh seaweed + 2 Thistle | 25/65 · 3 regen · 30m | Deckhand's Provision |
| ![Wolf Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/wolf-roast.png) | Wolf Roast | Meal | Mountain | 1 Cooked wolf meat + 2 Onion + 1 Honey | 72/24 · 4 regen · 30m | Wolf King's Carving Board |
| ![Frostberry Preserve](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/frostberry-preserve.png) | Frostberry Preserve | Meal | Mountain | 3 Blueberries + 2 Honey + 1 Freeze gland | 24/72 · 2 regen · 30m | Peak Runner's Supper |
| ![Lox Rib Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/lox-rib-roast.png) | Lox Rib Roast | Meal | Plains | 1 Cooked lox meat + 2 Cloudberry + 1 Onion | 82/27 · 5 regen · 30m | Jarl's Lox Table |
| ![Barley Root Bake](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/barley-root-bake.png) | Barley Root Bake | Meal | Plains | 2 Barley flour + 2 Onion + 2 Carrot + 1 Cloudberry | 27/82 · 3 regen · 30m | Golden Harvest Board |
| ![Sap-Glazed Garden Medley](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/sap-glazed-garden-medley.png) | Sap-Glazed Garden Medley | Meal | Mistlands | 1 Salad + 2 Sap + 2 Jotun puffs | 32/85/15 · 4 regen · 30m | Mistwalker's Garden Table |
| ![Magecap Tart](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/magecap-tart.png) | Magecap Tart | Meal | Mistlands | 2 Magecap + 2 Barley flour + 1 Egg + 1 Royal jelly | 30/18/90 · 4 regen · 30m | Mistwalker's Garden Table |
| ![Bonemaw & Fiddlehead Roast](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bonemaw-fiddlehead-roast.png) | Bonemaw & Fiddlehead Roast | Meal | Ashlands | 1 Cooked bonemaw meat + 2 Fiddlehead + 2 Vineberry | 110/36 · 7 regen · 30m | Emberlord's Carving Table |
| ![Hunter's Hearth Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/hunters-hearth-board.png) | Hunter's Hearth Board | Feast | Meadows | 2 Herb-Roasted Venison + 2 Hunter's Skillet + 2 Cooked boar meat | 55/30 · 3 regen · 50m | — |
| ![Honeyed Forager's Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/honeyed-foragers-table.png) | Honeyed Forager's Table | Feast | Meadows | 3 Honeyed Mushrooms + 6 Honey + 8 Raspberry | 30/55 · 3 regen · 50m | — |
| ![Forester's Game Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/foresters-game-supper.png) | Forester's Game Supper | Feast | Black Forest | 2 Bear & Mushroom Stew + 1 Pulled Bear + 1 Minced Meat Sauce + 2 Boar Jerky | 60/35 · 4 regen · 50m | — |
| ![Forager's Breakfast Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/foragers-breakfast-board.png) | Forager's Breakfast Board | Feast | Black Forest | 2 Forest Root Medley + 2 Carrot Soup + 4 Queen's Jam | 35/60 · 3 regen · 50m | — |
| ![Cryptkeeper's Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/cryptkeepers-supper.png) | Cryptkeeper's Supper | Feast | Swamp | 2 Sausage & Turnip Bake + 2 Black Soup + 4 Sausages + 1 Woodland Herb Blend | 70/40 · 4 regen · 50m | — |
| ![Bog Harvester's Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/bog-harvesters-table.png) | Bog Harvester's Table | Feast | Swamp | 2 Bogroot Mash + 2 Turnip Stew + 2 Muckshake + 1 Woodland Herb Blend | 40/70 · 3 regen · 50m | — |
| ![Longship Leviathan Spread](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/longship-leviathan-spread.png) | Longship Leviathan Spread | Feast | Ocean | 2 Serpent Steak + 2 Serpent Stew + 2 Fisherman's Chowder + 1 Seafarer's Herbs | 80/45 · 5 regen · 50m | — |
| ![Deckhand's Provision](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/deckhands-provision.png) | Deckhand's Provision | Feast | Ocean | 2 Pickled Catch + 2 Fisherman's Chowder + 2 Queen's Jam + 1 Seafarer's Herbs | 45/80 · 4 regen · 50m | — |
| ![Wolf King's Carving Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/wolf-kings-carving-board.png) | Wolf King's Carving Board | Feast | Mountain | 2 Wolf Roast + 2 Wolf Skewer + 4 Wolf Jerky + 1 Mountain Peak Pepper Powder | 85/50 · 5 regen · 50m | — |
| ![Peak Runner's Supper](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/peak-runners-supper.png) | Peak Runner's Supper | Feast | Mountain | 2 Frostberry Preserve + 2 Eyescream + 2 Onion Soup + 1 Mountain Peak Pepper Powder | 50/85 · 4 regen · 50m | — |
| ![Jarl's Lox Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/jarls-lox-table.png) | Jarl's Lox Table | Feast | Plains | 2 Lox Rib Roast + 2 Lox Meat Pie + 2 Fish Wraps + 1 Grasslands Herbalist Harvest | 95/55 · 6 regen · 50m | — |
| ![Golden Harvest Board](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/golden-harvest-board.png) | Golden Harvest Board | Feast | Plains | 2 Barley Root Bake + 2 Blood Pudding + 2 Bread + 1 Grasslands Herbalist Harvest | 55/95 · 5 regen · 50m | — |
| ![Queen's Hunting Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/queens-hunting-table.png) | Queen's Hunting Table | Feast | Mistlands | 2 Misthare Supreme + 2 Meat Platter + 2 Honey Glazed Chicken + 1 Herbs of the Hidden Hills | 100/55/30 · 6 regen · 50m | — |
| ![Mistwalker's Garden Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/mistwalkers-garden-table.png) | Mistwalker's Garden Table | Feast | Mistlands | 2 Sap-Glazed Garden Medley + 2 Magecap Tart + 2 Mushroom Omelette + 1 Herbs of the Hidden Hills | 55/100/35 · 5 regen · 50m | — |
| ![Emberlord's Carving Table](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/emberlords-carving-table.png) | Emberlord's Carving Table | Feast | Ashlands | 2 Bonemaw & Fiddlehead Roast + 2 Piquant Pie + 2 Mashed Meat + 2 Fiery Svinstew + 1 Fiery Spice Powder | 115/65/35 · 7 regen · 50m | — |
| ![Cinder Runner's Spread](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/cinder-runners-spread.png) | Cinder Runner's Spread | Feast | Ashlands | 2 Roasted Crust Pie + 3 Scorching Medley + 2 Sparkling Shroomshake + 1 Fiery Spice Powder | 65/115/40 · 6 regen · 50m | — |
| ![Hidden Hills Sideboard](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/hidden-hills-sideboard.png) | Hidden Hills Sideboard | Sideboard | Mistlands | 1 Fish 'n' Bread + 2 Seeker Aspic + 1 Stuffed Mushroom + 1 Yggdrasil Porridge + 1 Cooked Egg | not edible | Mushrooms Galore à la Mistlands |
| ![Cinder Sideboard](https://raw.githubusercontent.com/factoryblack/RestlessValheim/main/docs/cook/icons/cinder-sideboard.png) | Cinder Sideboard | Sideboard | Ashlands | 1 Spicy Marmalade + 1 Sizzling Berry Broth + 1 Marinated Greens | not edible | Ashlands Gourmet Bowl |
| ![Minced Meat Sauce](https://static.wikia.nocookie.net/valheim/images/d/d3/Minced_Meat_Sauce.png/revision/latest) | Minced Meat Sauce | Meal | Black Forest | 1 Cooked boar meat + 1 Grilled neck tail + 1 Carrot | vanilla | Forester's Game Supper |
| ![Boar Jerky](https://static.wikia.nocookie.net/valheim/images/f/fc/Boar_Jerky.png/revision/latest) | Boar Jerky | Meal | Black Forest | 1 Cooked boar meat + 1 Honey → 2 | vanilla | Forester's Game Supper |
| ![Sausages](https://static.wikia.nocookie.net/valheim/images/a/a0/Sausages.png/revision/latest) | Sausages | Meal | Swamp | 4 Entrails + 1 Cooked boar meat + 1 Thistle → 4 | vanilla | Cryptkeeper's Supper, Swamp Dweller's Delight |
| ![Turnip Stew](https://static.wikia.nocookie.net/valheim/images/8/80/Turnip_Stew.png/revision/latest) | Turnip Stew | Meal | Swamp | 1 Cooked boar meat + 3 Turnip | vanilla | Bog Harvester's Table, Swamp Dweller's Delight |
| ![Wolf Jerky](https://static.wikia.nocookie.net/valheim/images/6/6a/Wolf_Jerky.png/revision/latest) | Wolf Jerky | Meal | Mountain | 1 Cooked wolf meat + 1 Honey → 2 | vanilla | Wolf King's Carving Board |
| ![Wolf Skewer](https://static.wikia.nocookie.net/valheim/images/c/cf/Wolf_Skewer.png/revision/latest) | Wolf Skewer | Meal | Mountain | 1 Cooked wolf meat + 2 Mushroom + 1 Onion | vanilla | Wolf King's Carving Board, Hearty Mountain Logger's Stew |
| ![Lox Meat Pie](https://static.wikia.nocookie.net/valheim/images/1/1f/Lox_Meat_Pie.png/revision/latest) | Lox Meat Pie | Meal | Plains | 2 Cloudberry + 2 Cooked lox meat + 4 Barley flour | vanilla | Jarl's Lox Table, Plains Pie Picnic |
| ![Meat Platter](https://static.wikia.nocookie.net/valheim/images/f/f5/Meat_Platter.png/revision/latest) | Meat Platter | Meal | Mistlands | 1 Cooked seeker meat + 1 Cooked lox meat + 1 Cooked hare meat | vanilla | Queen's Hunting Table |
| ![Honey Glazed Chicken](https://static.wikia.nocookie.net/valheim/images/9/99/Honey_Glazed_Chicken.png/revision/latest) | Honey Glazed Chicken | Meal | Mistlands | 1 Cooked chicken + 3 Honey + 2 Jotun puffs | vanilla | Queen's Hunting Table |
| ![Misthare Supreme](https://static.wikia.nocookie.net/valheim/images/0/02/Misthare_Supreme.png/revision/latest) | Misthare Supreme | Meal | Mistlands | 1 Cooked hare meat + 3 Jotun puffs + 2 Carrot | vanilla | Queen's Hunting Table, Mushrooms Galore à la Mistlands |
| ![Seeker Aspic](https://static.wikia.nocookie.net/valheim/images/b/b2/Seeker_Aspic.png/revision/latest) | Seeker Aspic | Meal | Mistlands | 2 Cooked seeker meat + 2 Magecap + 2 Royal jelly → 2 | vanilla | Hidden Hills Sideboard |
| ![Fiery Svinstew](https://static.wikia.nocookie.net/valheim/images/8/89/Fiery_Svinstew.png/revision/latest) | Fiery Svinstew | Meal | Ashlands | 1 Cooked asksvin meat + 2 Vineberry + 1 Smoke puff | vanilla | Emberlord's Carving Table |
| ![Mashed Meat](https://static.wikia.nocookie.net/valheim/images/7/7a/Mashed_Meat.png/revision/latest) | Mashed Meat | Meal | Ashlands | 1 Cooked asksvin meat + 1 Cooked volture meat + 1 Fiddlehead | vanilla | Emberlord's Carving Table |
| ![Piquant Pie](https://static.wikia.nocookie.net/valheim/images/0/07/Piquant_Pie.png/revision/latest) | Piquant Pie | Meal | Ashlands | 2 Vineberry + 2 Cooked asksvin meat + 4 Barley flour | vanilla | Emberlord's Carving Table |
| ![Mushrooms Galore à la Mistlands](https://static.wikia.nocookie.net/valheim/images/7/76/Mushrooms_Galore_%C3%A1_la_Mistlands.png/revision/latest) | Mushrooms Galore à la Mistlands | Feast | Mistlands | 1 Misthare Supreme + 3 Cooked seeker meat + 1 Hidden Hills Sideboard + 1 Herbs of the Hidden Hills | vanilla | — |
| ![Ashlands Gourmet Bowl](https://static.wikia.nocookie.net/valheim/images/f/f7/Ashlands_Gourmet_Bowl.png/revision/latest) | Ashlands Gourmet Bowl | Feast | Ashlands | 3 Cooked asksvin meat + 1 Cinder Sideboard + 2 Scorching Medley + 1 Fiery Spice Powder | vanilla | — |
| ![Deer Stew](https://static.wikia.nocookie.net/valheim/images/0/0e/Deer_Stew.png/revision/latest) | Deer Stew | Meal | Black Forest | 1 Cooked deer meat + 1 Blueberries + 1 Carrot | vanilla | Black Forest Buffet Platter |
|  | Pulled Bear | Meal | Black Forest | 1 Cooked bear meat + 2 Carrot + 1 Blueberries | vanilla | Forester's Game Supper |
| ![Carrot Soup](https://static.wikia.nocookie.net/valheim/images/c/c9/Carrot_Soup.png/revision/latest) | Carrot Soup | Meal | Black Forest | 1 Mushroom + 3 Carrot | vanilla | Forager's Breakfast Board |
| ![Queen's Jam](https://static.wikia.nocookie.net/valheim/images/0/01/Queen%27s_Jam.png/revision/latest) | Queen's Jam | Meal | Black Forest | 8 Raspberry + 6 Blueberries → 4 | vanilla | Forager's Breakfast Board, Deckhand's Provision |
| ![Black Soup](https://static.wikia.nocookie.net/valheim/images/1/1e/Black_Soup.png/revision/latest) | Black Soup | Meal | Swamp | 1 Bloodbag + 1 Honey + 1 Turnip | vanilla | Cryptkeeper's Supper |
| ![Muckshake](https://static.wikia.nocookie.net/valheim/images/6/67/Muckshake.png/revision/latest) | Muckshake | Meal | Swamp | 1 Ooze + 2 Raspberry + 2 Blueberries | vanilla | Bog Harvester's Table |
| ![Serpent Stew](https://static.wikia.nocookie.net/valheim/images/2/22/Serpent_Stew.png/revision/latest) | Serpent Stew | Meal | Ocean | 1 Mushroom + 1 Cooked serpent meat + 2 Honey | vanilla | Longship Leviathan Spread |
| ![Onion Soup](https://static.wikia.nocookie.net/valheim/images/0/05/Onion_Soup.png/revision/latest) | Onion Soup | Meal | Mountain | 3 Onion | vanilla | Peak Runner's Supper |
| ![Eyescream](https://static.wikia.nocookie.net/valheim/images/9/90/Eyescream.png/revision/latest) | Eyescream | Meal | Mountain | 3 Greydwarf Eye + 1 Freeze gland | vanilla | Peak Runner's Supper |
| ![Fish Wraps](https://static.wikia.nocookie.net/valheim/images/f/f1/Fish_Wraps.png/revision/latest) | Fish Wraps | Meal | Plains | 2 Cooked fish + 4 Barley flour | vanilla | Jarl's Lox Table |
| ![Blood Pudding](https://static.wikia.nocookie.net/valheim/images/e/e4/Blood_Pudding.png/revision/latest) | Blood Pudding | Meal | Plains | 2 Thistle + 2 Bloodbag + 4 Barley flour | vanilla | Golden Harvest Board |
| ![Bread](https://static.wikia.nocookie.net/valheim/images/e/e1/Bread.png/revision/latest) | Bread | Meal | Plains | 10 Barley flour → 2 | vanilla | Golden Harvest Board |
| ![Mushroom Omelette](https://static.wikia.nocookie.net/valheim/images/b/b7/Mushroom_Omelette.png/revision/latest) | Mushroom Omelette | Meal | Mistlands | 3 Egg + 3 Jotun puffs | vanilla | Mistwalker's Garden Table |
| ![Salad](https://static.wikia.nocookie.net/valheim/images/7/7e/Salad.png/revision/latest) | Salad | Meal | Mistlands | 3 Jotun puffs + 3 Onion + 3 Cloudberry → 3 | vanilla | Sap-Glazed Garden Medley |
| ![Fish 'n' Bread](https://static.wikia.nocookie.net/valheim/images/5/55/Fish_%27n%27_Bread.png/revision/latest) | Fish 'n' Bread | Meal | Mistlands | 1 Fish9 + 2 Bread Dough | vanilla | Hidden Hills Sideboard |
| ![Stuffed Mushroom](https://static.wikia.nocookie.net/valheim/images/b/b7/Stuffed_Mushroom.png/revision/latest) | Stuffed Mushroom | Meal | Mistlands | 3 Magecap + 1 Giant Blood Sack + 2 Turnip | vanilla | Hidden Hills Sideboard |
| ![Yggdrasil Porridge](https://static.wikia.nocookie.net/valheim/images/0/04/Yggdrasil_Porridge.png/revision/latest) | Yggdrasil Porridge | Meal | Mistlands | 4 Sap + 3 Barley + 2 Royal jelly | vanilla | Hidden Hills Sideboard |
| ![Cooked Egg](https://static.wikia.nocookie.net/valheim/images/a/ae/Cooked_egg.png/revision/latest) | Cooked Egg | Meal | Mistlands | 1 Egg | vanilla | Hidden Hills Sideboard |
| ![Roasted Crust Pie](https://static.wikia.nocookie.net/valheim/images/0/05/Roasted_Crust_Pie.png/revision/latest) | Roasted Crust Pie | Meal | Ashlands | 2 Vineberry + 1 Volture Egg + 4 Barley flour | vanilla | Cinder Runner's Spread |
| ![Scorching Medley](https://static.wikia.nocookie.net/valheim/images/f/f4/Scorching_Medley.png/revision/latest) | Scorching Medley | Meal | Ashlands | 3 Jotun puffs + 3 Onion + 3 Fiddlehead → 3 | vanilla | Cinder Runner's Spread, Ashlands Gourmet Bowl |
| ![Sparkling Shroomshake](https://static.wikia.nocookie.net/valheim/images/0/04/Sparkling_Shroomshake.png/revision/latest) | Sparkling Shroomshake | Meal | Ashlands | 4 Sap + 2 Vineberry + 2 Smoke puff + 2 Magecap | vanilla | Cinder Runner's Spread |
| ![Spicy Marmalade](https://static.wikia.nocookie.net/valheim/images/e/e8/Spicy_Marmalade.png/revision/latest) | Spicy Marmalade | Meal | Ashlands | 3 Vineberry + 1 Honey + 1 Fiddlehead | vanilla | Cinder Sideboard |
| ![Sizzling Berry Broth](https://static.wikia.nocookie.net/valheim/images/a/a2/Sizzling_Berry_Broth.png/revision/latest) | Sizzling Berry Broth | Meal | Ashlands | 3 Sap + 2 Fiddlehead + 2 Vineberry | vanilla | Cinder Sideboard |
| ![Marinated Greens](https://static.wikia.nocookie.net/valheim/images/c/c3/Marinated_Greens.png/revision/latest) | Marinated Greens | Meal | Ashlands | 3 Sap + 2 Magecap + 2 Fiddlehead + 2 Smoke puff | vanilla | Cinder Sideboard |
| ![Whole Roasted Meadow Boar](https://static.wikia.nocookie.net/valheim/images/4/4b/Whole_Roasted_Meadow_Boar.png/revision/latest) | Whole Roasted Meadow Boar | Feast | Meadows | 2 Cooked deer meat + 5 Cooked boar meat + 4 Dandelion + 1 Woodland Herb Blend | vanilla | — |
| ![Black Forest Buffet Platter](https://static.wikia.nocookie.net/valheim/images/0/0b/Black_Forest_Buffet_Platter.png/revision/latest) | Black Forest Buffet Platter | Feast | Black Forest | 3 Deer Stew + 5 Thistle + 4 Queen's Jam + 1 Woodland Herb Blend | vanilla | — |
| ![Swamp Dweller's Delight](https://static.wikia.nocookie.net/valheim/images/8/8e/Swamp_Dweller%27s_Delight.png/revision/latest) | Swamp Dweller's Delight | Feast | Swamp | 8 Sausages + 4 Bloodbag + 2 Turnip Stew + 1 Woodland Herb Blend | vanilla | — |
| ![Sailor's Bounty](https://static.wikia.nocookie.net/valheim/images/7/76/Sailor%27s_Bounty.png/revision/latest) | Sailor's Bounty | Feast | Ocean | 5 Cooked fish + 4 Thistle + 2 Cooked serpent meat + 1 Seafarer's Herbs | vanilla | — |
| ![Hearty Mountain Logger's Stew](https://static.wikia.nocookie.net/valheim/images/e/e0/Hearty_Mountain_Logger%27s_Stew.png/revision/latest) | Hearty Mountain Logger's Stew | Feast | Mountain | 2 Wolf Skewer + 3 Onion Soup + 4 Carrot + 1 Mountain Peak Pepper Powder | vanilla | — |
| ![Plains Pie Picnic](https://static.wikia.nocookie.net/valheim/images/f/f7/Plains_Pie_Picnic.png/revision/latest) | Plains Pie Picnic | Feast | Plains | 3 Bread + 2 Lox Meat Pie + 5 Cloudberry + 1 Grasslands Herbalist Harvest | vanilla | — |

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

|  | Dish | Biome | Now asks for |
| --- | --- | --- | --- |
| ![Minced Meat Sauce](https://static.wikia.nocookie.net/valheim/images/d/d3/Minced_Meat_Sauce.png/revision/latest) | Minced Meat Sauce | Black Forest | 1 Cooked boar meat + 1 Grilled neck tail + 1 Carrot |
| ![Boar Jerky](https://static.wikia.nocookie.net/valheim/images/f/fc/Boar_Jerky.png/revision/latest) | Boar Jerky | Black Forest | 1 Cooked boar meat + 1 Honey → 2 |
| ![Sausages](https://static.wikia.nocookie.net/valheim/images/a/a0/Sausages.png/revision/latest) | Sausages | Swamp | 4 Entrails + 1 Cooked boar meat + 1 Thistle → 4 |
| ![Turnip Stew](https://static.wikia.nocookie.net/valheim/images/8/80/Turnip_Stew.png/revision/latest) | Turnip Stew | Swamp | 1 Cooked boar meat + 3 Turnip |
| ![Wolf Jerky](https://static.wikia.nocookie.net/valheim/images/6/6a/Wolf_Jerky.png/revision/latest) | Wolf Jerky | Mountain | 1 Cooked wolf meat + 1 Honey → 2 |
| ![Wolf Skewer](https://static.wikia.nocookie.net/valheim/images/c/cf/Wolf_Skewer.png/revision/latest) | Wolf Skewer | Mountain | 1 Cooked wolf meat + 2 Mushroom + 1 Onion |
| ![Lox Meat Pie](https://static.wikia.nocookie.net/valheim/images/1/1f/Lox_Meat_Pie.png/revision/latest) | Lox Meat Pie | Plains | 2 Cloudberry + 2 Cooked lox meat + 4 Barley flour |
| ![Meat Platter](https://static.wikia.nocookie.net/valheim/images/f/f5/Meat_Platter.png/revision/latest) | Meat Platter | Mistlands | 1 Cooked seeker meat + 1 Cooked lox meat + 1 Cooked hare meat |
| ![Honey Glazed Chicken](https://static.wikia.nocookie.net/valheim/images/9/99/Honey_Glazed_Chicken.png/revision/latest) | Honey Glazed Chicken | Mistlands | 1 Cooked chicken + 3 Honey + 2 Jotun puffs |
| ![Misthare Supreme](https://static.wikia.nocookie.net/valheim/images/0/02/Misthare_Supreme.png/revision/latest) | Misthare Supreme | Mistlands | 1 Cooked hare meat + 3 Jotun puffs + 2 Carrot |
| ![Seeker Aspic](https://static.wikia.nocookie.net/valheim/images/b/b2/Seeker_Aspic.png/revision/latest) | Seeker Aspic | Mistlands | 2 Cooked seeker meat + 2 Magecap + 2 Royal jelly → 2 |
| ![Fiery Svinstew](https://static.wikia.nocookie.net/valheim/images/8/89/Fiery_Svinstew.png/revision/latest) | Fiery Svinstew | Ashlands | 1 Cooked asksvin meat + 2 Vineberry + 1 Smoke puff |
| ![Mashed Meat](https://static.wikia.nocookie.net/valheim/images/7/7a/Mashed_Meat.png/revision/latest) | Mashed Meat | Ashlands | 1 Cooked asksvin meat + 1 Cooked volture meat + 1 Fiddlehead |
| ![Piquant Pie](https://static.wikia.nocookie.net/valheim/images/0/07/Piquant_Pie.png/revision/latest) | Piquant Pie | Ashlands | 2 Vineberry + 2 Cooked asksvin meat + 4 Barley flour |
| ![Mushrooms Galore à la Mistlands](https://static.wikia.nocookie.net/valheim/images/7/76/Mushrooms_Galore_%C3%A1_la_Mistlands.png/revision/latest) | Mushrooms Galore à la Mistlands | Mistlands | 1 Misthare Supreme + 3 Cooked seeker meat + 1 Hidden Hills Sideboard + 1 Herbs of the Hidden Hills |
| ![Ashlands Gourmet Bowl](https://static.wikia.nocookie.net/valheim/images/f/f7/Ashlands_Gourmet_Bowl.png/revision/latest) | Ashlands Gourmet Bowl | Ashlands | 3 Cooked asksvin meat + 1 Cinder Sideboard + 2 Scorching Medley + 1 Fiery Spice Powder |

## Vanilla spices

Not new items. They sit on feast boards.

- Woodland Herb Blend (`SpiceForests`)
- Seafarer's Herbs (`SpiceOceans`)
- Mountain Peak Pepper Powder (`SpiceMountains`)
- Grasslands Herbalist Harvest (`SpicePlains`)
- Herbs of the Hidden Hills (`SpiceMistlands`)
- Fiery Spice Powder (`SpiceAshlands`)

This package does not replace RestlessCore. Install the **Restless Valheim** modpack, or Core then this.
