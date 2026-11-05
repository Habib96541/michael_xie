Add-Type -AssemblyName System.Drawing

$artDir = "Assets/CyberWhack/Art"
if (!(Test-Path $artDir)) { New-Item -ItemType Directory -Path $artDir -Force }

function Create-RoundedRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2.0
    if ($d -gt $w) { $d = $w }
    if ($d -gt $h) { $d = $h }
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

# ========================================================
# 1. CartoonStatPlaque (360x140) - For Time & Score
# ========================================================
$w = 360; $h = 140
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer 3D wood drop shadow
$shadowPath = Create-RoundedRectPath 6 12 ($w - 12) ($h - 18) 22
$shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 35, 18, 8))
$g.FillPath($shadowBrush, $shadowPath)
$shadowBrush.Dispose(); $shadowPath.Dispose()

# Outer Wooden Rim / Bevel
$woodPath = Create-RoundedRectPath 4 4 ($w - 8) ($h - 14) 22
$woodGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 4), (New-Object System.Drawing.PointF 0, ($h - 10)), ([System.Drawing.Color]::FromArgb(255, 95, 52, 24)), ([System.Drawing.Color]::FromArgb(255, 52, 26, 10))
$g.FillPath($woodGrad, $woodPath)
$woodPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 125, 70, 32)), 3
$g.DrawPath($woodPen, $woodPath)
$woodGrad.Dispose(); $woodPen.Dispose(); $woodPath.Dispose()

# Inner Warm Cream/Honey Board
$boardPath = Create-RoundedRectPath 14 12 ($w - 28) ($h - 30) 16
$boardGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 12), (New-Object System.Drawing.PointF 0, ($h - 18)), ([System.Drawing.Color]::FromArgb(255, 255, 248, 230)), ([System.Drawing.Color]::FromArgb(255, 250, 222, 160))
$g.FillPath($boardGrad, $boardPath)
$innerPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 205, 155, 80)), 2
$g.DrawPath($innerPen, $boardPath)
$boardGrad.Dispose(); $innerPen.Dispose(); $boardPath.Dispose()

# Icon badge backing circle on left
$circleBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 235, 190))
$g.FillEllipse($circleBrush, 22, 22, 90, 90)
$circlePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 220, 165, 85)), 3
$g.DrawEllipse($circlePen, 22, 22, 90, 90)
$circleBrush.Dispose(); $circlePen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonStatPlaque.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 2. CartoonComboTrack (620x60) - Pill groove
# ========================================================
$w = 620; $h = 60
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer chocolate rim
$rimPath = Create-RoundedRectPath 2 2 ($w - 4) ($h - 4) 28
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 80, 42, 18))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 110, 60, 28)), 2
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Inner sunken dark cavity
$cavityPath = Create-RoundedRectPath 7 7 ($w - 14) ($h - 14) 23
$cavityBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 36, 18, 8))
$g.FillPath($cavityBrush, $cavityPath)
$cavityPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(200, 20, 10, 4)), 2
$g.DrawPath($cavityPen, $cavityPath)
$cavityBrush.Dispose(); $cavityPen.Dispose(); $cavityPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonComboTrack.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 3. CartoonComboFill (620x60) - Juicy gradient energy bar
# ========================================================
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$fillPath = Create-RoundedRectPath 7 7 ($w - 14) ($h - 14) 23
$fillGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 7), (New-Object System.Drawing.PointF 0, ($h - 7)), ([System.Drawing.Color]::FromArgb(255, 255, 235, 59)), ([System.Drawing.Color]::FromArgb(255, 255, 109, 0))
$g.FillPath($fillGrad, $fillPath)

# Top glossy highlight arc
$hlPath = Create-RoundedRectPath 14 10 ($w - 28) (($h - 14) * 0.42) 12
$hlGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 10), (New-Object System.Drawing.PointF 0, 30), ([System.Drawing.Color]::FromArgb(220, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($hlGrad, $hlPath)

$fillGrad.Dispose(); $fillPath.Dispose(); $hlGrad.Dispose(); $hlPath.Dispose()
$g.Dispose()
$bmp.Save("$artDir/CartoonComboFill.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 4. CartoonIcon_Clock (100x100) - Cartoon pocket stopwatch
# ========================================================
$w = 100; $h = 100
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Top brass loop & pin
$loopPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 148, 20)), 5
$g.DrawEllipse($loopPen, 40, 4, 20, 16)
$pinBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 175, 30))
$g.FillRectangle($pinBrush, 45, 16, 10, 8)
$loopPen.Dispose(); $pinBrush.Dispose()

