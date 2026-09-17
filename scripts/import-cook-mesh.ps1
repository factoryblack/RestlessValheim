# Pull Meshy FBX zips into art/cook/mesh/, named from cook.yaml ids.
# Zip filename is already the id. Files inside are Meshy auto-names.
# Keep the mesh as-is (local / gitignored) and a 1024 albedo for the repo.
# Leave metallic / roughness / extras in the zip. 4K masters stay in Downloads.
# Source: C:\Users\jules\Downloads\valheim foods\fbx\{id}.zip
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$src = 'C:\Users\jules\Downloads\valheim foods\fbx'
$dest = Join-Path $root 'art\cook\mesh'
if (-not (Test-Path -LiteralPath $src)) { throw "missing $src" }
New-Item -ItemType Directory -Force -Path $dest | Out-Null

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

function Test-Albedo([string]$name) {
    return $name -like '*.png' -and $name -notmatch 'metallic|roughness|normal|ao|emissive'
}

function Save-Albedo1024([System.IO.Stream]$from, [string]$to) {
    $src = [System.Drawing.Image]::FromStream($from, $true, $true)
    try {
        if ($src.Width -ne $src.Height) {
            throw "albedo is $($src.Width)x$($src.Height); expected square"
        }
        $bmp = New-Object System.Drawing.Bitmap 1024, 1024, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.DrawImage($src, 0, 0, 1024, 1024)
            $bmp.Save($to, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $g.Dispose()
            $bmp.Dispose()
        }
    }
    finally { $src.Dispose() }
}

$log = @()
$log += "id`tmeshy_fbx`tmeshy_albedo`tfbx_bytes`talbedo_4k_bytes`talbedo_1024_bytes"
foreach ($id in $ids) {
    $stem = $id.Replace('_', '-')
    $zipPath = Join-Path $src "$stem.zip"
    if (-not (Test-Path -LiteralPath $zipPath)) { throw "missing zip $stem.zip" }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $files = @($zip.Entries | Where-Object { $_.Length -gt 0 })
        $fbx = @($files | Where-Object { $_.Name -like '*.fbx' })
        $albedo = @($files | Where-Object { Test-Albedo $_.Name })
        if ($fbx.Count -ne 1) { throw "${stem}: expected 1 fbx, found $($fbx.Count)" }
        if ($albedo.Count -ne 1) { throw "${stem}: expected 1 albedo, found $($albedo.Count) ($(($albedo | ForEach-Object Name) -join ', '))" }

        $fbxOut = Join-Path $dest "$stem.fbx"
        $albOut = Join-Path $dest "$stem.png"
        $fbxStream = $fbx[0].Open()
        try {
            $out = [System.IO.File]::Create($fbxOut)
            try { $fbxStream.CopyTo($out) } finally { $out.Dispose() }
        }
        finally { $fbxStream.Dispose() }

        $albStream = $albedo[0].Open()
        try {
            $memory = New-Object System.IO.MemoryStream
            try {
                $albStream.CopyTo($memory)
                $memory.Position = 0
                Save-Albedo1024 $memory $albOut
            }
            finally { $memory.Dispose() }
        }
        finally { $albStream.Dispose() }

        $outLen = (Get-Item -LiteralPath $albOut).Length
        $log += "{0}`t{1}`t{2}`t{3}`t{4}`t{5}" -f $stem, $fbx[0].Name, $albedo[0].Name, $fbx[0].Length, $albedo[0].Length, $outLen
        Write-Host ("{0,-32} 4K {1,5:N1} MB -> 1024 {2,4:N1} MB" -f $stem, ($albedo[0].Length/1MB), ($outLen/1MB))
    }
    finally { $zip.Dispose() }
}

$log -join "`n" | Set-Content -LiteralPath (Join-Path $dest 'manifest.tsv') -Encoding utf8
Write-Host "imported $($ids.Count) meshes + 1024 albedos -> art/cook/mesh (4K masters stay in the Meshy zips)"
