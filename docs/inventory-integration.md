# Inventory material integration

The approved inventory study is now consumed by the existing dresser. Eight PNGs
are embedded through the project's existing Assets wildcard and registered in Kit.
The study and SVG masters remain in art/inventory-study.

DressSlot has an optional inventory skin. Player/container grids and extra slots
opt in; HUD hotbar, HUD quick slots, recipe portraits and other callers keep their
existing skin. The inventory has a recessed charcoal cell, a thin cream hover or
focus outline and a quiet amber equipped underline. The slot-lock marker remains
the small top-right diamond. Quality stays a compact numeral in the other corner.
Durability is a narrow 4-unit ribbon between that corner and the count strip.

Six low-opacity silhouettes appear only in empty equipment cells. Quick slots
display live configured bindings at bottom-left, with quantities bottom-right.
InventoryScreen fits one shared paper backing to the visible player cells,
including equipment/quick slots where enabled. Plain rules divide the hotbar from
the bag and the bag from equipment. No grid coordinates, inventory dimensions,
drag handlers, item transfers or gameplay settings are changed. Native geometry
is intentionally retained, rather than reproducing the study's spacing exactly.
Armour retains its game icon; carry weight uses the new shared monochrome mask.

This follows the existing InventoryScreenEnabled setting. Extra slots switch back
to their previous material/captions when the inventory skin is disabled. Their
paint signature includes the setting, so this refresh does not require moving an
item. The new feedback component owns only visual children and no click handlers.
The fitted backing/dividers belong to InventoryScreen's normal cleanup set.
The carry icon uses the existing reversible artwork registry.

## Validation

Static syntax, changed-file review and embedded-sprite checks performed. This
environment has no .NET SDK or Valheim installation; compilation and in-game
layout/input verification remain required.

Check in game:

- Open bag/chest at your normal UI scale; ensure every existing cell is reachable.
- Hover, controller-focus, equip and drag through bag/equipment/quick slots.
- MMB-lock cells: diamond stays top-right and locking behaviour is unchanged.
- Empty/refill each equipment type; silhouettes must disappear under item art.
- Change quick bindings and quantities; verify key/count/wear clearance.
- Toggle the inventory skin and extra slots independently, then reopen the menu.
- Check chest grids taller than the bag and armour/weight readings after changes.

All of these are playtest checks, not claims of runtime verification.
