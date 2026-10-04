# RestlessPlant guide

Grow forage alongside ordinary crops, then spend less time placing and picking each plant.

> (screenshot coming) — An aligned rectangular cultivator preview beside existing rows.

## Plant something new

Equip the cultivator and select a berry bush, mushroom or other available forage piece. Each placement costs the item it grows. Use a small grid first, then expand it as your stock allows.

The roster includes raspberry, blueberry, cloudberry, lingonberry, mushrooms, thistle, dandelion and later-biome forage such as magecap, jotun puffs, smoke puff and fiddlehead. Entries whose game prefabs are unavailable are skipped.

Saplings and decorative plants are separate host-controlled options. Ordinary carrots, turnips, onions, barley and flax continue to work with the planting and harvest tools.

## Align and resize

| Control | Default action |
| --- | --- |
| Left / right bracket | Reduce / increase columns |
| Minus / equals | Reduce / increase rows |
| F10 | Toggle alignment with a nearby planted field |
| F8 → RestlessPlant | Grid, personal controls and permitted gameplay options |

Grid width and depth can be changed independently, up to seven cells each. Spacing follows the crop's growth needs; a minimum-spacing value of zero means use the crop's own spacing.

Each cell is checked for ground, biome, sun, space and materials. Invalid cells are red and skipped. Ivy and ashvine use the game's own plant check. The seed stays in the bag when that check says the vine cannot grow. An oversized preview can still plant its valid cells. **Every placed cell pays its own cost.** The till tool still cultivates one patch; expanding the crop grid does not enlarge terrain cultivation.

## Harvest and regrow

Pick a ripe player-grown plant to harvest eligible ripe player-grown crops within the configured area. It is not limited to the same crop type. Wild plants are not included simply because they are nearby.

One-shot crops replant the harvested crop if replanting is enabled and you can pay its cost. Berry bushes and forage retain their regrowth cycle. Picked mushrooms shrink and regrow; on bushes the berries change, not the whole bush.

Hover a growing or harvested plant for its remaining wait when hints are enabled. Bulk beehive harvesting is a separate option; bird nests retain their own behaviour.

> (screenshot coming) — Mixed forage/crop garden with a readable regrowth hint.

## Host and personal settings

The host controls extra crops, saplings, decorative flora, grow-anywhere, harvesting and replanting. Grid dimensions, field alignment and keybinds are local.

**Grow anywhere is off by default.** When enabled, it changes the normal ground/biome/sun/space restrictions. Use it deliberately; it is not required for ordinary farming.

[Install](install.md) · [Troubleshoot](troubleshooting.md) · [All guides](README.md)

