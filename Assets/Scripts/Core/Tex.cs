using System;
using System.Collections.Generic;
using UnityEngine;

namespace Crash
{
    /// Procedurally generated sprites - the whole game ships without image assets.
    public static class Tex
    {
        static readonly Dictionary<string, Sprite> Cache_ = new Dictionary<string, Sprite>();

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.filterMode = FilterMode.Bilinear;
            t.wrapMode = TextureWrapMode.Clamp;
            t.hideFlags = HideFlags.HideAndDontSave;
            return t;
        }

        static Sprite Store(string key, Texture2D tex, Vector4 border)
        {
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, border);
            sp.hideFlags = HideFlags.HideAndDontSave;
            Cache_[key] = sp;
            return sp;
        }

        static Sprite Get(string key, Func<Sprite> make)
        {
            if (Cache_.TryGetValue(key, out var s) && s != null) return s;
            return make();
        }

        /// Rounded rectangle, 9-sliced so it can be any size.
        public static Sprite Round(int size = 96, int radius = 28)
        {
            return Get("rr" + size + "_" + radius, () =>
            {
                var tex = NewTex(size, size);
                var px = new Color32[size * size];
                float half = size * 0.5f;
                float r = radius;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Abs(x + 0.5f - half) - (half - r);
                        float dy = Mathf.Abs(y + 0.5f - half) - (half - r);
                        float ox = Mathf.Max(dx, 0f), oy = Mathf.Max(dy, 0f);
                        float d = Mathf.Min(Mathf.Max(dx, dy), 0f) + Mathf.Sqrt(ox * ox + oy * oy) - r;
                        float a = Mathf.Clamp01(0.5f - d);
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                float b = radius + 2f;
                return Store("rr" + size + "_" + radius, tex, new Vector4(b, b, b, b));
            });
        }

        public static Sprite Circle(int size = 64)
        {
            return Get("ci" + size, () =>
            {
                var tex = NewTex(size, size);
                var px = new Color32[size * size];
                float half = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - half, dy = y + 0.5f - half;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) - (half - 0.5f);
                        px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                    }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                return Store("ci" + size, tex, Vector4.zero);
            });
        }

        /// Soft radial glow used for halos, glows and flashes.
        public static Sprite Glow(int size = 128)
        {
            return Get("gl" + size, () =>
            {
                var tex = NewTex(size, size);
                var px = new Color32[size * size];
                float half = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                return Store("gl" + size, tex, Vector4.zero);
            });
        }

        public static Sprite Ring(int size = 256, int thickness = 20)
        {
            return Get("ri" + size + "_" + thickness, () =>
            {
                var tex = NewTex(size, size);
                var px = new Color32[size * size];
                float half = size * 0.5f;
                float rad = half - thickness * 0.5f - 1f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - half, dy = y + 0.5f - half;
                        float d = Mathf.Abs(Mathf.Sqrt(dx * dx + dy * dy) - rad) - thickness * 0.5f;
                        px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d));
                    }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                return Store("ri" + size + "_" + thickness, tex, Vector4.zero);
            });
        }

        public static Sprite Vignette(int size = 256)
        {
            return Get("vg" + size, () =>
            {
                var tex = NewTex(size, size);
                var px = new Color32[size * size];
                float half = size * 0.5f;
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01(Mathf.InverseLerp(0.55f, 1.28f, d)) * 0.85f;
                        px[y * size + x] = new Color(0f, 0f, 0f, a);
                    }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                return Store("vg" + size, tex, Vector4.zero);
            });
        }

        /// Vertical white ramp (opaque at top, transparent at bottom) - tint per use.
        public static Sprite Fade(int h = 128)
        {
            return Get("fd" + h, () =>
            {
                var tex = NewTex(4, h);
                var px = new Color[4 * h];
                for (int y = 0; y < h; y++)
                {
                    float a = Mathf.Pow(y / (float)(h - 1), 1.35f);
                    for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color(1f, 1f, 1f, a);
                }
                tex.SetPixels(px);
                tex.Apply(false, false);
                return Store("fd" + h, tex, Vector4.zero);
            });
        }

        // ---------- shape helpers ----------

        public static bool InPoly(Vector2[] p, Vector2 pt)
        {
            bool inside = false;
            int n = p.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((p[i].y > pt.y) != (p[j].y > pt.y)) &&
                    (pt.x < (p[j].x - p[i].x) * (pt.y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                    inside = !inside;
            }
            return inside;
        }

        static Texture2D Masked(int w, int h, Func<Vector2, Color> f, int ss = 3)
        {
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            float inv = 1f / (ss * ss);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float r = 0f, g = 0f, b = 0f, a = 0f;
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            var pt = new Vector2((x + (sx + 0.5f) / ss) / w, (y + (sy + 0.5f) / ss) / h);
                            Color c = f(pt);
                            r += c.r * c.a;
                            g += c.g * c.a;
                            b += c.b * c.a;
                            a += c.a;
                        }
                    a *= inv;
                    if (a > 0.002f)
                        px[y * w + x] = new Color(r * inv / a, g * inv / a, b * inv / a, a);
                    else
                        px[y * w + x] = new Color(0f, 0f, 0f, 0f);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        /// Stylised rocket, nose up. White-ish body so it can be tinted.
        public static Sprite Rocket(int w = 168, int h = 360)
        {
            return Get("rk", () =>
            {
                var body = new[]
                {
                    new Vector2(0.5f, 0.985f), new Vector2(0.632f, 0.70f), new Vector2(0.648f, 0.245f),
                    new Vector2(0.578f, 0.135f), new Vector2(0.422f, 0.135f), new Vector2(0.352f, 0.245f),
                    new Vector2(0.368f, 0.70f)
                };
                var finL = new[] { new Vector2(0.40f, 0.295f), new Vector2(0.225f, 0.06f), new Vector2(0.40f, 0.085f) };
                var finR = new[] { new Vector2(0.60f, 0.295f), new Vector2(0.775f, 0.06f), new Vector2(0.60f, 0.085f) };
                var noz = new[]
                {
                    new Vector2(0.425f, 0.155f), new Vector2(0.575f, 0.155f),
                    new Vector2(0.615f, 0.035f), new Vector2(0.385f, 0.035f)
                };
                var winC = new Vector2(0.5f, 0.60f);
                const float winR = 0.088f;

                Color steel = Pal.Hex("#DDE6FF");
                Color steelLo = Pal.Hex("#8C9BE0");
                Color fin = Pal.Hex("#6C5BFF");
                Color nozzle = Pal.Hex("#2B3060");
                Color glass = Pal.Hex("#15204A");
                Color accent = Pal.Hex("#39EBD2");

                return Store("rk", Masked(w, h, p =>
                {
                    if (Tex.InPoly(finL, p) || Tex.InPoly(finR, p))
                        return fin;
                    if (Tex.InPoly(noz, p))
                        return nozzle;
                    if (!Tex.InPoly(body, p)) return new Color(0f, 0f, 0f, 0f);

                    // body shading + accents
                    float shade = Mathf.Clamp01((p.x - 0.33f) / 0.34f);
                    Color c = Color.Lerp(steel, steelLo, shade * 0.85f);
                    if (p.y > 0.235f && p.y < 0.275f) c = Color.Lerp(c, accent, 0.85f);
                    if (p.y > 0.845f) c = accent;

                    Vector2 d = p - winC;
                    float dist = d.magnitude;
                    if (dist < winR + 0.016f) c = Color.Lerp(Pal.Hex("#7FE9FF"), glass, Mathf.InverseLerp(winR, winR + 0.016f, dist));
                    if (dist < winR * 0.42f) c = Color.Lerp(c, new Color(1f, 1f, 1f, 1f), 0.45f);
                    return c;
                }), Vector4.zero);
            });
        }

        /// Exhaust flame, wide at the top, pointed at the bottom.
        public static Sprite Flame(int w = 128, int h = 260)
        {
            return Get("fl", () =>
            {
                return Store("fl", Masked(w, h, p =>
                {
                    float v = 1f - p.y; // 0 at nozzle, 1 at tip
                    float half = 0.40f * Mathf.Pow(1f - v, 0.72f) * (1f + 0.16f * Mathf.Sin(v * 17f));
                    float dx = Mathf.Abs(p.x - 0.5f);
                    if (dx > half) return new Color(0f, 0f, 0f, 0f);
                    float core = 1f - Mathf.Clamp01(dx / Mathf.Max(0.001f, half));
                    Color hot = Color.white;
                    Color mid = Pal.Hex("#FFC247");
                    Color tail = Pal.Hex("#FF5E3A");
                    Color c = Color.Lerp(tail, mid, core);
                    c = Color.Lerp(c, hot, Mathf.Pow(core, 3.2f) * (1f - v * 0.55f));
                    c.a = Mathf.Pow(1f - v, 0.9f);
                    return c;
                }), Vector4.zero);
            });
        }

        /// Shield icon used for the lives row.
        public static Sprite Shield(int size = 128, Color tint = default(Color))
        {
            return Get("sh" + size, () =>
            {
                var poly = new[]
                {
                    new Vector2(0.5f, 0.985f), new Vector2(0.93f, 0.80f), new Vector2(0.86f, 0.34f),
                    new Vector2(0.5f, 0.02f), new Vector2(0.14f, 0.34f), new Vector2(0.07f, 0.80f)
                };
                var inner = new[]
                {
                    new Vector2(0.5f, 0.86f), new Vector2(0.82f, 0.71f), new Vector2(0.76f, 0.37f),
                    new Vector2(0.5f, 0.16f), new Vector2(0.24f, 0.37f), new Vector2(0.18f, 0.71f)
                };
                Color edge = Pal.Hex("#39EBD2");
                Color fill = Pal.Hex("#1B2B54");
                Color shine = Pal.Hex("#F5F7FF");
                return Store("sh" + size, Masked(size, size, p =>
                {
                    if (!Tex.InPoly(poly, p)) return new Color(0f, 0f, 0f, 0f);
                    Color c = Tex.InPoly(inner, p) ? fill : edge;
                    if (p.y > 0.62f && p.x > 0.42f && p.x < 0.5f + (0.95f - p.y) * 0.25f && p.x < 0.56f)
                        c = Color.Lerp(c, shine, 0.20f);
                    return c;
                }), Vector4.zero);
            });
        }
    }
}
