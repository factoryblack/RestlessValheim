# Copy isolated plate renders into the repo and make 256px slot icons.
# Source: C:\Users\jules\Downloads\valheim foods\isolated\{cook.yaml-id}.png
# Full size  -> docs/cook/wiki/     (Thunderstore wiki via GitHub raw)
# 256 slot   -> docs/cook/icons/    (matrix thumbs) and src/RestlessCook/Assets/ (DLL)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$src = 'C:\Users\jules\Downloads\valheim foods\isolated'
if (-not (Test-Path -LiteralPath $src)) { throw "missing $src" }

$wiki = Join-Path $root 'docs\cook\wiki'
$thumbs = Join-Path $root 'docs\cook\icons'
$assets = Join-Path $root 'src\RestlessCook\Assets'
foreach ($dir in @($wiki, $thumbs, $assets)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

$yaml = Get-Content -LiteralPath (Join-Path $root 'cook.yaml')
$ids = @()
$pendingId = $null
foreach ($line in $yaml) {
    if ($line -match '^\s+- id:\s+(\S+)') { $pendingId = $Matches[1]; continue }
    if ($pendingId -and $line -match '^\s+operation:\s+add\s*$') {
        $ids += $pendingId
        $pendingId = $null
    }
}

function Save-Slot([string]$from, [string]$to) {
    $srcImg = [System.Drawing.Image]::FromFile($from)
    try {
        $bmp = New-Object System.Drawing.Bitmap 256, 256
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $g.DrawImage($srcImg, 0, 0, 256, 256)
            $bmp.Save($to, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $g.Dispose()
            $bmp.Dispose()
        }
    }
    finally { $srcImg.Dispose() }
}

$missing = @()
foreach ($id in $ids) {
    $stem = $id.Replace('_', '-')
    $file = Join-Path $src "$stem.png"
    if (-not (Test-Path -LiteralPath $file)) {
        $missing += $stem
        continue
    }
    Copy-Item -LiteralPath $file -Destination (Join-Path $wiki "$stem.png") -Force
    Save-Slot $file (Join-Path $thumbs "$stem.png")
    Copy-Item -LiteralPath (Join-Path $thumbs "$stem.png") -Destination (Join-Path $assets "$stem.png") -Force
}

if ($missing.Count) { throw "no isolated PNG for: $($missing -join ', ')" }
Write-Host "imported $($ids.Count) plates -> docs/cook/wiki, docs/cook/icons, src/RestlessCook/Assets"
