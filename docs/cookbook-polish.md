# Cookbook polish and preparation timing

## Presentation
Generated cook-portrait.png is a reusable shared Core asset (real alpha centre/outside), used around actual game icons. The recipe tree now uses larger portrait cards; a selected dish has its own portrait, title and state ribbon. Orders show every requirement with a separate status/progress track. Fractions come from native rack timers or the persisted preparation job; unavailable timers are not fabricated. Pantry uses Kitchen.Pantry/Take and protects reserved food.

Filters: All, Feasts, Meals, Prep, Bait, Meads. Six equal buttons, three per row. Prep is dough, raw fish, uncooked oven loads, and the sideboards. Bait is the fishing-bait recipes. Mead-kettle recipes, including barley wine base, are meads. Vanilla recipes are discovered from ObjectDB with actual ingredients, quantities and station levels. This adds no fermentation automation. Unsupported station/conversion types still need backend adapters; no brewing duration is simulated.

## Real preparation jobs
Cauldron, mead kettle and preparation-table crafts now commit ingredients and start a persisted job instead of adding output immediately. Output is added only on completion. Jobs use the native InventoryGui crafting duration when available (2-second headless fallback); cook.yaml rows may explicitly override it with preparation_seconds. Zero/omitted means native. No existing recipe durations are arbitrarily lengthened.

One preparation job per station kind per kitchen may run at a time; different kinds can run together. This is a conservative per-table work capacity, not a physical station reservation system across tables. Rack/oven durations remain native. Timers advance in bounded loaded simulation ticks, pause if the required station/level disappears, and persist elapsed work across owner/save transitions. Cancellation restores committed inputs to pantry and removes the job. Existing P/O save records remain supported; new W records hold jobs and paid inputs. Do not downgrade while jobs are active: older builds do not preserve W records.

The native-rack progress represents the next portion of that output, not a combined ETA for a batch. Oven/fermentation percentages are not available from the current adapter, so they do not receive invented time bars. Existing owner-authority, transfer and physical-station interaction behaviour remains a separate multiplayer validation concern.

## Validation
CI builds all modules and runs existing regressions plus preparation-clock checks (no early output threshold, pause/resume, capped catch-up, duration bounds and elapsed-value roundtrip).
Unity validation required: long names and UI scale; filter/search; deep branches; live order progression; station removed/restored mid-job; cancel mid-job then collect refunded pantry ingredients; save/reload mid-job; partial automatic delivery/full inventory; two-player owner handoff. Install matching Core and Cook DLLs. No in-game visual or multiplayer test was available in this environment.

## Screenshot follow-up: compact planning layout

The tree now uses a 420×116 featured dish and 252×116 ingredient cards. Four immediate ingredients fit within the 552-high viewport; additional branches still scroll. Portrait pivots are centred so preserved-aspect sprites stay centred inside their frames. Only the featured dish keeps the decorative portrait frame.

The right panel uses a compact header, a fixed station requirement and four visible ingredient rows with ready/needed counts. Clicking a requirement explores that branch. Dependencies fulfilled by an already prepared or cooking ancestor say Covered, rather than claiming that their raw ingredients remain in stock. Longer lists and secondary information can scroll. Neutral borders identify ordinary cards; selection is gold and missing stock uses amber status text. Native scrollbar sliding areas are normalised to six pixels with an 18-pixel content gutter.

Recipes follow biome progression, Black Forest/Deep North labels are formatted, and selecting a recipe preserves the sidebar scroll position. Queue wording explains that orders may wait for ingredients. This follow-up only changes presentation, retains pooled cards and the existing one-second snapshot refresh, and adds no per-frame stock scans.

Verify in Unity: long names at smaller resolutions, four/six-ingredient recipes, deep branch/back navigation, native scrollbar handle width, selected-order progress, and switching Kitchen/Pantry/Recipes without stale requirements. A successful CI build does not replace these visual checks.

## Hero frame follow-up

The featured recipe now uses cook-portrait as its actual outer surface through the shared CookHeroSurface helper. Nine-slice borders retain the corner folds on a wide card; the inner thumbnail frame and generic outer rim are removed. A small coloured diamond beside the status text communicates prepared/waiting/blocked independently of the bronze frame. Pooled cards restore their normal paper rim when reused in Orders or Pantry. Requires matching Core and Cook builds. In-game checks: corner proportions, long hero titles, all three status colours, and tab transitions.

## Cooking and frame corrections

New orders now request additional finished dishes. Previously Assign allocated a finished dish already in the player's inventory to the root of a new order without increasing its Ready counter. Queue-one could remain inert, while queue-two cooked only the shortfall. Root allocation now uses only that order's completed, uncollected output; ingredient and intermediate stock remains usable. The same semantics apply to the recipe preview.

The native catalogue now follows missing recipe dependencies from ObjectDB as well as discovering meads. This allows native spice blends to expand into their real inputs and station requirements without inventing cook.yaml rows or overriding custom recipes. Queuing opens the new order, and its footer reports active preparation, a blocked station or a missing raw ingredient.

Frame regions preserve the bottom-centre knot independently of the stretchable edge sections (eleven regions rather than nine). No new texture is needed. Pooled cards hide the frame when reused for ordinary rows.

Tests now link the real KitchenRun.Timing implementation against minimal game-boundary substitutes: start, persisted reload, station pause/resume, final completion, intermediate completion, duplicate-award prevention and orphan refunds. Root allocation checks reproduce queue-one/queue-two with an existing dish. These do not emulate Unity station ownership or physically loading racks; in-game testing still needs those paths.
