# Cookbook polish and preparation timing

## Presentation
Generated cook-portrait.png is a reusable shared Core asset (real alpha centre/outside), used around actual game icons. The recipe tree now uses larger portrait cards; a selected dish has its own portrait, title and state ribbon. Orders show every requirement with a separate status/progress track. Fractions come from native rack timers or the persisted preparation job; unavailable timers are not fabricated. Pantry uses Kitchen.Pantry/Take and protects reserved food.

Filters: All, Feasts, Meals, Ingredients (prepared sideboards), Meads. Vanilla mead-base recipes are discovered from ObjectDB with actual ingredients, quantities and station levels. This adds no fermentation automation. Unsupported station/conversion types still need backend adapters; no brewing duration is simulated.

## Real preparation jobs
Cauldron, mead kettle and preparation-table crafts now commit ingredients and start a persisted job instead of adding output immediately. Output is added only on completion. Jobs use the native InventoryGui crafting duration when available (2-second headless fallback); cook.yaml rows may explicitly override it with preparation_seconds. Zero/omitted means native. No existing recipe durations are arbitrarily lengthened.

One preparation job per station kind per kitchen may run at a time; different kinds can run together. This is a conservative per-table work capacity, not a physical station reservation system across tables. Rack/oven durations remain native. Timers advance in bounded loaded simulation ticks, pause if the required station/level disappears, and persist elapsed work across owner/save transitions. Cancellation restores committed inputs to pantry and removes the job. Existing P/O save records remain supported; new W records hold jobs and paid inputs. Do not downgrade while jobs are active: older builds do not preserve W records.

The native-rack progress represents the next portion of that output, not a combined ETA for a batch. Oven/fermentation percentages are not available from the current adapter, so they do not receive invented time bars. Existing owner-authority, transfer and physical-station interaction behaviour remains a separate multiplayer validation concern.

## Validation
CI builds all modules and runs existing regressions plus preparation-clock checks (no early output threshold, pause/resume, capped catch-up, duration bounds and elapsed-value roundtrip).
Unity validation required: long names and UI scale; filter/search; deep branches; live order progression; station removed/restored mid-job; cancel mid-job then collect refunded pantry ingredients; save/reload mid-job; partial automatic delivery/full inventory; two-player owner handoff. Install matching Core and Cook DLLs. No in-game visual or multiplayer test was available in this environment.
