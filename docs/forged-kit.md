# Forged component kit

Adds 19 embedded PNG assets to the shared RestlessUi kit. This branch includes all
four prior UI changes. This is an implementation for the next in-game test, not a
verified Unity render. `forged-kit-preview.png` is a composition of the exported
assets at small sizes, using a generic serif for annotation.

## Assets and components

| Asset | Export | Runtime use |
|---|---|---|
| forged-badge | 516 x 103 | Damage and extension badges; 52/14 pixel sprite borders, multiplier 2 |
| forged-action | 772 x 133 | Craft action; 80/20 pixel borders, multiplier 2 |
| quality-gem | 32 x 32 | Bevelled metal marks beside category; no Quality caption |
| category-ribbon | 160 x 32 | Quiet category backing |
| station-medallion | 64 x 64 | Required station emblem and numeral |
| glyph-station | 64 x 64 | Station symbol; separate from the level |
| tab-inset / tab-marker | 160 x 56 / 76 x 14 | Navigation, Craft/Upgrade and Settings selected states |
| scroll-thumb | 14 x 64 | Shaped thumb on the recipe detail scrollbar |
| glyph-blunt/slash/pierce/chop/pickaxe | 64 x 64 each | Physical damage symbols |
| glyph-fire/frost/poison/lightning/spirit | 64 x 64 each | Elemental damage symbols |

Vector sources for the 17 small assets are in `src/RestlessQoL/Assets/_svg`.
The two textured plates were made with the built-in image-generation tool, then
mechanically alpha-trimmed and resized. Both have real alpha; text and glyphs are
separate. The existing torn outer paper surfaces remain in use.

`ForgedSurface`, `ForgedTab`, `QualityMarks`, `ForgedScrollbar` and `RestlessHint`
are shared helpers. They do not choose rarity, set membership or gameplay effects.
Extensions still supply badge text/tint through TooltipApi. Elemental glyphs use
exact localised damage-label matching; unfamiliar lines remain readable text.

Quality levels above the available gem space show a compact +N remainder rather
than misrepresenting the level. Normal levels display only the repeated marks.
The old PaperQuality entry point now delegates to the new marks.

## Crafting layout and input

The scroll viewport is now inset from the owned detail plate, below the live icon
and name, rather than copying the vanilla TMP description rectangle. This addresses
the left-edge clipping shown in the screenshots. The action/material region remains
outside the scroll area. A shaped scrollbar shows overflow and supports dragging.

RestlessScrollRect moves 56 UI units per wheel delta, clamps to content bounds and
disables inertia. It is reusable for other lists. Recipe changes reset the scroll;
same-recipe text refreshes preserve the offset, clamped to the new content height.
Formatting-only raw TMP changes do not trigger rebuilds. Resolution/root-scale changes
now invalidate the inventory layout stamp.

Required station level uses a station glyph with the numeral beneath it. Its hover
hint explains the requirement. The original star/background are suppressed through
the existing reversible dresser. State changes retain shortage colour. Navigation
icons are fitted to 42 units where the prefab has an isolated icon; original sizes,
tab positions and button graphics restore when the dresser is disabled.

## Asset production prompts

Badge, built-in image generation:
"Generate ONE production game UI sprite, not a screenshot: a blank horizontal Nordic
forged iron badge plate on a genuinely transparent background. Shape approximately
4:1 width to height, front-on orthographic, isolated with generous transparent margin.
Dark charcoal textured inset, visibly hammered aged bronze bevel with irregular chipped
cut corners, restrained dark bronze border, subtle warm bounce on upper left edge.
Interior mostly calm and dark for cream text to be added in game. No text, NO symbols,
NO icons, no other objects, no drop shadow beyond silhouette, no glow, no checkerboard
painted in. Crisp silhouette, polished Valheim-like dark Nordic RPG material, readable
at 180x44 pixels. Plate centred across image. This is a reusable scalable badge backing:
retain a broad horizontal central field, all ornament confined to end caps."

Action, built-in image generation:
"Generate ONE blank production game UI action button sprite on genuinely transparent
background. Front-on orthographic horizontal bronze plate about 5:1 ratio width to
height, centred with transparent margins. Nordic RPG Valheim aesthetic: thick angular
clipped corners, forged aged bronze frame, rich muted amber-brown textured inset,
subtle etched angular Nordic ornament ONLY at left and right ends. Long calm empty
middle for a separately rendered label. Heavier and warmer than a dark item badge.
Crisp readable silhouette, painterly realistic metal grain, restrained lighting, no
external glow, NO TEXT, no letters, no icons, no objects or backdrop, no baked checkerboard.
Suitable for a reusable 420x64 pixel Craft or Confirm button; border ornament contained
inside end caps."

Run `node scripts/export-forged-kit.cjs BADGE_SOURCE ACTION_SOURCE` with Sharp available
to reproduce the exports from generated sources. Omitting paths retains the two
committed raster plates and regenerates vectors, their PNGs and the composition proof.

## Verification

Exports checked for alpha, dimensions and inclusion in Kit.Library; small-size asset
composition inspected. Source review and catalogue generation completed. No .NET SDK,
C# compiler or Valheim/Unity runtime is available here, so compilation and game
interaction remain unverified. No claim of screenshot parity is made.

Build the cumulative `codex/forged-ui-kit` branch on the existing Windows setup.
Check Early Axes quality 3 beside Two-handed, Torch's Utility category with no redundant
One-handed footer, the new damage symbols, station emblem/hover and missing-resource
colours. Scroll Club's detail to the last damage badge; confirm neither edge clips,
wheel travel responds immediately, updating the same item preserves position and a new
recipe starts at the top. Test short Leather Helmet content and a long mod description.
Verify recipe/action buttons, navigation panels and Settings tab markers. Check narrow
UI scales, held wheel input, drag scrolling, skin disable/re-enable and menu teardown.
Dedicated controller scrolling of the detail body remains a follow-up; existing
game controller selection and button callbacks are retained.
