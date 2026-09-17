# Build, drop the DLL into the Steam Valheim BepInEx folder, print how to playtest.
# This Steam install already has BepInEx + Jötunn. Launch Valheim from Steam (not a vanilla shortcut).
param(
    [switch]$Launch
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

& $dotnet build (Join-Path $root 'src\RestlessQoL\RestlessQoL.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw 'build failed' }

& (Join-Path $PSScriptRoot 'install-local.ps1')

$valheim = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim'
$log = Join-Path $valheim 'BepInEx\LogOutput.log'
Write-Host ''
Write-Host 'Launch Valheim from Steam (this folder is already BepInEx-patched).'
Write-Host 'After the main menu, confirm BepInEx\LogOutput.log has:'
Write-Host '  RestlessCore 0.1.0 loaded (30 modules registered)'
Write-Host "  $log"
Write-Host ''
Write-Host 'Hotkeys: ` = quick stack, Shift+` = restock'
Write-Host 'Config after first launch: BepInEx\config\restless.core.cfg  (everything on by default)'
Write-Host ''
Write-Host 'Playtest:'
Write-Host '  1. Chest with wood next to a workbench — craft a hammer without taking the wood'
Write-Host '  2. ` / Shift+` against a chest that already has that stack'
Write-Host '  3. Smelter / kiln / cooking station / fermenter with ore or food only in the chest'
Write-Host '  4. Hammer-repair one piece — nearby player-built pieces should heal'
Write-Host '  5. Walk further from a workbench than vanilla (20 m)'
Write-Host '  6. Campfire stays lit with 0 wood'
Write-Host '  7. Hoe: dig / raise past 8 m (cap is 20)'
Write-Host '  8. Swim with a weapon out; empty a tombstone (death pin goes); ballista vs a tame'
Write-Host '  9. Chop trees with an axe (combo should keep); load a crossbow, swap off, swap back'
Write-Host ''
Write-Host 'r2modman: install BepInEx + Jotunn from Thunderstore, then Import local'
Write-Host '  artifacts\Restless-RestlessCore-0.1.1.zip  (run scripts\pack.ps1 first)'

if ($Launch) {
    Start-Process 'steam://rungameid/892970'
    Write-Host 'Launched via Steam.'
}
