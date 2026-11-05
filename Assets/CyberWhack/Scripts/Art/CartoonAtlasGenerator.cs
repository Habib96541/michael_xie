using System;
using System.IO;
using UnityEngine;

namespace CyberWhack.Art
{
    public enum CartoonMoleVariant
    {
        Normal,
        Golden,
        Helmet,
        HelmetCracked
    }

    /// <summary>
    /// Generates high-resolution, vector-clean, copyright-safe cartoon assets
    /// matching the reference screenshot from plays.org/whack-a-mole:
    /// - Warm desert/dirt earth ground (#ed973e)
    /// - Dark charcoal moles with beady specular eyes and terracotta snout
    /// - Golden overclocked mole with royal crown
    /// - Construction hard-hat mole (cracks on first hit)
    /// - Classic cartoon round black bomb with burning fire fuse
    /// - Layered subterranean dirt mounds with rounded mud clods
    /// - Glossy golden Start button & green Settings gear icon
    /// - Comic mallet strike VFX with starburst particles
    /// </summary>
    public static class CartoonAtlasGenerator
    {
        // Reference Color Palette from Screenshot
        public static readonly Color32 GroundOchre        = new Color32(237, 151, 62, 255);  // #ed973e
        public static readonly Color32 MoleCharcoal       = new Color32(49, 49, 53, 255);    // #313135
        public static readonly Color32 MoleShade          = new Color32(32, 32, 35, 255);    // #202023
        public static readonly Color32 MoleMuzzle         = new Color32(178, 101, 71, 255);  // #b26547
        public static readonly Color32 MoleMuzzleShade    = new Color32(145, 80, 55, 255);
        public static readonly Color32 MoleNose           = new Color32(28, 28, 30, 255);    // #1c1c1e
        public static readonly Color32 ClawsWhite         = new Color32(250, 250, 255, 255);
        
        public static readonly Color32 GoldFur            = new Color32(245, 176, 65, 255);  // #f5b041
        public static readonly Color32 GoldFurShade       = new Color32(214, 137, 16, 255);
        public static readonly Color32 CrownGold          = new Color32(255, 215, 0, 255);   // #ffd700
        
        public static readonly Color32 HardHatYellow      = new Color32(244, 208, 63, 255);  // #f4d03f
        public static readonly Color32 HardHatShade       = new Color32(212, 172, 13, 255);
        public static readonly Color32 HardHatHighlight   = new Color32(254, 249, 231, 255);
        
        public static readonly Color32 BombBody           = new Color32(28, 28, 30, 255);    // #1c1c1e
        public static readonly Color32 BombHighlight      = new Color32(255, 255, 255, 230);
        public static readonly Color32 FlameOrange        = new Color32(255, 87, 34, 255);   // #ff5722
        public static readonly Color32 FlameYellow        = new Color32(255, 235, 59, 255);  // #ffeb3b
        
        public static readonly Color32 HoleCavity         = new Color32(40, 22, 12, 255);    // #28160c
        public static readonly Color32 DirtDark           = new Color32(65, 38, 20, 255);    // #412614
        public static readonly Color32 MudClodBase        = new Color32(86, 53, 29, 255);    // #56351d
        public static readonly Color32 MudClodHighlight   = new Color32(120, 74, 40, 255);   // #784a28
        
        public static readonly Color32 StartBtnTop        = new Color32(255, 220, 0, 255);   // #ffdc00
        public static readonly Color32 StartBtnBottom     = new Color32(243, 156, 18, 255);  // #f39c12
        public static readonly Color32 StartBtnShadow     = new Color32(185, 119, 14, 255);
        
        public static readonly Color32 SettingsGreenTop   = new Color32(76, 217, 100, 255);  // #4cd964
        public static readonly Color32 SettingsGreenBase  = new Color32(46, 180, 75, 255);
        public static readonly Color32 SettingsGreenShadow= new Color32(30, 130, 50, 255);

        // ==========================================
        // 1. CARTOON MOLE SPRITE (Normal, Gold, Helmet, Cracked)
        // ==========================================
        public static Sprite CreateCartoonMoleSprite(int size = 256, CartoonMoleVariant variant = CartoonMoleVariant.Normal)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.40f;
            float headR = size * 0.34f;
            float bodyH = size * 0.48f;

