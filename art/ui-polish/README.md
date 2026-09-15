# Restless UI artwork polish

Seven independent transparent PNG candidates, built from main at
`e77056da7878ac73e570c74c646c2e98f86a4ddc`.
The previous working branch already pointed at this exact commit when checked,
so no commit replay or history rewrite was needed. This branch starts there.

This change contains artwork, a review board and asset documentation only.
No runtime assets have been replaced, no sprite registration changed, and no
code, configuration, exporter or build files are modified. Files deliberately
live outside the embedded Assets directory until their appearance is accepted.

## Most valuable changes

| Priority | Candidate | Purpose and intended scale |
|---|---|---|
| 1 | corner-overlay.png | Fuller solid corner, layered torn diagonal, broad emblem seat; roughly 128–168 UI units. Keep title and corner from intersecting. |
| 1 | craft-hammer.png | One instantly recognisable craft symbol in relief, replacing the flat anvil/hammer combination. Prefer 32–48 units; 24 is the lower small-size check. |
| 1 | station-socket.png | Quiet bronze/leather socket for a separate symbol and level; around 52–64 units. It can later serve other requirements without baking in their meaning. |
| 2 | portrait-frame.png | Open-centred leather and bronze frame around item artwork; roughly 72–96 units. Avoid giving every inventory cell this much ornament. |
| 2 | pine-emblem.png | Separate cast-metal corner/header emblem; approximately 20 x 58 units. Keep it subdued and off the text. |
| 3 | knot-divider.png | Cast-metal counterpart to the current flat divider; around 300 x 30 units. Keep the knot proportional when adapting the width. |
| 3 | category-strip.png | Quiet weathered backing for category text, distinct from an action button; around 144 x 23 units. |

The station emblem is a generic crafting symbol, not an illustration of a specific
workbench or forge. Station-specific item artwork should remain separate. The socket
contains no numeral; the corner contains no emblem; the frame contains no item;
the category ribbon contains no label. These separations preserve reusability.

## Direction

Keep torn paper for large surfaces, aged leather for quiet labels, and bronze for
small symbols and structural details. The upper-left light and dark lower-right
edges are consistent across this pass. Avoid making every border bright, thick or
ornate. The primary Craft plate can remain the warmest surface.

The biggest remaining asset opportunity after this family is a complete navigation
icon family with consistent silhouette weight and material. It should be designed
as a set rather than replacing just the raven or shield. After that, consistent
food/status silhouettes and explicit selected/pressed state artwork will offer more
benefit than adding ornament to every row. Those are recommendations, not assets
claimed as delivered here.

## Review and provenance

`review-board.png` uses the actual exported PNGs at large and small scales. Its
labels and example level are illustrative, not baked into the sprites. It is an
asset placement study, not a Unity screenshot. The station symbol reads best at
32 units or larger; its texture intentionally simplifies at smaller sizes.

All seven were made with the built-in image-generation tool. Exact prompts are in
`prompts.json`; export dimensions and source image identifiers are in `manifest.json`.
The source generations remain separate. Exports only crop transparent margins,
resize proportionally, and add two transparent pixels of padding. Preserve RGBA.
The frame's central aperture is genuinely transparent.

Validated alpha channels, non-empty silhouettes, frame centre transparency and the
small-size board. The final Git diff must contain only `art/ui-polish/` paths.
No compilation or in-game test is needed to establish that no code changed;
actual integration and in-game appearance remain a separate task.
