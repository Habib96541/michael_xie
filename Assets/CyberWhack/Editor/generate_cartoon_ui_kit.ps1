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

function Add-RadialGlow($g, [float]$cx, [float]$cy, [float]$radius, [System.Drawing.Color]$centerColor, [System.Drawing.Color]$surroundColor) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddEllipse(($cx - $radius), ($cy - $radius), ($radius * 2), ($radius * 2))
    $pbr = New-Object System.Drawing.Drawing2D.PathGradientBrush $path
    $pbr.CenterColor = $centerColor
    $pbr.SurroundColors = @($surroundColor)
    $g.FillPath($pbr, $path)
    $pbr.Dispose()
    $path.Dispose()
}

function Draw-TextOutlineAndShadow($g, [string]$text, [float]$fontSize, [float]$x, [float]$y, [System.Drawing.Color]$fillCol, [System.Drawing.Color]$strokeCol, [float]$strokeW, [System.Drawing.Color]$shadowCol, [float]$shadowOffY, [bool]$center) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $sf = New-Object System.Drawing.StringFormat
    if ($center) {
        $sf.Alignment = [System.Drawing.StringAlignment]::Center
        $sf.LineAlignment = [System.Drawing.StringAlignment]::Center
    }
    $fam = New-Object System.Drawing.FontFamily "Arial Rounded MT Bold"
    $path.AddString($text, $fam, [int][System.Drawing.FontStyle]::Bold, $fontSize, (New-Object System.Drawing.PointF $x, $y), $sf)

    if ($shadowCol.A -gt 0) {
        $state = $g.Save()
        $g.TranslateTransform(0, $shadowOffY)
        $shPen = New-Object System.Drawing.Pen $shadowCol, ($strokeW + 2)
        $shPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $g.DrawPath($shPen, $path)
        $g.FillPath((New-Object System.Drawing.SolidBrush $shadowCol), $path)
        $shPen.Dispose()
        $g.Restore($state)
    }

    if ($strokeW -gt 0) {
        $strPen = New-Object System.Drawing.Pen $strokeCol, $strokeW
        $strPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $g.DrawPath($strPen, $path)
        $strPen.Dispose()
    }

    $fillBrush = New-Object System.Drawing.SolidBrush $fillCol
    $g.FillPath($fillBrush, $path)
    $fillBrush.Dispose()

    $fam.Dispose(); $sf.Dispose(); $path.Dispose()
}

Write-Host "--- Generating High-Polish Cartoon UI Kit Assets ---"

# ========================================================
# 1. CartoonBackground.png (1080x1920) - Desert Arena with Rocky Cliffs & Grass
# ========================================================
Write-Host "Generating CartoonBackground.png..."
$w = 1080; $h = 1920
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

$bgGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF 0, $h), ([System.Drawing.Color]::FromArgb(255, 238, 160, 75)), ([System.Drawing.Color]::FromArgb(255, 222, 134, 48))
$g.FillRectangle($bgGrad, 0, 0, $w, $h)
$bgGrad.Dispose()

Add-RadialGlow $g ($w * 0.5) ($h * 0.5) 1100 ([System.Drawing.Color]::FromArgb(0, 0, 0, 0)) ([System.Drawing.Color]::FromArgb(75, 95, 42, 12))

function Draw-RockFacet($g, $points, [System.Drawing.Color]$fillCol, [System.Drawing.Color]$strokeCol, [float]$penW) {
    $poly = New-Object 'System.Drawing.PointF[]' $points.Length
    for ($i = 0; $i -lt $points.Length; $i++) {
        $poly[$i] = New-Object System.Drawing.PointF $points[$i][0], $points[$i][1]
    }
    $brush = New-Object System.Drawing.SolidBrush $fillCol
    $pen = New-Object System.Drawing.Pen $strokeCol, $penW
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $g.FillPolygon($brush, $poly)
    $g.DrawPolygon($pen, $poly)
    $brush.Dispose(); $pen.Dispose()
}

function Draw-GrassTuft($g, [float]$bx, [float]$by, [float]$scale, [float]$rotAngle) {
    $state = $g.Save()
    $g.TranslateTransform($bx, $by)
    $g.RotateTransform($rotAngle)
    $g.ScaleTransform($scale, $scale)

    $grassPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 38, 90, 18)), 3
    $grassPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $grassBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 112, 198, 42))

    foreach ($blade in @(
        @(@(-2, 0), @(-16, -26), @(-18, -48), @(-6, -32), @(0, 0)),
        @(@(-4, 0), @(-4, -36), @(2, -64), @(10, -36), @(4, 0)),
        @(@(2, 0), @(12, -28), @(22, -45), @(16, -22), @(4, 0))
    )) {
        $pts = New-Object 'System.Drawing.PointF[]' $blade.Length
        for ($i = 0; $i -lt $blade.Length; $i++) {
            $pts[$i] = New-Object System.Drawing.PointF $blade[$i][0], $blade[$i][1]
        }
        $g.FillPolygon($grassBrush, $pts)
        $g.DrawPolygon($grassPen, $pts)
    }

    $grassBrush.Dispose(); $grassPen.Dispose()
    $g.Restore($state)
}

$rockDark = [System.Drawing.Color]::FromArgb(255, 78, 36, 16)
$rockMid  = [System.Drawing.Color]::FromArgb(255, 125, 62, 32)
$rockLight= [System.Drawing.Color]::FromArgb(255, 168, 88, 48)
$rockEdge = [System.Drawing.Color]::FromArgb(255, 42, 18, 8)

