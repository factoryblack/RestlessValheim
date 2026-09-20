# World map material pass

Large-map controls now use the existing shared paper kit. The terrain capture,
native pin artwork, map coordinate handling and pin/filter actions remain in place.

## Presentation

- Biome plate: 260 × 42, centred 20px Averia, bounded to 16px for long names.
- Filter rows: 240 × 44 with 18px labels, a 16px minimum and existing native icons.
- Shared/death/boss/ping controls use the same capsule switch helper as settings.
- Pin-name backing and key-hint plate use the paper material. Editable text keeps
  its native input component; caret and text-selection graphics are preserved.
- Selected pin types use a fixed-size amber edge, rather than growing into the
  next icon. Native Selectables receive the existing focus-corners feedback where
  available; no new click handlers are added to pin buttons.
- Biome/hint docking and pin-name backing follow current bounds while open.

## Lifecycle fixes

Native selected-image enabled/active state stays live for selection detection;
only its colour is hidden/restored. Menu styling previously disabled the selected
image it also consulted to determine which pin type was selected.

Changed native image, text, CanvasGroup, shared-toggle and icon-panel geometry
properties are captured before mutation and restored on undress. Reparented name
and hint plates are explicitly owned, so cleanup does not rely on their old parent.
Existing MapStudio marker adoption and return behaviour is unchanged.

## Assets

No new bitmap assets are needed. This pass consumes paper-panel, paper-chip, their
rim overlays, existing focus-corners and the shared generated capsule track. Keep
native pin symbols: replacing them would add artwork without improving recognition.
The pin-name field can use paper-chip without a separate input-field texture.

The next remaining surface for a comparable material pass is BuildMenu. General
warning/info glyphs and explicit swords on/off artwork remain separate candidates
from the ESC asset audit; neither is a dependency for this map pass.

## Verification

C# syntax parsing passes; there is no Valheim/Unity runtime or reference build in
this workspace. Compile and check these in game before merging:

1. Open/close M repeatedly; pan and zoom; player, ship and pin labels stay aligned.
2. Select every pin type with mouse and controller; exactly the active type has
   the amber backing. Focus must remain distinct from persistent selection.
3. Add, rename, cross off and remove pins. Confirm input caret, selection, typing,
   submit and cancel; check long names and IME behaviour if used.
4. Toggle shared map, deaths, bosses and pings; visual state must match visibility.
5. Change UI scale/resolution while open; inspect hints/filter collision at narrow
   resolutions and confirm pin selection remains accessible.
6. Disable/re-enable the map style; original controls, text and panel positions
   return. Repeat after reconnect/scene change and look for orphaned plates.

This branch is based on main and does not require unmerged collection or ESC PRs.
