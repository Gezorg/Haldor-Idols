Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
$bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 38, 28, 20),
    [System.Drawing.Color]::FromArgb(255, 12, 9, 7),
    [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal)
$g.FillRectangle($bg, $rect)

# Soft glow behind the idol
$glowPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$glowPath.AddEllipse(48, 40, 160, 176)
$glow = New-Object System.Drawing.Drawing2D.PathGradientBrush($glowPath)
$glow.CenterColor = [System.Drawing.Color]::FromArgb(80, 201, 162, 74)
$glow.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 201, 162, 74))
$g.FillPath($glow, $glowPath)

function New-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

$goldTop = [System.Drawing.Color]::FromArgb(255, 245, 219, 140)
$goldBottom = [System.Drawing.Color]::FromArgb(255, 176, 124, 42)

function New-GoldBrush([float]$x, [float]$y, [float]$w, [float]$h) {
    $r = New-Object System.Drawing.Rectangle([int]$x, [int]$y, [int][Math]::Max(1, $w), [int][Math]::Max(1, $h))
    return New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $r, $goldTop, $goldBottom, [System.Drawing.Drawing2D.LinearGradientMode]::Vertical)
}

# Border
$borderPath = New-RoundedPath 10 10 236 236 26
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(190, 201, 162, 74), 5)
$g.DrawPath($borderPen, $borderPath)
$innerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 245, 219, 140), 1.5)
$innerPath = New-RoundedPath 18 18 220 220 20
$g.DrawPath($innerPen, $innerPath)

# Base
$basePath = New-RoundedPath 86 196 84 18 7
$g.FillPath((New-GoldBrush 86 196 84 18), $basePath)

# Body (tapered)
$body = New-Object System.Drawing.Drawing2D.GraphicsPath
$body.AddBezier(100, 104, 92, 132, 100, 168, 108, 198)
$body.AddLine(148, 198, 148, 198)
$body.AddBezier(156, 168, 164, 132, 156, 104, 148, 104)
$body.AddLine(100, 104, 100, 104)
$body.CloseFigure()
$g.FillPath((New-GoldBrush 90 104 76 94), $body)

# Arms
$armsPath = New-RoundedPath 74 124 108 16 8
$g.FillPath((New-GoldBrush 74 124 108 16), $armsPath)

# Head
$headBrush = New-GoldBrush 104 50 48 48
$g.FillEllipse($headBrush, 104, 50, 48, 48)
$headPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(160, 255, 241, 196), 2)
$g.DrawEllipse($headPen, 104, 50, 48, 48)

# Eyes / rune marks
$dark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(210, 60, 40, 16))
$g.FillEllipse($dark, 117, 68, 6, 6)
$g.FillEllipse($dark, 133, 68, 6, 6)

# Small rune diamond on the chest
$rune = New-Object System.Drawing.Drawing2D.GraphicsPath
$rune.AddPolygon(@(
    (New-Object System.Drawing.PointF(128, 140)),
    (New-Object System.Drawing.PointF(140, 158)),
    (New-Object System.Drawing.PointF(128, 176)),
    (New-Object System.Drawing.PointF(116, 158))
))
$g.FillPath($dark, $rune)

$g.Dispose()
$bg.Dispose()

$out = Join-Path (Split-Path $PSScriptRoot -Parent) 'package\icon.png'
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

"Saved $out"