# Outer gold casing
$cx = 50; $cy = 58; $r = 38
$caseGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 12, 20), (New-Object System.Drawing.PointF 88, 96), ([System.Drawing.Color]::FromArgb(255, 255, 225, 75)), ([System.Drawing.Color]::FromArgb(255, 205, 130, 15))
$g.FillEllipse($caseGrad, ($cx - $r), ($cy - $r), ($r * 2), ($r * 2))
$casePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 160, 95, 10)), 3
$g.DrawEllipse($casePen, ($cx - $r), ($cy - $r), ($r * 2), ($r * 2))
$caseGrad.Dispose(); $casePen.Dispose()

# White dial face
$dialR = 30
$dialBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
$g.FillEllipse($dialBrush, ($cx - $dialR), ($cy - $dialR), ($dialR * 2), ($dialR * 2))
$dialPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 230, 210, 175)), 2
$g.DrawEllipse($dialPen, ($cx - $dialR), ($cy - $dialR), ($dialR * 2), ($dialR * 2))
$dialBrush.Dispose(); $dialPen.Dispose()

# Clock tick marks
$tickPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 120, 120, 130)), 2
$g.DrawLine($tickPen, $cx, ($cy - $dialR + 4), $cx, ($cy - $dialR + 9))
$g.DrawLine($tickPen, ($cx + $dialR - 9), $cy, ($cx + $dialR - 4), $cy)
$g.DrawLine($tickPen, $cx, ($cy + $dialR - 9), $cx, ($cy + $dialR - 4))
$g.DrawLine($tickPen, ($cx - $dialR + 4), $cy, ($cx - $dialR + 9), $cy)
$tickPen.Dispose()

# Clock hands (pointing to 10:10)
$handPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 45, 45, 55)), 3.5
$g.DrawLine($handPen, $cx, $cy, ($cx - 14), ($cy - 14))
$g.DrawLine($handPen, $cx, $cy, ($cx + 18), ($cy - 10))
$centerBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 235, 75, 60))
$g.FillEllipse($centerBrush, ($cx - 4), ($cy - 4), 8, 8)
$handPen.Dispose(); $centerBrush.Dispose()

# Glass reflection arc
$glassPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120, 255, 255, 255)), 4
$g.DrawArc($glassPen, ($cx - 24), ($cy - 24), 48, 48, 190, 80)
$glassPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonIcon_Clock.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 5. CartoonIcon_StarCoin (100x100) - Golden Star Medal
# ========================================================
$w = 100; $h = 100
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer coin bevel
$cx = 50; $cy = 50; $r = 42
$coinGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 10, 10), (New-Object System.Drawing.PointF 90, 90), ([System.Drawing.Color]::FromArgb(255, 255, 235, 80)), ([System.Drawing.Color]::FromArgb(255, 215, 130, 10))
$g.FillEllipse($coinGrad, ($cx - $r), ($cy - $r), ($r * 2), ($r * 2))
$coinPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 165, 95, 8)), 4
$g.DrawEllipse($coinPen, ($cx - $r), ($cy - $r), ($r * 2), ($r * 2))
$coinGrad.Dispose(); $coinPen.Dispose()

# Inner groove
$innerR = 34
$innerPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 245, 185, 35)), 2
$g.DrawEllipse($innerPen, ($cx - $innerR), ($cy - $innerR), ($innerR * 2), ($innerR * 2))
$innerPen.Dispose()

# 5-point star
$starPts = New-Object 'System.Drawing.PointF[]' 10
$outerR = 24.0; $starInR = 11.0
for ($i = 0; $i -lt 10; $i++) {
    $rad = ($i * 36 - 90) * [Math]::PI / 180.0
    $currR = if ($i % 2 -eq 0) { $outerR } else { $starInR }
    $starPts[$i] = New-Object System.Drawing.PointF ($cx + $currR * [Math]::Cos($rad)), ($cy + $currR * [Math]::Sin($rad))
}
$starBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 220))
$g.FillPolygon($starBrush, $starPts)
$starPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 215, 140, 20)), 2
$g.DrawPolygon($starPen, $starPts)
$starBrush.Dispose(); $starPen.Dispose()

