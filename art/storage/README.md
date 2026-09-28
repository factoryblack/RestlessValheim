# Storekeeper art

- `mesh/` — Meshy FBX (local, gitignored), the 1024 albedo, and `table.metal.png` (metal in red, smoothness in alpha).
- `runtime/` — baked `table.rcm` plus those two pictures. The plugin loads this folder from `BepInEx/plugins/RestlessStorage/mesh/`.

`scripts/import-storage-mesh.py` reads `Downloads/modelx.zip`. `scripts/bake-storage-meshes.ps1` writes the RCM through Blender and a 256px hammer icon.
