# Work-order board

RestlessWorks is the workshop, in the same way RestlessCook is the kitchen. The window should follow the Cookbook: one place, a recipe list, the selected item's dependencies, orders, and the stations that will do the work. Keep it a separate window. The preparation table stays food. The workbench and forge stay where equipment is made and improved.

The screen is not in this package. Bind `Works.Opened` the way the Cookbook binds `Kitchen.Opened`.

## What the player does

The hammer piece is `piece_restless_works`, "Work-order board", under Furniture, at a workbench. Use it to open the window. The machines nearby do the smelting.

Two instructions, both on `Works.Place`:

- **Make** — produce this many more, then stop. Stock already in a chest does not count.
- **Keep** — refill until usable stock reaches the target. Usable stock is the bag, chests and piles in search range, plus the board's pantry, after subtracting output reserved for a Make order. Ore already in a machine counts as in production, so three smelters do not each start the same shortage.

A Keep line can read:

`72 available · 20 in production · 8 remaining`

Those are `WorksOrder.Available`, `InProduction` and `Remaining`.

## Bind these

| Cookbook | Workshop |
| --- | --- |
| `Kitchen.Opened` | `Works.Opened` (`Piece`) |
| `Kitchen.Rows` / `Find` | `Works.Recipes` / `Find` |
| `Kitchen.Plan` | `Works.Plan` |
| `Kitchen.Orders` | `Works.Orders` |
| `Kitchen.Stations` | `Works.Machines` |
| `Kitchen.Live` | progress is already on `WorksOrder` |
| `Kitchen.Place` | `Works.Place(board, output, Make or Keep, count)` |
| `Kitchen.Cancel` | `Works.Cancel` |
| `Kitchen.Collect` | `Works.Collect` (Make only; hands finished output to the player who ordered it) |
| `Kitchen.Pantry` / `Take` | `Works.Pantry` / `Take` |

`WorksStep.Available` is the full amount on hand, including amounts above `Need`, same idea as the cookbook's `5000 / 5`. Requirement rows are `WorksUse`.

`WorksMachine` is one kiln, smelter, blast furnace, windmill, spinning wheel or eitr refinery in range. It has fuel, free slots, what is cooking, what is ready, and a world `Position`. A click that highlights the machine in the world can use that position later; nothing draws the highlight yet.

## What moved off the kitchen

The preparation table used to list every `Smelter`. Kilns, smelters and the other production machines are on this roster now. The stone oven stays with cooking, because food recipes still load it. Cauldrons, racks and the mead kettle are unchanged.

## Not in this pass

- No workshop window.
- Recipes are the machines' own conversions. Bronze, nails and gear are still forge crafts, not board recipes.
- Finished output waits on the board until Collect or Take. A designated output chest is not wired yet.
- Fuel on a plan is an estimate of one fuel item per product. The machine still burns on its own timer.
- Each Keep order is its own target. Two Keep 30 orders for the same item ask for 60.
- The last player to use the board owns it, and the tick follows them. Same rule as the kitchen.
- Not playtested.
