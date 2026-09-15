# Restless Settings material pass

This change builds on `codex/tooltip-foundation` and uses its actual embedded panel,
chip, rim and ornament textures. No additional generated bitmap is needed for this
surface: the new control shapes reuse the existing circle and diamond primitives.

## Shared controls

- `PaperSettingsHeader`: 1120 × 86 overhanging material header, separate tree emblems.
- `PaperControl`: material surface with pointer interaction explicitly enabled.
- `PaperSwitch`: existing Button switch behaviour, round on/off thumb and accent edge.
- `PaperSlider`: 200 × 28 hit area, thin filled track and 20 × 20 diamond handle.
- `PaperSelectable`: shared hover/focus, pressed and disabled colour transitions.
- `PaperDivider`: existing small knot motif and rules used for section headings.

The 960 × 720 sheet fits its canvas including header overhang. Tabs, title and ESC
have separate horizontal regions. Gameplay lock indication lives in the footer beside
wrapped descriptions. Nested rows retain their grouping with a smaller indent.
The selected tab has a material plate rather than the old glow image. Background
dimming is reduced so the world remains visible around the opaque sheet.

All existing configuration entries, range/snap rules, six tab definitions, key capture,
scroll relay and fade/input-block lifecycle remain. Q/E and the panel hotkey are
suspended while an InputField is focused. Keybind buttons remain local and editable
even when their paired gameplay enable switch is host-locked.

## Validation

Source review and diff checks only; there is no .NET SDK or Valheim/Unity runtime in
the authoring environment. This is a draft implementation, not a verified screenshot.

On the existing Windows setup:

1. Build with `dotnet build src/RestlessQoL/RestlessQoL.csproj -c Release` and install
   through `scripts/test-local.ps1`. Open with F8 and through the pause menu.
2. Check Storage, Building, Player, World, HUD and Character; hide/show the Character
   tab through its existing toggle. Verify selected plates and Q/E navigation.
3. Drag each slider, confirm min/max and fractional snapping, and check the fill
   follows the diamond. Scroll while hovering controls and reach the final row.
4. Toggle a bool and a keyed bool; verify thumb position, value and saved config.
   Rebind with modifiers and cancel with ESC. Type Q/E in the World's text fields:
   typing must not change tabs. Click outside the field and confirm shortcuts resume.
5. Join as a non-admin client under host lock. Gameplay controls must be disabled;
   local keybinds and HUD preferences must remain usable. Verify host indication.
6. Check keyboard focus/disabled states, hover descriptions and narrow/short canvases.
   Close via ESC, the visible button and the dim backdrop. Reopen during fade-out.
7. Compare the actual material against the supplied reference, especially tiled grain,
   header seams, type readability and the thumb/slider hit areas at your UI scale.

Tooltip follow-ups recorded from feedback: stronger badge colour differentiation,
larger corner plate around the emblem and optional quality diamonds. This Settings
change does not silently alter those tooltip decisions.
