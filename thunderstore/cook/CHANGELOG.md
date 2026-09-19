# Changelog

## 0.1.5

- Remove Feast.Awake patch — method was removed in the latest Valheim update; feasts initialize correctly via UpdateVisual and Interact

## 0.1.4

- Custom feast boards craft at the food preparation table; the serving tray places the leftover board
- Custom feast leftover recipes bind real meal ItemDrops so they grey out without ingredients
- Feast leftovers get an explicit prep-table recipe; the list forgets the empty-cost leak and only shows boards you know or hold
- Placed feast boards keep a leftover food item, servings, and a plate collider so E can eat them
- Dropped meals no longer get the feast plate collider (convex mesh on ItemDrop prefabs made them pop upward)

## 0.1.3

- Custom feasts are serving-tray pieces (vanilla models were the Meadows/Black Forest boards). Unlock by knowing the meals, then place with the tray
- Meal, sideboard, leftover, and feast plates sit at the prefab origin at baked metre scale; vanilla food/feast meshes stay hidden, including when a feast updates as you eat
- Plate materials use a lit albedo shader instead of vanilla skinned-food materials

## 0.1.2

- Matrix drops the internal Op column and keeps Recipe
- Vanilla rewrite and reference rows use Iron Gate icons from the Valheim wiki (Pulled Bear has no wiki file yet)
- Serving tray (Feaster) recipe registers after vanilla prefabs so the workbench craft actually appears
- Recipe discovery is vanilla again: hold the ingredients. Feast crafts stay on the food preparation table

## 0.1.1

- Custom meals, feasts, and sideboards use isolated plate icons
- Custom dishes use the Meshy plate meshes and 1024 albedos
- Thunderstore readme is the cooking wiki and full recipe matrix
- Locked v0.1 food stats: vanilla feasts stay balanced, custom feasts split health/stamina, Mistlands+ custom feasts carry eitr, sideboards stay inedible
- Food meshes unflip Meshy UVs, bleed atlas gutters, and drop leftover vanilla meat/feast gloss
- Food preparation table and Serving tray become Meadows workbench crafts; Meadows/Black Forest custom feasts drop Woodland Herb Blend
- Custom feast recipes are taught when known-recipes refresh and when you use the prep table
- Jötunn 2.30.1 (needs RestlessCore 0.1.3+)

## 0.1.0

- Meadows through Ashlands cooking graph: 17 meals, 16 feasts, 2 sideboards
- 14 cooked-first vanilla rewrites and 2 feast reroutes
- Hidden Hills and Cinder sideboards feed Mushrooms Galore and the Ashlands Gourmet Bowl
