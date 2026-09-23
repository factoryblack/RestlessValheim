# Drawer art

- `mesh/` — Meshy FBX (local, gitignored) plus a 1024 albedo named `wooden`, `personal`, `reinforced`, `blackmetal`.
- `runtime/` — baked `.rcm`, the 1024 albedo, and `{id}.metal.png` (metal in red, smoothness in alpha). The plugin loads this folder from `BepInEx/plugins/RestlessDrawers/mesh/`.

`scripts/import-drawer-mesh.ps1` reads the Meshy zips in Downloads. `scripts/pack-drawer-surface.py` folds each zip's metal and roughness pictures into `{id}.metal.png`. `scripts/bake-drawer-meshes.py` writes RCM2 through Blender and copies both pictures.
