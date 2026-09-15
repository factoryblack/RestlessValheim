# UI playtest refinements

Follow-up to the first in-game screenshots of tooltip, Settings and crafting.
Includes the previous three material branches. No bitmap replacement in this pass:
the shared texture scale doubles the apparent edge depth, and a charcoal rim overlay
subdues baked gold on idle surfaces. Selected and elemental accents retain colour.

## Changes

- Tooltip quality previously rendered a numeric badge and the slot's numeric marker;
  it did not draw diamonds. It now has a fixed-header quality caption and geometric
  diamonds. Each diamond is an actual rotated UI square, independent of sprite/font
  availability. Available width caps the visible diamonds; the caption always shows
  the full level. Upgradeable quality-1 items also show their quality.
- The portrait no longer uses inventory slot overlays. Duplicate quality stats and
  repeated category footer lines are removed. Torch category is Utility. Crafter and
  repair requirements move into a quieter supporting group. Elemental damage gets
  distinct colours; physical damage stays neutral. The corner plate is larger and
  title layout reserves more space around it.
- Crafting description uses the same parser, measured stat rows and damage chips as
  inspect, in a clipped scrollable region. Unknown mod lines remain. Selecting a new
  recipe resets its scroll position; identical content preserves scrolling. Header
  padding is reduced. Modified Craft/Upgrade tab positions restore on undress.
- Unused material slots check the icon GameObject's active state as well as its Image
  state. The custom count is explicitly cleared and hidden when the slot is not live,
  including the per-frame refresh path. Required station level is labelled Station in
  its own existing cell; it is not treated as a resource count.
- Settings uses sliced capsule tracks and stronger section headings. Hover exit
  restores the quieter idle rim rather than exposing the original gold edge again.

## Validation status and focused playtest

Source review and catalogue generation are available here. Compilation, Unity layout,
and game interaction cannot be verified in this environment (no .NET/Valheim runtime).
This is a draft implementation, not a claim of a tested in-game result.

On the existing Windows setup, build Release and install through scripts/test-local.ps1.
Use the cumulative `codex/ui-playtest-refinements` branch.

1. Inspect Early Axes at quality 3: one quality caption and three visible diamonds;
   no quality stat or icon number, no durability slit, no duplicated Two-handed footer.
   Check quality 1, long names and unusually high mod quality levels too.
2. Inspect Torch: Utility beneath its title; neutral Blunt and orange Fire badge;
   intact durability stat; larger corner without covering the title.
3. Select a recipe with several resources, then Torch. Confirm unused slots have no
   old 25 counts. Verify the separate station requirement is readable. Switch back,
   and test missing/sufficient bag and chest materials and a higher required station.
4. Scroll a long crafting description, then select another recipe; its content should
   begin at the top. Confirm all stats/damage remain readable and Craft/Upgrade,
   progress/cancel and recipe-list scrolling still work. New detail scrolling uses
   pointer wheel/drag; dedicated controller scrolling is not implemented in this pass.
5. Toggle Settings switches, hover/exit rows and inspect selected/disabled states.
   Check tooltip and Settings at different UI scales for border tiling and clipping.
6. Disable/re-enable the inventory skin and reopen Tab: no duplicate content or stale
   custom text; original button appearance and tab positions return on undress.

Station artwork remains the game's own. Isolated station/header icons larger than
56 UI units are reduced to that size, with their original scale restored on undress.
The size guard deliberately skips larger panel parents in differently nested prefabs.
