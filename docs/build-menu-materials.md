# Build menu: grid and detail band

The latest playtest rejected the native-position tooltip overlay. This pass replaces
it with an owned two-column detail panel and reserves space for it in the menu.

## Layout

- Paper detail band: maximum width 1400, 32px minimum side margins, height 22% of
  HUD height bounded to 190–240 units; bottom offset at least 130 units for hotbar clearance.
- Left 58%: 62px piece icon, wrapped title (28px, minimum 20), then 20px description.
  Long descriptions use a clipped, wheel-scrollable area without reducing body type.
- Right: native requirements rendered as rows, with a 36px icon, wrapping 18px
  name and separate 18px count. Station-only rows use the count space for the name.
  Long names increase row height. Additional requirements scroll within this column.
- Grid: fit live tab/search/viewport/scrollbar bounds to the remaining space above
  the band with a uniform BuildUi transform scale and position change. Native cells,
  layouts, navigation and coordinate relationships stay together. Never enlarge the
  native grid above its original scale.
- Native scrollbar sprites, handle length/value and interaction states remain.
  Track thickness is explicitly 10 UI units; no custom scrollbar artwork or focus.

## Live data and cleanup

All text, icons, amounts and station labels come from the native HUD output.
No recipe costs or nearby-storage totals are recalculated. Requirement colour tags
and shortage colours are retained. Inactive/surplus rows are hidden each refresh.
Description/requirement scroll position resets when selected name/description
changes, not when resource amounts update.

Original detail graphics, including transparent background Images, are suppressed
individually while the menu is open. Never hide BuildHud via CanvasGroup: it also
parents the grid/search. The paper panel lives separately under the HUD root.
Source alpha, grid transform and scrollbar rect state are restored on close,
disable and GUI teardown. New child visuals are destroyed with the detail panel.

## Existing controls

Category/tag paper controls and piece-card state styling remain. Native search,
placeholder, caret, IME, piece counts/stars/arrows, callbacks and requirement logic
are retained. The grid backing is inset 3px. No new bitmap assets are introduced.

## Verification boundary

C# syntax parsing passes. No Unity runtime or reference compilation is available.
Do not treat source validation as a visual or input test.

Check in game before merge:

1. Screenshot's Thatch Roof Inner Corner 67° and Anvils: one background, clear
   Workbench name and resource counts, panel above hotbar, grid above panel.
2. Long title/description and many requirements: wrapping, wheel scroll and no
   stale rows after switching to a cheaper or no-cost piece.
3. Shortage colours and changing nearby resource counts remain live.
4. Both native scrollbars: thin track, proportional thumb, mouse drag and wheel.
5. Hammer, hoe and serving tray; tags/materials/recent/favourites; search and all
   controller routes after uniform menu scaling.
6. 1080p, 1440p, narrow aspect and UI-scale changes: verify grid readability and
   bottom hotbar clearance. The current hotbar reserve assumes the standard dock.
7. Close/disable/reopen and reconnect: native detail/placement helper returns and
   root/scrollbar transforms are restored without duplicate panels.
