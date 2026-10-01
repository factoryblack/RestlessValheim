# Changelog

Changes are listed newest first. **Unreleased** describes current source changes that are not assigned to a new tagged release yet.

## Unreleased

- Refreshed the Cookbook introduction and moved the detailed food catalogue into linked, generated recipe guides.

## 0.3.0

- Added larger dish portraits and filters for feasts, meals, ingredients and meads.
- Cauldron and preparation crafts take their crafting time; cancellation returns their ingredients.
- Orders show preparation progress for each step and do not count a carried dish as the newly completed order.
- Native dishes such as spice blends show their real ingredients and station.
- Improved featured cards and prepared/waiting/blocked feedback.
- Requires Core 0.2.1.

## 0.2.0

- The food preparation table opens the Cookbook and manages kitchen orders.
- Orders load nearby cooking racks/ovens, collect prepared results and deliver the completed feast to its ordering player.
- Station level, fire, fuel and hook capacity remain required.
- Requires Core 0.2.0.

## 0.1.9

- Fixed custom food models missing after mod-manager installation flattened the asset folder.

## 0.1.8

- Fixed placement and scale of custom feast boards.
- Removed duplicated vanilla food values from custom feast leftovers.
- Kept incomplete leftover recipes disabled instead of repeatedly unlocking.
- Improved food lighting and fixed dropped Honeyed Mushrooms jumping off the ground.

## 0.1.7

- Fixed learned recipes repeatedly triggering unlock notifications.
- Removed duplicate asset warnings.
- Local builds now copy the food models/textures alongside the DLL, and missing custom models produce clearer log messages.

## 0.1.6

- Increased custom feast-model scale to better match vanilla food presentation.

## 0.1.5

- Fixed feast initialisation compatibility with the updated game.

## 0.1.4

- Fixed preparation-table crafts, serving-tray placement and ingredient availability for custom feast boards.
- Placed feasts retain servings and can be eaten.
- Fixed dropped meals jumping because of feast collision settings.

## 0.1.3

- Custom feasts can be placed with the serving tray and unlock from their meal ingredients.
- Improved model alignment, leftover plates and lit food materials.

## 0.1.2

- Made recipe tables easier to read and added vanilla dish icons.
- Fixed serving-tray recipe registration and restored normal ingredient-based discovery.

## 0.1.1

- Added custom dish icons and models, updated food values and the full recipe reference.
- Improved food textures and materials.
- Moved preparation-table and serving-tray access to Meadows and removed spice requirements from early custom feasts.
- Improved recipe discovery.
- Requires Jötunn 2.30.1 and Core 0.1.3 or later.

## 0.1.0

- Initial Meadows–Ashlands food expansion: 17 meals, 16 feasts and two sideboards.
- Changed 14 vanilla recipes to use cooked protein and rerouted two vanilla feasts.
- Hidden Hills and Cinder sideboards connect meals to vanilla feast recipes.
