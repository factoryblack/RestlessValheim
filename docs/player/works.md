# RestlessWorkshop guide

Install **RestlessWorkshop**. A board already placed in a world keeps its orders.

**Make a batch. Keep a reserve.**

Use a work-order board to manage nearby production machines from one window.

> (screenshot coming) — Wall-mounted board beside kilns and smelters.

## Start an order

1. Build the **Work-order board** at a workbench from the hammer's Furniture category.
2. Place it near production machines and supplies.
3. Use it to open the Workshop. Choose a product in **Production**, set a quantity and choose **Make** or **Keep**.

| Instruction | Meaning | Example |
| --- | --- | --- |
| Make | Produce this many additional units, then stop | Make 30 coal even if you already have coal |
| Keep | Replenish usable stock when it falls below the target | Keep 30 coal available around the workshop |

Keep accounts for accessible nearby-container, pile and board stock after Make reservations, plus allocated incoming production. Player bags are neither counted nor consumed. Chest access follows the player who placed the board, so changing network ownership does not change its supplies. It does not require each machine to fill the entire shortage independently. The order API prevents a second Keep target for the same output.

## Read the window

- **Production:** search/filter products, inspect input/fuel requirements and place orders.
- **Orders:** finished/available stock, incoming output, remaining work, collection and cancellation.
- **Machines:** live input queues, free slots, processed output and fuel.
- **Output:** board stock that can be taken, separated from stock reserved for Make orders.

> (screenshot coming) — Keep 30 order with available stock, incoming production and remaining shortage.

Coverage bars show **quantities**, not elapsed-time estimates. Machines keep their native processing time and fuel use. The board supports up to eight orders, with quantities from 1 to 999.

## Machines and dependencies

Kilns, smelters, blast furnaces, windmills, spinning wheels and eitr refineries use their available native conversions. The stone oven stays with Cook.

A plan can reveal recipes for inputs or fuel, but **dependencies must currently be ordered separately**. For example, order coal if the workshop needs it; an ingot order does not automatically queue every upstream material.

Plans use the machine's own fuel rate. A smelter burns two coal for one bar. Missing inputs, fuel or machine capacity can leave an order waiting.

The workbench and forge still craft/upgrade equipment normally. Bronze, nails and other forge recipes are not automatically added as board products.

## Collect and cancel

Output from an active board-controlled machine is collected on that board. Collect reserved Make output through Orders, or take unreserved stock through Output. Unmanaged output retains its normal drop behaviour.

Keep stock is unreserved. Taking it into your bag reduces shared stock, and Keep replenishes the shortage. Automatic delivery into a chosen output chest is not implemented.

Stopping an order removes its instruction. Materials already inside machines keep processing. Cancellation requires a second click.

## Multiplayer and settings

The last player to use the board takes ownership, and that peer runs production. Supply permissions stay tied to the player who placed the board. If control changes while the window is open, mutation controls are disabled until you reopen it. Collection of a Make order remains restricted to its ordering player.

Use **F8 → RestlessWorkshop** for the local window toggle. Closing or disabling the window does not stop placed orders. Wider multiplayer ownership behaviour still needs playtesting.

Workshop requires Core. The Restless Valheim pack installs it with the rest of the collection. If an older installed release reports that the workshop window is not included, update to the build containing the UI.

[Install](install.md) · [Troubleshoot](troubleshooting.md) · [All guides](README.md)