# Glass highlight
$glassPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(140, 255, 255, 255)), 4
$g.DrawArc($glassPen, 18, 16, 64, 64, 200, 70)
$glassPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonIcon_StarCoin.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 6. CartoonBtn_Exit (120x80) - Coral Red Pause/Exit
# ========================================================
$w = 120; $h = 80
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# 3D shadow base
$shPath = Create-RoundedRectPath 4 12 ($w - 8) ($h - 16) 18
$shBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 160, 25, 40))
$g.FillPath($shBrush, $shPath)
$shBrush.Dispose(); $shPath.Dispose()

# Main coral red face
$btnPath = Create-RoundedRectPath 4 4 ($w - 8) ($h - 16) 18
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 4), (New-Object System.Drawing.PointF 0, ($h - 12)), ([System.Drawing.Color]::FromArgb(255, 255, 95, 105)), ([System.Drawing.Color]::FromArgb(255, 230, 40, 60))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 160, 165)), 2.5
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

# Top gloss arc
$glPath = Create-RoundedRectPath 12 7 ($w - 24) 26 12
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 7), (New-Object System.Drawing.PointF 0, 33), ([System.Drawing.Color]::FromArgb(180, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Exit.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 7. CartoonBtn_AudioOn & CartoonBtn_AudioOff (90x90)
# ========================================================
foreach ($isOff in @($false, $true)) {
    $w = 90; $h = 90
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # 3D shadow base
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 22, 105, 45))), 4, 12, 82, 74)

    # Main green circle
    $grnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 4, 4), (New-Object System.Drawing.PointF 86, 80), ([System.Drawing.Color]::FromArgb(255, 76, 217, 100)), ([System.Drawing.Color]::FromArgb(255, 38, 162, 65))
    $g.FillEllipse($grnGrad, 4, 4, 82, 76)
    $grnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 145, 240, 160)), 2.5
    $g.DrawEllipse($grnPen, 4, 4, 82, 76)
    $grnGrad.Dispose(); $grnPen.Dispose()

    # White speaker cone
    $speakerPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $speakerPath.AddRectangle((New-Object System.Drawing.RectangleF 20, 33, 14, 18))
    $conePts = New-Object 'System.Drawing.PointF[]' 4
    $conePts[0] = New-Object System.Drawing.PointF 34, 33
    $conePts[1] = New-Object System.Drawing.PointF 48, 22
    $conePts[2] = New-Object System.Drawing.PointF 48, 62
    $conePts[3] = New-Object System.Drawing.PointF 34, 51
    $speakerPath.AddPolygon($conePts)
    $spkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $g.FillPath($spkBrush, $speakerPath)
    $spkBrush.Dispose(); $speakerPath.Dispose()

    if (!$isOff) {
        # Soundwave arcs
        $wavePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 3.5
        $wavePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $wavePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($wavePen, 44, 27, 26, 30, -50, 100)
        $g.DrawArc($wavePen, 48, 19, 38, 46, -50, 100)
        $wavePen.Dispose()
    } else {
        # Red diagonal strike-through slash
        $slashPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 235, 50, 50)), 5
        $slashPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $slashPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawLine($slashPen, 20, 20, 70, 64)
        $slashPen.Dispose()
    }

    # Top glare
    $glPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(120, 255, 255, 255)), 3
    $g.DrawArc($glPen, 14, 8, 62, 50, 200, 80)
    $glPen.Dispose()

    $g.Dispose()
    $fname = if ($isOff) { "CartoonBtn_AudioOff.png" } else { "CartoonBtn_AudioOn.png" }
    $bmp.Save("$artDir/$fname", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# ========================================================
# 8. CartoonBadge_Multiplier (120x120) - Comic medallion
# ========================================================
$w = 120; $h = 120
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# 12-point starburst
$pts = New-Object 'System.Drawing.PointF[]' 24
$cx = 60; $cy = 60
$rOut = 54.0; $rIn = 44.0
for ($i = 0; $i -lt 24; $i++) {
    $rad = ($i * 15) * [Math]::PI / 180.0
    $cur = if ($i % 2 -eq 0) { $rOut } else { $rIn }
    $pts[$i] = New-Object System.Drawing.PointF ($cx + $cur * [Math]::Cos($rad)), ($cy + $cur * [Math]::Sin($rad))
}
# Shadow
$shPts = New-Object 'System.Drawing.PointF[]' 24
for ($i = 0; $i -lt 24; $i++) { $shPts[$i] = New-Object System.Drawing.PointF ($pts[$i].X), ($pts[$i].Y + 4) }
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 30, 15, 5))), $shPts)

