# Python 3.11+ is required for TOML-based staging.
param([string]$Python = 'python')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    foreach ($script in @('sync-versions.py', 'sync-public-docs.py', 'sync-cook-readme.py')) {
        & $Python "scripts/$script" --check
        if ($LASTEXITCODE -ne 0) { throw "$script check failed" }
    }
    & $Python scripts/check-public-docs.py
    if ($LASTEXITCODE -ne 0) { throw 'documentation check failed' }
    $projects = & $Python scripts/stage-packages.py --projects
    if ($LASTEXITCODE -ne 0) { throw 'project discovery failed' }
    foreach ($project in ($projects | ConvertFrom-Json)) {
        & dotnet build $project -c Release
        if ($LASTEXITCODE -ne 0) { throw "$project build failed" }
    }
    & $Python scripts/stage-packages.py
    if ($LASTEXITCODE -ne 0) { throw 'package staging failed' }
} finally {
    Pop-Location
}
