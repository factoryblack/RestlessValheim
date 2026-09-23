# Loadout totals and shared set presentation

The totals sheet now has a compact folded corner and character crest, a recessed
plaque for the selected total, larger rows, and an inset source reader. Its 920x640
design scales down to the available parent area. Artwork never reserves extra
vertical space below the header. The existing F8 loadout toggle controls this work.

Equipment sets appear immediately after vitals. Their rows include the linked-set
seal, equipped/required count and explicit Active/Inactive label. Selecting a set
opens the same SetWell/SetIdentity used by item and recipe tooltips, followed by
equipped pieces and the original effect description. The progress rail is distinct
from item-quality diamonds. It is bounded to 12 segments for unusually large addon
sets, while its fill is proportional and the numeric count remains exact. Preview
tooltips without a player show the required count without inventing equipped pieces.

Sources are visible by default and can be collapsed as a group. Each entry has
separate measured name/value lines, and calculation notes remain visible even when
the sources are collapsed. No contribution is inferred from the artwork. All
existing source attribution, residual adjustments, resistance explanations and
damage arithmetic remain in StatSheet.Data. Set rows only gain display metadata.

The sheet keeps its existing 0.25s visible-only sampling. Row topology is rebuilt
only when row keys change; the source reader is rebuilt only when its content or
expanded state changes. Scroll position and focus are restored during reader
refresh. No new Update method, effect hook, player mutation or network setting is
introduced. The scrollbar still clones the native recipe scrollbar.

## Assets

Built-in image generation produced four independent transparent originals:

| Asset | Brief / runtime role |
| --- | --- |
| loadout-corner | Upper-right triangular fold of charcoal leather/paper, bronze seam, two studs; 96px header ornament |
| loadout-crest | Simple embossed human bust in a small bronze/iron clasp; 40px character identity |
| loadout-stat-plaque | Recessed charcoal leather with short iron end caps, uninterrupted centre; sliced backing for a live title and large value |
| equipment-set-seal | Three linked oblong iron/bronze rings, transparent gaps; shared set-membership symbol |

All briefs requested orthographic sprites, muted worn bronze, matte charcoal,
tight transparent framing, and no baked text, runes, jewels, glow or external shadow.
Originals are in art/loadout/originals. Runtime exports and crop dimensions are
recorded in art/loadout/manifest.json. With sharp installed, run
`node scripts/export-loadout-art.cjs` to crop transparent margins and downsample;
`node scripts/review-loadout-art.cjs` assembles the illustrative review board.
The board is a placement study with illustrative values, not a Unity screenshot.

## Validation

Inspect exports at intended size and verify all four PNGs retain alpha. Build all
five plugins and run the existing loadout arithmetic and set extraction regressions.
Unity playtest remains required: no set/partial set/full set, multiple sets, changing
gear with the sheet open, long translated names, food decay, source collapse, mouse
wheel and gamepad navigation, low/high UI scale, and toggling the sheet off/on.
Check the same partial/full set inside inventory and crafting tooltips.

This branch builds on the separate set-tooltip clarity PR so extraction logic is
shared rather than duplicated. Merge that base before retargeting this PR to main.
