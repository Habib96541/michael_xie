using UnityEngine;

namespace NeonDash
{
    /// <summary>The game's colour language: deep navy space, neon cyan and magenta accents.</summary>
    public static class Palette
    {
        public static readonly Color Background = new Color(0.016f, 0.026f, 0.070f, 1f);
        public static readonly Color BackgroundGlow = new Color(0.10f, 0.03f, 0.28f, 1f);
        public static readonly Color Panel = new Color(0.047f, 0.047f, 0.115f, 1f);
        public static readonly Color PanelSoft = new Color(0.10f, 0.094f, 0.21f, 1f);
        public static readonly Color PanelLine = new Color(0.25f, 0.24f, 0.48f, 1f);
        public static readonly Color Action = new Color(0.47f, 0.22f, 0.86f, 1f);

        public static readonly Color Cyan = new Color(0.00f, 0.86f, 1.00f, 1f);
        public static readonly Color Magenta = new Color(1.00f, 0.05f, 0.56f, 1f);
        public static readonly Color Lime = new Color(0.26f, 1.00f, 0.20f, 1f);
        public static readonly Color Gold = new Color(1.00f, 0.82f, 0.12f, 1f);
        public static readonly Color Pink = new Color(1.00f, 0.45f, 0.72f, 1f);
        public static readonly Color Violet = new Color(0.55f, 0.30f, 1.00f, 1f);

        public static readonly Color Text = new Color(0.93f, 0.96f, 1.00f, 1f);
        public static readonly Color TextDim = new Color(0.60f, 0.69f, 0.85f, 1f);

        public static readonly Color Track = new Color(0.055f, 0.080f, 0.160f, 1f);
        public static readonly Color TrackLine = new Color(0.06f, 0.95f, 1.00f, 0.85f);
        public static readonly Color Block = new Color(0.10f, 0.14f, 0.26f, 1f);
        public static readonly Color BlockEdge = new Color(0.35f, 0.55f, 0.95f, 1f);
        public static readonly Color Spike = new Color(0.13f, 0.16f, 0.28f, 1f);
        public static readonly Color SpikeEdge = new Color(1.00f, 0.16f, 0.55f, 1f);

        public static Color Alpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        public static Color Lerp(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, t);
        }
    }
}
