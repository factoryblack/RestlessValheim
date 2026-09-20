# Compendium, trophies and achievements

All three existing inventory tabs adopt shared paper shells, rows/cards and close
controls. Shells fit active viewports, titles and controls rather than scrolling
content. Achievement details have their own shell and are excluded from the parent
panel's bounds, so opening details does not resize the parent decoration.

Compendium entries use cream names, amber selection and existing native buttons.
For a dedicated paragraph ScrollRect, the mirrored body gets 20-unit padding and
18px text; preferred height drives the scroll content extent. Both text-as-content
and a content node containing just the text/mirror are supported. Mixed-content
prefabs retain native geometry. No replacement scroll component or input handler
is introduced. Modified content geometry and disabled layout controllers restore
through the existing UI cleanup registries.

Trophy/achievement cards retain native artwork and tints. Their material no longer
uses the inventory slot routine, which could suppress progress/chrome appropriate
to these cards. Names, descriptions and native status/progress copy use readable,
bounded labels. Previously unrecognised achievement text was silenced; it is now
mirrored with unique source-specific names. Native locked/completed indicators,
unlock calculations and GuiBar values are retained rather than inferred from text.
Achievement requirement rows and completion summaries share the same typography.

Collection source text changes and list-text counts participate in the refresh
signature. A new entry, changed requirement or completion summary can refresh while
open. No new assets, progression mechanics or public API changes are included.

Validation: C# syntax parsing and changed-file review. No .NET SDK or Valheim runtime
is available here; compilation and rendering still require testing. Check long
compendium entries through to the final line, repeated entry selection, controller
scroll/close, large trophy collections, locked/completed achievements, live progress,
details opening/closing and UI disable/re-enable. Verify mixed-content reader
fallbacks and all native indicators in the actual game prefab.