# Gold starburst
$burstGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 120, 120), ([System.Drawing.Color]::FromArgb(255, 255, 220, 50)), ([System.Drawing.Color]::FromArgb(255, 220, 130, 10))
$g.FillPolygon($burstGrad, $pts)
$burstPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 140, 75, 10)), 3
$g.DrawPolygon($burstPen, $pts)
$burstGrad.Dispose(); $burstPen.Dispose()

# Inner warm amber core
$coreGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 24), (New-Object System.Drawing.PointF 0, 96), ([System.Drawing.Color]::FromArgb(255, 255, 152, 0)), ([System.Drawing.Color]::FromArgb(255, 230, 81, 0))
$g.FillEllipse($coreGrad, 24, 24, 72, 72)
$corePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 235, 120)), 2.5
$g.DrawEllipse($corePen, 24, 24, 72, 72)
$coreGrad.Dispose(); $corePen.Dispose()

# Top gloss arc
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(90, 255, 255, 255))), 30, 28, 60, 30)

$g.Dispose()
$bmp.Save("$artDir/CartoonBadge_Multiplier.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 9. CartoonParchmentModal (760x920) - Grand wood board
# ========================================================
$w = 760; $h = 920
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer 3D wood frame
$framePath = Create-RoundedRectPath 10 10 ($w - 20) ($h - 20) 36
$frameGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 10), (New-Object System.Drawing.PointF 0, ($h - 10)), ([System.Drawing.Color]::FromArgb(255, 105, 58, 28)), ([System.Drawing.Color]::FromArgb(255, 58, 28, 12))
$g.FillPath($frameGrad, $framePath)
$framePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 135, 78, 38)), 4
$g.DrawPath($framePen, $framePath)
$frameGrad.Dispose(); $framePen.Dispose(); $framePath.Dispose()

# 4 Corner Brass Rivets
$rivetBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 195, 55))
$rivetPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 150, 95, 15)), 2
$cornerOffsets = @( @(34, 34), @(($w - 54), 34), @(34, ($h - 54)), @(($w - 54), ($h - 54)) )
foreach ($co in $cornerOffsets) {
    $g.FillEllipse($rivetBrush, $co[0], $co[1], 20, 20)
    $g.DrawEllipse($rivetPen, $co[0], $co[1], 20, 20)
}
$rivetBrush.Dispose(); $rivetPen.Dispose()

# Inner Warm Cream Parchment Sheet
$parchPath = Create-RoundedRectPath 28 28 ($w - 56) ($h - 56) 26
$parchGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 28), (New-Object System.Drawing.PointF 0, ($h - 28)), ([System.Drawing.Color]::FromArgb(255, 255, 250, 238)), ([System.Drawing.Color]::FromArgb(255, 248, 230, 195))
$g.FillPath($parchGrad, $parchPath)
$parchPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 175, 115)), 3
$g.DrawPath($parchPen, $parchPath)
$parchGrad.Dispose(); $parchPen.Dispose(); $parchPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonParchmentModal.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 10. CartoonParchmentRow (680x130) - Row card for modals
# ========================================================
$w = 680; $h = 130
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 6 8 ($w - 12) ($h - 12) 18
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(80, 50, 25, 10))), $shPath)
$shPath.Dispose()

