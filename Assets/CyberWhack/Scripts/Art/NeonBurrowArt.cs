using UnityEngine;

namespace CyberWhack.Art
{
    /// <summary>
    /// Small, runtime-generated art set for the Moonlit Burrow theme.
    /// It keeps the project self-contained while giving the game a clearly
    /// different visual identity from the original warm cartoon treatment.
    /// </summary>
    public static class NeonBurrowArt
    {
        public static readonly Color NightInk = new Color32(8, 11, 29, 255);
        public static readonly Color NightBlue = new Color32(14, 22, 55, 255);
        public static readonly Color PanelBlue = new Color32(15, 24, 48, 245);
        public static readonly Color PanelBlueLight = new Color32(26, 40, 75, 255);
        public static readonly Color Cyan = new Color32(53, 232, 225, 255);
        public static readonly Color CyanSoft = new Color32(123, 255, 238, 255);
        public static readonly Color Violet = new Color32(157, 122, 255, 255);
        public static readonly Color Amber = new Color32(255, 193, 83, 255);
        public static readonly Color Danger = new Color32(255, 91, 125, 255);
        public static readonly Color Text = new Color32(237, 246, 255, 255);
        public static readonly Color MutedText = new Color32(146, 170, 204, 255);

        private static Sprite _background;
        private static Sprite _audioOn;
        private static Sprite _audioOff;
        private static Sprite _multiplierBadge;
        private static Sprite _energyFill;

        public static Sprite Background
        {
            get
            {
                if (_background == null) _background = CreateNightGardenBackground(512, 1024);
                return _background;
            }
        }

        public static Sprite AudioOn
        {
            get
            {
                if (_audioOn == null) _audioOn = CreateAudioIcon(true, 128);
                return _audioOn;
            }
        }

        public static Sprite AudioOff
        {
            get
            {
                if (_audioOff == null) _audioOff = CreateAudioIcon(false, 128);
                return _audioOff;
            }
        }

        public static Sprite MultiplierBadge
        {
            get
            {
                if (_multiplierBadge == null) _multiplierBadge = CreateMultiplierBadge(192);
                return _multiplierBadge;
            }
        }

        public static Sprite EnergyFill
        {
            get
            {
                if (_energyFill == null) _energyFill = CreateEnergyFill(256, 64);
                return _energyFill;
            }
        }

        public static Sprite CreateNightGardenBackground(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] pixels = new Color[width * height];

            Vector2 moon = new Vector2(width * 0.79f, height * 0.82f);
            for (int y = 0; y < height; y++)
            {
                float vertical = y / (float)(height - 1);
                Color rowColor = Color.Lerp(NightInk, NightBlue, vertical);
                for (int x = 0; x < width; x++)
                {
                    float dx = (x - moon.x) / width;
                    float dy = (y - moon.y) / height;
                    float glow = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 5.5f);
                    pixels[y * width + x] = Color.Lerp(rowColor, new Color(0.12f, 0.22f, 0.34f, 1f), glow * 0.28f);
                }
            }

            // Moon and halo.
            Circle(pixels, width, height, moon.x, moon.y, width * 0.105f, new Color(0.78f, 0.91f, 1f, 0.92f));
            Circle(pixels, width, height, moon.x - width * 0.025f, moon.y + height * 0.012f, width * 0.082f, new Color(0.93f, 0.98f, 1f, 0.96f));
            Circle(pixels, width, height, moon.x + width * 0.05f, moon.y + height * 0.02f, width * 0.016f, new Color(0.64f, 0.79f, 0.92f, 0.4f));

            // Deterministic stars, so the backdrop never changes between runs.
            for (int i = 0; i < 44; i++)
            {
                int sx = (i * 83 + 29) % width;
                int sy = (i * 137 + 47) % (height - 230) + 120;
                float radius = (i % 7 == 0) ? 2.1f : 1.1f;
                Color starColor = (i % 5 == 0) ? new Color(0.48f, 1f, 0.94f, 0.9f) : new Color(0.74f, 0.84f, 1f, 0.68f);
                Circle(pixels, width, height, sx, sy, radius, starColor);
            }

            // Low rolling hills and a luminous underground garden floor.
            for (int x = 0; x < width; x++)
            {
                float normalizedX = x / (float)width;
                int horizon = Mathf.RoundToInt(height * (0.24f + Mathf.Sin(normalizedX * 6.1f) * 0.018f));
                for (int y = 0; y < horizon; y++)
                {
                    float depth = y / (float)Mathf.Max(1, horizon);
                    pixels[y * width + x] = Color.Lerp(new Color32(6, 12, 30, 255), new Color32(13, 31, 55, 255), depth);
                }

                // Thin bioluminescent contour lines give the play field depth.
                int contour = Mathf.RoundToInt(height * (0.22f + Mathf.Sin(normalizedX * 11f) * 0.012f));
                for (int offset = -1; offset <= 1; offset++)
                {
                    int py = contour + offset;
                    if (py >= 0 && py < height) pixels[py * width + x] = new Color(0.14f, 0.42f, 0.45f, 0.35f);
                }
            }

