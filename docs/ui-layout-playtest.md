# Layout follow-up after in-game review

Follow-up on PR #7, based on its tested head 38eeef140ea4b440ca64b02ef2bb43e7e86be63b.
Main still precedes that integration, so this stays on the integration branch.

- Tooltip header measures its actual portrait and metadata instead of reserving
  122 units for a decoration. The corner is 112 units, with a proportional emblem,
  so the description can start earlier without colliding with the torn edge.
- Crafting detail owns a 24-unit inset, 64-unit portrait, 18-unit identity gap and
  measured wrapped 24-point title. Description/stat text is two points larger.
  The native scrollbar keeps its behaviour but occupies a separate gutter;
  its top now follows the measured detail header. Body row spacing is 8 units.
- Material icons occupy the upper cell area. Counts have a separate 24-unit
  dark strip and centred 20-point type, rather than overlapping the item image.
  Station numerals are centred and enlarged; a missing mirrored station readout
  is now created, and the obsolete slot background is suppressed.
- Remove the long crafting header rule. Navigation backings become shallow and
  subdued; Craft/Upgrade labels are centred at 18 points, the Craft action at 24.
- PVP is handled independently of the Button-only navigation path. Native Toggle
  state drives the crossed-swords overlay, with Player.IsPVPEnabled queried as a
  compatibility fallback for button prefabs. On is bronze; off is dark and slashed.
  Neither artwork nor state reads alter gameplay or the native input callbacks.

Native icon rectangles are captured before repositioning and restored on undress.
All PVP artwork belongs to the dresser; original image colours use its existing
restoration path. Native wheel scrolling and scrollbar style are unchanged.
No new generated textures are needed: the off state composes existing swords with
a non-interactive slash and tint.

Validation: syntax parsing plus review of resource references, input forwarding,
layout ownership and restore paths. This environment cannot compile or run Valheim.
Check long item names, recipe changes, several material counts, empty station
requirements, PVP on/off by mouse/controller, and disabling/re-enabling the dresser.