            Color32 bodyCol = (variant == CartoonMoleVariant.Golden) ? GoldFur : MoleCharcoal;
            Color32 bodyShade = (variant == CartoonMoleVariant.Golden) ? GoldFurShade : MoleShade;
            Color32 muzzleCol = (variant == CartoonMoleVariant.Golden) ? new Color32(250, 215, 160, 255) : MoleMuzzle;
            Color32 muzzleShd = (variant == CartoonMoleVariant.Golden) ? new Color32(220, 175, 120, 255) : MoleMuzzleShade;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    float dx = (x - cx) / headR;
                    float dyHead = (y - cy) / headR;
                    float distHeadSq = dx * dx + dyHead * dyHead;

                    // Body arched dome: circle for top half, rounded rectangle for lower half
                    bool inHeadTop = (y >= cy) && (distHeadSq <= 1.0f);
                    bool inBodyBase = (y < cy) && (y >= cy - bodyH) && (Mathf.Abs(x - cx) <= headR);

                    // Slightly wider base at bottom
                    if (y < cy)
                    {
                        float taper = 1.0f + (cy - y) / bodyH * 0.08f;
                        inBodyBase = (Mathf.Abs(x - cx) <= headR * taper);
                    }

                    if (inHeadTop || inBodyBase)
                    {
                        // 3D smooth lighting shading
                        float light = Mathf.Clamp01(1.0f - (dx * 0.35f + (y - cy) / headR * 0.35f));
                        Color32 c = Color32.Lerp(bodyCol, bodyShade, light * 0.5f);
                        cols[idx] = c;
                    }

                    // Construction Hard Hat
                    if (variant == CartoonMoleVariant.Helmet || variant == CartoonMoleVariant.HelmetCracked)
                    {
                        float hy = cy + headR * 0.28f;
                        float hdx = (x - cx) / (headR * 1.12f);
                        float hdy = (y - hy) / (headR * 0.72f);
                        float hDistSq = hdx * hdx + hdy * hdy;

                        // Hat Dome
                        if (y >= hy - 2 && hDistSq <= 1.0f)
                        {
                            float shade = Mathf.Clamp01((x - cx) / (headR * 1.12f) * 0.4f + (hy + headR * 0.6f - y) / (headR * 0.6f) * 0.4f);
                            cols[idx] = Color32.Lerp(HardHatYellow, HardHatShade, shade);

                            // Hat Center Ridge
                            if (Mathf.Abs(x - cx) <= 6 && y > hy + 4)
                            {
                                cols[idx] = HardHatHighlight;
                            }
                        }

                        // Hat Rim / Visor Flange
                        float rimY = hy - 4;
                        if (Mathf.Abs(y - rimY) <= 7 && Mathf.Abs(x - cx) <= headR * 1.18f)
                        {
                            cols[idx] = (y >= rimY) ? HardHatHighlight : HardHatShade;
                        }

                        // Fracture cracks on cracked variant
                        if (variant == CartoonMoleVariant.HelmetCracked)
                        {
                            // Jagged crack line from top right down to visor
                            float crackX = cx + 22 + (y - hy) * 0.35f + Mathf.Sin(y * 0.4f) * 4f;
                            if (y >= hy - 4 && y <= hy + headR * 0.65f && Mathf.Abs(x - crackX) <= 2.2f)
                            {
                                cols[idx] = new Color32(40, 30, 10, 255);
                            }
                            // Branch crack
                            float crackBranch = crackX - 12 - (y - hy) * 0.2f;
                            if (y >= hy + 10 && y <= hy + 35 && Mathf.Abs(x - crackBranch) <= 1.8f)
                            {
                                cols[idx] = new Color32(40, 30, 10, 255);
                            }
                        }
                    }

