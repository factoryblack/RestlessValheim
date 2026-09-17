# Bake art/cook/mesh/*.fbx into art/cook/runtime/*.rcm through Blender.
$ErrorActionPreference = 'Stop'
$blender = 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe'
if (-not (Test-Path -LiteralPath $blender)) { throw "missing $blender" }
$script = Join-Path $PSScriptRoot 'bake-cook-meshes.py'
& $blender --background --python $script
if ($LASTEXITCODE -ne 0) { throw "bake failed" }