# Main creamy card
$rowPath = Create-RoundedRectPath 4 4 ($w - 8) ($h - 12) 18
$rowGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 4), (New-Object System.Drawing.PointF 0, ($h - 8)), ([System.Drawing.Color]::FromArgb(255, 255, 255, 250)), ([System.Drawing.Color]::FromArgb(255, 252, 238, 208))
$g.FillPath($rowGrad, $rowPath)
$rowPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 225, 180, 105)), 2.5
$g.DrawPath($rowPen, $rowPath)
$rowGrad.Dispose(); $rowPen.Dispose(); $rowPath.Dispose()

# Portrait backing circle on left
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 242, 215))), 18, 16, 92, 92)
$g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 215, 165, 90)), 2), 18, 16, 92, 92)

$g.Dispose()
$bmp.Save("$artDir/CartoonParchmentRow.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 11. CartoonRibbon_Header (640x140) - 3D festive ribbon
# ========================================================
$w = 640; $h = 140
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Swallowtail Left & Right back fold tips
$leftFold = New-Object 'System.Drawing.PointF[]' 5
$leftFold[0] = New-Object System.Drawing.PointF 6, 45
$leftFold[1] = New-Object System.Drawing.PointF 80, 45
$leftFold[2] = New-Object System.Drawing.PointF 80, 105
$leftFold[3] = New-Object System.Drawing.PointF 6, 105
$leftFold[4] = New-Object System.Drawing.PointF 35, 75
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 150, 20, 25))), $leftFold)

$rightFold = New-Object 'System.Drawing.PointF[]' 5
$rightFold[0] = New-Object System.Drawing.PointF ($w - 80), 45
$rightFold[1] = New-Object System.Drawing.PointF ($w - 6), 45
$rightFold[2] = New-Object System.Drawing.PointF ($w - 35), 75
$rightFold[3] = New-Object System.Drawing.PointF ($w - 6), 105
$rightFold[4] = New-Object System.Drawing.PointF ($w - 80), 105
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 150, 20, 25))), $rightFold)

# Dark 3D tuck triangle
$tuckL = @( (New-Object System.Drawing.PointF 65, 95), (New-Object System.Drawing.PointF 80, 95), (New-Object System.Drawing.PointF 80, 115) )
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 90, 10, 15))), $tuckL)
$tuckR = @( (New-Object System.Drawing.PointF ($w - 80), 95), (New-Object System.Drawing.PointF ($w - 65), 95), (New-Object System.Drawing.PointF ($w - 80), 115) )
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 90, 10, 15))), $tuckR)

# Main Banner Center
$centerPath = Create-RoundedRectPath 65 25 ($w - 130) 80 16
$ribbonGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 25), (New-Object System.Drawing.PointF 0, 105), ([System.Drawing.Color]::FromArgb(255, 235, 55, 65)), ([System.Drawing.Color]::FromArgb(255, 185, 25, 35))
$g.FillPath($ribbonGrad, $centerPath)
$goldPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 215, 65)), 3.5
$g.DrawPath($goldPen, $centerPath)
$ribbonGrad.Dispose(); $goldPen.Dispose(); $centerPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonRibbon_Header.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 12. CartoonBtn_Gold (360x100) - Primary Candy CTA
# ========================================================
$w = 360; $h = 100
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# 3D shadow base
$shPath = Create-RoundedRectPath 6 16 ($w - 12) ($h - 22) 36
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 175, 105, 10))), $shPath)
$shPath.Dispose()

# Main face
$btnPath = Create-RoundedRectPath 6 6 ($w - 12) ($h - 22) 36
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 6), (New-Object System.Drawing.PointF 0, ($h - 16)), ([System.Drawing.Color]::FromArgb(255, 255, 225, 45)), ([System.Drawing.Color]::FromArgb(255, 245, 155, 18))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 250, 160)), 3
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

# Top specular glare
$glPath = Create-RoundedRectPath 22 10 ($w - 44) 30 14
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 10), (New-Object System.Drawing.PointF 0, 40), ([System.Drawing.Color]::FromArgb(200, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(25, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Gold.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 13. CartoonBtn_Green (360x100) - Secondary Candy CTA
# ========================================================
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# 3D shadow base
$shPath = Create-RoundedRectPath 6 16 ($w - 12) ($h - 22) 36
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 20, 115, 50))), $shPath)
$shPath.Dispose()

