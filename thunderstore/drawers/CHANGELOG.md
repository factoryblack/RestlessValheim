# Changelog

## 0.1.0

- Wood, personal, reinforced, and black metal drawers clone their matching vanilla chests
- Same slot count, same recipe, same unlock timing
- One container per cabinet; the fronts are visual only
- Shared grid: any drawer snaps to any other when close and facing the same way
- Snap pitch follows the cabinet you are joining, with a 4cm overlap so lids and sides touch
- Drawers skip WearNTear support: they will not collapse, and they no longer sit on the leftover chest AABB
- Same-column aim stacks on the lid; occupancy is the cell origin, not the collider
- Lit shader comes from the chest body — skip snow / stack / wind overlays (those explode the ghost)
- The four most abundant stacks show as icons on the front
- Nearby craft, build, quick-stack, and vacuum treat them as chests
- First Thunderstore ship. F8 links this page once Core 0.1.13 is loaded.
