# Drawer art

- `mesh/` — Meshy FBX (local, gitignored) plus a 1024 albedo named `wooden`, `personal`, `reinforced`, `blackmetal`.
- `runtime/` — baked `.rcm` plus those 1024 albedos. The plugin loads this folder from `BepInEx/plugins/RestlessDrawers/mesh/`.

`scripts/import-drawer-mesh.ps1` reads the Meshy zips in Downloads. `scripts/bake-drawer-meshes.py` writes RCM2 through Blender. Metallic and roughness stay in the zip.