# Main face
$btnPath = Create-RoundedRectPath 6 6 ($w - 12) ($h - 22) 36
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 6), (New-Object System.Drawing.PointF 0, ($h - 16)), ([System.Drawing.Color]::FromArgb(255, 75, 220, 100)), ([System.Drawing.Color]::FromArgb(255, 35, 165, 65))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 175, 250, 185)), 3
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

# Top specular glare
$glPath = Create-RoundedRectPath 22 10 ($w - 44) 30 14
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 10), (New-Object System.Drawing.PointF 0, 40), ([System.Drawing.Color]::FromArgb(200, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(25, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Green.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 14. CartoonBtn_Wood (360x90) - Warm Timber Button
# ========================================================
$w = 360; $h = 90
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# 3D shadow base
$shPath = Create-RoundedRectPath 6 14 ($w - 12) ($h - 20) 24
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 45, 22, 10))), $shPath)
$shPath.Dispose()

# Main face
$btnPath = Create-RoundedRectPath 6 6 ($w - 12) ($h - 20) 24
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 6), (New-Object System.Drawing.PointF 0, ($h - 14)), ([System.Drawing.Color]::FromArgb(255, 125, 70, 32)), ([System.Drawing.Color]::FromArgb(255, 80, 42, 18))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 170, 105, 55)), 2.5
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Wood.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# 15. CartoonCountdownBurst (400x400) - Starburst for countdown
# ========================================================
$w = 400; $h = 400
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$cx = 200; $cy = 200
$pts = New-Object 'System.Drawing.PointF[]' 32
$rOut = 185.0; $rIn = 125.0
for ($i = 0; $i -lt 32; $i++) {
    $rad = ($i * 11.25) * [Math]::PI / 180.0
    $cur = if ($i % 2 -eq 0) { $rOut } else { $rIn }
    $pts[$i] = New-Object System.Drawing.PointF ($cx + $cur * [Math]::Cos($rad)), ($cy + $cur * [Math]::Sin($rad))
}
# Shadow
$shPts = New-Object 'System.Drawing.PointF[]' 32
for ($i = 0; $i -lt 32; $i++) { $shPts[$i] = New-Object System.Drawing.PointF ($pts[$i].X), ($pts[$i].Y + 8) }
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 30, 15, 5))), $shPts)

# Outer fiery orange
$burstGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 400, 400), ([System.Drawing.Color]::FromArgb(255, 255, 195, 0)), ([System.Drawing.Color]::FromArgb(255, 245, 60, 20))
$g.FillPolygon($burstGrad, $pts)
$burstPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 235, 100)), 5
$g.DrawPolygon($burstPen, $pts)
$burstGrad.Dispose(); $burstPen.Dispose()

# Inner bright sun circle
$inGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 75), (New-Object System.Drawing.PointF 0, 325), ([System.Drawing.Color]::FromArgb(255, 255, 255, 220)), ([System.Drawing.Color]::FromArgb(255, 255, 215, 0))
$g.FillEllipse($inGrad, 75, 75, 250, 250)
$inPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 140, 0)), 4
$g.DrawEllipse($inPen, 75, 75, 250, 250)
$inGrad.Dispose(); $inPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonCountdownBurst.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# ========================================================
# Also update PanelFrame.png, CyberButton.png, CyberButton_Amber.png, IntelCardFrame.png
# to be identical to the warm cartoon sprites so old references automatically inherit warmth!
# ========================================================
Copy-Item "$artDir/CartoonStatPlaque.png" "$artDir/PanelFrame.png" -Force
Copy-Item "$artDir/CartoonParchmentModal.png" "$artDir/IntelCardFrame.png" -Force
Copy-Item "$artDir/CartoonBtn_Gold.png" "$artDir/CyberButton.png" -Force
Copy-Item "$artDir/CartoonBtn_Green.png" "$artDir/CyberButton_Amber.png" -Force
Copy-Item "$artDir/CartoonBtn_AudioOn.png" "$artDir/SoundIcon_On.png" -Force
Copy-Item "$artDir/CartoonBtn_AudioOff.png" "$artDir/SoundIcon_Off.png" -Force

Write-Output "Successfully created all polished Cartoon UI art assets into Assets/CyberWhack/Art!"