                    // Golden Crown for Gold Mole
                    if (variant == CartoonMoleVariant.Golden)
                    {
                        float crownBaseY = cy + headR * 0.82f;
                        if (y >= crownBaseY && y <= crownBaseY + 28 && Mathf.Abs(x - cx) <= 36)
                        {
                            // 3 pointed crown
                            float lx = x - cx;
                            bool inLeftPeak = (Mathf.Abs(lx + 24) <= 10) && (y <= crownBaseY + 26 - Mathf.Abs(lx + 24));
                            bool inCenterPeak = (Mathf.Abs(lx) <= 12) && (y <= crownBaseY + 32 - Mathf.Abs(lx) * 1.3f);
                            bool inRightPeak = (Mathf.Abs(lx - 24) <= 10) && (y <= crownBaseY + 26 - Mathf.Abs(lx - 24));
                            bool inCrownBand = (y <= crownBaseY + 10);

                            if (inLeftPeak || inCenterPeak || inRightPeak || inCrownBand)
                            {
                                cols[idx] = (y > crownBaseY + 18) ? CrownGold : new Color32(230, 175, 0, 255);
                            }
                        }
                    }

                    // Snout / Muzzle (Centered rounded pear shape)
                    float snoutCy = cy - headR * 0.18f;
                    float snoutR = headR * 0.42f;
                    float sDx = (x - cx) / snoutR;
                    float sDy = (y - snoutCy) / (snoutR * 0.85f);
                    if (sDx * sDx + sDy * sDy <= 1.0f)
                    {
                        float snoutLight = Mathf.Clamp01(1.0f - (sDx * 0.3f + sDy * 0.3f));
                        cols[idx] = Color32.Lerp(muzzleCol, muzzleShd, snoutLight * 0.4f);

                        // Black nose at top of snout
                        float noseCy = snoutCy + snoutR * 0.42f;
                        float nDx = (x - cx) / (snoutR * 0.38f);
                        float nDy = (y - noseCy) / (snoutR * 0.28f);
                        if (nDx * nDx + nDy * nDy <= 1.0f)
                        {
                            cols[idx] = MoleNose;
                            // Tiny nose highlight
                            if (nDx > 0.1f && nDy > 0.1f && nDx * nDx + nDy * nDy <= 0.35f)
                            {
                                cols[idx] = new Color32(180, 180, 190, 255);
                            }
                        }
                    }

                    // Beady Eyes (Left & Right)
                    float eyeY = cy + headR * 0.22f;
                    float eyeXDist = headR * 0.48f;
                    float eyeR = headR * 0.12f;

                    // Skip eyes if helmet covers them (helmet is placed high enough so eyes are visible)
                    float dEyeLeft = Mathf.Sqrt(Mathf.Pow(x - (cx - eyeXDist), 2) + Mathf.Pow(y - eyeY, 2));
                    float dEyeRight = Mathf.Sqrt(Mathf.Pow(x - (cx + eyeXDist), 2) + Mathf.Pow(y - eyeY, 2));

                    if (dEyeLeft <= eyeR || dEyeRight <= eyeR)
                    {
                        cols[idx] = new Color32(10, 10, 12, 255);

                        // White Specular Highlight Glint
                        float glintLx = (cx - eyeXDist) + eyeR * 0.28f;
                        float glintRx = (cx + eyeXDist) + eyeR * 0.28f;
                        float glintY = eyeY + eyeR * 0.28f;
                        float dGlintLeft = Mathf.Sqrt(Mathf.Pow(x - glintLx, 2) + Mathf.Pow(y - glintY, 2));
                        float dGlintRight = Mathf.Sqrt(Mathf.Pow(x - glintRx, 2) + Mathf.Pow(y - glintY, 2));

                        if (dGlintLeft <= eyeR * 0.45f || dGlintRight <= eyeR * 0.45f)
                        {
                            cols[idx] = new Color32(255, 255, 255, 255);
                        }
                    }

                    // Front Paws with 3 White Claws
                    float pawY = cy - bodyH * 0.72f;
                    float pawXDist = headR * 0.75f;
                    float pawR = headR * 0.26f;

                    // Left Paw & Right Paw mounds
                    float dPawLeft = Mathf.Sqrt(Mathf.Pow(x - (cx - pawXDist), 2) + Mathf.Pow(y - pawY, 2));
                    float dPawRight = Mathf.Sqrt(Mathf.Pow(x - (cx + pawXDist), 2) + Mathf.Pow(y - pawY, 2));

                    if (dPawLeft <= pawR || dPawRight <= pawR)
                    {
                        cols[idx] = bodyCol;
                    }

