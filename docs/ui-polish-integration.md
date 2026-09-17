# UI artwork integration

Integrates the artwork from asset PR #6 (commit
`d919f8bf3cc0e5cf61db08466c25f9d12021d213`) onto main
`e77056da7878ac73e570c74c646c2e98f86a4ddc`.
The 22 runtime PNGs are byte-for-byte copies of the approved art candidates.
Their source prompts and SVG masters remain in that asset PR under art/ui-polish.

## Visible changes

- Tooltip: solid overlapping corner and separate bronze pine; open portrait frame;
  leather category strip and faceted quality lozenges; cast bronze divider centre.
  Standard width is 440 UI units. Title space is reserved beside the corner and
  metadata wraps when category and quality cannot fit together. Quality overflow
  remains explicit, and the diamonds use a tight sprite crop to avoid tiny marks.
- Crafting: bronze navigation family on native icon nodes; hammer and station socket;
  recipe portrait frame; new tab strips and selection markers; a warning symbol
  accompanies the existing red material shortage count.
- Settings: bronze header pines, category-style selected tabs, faceted slider handle,
  close symbol alongside ESC, explicit host-lock icon and shared focus feedback.
- Inventory slot locking uses the reusable lock symbol instead of a quality-like diamond.

RestlessUi owns the materials and RestlessControlFeedback owns visual hover/focus
feedback. Buttons, sliders, input fields and their existing callbacks keep control
of interaction. Focus corner sprites use preserved corners rather than stretching.
Keyboard focus is distinct from selected navigation content.
Native PVP checked/unchecked visibility and colour remain authoritative.
The equipped and expand/collapse symbols are registered for future consumers;
no new equipped or collapsible behaviour is introduced just to use an asset.

Native navigation/station sprites, colours, aspect settings and icon scales are
captured and restored when the inventory dresser is disabled. Added feedback
components and decorations are removed. Decorative sprites do not receive raycasts.
The station hover target remains intentionally interactive.

The recipe scroll implementation, cloned native scrollbar and same-recipe position
preservation from main are unchanged. Live material cleanup, host synchronization,
crafting callbacks and extension data contracts remain in place.

## Validation

Review source syntax and the final diff; confirm all registered PNGs are embedded
through the existing Assets wildcard. Check image transparency and source blob
identity. Compilation and in-game rendering require the user's Valheim installation;
this workspace has no .NET SDK or game runtime.

In-game review: open tooltip at quality 1, 3 and a high modded quality; check a long
item title/category; switch craft/upgrade recipes with different resource counts;
inspect missing material and station requirements; navigate tabs with mouse and
controller; toggle PVP; open host-locked settings; disable/re-enable custom inventory.
Verify Averia metrics, no clipped descriptions, and the retained native scrolling.
