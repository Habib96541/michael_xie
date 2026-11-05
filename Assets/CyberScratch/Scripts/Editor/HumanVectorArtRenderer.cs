#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CyberScratch.Editor
{
    public static class HumanVectorArtRenderer
    {
        private const string SpritesPath = "Assets/CyberScratch/Sprites";

        [MenuItem("CyberScratch/Recreate All Authentic 2D Vector Assets", false, 0)]
        public static void GenerateAllVectorAssets()
        {
            if (!Directory.Exists(SpritesPath))
            {
                Directory.CreateDirectory(SpritesPath);
            }

            // 1. Six High-Fidelity 2D Vector Scratch Symbols (512x512)
            SavePNG("Symbol_BronzeGear.png", RenderBronzeCog(512));
            SavePNG("Symbol_SilverIngot.png", RenderSilverIngot(512));
            SavePNG("Symbol_GoldCore.png", RenderGoldCore(512));
            SavePNG("Symbol_PlasmaGem.png", RenderPlasmaGem(512));
            SavePNG("Symbol_CyberCrown.png", RenderCyberCrown(512));
            SavePNG("Symbol_Lucky777.png", RenderLucky777(512));

            // 2. Realistic Scratch Foil Coating & Brush Stamp
            SavePNG("Foil_CyberHex.png", RenderRealisticFoil(512), TextureWrapMode.Repeat);
            SavePNG("Brush_SoftRound.png", RenderSoftBrush(128));

            // 3. Crisp UI Frames, Borders, Badges, and Buttons
            SavePNG("UI_CellFrame.png", RenderCellFrame(512));
            SavePNG("UI_RowBorder_Idle.png", RenderRowBorder(512, 128, new Color(0.08f, 0.75f, 0.95f), new Color(0.12f, 0.16f, 0.22f)));
            SavePNG("UI_RowBorder_Win.png", RenderRowBorder(512, 128, new Color(1.0f, 0.85f, 0.15f), new Color(0.28f, 0.20f, 0.05f)));
            SavePNG("UI_PrizeTag.png", RenderPrizeTag(320, 160));
            SavePNG("UI_Button_Amber.png", RenderPillButton(360, 110, new Color(1.0f, 0.62f, 0.05f), new Color(0.85f, 0.32f, 0.02f)));
            SavePNG("UI_Button_Emerald.png", RenderPillButton(360, 110, new Color(0.05f, 0.92f, 0.45f), new Color(0.02f, 0.65f, 0.28f)));
            SavePNG("UI_Button_Cyan.png", RenderPillButton(360, 110, new Color(0.10f, 0.82f, 1.0f), new Color(0.04f, 0.48f, 0.85f)));
            SavePNG("FX_Sparkle.png", RenderSparkle(128));

            AssetDatabase.Refresh();
            ConfigureTextureImporters();

            Debug.Log("<color=#00FFAA><b>[CyberScratch]</b> All 16 authentic human-crafted 2D vector assets generated cleanly at 512x512!</color>");
        }

        private static void SavePNG(string fileName, Texture2D texture, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            byte[] bytes = texture.EncodeToPNG();
            string fullPath = Path.Combine(SpritesPath, fileName);
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(texture);
        }

        private static void ConfigureTextureImporters()
        {
            string[] files = Directory.GetFiles(SpritesPath, "*.png");
            foreach (string file in files)
            {
                string assetPath = file.Replace('\\', '/');
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 100;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;

                    if (assetPath.Contains("Foil_CyberHex"))
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                    }
                    else
                    {
                        importer.wrapMode = TextureWrapMode.Clamp;
                    }

                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        #region Vector Math & SDF Helpers

        private static float SDFCircle(Vector2 p, Vector2 c, float r)
        {
            return Vector2.Distance(p, c) - r;
        }

        private static float SDFBox(Vector2 p, Vector2 c, Vector2 b)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - b;
            return Mathf.Min(Mathf.Max(d.x, d.y), 0f) + new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude;
        }

        private static float SDFRoundedBox(Vector2 p, Vector2 c, Vector2 b, float r)
        {
            return SDFBox(p, c, b - new Vector2(r, r)) - r;
        }

        private static float AntiAlias(float d, float feather = 1.2f)
        {
            return Mathf.Clamp01(0.5f - d / feather);
        }

        private static Color BlendOver(Color bg, Color fg)
        {
            float outA = fg.a + bg.a * (1f - fg.a);
            if (outA <= 0.001f) return Color.clear;
            Vector3 rgb = (new Vector3(fg.r, fg.g, fg.b) * fg.a + new Vector3(bg.r, bg.g, bg.b) * bg.a * (1f - fg.a)) / outA;
            return new Color(rgb.x, rgb.y, rgb.z, outA);
        }

        #endregion

        #region 1. Bronze Cog (Industrial gear with copper bevels & center bolt)

        private static Texture2D RenderBronzeCog(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = size * 0.44f;
            float rootR = size * 0.35f;
            float innerR = size * 0.26f;
            float hubR = size * 0.16f;
            float holeR = size * 0.08f;
            int teethCount = 8;

            Color cDarkRim = new Color(0.24f, 0.10f, 0.04f, 1f);
            Color cCopperLight = new Color(0.96f, 0.64f, 0.38f, 1f);
            Color cCopperMid = new Color(0.82f, 0.44f, 0.20f, 1f);
            Color cCopperDark = new Color(0.52f, 0.22f, 0.08f, 1f);
            Color cSpecular = new Color(1.0f, 0.88f, 0.72f, 1f);
            Color cHubMetal = new Color(0.18f, 0.20f, 0.25f, 1f);
            Color cBolt = new Color(0.88f, 0.52f, 0.25f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float dist = Vector2.Distance(p, center);
                    float angle = Mathf.Atan2(p.y - center.y, p.x - center.x);

                    // 8-tooth gear profile
                    float toothMod = Mathf.Cos(angle * teethCount);
                    // Smooth trapezoidal tooth profile
                    float toothProfile = Mathf.Clamp((toothMod + 0.2f) * 1.8f, -1f, 1f);
                    float currentR = Mathf.Lerp(rootR, outerR, toothProfile * 0.5f + 0.5f);

                    float gearSDF = dist - currentR;
                    float gearAlpha = AntiAlias(gearSDF);

                    if (gearAlpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Shading & lighting (light from top-left)
                    float lightDir = (Mathf.Cos(angle - 2.35f) + 1f) * 0.5f; // Light from ~135 deg
                    float vGrad = (p.y / size);
                    Color bodyColor = Color.Lerp(cCopperDark, Color.Lerp(cCopperMid, cCopperLight, lightDir), vGrad * 0.5f + 0.5f);

                    // Outer tooth bevel rim
                    if (gearSDF > -6f)
                    {
                        float rimLight = (p.y > center.y || p.x < center.x) ? 0.8f : 0.2f;
                        bodyColor = Color.Lerp(cDarkRim, cSpecular, rimLight);
                    }

                    // Recessed inner circular groove
                    float grooveSDF = Mathf.Abs(dist - (rootR + innerR) * 0.5f) - (rootR - innerR) * 0.35f;
                    if (grooveSDF < 0f)
                    {
                        bodyColor = Color.Lerp(cCopperDark * 0.7f, bodyColor, Mathf.Clamp01(grooveSDF / -4f));
                    }

                    // Inner titanium hub
                    float hubSDF = dist - hubR;
                    if (hubSDF < 0f)
                    {
                        bodyColor = cHubMetal;
                        if (hubSDF > -3f) bodyColor = cDarkRim; // Chamfer rim
                    }

                    // Central copper hex bolt
                    float boltSDF = dist - holeR;
                    if (boltSDF < 0f)
                    {
                        // 6-sided bolt angle modulation
                        float hexAngle = Mathf.Repeat(angle * 6f / (2f * Mathf.PI), 1f);
                        float hexShade = Mathf.Abs(hexAngle - 0.5f) * 2f;
                        bodyColor = Color.Lerp(cBolt, cSpecular, hexShade * 0.5f);
                        if (dist < holeR * 0.45f)
                        {
                            bodyColor = cDarkRim; // Center socket
                        }
                    }

                    tex.SetPixel(x, y, new Color(bodyColor.r, bodyColor.g, bodyColor.b, gearAlpha));
                }
            }
            tex.Apply();
            return tex;
        }

        #endregion

        #region 2. Silver Ingot (Stack of two polished chrome bullion bars)

        private static Texture2D RenderSilverIngot(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            // Draw lower ingot bar
            DrawVectorIngot(tex, size,
                center: new Vector2(size * 0.46f, size * 0.36f),
                width: size * 0.68f, height: size * 0.26f, depthY: size * 0.12f,
                isTopBar: false);

            // Draw upper ingot bar (overlapping slightly higher and shifted)
            DrawVectorIngot(tex, size,
                center: new Vector2(size * 0.54f, size * 0.62f),
                width: size * 0.62f, height: size * 0.25f, depthY: size * 0.11f,
                isTopBar: true);

            tex.Apply();
            return tex;
        }

        private static void DrawVectorIngot(Texture2D tex, int size, Vector2 center, float width, float height, float depthY, bool isTopBar)
        {
            float halfW = width * 0.5f;
            float halfH = height * 0.5f;
            float slopeX = width * 0.08f;

            Color cChromeTop = new Color(0.96f, 0.98f, 1.0f, 1f);
            Color cChromeLight = new Color(0.85f, 0.90f, 0.96f, 1f);
            Color cChromeMid = new Color(0.68f, 0.76f, 0.86f, 1f);
            Color cChromeDark = new Color(0.38f, 0.45f, 0.56f, 1f);
            Color cEdgeGlint = new Color(1.0f, 1.0f, 1.0f, 1f);
            Color cShadow = new Color(0.08f, 0.10f, 0.14f, 0.9f);

            int minX = Mathf.Max(0, (int)(center.x - halfW - slopeX - 4));
            int maxX = Mathf.Min(size - 1, (int)(center.x + halfW + slopeX + 4));
            int minY = Mathf.Max(0, (int)(center.y - halfH - depthY - 4));
            int maxY = Mathf.Min(size - 1, (int)(center.y + halfH + 4));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float localX = x - center.x;
                    float localY = y - center.y;

                    // 1. Top Face: trapezoid from y = -halfH to y = halfH
                    float tTopY = (localY + halfH) / (height);
                    float insetX = (1f - tTopY) * slopeX;
                    float faceWidth = halfW - insetX;

                    bool inTopFace = (localY >= -halfH && localY <= halfH && Mathf.Abs(localX) <= faceWidth);

                    // 2. Front Face: rectangle below y = -halfH down to y = -halfH - depthY
                    bool inFrontFace = (localY >= -halfH - depthY && localY < -halfH && Mathf.Abs(localX) <= (halfW - slopeX));

                    if (!inTopFace && !inFrontFace) continue;

                    Color pixelCol = cChromeMid;

                    if (inTopFace)
                    {
                        float normX = (localX + faceWidth) / (faceWidth * 2f);
                        // Clean linear gradient from top to bottom
                        pixelCol = Color.Lerp(cChromeMid, cChromeLight, tTopY);

                        // Polished diagonal specular sheen band
                        if (Mathf.Abs(normX - 0.38f) < 0.10f)
                        {
                            pixelCol = Color.Lerp(pixelCol, cEdgeGlint, 0.9f);
                        }

                        // Top bevel rim
                        if (Mathf.Abs(localY - halfH) < 2.5f || Mathf.Abs(Mathf.Abs(localX) - faceWidth) < 2.5f)
                        {
                            pixelCol = (tTopY > 0.5f) ? cEdgeGlint : cChromeDark;
                        }

                        // Embossed hallmark stamp on top face
                        if (isTopBar && Mathf.Abs(localX) < width * 0.22f && Mathf.Abs(localY) < height * 0.22f)
                        {
                            if (Mathf.Abs(Mathf.Abs(localX) - width * 0.22f) < 2f || Mathf.Abs(Mathf.Abs(localY) - height * 0.22f) < 2f)
                            {
                                pixelCol = Color.Lerp(pixelCol, cChromeDark, 0.6f);
                            }
                        }
                    }
                    else if (inFrontFace)
                    {
                        // Front beveled face: darker chrome with bottom shadow
                        float tFrontY = (localY - (-halfH - depthY)) / depthY;
                        pixelCol = Color.Lerp(cChromeDark, cChromeMid, tFrontY);

                        // Specular vertical reflection
                        float normX = (localX + halfW) / (halfW * 2f);
                        if (Mathf.Abs(normX - 0.38f) < 0.08f)
                        {
                            pixelCol = Color.Lerp(pixelCol, cChromeLight, 0.7f);
                        }

                        // Bottom rim shadow
                        if (tFrontY < 0.2f)
                        {
                            pixelCol = Color.Lerp(cShadow, pixelCol, tFrontY * 5f);
                        }
                    }

                    Color current = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, BlendOver(current, pixelCol));
                }
            }
        }

        #endregion

        #region 3. Gold Power Core (Cylindrical high-tech battery glowing warm amber)

        private static Texture2D RenderGoldCore(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float bodyW = size * 0.44f;
            float bodyH = size * 0.70f;
            float cornerR = size * 0.08f;

            Color cGoldLight = new Color(1.0f, 0.94f, 0.65f, 1f);
            Color cGoldMid = new Color(1.0f, 0.82f, 0.18f, 1f);
            Color cGoldDark = new Color(0.58f, 0.38f, 0.06f, 1f);
            Color cCapMetal = new Color(0.82f, 0.86f, 0.92f, 1f);
            Color cChamberBg = new Color(0.08f, 0.10f, 0.14f, 1f);
            Color cAmberGlow = new Color(1.0f, 0.55f, 0.05f, 1f);
            Color cCoreYellow = new Color(1.0f, 0.95f, 0.40f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float normU = (p.x - center.x) / (bodyW * 0.5f); // -1 to 1
                    float normV = (p.y - center.y) / (bodyH * 0.5f); // -1 to 1

                    // Top terminal nub (+)
                    float nubSDF = SDFRoundedBox(p, new Vector2(center.x, center.y + bodyH * 0.52f), new Vector2(bodyW * 0.22f, size * 0.05f), 8f);
                    float nubAlpha = AntiAlias(nubSDF);

                    // Main cylinder body
                    float bodySDF = SDFRoundedBox(p, center, new Vector2(bodyW * 0.5f, bodyH * 0.5f), cornerR);
                    float bodyAlpha = AntiAlias(bodySDF);

                    if (bodyAlpha <= 0f && nubAlpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    if (nubAlpha > 0f && bodyAlpha <= 0.1f)
                    {
                        Color nubCol = Color.Lerp(cCapMetal * 0.6f, cCapMetal, (normU + 1f) * 0.5f);
                        tex.SetPixel(x, y, new Color(nubCol.r, nubCol.g, nubCol.b, nubAlpha));
                        continue;
                    }

                    // Cylindrical gradient shading
                    float cylLight = Mathf.Cos(normU * 1.5f);
                    Color bodyColor = Color.Lerp(cGoldDark, cGoldMid, Mathf.Clamp01(cylLight));

                    // Specular highlight band
                    if (Mathf.Abs(normU + 0.35f) < 0.15f)
                    {
                        bodyColor = Color.Lerp(bodyColor, cGoldLight, 0.85f);
                    }

                    // Top and bottom chrome caps
                    if (normV > 0.72f || normV < -0.72f)
                    {
                        bodyColor = Color.Lerp(cCapMetal * 0.7f, cCapMetal, (normU + 1f) * 0.5f);
                        if (Mathf.Abs(normU + 0.35f) < 0.15f) bodyColor = Color.white;
                    }
                    // Central glass energy chamber
                    else if (Mathf.Abs(normV) < 0.65f && Mathf.Abs(normU) < 0.75f)
                    {
                        bodyColor = cChamberBg;

                        // 3 glowing power cell bars
                        float segY = Mathf.Repeat((normV + 0.65f) * 2.3f, 1f);
                        if (segY > 0.25f && segY < 0.85f)
                        {
                            float glowCenter = Mathf.Cos(normU * 1.8f);
                            bodyColor = Color.Lerp(cAmberGlow, cCoreYellow, Mathf.Clamp01(glowCenter));
                        }
                    }

                    // Outer border
                    if (bodySDF > -3f)
                    {
                        bodyColor = Color.Lerp(bodyColor, Color.black, 0.4f);
                    }

                    tex.SetPixel(x, y, new Color(bodyColor.r, bodyColor.g, bodyColor.b, bodyAlpha));
                }
            }
            tex.Apply();
            return tex;
        }

        #endregion

        #region 4. Plasma Gem (Faceted violet crystal with bright edge highlights)

        private static Texture2D RenderPlasmaGem(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = size * 0.42f;
            float tableR = size * 0.20f;

            Color cCoreViolet = new Color(0.48f, 0.08f, 0.78f, 1f);
            Color cMagentaFacet = new Color(0.85f, 0.22f, 0.95f, 1f);
            Color cLightViolet = new Color(0.72f, 0.45f, 1.0f, 1f);
            Color cCyanReflect = new Color(0.20f, 0.92f, 1.0f, 1f);
            Color cWhiteGlint = new Color(1.0f, 1.0f, 1.0f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 d = p - center;

                    // Octagonal shape SDF
                    float octSDF = Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y)) + 0.4142f * Mathf.Min(Mathf.Abs(d.x), Mathf.Abs(d.y)) - outerR;
                    float alpha = AntiAlias(octSDF);

                    if (alpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float dist = d.magnitude;
                    float angle = Mathf.Atan2(d.y, d.x);
                    Color facetColor;

                    // Central Table Facet
                    if (dist < tableR)
                    {
                        float tGrad = (d.y / tableR) * 0.5f + 0.5f;
                        facetColor = Color.Lerp(cCoreViolet, cMagentaFacet, tGrad);
                    }
                    else
                    {
                        // 8 Star/Crown Facets
                        int facetIndex = Mathf.FloorToInt(Mathf.Repeat((angle + Mathf.PI * 0.125f) / (Mathf.PI * 0.25f), 8f));
                        bool isUpper = (d.y > 0f);
                        bool isLeft = (d.x < 0f);

                        if (facetIndex % 2 == 0)
                        {
                            facetColor = (isUpper && isLeft) ? Color.Lerp(cMagentaFacet, cCyanReflect, 0.55f) : cLightViolet;
                        }
                        else
                        {
                            facetColor = isUpper ? cMagentaFacet : cCoreViolet;
                        }

                        // Cyan reflective highlight on top-left edge
                        if (d.x < -outerR * 0.3f && d.y > outerR * 0.3f)
                        {
                            facetColor = Color.Lerp(facetColor, cCyanReflect, 0.8f);
                        }
                    }

                    // Facet edge grooves (sharp clean lines)
                    float tableEdge = Mathf.Abs(dist - tableR);
                    if (tableEdge < 2f || octSDF > -3f)
                    {
                        facetColor = (d.y > 0f && d.x < 0f) ? cWhiteGlint : cCyanReflect;
                    }

                    tex.SetPixel(x, y, new Color(facetColor.r, facetColor.g, facetColor.b, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        #endregion

        #region 5. Cyber Crown (Angular minimalist 3-point neon-blue royal crown)

        private static Texture2D RenderCyberCrown(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            Color cDeepBlue = new Color(0.04f, 0.18f, 0.45f, 1f);
            Color cNeonCyan = new Color(0.05f, 0.85f, 1.0f, 1f);
            Color cElectricBlue = new Color(0.12f, 0.55f, 0.95f, 1f);
            Color cWhiteJewel = new Color(0.95f, 0.98f, 1.0f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size;
                    float v = (float)y / size;

                    // Base headband: u in [0.18, 0.82], v in [0.22, 0.36]
                    bool inBase = (u >= 0.18f && u <= 0.82f && v >= 0.22f && v <= 0.36f);

                    // 3 Crown peaks: center, left, right triangles
                    bool inCenterPeak = InTriangle(u, v, 0.50f, 0.82f, 0.34f, 0.36f, 0.66f, 0.36f);
                    bool inLeftPeak = InTriangle(u, v, 0.22f, 0.70f, 0.18f, 0.36f, 0.38f, 0.36f);
                    bool inRightPeak = InTriangle(u, v, 0.78f, 0.70f, 0.62f, 0.36f, 0.82f, 0.36f);

                    // 3 Apex jewels
                    float dJewelC = Vector2.Distance(new Vector2(u, v), new Vector2(0.50f, 0.83f));
                    float dJewelL = Vector2.Distance(new Vector2(u, v), new Vector2(0.22f, 0.71f));
                    float dJewelR = Vector2.Distance(new Vector2(u, v), new Vector2(0.78f, 0.71f));

                    if (dJewelC < 0.038f || dJewelL < 0.032f || dJewelR < 0.032f)
                    {
                        tex.SetPixel(x, y, cWhiteJewel);
                        continue;
                    }

                    if (!inBase && !inCenterPeak && !inLeftPeak && !inRightPeak)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Shading & neon gradient
                    Color crownCol = Color.Lerp(cDeepBlue, Color.Lerp(cElectricBlue, cNeonCyan, v * 1.2f), v);

                    // Base neon LED tracks
                    if (inBase)
                    {
                        if (v <= 0.24f || v >= 0.34f || u <= 0.20f || u >= 0.80f)
                        {
                            crownCol = cNeonCyan;
                        }
                    }

                    tex.SetPixel(x, y, crownCol);
                }
            }
            tex.Apply();
            return tex;
        }

        private static bool InTriangle(float px, float py, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            float d1 = (px - x3) * (y2 - y3) - (x2 - x3) * (py - y3);
            float d2 = (px - x1) * (y3 - y1) - (x3 - x1) * (py - y1);
            float d3 = (px - x2) * (y1 - y2) - (x1 - x2) * (py - y2);
            bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
            return !(hasNeg && hasPos);
        }

        #endregion

        #region 6. Lucky 777 (Sharp italicized red and gold chrome '777' emblem)

        private static Texture2D RenderLucky777(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            Color cCrimson = new Color(0.92f, 0.12f, 0.24f, 1f);
            Color cCrimsonDark = new Color(0.55f, 0.04f, 0.12f, 1f);
            Color cGoldChrome = new Color(1.0f, 0.86f, 0.24f, 1f);
            Color cGoldLight = new Color(1.0f, 0.98f, 0.72f, 1f);
            Color cShadow = new Color(0.08f, 0.01f, 0.03f, 0.85f);

            // Left 7 (shadow then digit)
            DrawItalicSeven(tex, size, 0.29f, 0.49f, 0.32f, 0.50f, cShadow, isShadow: true);
            DrawItalicSeven(tex, size, 0.28f, 0.51f, 0.32f, 0.50f, cCrimson, isShadow: false, cGoldChrome, cCrimsonDark, cGoldLight);

            // Right 7 (shadow then digit)
            DrawItalicSeven(tex, size, 0.73f, 0.49f, 0.32f, 0.50f, cShadow, isShadow: true);
            DrawItalicSeven(tex, size, 0.72f, 0.51f, 0.32f, 0.50f, cCrimson, isShadow: false, cGoldChrome, cCrimsonDark, cGoldLight);

            // Center 7 (dominant, overlapping in front)
            DrawItalicSeven(tex, size, 0.51f, 0.52f, 0.36f, 0.56f, cShadow, isShadow: true);
            DrawItalicSeven(tex, size, 0.50f, 0.54f, 0.36f, 0.56f, cCrimson, isShadow: false, cGoldChrome, cCrimsonDark, cGoldLight);

            tex.Apply();
            return tex;
        }

        private static void DrawItalicSeven(Texture2D tex, int size, float cx, float cy, float w, float h, Color baseCol, bool isShadow, Color? goldBevel = null, Color? darkRed = null, Color? lightGold = null)
        {
            int px = (int)(cx * size);
            int py = (int)(cy * size);
            int pw = (int)(w * size);
            int ph = (int)(h * size);

            for (int dy = -ph / 2; dy <= ph / 2; dy++)
            {
                for (int dx = -pw / 2; dx <= pw / 2; dx++)
                {
                    int x = px + dx;
                    int y = py + dy;
                    if (x < 0 || x >= size || y < 0 || y >= size) continue;

                    float normY = (float)dy / ph; // -0.5 to 0.5
                    float normX = (float)dx / pw; // -0.5 to 0.5

                    // Forward italic shear
                    float shearedX = normX - normY * 0.32f;

                    // Top horizontal bar: normY in [0.26, 0.50], shearedX in [-0.46, 0.46]
                    bool inTopBar = (normY >= 0.26f && normY <= 0.50f && shearedX >= -0.46f && shearedX <= 0.46f);

                    // Diagonal stem: line from top right to bottom left
                    float stemLine = shearedX - (normY * 0.78f);
                    bool inStem = (Mathf.Abs(stemLine - 0.04f) < 0.16f && normY >= -0.50f && normY <= 0.32f);

                    if (!inTopBar && !inStem) continue;

                    Color finalCol = baseCol;

                    if (!isShadow && goldBevel.HasValue && darkRed.HasValue && lightGold.HasValue)
                    {
                        // Vertical red gradient
                        finalCol = Color.Lerp(darkRed.Value, baseCol, normY + 0.5f);

                        // Gold chrome outer bevel rim
                        bool isBevelEdge = (normY > 0.44f || shearedX > 0.40f || shearedX < -0.40f || (normY < -0.44f));
                        if (isBevelEdge)
                        {
                            finalCol = (normY > 0.42f) ? lightGold.Value : goldBevel.Value;
                        }
                    }

                    Color curr = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, BlendOver(curr, finalCol));
                }
            }
        }

        #endregion

        #region 7. Scratch Foil (Brushed titanium with elegant silver honeycomb hex grid)

        private static Texture2D RenderRealisticFoil(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color cDarkBase = new Color(0.14f, 0.16f, 0.20f, 1f);
            Color cLightBase = new Color(0.24f, 0.28f, 0.35f, 1f);
            Color cHexLine = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            Color cSheen = new Color(0.20f, 0.55f, 0.75f, 0.35f);

            float hexRadius = 28f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Fine horizontal brushed metal lines
                    float grain = Mathf.Sin(y * 8f) * 0.04f + Mathf.Sin(y * 2.3f + x * 0.1f) * 0.03f;
                    Color baseCol = Color.Lerp(cDarkBase, cLightBase, 0.5f + grain);

                    // Regular honeycomb hexagonal grid
                    float hx = x / hexRadius;
                    float hy = y / (hexRadius * 0.866025f);
                    if (Mathf.Repeat(hy, 2f) > 1f) hx += 0.5f;

                    float fX = Mathf.Abs(Mathf.Repeat(hx, 1f) - 0.5f);
                    float fY = Mathf.Abs(Mathf.Repeat(hy, 1f) - 0.5f);
                    float hexEdge = Mathf.Max(fX * 1.5f + fY, fY * 2f);

                    // Hex border groove
                    if (Mathf.Abs(hexEdge - 0.96f) < 0.06f)
                    {
                        baseCol = Color.Lerp(baseCol, cHexLine, 0.75f);
                    }

                    // Diagonal luxury metallic sheen stripe (repeats cleanly)
                    float diag = (x + y * 0.7f) / size;
                    float sheenWave = Mathf.Sin(diag * 4f * Mathf.PI);
                    if (sheenWave > 0.6f)
                    {
                        baseCol = Color.Lerp(baseCol, cSheen, (sheenWave - 0.6f) * 2f);
                    }

                    tex.SetPixel(x, y, baseCol);
                }
            }
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.Apply();
            return tex;
        }

        #endregion

        #region 8. UI Frames, Badges, and Buttons

        private static Texture2D RenderSoftBrush(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float r = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float norm = dist / r;
                    float alpha = (norm < 1.0f) ? (1.0f - (norm * norm * (3f - 2f * norm))) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        private static Texture2D RenderCellFrame(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Vector2 halfSize = new Vector2(size * 0.46f, size * 0.46f);

            Color cDarkBg = new Color(0.06f, 0.08f, 0.12f, 0.98f);
            Color cTitaniumRim = new Color(0.24f, 0.30f, 0.40f, 1f);
            Color cCyanCorner = new Color(0.12f, 0.85f, 1.0f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SDFRoundedBox(p, center, halfSize, 28f);
                    float alpha = AntiAlias(d);

                    if (alpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color col = cDarkBg;

                    // 4px outer titanium rim
                    if (d > -5f)
                    {
                        col = cTitaniumRim;
                    }
                    // Corner cyan accents
                    else if (d > -10f && (x < 48 || y < 48 || x > size - 48 || y > size - 48))
                    {
                        col = cCyanCorner;
                    }

                    tex.SetPixel(x, y, new Color(col.r, col.g, col.b, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D RenderRowBorder(int width, int height, Color ledColor, Color frameColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (float)x / width;
                    // Outer titanium edge
                    if (y <= 3 || y >= height - 4 || x <= 3 || x >= width - 4)
                    {
                        tex.SetPixel(x, y, frameColor * 1.6f);
                    }
                    // LED channel line along top and bottom
                    else if ((y >= 6 && y <= 11) || (y >= height - 12 && y <= height - 7))
                    {
                        float ledAlpha = Mathf.Sin(u * Mathf.PI);
                        tex.SetPixel(x, y, Color.Lerp(frameColor, ledColor, ledAlpha * 0.95f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, frameColor);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D RenderPrizeTag(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color tagBg = new Color(0.06f, 0.08f, 0.12f, 0.98f);
            Color tagBorder = new Color(0.18f, 0.70f, 0.92f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float bevelX = height * 0.32f;
                    if (x < bevelX && (Mathf.Abs(y - height * 0.5f) > (x / bevelX) * (height * 0.5f)))
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    if (x <= 4 || x >= width - 4 || y <= 4 || y >= height - 4)
                    {
                        tex.SetPixel(x, y, tagBorder);
                    }
                    else
                    {
                        tex.SetPixel(x, y, tagBg);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D RenderPillButton(int width, int height, Color topCol, Color bottomCol)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
            Vector2 halfSize = new Vector2(width * 0.48f, height * 0.46f);
            float radius = height * 0.45f;

            for (int y = 0; y < height; y++)
            {
                float v = (float)y / height;
                for (int x = 0; x < width; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = SDFRoundedBox(p, center, halfSize, radius);
                    float alpha = AntiAlias(d);

                    if (alpha <= 0f)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    Color grad = Color.Lerp(bottomCol, topCol, v);

                    // Top specular rim
                    if (d > -4f && v > 0.5f)
                    {
                        grad = Color.Lerp(grad, Color.white, 0.55f);
                    }
                    // Outer rim shadow
                    else if (d > -3f)
                    {
                        grad = Color.Lerp(grad, Color.black, 0.45f);
                    }

                    tex.SetPixel(x, y, new Color(grad.r, grad.g, grad.b, alpha));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D RenderSparkle(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            Color core = new Color(1f, 1f, 1f, 1f);
            Color glow = new Color(0.2f, 0.85f, 1.0f, 0.85f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Abs(x - center);
                    float dy = Mathf.Abs(y - center);

                    float star = Mathf.Pow(Mathf.Max(0.001f, 1.0f - dx / center), 4f) * Mathf.Pow(Mathf.Max(0.001f, 1.0f - dy / (center * 0.22f)), 2f)
                               + Mathf.Pow(Mathf.Max(0.001f, 1.0f - dy / center), 4f) * Mathf.Pow(Mathf.Max(0.001f, 1.0f - dx / (center * 0.22f)), 2f);

                    star = Mathf.Clamp01(star);
                    Color col = Color.Lerp(glow, core, star * star);
                    col.a = star;
                    tex.SetPixel(x, y, col);
                }
            }
            tex.Apply();
            return tex;
        }

        #endregion
    }
}
#endif
