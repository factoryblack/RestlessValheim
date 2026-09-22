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
    $mesh = Join-Path $cookPlugins 'mesh'
    New-Item -ItemType Directory -Force -Path $mesh | Out-Null
    Get-ChildItem (Join-Path $root 'art\cook\runtime') -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in '.rcm', '.png' } |
        ForEach-Object { Copy-Item $_.FullName (Join-Path $mesh $_.Name) -Force }
    Write-Host "installed $(Join-Path $cookPlugins 'RestlessCook.dll')"
}

$pilesDll = Join-Path $root 'dist\RestlessPiles.dll'
if (Test-Path $pilesDll) {
    $pilesPlugins = Join-Path $valheimPlugins 'RestlessPiles'
    New-Item -ItemType Directory -Force -Path $pilesPlugins | Out-Null
    Copy-Item $pilesDll (Join-Path $pilesPlugins 'RestlessPiles.dll') -Force
    Write-Host "installed $(Join-Path $pilesPlugins 'RestlessPiles.dll')"
}

$plantDll = Join-Path $root 'dist\RestlessPlant.dll'
if (Test-Path $plantDll) {
    $plantPlugins = Join-Path $valheimPlugins 'RestlessPlant'
    New-Item -ItemType Directory -Force -Path $plantPlugins | Out-Null
    Copy-Item $plantDll (Join-Path $plantPlugins 'RestlessPlant.dll') -Force
    Write-Host "installed $(Join-Path $plantPlugins 'RestlessPlant.dll')"
}

$drawersDll = Join-Path $root 'dist\RestlessDrawers.dll'
if (Test-Path $drawersDll) {
    $drawersPlugins = Join-Path $valheimPlugins 'RestlessDrawers'
    New-Item -ItemType Directory -Force -Path $drawersPlugins | Out-Null
    Copy-Item $drawersDll (Join-Path $drawersPlugins 'RestlessDrawers.dll') -Force
    $mesh = Join-Path $drawersPlugins 'mesh'
    New-Item -ItemType Directory -Force -Path $mesh | Out-Null
    Get-ChildItem (Join-Path $root 'art\drawers\runtime') -File -ErrorAction SilentlyContinue |
        Where-Object { $_.Extension -in '.rcm', '.png' } |
        ForEach-Object { Copy-Item $_.FullName (Join-Path $mesh $_.Name) -Force }
    Write-Host "installed $(Join-Path $drawersPlugins 'RestlessDrawers.dll')"
}
Write-Host 'Launch Valheim from Steam. This install already has BepInEx + Jotunn.'
Write-Host 'Extra slots ship in this DLL. No sidecars.'
