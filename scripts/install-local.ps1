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

$cookDll = Join-Path $root 'dist\RestlessCook.dll'
if (Test-Path $cookDll) {
    $cookPlugins = Join-Path $valheimPlugins 'RestlessCook'
    New-Item -ItemType Directory -Force -Path $cookPlugins | Out-Null
    Copy-Item $cookDll (Join-Path $cookPlugins 'RestlessCook.dll') -Force
    Write-Host "installed $(Join-Path $cookPlugins 'RestlessCook.dll')"
}
Write-Host 'Launch Valheim from Steam. This install already has BepInEx + Jotunn.'
Write-Host 'Extra slots ship in this DLL. No sidecars.'
