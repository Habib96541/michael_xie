using UnityEngine;

namespace BottleFlip
{
    /// <summary>Game palette. Every colour in the game comes from here.</summary>
    public static class Pal
    {
        public static readonly Color Ink = Hex("0A0A1C");
        public static readonly Color InkDeep = Hex("05050F");
        public static readonly Color Panel = Hex("14163A");
        public static readonly Color Text = Hex("FFFFFF");
        public static readonly Color Muted = Hex("9C99C4");
        public static readonly Color Amber = Hex("FFC64B");
        public static readonly Color AmberDeep = Hex("FF9E2C");
        public static readonly Color AmberSoft = Hex("FFE0A0");
        public static readonly Color Coral = Hex("FF5E5B");
        public static readonly Color Teal = Hex("33E0C0");
        public static readonly Color OnAmber = Hex("241503");

        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out var c)) return c;
            return Color.magenta;
        }

        public static Color Alpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    /// <summary>Runtime-generated textures, sprites and materials (no external asset dependencies).</summary>
    public static class Art
    {
        static Texture2D _softCircle;
        static Texture2D _glow;
        static Texture2D _vignette;
        static Texture2D _blob;
        static Texture2D _label;
        static Texture2D _square;
        static Texture2D _confetti;
        static Texture2D _radialStage;

        static Texture2D NewTex(int w, int h, string name)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2
            };
            return t;
        }

        /// <summary>White texture with a soft radial alpha falloff.</summary>
        public static Texture2D SoftCircle
        {
            get
            {
                if (_softCircle != null) return _softCircle;
                const int s = 128;
                var t = NewTex(s, s, "SoftCircle");
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f;
                    float dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _softCircle = t;
                return t;
            }
        }

        /// <summary>Additive-friendly glow: white core fading to black (RGB carries the energy).</summary>
        public static Texture2D Glow
        {
            get
            {
                if (_glow != null) return _glow;
                const int s = 128;
                var t = NewTex(s, s, "Glow");
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f;
                    float dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float k = Mathf.Pow(1f - d, 2.4f);
                    px[y * s + x] = new Color(k, k, k, 1f);
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _glow = t;
                return t;
            }
        }

        /// <summary>Dark soft blob used for contact shadows and the panel vignette.</summary>
        public static Texture2D Blob
        {
            get
            {
                if (_blob != null) return _blob;
                const int s = 128;
                var t = NewTex(s, s, "Blob");
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f;
                    float dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = Mathf.Pow(1f - d, 1.8f);
                    px[y * s + x] = new Color(0f, 0f, 0f, a);
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _blob = t;
                return t;
            }
        }

        /// <summary>Screen vignette: transparent centre, dark edges.</summary>
        public static Texture2D Vignette
        {
            get
            {
                if (_vignette != null) return _vignette;
                const int s = 256;
                var t = NewTex(s, s, "Vignette");
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f;
                    float dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx * 0.82f + dy * dy * 0.6f));
                    float a = Mathf.SmoothStep(0f, 0.92f, Mathf.Max(0f, d - 0.45f));
                    px[y * s + x] = new Color(0f, 0f, 0f, a);
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _vignette = t;
                return t;
            }
        }

        public static Texture2D White
        {
            get
            {
                if (_square != null) return _square;
                var t = NewTex(4, 4, "White");
                var px = new Color[16];
                for (int i = 0; i < 16; i++) px[i] = Color.white;
                t.SetPixels(px);
                t.Apply(false, false);
                _square = t;
                return t;
            }
        }

        /// <summary>Vertical gradient (bottom -> top) used for the backdrop and UI gradients.</summary>
        public static Texture2D Gradient(Color bottom, Color top, int h = 256)
        {
            var t = NewTex(4, h, "Gradient");
            var px = new Color[4 * h];
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bottom, top, y / (float)(h - 1));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            t.SetPixels(px);
            t.Apply(false, false);
            return t;
        }

        /// <summary>Warm pool of light used behind the stage.</summary>
        public static Texture2D StagePool
        {
            get
            {
                if (_radialStage != null) return _radialStage;
                const int s = 256;
                var t = NewTex(s, s, "StagePool");
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x + 0.5f) / s * 2f - 1f;
                    float dy = (y + 0.5f) / s * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _radialStage = t;
                return t;
            }
        }

        public static Texture2D Confetti
        {
            get
            {
                if (_confetti != null) return _confetti;
                const int s = 32;
                var t = NewTex(s, s, "Confetti");
                var px = new Color[s * s];
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Abs((x + 0.5f) / s * 2f - 1f);
                    float dy = Mathf.Abs((y + 0.5f) / s * 2f - 1f);
                    float m = Mathf.Max(dx, dy);
                    float a = Mathf.Clamp01((0.86f - m) * 30f);
                    px[y * s + x] = new Color(1f, 1f, 1f, a);
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _confetti = t;
                return t;
            }
        }

        /// <summary>Bottle label art, wraps around the bottle body.</summary>
        public static Texture2D Label
        {
            get
            {
                if (_label != null) return _label;
                const int s = 256;
                var t = NewTex(s, s, "Label");
                var px = new Color[s * s];
                Color cream = Pal.Hex("FFF6E8");
                Color coral = Pal.Coral;
                Color deep = Pal.Hex("B3352F");
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float u = x / (float)(s - 1);
                    float v = y / (float)(s - 1);
                    Color c = coral;

                    // Top and bottom trim.
                    if (v < 0.11f || v > 0.89f) c = cream;
                    else if (v < 0.17f || v > 0.83f) c = deep;

                    // Bold diagonal stripes in the middle band.
                    if (v > 0.38f && v < 0.62f)
                    {
                        float stripe = Mathf.Repeat(u * 3f + (v - 0.5f) * 0.5f, 1f);
                        if (stripe < 0.24f) c = cream;
                    }

                    // Round emblem dots.
                    float2 d1 = new float2(Mathf.Repeat(u * 2f, 1f) - 0.5f, v - 0.27f);
                    float2 d2 = new float2(Mathf.Repeat(u * 2f + 0.5f, 1f) - 0.5f, v - 0.73f);
                    if (d1.Length() < 0.075f || d2.Length() < 0.075f) c = cream;

                    px[y * s + x] = c;
                }
                t.SetPixels(px);
                t.Apply(false, false);
                _label = t;
                return t;
            }
        }

        struct float2
        {
            public float x, y;
            public float2(float x, float y) { this.x = x; this.y = y; }
            public float Length() => Mathf.Sqrt(x * x + y * y);
        }

        // ---------- UI sprites ----------

        static float RoundedSdf(float px, float py, float w, float h, float r)
        {
            float dx = Mathf.Abs(px - w * 0.5f) - (w * 0.5f - r);
            float dy = Mathf.Abs(py - h * 0.5f) - (h * 0.5f - r);
            float ax = Mathf.Max(dx, 0f), ay = Mathf.Max(dy, 0f);
            return Mathf.Sqrt(ax * ax + ay * ay) + Mathf.Min(Mathf.Max(dx, dy), 0f) - r;
        }

        /// <summary>
        /// 9-sliced rounded rectangle sprite. PixelsPerUnit = 1 so the border (in texels)
        /// maps directly to canvas units, keeping corners circular at any size.
        /// </summary>
        public static Sprite RoundedSprite(int radius, Color bottom, Color top, int stroke = 0, Color? strokeColor = null)
        {
            radius = Mathf.Max(1, radius);
            int pad = 2;
            int size = radius * 2 + pad * 2;
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "Round" + radius
            };
            var px = new Color[size * size];
            Color sCol = strokeColor ?? Color.white;
            float r = radius;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float cx = x + 0.5f;
                float cy = y + 0.5f;
                float d = RoundedSdf(cx, cy, size, size, r);
                float aFill = Mathf.Clamp01(0.5f - d);
                float aInner = Mathf.Clamp01(0.5f - (d + stroke));
                float aRing = Mathf.Clamp01(aFill - aInner);
                float g = Mathf.Clamp01(y / (float)(size - 1));
                Color fill = Color.Lerp(bottom, top, g);
                Color c = Color.Lerp(fill, sCol, stroke > 0 ? aRing : 0f);
                c.a = aFill;
                if (stroke > 0) c.a = Mathf.Max(aFill, aRing);
                px[y * size + x] = c;
            }
            t.SetPixels(px);
            t.Apply(false, false);
            var border = new Vector4(radius + pad, radius + pad, radius + pad, radius + pad);
            var sprite = Sprite.Create(t, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = "RoundSprite" + radius;
            return sprite;
        }

        /// <summary>Soft circular haze used for glows behind buttons and taps.</summary>
        public static Sprite SoftSprite()
        {
            var t = SoftCircle;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite TextureSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // ---------- Materials ----------

        public static Material Glossy(Color color, float metallic, float smoothness, string name = "Glossy")
        {
            var sh = Shader.Find("Standard");
            var m = new Material(sh) { name = name };
            m.SetColor("_Color", color);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Glossiness", smoothness);
            return m;
        }

        public static Material Textured(Texture2D tex, Color tint, float smoothness = 0.2f)
        {
            var sh = Shader.Find("Standard");
            var m = new Material(sh) { name = "Textured" };
            m.SetColor("_Color", tint);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", 0f);
            m.mainTexture = tex;
            return m;
        }

        public static Material Emissive(Color color, Texture2D tex = null)
        {
            var sh = Shader.Find("Standard");
            var m = new Material(sh) { name = "Emissive" };
            m.SetColor("_Color", Color.white);
            m.SetFloat("_Glossiness", 0.9f);
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (tex != null) m.mainTexture = tex;
            return m;
        }

        /// <summary>Transparent, lighting-independent material (decals, backdrops, shadows).</summary>
        public static Material Unlit(Texture tex, Color tint, bool additive = false, string name = "Unlit")
        {
            var sh = Shader.Find(additive ? "BottleFlip/Additive" : "Sprites/Default");
            var m = new Material(sh) { name = name };
            m.mainTexture = tex;
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", tint);
            m.renderQueue = additive ? 3100 : 3000;
            return m;
        }

        public static Mesh QuadMesh(float size = 1f, string name = "Quad")
        {
            float h = size * 0.5f;
            var mesh = new Mesh { name = name };
            mesh.vertices = new[]
            {
                new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f),
                new Vector3(-h, h, 0f), new Vector3(h, h, 0f)
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.normals = new[] { -Vector3.forward, -Vector3.forward, -Vector3.forward, -Vector3.forward };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
