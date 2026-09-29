# Cookbook readability pass

Based on main c12208d. Presentation only; cooking allocation, timing, persistence and station behaviour remain on current main.

- Hero expands to 492×152. Ornament scale increases from 1/6 to 1/4.5 while the bottom knot and corners retain their aspect ratios. Text stays inside the frame with room above the bottom ornament.
- Recipe entries are 72 high at an 80 pitch, with a two-line name area and centred icon. Orders retain their completion counter. Hidden card elements reset when pooled cards are reused.
- Status copy distinguishes raw stock (Available/Missing), prepared output and the remaining quantity to make. Cooking quantities/timers and explicit blocked states remain visible.
- Odd final ingredient rows are centred. Connectors originate at the visible hero ornament; recipe branches have a chevron. Raw ingredient inspection updates the right panel without collapsing the parent tree.
- A fixed explanation immediately above Queue reports the root recipe's readiness/waiting reason. Selecting a child does not change the order target. Other tabs hide this explanation so it cannot overlap Collect.
- LookHints exposes an owner-scoped suppression token. Cookbook acquires it on open and releases on disable, hiding both vanilla and Restless hover text. Existing crosshair updates perform the check; no new per-frame scans or layout rebuilds.

In-game checks: three/four/six ingredient branches, long recipe titles, stock vs preparation statuses, progress bars, branch/back/raw inspection, tabs, and hover restoration on Escape, walking away, death and settings opening. CI compilation/regressions cannot validate the final Unity rendering.
