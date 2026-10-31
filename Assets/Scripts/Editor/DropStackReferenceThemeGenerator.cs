using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace DropStack.Editor
{
    public static class DropStackReferenceThemeGenerator
    {
        private const string SpritesDir = "Assets/Sprites/Theme";

        [MenuItem("Drop Stack/Generate Reference Theme Sprites")]
        public static void GenerateThemeSprites()
        {
            if (!Directory.Exists(SpritesDir))
            {
                Directory.CreateDirectory(SpritesDir);
            }

            GeneratePillNavy();
            GenerateModalOuter();
            GenerateModalInner();
            GenerateButtonBlue();
            GenerateButtonOrange();
            GenerateButtonRedClose();
            GenerateToggleTrack();
            GenerateToggleKnob();
            GenerateIconGem();
            GenerateIconCrown();
            GenerateIconRestart();
            GenerateIconGear();
            GenerateDangerLine();
            GenerateBoardFrame();
            GenerateArcadeArt();
            File.WriteAllBytes(Path.Combine(SpritesDir,"block_gloss.png"), DropStack.Rendering.BlockSpriteGenerator.BlockSprite.texture.EncodeToPNG());

            AssetDatabase.Refresh();

            ConfigureSliced("Assets/Sprites/Theme/pill_navy.png", 16);
            ConfigureSliced("Assets/Sprites/Theme/modal_outer.png", 24);
            ConfigureSliced("Assets/Sprites/Theme/modal_inner.png", 20);
            ConfigureSliced("Assets/Sprites/Theme/btn_blue.png", 18);
            ConfigureSliced("Assets/Sprites/Theme/btn_orange.png", 18);
            ConfigureSliced("Assets/Sprites/Theme/board_frame.png", 28);
            ConfigureSliced("Assets/Sprites/Theme/toggle_track.png", 16);

            ConfigureDefault("Assets/Sprites/Theme/btn_red_close.png");
            ConfigureDefault("Assets/Sprites/Theme/toggle_knob.png");
            ConfigureDefault("Assets/Sprites/Theme/icon_gem.png");
            ConfigureDefault("Assets/Sprites/Theme/icon_crown.png");
            ConfigureDefault("Assets/Sprites/Theme/icon_restart.png");
            ConfigureDefault("Assets/Sprites/Theme/icon_gear.png");
            ConfigureDefault("Assets/Sprites/Theme/danger_line.png");
            ConfigureSliced("Assets/Sprites/Theme/panel_dark.png", 18);
            ConfigureDefault("Assets/Sprites/Theme/arcade_backdrop.png");
            ConfigureDefault("Assets/Sprites/Theme/block_gloss.png");

            AssetDatabase.Refresh();
            Debug.Log("[DropStack Theme] All Reference UI Sprites Successfully Generated!");
        }

        private static void GeneratePillNavy()
        {
            int w = 64, h = 48;
            Texture2D tex = CreateEmpty(w, h);
            Color fill = new Color(0.086f, 0.188f, 0.404f); // #163067
            Color border = new Color(0.18f, 0.32f, 0.62f); // #2E529E

            DrawRoundedRect(tex, 0, 0, w, h, 20f, fill, border, 2f);
            SaveTex(tex, "pill_navy.png");
        }

        private static void GenerateModalOuter()
        {
            int w = 80, h = 80;
            Texture2D tex = CreateEmpty(w, h);
            Color fill = new Color(0.114f, 0.231f, 0.478f); // #1D3B7A
            Color border = new Color(0.075f, 0.153f, 0.325f); // #132753

            DrawRoundedRect(tex, 0, 0, w, h, 24f, fill, border, 2.5f);
            SaveTex(tex, "modal_outer.png");
        }

        private static void GenerateModalInner()
        {
            int w = 64, h = 64;
            Texture2D tex = CreateEmpty(w, h);
            Color fill = new Color(0.322f, 0.545f, 0.910f); // #528BE8
            Color border = new Color(0.235f, 0.424f, 0.745f); // #3C6CBD

            DrawRoundedRect(tex, 0, 0, w, h, 18f, fill, border, 2f);
            SaveTex(tex, "modal_inner.png");
        }

        private static void GenerateButtonBlue()
        {
            int w = 64, h = 64;
            Texture2D tex = CreateEmpty(w, h);
            Color topFill = new Color(0.165f, 0.549f, 0.996f); // #2A8CFF
            Color botBevel = new Color(0.098f, 0.388f, 0.784f); // #1963C8
            Color border = new Color(0.075f, 0.298f, 0.612f);

            DrawBeveledButton(tex, w, h, 18f, topFill, botBevel, border);
            SaveTex(tex, "btn_blue.png");
        }

        private static void GenerateButtonOrange()
        {
            int w = 64, h = 64;
            Texture2D tex = CreateEmpty(w, h);
            Color topFill = new Color(1.0f, 0.635f, 0.071f); // #FFA212
            Color botBevel = new Color(0.824f, 0.455f, 0.0f); // #D27400
            Color border = new Color(0.655f, 0.345f, 0.0f);

            DrawBeveledButton(tex, w, h, 18f, topFill, botBevel, border);
            SaveTex(tex, "btn_orange.png");
        }

        private static void GenerateButtonRedClose()
        {
            int size = 56;
            Texture2D tex = CreateEmpty(size, size);
            Color topFill = new Color(0.922f, 0.165f, 0.180f); // #EB2A2E
            Color botBevel = new Color(0.678f, 0.078f, 0.098f); // #AD1419
            Color border = new Color(0.490f, 0.047f, 0.063f);

            DrawBeveledButton(tex, size, size, 16f, topFill, botBevel, border);

            // Draw white bold X
            Color white = Color.white;
            float cx = size * 0.5f;
            float cy = size * 0.5f + 1f;
            float arm = 11f;
            float thick = 3.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;

                    float dist1 = Mathf.Abs(dx - dy) / 1.4142f;
                    float dist2 = Mathf.Abs(dx + dy) / 1.4142f;
                    float len1 = Mathf.Abs(dx + dy) / 1.4142f;
                    float len2 = Mathf.Abs(dx - dy) / 1.4142f;

                    if ((dist1 < thick && len1 < arm) || (dist2 < thick && len2 < arm))
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }

            tex.Apply();
            SaveTex(tex, "btn_red_close.png");
        }

        private static void GenerateToggleTrack()
        {
            int w = 64, h = 32;
            Texture2D tex = CreateEmpty(w, h);
            Color fill = new Color(0.125f, 0.231f, 0.463f); // #203B76
            Color border = new Color(0.082f, 0.157f, 0.325f);

            DrawRoundedRect(tex, 0, 0, w, h, 14f, fill, border, 1.5f);
            SaveTex(tex, "toggle_track.png");
        }

        private static void GenerateToggleKnob()
        {
            int size = 32;
            Texture2D tex = CreateEmpty(size, size);
            Color topFill = new Color(1.0f, 0.655f, 0.078f); // #FFA714
            Color botBevel = new Color(0.855f, 0.498f, 0.0f);
            Color border = new Color(0.68f, 0.38f, 0.0f);

            DrawBeveledButton(tex, size, size, 10f, topFill, botBevel, border);
            SaveTex(tex, "toggle_knob.png");
        }

        private static void GenerateIconGem()
        {
            int size = 48;
            Texture2D tex = CreateEmpty(size, size);
            Color brightGreen = new Color(0.161f, 0.898f, 0.443f); // #29E571
            Color darkGreen = new Color(0.078f, 0.627f, 0.298f);
            Color highlight = new Color(0.659f, 0.984f, 0.780f);

            float cx = size * 0.5f;
            float cy = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / 18f;
                    float dy = (y - cy) / 18f;

                    // Diamond / facet shape
                    float diamondDist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (diamondDist <= 1.0f)
                    {
                        if (dy > 0.1f && Mathf.Abs(dx) < 0.45f)
                            tex.SetPixel(x, y, highlight);
                        else if (dx > 0)
                            tex.SetPixel(x, y, darkGreen);
                        else
                            tex.SetPixel(x, y, brightGreen);
                    }
                }
            }

            tex.Apply();
            SaveTex(tex, "icon_gem.png");
        }

        private static void GenerateIconCrown()
        {
            int size = 48;
            Texture2D tex = CreateEmpty(size, size);
            Color gold = new Color(1.0f, 0.773f, 0.078f); // #FFC514
            Color goldDark = new Color(0.863f, 0.584f, 0.024f);
            Color highlight = new Color(1.0f, 0.933f, 0.533f);

            float cx = size * 0.5f;
            float cy = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx);
                    float dy = (y - cy);

                    // Crown base and 3 spikes
                    bool inBase = (dy >= -10 && dy <= -2 && Mathf.Abs(dx) <= 16);
                    bool inCenterSpike = (dy > -2 && dy <= 12 && Mathf.Abs(dx) <= (12 - dy) * 0.7f);
                    bool inLeftSpike = (dy > -2 && dy <= 10 && Mathf.Abs(dx + 12) <= (10 - dy) * 0.7f);
                    bool inRightSpike = (dy > -2 && dy <= 10 && Mathf.Abs(dx - 12) <= (10 - dy) * 0.7f);

                    if (inBase || inCenterSpike || inLeftSpike || inRightSpike)
                    {
                        if (dy > 5) tex.SetPixel(x, y, highlight);
                        else if (dx > 4) tex.SetPixel(x, y, goldDark);
                        else tex.SetPixel(x, y, gold);
                    }
                }
            }

            tex.Apply();
            SaveTex(tex, "icon_crown.png");
        }

        private static void GenerateIconRestart()
        {
            int size = 48;
            Texture2D tex = CreateEmpty(size, size);
            Color white = Color.white;
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float rOuter = 16f;
            float rInner = 10f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float angle = Mathf.Atan2(y - cy, x - cx) * Mathf.Rad2Deg;
                    if (angle < 0) angle += 360f;

                    // Arc from 45 deg to 315 deg
                    if (dist >= rInner && dist <= rOuter && (angle >= 40f && angle <= 320f))
                    {
                        tex.SetPixel(x, y, white);
                    }

                    // Arrowhead at top (around 45 deg)
                    if (x >= 28 && x <= 40 && y >= 30 && y <= 42)
                    {
                        float adx = x - 35f;
                        float ady = y - 36f;
                        if (Mathf.Abs(adx) + Mathf.Abs(ady) <= 6f)
                        {
                            tex.SetPixel(x, y, white);
                        }
                    }
                }
            }

            tex.Apply();
            SaveTex(tex, "icon_restart.png");
        }

        private static void GenerateIconGear()
        {
            int size = 48;
            Texture2D tex = CreateEmpty(size, size);
            Color white = Color.white;
            float cx = size * 0.5f;
            float cy = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    float angle = Mathf.Atan2(y - cy, x - cx);

                    // 6 cogs
                    float cogMod = Mathf.Cos(angle * 6f);
                    float outerR = 14f + cogMod * 3.5f;

                    if (dist >= 6f && dist <= outerR)
                    {
                        tex.SetPixel(x, y, white);
                    }
                }
            }

            tex.Apply();
            SaveTex(tex, "icon_gear.png");
        }

        private static void GenerateDangerLine()
        {
            int w = 256, h = 8;
            Texture2D tex = CreateEmpty(w, h);
            Color red = new Color(0.95f, 0.12f, 0.15f, 1f); // #F21E26
            Color redGlow = new Color(0.95f, 0.12f, 0.15f, 0.4f);

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (y >= 2 && y <= 5) tex.SetPixel(x, y, red);
                    else tex.SetPixel(x, y, redGlow);
                }
            }

            tex.Apply();
            SaveTex(tex, "danger_line.png");
        }

        private static void GenerateBoardFrame()
        {
            int w = 96, h = 96;
            Texture2D tex = CreateEmpty(w, h);
            Color bg = new Color(0.043f, 0.098f, 0.220f, 0.98f); // #0B1938
            Color border = new Color(0.129f, 0.247f, 0.490f, 1.0f); // #213F7D

            DrawRoundedRect(tex, 0, 0, w, h, 28f, bg, border, 3.5f);
            SaveTex(tex, "board_frame.png");
        }

        private static void GenerateArcadeArt()
        {
            var panel = CreateEmpty(64, 64);
            DrawRoundedRect(panel, 0, 0, 64, 64, 18, new Color(.025f,.09f,.15f,.94f), new Color(.13f,.31f,.39f,.9f), 1f);
            SaveTex(panel, "panel_dark.png");
            const int w = 1200, h = 900;
            var tex = CreateEmpty(w, h);
            var pixels = new Color[w*h];
            for (int y=0; y<h; y++) for (int x=0; x<w; x++) {
                float nx=x/(float)w, ny=y/(float)h;
                float glow=Mathf.Exp(-((nx-.50f)*(nx-.50f)*7+(ny-.48f)*(ny-.48f)*3));
                Color c=Color.Lerp(new Color(.012f,.025f,.045f),new Color(.035f,.10f,.15f),glow*.85f);
                if (x%48==0 || y%48==0) c=Color.Lerp(c,new Color(.08f,.19f,.25f),.17f);
                int bx=x/96, by=y/96; float seed=Mathf.Abs(Mathf.Sin(bx*127.1f+by*311.7f));
                int dx=x%96,dy=y%96;
                if(seed>.82f && dx>10 && dx<84 && dy>10 && dy<84) {
                    bool edge=dx<12||dx>81||dy<12||dy>81;
                    c=Color.Lerp(c,new Color(.06f,.23f,.3f),edge?.35f:.05f);
                }
                pixels[y*w+x]=c;
            }
            tex.SetPixels(pixels); tex.Apply(); SaveTex(tex,"arcade_backdrop.png");
        }

        #region Helpers

        private static Texture2D CreateEmpty(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void DrawRoundedRect(Texture2D tex, int x0, int y0, int w, int h, float radius, Color fill, Color border, float borderWidth)
        {
            for (int y = y0; y < y0 + h; y++)
            {
                for (int x = x0; x < x0 + w; x++)
                {
                    float dist = GetRoundedBoxDist(x - x0, y - y0, w, h, radius);
                    if (dist <= 0f)
                    {
                        if (dist >= -borderWidth)
                        {
                            tex.SetPixel(x, y, border);
                        }
                        else
                        {
                            tex.SetPixel(x, y, fill);
                        }
                    }
                    else if (dist < 1.0f)
                    {
                        Color c = border;
                        c.a = 1.0f - dist;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
        }

        private static void DrawBeveledButton(Texture2D tex, int w, int h, float radius, Color topFill, Color botBevel, Color border)
        {
            float bevelHeight = h * 0.2f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = GetRoundedBoxDist(x, y, w, h, radius);
                    if (dist <= 0f)
                    {
                        if (dist >= -2.0f)
                        {
                            tex.SetPixel(x, y, border);
                        }
                        else
                        {
                            // Bevel transition
                            if (y < bevelHeight)
                            {
                                tex.SetPixel(x, y, botBevel);
                            }
                            else
                            {
                                tex.SetPixel(x, y, topFill);
                            }
                        }
                    }
                    else if (dist < 1.0f)
                    {
                        Color c = border;
                        c.a = 1.0f - dist;
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
        }

        private static float GetRoundedBoxDist(float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Abs(x - (w - 1) * 0.5f) - ((w - 1) * 0.5f - r);
            float qy = Mathf.Abs(y - (h - 1) * 0.5f) - ((h - 1) * 0.5f - r);
            return Vector2.Distance(Vector2.Max(new Vector2(qx, qy), Vector2.zero), Vector2.zero)
                 + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static void SaveTex(Texture2D tex, string filename)
        {
            string path = Path.Combine(SpritesDir, filename);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        private static void ConfigureSliced(string path, int border)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.spriteBorder = new Vector4(border, border, border, border);
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureDefault(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings); settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.spritePixelsPerUnit = 64;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        #endregion
    }
}
