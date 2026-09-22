# Pull Meshy FBX zips into art/drawers/mesh/. Zip filename is the drawer id.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$src = 'C:\Users\jules\Downloads'
$dest = Join-Path $root 'art\drawers\mesh'
New-Item -ItemType Directory -Force -Path $dest | Out-Null

$ids = @('wooden', 'personal', 'reinforced', 'blackmetal')

function Test-Albedo([string]$name) {
    return $name -like '*.png' -and $name -notmatch 'metallic|roughness|normal|ao|emissive'
}

function Save-Albedo1024([System.IO.Stream]$from, [string]$to) {
    $img = [System.Drawing.Image]::FromStream($from, $true, $true)
    try {
        $bmp = New-Object System.Drawing.Bitmap 1024, 1024, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try {
            $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
            $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
            $g.DrawImage($img, 0, 0, 1024, 1024)
            $bmp.Save($to, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $g.Dispose()
            $bmp.Dispose()
        }
    }
    finally { $img.Dispose() }
}

foreach ($id in $ids) {
    $zipPath = Join-Path $src "$id.zip"
    if (-not (Test-Path -LiteralPath $zipPath)) { throw "missing zip $zipPath" }
    $zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $files = @($zip.Entries | Where-Object { $_.Length -gt 0 })
        $fbx = @($files | Where-Object { $_.Name -like '*.fbx' })
        $albedo = @($files | Where-Object { Test-Albedo $_.Name })
        if ($fbx.Count -ne 1) { throw "${id}: expected 1 fbx, found $($fbx.Count)" }
        if ($albedo.Count -ne 1) { throw "${id}: expected 1 albedo, found $($albedo.Count)" }

        $fbxOut = Join-Path $dest "$id.fbx"
        $albOut = Join-Path $dest "$id.png"
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

        Write-Host ("{0,-12} fbx {1,5:N1} MB  albedo 1024 {2,4:N1} MB" -f $id, ($fbx[0].Length/1MB), ((Get-Item $albOut).Length/1MB))
    }
    finally { $zip.Dispose() }
}

python (Join-Path $PSScriptRoot 'pack-drawer-surface.py')
if ($LASTEXITCODE -ne 0) { throw "surface pack failed" }
