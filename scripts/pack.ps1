# Build the plugin and zip Thunderstore plugin + pack packages into artifacts/.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
if (-not (Test-Path $dotnet)) { $dotnet = 'dotnet' }

& $dotnet build (Join-Path $root 'src\RestlessQoL\RestlessQoL.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "build failed" }

& $dotnet build (Join-Path $root 'src\RestlessCook\RestlessCook.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "cook build failed" }

& $dotnet build (Join-Path $root 'src\RestlessPiles\RestlessPiles.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "piles build failed" }

& $dotnet build (Join-Path $root 'src\RestlessPlant\RestlessPlant.csproj') -c Release
if ($LASTEXITCODE -ne 0) { throw "plant build failed" }

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

Zip-Dir 'Restless-RestlessCore-0.1.9' {
    param($d)
    Copy-Item $dll $d
    Copy-Item (Join-Path $root 'thunderstore\plugin\manifest.json') $d
    Copy-Item (Join-Path $root 'thunderstore\plugin\README.md') $d
    Copy-Item (Join-Path $root 'thunderstore\plugin\CHANGELOG.md') $d
    Copy-Item $icon (Join-Path $d 'icon.png')
}

$packIcon = Join-Path $root 'thunderstore\pack\icon.png'
if (-not (Test-Path $packIcon)) { throw "missing thunderstore/pack/icon.png (256x256 PNG)" }

Zip-Dir 'Restless-Restless_Valheim-0.1.11' {
    param($d)
    Copy-Item (Join-Path $root 'thunderstore\pack\manifest.json') $d
    Copy-Item (Join-Path $root 'thunderstore\pack\README.md') $d
    Copy-Item (Join-Path $root 'thunderstore\pack\CHANGELOG.md') $d
    Copy-Item $packIcon (Join-Path $d 'icon.png')
}

$cookDll = Join-Path $root 'dist\RestlessCook.dll'
$cookIcon = Join-Path $root 'thunderstore\cook\icon.png'
if ((Test-Path $cookDll) -and (Test-Path $cookIcon)) {
    Zip-Dir 'Restless-RestlessCook-0.1.8' {
        param($d)
        Copy-Item $cookDll $d
        Copy-Item (Join-Path $root 'thunderstore\cook\manifest.json') $d
        Copy-Item (Join-Path $root 'thunderstore\cook\README.md') $d
        Copy-Item (Join-Path $root 'thunderstore\cook\CHANGELOG.md') $d
        Copy-Item $cookIcon (Join-Path $d 'icon.png')
        $mesh = Join-Path $d 'mesh'
        New-Item -ItemType Directory -Force -Path $mesh | Out-Null
        Get-ChildItem (Join-Path $root 'art\cook\runtime') -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -in '.rcm', '.png' } |
            ForEach-Object { Copy-Item $_.FullName (Join-Path $mesh $_.Name) -Force }
    }
}

$pilesDll = Join-Path $root 'dist\RestlessPiles.dll'
$pilesIcon = Join-Path $root 'thunderstore\piles\icon.png'
if ((Test-Path $pilesDll) -and (Test-Path $pilesIcon)) {
    Zip-Dir 'Restless-RestlessPiles-0.1.4' {
        param($d)
        Copy-Item $pilesDll $d
        Copy-Item (Join-Path $root 'thunderstore\piles\manifest.json') $d
        Copy-Item (Join-Path $root 'thunderstore\piles\README.md') $d
        Copy-Item (Join-Path $root 'thunderstore\piles\CHANGELOG.md') $d
        Copy-Item $pilesIcon (Join-Path $d 'icon.png')
    }
}

$plantDll = Join-Path $root 'dist\RestlessPlant.dll'
$plantIcon = Join-Path $root 'thunderstore\plant\icon.png'
if ((Test-Path $plantDll) -and (Test-Path $plantIcon)) {
    Zip-Dir 'Restless-RestlessPlant-0.1.3' {
        param($d)
        Copy-Item $plantDll $d
        Copy-Item (Join-Path $root 'thunderstore\plant\manifest.json') $d
        Copy-Item (Join-Path $root 'thunderstore\plant\README.md') $d
        Copy-Item (Join-Path $root 'thunderstore\plant\CHANGELOG.md') $d
        Copy-Item $plantIcon (Join-Path $d 'icon.png')
    }
}