# Top Left Rock Formation
Draw-RockFacet $g @(@(-10, -10), @(180, -10), @(150, 90), @(100, 160), @(30, 200), @(-10, 220)) $rockDark $rockEdge 5
Draw-RockFacet $g @(@(-10, -10), @(140, -10), @(95, 110), @(20, 140), @(-10, 150)) $rockMid $rockEdge 4
Draw-RockFacet $g @(@(-10, -10), @(80, -10), @(45, 70), @(-10, 80)) $rockLight $rockEdge 3
Draw-GrassTuft $g 125 145 0.95 -12
Draw-GrassTuft $g 40 185 0.85 10

# Top Right Rock Formation
Draw-RockFacet $g @(@(($w + 10), -10), @(($w - 180), -10), @(($w - 150), 90), @(($w - 100), 160), @(($w - 30), 200), @(($w + 10), 220)) $rockDark $rockEdge 5
Draw-RockFacet $g @(@(($w + 10), -10), @(($w - 140), -10), @(($w - 95), 110), @(($w - 20), 140), @(($w + 10), 150)) $rockMid $rockEdge 4
Draw-RockFacet $g @(@(($w + 10), -10), @(($w - 80), -10), @(($w - 45), 70), @(($w + 10), 80)) $rockLight $rockEdge 3
Draw-GrassTuft $g ($w - 125) 145 0.95 12
Draw-GrassTuft $g ($w - 40) 185 0.85 -10

# Bottom Left Rock Formation
Draw-RockFacet $g @(@(-10, ($h + 10)), @(210, ($h + 10)), @(190, ($h - 80)), @(120, ($h - 160)), @(30, ($h - 190)), @(-10, ($h - 190))) $rockDark $rockEdge 5
Draw-RockFacet $g @(@(-10, ($h + 10)), @(170, ($h + 10)), @(140, ($h - 60)), @(80, ($h - 120)), @(-10, ($h - 130))) $rockMid $rockEdge 4
Draw-RockFacet $g @(@(-10, ($h + 10)), @(100, ($h + 10)), @(50, ($h - 60)), @(-10, ($h - 70))) $rockLight $rockEdge 3
Draw-GrassTuft $g 155 ($h - 130) 1.0 -15
Draw-GrassTuft $g 60 ($h - 170) 0.85 14

# Bottom Right Rock Formation
Draw-RockFacet $g @(@(($w + 10), ($h + 10)), @(($w - 210), ($h + 10)), @(($w - 190), ($h - 80)), @(($w - 120), ($h - 160)), @(($w - 30), ($h - 190)), @(($w + 10), ($h - 190))) $rockDark $rockEdge 5
Draw-RockFacet $g @(@(($w + 10), ($h + 10)), @(($w - 170), ($h + 10)), @(($w - 140), ($h - 60)), @(($w - 80), ($h - 120)), @(($w + 10), ($h - 130))) $rockMid $rockEdge 4
Draw-RockFacet $g @(@(($w + 10), ($h + 10)), @(($w - 100), ($h + 10)), @(($w - 50), ($h - 60)), @(($w + 10), ($h - 70))) $rockLight $rockEdge 3
Draw-GrassTuft $g ($w - 155) ($h - 130) 1.0 15
Draw-GrassTuft $g ($w - 60) ($h - 170) 0.85 -14

$g.Dispose()
$bmp.Save("$artDir/CartoonBackground.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 2. WhackTitle.png (800x190) - Clean 3D Bubbly Logo: WHACK-A-MOLE (No "HUD KIT" / "GAME HUD UI KIT")
# ========================================================
Write-Host "Generating WhackTitle.png..."
$w = 800; $h = 190
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Main Title Text: "WHACK-A-MOLE" (Centered with 3D Depth)
$titleText = "WHACK-A-MOLE"
$fontFam = New-Object System.Drawing.FontFamily "Arial Rounded MT Bold"
$sfCenter = New-Object System.Drawing.StringFormat
$sfCenter.Alignment = [System.Drawing.StringAlignment]::Center
$sfCenter.LineAlignment = [System.Drawing.StringAlignment]::Center

$titlePath = New-Object System.Drawing.Drawing2D.GraphicsPath
$titleY = [float]($h * 0.5 + 2)
$titlePath.AddString($titleText, $fontFam, [int][System.Drawing.FontStyle]::Bold, 84.0, (New-Object System.Drawing.PointF ($w * 0.5), $titleY), $sfCenter)

# 3D Depth Extrusion Downward
$depthSteps = 15
for ($d = $depthSteps; $d -ge 1; $d--) {
    $depthPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 48, 18, 6)), (12 + $d * 0.4)
    $depthPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $state = $g.Save()
    $g.TranslateTransform(0, $d)
    $g.DrawPath($depthPen, $titlePath)
    $g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 48, 18, 6))), $titlePath)
    $g.Restore($state)
    $depthPen.Dispose()
}

# Dark Chocolate Outline
$outPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 38, 14, 4)), 13
$outPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawPath($outPen, $titlePath)
$outPen.Dispose()

# Face Gradient (Light cream/yellow at top to warm fiery orange at bottom)
$faceGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, ($titleY - 45)), (New-Object System.Drawing.PointF 0, ($titleY + 45)), ([System.Drawing.Color]::FromArgb(255, 255, 255, 230)), ([System.Drawing.Color]::FromArgb(255, 250, 130, 20))
$cb = New-Object System.Drawing.Drawing2D.ColorBlend 3
$cb.Colors = @( [System.Drawing.Color]::FromArgb(255, 255, 252, 230), [System.Drawing.Color]::FromArgb(255, 255, 204, 50), [System.Drawing.Color]::FromArgb(255, 240, 105, 15) )
$cb.Positions = @( 0.0, 0.45, 1.0 )
$faceGrad.InterpolationColors = $cb
$g.FillPath($faceGrad, $titlePath)
$faceGrad.Dispose()

