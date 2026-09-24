# Reader scrolling and station requirements

RestlessScrollRect is the shared opt-in vertical reader component, now used by
both loadout columns as well as the existing crafting/tooltip consumers.
Default travel is 4 x 32 UI units per notch; loadout uses its 46-unit row pitch
(184 units). Travel is capped at 85% of the viewport. Fractional trackpad input
and multiple notches are retained. The first quarter of travel is immediate;
the remainder eases out over 100ms using unscaled time. Repeated input adds to
the destination; reversal discards pending travel in the old direction.

No inertia or elastic boundaries. Native scrollbar visuals, dragging and gamepad
focus remain. Direct content changes cancel pending wheel motion; resizing
clamps it. Disable and drag cancel it. The existing LateUpdate only does motion
math while animating, with no scene searches, allocation or layout rebuild.
Nearest-reader event ownership prevents simultaneous nested scrolling.

Loadout rebuilds already depend on changed row keys and source content. Those
guards remain; the existing 0.25s stat sampling is unchanged. Rows passing
under a stationary pointer during wheel motion do not repeatedly switch the
source reader; click and controller selection remain immediate. This is not a
global replacement for native ScrollRects or a settings-menu rollout.

The required station occupies a material-style socket before resources, with
an extra gutter, native station icon, required level, native shortage colour
and hover explanation. Recipe and upgrade quality determine the requirement;
handcraft hides it. The footer includes the socket in wrapping calculations.

Validation: WheelMotion console regressions cover immediate response, complete
settling, accumulation, reversal, fractional input, boundaries, shrinking/empty
content, frame partition and cancellation. CI also compiles all plugins and runs
existing loadout/set tests. Unity checks still required: mouse wheel, trackpad,
scrollbar drag, controller focus, live stat changes, UI scale, recipes with
several materials, upgrades and unmet station levels.

Selection fix: native InventoryGui.m_selectedRecipe is RecipeDataPair. The
old KeyValuePair cast silently suppressed the station, category and recipe
preview. A cached typed-field lookup now reads the actual container. CI checks
the game's managed assembly for exactly one Recipe and ItemData instance field
in the selected container, so a game update cannot silently invalidate it.
