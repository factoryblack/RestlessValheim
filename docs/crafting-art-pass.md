# Crafting material pass

The crafting surface now has four purpose-built assets, exposed through shared
RestlessUi helpers rather than a second widget system. Existing paper, portrait
frames, Averia labels, native item icons and action buttons remain the foundation.

| Asset / helper | Role |
| --- | --- |
| craft-station-corner / StationCorner | Open iron corner around the native station icon; restores distinct workbench, forge and modded station identities |
| craft-tab-ribbon / CraftTab | Leather ribbon for Craft/Upgrade, with a separate live label and compact selected marker |
| craft-material-socket / MaterialSocket | Transparent icon opening with a baked lower ledge for the live requirement count |
| craft-selection-clasp / RecipeSelection | Small left-edge selection cue; existing native row target and quality remain intact |

Resource icons and quantities have separate rectangles. The lower 27% of each
native cell is reserved for its count. Cells and hit targets keep their original
positions. Empty/unused requirements still clear their count and hide their socket;
shortage colour and missing-resource feedback continue to follow native values.
The station requirement medal uses the selected recipe's required station icon.

The new sprites are cached through Kit and included by the existing PNG resource
glob. Runtime PNGs total about 211 KiB. Original generations are retained under
art/crafting/originals; scripts/export-crafting-art.cjs records alpha crops and
dimensions in art/crafting/manifest.json. Export only crops transparent margins,
downsamples and adds two transparent pixels; it does not synthesize new artwork.

No update loop, polling cadence, crafting calculation, scroll behaviour, recipe
parser or input binding is added. Owned art is removed through the existing Ours
registry (or its owned parent); native icon rectangles use CraftRects restoration.

## Validation

The review board is an asset-size study, not a Unity screenshot. In-game validation
is still required: open handcraft/workbench/forge and a modded station; switch
Craft/Upgrade; select recipes with zero through all supported material slots;
check a shortage, long count, station level, gamepad focus and disabled action;
toggle the Restless inventory off/on and compare native icon/layout restoration.
Check low/high UI scales for icon/count clearance. Vanilla scrollbars are unchanged.

## Art direction / generation briefs

All four originals were generated separately as transparent, orthographic sprites.
Palette: matte charcoal leather, dark forged iron, restrained antique bronze.
No baked labels, item icons, runes, glow, background or cast shadow.

- Station corner: asymmetrical upper-left L bracket, short folded leather flap,
  two small rivets, open right/bottom and transparent centre; readable at 96px.
- Socket: tall square frame, upper 76% transparent for a native resource icon,
  shallow blank leather quantity ledge, thin iron and bronze corner clips; 80px use.
- Tab: blank horizontal leather strap with notched ends, thin iron top lip,
  bronze lower stitching and a single small left clasp; 140x38px use.
- Selection: narrow vertical dark-iron strip, bronze folded right edge and small
  right-facing notch; no diamond or padlock; 6x28px use.