            // Tiny reeds along the bottom edge, framing the 3x3 play field.
            for (int i = 0; i < 18; i++)
            {
                float x = (i % 2 == 0) ? 20f + i * 7f : width - 24f - i * 6f;
                float baseY = 20f + (i % 4) * 7f;
                Line(pixels, width, height, x, baseY, x - 7f, baseY + 28f + (i % 3) * 4f, new Color(0.13f, 0.48f, 0.44f, 0.75f), 2f);
                Line(pixels, width, height, x, baseY, x + 8f, baseY + 22f, new Color(0.10f, 0.36f, 0.40f, 0.72f), 2f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateAudioIcon(bool enabled, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            Circle(pixels, size, size, center.x, center.y, size * 0.43f, new Color(0.04f, 0.09f, 0.18f, 0.98f));
            CircleOutline(pixels, size, size, center.x, center.y, size * 0.43f, size * 0.055f, enabled ? Cyan : Danger);

            Color iconColor = enabled ? CyanSoft : new Color(1f, 0.74f, 0.79f, 1f);
            float y = size * 0.5f;
            FillRect(pixels, size, size, size * 0.31f, y - size * 0.08f, size * 0.12f, size * 0.16f, iconColor);
            for (int i = 0; i < 5; i++)
            {
                float pointX = size * 0.28f + i * size * 0.022f;
                Line(pixels, size, size, pointX, y - size * 0.08f, size * 0.47f, y, iconColor, 2f);
                Line(pixels, size, size, pointX, y + size * 0.08f, size * 0.47f, y, iconColor, 2f);
            }

            if (enabled)
            {
                Line(pixels, size, size, size * 0.58f, y - size * 0.17f, size * 0.68f, y - size * 0.08f, iconColor, 4f);
                Line(pixels, size, size, size * 0.68f, y - size * 0.08f, size * 0.68f, y + size * 0.08f, iconColor, 4f);
                Line(pixels, size, size, size * 0.68f, y + size * 0.08f, size * 0.58f, y + size * 0.17f, iconColor, 4f);
            }
            else
            {
                Line(pixels, size, size, size * 0.57f, y - size * 0.2f, size * 0.78f, y + size * 0.2f, iconColor, 5f);
                Line(pixels, size, size, size * 0.78f, y - size * 0.2f, size * 0.57f, y + size * 0.2f, iconColor, 5f);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateMultiplierBadge(int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - size * 0.5f;
                    float dy = y - size * 0.5f;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distance > size * 0.45f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float light = Mathf.Clamp01(1f - (dx + dy) / size);
                        pixels[y * size + x] = Color.Lerp(new Color32(37, 46, 92, 255), new Color32(100, 73, 177, 255), light * 0.8f);
                    }
                }
            }
            CircleOutline(pixels, size, size, size * 0.5f, size * 0.5f, size * 0.43f, size * 0.025f, Cyan);
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateEnergyFill(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear
            };
            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float edge = Mathf.Min(x, width - 1 - x, y, height - 1 - y);
                    float alpha = Mathf.Clamp01(edge / 4f);
                    float t = x / (float)(width - 1);
                    pixels[y * width + x] = Color.Lerp(Cyan, Violet, t) * alpha;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void Circle(Color[] pixels, int width, int height, float cx, float cy, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius));
            float radiusSq = radius * radius;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= radiusSq) pixels[y * width + x] = color;
                }
            }
        }

        private static void CircleOutline(Color[] pixels, int width, int height, float cx, float cy, float radius, float thickness, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - thickness));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(cx + radius + thickness));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - thickness));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(cy + radius + thickness));
            float outer = (radius + thickness) * (radius + thickness);
            float inner = (radius - thickness) * (radius - thickness);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distanceSq = dx * dx + dy * dy;
                    if (distanceSq <= outer && distanceSq >= inner) pixels[y * width + x] = color;
                }
            }
        }

        private static void FillRect(Color[] pixels, int width, int height, float x, float y, float w, float h, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(x));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(x + w));
            int minY = Mathf.Max(0, Mathf.FloorToInt(y));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(y + h));
            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++) pixels[py * width + px] = color;
            }
        }

        private static void Line(Color[] pixels, int width, int height, float x0, float y0, float x1, float y1, Color color, float thickness)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1)) * 1.5f));
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Circle(pixels, width, height, Mathf.Lerp(x0, x1, t), Mathf.Lerp(y0, y1, t), thickness, color);
            }
        }
    }
}
