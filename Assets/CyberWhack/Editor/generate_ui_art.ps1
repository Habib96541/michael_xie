Add-Type -AssemblyName System.Drawing

$artDir = "Assets/CyberWhack/Art"
if (!(Test-Path $artDir)) { New-Item -ItemType Directory -Path $artDir -Force }

$CarbonGraphite = [System.Drawing.Color]::FromArgb(255, 11, 14, 20)
$DarkerBg       = [System.Drawing.Color]::FromArgb(255, 7, 9, 13)
$SteelRim        = [System.Drawing.Color]::FromArgb(255, 45, 55, 72)
$SteelBevel      = [System.Drawing.Color]::FromArgb(255, 74, 85, 104)
$ElectricCyan    = [System.Drawing.Color]::FromArgb(255, 0, 229, 255)
$WarningAmber    = [System.Drawing.Color]::FromArgb(255, 255, 183, 0)
$HazardCrimson   = [System.Drawing.Color]::FromArgb(255, 255, 0, 85)

# 1. CyberButton (Chamfered Sci-Fi Angled Button 256x80)
$bw = 256
$bh = 80
$bmp = New-Object System.Drawing.Bitmap $bw, $bh
$chamfer = 14
for ($y = 0; $y -lt $bh; $y++) {
    for ($x = 0; $x -lt $bw; $x++) {
        # Check chamfered corners (top-left & bottom-right cut)
        $cutTL = ($x + $y) -lt $chamfer
        $cutBR = (($bw - 1 - $x) + ($bh - 1 - $y)) -lt $chamfer
        if ($cutTL -or $cutBR) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            continue
        }

        $isBorder = ($x -le 2 -or $x -ge ($bw - 3) -or $y -le 2 -or $y -ge ($bh - 3))
        $isChamferBorderTL = ($x + $y) -ge $chamfer -and ($x + $y) -le ($chamfer + 2)
        $isChamferBorderBR = (($bw - 1 - $x) + ($bh - 1 - $y)) -ge $chamfer -and (($bw - 1 - $x) + ($bh - 1 - $y)) -le ($chamfer + 2)

        if ($isBorder -or $isChamferBorderTL -or $isChamferBorderBR) {
            $bmp.SetPixel($x, $y, $ElectricCyan)
        } else {
            # Center subtle gradient
            $t = $y / [float]$bh
            $r = [int](11 + $t * 8)
            $g = [int](14 + $t * 12)
            $b = [int](20 + $t * 22)
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
        }
    }
}
$bmp.Save("$artDir/CyberButton.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 2. CyberButton_Amber (Accent Button 256x80)
$bmp = New-Object System.Drawing.Bitmap $bw, $bh
for ($y = 0; $y -lt $bh; $y++) {
    for ($x = 0; $x -lt $bw; $x++) {
        $cutTL = ($x + $y) -lt $chamfer
        $cutBR = (($bw - 1 - $x) + ($bh - 1 - $y)) -lt $chamfer
        if ($cutTL -or $cutBR) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            continue
        }
        $isBorder = ($x -le 2 -or $x -ge ($bw - 3) -or $y -le 2 -or $y -ge ($bh - 3))
        $isChamferBorderTL = ($x + $y) -ge $chamfer -and ($x + $y) -le ($chamfer + 2)
        $isChamferBorderBR = (($bw - 1 - $x) + ($bh - 1 - $y)) -ge $chamfer -and (($bw - 1 - $x) + ($bh - 1 - $y)) -le ($chamfer + 2)

        if ($isBorder -or $isChamferBorderTL -or $isChamferBorderBR) {
            $bmp.SetPixel($x, $y, $WarningAmber)
        } else {
            $t = $y / [float]$bh
            $r = [int](25 + $t * 15)
            $g = [int](18 + $t * 10)
            $b = [int](8 + $t * 5)
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(255, $r, $g, $b))
        }
    }
}
$bmp.Save("$artDir/CyberButton_Amber.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 3. Sound Icons (ON & OFF, 64x64)
$sw = 64
$sh = 64
foreach ($isMuted in @($false, $true)) {
    $bmp = New-Object System.Drawing.Bitmap $sw, $sh
    for ($y = 0; $y -lt $sh; $y++) {
        for ($x = 0; $x -lt $sw; $x++) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
            # Speaker cone
            $isConeBack = ($x -ge 12 -and $x -le 22 -and $y -ge 24 -and $y -le 40)
            $dx = $x - 22
            $dy = [Math]::Abs($y - 32)
            $isConeFront = ($x -gt 22 -and $x -le 36 -and $dy -le (8 + $dx * 0.9))

            if ($isConeBack -or $isConeFront) {
                $bmp.SetPixel($x, $y, $ElectricCyan)
                continue
            }

            if (!$isMuted) {
                # Sound soundwave arcs
                $dist = [Math]::Sqrt(($x - 28)*($x - 28) + ($y - 32)*($y - 32))
                $isWave1 = [Math]::Abs($dist - 16) -lt 1.6 -and $x -gt 38 -and [Math]::Abs($y - 32) -lt 14
                $isWave2 = [Math]::Abs($dist - 24) -lt 1.6 -and $x -gt 44 -and [Math]::Abs($y - 32) -lt 22
                if ($isWave1 -or $isWave2) {
                    $bmp.SetPixel($x, $y, $ElectricCyan)
                }
            } else {
                # Mute cross X
                $isDiag1 = [Math]::Abs(($x - 44) - ($y - 32)) -lt 1.8 -and $x -ge 38 -and $x -le 52
                $isDiag2 = [Math]::Abs(($x - 44) + ($y - 32)) -lt 1.8 -and $x -ge 38 -and $x -le 52
                if ($isDiag1 -or $isDiag2) {
                    $bmp.SetPixel($x, $y, $HazardCrimson)
                }
            }
        }
    }
    $fname = if ($isMuted) { "SoundIcon_Off.png" } else { "SoundIcon_On.png" }
    $bmp.Save("$artDir/$fname", [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

# 4. IntelCardFrame (256x160 with glowing cyan notch)
$cw = 256
$ch = 160
$bmp = New-Object System.Drawing.Bitmap $cw, $ch
for ($y = 0; $y -lt $ch; $y++) {
    for ($x = 0; $x -lt $cw; $x++) {
        $isBorder = ($x -le 2 -or $x -ge ($cw - 3) -or $y -le 2 -or $y -ge ($ch - 3))
        $isLeftAccent = ($x -le 6 -and $y -ge 20 -and $y -le ($ch - 20))
        $isCorner = ($x -le 10 -or $x -ge ($cw - 11)) -and ($y -le 10 -or $y -ge ($ch - 11))

        if ($isLeftAccent -or $isCorner) {
            $bmp.SetPixel($x, $y, $ElectricCyan)
        } elseif ($isBorder) {
            $bmp.SetPixel($x, $y, $SteelRim)
        } else {
            $bmp.SetPixel($x, $y, $DarkerBg)
        }
    }
}
$bmp.Save("$artDir/IntelCardFrame.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

# 5. CyberTerminalGrid (Subtle sci-fi circuit scanlines background 128x128)
$gw = 128
$gh = 128
$bmp = New-Object System.Drawing.Bitmap $gw, $gh
for ($y = 0; $y -lt $gh; $y++) {
    for ($x = 0; $x -lt $gw; $x++) {
        $isGridLine = ($x % 32 -eq 0) -or ($y % 32 -eq 0)
        $isDot = ($x % 32 -eq 0) -and ($y % 32 -eq 0)
        if ($isDot) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(180, 0, 229, 255))
        } elseif ($isGridLine) {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(40, 0, 229, 255))
        } elseif ($y % 4 -eq 0) {
            # Subtle scanline
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(25, 0, 0, 0))
        } else {
            $bmp.SetPixel($x, $y, [System.Drawing.Color]::FromArgb(0, 0, 0, 0))
        }
    }
}
$bmp.Save("$artDir/CyberTerminalGrid.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output "Successfully generated Cyber UI Polish assets!"