# Top Inner Specular Highlight Rim
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(210, 255, 255, 255)), 3.5
$rimPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$state = $g.Save()
$g.TranslateTransform(0, -2)
$g.DrawPath($rimPen, $titlePath)
$g.Restore($state)
$rimPen.Dispose()

# Flame on top right of "E" (approx X=674, Y=titleY - 26)
$flameX = 674; $flameY = [float]($titleY - 26)
$flameOuter = @(
    (New-Object System.Drawing.PointF $flameX, ($flameY - 15)),
    (New-Object System.Drawing.PointF ($flameX + 12), ($flameY - 2)),
    (New-Object System.Drawing.PointF ($flameX + 14), ($flameY + 13)),
    (New-Object System.Drawing.PointF ($flameX + 7), ($flameY + 23)),
    (New-Object System.Drawing.PointF ($flameX - 7), ($flameY + 23)),
    (New-Object System.Drawing.PointF ($flameX - 13), ($flameY + 11)),
    (New-Object System.Drawing.PointF ($flameX - 9), ($flameY - 3))
)
$flamePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 180, 40, 10)), 3
$flameBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 87, 34))
$g.FillPolygon($flameBrush, $flameOuter)
$g.DrawPolygon($flamePen, $flameOuter)
$flamePen.Dispose(); $flameBrush.Dispose()

$flameInner = @(
    (New-Object System.Drawing.PointF ($flameX + 1), ($flameY - 4)),
    (New-Object System.Drawing.PointF ($flameX + 6), ($flameY + 4)),
    (New-Object System.Drawing.PointF ($flameX + 3), ($flameY + 17)),
    (New-Object System.Drawing.PointF ($flameX - 3), ($flameY + 17)),
    (New-Object System.Drawing.PointF ($flameX - 5), ($flameY + 7))
)
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 235, 59))), $flameInner)

$titlePath.Dispose(); $fontFam.Dispose(); $sfCenter.Dispose()
$g.Dispose()
$bmp.Save("$artDir/WhackTitle.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 3. CartoonBtn_Pause.png (180x180) - Dark Chocolate Pause Button Background
# ========================================================
Write-Host "Generating CartoonBtn_Pause.png..."
$w = 180; $h = 180
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 18 ($w - 20) ($h - 26) 38
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 32, 14, 5))), $shPath)
$shPath.Dispose()

# Outer wooden button
$btnPath = Create-RoundedRectPath 10 10 ($w - 20) ($h - 26) 38
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 10), (New-Object System.Drawing.PointF 0, ($h - 16)), ([System.Drawing.Color]::FromArgb(255, 96, 52, 24)), ([System.Drawing.Color]::FromArgb(255, 48, 22, 8))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 32, 14, 5)), 6
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

# Inner warm rim
$inPath = Create-RoundedRectPath 16 16 ($w - 32) ($h - 38) 32
$inPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 150, 92, 48)), 3
$g.DrawPath($inPen, $inPath)
$inPen.Dispose(); $inPath.Dispose()

# Top specular gloss
$glPath = Create-RoundedRectPath 24 16 ($w - 48) 36 18
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 16), (New-Object System.Drawing.PointF 0, 52), ([System.Drawing.Color]::FromArgb(160, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(15, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

# Save clean wooden button first (used for New Seed button, menus, etc. with NO pause bars)
$bmp.Save("$artDir/CartoonBtn_Wood.png", [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item "$artDir/CartoonBtn_Wood.png" "$artDir/CartoonBtn_Exit.png" -Force

# Two white vertical rounded pause bars in upper center (ONLY for Pause button)
$barW = 16; $barH = 48; $barY = 38
$barL = Create-RoundedRectPath 64 $barY $barW $barH 7
$barR = Create-RoundedRectPath 100 $barY $barW $barH 7
$barBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
$g.FillPath($barBrush, $barL)
$g.FillPath($barBrush, $barR)
$barBrush.Dispose(); $barL.Dispose(); $barR.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Pause.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 4. CartoonCard_Time.png / CartoonStatPlaque.png (560x180) - Time Plaque
# ========================================================
Write-Host "Generating CartoonCard_Time.png..."
$w = 560; $h = 180
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 18 ($w - 20) ($h - 26) 38
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 36, 16, 6))), $shPath)
$shPath.Dispose()

# Outer dark chocolate rim
$rimPath = Create-RoundedRectPath 10 10 ($w - 20) ($h - 26) 38
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 58, 28, 10))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 42, 18, 6)), 5
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Inner warm cream pill
$cardPath = Create-RoundedRectPath 18 18 ($w - 36) ($h - 42) 32
$cardGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 18), (New-Object System.Drawing.PointF 0, ($h - 24)), ([System.Drawing.Color]::FromArgb(255, 255, 253, 245)), ([System.Drawing.Color]::FromArgb(255, 252, 233, 192))
$g.FillPath($cardGrad, $cardPath)
$cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 172, 110)), 3
$g.DrawPath($cardPen, $cardPath)
$cardGrad.Dispose(); $cardPen.Dispose(); $cardPath.Dispose()

