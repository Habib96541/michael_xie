using UnityEngine;

namespace DropStack.Rendering
{
    public static class BlockSpriteGenerator
    {
        private static Sprite _cachedBlockSprite;
        private static Sprite _cachedGhostSprite;
        private static Sprite _cachedGridTileSprite;
        private static Sprite _cachedGlowParticleSprite;

        public static Sprite BlockSprite
        {
            get
            {
                if (_cachedBlockSprite == null)
                {
                    _cachedBlockSprite = GenerateBlockSprite(64);
                }
                return _cachedBlockSprite;
            }
        }

        public static Sprite GhostSprite
        {
            get
            {
                if (_cachedGhostSprite == null)
                {
                    _cachedGhostSprite = GenerateGhostSprite(64);
                }
                return _cachedGhostSprite;
            }
        }

        public static Sprite GridTileSprite
        {
            get
            {
                if (_cachedGridTileSprite == null)
                {
                    _cachedGridTileSprite = GenerateGridTileSprite(64);
                }
                return _cachedGridTileSprite;
            }
        }

        public static Sprite GlowParticleSprite
        {
            get
            {
                if (_cachedGlowParticleSprite == null)
                {
                    _cachedGlowParticleSprite = GenerateGlowParticleSprite(32);
                }
                return _cachedGlowParticleSprite;
            }
        }

        private static Sprite GenerateBlockSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float border = 3f;
            float innerBorder = 7f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;

                    // Distance from edges
                    float distLeft = x;
                    float distRight = size - 1 - x;
                    float distBottom = y;
                    float distTop = size - 1 - y;
                    float distEdge = Mathf.Min(Mathf.Min(distLeft, distRight), Mathf.Min(distBottom, distTop));

                    if (distEdge < 1f)
                    {
                        // Outer dark border edge
                        pixels[index] = new Color(0.25f, 0.25f, 0.25f, 1f);
                    }
                    else if (distEdge < border)
                    {
                        // Sub-border
                        pixels[index] = new Color(.62f, .62f, .62f, 1f);
                    }
                    else if (distEdge < innerBorder)
                    {
                        // Bevel shading: top & left are lighter, bottom & right are darker
                        bool isTopOrLeft = (distLeft <= distEdge + 0.5f) || (distTop <= distEdge + 0.5f);
                        if (isTopOrLeft)
                        {
                            pixels[index] = new Color(1f, 1f, 1f, 0.95f);
                        }
                        else
                        {
                            pixels[index] = new Color(0.55f, 0.55f, 0.65f, 0.95f);
                        }
                    }
                    else
                    {
                        // Center face: gentle subtle radial gloss
                        float nx = (float)x / size;
                        float ny = (float)y / size;
                        float gloss = Mathf.Clamp01(1.0f - Mathf.Abs(nx - 0.35f) * 0.4f - Mathf.Abs(ny - 0.65f) * 0.5f);
                        float val = Mathf.Lerp(.62f, .85f, gloss);
                        if (distEdge >= 10 && distEdge <= 12) val = .96f;
                        if (x >= 7 && x < 15 && y >= size-15 && y < size-7) val = 1.35f;
                        pixels[index] = new Color(val, val, val, 1.0f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite GenerateGhostSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float border = 4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float distEdge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));

                    if (distEdge < border)
                    {
                        // Glowing outline
                        pixels[index] = new Color(1f, 1f, 1f, 0.85f);
                    }
                    else
                    {
                        // Translucent glass interior
                        pixels[index] = new Color(1f, 1f, 1f, 0.12f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite GenerateGridTileSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    bool isBorder = (x < 2 || x >= size - 2 || y < 2 || y >= size - 2);
                    if (isBorder)
                    {
                        pixels[index] = new Color(.16f,.25f,.37f,1f);
                    }
                    else
                    {
                        pixels[index] = new Color(.035f,.055f,.105f,1f);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite GenerateGlowParticleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float maxRadius = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int index = y * size + x;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = Mathf.Clamp01(dist / maxRadius);
                    float alpha = Mathf.Pow(1.0f - t, 2.2f);
                    pixels[index] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
