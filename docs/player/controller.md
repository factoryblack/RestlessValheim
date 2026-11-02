# Controller controls

The controller cursor is separate from the equipped-item marker. It stays visible when an empty hotbar slot is selected.

In the inventory, move right from the bag to reach the equipment columns, then the three extra quick slots. Left returns to the bag. Directional input follows these visible positions. The extra slots keep their existing saved item positions.

Item actions use the native controls: A selects, left trigger plus A splits, right trigger plus A drops, X uses/equips, and left trigger plus X transfers to or from the open container. Panel-switch controls remain available.

With a chest open, left-stick click takes all and right-stick click stacks matching items. The visible button labels show their current game bindings. Without a chest open, right-stick click in the player inventory stacks nearby using the normal quick-stack protections. Nearby stacking is disabled while dragging, typing or using a dialog, and when storage or quick stacking is disabled.

Drawers can run with BepInExPack and JÃ¶tunn without Core. Optional Core enables its additional nearby-storage and UI features.

Automated checks cover the controller logic and native API contracts. Physical controller use, rendered focus/labels, resolution changes, standalone loading and multiplayer transfers have not been tested in-game.

## Native interface choices

F8 keeps the interface and HUD features individually configurable. In Interface, turn off Restless inventory, item tooltip, loadout totals, slot locks and armor/quick slots to use their native counterparts. In HUD, the hotbar, buffs, map, notices, vitals and hints have their own switches. These choices do not require disabling the storage features you want to keep.

Disabling extra slots returns their items to the bag; if there is no room, overflow is dropped near the player. Empty those slots first if you want everything to remain in the bag.
