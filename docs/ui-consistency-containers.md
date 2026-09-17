# Consistency and containers

The container now shares the inventory's paper surface and recessed cells. Its
frame fits the existing grid, visible action controls, name and weight readout.
Take All and Stack use PaperControl and the existing reversible button registry,
including disabled/focus feedback. Their native callbacks and geometry remain.
The chest name loses its separate plaque and uses bounded, wrapping type. Weight
uses the same carry icon as the player inventory. A small footer reports occupied
cells / total cells, read directly from the current container inventory. It is
not a weight limit. The frame hides when no active container grid is present.

BoundedLabel is shared by compact inventory counts/binds, craft tabs, settings
key buttons/row titles and chest controls. It constrains text to the allotted
rectangle with a limited font-size range. Inventory quantities prefer 16px with
a 12px floor; this does not increase gameplay HUD text. Narrative descriptions
and tooltip/recipe stat rows retain their existing measured heights and wrapping.
Settings text inputs explicitly remain single-line so long strings can scroll
with the caret instead of wrapping out of a 28px field.

Inventory pointer focus now ignores exits into descendant graphics, matching the
shared button feedback. No new ornaments or artwork are added. The top-right
slot-lock diamond is unchanged. No public extension API is changed in this pass.

Validation: C# syntax parsing and changed-file review. No .NET/Valheim runtime is
available here; build and playtest remain required. Check small/large chests,
rapidly switching containers, long renamed chests, empty/full capacity readings,
disabled Take All/Stack, controller navigation, long key combinations and the
inventory skin toggle. Check footer clearance at your normal UI scale; native
grid and button positions are intentionally retained.
