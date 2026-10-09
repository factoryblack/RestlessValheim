# Changelog

Changes are listed newest first. **Unreleased** describes current source changes that are not assigned to a new tagged release yet.

## 0.1.5

- Workshop orders and queued-machine fuel use the order creator’s accessible stores rather than the controller owner’s inventory.
- Keep targets and input loads use stock belonging to the same actor. Requires Core 0.2.7.

## 0.1.4

- A Make order counts each unit as it drops at the machine, and then stops.
- Requires Core 0.2.5.

## 0.1.3

- An iron order takes iron scraps. Iron ore is used only when no scraps are on hand.
- Requires Core 0.2.4.

## 0.1.2

- A smelter or blast furnace is given the coal that bar actually burns, and the finished unit drops at the machine.
- Workshop controls use the shared UI kit. The layout is unchanged.
- A machine that still has ore queued keeps receiving fuel after the order has enough ore.
- Each queued conversion counts toward the order, and that machine stays reserved for the board using it.
- Background pulls skip locked cells, the hotbar and quick slots.
- Destroying the board drops its pantry.
- Requires Core 0.2.3.

## 0.1.1

- RestlessWorkshop is included in the Restless Valheim pack.
- Requires Core 0.2.2.

## 0.1.0

- Added a wall-mounted work-order board, built at a workbench, for kilns, smelters and the other production machines nearby. The stone oven stays with cooking.
- Added the Workshop window: Production, Orders, Machines and Output, with search, filters, Make and Keep quantities, stock coverage, collection and confirmed cancellation.
- Finished output waits on the board. There is no designated output chest yet.
- The listing, F8 entry and window are RestlessWorkshop. A board already placed keeps its orders.
- Updated the package icon to the shared Restless ecosystem artwork, with a matching ring size, centred artwork and transparent background.
- Requires Core 0.2.2. This package is not part of the Restless Valheim pack.
