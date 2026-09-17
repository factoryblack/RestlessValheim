# Character and skills presentation

This pass updates the existing F8 Character ledger tab and the inventory's native
Skills dialog. It does not add a second character menu or progression system.

## Character

Journey, Survival, and Craft and gather organise all nine existing summary values.
Related values share paired rows. RestlessUi.Metric is a reusable display-only
label/value component with bounded type, consistent padding and right-aligned
values. Hunts retains Ledger's ordering and now shows every recorded enemy instead
of stopping at eight. Empty hunts has an explicit message; Additional records
appears only when Ledger provides entries. The existing scroll area handles long
lists. Counting, persistence and refresh timing remain owned by Ledger/settings.

## Skills

The shared paper surface is fitted around the native viewport, title, totals and
close controls, not around scrolling list content. Existing skill rows use the
shared paper control material. Names prefer 18px, levels 20px and bonuses 16px,
with bounded sizing for the original rectangles. Totals and close controls adopt
the same typography and interaction treatment as other Restless screens.

Icons, hover descriptions, native progress-bar geometry/value calculations,
scroll behaviour and input handlers remain in place. Bar image colours use the
shared ink/cream palette and are restored when the UI is undressed. Skill total,
list count and native text changes now participate in the refresh signature, so
repopulated skill lists and changing level/bonus text are mirrored while open.

No new images, skill-tree rules, rarity mechanics or public APIs are introduced.

## Validation

C# syntax parsing and source-diff review completed. Compilation and in-game
rendering remain unverified because no .NET SDK/Valheim installation is available.

In game, check Skills with few/many skills and bonuses, scrolling, close/controller
focus and disable/re-enable. Check Character with zero hunts, more than eight
enemy types, large counts and long extra-record labels. Verify native progress
bars remain readable and reflect the same progress as before.
