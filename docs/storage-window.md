# Storage window — UI integration

The Storekeeper's Table opens the shared Core storage browser and can take items from accessible nearby containers. Use matching builds of RestlessCore.dll and RestlessStorage.dll.

## Ownership

- Core owns `StorageWindowApi`, immutable display DTOs and the shared window, using RestlessUi materials/type and RestlessScrollRect.
- RestlessStorage owns the table, its local F8 visual toggle and `StorageSource`.
- The source queries `NearbyStorage.ForLocalPlayer()` only while the window is open, at most once per second. Existing range, enabled state and container access checks apply. There is no claim that unloaded storage is connected.
- Read-only totals include all items in each accessible loaded inventory. They are not a promise of immediately withdrawable stock; reservations and transfer capacity are backend concerns.
- Grouping preserves prefab, quality, variant, world level, durability, crafter and custom data. Display names alone are not stable transfer identities.

## Presentation

1200 × 800 logical sheet, scaled to the available canvas. Six-column browser with 30 reused visible cards. Search, game-type category and name/quantity/unit-weight sorting. Selection is retained by opaque ID across snapshot updates. Item details and source locations scroll separately from the fixed quantity controls. Existing paper, portrait and control assets are reused; native recipe scrollbar artwork is borrowed when available. No new asset pack.

Snapshot revisions repaint changed content; scrolling only rebinds visible cells when the visible row changes. No frame-by-frame storage scan or full layout rebuild. World validity/input/resize checks remain lightweight. Empty storage, inaccessible storage and empty search results have distinct states. Closing, moving away, death, disabling the setting, scene teardown and addon unload release owned input blocking. ESC is consumed once instead of also opening the pause menu.

This is a mouse/keyboard preview. Controller navigation across virtualised offscreen grid rows remains a follow-up; visible Unity controls retain normal selectable navigation.

## Transfer

`StorageSnapshot.CanWithdraw` is true while nearby storage is enabled. Take calls `Withdraw` with the opaque item key from `ItemKey`.

The chest owner removes up to the asked amount of matching stacks and replies with those items. The taker adds what fits, then acknowledges. The acknowledgement carries anything that did not fit, and the owner puts that back. A second acknowledgement is ignored. If the owner hears nothing for 10 seconds, the whole take goes back into the chest. A late reply after the taker has given up is ignored, so it is not added twice.

Leave-one is a crafting reservation. A deliberate take does not keep one behind. This does not use `RequestConsume`.

The window shows one result message. Closing it does not cancel a take that already reached the owner. It does not retry a chest that did not answer.

## Playtest

- Table opens the window and ESC closes it without a second menu. Reopen repeatedly and quit/rejoin without a stuck cursor.
- Search/type quantities while the once-per-second refresh occurs. Test zero stores, empty stores, no search matches and hundreds of distinct entries.
- Change quantities in nearby chests while browsing. Check selection, totals, source rows and retained scroll.
- Test long names/descriptions, different quality/custom-data items, 16:9/ultrawide and increased UI scale.
- Disable browser in F8; disable nearby storage as host; destroy the table; die or move away.
- Take a stack, a partial amount, and more than the inventory can hold. The overflow returns to that chest. Quality, variant, and custom data stay with the item.

CI compilation/regressions do not substitute for the Unity visual/input checks above.
