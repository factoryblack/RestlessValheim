# Drop the built DLL into the Steam Valheim BepInEx plugins folder used for local testing.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dll = Join-Path $root 'dist\RestlessCore.dll'
if (-not (Test-Path $dll)) { throw "Build first. Missing $dll" }

$valheimPlugins = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim\BepInEx\plugins'
$old = Join-Path $valheimPlugins 'RestlessQoL'
if (Test-Path $old) { Remove-Item $old -Recurse -Force }

$plugins = Join-Path $valheimPlugins 'RestlessCore'
New-Item -ItemType Directory -Force -Path $plugins | Out-Null
Copy-Item $dll (Join-Path $plugins 'RestlessCore.dll') -Force
Write-Host "installed $(Join-Path $plugins 'RestlessCore.dll')"
Write-Host 'Launch Valheim from Steam. This install already has BepInEx + Jotunn.'
Write-Host 'Optional extras (MyLittleUI, equipment slots) are sidecars - not required for bite 1.'