# Top gloss highlight
$glPath = Create-RoundedRectPath 30 20 ($w - 60) 40 20
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 20), (New-Object System.Drawing.PointF 0, 60), ([System.Drawing.Color]::FromArgb(190, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

# Left Badge Inset Circle (X=36, Y=30, size=108)
$badgeX = 36; $badgeY = 30; $badgeS = 108
$badgeGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF $badgeX, $badgeY), (New-Object System.Drawing.PointF ($badgeX + $badgeS), ($badgeY + $badgeS)), ([System.Drawing.Color]::FromArgb(255, 255, 240, 175)), ([System.Drawing.Color]::FromArgb(255, 245, 198, 92))
$g.FillEllipse($badgeGrad, $badgeX, $badgeY, $badgeS, $badgeS)
$badgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 155, 60)), 3.5
$g.DrawEllipse($badgePen, $badgeX, $badgeY, $badgeS, $badgeS)
$badgeGrad.Dispose(); $badgePen.Dispose()

# Stopwatch Icon inside badge
$swCx = $badgeX + $badgeS * 0.5; $swCy = $badgeY + $badgeS * 0.54; $swR = 34
$loopPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 215, 142, 18)), 4
$g.DrawEllipse($loopPen, ($swCx - 9), ($swCy - $swR - 14), 18, 14)
$loopPen.Dispose()
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 235, 165, 25))), ($swCx - 5), ($swCy - $swR - 4), 10, 6)

$watchGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF ($swCx - $swR), ($swCy - $swR)), (New-Object System.Drawing.PointF ($swCx + $swR), ($swCy + $swR)), ([System.Drawing.Color]::FromArgb(255, 255, 225, 75)), ([System.Drawing.Color]::FromArgb(255, 215, 135, 15))
$g.FillEllipse($watchGrad, ($swCx - $swR), ($swCy - $swR), ($swR * 2), ($swR * 2))
$watchPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 160, 95, 10)), 3
$g.DrawEllipse($watchPen, ($swCx - $swR), ($swCy - $swR), ($swR * 2), ($swR * 2))
$watchGrad.Dispose(); $watchPen.Dispose()

$faceR = 26
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)), ($swCx - $faceR), ($swCy - $faceR), ($faceR * 2), ($faceR * 2))
$g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 230, 205, 160)), 2), ($swCx - $faceR), ($swCy - $faceR), ($faceR * 2), ($faceR * 2))

$handPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 45, 45, 55)), 3.5
$handPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$handPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($handPen, $swCx, $swCy, ($swCx - 11), ($swCy - 11))
$g.DrawLine($handPen, $swCx, $swCy, ($swCx + 14), ($swCy - 9))
$handPen.Dispose()
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 220, 60, 45))), ($swCx - 3.5), ($swCy - 3.5), 7, 7)

# Vertical divider line
$divPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 175, 120)), 3
$g.DrawLine($divPen, 164, 34, 164, 134)
$divPen.Dispose()

# Top Yellow Radiating Accent Rays
function Draw-RayAccents($g, [float]$rx, [float]$ry, [bool]$flip) {
    $rPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 205, 55)), 4
    $rPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $rPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $dir = if ($flip) { -1 } else { 1 }
    $g.DrawLine($rPen, $rx, $ry, ($rx + 14 * $dir), ($ry - 12))
    $g.DrawLine($rPen, ($rx + 12 * $dir), ($ry + 6), ($rx + 24 * $dir), ($ry - 4))
    $rPen.Dispose()
}
Draw-RayAccents $g 148 18 $true
Draw-RayAccents $g ($w - 30) 18 $false

$g.Dispose()
$bmp.Save("$artDir/CartoonStatPlaque.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 5. CartoonCard_Score.png / CartoonParchmentRow.png (720x160) - Score Plaque
# ========================================================
Write-Host "Generating CartoonCard_Score.png..."
$w = 720; $h = 160
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 16 ($w - 20) ($h - 24) 36
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 36, 16, 6))), $shPath)
$shPath.Dispose()

# Outer dark chocolate rim
$rimPath = Create-RoundedRectPath 10 8 ($w - 20) ($h - 24) 36
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 58, 28, 10))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 42, 18, 6)), 5
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Inner warm cream pill
$cardPath = Create-RoundedRectPath 18 16 ($w - 36) ($h - 40) 30
$cardGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 16), (New-Object System.Drawing.PointF 0, ($h - 24)), ([System.Drawing.Color]::FromArgb(255, 255, 253, 245)), ([System.Drawing.Color]::FromArgb(255, 252, 233, 192))
$g.FillPath($cardGrad, $cardPath)
$cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 172, 110)), 3
$g.DrawPath($cardPen, $cardPath)
$cardGrad.Dispose(); $cardPen.Dispose(); $cardPath.Dispose()

# Top-Right Glossy Highlight Specular Ellipse
$glBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 255, 255, 255))
$g.FillEllipse($glBrush, ($w - 120), 22, 50, 14)
$g.FillEllipse($glBrush, ($w - 60), 26, 12, 12)
$glBrush.Dispose()

# Left Badge Inset Circle (X=36, Y=26, size=96)
$badgeX = 36; $badgeY = 26; $badgeS = 96
$badgeGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF $badgeX, $badgeY), (New-Object System.Drawing.PointF ($badgeX + $badgeS), ($badgeY + $badgeS)), ([System.Drawing.Color]::FromArgb(255, 255, 240, 175)), ([System.Drawing.Color]::FromArgb(255, 245, 198, 92))
$g.FillEllipse($badgeGrad, $badgeX, $badgeY, $badgeS, $badgeS)
$badgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 155, 60)), 3.5
$g.DrawEllipse($badgePen, $badgeX, $badgeY, $badgeS, $badgeS)
$badgeGrad.Dispose(); $badgePen.Dispose()

