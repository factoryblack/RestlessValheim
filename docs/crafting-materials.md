# Crafting material adoption

Builds on `codex/settings-materials`, including the tooltip foundation. Crafting now
uses the same embedded charcoal paper-panel, paper-chip, tintable torn rims and knot
divider. No new bitmap is required for this pass.

The existing live inventory layout supplies the bounds: one paper backdrop encloses
the crafting header, recipe list, detail pane, material requirements and action area.
The detail pane and header have their own surfaces; recipe selection has an amber
rim. The separate navigation strip and its real buttons use paper surfaces, preserving
their existing icons and destinations. Craft/Upgrade, Craft, Cancel and quality-step
buttons use the shared paper control and selectable feedback. Original button
graphics, transitions and colours are restored when the skin is disabled or rebuilt.

This is a material/layout adaptation to the real game's controls, not a literal copy
of the mockup's portrait dimensions or five invented navigation destinations. It
keeps the existing recipe text layout, item icons, quality diamonds, scrolling,
upgrade flow, crafting progress, controller navigation and gameplay callbacks.
Future structured recipe stats can consume the shared tooltip API; this pass does
not replace Valheim's recipe descriptions with a second hard-coded stat parser.

Material counts retain the live text rather than requiring an integer, allowing
have/need formats from other integrations. Red source shortage colours are shown
with the kit's red; other counts use amber. Feedback updates each visible frame,
including changes from nearby storage. Requirement text and active state also
participate in the dresser's change stamp. No fake material slots or stock values
are added. The existing inventory-screen setting controls this entire presentation.

## Verification

Source/diff checks and catalogue generation only. The authoring environment has no
.NET SDK or Valheim/Unity runtime; compilation and in-game appearance are unverified.

Build `src/RestlessQoL/RestlessQoL.csproj` in Release on the existing Windows setup
and install through `scripts/test-local.ps1`. Use `codex/crafting-materials` to test
tooltip, Settings and crafting together.

- Open hand crafting and a station; check recipe selection, scrolling, long names
  and descriptions, material slots and the surrounding paper at different UI scales.
- Craft with sufficient and missing bag/chest materials. Watch the counts, shortage
  colour, disabled action, progress and Cancel; change chest contents with Tab open.
- Switch Craft/Upgrade; use quality-step buttons and check recipe quality diamonds,
  station requirements and upgraded item details. Test no eligible recipes.
- Activate each existing navigation icon with mouse and controller. Check focus,
  pressed/disabled feedback and that each existing destination still opens.
- Disable/re-enable the inventory skin and reopen Tab; inspect for duplicate paper,
  stale focus colours or hidden controls. Test alongside the untested Settings pass.

Tooltip badge colours, corner size and optional tooltip quality diamonds remain the
separate follow-ups recorded in `settings-materials.md`.
