param(
    [string]$OutputPath = ""
)

Add-Type -AssemblyName System.Drawing

# Resolve the output path: explicit parameter, then AIZEN_SOURCE_ROOT (CI/cloud build),
# then the local development folder.
if (-not $OutputPath) {
    $root = $env:AIZEN_SOURCE_ROOT
    if (-not $root) { $root = Split-Path -Parent $PSScriptRoot }   # repo root, no hard-coded path
    $OutputPath = Join-Path $root "src\AizenSearch.App\AizenRex.ico"
}
$outputPath = $OutputPath
$sizes = @(256, 128, 64, 48, 32, 16)
$pngBytesList = @()

foreach ($sz in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($sz, $sz, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $scale = $sz / 256.0

    # Background Fluent blue volumetric ring
    $blueBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(30*$scale, 30*$scale)),
        (New-Object System.Drawing.PointF(220*$scale, 220*$scale)),
        [System.Drawing.Color]::FromArgb(255, 0, 162, 237),
        [System.Drawing.Color]::FromArgb(255, 0, 41, 82)
    )
    $g.FillEllipse($blueBrush, 32*$scale, 40*$scale, 184*$scale, 184*$scale)

    # Violet Loop behind
    $violetBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(40*$scale, 20*$scale)),
        (New-Object System.Drawing.PointF(180*$scale, 160*$scale)),
        [System.Drawing.Color]::FromArgb(240, 194, 133, 255),
        [System.Drawing.Color]::FromArgb(240, 45, 11, 102)
    )
    $g.FillEllipse($violetBrush, 50*$scale, 30*$scale, 130*$scale, 130*$scale)

    # Dark Cavity
    $cavityBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 19, 27, 38))
    $g.FillEllipse($cavityBrush, 76*$scale, 82*$scale, 96*$scale, 96*$scale)

    # Prismatic Prism Core
    $prismBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(122*$scale, 78*$scale)),
        (New-Object System.Drawing.PointF(122*$scale, 148*$scale)),
        [System.Drawing.Color]::FromArgb(255, 128, 234, 255),
        [System.Drawing.Color]::FromArgb(255, 0, 90, 158)
    )
    $pts = @(
        (New-Object System.Drawing.PointF(122*$scale, 78*$scale)),
        (New-Object System.Drawing.PointF(88*$scale, 148*$scale)),
        (New-Object System.Drawing.PointF(156*$scale, 148*$scale))
    )
    $g.FillPolygon($prismBrush, $pts)

    # Inner cutout
    $innerPts = @(
        (New-Object System.Drawing.PointF(122*$scale, 104*$scale)),
        (New-Object System.Drawing.PointF(108*$scale, 136*$scale)),
        (New-Object System.Drawing.PointF(136*$scale, 136*$scale))
    )
    $g.FillPolygon($cavityBrush, $innerPts)

    # Amber crossbar
    $amberBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        (New-Object System.Drawing.PointF(96*$scale, 130*$scale)),
        (New-Object System.Drawing.PointF(148*$scale, 130*$scale)),
        [System.Drawing.Color]::FromArgb(255, 255, 211, 56),
        [System.Drawing.Color]::FromArgb(255, 232, 17, 35)
    )
    $pen = New-Object System.Drawing.Pen($amberBrush, [Math]::Max(1.0, 6*$scale))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($pen, 96*$scale, 130*$scale, 148*$scale, 130*$scale)

    # Specular bulb
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
    $g.FillEllipse($whiteBrush, 88*$scale, 72*$scale, [Math]::Max(2.0, 16*$scale), [Math]::Max(2.0, 16*$scale))

    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $pngBytesList += ,$ms.ToArray()
    $ms.Dispose()
    $bmp.Dispose()
}

$fs = New-Object System.IO.FileStream($outputPath, [System.IO.FileMode]::Create)
$bw = New-Object System.IO.BinaryWriter($fs)

$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)

$offset = 6 + ($sizes.Count * 16)

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $s = $sizes[$i]
    $bytes = $pngBytesList[$i]
    $w = if ($s -ge 256) { [byte]0 } else { [byte]$s }
    $h = if ($s -ge 256) { [byte]0 } else { [byte]$s }

    $bw.Write($w)
    $bw.Write($h)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$bytes.Length)
    $bw.Write([UInt32]$offset)

    $offset += $bytes.Length
}

for ($i = 0; $i -lt $sizes.Count; $i++) {
    $bw.Write($pngBytesList[$i])
}

$bw.Flush()
$fs.Close()
Write-Host "Generated multi-resolution ICO at: $outputPath"
