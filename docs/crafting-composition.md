# Crafting screenshot follow-up

The September 23 screenshots exposed a composition problem, not a need for more
ornament. Crafting stacked translucent paper on the body, header and detail panel;
each recipe row repeated a heavy torn border. Material quantities were constrained
to a proportional lane smaller than their minimum font line height.

This pass captures a stationary native envelope once per dressed screen, in local
coordinates, and lays out the header, recipe list, detail area and action footer
inside it. It does not derive new bounds from already moved icons or wrapped copy.
The existing native controls, recipe selection and input handlers remain in use.

- One full-opacity paper backing; transparent inset header/detail regions.
- Aligned station icon, left-aligned title, level badge, tabs and upgrade controls.
- Quieter recipe rows with a selected fill and the existing small clasp.
- Smaller portrait, category below the item name, and less redundant reader inset.
  The category/handedness line is removed from description notes, not unknown prose.
- Visible materials pack into rows above Craft; the station requirement is a
  single labelled line rather than another socket. The outer sheet stays fixed.
- Counts have a dedicated 26-unit lane and explicit overflow policy; native text
  and shortage colours remain the source, including nearby-storage have/need copy.
- All moved native rectangles use the existing restoration registry. Existing
  refresh gating and native scrollbar artwork are retained; no new update loop.

The loadout screenshot gets a small companion correction: set wells use the shared
paper material, piece rails are compact, and the selected plaque no longer repeats
the set name immediately above the same set name.

## Validation limits

Compile and existing arithmetic/set-parser checks run in GitHub Actions. No live
Unity renderer is available here. Playtest handcraft, workbench and forge; Craft
and Upgrade including multi-quality controls; long descriptions; zero/one/many
requirements and shortages; cancelling a craft; scrolling both panes; gamepad
selection; UI scaling and toggling Restless inventory off/on. Compare native
control restoration. No new imagery or gameplay/stat calculation is introduced.

## September 23 containment correction

The paper rim now opts out of Unity layout groups. It was participating as a
blank row inside set wells and consuming their content space. Tooltip and loadout
corner graphics share the sheet's top-right anchor, and the crafting corner is
attached to the sheet instead of the station icon.

Material sockets use the full 80 by 104 requirement cell, with a bounded 12–14px
count in the artwork's bottom ledge. Icon and text bounds are separate. Set stat
columns have a 20px gutter and independently measured wrapping. Selected native
Craft/Upgrade buttons retain their disabled click behaviour but show selected
colours. Station requirement visibility follows recipe data, not suppressed
native text visibility; handcraft titles reclaim the missing icon's space.

No new update loop or gameplay calculation. Build validation is not Unity
visual validation: check long names, four-digit material counts, shortages,
Craft/Upgrade selection, handcraft/station transitions, set wells and UI scales.