# 5-point Golden Cartoon Star inside badge
$starCx = $badgeX + $badgeS * 0.5; $starCy = $badgeY + $badgeS * 0.5; $rOut = 32.0; $rIn = 14.5
$starPts = New-Object 'System.Drawing.PointF[]' 10
for ($i = 0; $i -lt 10; $i++) {
    $rad = ($i * 36 - 90) * [Math]::PI / 180.0
    $cur = if ($i % 2 -eq 0) { $rOut } else { $rIn }
    $starPts[$i] = New-Object System.Drawing.PointF ($starCx + $cur * [Math]::Cos($rad)), ($starCy + $cur * [Math]::Sin($rad))
}
$starGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF ($starCx - $rOut), ($starCy - $rOut)), (New-Object System.Drawing.PointF ($starCx + $rOut), ($starCy + $rOut)), ([System.Drawing.Color]::FromArgb(255, 255, 235, 60)), ([System.Drawing.Color]::FromArgb(255, 245, 140, 15))
$g.FillPolygon($starGrad, $starPts)
$starPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 185, 95, 10)), 3.5
$starPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawPolygon($starPen, $starPts)
$starGrad.Dispose(); $starPen.Dispose()

# White Glint Sparkle on Star Top Tip
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(230, 255, 255, 255))), ($starCx - 6), ($starCy - 28), 12, 12)

# Vertical divider line
$divPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 175, 120)), 3
$g.DrawLine($divPen, 154, 28, 154, 120)
$divPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonParchmentRow.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 6. CartoonComboTrack.png (720x150) - Combo Track with 1X Oval Badge
# ========================================================
Write-Host "Generating CartoonComboTrack.png..."
$w = 720; $h = 150
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 16 ($w - 20) ($h - 24) 36
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 36, 16, 6))), $shPath)
$shPath.Dispose()

# Outer dark chocolate rim
$rimPath = Create-RoundedRectPath 10 8 ($w - 20) ($h - 24) 36
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 58, 28, 10))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 42, 18, 6)), 5
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Inner warm cream pill
$cardPath = Create-RoundedRectPath 18 16 ($w - 36) ($h - 40) 30
$cardGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 16), (New-Object System.Drawing.PointF 0, ($h - 24)), ([System.Drawing.Color]::FromArgb(255, 255, 253, 245)), ([System.Drawing.Color]::FromArgb(255, 252, 233, 192))
$g.FillPath($cardGrad, $cardPath)
$cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 172, 110)), 3
$g.DrawPath($cardPen, $cardPath)
$cardGrad.Dispose(); $cardPen.Dispose(); $cardPath.Dispose()

# Left Badge Inset Oval (X=36, Y=25, W=130, H=78) - Clean, No Baked Text
$badgeX = 36; $badgeY = 25; $badgeW = 130; $badgeH = 78
$badgePath = Create-RoundedRectPath $badgeX $badgeY $badgeW $badgeH 36
$badgeGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF $badgeX, $badgeY), (New-Object System.Drawing.PointF ($badgeX + $badgeW), ($badgeY + $badgeH)), ([System.Drawing.Color]::FromArgb(255, 255, 240, 175)), ([System.Drawing.Color]::FromArgb(255, 245, 198, 92))
$g.FillPath($badgeGrad, $badgePath)
$badgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 155, 60)), 3.5
$g.DrawPath($badgePen, $badgePath)
$badgeGrad.Dispose(); $badgePen.Dispose(); $badgePath.Dispose()

# Top gloss in oval badge
$ovGlPath = Create-RoundedRectPath ($badgeX + 10) ($badgeY + 8) ($badgeW - 20) 24 12
$ovGlGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, ($badgeY + 8)), (New-Object System.Drawing.PointF 0, ($badgeY + 32)), ([System.Drawing.Color]::FromArgb(180, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($ovGlGrad, $ovGlPath)
$ovGlGrad.Dispose(); $ovGlPath.Dispose()

# Vertical divider line
$divPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 175, 120)), 3
$g.DrawLine($divPen, 185, 28, 185, 115)
$divPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonComboTrack.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 7. CartoonCard_BestScore.png / PanelFrame.png (480x150) - Best Score Plaque
# ========================================================
Write-Host "Generating CartoonCard_BestScore.png..."
$w = 480; $h = 150
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 16 ($w - 20) ($h - 24) 36
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 36, 16, 6))), $shPath)
$shPath.Dispose()

# Outer dark chocolate rim
$rimPath = Create-RoundedRectPath 10 8 ($w - 20) ($h - 24) 36
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 58, 28, 10))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 42, 18, 6)), 5
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Inner warm cream pill
$cardPath = Create-RoundedRectPath 18 16 ($w - 36) ($h - 40) 30
$cardGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 16), (New-Object System.Drawing.PointF 0, ($h - 24)), ([System.Drawing.Color]::FromArgb(255, 255, 253, 245)), ([System.Drawing.Color]::FromArgb(255, 252, 233, 192))
$g.FillPath($cardGrad, $cardPath)
$cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 172, 110)), 3
$g.DrawPath($cardPen, $cardPath)
$cardGrad.Dispose(); $cardPen.Dispose(); $cardPath.Dispose()

