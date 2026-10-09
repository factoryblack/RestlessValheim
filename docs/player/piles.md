# RestlessPiles guide

Familiar buildable resource stacks become bulk stores for their own material.

> (screenshot coming) — Resource pile tray with a stored total above one bag stack.

## Deposit and take

1. Place a supported pile using the hammer, or use an existing placed pile.
2. Press **E** to open its tray.
3. Choose **Stack** to deposit matching bag items, or **Take stack** to withdraw up to one inventory stack.

Core's **Backtick** Quick Stack also deposits matching resources into nearby piles. Hotbar, equipment/quick slots and locked cells remain protected.

Take stack fills a matching partial stack or creates one new stack. It does not empty the pile across every free inventory cell. The material used to build the piece and the additional stored contents are tracked separately; removing or destroying the pile returns the build cost and stored extras as ordinary drops.

## Supported materials

Supported single-resource vanilla stacks/piles include wood variants, stone, coal, black marble, grausten, bones, skulls and coin piles, where those pieces exist. A wood stack holds wood; a stone pile holds stone. Empty piles still know their material.

No additional mixed-item chest inventory is created. Existing placed piles remain usable.

## Shared storage actions

- Ground-item routing prefers a matching pile, then a matching chest.
- Crafting/building can use pile stock after checking chests.
- Supported stations can find pile inputs and fuel.
- [Storage](storage.md) lists nearby piles and lets you take from them.
- [Cook](cook.md) can use nearby pile stock in kitchen orders.

## Range and settings

Use **F8 → RestlessPiles**. Piles follow Core's search range by default. Enable **Isolate pile range** to use a separate pile radius; the slider appears only when isolation is enabled.

The gameplay toggle and range settings are host-controlled. A physical pile is a local store in the world, not access to every pile across the map. Ward permissions apply to browsing, taking, crafting, deposits and vacuum. Storage-browser takes use the same range as the displayed piles, including a smaller isolated pile range.

[Install](install.md) · [Troubleshoot](troubleshooting.md) · [All guides](README.md)

