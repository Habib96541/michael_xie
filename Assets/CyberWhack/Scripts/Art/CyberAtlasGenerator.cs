using System;
using System.IO;
using UnityEngine;

namespace CyberWhack.Art
{
    /// <summary>
    /// Generates copyright-safe, high-contrast, cel-shaded 2D vector-style sprites
    /// adhering to the "Industrial Subterranean Cybercore" aesthetic.
    /// Uses deep carbon graphite (#0b0e14), electric cyan (#00e5ff), warning amber (#ffb700),
    /// and hazard crimson (#ff0055).
    /// </summary>
    public static class CyberAtlasGenerator
    {
        public static readonly Color CarbonGraphite = new Color32(11, 14, 20, 255);
        public static readonly Color DarkCavity     = new Color32(5, 7, 10, 255);
        public static readonly Color SteelRim        = new Color32(45, 55, 72, 255);
        public static readonly Color SteelBevel      = new Color32(74, 85, 104, 255);
        public static readonly Color ElectricCyan    = new Color32(0, 229, 255, 255);
        public static readonly Color ElectricCyanGlow= new Color32(0, 229, 255, 90);
        public static readonly Color WarningAmber    = new Color32(255, 183, 0, 255);
        public static readonly Color HazardCrimson   = new Color32(255, 0, 85, 255);
        public static readonly Color TitaniumChassis = new Color32(143, 160, 181, 255);
        public static readonly Color DarkTitanium    = new Color32(70, 80, 95, 255);
        public static readonly Color ChromeGold      = new Color32(245, 197, 24, 255);
        public static readonly Color DarkGold        = new Color32(180, 130, 15, 255);

        public static Sprite CreateHatchRingSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float maxR = center;
            float innerHoleR = maxR * 0.68f;
            float ledTrackR = maxR * 0.78f;
            float bevelR = maxR * 0.94f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    int idx = y * size + x;

                    if (r > maxR)
                    {
                        cols[idx] = Color.clear;
                    }
                    else if (r > bevelR)
                    {
                        // Outer bevel rim
                        cols[idx] = SteelBevel;
                    }
                    else if (r > ledTrackR + 3f)
                    {
                        // Steel flange with rivet notches
                        float angle = Mathf.Atan2(dy, dx);
                        bool isBolt = Mathf.Abs(Mathf.Sin(angle * 4f)) > 0.96f && r > (ledTrackR + 10f);
                        cols[idx] = isBolt ? SteelBevel : SteelRim;
                    }
                    else if (r > ledTrackR - 3f)
                    {
                        // Cyan LED track lighting
                        cols[idx] = ElectricCyan;
                    }
                    else if (r > innerHoleR)
                    {
                        // Recessed interior metal lip
                        cols[idx] = CarbonGraphite;
                    }
                    else
                    {
                        // Subterranean dark hole cavity
                        float depthShade = Mathf.Clamp01(r / innerHoleR);
                        cols[idx] = Color.Lerp(DarkCavity, CarbonGraphite, depthShade * 0.5f);
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateStandardCyberMoleSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = Color.clear;

                    float dx = x - cx;
                    float dy = y - cy;

                    // Twin radio antennas (top diagonals)
                    bool isLeftAntenna = (x >= cx - 60f && x <= cx - 52f && y >= cy + 40f && y <= cy + 105f);
                    bool isRightAntenna = (x >= cx + 52f && x <= cx + 60f && y >= cy + 40f && y <= cy + 105f);
                    if (isLeftAntenna || isRightAntenna)
                    {
                        cols[idx] = (y >= cy + 95f) ? ElectricCyan : SteelRim;
                        continue;
                    }

                    // Badger/cyber head silhouette (rounded tapered trapezoid)
                    float headW = Mathf.Lerp(88f, 52f, Mathf.Clamp01((dy + 50f) / 120f));
                    if (Mathf.Abs(dx) < headW && dy >= -65f && dy <= 65f)
                    {
                        // Base chassis
                        Color baseColor = TitaniumChassis;
                        if (dx < -headW * 0.5f || dx > headW * 0.5f) baseColor = DarkTitanium;

                        // Electric Cyan LED Visor (horizontal glowing visor bar)
                        if (dy >= -5f && dy <= 16f && Mathf.Abs(dx) < 58f)
                        {
                            if (dy >= -2f && dy <= 13f && Mathf.Abs(dx) < 54f)
                            {
                                cols[idx] = ElectricCyan; // Visor core
                            }
                            else
                            {
                                cols[idx] = CarbonGraphite; // Visor frame
                            }
                        }
                        // Cyber snout & sensor grill
                        else if (dy < -20f && dy >= -55f && Mathf.Abs(dx) < 32f)
                        {
                            bool isGrillStripe = ((int)(dy) % 8) < 4;
                            cols[idx] = isGrillStripe ? CarbonGraphite : SteelRim;
                        }
                        else
                        {
                            cols[idx] = baseColor;
                        }
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), 100f);
        }

        public static Sprite CreateOverclockedGoldMoleSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = Color.clear;

                    float dx = x - cx;
                    float dy = y - cy;

                    // Dual golden lightning energy arcs (coils on sides)
                    float coilDistLeft = Mathf.Abs(dx + 72f) + Mathf.Abs((dy % 20f) - 10f);
                    float coilDistRight = Mathf.Abs(dx - 72f) + Mathf.Abs((dy % 20f) - 10f);
                    if ((coilDistLeft < 8f || coilDistRight < 8f) && dy > -40f && dy < 60f)
                    {
                        cols[idx] = WarningAmber;
                        continue;
                    }

                    // Gold chassis head
                    float headW = Mathf.Lerp(88f, 52f, Mathf.Clamp01((dy + 50f) / 120f));
                    if (Mathf.Abs(dx) < headW && dy >= -65f && dy <= 65f)
                    {
                        // Gold bevel
                        Color baseColor = (dx < 0) ? ChromeGold : Color.Lerp(ChromeGold, Color.white, 0.25f);
                        if (Mathf.Abs(dx) > headW * 0.65f) baseColor = DarkGold;

                        // Amber Overclocked Visor
                        if (dy >= -5f && dy <= 16f && Mathf.Abs(dx) < 58f)
                        {
                            if (dy >= -2f && dy <= 13f && Mathf.Abs(dx) < 54f)
                            {
                                cols[idx] = WarningAmber;
                            }
                            else
                            {
                                cols[idx] = CarbonGraphite;
                            }
                        }
                        // Overclocked turbine intake
                        else if (dy < -20f && dy >= -55f && Mathf.Abs(dx) < 32f)
                        {
                            cols[idx] = WarningAmber;
                        }
                        else
                        {
                            cols[idx] = baseColor;
                        }
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), 100f);
        }

        public static Sprite CreateArmoredTankMoleSprite(int size = 256, bool cracked = false)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.44f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = Color.clear;

                    float dx = x - cx;
                    float dy = y - cy;

                    // Heavy carbon armored helmet (wider silhouette)
                    float headW = Mathf.Lerp(96f, 62f, Mathf.Clamp01((dy + 55f) / 130f));
                    if (Mathf.Abs(dx) < headW && dy >= -65f && dy <= 72f)
                    {
                        Color helmetColor = CarbonGraphite;
                        if (Mathf.Abs(dx) > headW * 0.7f) helmetColor = SteelRim;

                        // Heavy bolt rivets along perimeter
                        bool isRivet = (Mathf.Abs(dy) % 28f < 5f) && (Mathf.Abs(dx) > headW - 14f);
                        if (isRivet)
                        {
                            cols[idx] = SteelBevel;
                            continue;
                        }

                        // Reinforced Blast Visor slit
                        if (dy >= -4f && dy <= 14f && Mathf.Abs(dx) < 62f)
                        {
                            if (cracked)
                            {
                                // Visor with jagged electric crack fissure
                                float crackLine = Mathf.Sin(dx * 0.15f) * 6f;
                                if (Mathf.Abs((dy - 5f) - crackLine) < 2.5f)
                                {
                                    cols[idx] = ElectricCyan; // Glowing fracture
                                }
                                else
                                {
                                    cols[idx] = new Color32(35, 45, 55, 255); // Damaged visor glass
                                }
                            }
                            else
                            {
                                cols[idx] = new Color32(20, 28, 38, 255); // Solid blast shield
                            }
                        }
                        // Center blast plate reinforcement
                        else if (dy > 20f && dy < 60f && Mathf.Abs(dx) < 22f)
                        {
                            cols[idx] = SteelRim;
                        }
                        else
                        {
                            cols[idx] = helmetColor;
                        }
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.2f), 100f);
        }

        public static Sprite CreateEmpDecoyMineSprite(int size = 256)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float cx = (size - 1) * 0.5f;
            float cy = (size - 1) * 0.5f;
            float sphereR = 64f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int idx = y * size + x;
                    cols[idx] = Color.clear;

                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // 4 glowing electric prongs (naval mine horns)
                    float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    bool isProng = (Mathf.Abs(angle % 90f) < 8f || Mathf.Abs((angle % 90f) - 90f) < 8f) && dist > 55f && dist < 105f;
                    if (isProng)
                    {
                        cols[idx] = (dist > 92f) ? HazardCrimson : SteelRim;
                        continue;
                    }

                    // Naval drone spherical core
                    if (dist <= sphereR)
                    {
                        // Hazard diagonal warning stripes across body
                        float stripe = (dx + dy) * 0.12f;
                        bool isBlackStripe = (Mathf.Sin(stripe) > 0f);

                        Color baseCrimson = isBlackStripe ? CarbonGraphite : HazardCrimson;

                        // Central blinking danger diode
                        if (dist < 18f)
                        {
                            cols[idx] = WarningAmber;
                        }
                        else
                        {
                            cols[idx] = baseCrimson;
                        }
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateShockwaveRingSprite(int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float targetR = center * 0.85f;
            float thickness = 7.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    int idx = y * size + x;

                    float delta = Mathf.Abs(r - targetR);
                    if (delta < thickness)
                    {
                        float alpha = 1.0f - (delta / thickness);
                        cols[idx] = new Color(ElectricCyan.r, ElectricCyan.g, ElectricCyan.b, alpha);
                    }
                    else
                    {
                        cols[idx] = Color.clear;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateTargetReticleSprite(int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];

            float center = (size - 1) * 0.5f;
            float ringR = center * 0.70f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    int idx = y * size + x;

                    // Holographic crosshair marks
                    bool isCrosshairX = Mathf.Abs(dy) < 3f && ((Mathf.Abs(dx) > ringR - 15f && Mathf.Abs(dx) < ringR + 15f));
                    bool isCrosshairY = Mathf.Abs(dx) < 3f && ((Mathf.Abs(dy) > ringR - 15f && Mathf.Abs(dy) < ringR + 15f));

                    if (isCrosshairX || isCrosshairY)
                    {
                        cols[idx] = ElectricCyan;
                    }
                    else if (Mathf.Abs(r - ringR) < 3.0f)
                    {
                        cols[idx] = ElectricCyan;
                    }
                    else
                    {
                        cols[idx] = Color.clear;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreatePanelFrameSprite(int w = 256, int h = 128)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    bool isBorder = (x < 3 || x >= w - 3 || y < 3 || y >= h - 3);
                    bool isCornerCyan = ((x < 14 || x >= w - 14) && (y < 14 || y >= h - 14));

                    if (isCornerCyan)
                    {
                        cols[idx] = ElectricCyan;
                    }
                    else if (isBorder)
                    {
                        cols[idx] = SteelRim;
                    }
                    else
                    {
                        cols[idx] = CarbonGraphite;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateCyberButtonSprite(int w = 256, int h = 80, bool isAmber = false)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[w * h];
            Color accentColor = isAmber ? WarningAmber : ElectricCyan;
            int chamfer = 14;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    bool cutTL = (x + y) < chamfer;
                    bool cutBR = ((w - 1 - x) + (h - 1 - y)) < chamfer;

                    if (cutTL || cutBR)
                    {
                        cols[idx] = Color.clear;
                        continue;
                    }

                    bool isBorder = (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3);
                    bool isChamferTL = (x + y) >= chamfer && (x + y) <= (chamfer + 2);
                    bool isChamferBR = ((w - 1 - x) + (h - 1 - y)) >= chamfer && ((w - 1 - x) + (h - 1 - y)) <= (chamfer + 2);

                    if (isBorder || isChamferTL || isChamferBR)
                    {
                        cols[idx] = accentColor;
                    }
                    else
                    {
                        float t = y / (float)h;
                        cols[idx] = Color.Lerp(CarbonGraphite, new Color(0.08f, 0.12f, 0.18f, 1f), t);
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateIntelCardSprite(int w = 256, int h = 160)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int idx = y * w + x;
                    bool isBorder = (x <= 2 || x >= w - 3 || y <= 2 || y >= h - 3);
                    bool isLeftAccent = (x <= 6 && y >= 20 && y <= h - 20);
                    bool isCorner = (x <= 10 || x >= w - 11) && (y <= 10 || y >= h - 11);

                    if (isLeftAccent || isCorner)
                    {
                        cols[idx] = ElectricCyan;
                    }
                    else if (isBorder)
                    {
                        cols[idx] = SteelRim;
                    }
                    else
                    {
                        cols[idx] = new Color32(7, 9, 13, 255);
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