# Top gloss
$glPath = Create-RoundedRectPath 28 18 ($w - 56) 32 16
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 18), (New-Object System.Drawing.PointF 0, 50), ([System.Drawing.Color]::FromArgb(180, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

# Left Badge Inset Circle (X=32, Y=22, S=90)
$badgeX = 32; $badgeY = 22; $badgeS = 90
$badgeGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF $badgeX, $badgeY), (New-Object System.Drawing.PointF ($badgeX + $badgeS), ($badgeY + $badgeS)), ([System.Drawing.Color]::FromArgb(255, 255, 240, 175)), ([System.Drawing.Color]::FromArgb(255, 245, 198, 92))
$g.FillEllipse($badgeGrad, $badgeX, $badgeY, $badgeS, $badgeS)
$badgePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 155, 60)), 3.5
$g.DrawEllipse($badgePen, $badgeX, $badgeY, $badgeS, $badgeS)
$badgeGrad.Dispose(); $badgePen.Dispose()

# Golden Trophy Icon inside badge
$tpCx = $badgeX + $badgeS * 0.5; $tpCy = $badgeY + $badgeS * 0.52
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 195, 110, 15))), ($tpCx - 18), ($tpCy + 18), 36, 10)
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 235, 155, 25))), ($tpCx - 14), ($tpCy + 12), 28, 8)
$g.FillRectangle((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 245, 185, 35))), ($tpCx - 6), ($tpCy + 4), 12, 10)

$cupPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$cupPath.AddArc(($tpCx - 20), ($tpCy - 24), 40, 36, 0, 180)
$cupPath.CloseFigure()
$cupGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF ($tpCx - 20), ($tpCy - 24)), (New-Object System.Drawing.PointF ($tpCx + 20), ($tpCy + 12)), ([System.Drawing.Color]::FromArgb(255, 255, 235, 75)), ([System.Drawing.Color]::FromArgb(255, 220, 130, 15))
$g.FillPath($cupGrad, $cupPath)
$cupPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 160, 90, 8)), 2.5
$g.DrawPath($cupPen, $cupPath)
$cupGrad.Dispose(); $cupPen.Dispose(); $cupPath.Dispose()

$hdlPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 215, 135, 15)), 3.5
$g.DrawArc($hdlPen, ($tpCx - 28), ($tpCy - 20), 16, 20, 90, 180)
$g.DrawArc($hdlPen, ($tpCx + 12), ($tpCy - 20), 16, 20, -90, 180)
$hdlPen.Dispose()

$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)), ($tpCx - 4), ($tpCy - 10), 8, 8)

# Vertical divider line
$divPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 212, 175, 120)), 3
$g.DrawLine($divPen, 142, 28, 142, 115)
$divPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/PanelFrame.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 8. CartoonSeedBar.png (680x120) - Plain Parchment Bar for SeedBox (No Star!)
# ========================================================
Write-Host "Generating CartoonSeedBar.png..."
$w = 680; $h = 120
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 8 12 ($w - 16) ($h - 20) 26
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(140, 36, 16, 6))), $shPath)
$shPath.Dispose()

# Outer dark chocolate rim
$rimPath = Create-RoundedRectPath 8 6 ($w - 16) ($h - 20) 26
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 68, 34, 12))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 48, 22, 8)), 4
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Inner warm cream pill
$cardPath = Create-RoundedRectPath 16 12 ($w - 32) ($h - 32) 22
$cardGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 12), (New-Object System.Drawing.PointF 0, ($h - 20)), ([System.Drawing.Color]::FromArgb(255, 255, 253, 245)), ([System.Drawing.Color]::FromArgb(255, 250, 230, 192))
$g.FillPath($cardGrad, $cardPath)
$cardPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 218, 172, 110)), 2.5
$g.DrawPath($cardPen, $cardPath)
$cardGrad.Dispose(); $cardPen.Dispose(); $cardPath.Dispose()