                    // 3 White Claws per paw
                    for (int c = -1; c <= 1; c++)
                    {
                        float clawY = pawY + pawR * 0.5f;
                        // Left claws
                        float lClawX = (cx - pawXDist) + c * 10f;
                        float dClawL = Mathf.Sqrt(Mathf.Pow(x - lClawX, 2) + Mathf.Pow((y - clawY) * 0.7f, 2));
                        if (dClawL <= 7f)
                        {
                            cols[idx] = ClawsWhite;
                        }

                        // Right claws
                        float rClawX = (cx + pawXDist) + c * 10f;
                        float dClawR = Mathf.Sqrt(Mathf.Pow(x - rClawX, 2) + Mathf.Pow((y - clawY) * 0.7f, 2));
                        if (dClawR <= 7f)
                        {
                            cols[idx] = ClawsWhite;
                        }
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), 100f);
        }

        // ==========================================
        // 2. CARTOON BOMB HAZARD SPRITE
        // ==========================================
        public static Sprite CreateCartoonBombSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[size * size];

            float cx = size * 0.46f;
            float cy = size * 0.42f;
            float bombR = size * 0.32f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    // Bomb sphere
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= bombR)
                    {
                        // 3D sphere gradient
                        float light = Mathf.Clamp01(1.0f - (dx / bombR * 0.45f + dy / bombR * 0.45f));
                        cols[idx] = Color32.Lerp(BombBody, new Color32(50, 50, 55, 255), light * 0.4f);

                        // Curved crescent specular shine on top left
                        float hx = cx - bombR * 0.42f;
                        float hy = cy + bombR * 0.42f;
                        float hDist = Mathf.Sqrt(Mathf.Pow(x - hx, 2) + Mathf.Pow(y - hy, 2));
                        if (hDist <= bombR * 0.28f)
                        {
                            cols[idx] = BombHighlight;
                        }
                        // Secondary small glint
                        float hx2 = cx - bombR * 0.18f;
                        float hy2 = cy + bombR * 0.62f;
                        if (Mathf.Sqrt(Mathf.Pow(x - hx2, 2) + Mathf.Pow(y - hy2, 2)) <= bombR * 0.10f)
                        {
                            cols[idx] = BombHighlight;
                        }
                    }

                    // Brass Fuse Collar / Cap
                    float capY = cy + bombR - 3;
                    if (y >= capY && y <= capY + 16 && Mathf.Abs(x - cx) <= 16)
                    {
                        cols[idx] = (y >= capY + 10) ? new Color32(160, 160, 170, 255) : new Color32(100, 100, 110, 255);
                    }

                    // Curved Fuse String
                    float fuseStartY = capY + 16;
                    float fuseEndX = cx + 38;
                    float fuseEndY = fuseStartY + 28;

                    // Simple quadratic curve
                    float t = (y - fuseStartY) / (fuseEndY - fuseStartY);
                    if (t >= 0f && t <= 1f)
                    {
                        float curveX = Mathf.Lerp(cx, fuseEndX, t * t);
                        if (Mathf.Abs(x - curveX) <= 3.5f)
                        {
                            cols[idx] = new Color32(190, 160, 120, 255);
                        }
                    }

                    // Burning Fire Flame Flare at Fuse Tip
                    float flameCx = fuseEndX;
                    float flameCy = fuseEndY + 12;
                    float fdx = (x - flameCx) / 18f;
                    float fdy = (y - flameCy) / 28f;
                    float flameDist = fdx * fdx + fdy * fdy;

                    // Teardrop flame
                    if (flameDist <= 1.0f)
                    {
                        // Outer flame orange/red
                        cols[idx] = FlameOrange;

                        // Inner flame yellow core
                        float cdx = (x - flameCx) / 9f;
                        float cdy = (y - (flameCy - 4)) / 14f;
                        if (cdx * cdx + cdy * cdy <= 1.0f)
                        {
                            cols[idx] = FlameYellow;
                        }
                    }

                    // Little flying spark embers
                    if (Mathf.Abs(x - (flameCx + 16)) <= 2.5f && Mathf.Abs(y - (flameCy + 14)) <= 2.5f)
                    {
                        cols[idx] = FlameYellow;
                    }
                    if (Mathf.Abs(x - (flameCx - 14)) <= 2.0f && Mathf.Abs(y - (flameCy + 18)) <= 2.0f)
                    {
                        cols[idx] = FlameOrange;
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.25f), 100f);
        }

        // ==========================================
        // 3. CARTOON DIRT MOUND HOLE (Hatch Node Rim)
        // ==========================================
        public static Sprite CreateCartoonDirtMoundSprite(int w = 320, int h = 180)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[w * h];

            float cx = w * 0.5f;
            float cyHole = h * 0.52f;
            float holeRx = w * 0.42f;
            float holeRy = h * 0.26f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    // Hole Cavity Oval
                    float hdx = (x - cx) / holeRx;
                    float hdy = (y - cyHole) / holeRy;
                    float holeDistSq = hdx * hdx + hdy * hdy;

                    if (holeDistSq <= 1.0f)
                    {
                        // Dark subterranean shadow gradient
                        float depth = Mathf.Clamp01(1.0f - (y - (cyHole - holeRy)) / (holeRy * 2.0f));
                        cols[idx] = Color32.Lerp(HoleCavity, DirtDark, depth * 0.6f);
                    }

                    // Rim of dirt clumps in front (lower half) and around rim
                    float moundCy = cyHole - holeRy * 0.4f;
                    float moundRx = holeRx * 1.08f;
                    float moundRy = holeRy * 1.35f;

                    float mdx = (x - cx) / moundRx;
                    float mdy = (y - moundCy) / moundRy;
                    float moundDistSq = mdx * mdx + mdy * mdy;

                    // Clods: rounded multi-bump mud clumps
                    float bump1 = Mathf.Sin(x * 0.08f) * 8f;
                    float bump2 = Mathf.Cos(x * 0.16f) * 5f;
                    float clumpY = moundCy + bump1 + bump2;

                    if (moundDistSq <= 1.0f && y <= cyHole + 6)
                    {
                        // Mud clod layering
                        float clodLight = Mathf.Clamp01((y - (moundCy - moundRy)) / (moundRy * 1.4f));
                        Color32 clodCol = Color32.Lerp(DirtDark, MudClodHighlight, clodLight);

                        // Organic shadow lines
                        if (Mathf.Abs(bump1) < 2f)
                        {
                            clodCol = MudClodBase;
                        }

                        // Blend over hole bottom
                        cols[idx] = clodCol;
                    }

                    // Dark cast shadow under the mound
                    if (y <= moundCy - moundRy * 0.5f && moundDistSq <= 1.15f)
                    {
                        float alpha = Mathf.Clamp01((1.15f - moundDistSq) / 0.15f);
                        if (cols[idx].a == 0)
                        {
                            cols[idx] = new Color32(40, 22, 12, (byte)(alpha * 160));
                        }
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.45f), 100f);
        }

        // ==========================================
        // 4. CARTOON MALLET STRIKE (Wooden Toy Hammer)
        // ==========================================
        public static Sprite CreateCartoonMalletSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;

            // 35 degree rotation angle for dynamic whack swing
            float angleRad = 35f * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(angleRad);
            float sinA = Mathf.Sin(angleRad);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    // Rotate coordinates around center
                    float rx = (x - cx) * cosA - (y - cy) * sinA;
                    float ry = (x - cx) * sinA + (y - cy) * cosA;

                    // Mallet Head: Cylinder block at top (rx in [-55, 55], ry in [20, 80])
                    if (Mathf.Abs(rx) <= 52 && ry >= 20 && ry <= 80)
                    {
                        // Wooden bevel gradient
                        float light = Mathf.Clamp01((rx + 52) / 104f);
                        Color32 wood = Color32.Lerp(new Color32(160, 90, 45, 255), new Color32(110, 55, 25, 255), light);

                        // Metallic/dark grain bands on left & right edges
                        if (Mathf.Abs(rx) >= 42)
                        {
                            wood = new Color32(80, 40, 18, 255);
                        }
                        cols[idx] = wood;
                    }

                    // Wooden Handle: long cylinder going down (rx in [-10, 10], ry in [-80, 20])
                    if (Mathf.Abs(rx) <= 9 && ry >= -85 && ry <= 20)
                    {
                        float handleLight = Mathf.Clamp01((rx + 9) / 18f);
                        Color32 hCol = Color32.Lerp(new Color32(225, 185, 135, 255), new Color32(180, 140, 95, 255), handleLight);
                        cols[idx] = hCol;
                    }

                    // Rounded handle pommel at bottom
                    if (Mathf.Abs(rx) <= 13 && ry >= -95 && ry <= -85)
                    {
                        cols[idx] = new Color32(160, 90, 45, 255);
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ==========================================
        // 5. CARTOON COMIC STARBURST (Hit Impact VFX)
        // ==========================================
        public static Sprite CreateCartoonStarburstSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[size * size];

            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float maxR = size * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    float dx = x - cx;
                    float dy = y - cy;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);

                    // 12-point comic impact starburst
                    float starR = maxR * (0.65f + 0.35f * Mathf.Cos(angle * 12f));

                    if (r <= starR)
                    {
                        // Inner bright yellow, outer golden orange
                        float t = r / starR;
                        Color32 starCol = Color32.Lerp(new Color32(255, 255, 150, 255), new Color32(255, 180, 0, 255), t);

                        // Bright comic edge outline
                        if (r >= starR - 5f)
                        {
                            starCol = new Color32(255, 90, 0, 255);
                        }
                        cols[idx] = starCol;
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ==========================================
        // 6. GLOSSY GOLDEN START BUTTON
        // ==========================================
        public static Sprite CreateCartoonStartButtonSprite(int w = 280, int h = 90)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[w * h];

            float r = h * 0.48f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    // Rounded capsule test
                    float cx = (x < r) ? r : (x > w - r ? w - r : x);
                    float cy = h * 0.5f;
                    float dist = Mathf.Sqrt(Mathf.Pow(x - cx, 2) + Mathf.Pow(y - cy, 2));

                    if (dist <= r)
                    {
                        // Gradient from top (golden yellow) to bottom (deep amber)
                        float t = (float)y / h;
                        Color32 btnCol = Color32.Lerp(StartBtnBottom, StartBtnTop, t);

                        // Bottom 3D shadow rim
                        if (y <= 8 && dist <= r)
                        {
                            btnCol = StartBtnShadow;
                        }

                        // Top specular shine highlight bar
                        if (y >= h * 0.55f && y <= h * 0.88f && dist <= r - 4)
                        {
                            float hlAlpha = Mathf.Clamp01((y - h * 0.55f) / (h * 0.33f));
                            btnCol = Color32.Lerp(btnCol, new Color32(255, 255, 255, 255), hlAlpha * 0.45f);
                        }

                        cols[idx] = btnCol;
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        // ==========================================
        // 7. CARTOON SETTINGS GEAR BUTTON (Grass Green)
        // ==========================================
        public static Sprite CreateCartoonSettingsButtonSprite(int size = 90)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color32[] cols = new Color32[size * size];

            float cornerR = 20f;
            float cx = size * 0.5f;
            float cy = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = new Color32(0, 0, 0, 0);

                    // Rounded rectangle
                    float rx = (x < cornerR) ? cornerR : (x > size - cornerR ? size - cornerR : x);
                    float ry = (y < cornerR) ? cornerR : (y > size - cornerR ? size - cornerR : y);
                    float dist = Mathf.Sqrt(Mathf.Pow(x - rx, 2) + Mathf.Pow(y - ry, 2));

                    if (dist <= cornerR)
                    {
                        float t = (float)y / size;
                        Color32 bg = Color32.Lerp(SettingsGreenBase, SettingsGreenTop, t);

                        // Bottom rim shadow
                        if (y <= 7) bg = SettingsGreenShadow;
                        cols[idx] = bg;

                        // Gear Icon in White
                        float gdx = x - cx;
                        float gdy = y - cy;
                        float gr = Mathf.Sqrt(gdx * gdx + gdy * gdy);
                        float gAngle = Mathf.Atan2(gdy, gdx);

                        // 6 teeth gear
                        float gearToothR = 26f + 7f * Mathf.Cos(gAngle * 6f);
                        if (gr <= gearToothR && gr >= 10f)
                        {
                            cols[idx] = new Color32(255, 255, 255, 255);
                        }
                    }
                }
            }

            tex.SetPixels32(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
