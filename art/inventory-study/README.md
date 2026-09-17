# Restrained inventory study

User direction: keep the existing slot-lock diamond. Do not replace familiar,
quiet indicators with elaborate or literal symbols just because new art exists.
This commit restores DressLock to its original diamond, size, position and tint.
The remaining files are art candidates; the inventory layout is not wired in yet.

## New assets

- inventory-slot.png: one quiet, recessed charcoal cell, exported at 256 square.
  Prefer proportional rendering at approximately 64 UI units. No gold rim, clasp
  or emblem. A normal-state tint can darken it if required by gameplay backgrounds.
- empty-head/chest/legs/cape/utility/trinket: six 64-pixel monochrome silhouettes,
  with editable SVG masters. Draw around 32–38 units at 0.15–0.20 opacity only
  when the equipment slot is empty. The game's item artwork replaces them.
- carry-weight: a simple 64-pixel mask; around 20–24 units in the stats footer.

There are no new bespoke assets for every hover or drag state. Use the same slot
with a thin cream focus outline, a restrained selected underline, and an invalid
drop outline with existing feedback text. Keep the locked diamond independent of
all these states. Do not substitute quality lozenges for the lock diamond.
Reuse existing paper surfaces and simple rules for the attached sections.
No ornate new separators or key/count plaques are needed.

## Layout direction

Preserve the existing 8-by-4 bag including its 8-slot hotbar, the 2-by-3 equipment
area and all 3 quick slots at the far right. The hotbar remains part of the same
inventory data; the separation is visual only. Use a small gap below it and a
quiet seam. Equipment and quick slots share an attached backing, divided by space.
Armour and weight occupy the footer. No persistent section headings are needed.

Slot bindings, quality numerals and quantities require distinct positions during
integration. Keep quality compact in the inventory; the tooltip can use diamonds.
The small locked diamond remains top-right. Keep durability narrow and inset
below that corner, leaving the quantity bottom-right with clearance from the
durability track. Future rarity support must not repurpose selection or lock marks.
Use live configured keybindings, not keys baked into sprites.

## Proof and provenance

inventory-study.png assembles the actual exports at 64-pixel cell size, using
existing paper artwork. It is an empty layout/state proof, not an in-game screenshot.
No replacement item art is proposed. The mock keys X/C/V follow the supplied
screenshot as illustrative labels only; integration must read the actual bindings.
The font in the proof is a serif substitute for Averia.

The slot texture was made with the built-in image-generation tool; exact prompt
and source identifier are in prompts.json. Export processing only crops transparent
margins, resizes and pads. The seven small masks are authored SVG geometry.
All exported sprites have been decoded and checked for transparent margins.
The board was inspected for capacity, equipment/quick-slot retention and restrained
state presentation. No new runtime sprites are registered in this asset pass.
