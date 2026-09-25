# Restless UI consistency audit — 25 September 2026

Reviewed UI source at main `29b2d6567170524840eafd987693cc95af6b0299`,
including merged PR #24 and the newer native station-plate suppression.
Scope: shared kit, settings/expansions, inventory/crafting, tooltips, loadout,
collections/skills, build detail, map and menu chrome. This is a source audit,
not a Unity visual or performance certification. No gameplay calculations changed.

This PR enlarges the crafting corner from 76 to 96 UI units (26% per side),
retaining its top-left sheet anchor. Findings below are follow-up work, not fixes
included in this PR. Existing paper-rim layout exclusion and RecipeDataPair
selection decoding are already present and are not reported as open defects.

## 1. Compendium mirror can remain stationary while its content scrolls

**Priority: first functional fix. Conditional on the supported direct-text prefab.**
`Hud/InventoryScreen.Collections.cs`, `ReadableParagraph`, has a branch where
`scroll.content == source.rectTransform`. The visible Text mirror remains a
sibling of that source and receives a one-time copied position. It therefore
is not moved by ScrollRect. `RefreshCollections` runs only under `NeedsSync` in
`InventoryScreen.cs`; `MixOverlay`/`MixTmp` hash text/active state, not scroll
position. Wheel/drag movement alone does not refresh that sibling.

Reproduce with a long compendium entry whose native TextArea is the scroll
content: scroll without changing selection or inventory. The hidden TMP moves
while the visible mirror can stay still or jump on the next unrelated refresh.
Use an owned content wrapper containing the mirror, or an onValueChanged position
binding for this branch. Restore the native hierarchy/geometry on undress. Do not
solve it by rerunning the whole inventory dresser every frame.

## 2. Core settings can show stale values/editability while expansion settings update

**Priority: first functional fix. Confirmed differing subscription paths.**
`Core/SettingsUi.ExpansionControls.cs`, `ObserveSetting`, returns when `editable`
is null. Core controls in `SettingsUi.cs` normally omit that callback; they paint
the initial config value and capture `locked` during `Rebuild`. Their click/slider
callbacks likewise only recheck permissions when `editable` is non-null.
Expansion controls subscribe to SettingChanged and refresh host editability;
core controls do not share that behaviour.

Reproduce by leaving a core page open while a host changes a synchronised setting,
or while admin/edit permissions change. Compare with an expansion page. The
core view can retain old values or old enabled states until reopened/rebuilt.
This is a UI consistency finding, not evidence of a server-authority bypass.
Use the same observation/repaint path for both, without rebuilding/resetting
scroll position for routine value updates.

## 3. Scroll behaviour is still fragmented

**Priority: next shared interaction pass. Confirmed implementation differences.**

| Surface | Current input behaviour |
| --- | --- |
| Loadout totals + sources | RestlessScrollRect, 46-unit rows x 4, brief easing |
| Crafting detail | RestlessScrollRect, default 32-unit rows x 4, brief easing |
| Crafting recipe list / collections / skills | Retained native ScrollRects |
| F8 settings body | Standard ScrollRect, 240 units, no inertia/easing |
| F8 tab rail | Standard ScrollRect, sensitivity 44 |
| Build detail description + costs | Standard ScrollRect, sensitivity 32 |
| Item tooltip | Non-interactive card; PgUp/PgDn at 80% viewport, no wheel handler |

Earlier messaging saying the new handler already covered item tooltips was
incorrect. `Hud/ItemTooltip.cs`, `Bind` and `Scroll`, explicitly use page keys;
the card blocks no raycasts. Do not simply turn on raycasts: that can steal the
inventory hover that keeps the tooltip open. Tooltip input needs a deliberate
ownership/pinning or modifier-key design. Adopt the shared reader behaviour in
owned F8/build readers first; retain native scrollbar artwork and restoration
when adapting native readers.

## 4. Small hover hints have a fixed box regardless of copy length

**Priority: shared text containment. Confirmed fixed geometry; visual overflow depends on copy.**
`Core/RestlessHint.cs` always uses a 200 x 52 card with 16 horizontal and 8 vertical
padding. Text wraps, but height is never measured and there is no long-copy
fallback. Station names, extra explanatory text and localisation can exceed
that box. Position clamping also assumes the fixed dimensions.

Measure the text at a bounded width, grow height to the measured result, and
clamp the final card to the canvas. Test a long station name near both screen
edges, including an unmet requirement. Keep the compact appearance for short
hints; no extra ornamental asset is needed.

## 5. Crafting has no minimum space budget for its reader

**Priority: layout robustness for narrow screens and expansion recipes.**
`Hud/InventoryScreen.Composition.cs`, `ComposeCraftDetail`, sets footer height to
`100 + rows * 112`, while the header consumes another 116 units. No rule reserves
minimum detail height before placing those sections. `InventoryScreen.Crafting.cs`
then measures the identity and forces the reader top to at least `y0 + 50`, even
if the title/icon/category already consume the remaining detail space.

For example, a 600-unit sheet with three requirement rows leaves only 48 units
for the detail panel before the portrait/title are considered. Long names make
this worse. This is an arithmetic containment risk, not a claim that a particular
vanilla recipe currently triggers it. Reserve a minimum reader area and put an
overfull requirements region into bounded scrolling or another explicit layout.
Keep the outer sheet stable while recipe contents change.

## 6. Narrow tooltip fallback is smaller than its header's minimum geometry

**Priority: layout robustness. Confirmed coordinate mismatch at the fallback width.**
`Hud/ItemTooltip.cs`, `Bind` allows card width 180. `Rebuild` uses 18-unit side
padding, an 86-unit title offset and a minimum title width of 100. At that card
width the available inner width is 144, but title offset + width is 186: a
42-unit overshoot of the inner area. Corner reservation and category/quality
layout cannot fit either. The height scale fallback does not resolve the relative
horizontal mismatch.

Use a compact header arrangement below a measured width threshold (e.g. title
below the portrait), or enforce/scale a valid minimum card width. Test long names,
quality marks and the corner together on a narrow parent canvas.

## Shared sizing and validation follow-up

Create shared constants for reader gutter, socket/count insets, compact text
minimums and sheet-edge clearance once the above behaviours are settled. The
current differences are not all bugs: inventory/HUD density should remain distinct
from reading screens. Avoid flattening every font and padding into one size.

Suggested verification order:
1. Compendium scrolling and settings live updates/host editability.
2. F8/build reader scrolling; decide tooltip input ownership separately.
3. Measured hover hints, minimum crafting reader budget and narrow tooltip header.
4. In-game checks at UI-scale extremes, long names/translated text, large counts,
   zero/many requirements, controller focus and enabling/disabling Restless screens.

Performance constraint: use event-driven position/config updates and existing
content invalidation. No new hierarchy scans or layout rebuilds per idle frame.
