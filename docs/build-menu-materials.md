# Build menu: grid and detail band

The latest playtest rejected the native-position tooltip overlay. This pass replaces
it with an owned two-column detail panel and reserves space for it in the menu.

## Layout

- Compact card: width matches the fitted piece grid (never wider), centred and
  docked 16 units below it. The reserved grid+card envelope sits on the hotbar
  clearance so a short BuildUi is not glued to the top of the HUD.
- Height follows measured description and requirement row heights, bounded to
  128–280 units. Two or more requirements sit in two columns so long recipes
  are not clipped. A single-cost piece stays one column.
- Left 48%: 44-unit item icon, 24px title (minimum 20 for long names) and 18px
  description. Description overflow still scrolls; body type is never shrunk.
- Right: 30-unit requirement icons, wrapping 16px names and a separate 16px count
  column. Base row height is 38 plus 4 spacing. Station names retain the unused
  count space; leftover requirements still scroll.
- Grid fitting reserves a stable maximum card height, independent of the hovered
  piece's content. Switching pieces changes the card's bottom edge, not the grid
  position or the card's top edge. The card retains the existing hotbar reserve.
- Native scrollbar artwork and 10-unit tracks are retained.

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

## Compact follow-up validation

Syntax and pure layout arithmetic are checked; neither is a Unity visual test.
Verify the screenshot's roof piece has a compact card attached below the grid,
then hover a long-description/many-resource recipe: the grid must stay still,
costs sit two-across, card height may grow downward, and neither column may
overlap the hotbar. The pair sits above the hotbar, not at the top of the HUD.
