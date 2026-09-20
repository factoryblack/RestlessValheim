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
| Scrolling | Visible dark track and existing scroll-thumb sprite | Native Scrollbar handle geometry, scroll mechanics and dragging |
| Key hints | Paper chips with corrected vertical padding | Source binding text and native controller artwork |
| Requirements/detail | No content rewrite | Native material/station feedback, descriptions and mod-provided information |

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

No new textures are needed for this pass: paper-panel/chip and their rims,
scroll-thumb, and focus-corners cover the controls. Keep native piece and category
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

This branch is independent of the unmerged collection, ESC and map passes.