# Top gloss
$glPath = Create-RoundedRectPath 24 14 ($w - 48) 24 12
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 14), (New-Object System.Drawing.PointF 0, 38), ([System.Drawing.Color]::FromArgb(160, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonSeedBar.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()


# ========================================================
# 9. CartoonBtn_AudioOn.png & CartoonBtn_AudioOff.png (160x160) - Green Circular Buttons
# ========================================================
Write-Host "Generating Audio Buttons..."
foreach ($isOff in @($false, $true)) {
    $w = 160; $h = 160
    $bmp = New-Object System.Drawing.Bitmap $w, $h
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    # Drop shadow
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 32, 14, 5))), 10, 18, ($w - 20), ($h - 22))

    # Outer dark chocolate rim
    $g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 52, 26, 8))), 10, 10, ($w - 20), ($h - 22))
    $g.DrawEllipse((New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 36, 16, 5)), 4), 10, 10, ($w - 20), ($h - 22))

    # Vibrant Green Circle
    $grnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 18, 18), (New-Object System.Drawing.PointF ($w - 18), ($h - 20)), ([System.Drawing.Color]::FromArgb(255, 76, 217, 100)), ([System.Drawing.Color]::FromArgb(255, 36, 165, 62))
    $g.FillEllipse($grnGrad, 18, 18, ($w - 36), ($h - 38))
    $grnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 150, 245, 165)), 3
    $g.DrawEllipse($grnPen, 18, 18, ($w - 36), ($h - 38))
    $grnGrad.Dispose(); $grnPen.Dispose()

    # Top specular gloss arc
    $glPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(180, 255, 255, 255)), 5
    $g.DrawArc($glPen, 32, 24, ($w - 64), 60, 200, 80)
    $glPen.Dispose()

    # White Speaker Icon
    $spkCx = $w * 0.46; $spkCy = $h * 0.48
    $speakerPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $speakerPath.AddRectangle((New-Object System.Drawing.RectangleF ($spkCx - 24), ($spkCy - 14), 18, 28))
    $conePts = @(
        (New-Object System.Drawing.PointF ($spkCx - 8), ($spkCy - 14)),
        (New-Object System.Drawing.PointF ($spkCx + 12), ($spkCy - 28)),
        (New-Object System.Drawing.PointF ($spkCx + 12), ($spkCy + 28)),
        (New-Object System.Drawing.PointF ($spkCx - 8), ($spkCy + 14))
    )
    $speakerPath.AddPolygon($conePts)
    $spkBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    $g.FillPath($spkBrush, $speakerPath)
    $spkBrush.Dispose(); $speakerPath.Dispose()

    if (!$isOff) {
        $wavePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), 5
        $wavePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $wavePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawArc($wavePen, ($spkCx + 6), ($spkCy - 20), 24, 40, -50, 100)
        $g.DrawArc($wavePen, ($spkCx + 14), ($spkCy - 30), 38, 60, -50, 100)
        $wavePen.Dispose()
    } else {
        $slashPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 235, 45, 45)), 7
        $slashPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $slashPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $g.DrawLine($slashPen, ($spkCx - 22), ($spkCy + 26), ($spkCx + 32), ($spkCy - 26))
        $slashPen.Dispose()
    }

    $g.Dispose()
    $fname = if ($isOff) { "CartoonBtn_AudioOff.png" } else { "CartoonBtn_AudioOn.png" }
    $bmp.Save("$artDir/$fname", [System.Drawing.Imaging.ImageFormat]::Png)
    if (!$isOff) { Copy-Item "$artDir/$fname" "$artDir/SoundIcon_On.png" -Force }
    else { Copy-Item "$artDir/$fname" "$artDir/SoundIcon_Off.png" -Force }
    $bmp.Dispose()
}


# ========================================================
# 10. CartoonBtn_Green.png (360x140) - Glossy Green Pill with Book Icon (No Baked Text)
# ========================================================
Write-Host "Generating CartoonBtn_Green.png..."
$w = 360; $h = 140
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 16 ($w - 20) ($h - 24) 40
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 24, 60, 20))), $shPath)
$shPath.Dispose()

# Outer dark border
$rimPath = Create-RoundedRectPath 10 8 ($w - 20) ($h - 24) 40
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 34, 76, 26))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 24, 52, 18)), 5
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Main Green Pill Face
$btnPath = Create-RoundedRectPath 18 16 ($w - 36) ($h - 40) 34
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 16), (New-Object System.Drawing.PointF 0, ($h - 24)), ([System.Drawing.Color]::FromArgb(255, 80, 222, 100)), ([System.Drawing.Color]::FromArgb(255, 34, 168, 62))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 165, 248, 175)), 3.5
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

# Top Specular Highlight Arc
$glPath = Create-RoundedRectPath 30 18 ($w - 60) 36 18
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 18), (New-Object System.Drawing.PointF 0, 54), ([System.Drawing.Color]::FromArgb(210, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(25, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

# Cartoon Open Book Icon on Left (X=60, Y=64)
$bkCx = 62; $bkCy = 64
$bkL = @( (New-Object System.Drawing.PointF ($bkCx - 22), ($bkCy - 14)), (New-Object System.Drawing.PointF ($bkCx - 2), ($bkCy - 18)), (New-Object System.Drawing.PointF ($bkCx - 2), ($bkCy + 14)), (New-Object System.Drawing.PointF ($bkCx - 22), ($bkCy + 18)) )
$bkR = @( (New-Object System.Drawing.PointF ($bkCx + 2), ($bkCy - 18)), (New-Object System.Drawing.PointF ($bkCx + 22), ($bkCy - 14)), (New-Object System.Drawing.PointF ($bkCx + 22), ($bkCy + 18)), (New-Object System.Drawing.PointF ($bkCx + 2), ($bkCy + 14)) )
$pageBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 252, 235))
$pagePen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 48, 24, 8)), 3
$g.FillPolygon($pageBrush, $bkL); $g.DrawPolygon($pagePen, $bkL)
$g.FillPolygon($pageBrush, $bkR); $g.DrawPolygon($pagePen, $bkR)
$pageBrush.Dispose(); $pagePen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Green.png", [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item "$artDir/CartoonBtn_Green.png" "$artDir/CyberButton_Amber.png" -Force
$bmp.Dispose()


# ========================================================
# 11. CartoonBtn_Gold.png (520x150) - Large Golden Glossy Pill with Play Icon (No Baked Text)
# ========================================================
Write-Host "Generating CartoonBtn_Gold.png..."
$w = 520; $h = 150
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Drop shadow
$shPath = Create-RoundedRectPath 10 18 ($w - 20) ($h - 26) 44
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 48, 20, 6))), $shPath)
$shPath.Dispose()

# Outer dark chocolate rim
$rimPath = Create-RoundedRectPath 10 10 ($w - 20) ($h - 26) 44
$rimBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 62, 28, 8))
$g.FillPath($rimBrush, $rimPath)
$rimPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 42, 18, 5)), 6
$g.DrawPath($rimPen, $rimPath)
$rimBrush.Dispose(); $rimPen.Dispose(); $rimPath.Dispose()

