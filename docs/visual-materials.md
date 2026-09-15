# Restless visual materials: tooltip proof

This is the first material set for the shared UI system. It is opt-in through
`RestlessUi.PaperSurface`, `PaperCorner` and `PaperDivider`. The tooltip is the
first consumer; no unrelated screen is automatically reskinned.

## Assets

All PNGs live in `src/RestlessQoL/Assets` and are embedded by the existing wildcard.

| Asset | Export | Usage |
|---|---|---|
| paper-panel.png | 516 × 514 | Opaque charcoal surface with transparent torn perimeter; panels and square icon wells |
| paper-panel-rim.png | 516 × 514 | White alpha edge mask, independently tinted for optional rarity/state |
| paper-corner.png | 260 × 257 | Independent layered top-right plate; displayed at 94 × 118 |
| paper-chip.png | 516 × 103 | Label, badge and damage plate |
| paper-chip-rim.png | 516 × 103 | Independent small-plate accent mask |
| paper-tree.png | 64 × 128 | Separate corner emblem, vector source retained |
| paper-knot.png | 64 × 40 | Separate divider motif, vector source retained |

Generated material images have genuine alpha outside the silhouettes. Mechanical
export trims transparent margins and downsizes; it does not paint a background.
Edge masks derive from the source alpha, not a rectangular outline. Ornaments are
simple original vector geometry, rasterised for the existing runtime PNG loader.

Panel slice borders are 32 source pixels on all sides. Chips use 32 horizontal and
16 vertical. Unity's Image.Type.Tiled at pixelsPerUnitMultiplier=2 keeps the grain
and border size consistent as panels grow. The centre repeats instead of stretching.
The source texture is not mathematically seamless: inspect joins in-game before
approving this material for very large windows. The quiet centre reduces their
visibility; it does not guarantee their absence.

Never multiply these already dark surfaces by the old brown ChipTint. Their base
colour is white in Unity. Colour changes belong on the independent rim. Text and
live game icons remain separate. The slot still uses DressSlot for wear/stack/lock
behaviour, then adopts the new material. Muted inspect text is slightly brighter
than the old kit to remain legible on the charcoal texture.

## Proof

Open `material-proof.html` in a browser. It assembles the actual exported textures
at the intended sizes, including tiled centres and optional accent masks. It is a
material/layout study, not a Unity screenshot or pixel-exact runtime renderer.
Georgia substitutes for runtime Averia and ICON placeholders represent game sprites.
Wood and Torch demonstrate low/high base detail density; food and equipment fixtures
exercise additional sections. Expansion fixture values are invented for layout only.

Hover/selection animations and skill-tree presentation are not implemented. The
edge-mask asset is a primitive they can reuse, not a claim those states are finished.

## Generation record

Created with the built-in image-generation tool using the supplied Torch tooltip as
a material reference. Prompt briefs:

1. Empty square charcoal fibrous/slate-paper panel; fine torn perimeter and subtle
   bronze wear; quiet opaque interior; true transparent exterior; no text or symbols.
2. Separate opaque triangular upper-right plate; ragged top/right and diagonal torn
   edge; charcoal layered texture; true transparent exterior; no emblem or text.
3. One wide low empty charcoal label plate; delicate torn perimeter and warm worn
   bronze hairline rim; true transparent exterior; no text, icons or shadows.

Export script: `node scripts/prepare-materials.cjs PANEL_SOURCE CORNER_SOURCE CHIP_SOURCE`.
It requires `sharp` in the Node environment. The exported PNGs are the committed
production inputs; regenerating them is unnecessary to build the mod.

Compile and Valheim/Unity visual validation remain pending. Use the checklist in
`tooltip-api.md`, paying particular attention to tiled seams, corner overlap, masks,
font measurements, different UI scales and long-content paging.
