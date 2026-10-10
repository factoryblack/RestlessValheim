# Install exactly the plugin files declared by current package TOMLs.
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$ValheimInstall = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$Python = 'python'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pluginRoot = [IO.Path]::GetFullPath((Join-Path $ValheimInstall 'BepInEx\plugins'))
if (-not (Test-Path -LiteralPath $pluginRoot -PathType Container)) { throw "Missing plugins directory: $pluginRoot" }
if (-not $WhatIfPreference -and (Get-Process -Name valheim,valheim_server -ErrorAction SilentlyContinue)) {
    throw 'Close Valheim and the local server before installing.'
}
$planText = & $Python (Join-Path $PSScriptRoot 'stage-packages.py') --install-plan
if ($LASTEXITCODE -ne 0) { throw 'Package preflight failed; nothing installed.' }
$plan = $planText | ConvertFrom-Json
$prefix = $pluginRoot.TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
$files = @()
foreach ($package in $plan) {
    Write-Host "$($package.id) $($package.version)"
    foreach ($file in $package.files) {
        $destination = [IO.Path]::GetFullPath((Join-Path $pluginRoot $file.target))
        if (-not $destination.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Invalid target: $destination" }
        if (-not (Test-Path -LiteralPath $file.source -PathType Leaf)) { throw "Missing source: $($file.source)" }
        if ([IO.Path]::GetExtension($file.source) -eq '.dll') {
            $assembly = [Reflection.AssemblyName]::GetAssemblyName($file.source)
            $expected = [Version]($package.version + '.0')
            if ($assembly.Version -ne $expected) { throw "Stale build: $($file.source) is $($assembly.Version); expected $expected. Build every plugin before installing." }
        }
        if ((Test-Path -LiteralPath $destination) -and -not (Test-Path -LiteralPath $destination -PathType Leaf)) { throw "File target is a directory: $destination" }
        $files += [pscustomobject]@{Source=$file.source; Destination=$destination; Relative=$file.target}
    }
}
$backupRoot = Join-Path $root ('artifacts\install-backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
# Archive the deprecated plugin instead of deleting it or leaving duplicate GUIDs.
$legacy = [IO.Path]::GetFullPath((Join-Path $pluginRoot 'RestlessQoL'))
if (-not $legacy.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid legacy plugin target' }
if ((Test-Path -LiteralPath $legacy) -and $PSCmdlet.ShouldProcess($legacy, 'Archive deprecated plugin into the installation backup')) {
    New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
    Move-Item -LiteralPath $legacy -Destination (Join-Path $backupRoot 'RestlessQoL')
}
foreach ($file in $files) {
    if (-not $PSCmdlet.ShouldProcess($file.Destination, 'Back up existing file and install packaged file')) { continue }
    if (Test-Path -LiteralPath $file.Destination -PathType Leaf) {
        $backup = Join-Path $backupRoot $file.Relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Copy-Item -LiteralPath $file.Destination -Destination $backup
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $file.Destination) -Force | Out-Null
    Copy-Item -LiteralPath $file.Source -Destination $file.Destination -Force
}
Write-Host "Backup location, if files were replaced: $backupRoot"
Write-Host 'Installation does not change saved config values.'
