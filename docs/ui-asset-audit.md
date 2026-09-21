# Reusable UI asset audit — 20 September 2026

Scope: assets embedded in RestlessCore plus the ESC, map and building dressers.
This is a source audit, not proof of how every asset renders in Unity. The separate
collection-screen pass is PR13; this ESC pass does not assume it has been merged.

## Use what exists

| Surface or role | Existing kit | Decision |
| --- | --- | --- |
| ESC and logout/quit panels | paper-panel + paper-panel-rim | Reuse; no new exit/save/settings icons. |
| Menu actions | paper-chip + paper-chip-rim; focus-corners | Reuse with live selected/pressed/disabled control states. |
| Close/back controls | utility-close; text and key chips | Reuse; avoid a different X for each screen. |
| Item detail | corner-overlay, pine-emblem, portrait-frame, category-strip | Available; adjust padding/scale before commissioning replacement artwork. |
| Item quality | quality-lozenge | Keep beside category; no repeated Quality caption. |
| Inventory lock | diamond | Keep the small top-right diamond. utility-lock is not a replacement. |
| Inventory empty slots | empty-head/chest/legs/cape/utility/trinket | Complete for current slots; keep quiet and hide when occupied. |
| Scroll controls | scroll-thumb and native scroll mechanics | Reuse; spacing/hit area are layout concerns. |
| Crafting | craft-hammer, station-socket, forged-badge/action, damage glyphs | Available; no extra large station illustration needed. |
| Map | map-player; native pin sprites; shared controls | Keep recognizable native pins. MapScreen still uses older strip/chip presentation. |
| Building | native piece icons; shared controls | BuildMenu still uses older chip presentation. Avoid new piece-icon pack. |

## Small asset candidates to resolve before further screen passes

These are proposed candidates, not assets added or features implemented here.

| Candidate | Why it may help | Scope and acceptance |
| --- | --- | --- |
| Explicit swords on/off pair | Only one nav-swords PNG is embedded; a filename audit cannot establish runtime state clarity. | Check live PVP first. If tint alone is ambiguous, use matched silhouettes with a structural difference; do not rely on colour alone or change PVP logic. |
| Compact warning/information glyph pair | utility-missing exists, but is not a general warning vocabulary. Useful for host restrictions, missing requirements and confirmations where an icon earns its space. | Small single-colour transparent glyphs, tintable separately, readable at 16–24px. Pair with text; never add to every button. |
| Quiet long-field backing | Search/pin-name controls may need a calmer narrow surface than an action button. | First test existing paper-chip at the target size. New art only if its borders visibly crowd editable text. |

## Deliberately defer

- No new ornate corners, crest banners, giant separators or full-screen baked UI.
- No replacement trophy, skill, building or map-pin illustrations without an actual
  readability problem in a live screenshot.
- No rarity or skill-tree asset pack until those external API consumers define
  their states. Keep colour, glyph, label and backing independently composable.
- Native settings/player-list/cloud-warning screens need their own layout review,
  not broad decoration applied through ESC.

For any new bitmap, deliver transparent, tightly bounded art without baked words,
document intended pixel size and slicing, and compare idle/focus/disabled states on
the same charcoal surface. Test at game scale before expanding the set.
