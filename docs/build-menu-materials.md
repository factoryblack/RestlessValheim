# BuildUIV2 material pass

This pass covers the hammer/hoe/serving-tray menu dresser in RestlessCore. It uses
the current shared paper kit; it adds no gameplay, search, category or recipe logic.

## Layout and behaviour boundaries

| Area | Change | Preserved |
| --- | --- | --- |
| Grid | Quiet InventorySurface backing fitted to the ScrollRect viewport | Native grid dimensions, cells, masks, content movement and click coordinates |
| Piece card | Inset PaperControl, amber selected edge and muted hover edge | Piece icon tint, count/status labels, stars, arrows and special-piece behaviour |
| Category and tag controls | Native-sized paper controls; centred 18px Averia with bounded fit | Original layout groups, callbacks, category population and navigation |
| Search | Paper backing and text colours | Input component, localized placeholder, caret, text selection, focus and IME |
| Scrolling | Fully vanilla, per playtest feedback | Original artwork, colours, focus, handle geometry and dragging |
| Key hints | Paper chips with corrected vertical padding | Source binding text and native controller artwork |
| Requirements/detail | Shared paper backing and Averia text mirrors in native positions | Live name, description, resource/station text, counts, icons and shortage colours |

Selection is persistent amber; hover is muted. Existing Selectables receive the
shared focus-corners behaviour when present, without replacing click or submit
handlers. Piece state is read from BuildUi's selected/hovered references.

## Why the old inventory dressing was removed

DressSlot assumes ItemData. With a null item it hides inventory-like text, quality,
wear and other descendants; BuildUi pieces can use those children for different
information. The new piece backing calls shared PaperControl directly and only
hides recognised background/selection artwork. It never forces stars/arrows/icons
on, so native unavailable/hidden states remain authoritative.

The old broad cleanup also destroyed LayoutElements based on a child name. This
pass no longer does that. Owned child visuals ignore layout; native components
are not removed. Disable/re-enable after installing the new DLL in a fresh game
process rather than relying on hot-swapping from an older loaded assembly.

## Reversibility

Native image enabled state, sprite, type and colour are captured before mutation.
TMP enabled state, colour/alpha, maxVisibleCharacters and raycast state are restored.
Input Text font/colour changes are restored. Owned focus components and child
surfaces are destroyed on undress/GUI recreation. No native rect is resized or moved.

## Asset decision

No new textures are needed for this pass: paper-panel/chip and their rims plus focus-corners cover the controls. Scrollbars use vanilla art. Keep native piece and category
icons. Do not add ornate station emblems or decorative marks to each cell. Search
retains its existing input presentation instead of adding a new magnifier asset.

If a live screenshot shows a missing semantic cue, identify that specific cue
before extending the kit. There is no evidence here requiring a new build-icon pack.

## Validation and in-game checks

C# tree-sitter syntax parsing passed. Compilation is unavailable here (no dotnet or
Valheim reference environment); runtime geometry, appearance and event routing are
not verified. Test before merging:

1. Hammer: every category and tag, Show all, long/localized category labels, many
   pieces and any mod-added categories. Check native layout is not stretched.
2. Piece hover versus selected piece; unavailable pieces; counts, stars/arrows and
   special action. Verify actual selected placement piece matches the highlight.
3. Mouse wheel and scrollbar drag through a long grid; no clipping or concealed
   thumb. Controller traversal across tabs, tags and pieces; confirm/cancel.
4. Search: empty placeholder, typing, no matches, clearing, selection and caret;
   switch categories while filtering. Test non-Latin/IME input where applicable.
5. Hoe and serving-tray piece tables: short grids, special action and equipment
   switching while the menu is open. This does not add RestlessCook functionality.
6. Required materials/station and shortage feedback remain visible and update
   when selecting another piece or changing resource availability.
7. UI scale/resolution changes, open/close repeatedly, toggle styling off/on,
   reconnect and GUI recreation. Check no leftover panels or invisible labels.

Updated onto main after the collection, ESC and map passes were merged.


## Build-menu hover follow-up

The playtest showed the selected/hovered piece details still floating over the
world. BuildMenu.Hover now frames the live title, description, icon and requirement
slots with one shared paper surface (18px horizontal, 14px vertical padding).
Title uses up to 32px Averia, description up to 20px, resource names 16px and counts
18px, bounded within their native rectangles. Native recipe cost logic is not
duplicated. Requirement rich-text colours and non-neutral source colours remain
live. No decorative corner is added to compete with the compact detail space.

The mirrors are outside native layout participation and do not intercept clicks.
Source text is hidden by alpha only; alpha is restored and the panel/mirrors are
destroyed when selection closes, the style is disabled or the GUI is recreated.
Grid backing edges are inset 3px to keep torn lips clear of adjacent controls.

Follow-up runtime checks: Anvils/another station upgrade, a long description,
no-cost special action, red missing-resource counts and station requirement;
switch between pieces with different requirement counts to catch stale labels.
Check the paper stays above the hotbar and below the grid at each UI scale. Long
localized copy must fit the native text rectangles; syntax checks cannot verify
that. Confirm the placement helper returns when the menu closes.
