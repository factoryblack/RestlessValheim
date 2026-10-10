# Build every registered plugin, install complete packages, and print playtest guidance.
[CmdletBinding()]
param(
    [switch]$Launch,
    [switch]$WhatIf,
    [string]$ValheimInstall = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $WhatIf) {
    & (Join-Path $PSScriptRoot 'pack.ps1') -Python $Python
}
& (Join-Path $PSScriptRoot 'install-local.ps1') -ValheimInstall $ValheimInstall -Python $Python -WhatIf:$WhatIf
Write-Host 'Launch modded through Steam, then check BepInEx\LogOutput.log for the versions printed above.'
Write-Host 'Archives in artifacts use the current manifest versions; import those when testing with r2modman.'
Write-Host 'F8 shows saved host settings. Existing custom values stay intact and Global Reach defaults off.'
Write-Host 'Test crafting/building from chests, quick stack/restock, and station ore/fuel pulls.'
Write-Host 'Test repair and sealing, then station circles while changing Global Reach and turning it off.'
Write-Host 'Test crop/hive harvest, replanting, grid rows/columns and personal snapping.'
Write-Host 'Test pile isolation below and above Core reach, Storage takes, Drawers and workshop orders.'
Write-Host 'Use docs/maintainers/reliability.md for multiplayer and ownership checks; test 120m on a dense base.'
if ($Launch -and -not $WhatIf) { Start-Process 'steam://rungameid/892970' }
