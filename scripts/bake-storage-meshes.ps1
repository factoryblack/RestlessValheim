# Bake art/storage/mesh/table.fbx into art/storage/runtime/table.rcm through Blender.
$ErrorActionPreference = 'Stop'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if (-not (Test-Path -LiteralPath $blender)) { throw "missing $blender" }
$script = Join-Path $PSScriptRoot 'bake-storage-meshes.py'
& $blender --background --python $script
if ($LASTEXITCODE -ne 0) { throw "bake failed" }
