# Final asset pass

Asset candidates only. No runtime assets, C#, configuration or build scripts changed.
Five separately generated bronze navigation emblems join seven utility glyphs,
a quality lozenge, a selection marker and a focus overlay: 15 transparent PNGs.
The ten small geometric assets include editable SVG masters. Navigation source
prompts and generation identifiers are in `../navigation-prompts.json`.

## Material and scale

Navigation uses cast bronze with upper-left highlights and broad silhouettes.
Fit proportionally into a 48 px box, with approximately 44 px of visible artwork.
The board also shows 24 px stress checks; prefer 40–48 px for navigation.
Do not stretch these emblems or add a gold border to every tab.
Utility symbols use native geometry for 20–24 px controls. Their 18 px examples
are lower-size checks. Preserve aspect ratio and retain a larger interactive area.

Quality marks are faceted bronze lozenges, displayed around 14 x 20 px with 4 px
spacing beside the item category. A row of three means quality 3; omit a repeated
Quality caption. Keep quality independent of future rarity colour or meaning.
Long category names and high quality counts must wrap as groups or use an overflow
summary during integration; do not let them enter the reserved corner area.

## Control-state specification

| State | Shared artwork treatment | Additional feedback |
|---|---|---|
| Idle | Navigation icon opacity 0.72 | Quiet shared paper surface |
| Hover / keyboard focus | Icon opacity 1.0 | Cream focus corner overlay at 0.65 opacity |
| Selected | Icon opacity 1.0 | Separate bronze selection marker below icon |
| Pressed | Icon opacity 0.8 | 2 px downward displacement, dark lower seat |
| Disabled | Icon opacity 0.32 | Suppress hover and pressed feedback; expose reason where relevant |

These are proposed visual values, not implemented behaviours. Keyboard focus and
selection are independent: show both when appropriate. Do not confuse disabled
with a selectable but currently uncraftable recipe. Missing requirements use the
warning glyph plus a readable reason, never colour alone. Locked settings use the
lock glyph and host-controlled explanation. Equipped uses the check mark.

`focus-corners` is a square overlay. For rectangular controls use corner-preserving
slicing (16 px border on the 64 px source), not uniform stretch or aspect fitting
over the middle of the label. Keep selection and focus above material artwork but
clear of labels. Disable raycasts on decorative images during integration.
Tint states do not require duplicate texture files.

## Proofs and limitations

`states-and-icons.png` shows the actual exports, proposed states and category/quality
placement. `assembled-study.png` combines the earlier panel, portrait, corner,
station and badge assets with this pass in a tooltip and crafting composition.
The sample item, values and bronze item thumbnail stand-ins are illustrative;
they do not add game content. The live game should retain its actual item icons.
Both proofs use DejaVu Serif as an explicitly labelled substitute for unavailable
Averia. They are static placement studies, not Unity screenshots or scroll tests.
The crafting proof concentrates on shared art placement; it is not a prescription
to change the existing inventory layout or shorten recipe information.

All raster navigation originals were generated separately using the built-in
image tool, then alpha-trimmed and proportionally reduced into padded PNG exports.
SVG artwork is authored natively and rasterised at 64 px. No generative repainting
was used for the exact glyphs. Export sizes and source links are in `manifest.json`.
RGBA transparency, visible silhouettes and both boards were checked locally.
In-game sizing, font metrics, focus handling and interaction still need verification
when this artwork is integrated. No further decorative expansion is needed before
that integration pass.
