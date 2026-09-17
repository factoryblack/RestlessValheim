# Cook art

Two different PNG families. Do not mix them.

- `docs/cook/wiki/` and `docs/cook/icons/` — isolated plate *renders* for the Thunderstore wiki and 256px item slots.
- `art/cook/mesh/` — Meshy FBX (local, gitignored) plus a **1024** albedo named from the `cook.yaml` id.
- `art/cook/runtime/` — baked `.rcm` meshes plus those 1024 albedos. This folder is what the plugin loads from `BepInEx/plugins/RestlessCook/mesh/`. RCM2 stores Unity V. RCM1 is the old flipped bake and is unflipped on load. Albedo gutters are bled so seams do not sample black.

Meshy zip filenames were already correct. Files inside were auto-named. `scripts/import-cook-mesh.ps1` keeps the mesh and downsamples the 4K albedo to 1024. `scripts/bake-cook-meshes.py` is run through Blender and writes the `.rcm`. Metallic and roughness stay in the download zip; that zip is the 4K master.
