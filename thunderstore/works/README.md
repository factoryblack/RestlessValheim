# RestlessWorkshop

The public name is **RestlessWorkshop**. The existing Thunderstore package remains **RestlessWorks**; install that listing. Existing configurations and saved boards keep their identifiers.

**Make a batch. Keep a reserve.**

Manage kilns, smelters and other production machines from one work-order board. The real machines keep doing the work.

> (screenshot coming) — Wall-mounted work-order board beside a working smelter and kiln.

## Two useful instructions

**Make** produces an additional quantity, then stops. **Keep** replenishes usable stock when it falls below your target.

Keep 30 coal accounts for nearby usable supplies and incoming production instead of asking every kiln to make another 30. Make output is reserved for its ordering player; Keep output is unreserved stock.

> (screenshot coming) — Keep 30 order showing stock, incoming production and remaining shortage.

## See the workshop

- **Production:** search/filter products, inspect requirements and place orders.
- **Orders:** coverage, incoming output, remaining work, collection and cancellation.
- **Machines:** fuel, input queues and free slots.
- **Output:** stored board output and its reservations.

The board supports up to eight orders. Coverage bars show quantities, not elapsed-time estimates.

## Start here

1. Install Workshop with Core.
2. Build the **Work-order board** at a workbench from the hammer's Furniture category.
3. Place it near supported machines and supplies, open it and choose a product.
4. Set the quantity and choose Make or Keep.

Kilns, smelters, blast furnaces, windmills, spinning wheels and eitr refineries use their native conversions. Fire/fuel and capacity still matter. The stone oven stays with Cook.

## Know the current rules

Dependencies must currently be ordered separately. Fuel figures in a plan are estimates. The workbench and forge still handle their own crafting; bronze, nails and gear are not automatically board products.

Finished output stays on the board. Collect reserved Make output through Orders, or take unreserved stock through Output. A chosen output chest is not implemented.

Stopping an order does not remove materials already processing inside machines. The last player to use the board takes ownership; reopen the window if control changes. Multiplayer ownership still needs broader playtesting.

Use **F8 → RestlessWorkshop** for the local window toggle. Workshop is currently a **separate install**, outside the pack manifest. The guide follows current main; older builds may not contain the Workshop UI.

## Installation and help

Requires **RestlessCore**, BepInExPack and Jötunn. A mod manager installs the required dependencies. Use matching released plugin versions on all clients and the server.

[Player guide](https://github.com/factoryblack/RestlessValheim/blob/main/docs/player/works.md) · [Installation](https://github.com/factoryblack/RestlessValheim/blob/main/docs/player/install.md) · [Changelog](https://github.com/factoryblack/RestlessValheim/blob/main/thunderstore/works/CHANGELOG.md) · [Report a problem](https://github.com/factoryblack/RestlessValheim/issues/new/choose)

[RestlessCore](https://thunderstore.io/c/valheim/p/Restless/RestlessCore/) is the shared foundation. [Explore the collection](https://thunderstore.io/c/valheim/p/Restless/Restless_Valheim/).
