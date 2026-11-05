using System.Globalization;
using UnityEngine;

namespace Crash
{
    /// Central palette + shared font so every view looks consistent.
    public static class Pal
    {
        public static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.magenta;
        }

        public static readonly Color BgTop = Hex("#0C0B22");
        public static readonly Color BgBot = Hex("#03030A");
        public static readonly Color Ink = Hex("#F5F7FF");
        public static readonly Color InkDim = Hex("#98A1C8");
        public static readonly Color Cyan = Hex("#39EBD2");
        public static readonly Color CyanDeep = Hex("#12B8A6");
        public static readonly Color Violet = Hex("#8E63FF");
        public static readonly Color Magenta = Hex("#FF4FD8");
        public static readonly Color Gold = Hex("#FFC247");
        public static readonly Color Amber = Hex("#FF9A3C");
        public static readonly Color Red = Hex("#FF4D6D");
        public static readonly Color Card = Hex("#141834");
        public static readonly Color CardHi = Hex("#1D2350");
        public static readonly Color Line = Hex("#2A3060");

        static Font _font;

        public static Font UiFont
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
                if (_font == null)
                {
                    _font = Font.CreateDynamicFontFromOSFont(
                        new[] { "Segoe UI", "Arial", "Helvetica Neue", "Helvetica", "DejaVu Sans" }, 48);
                }
                return _font;
            }
        }

        public static string N(long v) => v.ToString("N0", CultureInfo.InvariantCulture);

        public static string Mult(float m)
        {
            if (m >= 1000f) return m.ToString("0", CultureInfo.InvariantCulture) + "x";
            return m.ToString("0.00", CultureInfo.InvariantCulture) + "x";
        }

        public static Color WithAlpha(this Color c, float a)
        {
            c.a = a;
            return c;
        }
    }
}
