using System;
using UnityEngine;

namespace CyberWhack.VFX
{
    /// <summary>
    /// Tiny runtime painter for the presentation layers. Every sprite is a
    /// white/neutral mask that the UI tints, so the sunny palette stays in one
    /// place and the textures cost a single build-time pass.
    /// </summary>
    public static class PolishSprites
    {
        private static Sprite _softDot;
        private static Sprite _ellipse;
        private static Sprite _sparkle;
        private static Sprite _clod;
        private static Sprite _shard;
        private static Sprite _sunRays;
        private static Sprite _sunDisc;
        private static Sprite _vignette;
        private static Sprite _contactShadow;
        private static Sprite _burstStar;
        private static Sprite _bird;

        /// <summary>Soft radial falloff, used for glows and smoke.</summary>
        public static Sprite SoftDot => _softDot != null ? _softDot : _softDot = Build("SoftDot", 128, 128, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            return new Color(1f, 1f, 1f, Mathf.Pow(1f - d, 2.1f));
        });

        /// <summary>Hard-edged circle with a one-pixel soft rim (eye lids, dots).</summary>
        public static Sprite Ellipse => _ellipse != null ? _ellipse : _ellipse = Build("Ellipse", 64, 64, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01((1f - d) * 32f);
            return new Color(1f, 1f, 1f, alpha);
        });

        /// <summary>Four-point cartoon twinkle.</summary>
        public static Sprite Sparkle => _sparkle != null ? _sparkle : _sparkle = Build("Sparkle", 128, 128, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx);
            float pointy = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2f)), 5f);
            float radius = 0.14f + 0.86f * pointy;
            float alpha = Mathf.Clamp01((radius - r) * 14f);
            alpha = Mathf.Max(alpha, Mathf.Pow(Mathf.Clamp01(1f - r * 1.4f), 3f) * 0.8f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
        });

        /// <summary>Irregular mud clod with a lit top edge.</summary>
        public static Sprite Clod => _clod != null ? _clod : _clod = Build("Clod", 96, 96, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx);
            float radius = 0.62f + 0.16f * Mathf.Sin(a * 3f + 1.1f) + 0.10f * Mathf.Sin(a * 5f - 0.7f);
            float alpha = Mathf.Clamp01((radius - r) * 18f);
            float shade = 0.82f + 0.30f * Mathf.Clamp01(v);
            return new Color(shade, shade, shade, Mathf.Clamp01(alpha));
        });

        /// <summary>Hard-hat / armor fragment.</summary>
        public static Sprite Shard => _shard != null ? _shard : _shard = Build("Shard", 64, 64, (u, v) =>
        {
            float dx = Mathf.Abs(u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            bool inside = dy > -0.9f && dy < (0.9f - dx * 1.6f);
            float edge = Mathf.Clamp01((0.9f - dx * 1.6f - dy) * 10f);
            return new Color(1f, 1f, 1f, inside ? Mathf.Clamp01(edge) : 0f);
        });

        /// <summary>Fourteen warm sun spikes.</summary>
        public static Sprite SunRays => _sunRays != null ? _sunRays : _sunRays = Build("SunRays", 256, 256, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            float a = Mathf.Atan2(dy, dx);
            float spikes = Mathf.Abs(Mathf.Cos(a * 7f));
            float radius = 0.20f + 0.78f * Mathf.Pow(spikes, 9f);
            float alpha = Mathf.Clamp01((radius - r) * 7f) * Mathf.Clamp01((1f - r) * 2.4f);
            alpha *= Mathf.Clamp01(r * 8f);
            return new Color(1f, 1f, 1f, alpha * 0.9f);
        });

        /// <summary>Warm sun disc with a feathered rim.</summary>
        public static Sprite SunDisc => _sunDisc != null ? _sunDisc : _sunDisc = Build("SunDisc", 128, 128, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float r = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            return new Color(1f, 1f, 1f, Mathf.Pow(1f - r, 1.6f));
        });

        /// <summary>Warm edge falloff for the whole screen.</summary>
        public static Sprite Vignette => _vignette != null ? _vignette : _vignette = Build("Vignette", 256, 256, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
            float edge = Mathf.SmoothStep(0.28f, 0.92f, r);
            return new Color(0.30f, 0.13f, 0.04f, edge * 0.55f);
        });

        /// <summary>Soft oval for ground shadows.</summary>
        public static Sprite ContactShadow => _contactShadow != null ? _contactShadow : _contactShadow = Build("ContactShadow", 128, 96, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = ((v - 0.5f) * 2f) / 0.55f;
            float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
            return new Color(1f, 1f, 1f, Mathf.Pow(1f - d, 1.7f) * 0.75f);
        });

        /// <summary>Twelve-point comic impact burst.</summary>
        public static Sprite BurstStar => _burstStar != null ? _burstStar : _burstStar = Build("BurstStar", 256, 256, (u, v) =>
        {
            float dx = (u - 0.5f) * 2f;
            float dy = (v - 0.5f) * 2f;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Atan2(dy, dx);
            float radius = 0.56f + 0.40f * Mathf.Cos(a * 12f);
            float alpha = Mathf.Clamp01((radius - r) * 22f);
            return new Color(1f, 1f, 1f, alpha);
        });

        /// <summary>Distant cartoon bird silhouette.</summary>
        public static Sprite Bird => _bird != null ? _bird : _bird = Build("Bird", 64, 48, (u, v) =>
        {
            float half = Mathf.Abs(u - 0.5f) * 2f;
            float wing = 0.62f - 0.34f * Mathf.Pow(half, 1.35f);
            float thickness = Mathf.Abs(v - wing);
            float alpha = Mathf.Clamp01((0.055f - thickness) * 22f);
            alpha *= Mathf.Clamp01(1.05f - half);
            return new Color(1f, 1f, 1f, alpha);
        });

        private static Sprite Build(string name, int width, int height, Func<float, float, Color> sample)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            Color32[] pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    pixels[y * width + x] = sample(u, v);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