# Main Golden Candy Face
$btnPath = Create-RoundedRectPath 18 18 ($w - 36) ($h - 42) 38
$btnGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 18), (New-Object System.Drawing.PointF 0, ($h - 24)), ([System.Drawing.Color]::FromArgb(255, 255, 235, 60)), ([System.Drawing.Color]::FromArgb(255, 245, 155, 18))
$g.FillPath($btnGrad, $btnPath)
$btnPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 250, 165)), 4
$g.DrawPath($btnPen, $btnPath)
$btnGrad.Dispose(); $btnPen.Dispose(); $btnPath.Dispose()

# Top Specular Highlight Arc
$glPath = Create-RoundedRectPath 32 20 ($w - 64) 40 20
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 20), (New-Object System.Drawing.PointF 0, 60), ([System.Drawing.Color]::FromArgb(220, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(25, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose()

# Play Triangle Icon on Left
$plyPts = @(
    (New-Object System.Drawing.PointF 75, 44),
    (New-Object System.Drawing.PointF 122, 68),
    (New-Object System.Drawing.PointF 75, 92)
)
$plyPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 52, 22, 6)), 7
$plyPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)), $plyPts)
$g.DrawPolygon($plyPen, $plyPts)
$plyPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBtn_Gold.png", [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item "$artDir/CartoonBtn_Gold.png" "$artDir/CyberButton.png" -Force
$bmp.Dispose()


# ========================================================
# 12. CartoonBadge_Multiplier.png (360x360) - 16-Point Sunburst Badge
# ========================================================
Write-Host "Generating Sunburst Badge..."
$w = 360; $h = 360
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$cx = 180; $cy = 180
# 16-point starburst (scaled so it stays inside canvas with rays)
$pts = New-Object 'System.Drawing.PointF[]' 32
$rOut = 145.0; $rIn = 108.0
for ($i = 0; $i -lt 32; $i++) {
    $rad = ($i * 11.25) * [Math]::PI / 180.0
    $cur = if ($i % 2 -eq 0) { $rOut } else { $rIn }
    $pts[$i] = New-Object System.Drawing.PointF ($cx + $cur * [Math]::Cos($rad)), ($cy + $cur * [Math]::Sin($rad))
}
# Shadow
$shPts = New-Object 'System.Drawing.PointF[]' 32
for ($i = 0; $i -lt 32; $i++) { $shPts[$i] = New-Object System.Drawing.PointF ($pts[$i].X), ($pts[$i].Y + 8) }
$g.FillPolygon((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(160, 36, 16, 6))), $shPts)

# Fiery orange-gold starburst
$burstGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 0), (New-Object System.Drawing.PointF $w, $h), ([System.Drawing.Color]::FromArgb(255, 255, 215, 45)), ([System.Drawing.Color]::FromArgb(255, 245, 95, 15))
$g.FillPolygon($burstGrad, $pts)
$burstPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 210, 60, 10)), 5
$g.DrawPolygon($burstPen, $pts)
$burstGrad.Dispose(); $burstPen.Dispose()

# Central bright yellow glossy disc
$discR = 76
$discGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, ($cy - $discR)), (New-Object System.Drawing.PointF 0, ($cy + $discR)), ([System.Drawing.Color]::FromArgb(255, 255, 250, 175)), ([System.Drawing.Color]::FromArgb(255, 255, 205, 30))
$g.FillEllipse($discGrad, ($cx - $discR), ($cy - $discR), ($discR * 2), ($discR * 2))
$discPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 245, 165, 20)), 4
$g.DrawEllipse($discPen, ($cx - $discR), ($cy - $discR), ($discR * 2), ($discR * 2))
$discGrad.Dispose(); $discPen.Dispose()

# Top gloss arc
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(180, 255, 255, 255))), ($cx - 48), ($cy - 62), 96, 42)

# Surrounding radiating cartoon yellow accent dashes
$rayPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 215, 55)), 5
$rayPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$rayPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($rayPen, 34, 115, 14, 105)
$g.DrawLine($rayPen, 40, 90, 20, 72)
$g.DrawLine($rayPen, 326, 115, 346, 105)
$g.DrawLine($rayPen, 320, 90, 340, 72)
$rayPen.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonBadge_Multiplier.png", [System.Drawing.Imaging.ImageFormat]::Png)
Copy-Item "$artDir/CartoonBadge_Multiplier.png" "$artDir/CartoonCountdownBurst.png" -Force
$bmp.Dispose()


# ========================================================
# 13. Progress Fill Bar
# ========================================================
Write-Host "Generating CartoonComboFill.png..."
$w = 640; $h = 60
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

$fPath = Create-RoundedRectPath 4 4 ($w - 8) ($h - 8) 24
$fGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 4), (New-Object System.Drawing.PointF 0, ($h - 4)), ([System.Drawing.Color]::FromArgb(255, 255, 238, 70)), ([System.Drawing.Color]::FromArgb(255, 250, 140, 15))
$g.FillPath($fGrad, $fPath)
$fPen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, 255, 250, 180)), 3
$g.DrawPath($fPen, $fPath)
$fGrad.Dispose(); $fPen.Dispose()

$glPath = Create-RoundedRectPath 14 8 ($w - 28) 22 10
$glGrad = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.PointF 0, 8), (New-Object System.Drawing.PointF 0, 30), ([System.Drawing.Color]::FromArgb(190, 255, 255, 255)), ([System.Drawing.Color]::FromArgb(20, 255, 255, 255))
$g.FillPath($glGrad, $glPath)
$glGrad.Dispose(); $glPath.Dispose(); $fPath.Dispose()

$g.Dispose()
$bmp.Save("$artDir/CartoonComboFill.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Host "--- Successfully Generated All Polished Assets! ---"
