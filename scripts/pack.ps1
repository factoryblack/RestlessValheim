# Python 3.11+ is required for TOML-based staging.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    foreach ($script in @('sync-versions.py', 'sync-public-docs.py', 'sync-cook-readme.py')) {
        & python "scripts/$script" --check
        if ($LASTEXITCODE -ne 0) { throw "$script check failed" }
    }
    & python scripts/check-public-docs.py
    if ($LASTEXITCODE -ne 0) { throw 'documentation check failed' }
    $projects = & python scripts/stage-packages.py --projects
    if ($LASTEXITCODE -ne 0) { throw 'project discovery failed' }
    foreach ($project in ($projects | ConvertFrom-Json)) {
        & dotnet build $project -c Release
        if ($LASTEXITCODE -ne 0) { throw "$project build failed" }
    }
    & python scripts/stage-packages.py
    if ($LASTEXITCODE -ne 0) { throw 'package staging failed' }
} finally {
    Pop-Location
}
