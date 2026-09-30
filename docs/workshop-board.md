# Work-order board

RestlessWorks is the workshop, in the same way RestlessCook is the kitchen. The window should follow the Cookbook: one place, a recipe list, the selected item's dependencies, orders, and the stations that will do the work. Keep it a separate window. The preparation table stays food. The workbench and forge stay where equipment is made and improved.

`WorkshopWindow` binds `Works.Opened`. Production, Orders, Machines and Output share the Restless paper controls and wheel scrolling. The window closes on Escape, departure from the board, or another major interface opening.

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

- Recipes are the machines' own conversions. Bronze, nails and gear are still forge crafts, not board recipes.
- Finished output waits on the board until Collect or Take. A designated output chest is not wired yet.
- Fuel on a plan is an estimate of one fuel item per product. The machine still burns on its own timer.
- The UI prevents a second Keep target for the same output. API callers remain responsible for duplicate targets.
- The last player to use the board owns it, and the tick follows them. Same rule as the kitchen.
- Not playtested.

## Window behaviour and playtest

Production supports search, machine filtering, Make/Keep quantities and separate ordering of dependencies. Dependencies are not automatically queued. Fuel requirements are labelled estimates. Orders show completed/available, incoming and remaining quantities; coverage bars are not timers. Machines show native queue/fuel counts. Output separates reserved Make output from stock anyone can take.

The window polls only while open, once per second, reuses cards and only redraws changed data. Board ownership loss disables mutations until reopened. Collection remains restricted to the Make order's player. Cancelling an instruction needs a second click and does not remove material already in machines.

Playtest: Make output through collection; Keep replenishment and duplicate guard; fuel/input shortages; full inventory collection; cancellation; two players switching board ownership; long names and small resolutions; search/filter and wheel scrolling; Escape then reopening. Runtime rendering and multiplayer behaviour still need an in-game pass.
