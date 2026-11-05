Add-Type -AssemblyName System.Drawing

$artDir = "Assets/CyberWhack/Art"
if (!(Test-Path $artDir)) { New-Item -ItemType Directory -Path $artDir -Force }

# Palette
$CarbonGraphite = [System.Drawing.Color]::FromArgb(255, 11, 14, 20)
$DarkCavity     = [System.Drawing.Color]::FromArgb(255, 5, 7, 10)
$SteelRim        = [System.Drawing.Color]::FromArgb(255, 45, 55, 72)
$SteelBevel      = [System.Drawing.Color]::FromArgb(255, 74, 85, 104)
$ElectricCyan    = [System.Drawing.Color]::FromArgb(255, 0, 229, 255)
$WarningAmber    = [System.Drawing.Color]::FromArgb(255, 255, 183, 0)
$HazardCrimson   = [System.Drawing.Color]::FromArgb(255, 255, 0, 85)
$TitaniumChassis = [System.Drawing.Color]::FromArgb(255, 143, 160, 181)
$DarkTitanium    = [System.Drawing.Color]::FromArgb(255, 70, 80, 95)
$ChromeGold      = [System.Drawing.Color]::FromArgb(255, 245, 197, 24)
$DarkGold        = [System.Drawing.Color]::FromArgb(255, 180, 130, 15)

# 1. HatchRing
$size = 256
$bmp = New-Object System.Drawing.Bitmap $size, $size
$cx = ($size - 1) * 0.5
$cy = ($size - 1) * 0.5
$maxR = $cx
$innerHoleR = $maxR * 0.68
$ledTrackR = $maxR * 0.78
$bevelR = $maxR * 0.94

