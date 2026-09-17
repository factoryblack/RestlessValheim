# Build the plugin and zip Thunderstore plugin + pack packages into artifacts/.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

& $dotnet build (Join-Path $root 'src\RestlessQoL\RestlessQoL.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "build failed" }

$dll = Join-Path $root 'dist\RestlessCore.dll'
if (-not (Test-Path $dll)) { throw "missing $dll" }

$icon = Join-Path $root 'thunderstore\icon.png'
if (-not (Test-Path $icon)) { throw "missing thunderstore/icon.png (256x256 PNG)" }

$out = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Zip-Dir([string]$name, [scriptblock]$stage) {
    $stageDir = Join-Path $out $name
    if (Test-Path $stageDir) { Remove-Item $stageDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $stageDir | Out-Null
    & $stage $stageDir
    $zip = Join-Path $out "$name.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $zip
    Write-Host "wrote $zip"
}

Zip-Dir 'Restless-RestlessCore-0.1.1' {
    param($d)
    Copy-Item $dll $d
    Copy-Item (Join-Path $root 'thunderstore\plugin\manifest.json') $d
    Copy-Item (Join-Path $root 'thunderstore\plugin\README.md') $d
    Copy-Item (Join-Path $root 'thunderstore\plugin\CHANGELOG.md') $d
    Copy-Item $icon (Join-Path $d 'icon.png')
}

Zip-Dir 'Restless-RestlessCorePack-0.1.1' {
    param($d)
    Copy-Item (Join-Path $root 'thunderstore\pack\manifest.json') $d
    Copy-Item (Join-Path $root 'thunderstore\pack\README.md') $d
    Copy-Item (Join-Path $root 'thunderstore\pack\CHANGELOG.md') $d
    Copy-Item $icon (Join-Path $d 'icon.png')
}