for ($y = 0; $y -lt $size; $y++) {
    for ($x = 0; $x -lt $size; $x++) {
        $dx = $x - $cx
        $dy = $y - $cy
        $r = [Math]::Sqrt($dx * $dx + $dy * $dy)
        if ($r -gt $maxR) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        } elseif ($r -gt $bevelR) {
            $bmp.SetPixel($x, $y, $SteelBevel)
        } elseif ($r -gt ($ledTrackR + 3)) {
            $angle = [Math]::Atan2($dy, $dx)
            $isBolt = [Math]::Abs([Math]::Sin($angle * 4.0)) -gt 0.96 -and $r -gt ($ledTrackR + 10)
            $col = if ($isBolt) { $SteelBevel } else { $SteelRim }
            $bmp.SetPixel($x, $y, $col)
        } elseif ($r -gt ($ledTrackR - 3)) {
            $bmp.SetPixel($x, $y, $ElectricCyan)
        } elseif ($r -gt $innerHoleR) {
            $bmp.SetPixel($x, $y, $CarbonGraphite)
        } else {
            $depth = [Math]::Min(1.0, [Math]::Max(0.0, $r / $innerHoleR))
            $rVal = [int](5 + $depth * 6)
            $gVal = [int](7 + $depth * 7)
            $bVal = [int](10 + $depth * 10)
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $rVal, $gVal, $bVal))
        }
    }
}
$bmp.Save("$artDir/HatchRing.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 2. StandardCyberMole
$bmp = New-Object System.Drawing.Bitmap $size, $size
$cy = ($size - 1) * 0.44
for ($y = 0; $y -lt $size; $y++) {
    for ($x = 0; $x -lt $size; $x++) {
        $dx = $x - $cx
        $dy = $y - $cy
        $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        $isAntenna = ($x -ge ($cx - 60) -and $x -le ($cx - 52) -and $y -ge ($cy + 40) -and $y -le ($cy + 105)) -or `
                     ($x -ge ($cx + 52) -and $x -le ($cx + 60) -and $y -ge ($cy + 40) -and $y -le ($cy + 105))
        if ($isAntenna) {
            $col = if ($y -ge ($cy + 95)) { $ElectricCyan } else { $SteelRim }
            $bmp.SetPixel($x, $y, $col)
            continue
        }
        $t = [Math]::Min(1.0, [Math]::Max(0.0, ($dy + 50) / 120.0))
        $headW = 88.0 + (52.0 - 88.0) * $t
        if ([Math]::Abs($dx) -lt $headW -and $dy -ge -65 -and $dy -le 65) {
            if ($dy -ge -5 -and $dy -le 16 -and [Math]::Abs($dx) -lt 58) {
                if ($dy -ge -2 -and $dy -le 13 -and [Math]::Abs($dx) -lt 54) {
                    $bmp.SetPixel($x, $y, $ElectricCyan)
                } else {
                    $bmp.SetPixel($x, $y, $CarbonGraphite)
                }
            } elseif ($dy -lt -20 -and $dy -ge -55 -and [Math]::Abs($dx) -lt 32) {
                $stripe = ([int]$dy % 8)
                $col = if ($stripe -lt 4) { $CarbonGraphite } else { $SteelRim }
                $bmp.SetPixel($x, $y, $col)
            } else {
                $col = if ([Math]::Abs($dx) -gt $headW * 0.5) { $DarkTitanium } else { $TitaniumChassis }
                $bmp.SetPixel($x, $y, $col)
            }
        }
    }
}
$bmp.Save("$artDir/StandardCyberMole.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 3. OverclockedGoldMole
$bmp = New-Object System.Drawing.Bitmap $size, $size
for ($y = 0; $y -lt $size; $y++) {
    for ($x = 0; $x -lt $size; $x++) {
        $dx = $x - $cx
        $dy = $y - $cy
        $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        $coilL = [Math]::Abs($dx + 72) + [Math]::Abs(($dy % 20) - 10)
        $coilR = [Math]::Abs($dx - 72) + [Math]::Abs(($dy % 20) - 10)
        if (($coilL -lt 8 -or $coilR -lt 8) -and $dy -gt -40 -and $dy -lt 60) {
            $bmp.SetPixel($x, $y, $WarningAmber)
            continue
        }
        $t = [Math]::Min(1.0, [Math]::Max(0.0, ($dy + 50) / 120.0))
        $headW = 88.0 + (52.0 - 88.0) * $t
        if ([Math]::Abs($dx) -lt $headW -and $dy -ge -65 -and $dy -le 65) {
            if ($dy -ge -5 -and $dy -le 16 -and [Math]::Abs($dx) -lt 58) {
                if ($dy -ge -2 -and $dy -le 13 -and [Math]::Abs($dx) -lt 54) {
                    $bmp.SetPixel($x, $y, $WarningAmber)
                } else {
                    $bmp.SetPixel($x, $y, $CarbonGraphite)
                }
            } elseif ($dy -lt -20 -and $dy -ge -55 -and [Math]::Abs($dx) -lt 32) {
                $bmp.SetPixel($x, $y, $WarningAmber)
            } else {
                $col = if ([Math]::Abs($dx) -gt $headW * 0.65) { $DarkGold } else { $ChromeGold }
                $bmp.SetPixel($x, $y, $col)
            }
        }
    }
}
$bmp.Save("$artDir/OverclockedGoldMole.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 4. ArmoredTankMole & Cracked
foreach ($cracked in @($false, $true)) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    for ($y = 0; $y -lt $size; $y++) {
        for ($x = 0; $x -lt $size; $x++) {
            $dx = $x - $cx
            $dy = $y - $cy
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            $t = [Math]::Min(1.0, [Math]::Max(0.0, ($dy + 55) / 130.0))
            $headW = 96.0 + (62.0 - 96.0) * $t
            if ([Math]::Abs($dx) -lt $headW -and $dy -ge -65 -and $dy -le 72) {
                $isRivet = ([Math]::Abs($dy) % 28 -lt 5) -and ([Math]::Abs($dx) -gt ($headW - 14))
                if ($isRivet) {
                    $bmp.SetPixel($x, $y, $SteelBevel)
                    continue
                }
                if ($dy -ge -4 -and $dy -le 14 -and [Math]::Abs($dx) -lt 62) {
                    if ($cracked) {
                        $crackLine = [Math]::Sin($dx * 0.15) * 6.0
                        if ([Math]::Abs(($dy - 5) - $crackLine) -lt 2.5) {
                            $bmp.SetPixel($x, $y, $ElectricCyan)
                        } else {
                            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, 35, 45, 55))
                        }
                    } else {
                        $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, 20, 28, 38))
                    }
                } elseif ($dy -gt 20 -and $dy -lt 60 -and [Math]::Abs($dx) -lt 22) {
                    $bmp.SetPixel($x, $y, $SteelRim)
                } else {
                    $col = if ([Math]::Abs($dx) -gt $headW * 0.7) { $SteelRim } else { $CarbonGraphite }
                    $bmp.SetPixel($x, $y, $col)
                }
            }
        }
    }
    $fname = if ($cracked) { "ArmoredTankMole_Cracked.png" } else { "ArmoredTankMole.png" }
    $bmp.Save("$artDir/$fname", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# 5. EmpDecoyMine
$bmp = New-Object System.Drawing.Bitmap $size, $size
$cy = ($size - 1) * 0.5
$sphereR = 64.0
for ($y = 0; $y -lt $size; $y++) {
    for ($x = 0; $x -lt $size; $x++) {
        $dx = $x - $cx
        $dy = $y - $cy
        $dist = [Math]::Sqrt($dx * $dx + $dy * $dy)
        $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        $angle = [Math]::Atan2($dy, $dx) * (180.0 / [Math]::PI)
        $isProng = (([Math]::Abs($angle % 90.0) -lt 8.0) -or ([Math]::Abs(($angle % 90.0) - 90.0) -lt 8.0)) -and $dist -gt 55 -and $dist -lt 105
        if ($isProng) {
            $col = if ($dist -gt 92) { $HazardCrimson } else { $SteelRim }
            $bmp.SetPixel($x, $y, $col)
            continue
        }
        if ($dist -le $sphereR) {
            $stripe = ($dx + $dy) * 0.12
            $isBlack = ([Math]::Sin($stripe) -gt 0)
            $col = if ($isBlack) { $CarbonGraphite } else { $HazardCrimson }
            if ($dist -lt 18) {
                $bmp.SetPixel($x, $y, $WarningAmber)
            } else {
                $bmp.SetPixel($x, $y, $col)
            }
        }
    }
}
$bmp.Save("$artDir/EmpDecoyMine.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 6. ShockwaveRing
$swSize = 128
$bmp = New-Object System.Drawing.Bitmap $swSize, $swSize
$swC = ($swSize - 1) * 0.5
$swR = $swC * 0.85
$thickness = 7.0
for ($y = 0; $y -lt $swSize; $y++) {
    for ($x = 0; $x -lt $swSize; $x++) {
        $dx = $x - $swC
        $dy = $y - $swC
        $r = [Math]::Sqrt($dx * $dx + $dy * $dy)
        $delta = [Math]::Abs($r - $swR)
        if ($delta -lt $thickness) {
            $alpha = [int]((1.0 - ($delta / $thickness)) * 255.0)
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($alpha, 0, 229, 255))
        } else {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        }
    }
}
$bmp.Save("$artDir/ShockwaveRing.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 7. TargetReticle
$bmp = New-Object System.Drawing.Bitmap $swSize, $swSize
$retR = $swC * 0.70
for ($y = 0; $y -lt $swSize; $y++) {
    for ($x = 0; $x -lt $swSize; $x++) {
        $dx = $x - $swC
        $dy = $y - $swC
        $r = [Math]::Sqrt($dx * $dx + $dy * $dy)
        $isCrossX = [Math]::Abs($dy) -lt 3 -and ([Math]::Abs($dx) -gt ($retR - 15) -and [Math]::Abs($dx) -lt ($retR + 15))
        $isCrossY = [Math]::Abs($dx) -lt 3 -and ([Math]::Abs($dy) -gt ($retR - 15) -and [Math]::Abs($dy) -lt ($retR + 15))
        if ($isCrossX -or $isCrossY -or [Math]::Abs($r - $retR) -lt 3.0) {
            $bmp.SetPixel($x, $y, $ElectricCyan)
        } else {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        }
    }
}
$bmp.Save("$artDir/TargetReticle.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 8. PanelFrame
$pw = 256
$ph = 128
$bmp = New-Object System.Drawing.Bitmap $pw, $ph
for ($y = 0; $y -lt $ph; $y++) {
    for ($x = 0; $x -lt $pw; $x++) {
        $isBorder = ($x -lt 3 -or $x -ge ($pw - 3) -or $y -lt 3 -or $y -ge ($ph - 3))
        $isCorner = (($x -lt 14 -or $x -ge ($pw - 14)) -and ($y -lt 14 -or $y -ge ($ph - 14)))
        if ($isCorner) {
            $bmp.SetPixel($x, $y, $ElectricCyan)
        } elseif ($isBorder) {
            $bmp.SetPixel($x, $y, $SteelRim)
        } else {
            $bmp.SetPixel($x, $y, $CarbonGraphite)
        }
    }
}
$bmp.Save("$artDir/PanelFrame.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output "Successfully generated all 9 sprite PNG files into Assets/CyberWhack/Art!"
