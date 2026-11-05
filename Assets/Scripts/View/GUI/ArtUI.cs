using UnityEngine;

namespace com.javierquevedo
{
    /// <summary>
    /// Shared IMGUI drawing helpers for the authored UI art in Resources/Art/Sprites:
    /// 9-sliced steel panels, sliced pill buttons, and shadowed Rajdhani text.
    /// </summary>
    public static class ArtUI
    {
        // Source art sizes (px). Sprites are rendered at 2x, so these are 2x the SVG values.
        public const float PanelTexBorder = 64f;   // hud_panel.png is 1024x512
        public const float PillTexBorder = 88f;    // pill_*.png is 768x192

        public static readonly Color LabelBlue = new Color(0.62f, 0.78f, 0.88f, 1f);
        public static readonly Color Cyan = new Color(0.45f, 0.95f, 1.00f, 1f);
        public static readonly Color Danger = new Color(1.00f, 0.38f, 0.34f, 1f);
        public static readonly Color Magenta = new Color(1.00f, 0.55f, 0.86f, 1f);
        public static readonly Color Gold = new Color(1.00f, 0.82f, 0.30f, 1f);

        private static Font _bold;
        private static Font _semiBold;

        public static Font Bold
        {
            get
            {
                if (_bold == null) _bold = Resources.Load<Font>("Fonts/Rajdhani-Bold");
                return _bold;
            }
        }

        public static Font SemiBold
        {
            get
            {
                if (_semiBold == null) _semiBold = Resources.Load<Font>("Fonts/Rajdhani-SemiBold");
                return _semiBold != null ? _semiBold : Bold;
            }
        }

        // Reused buffers for the 9-slice helper (OnGUI runs several times per frame).
        private static readonly float[] _sx = new float[4];
        private static readonly float[] _sy = new float[4];
        private static readonly float[] _su = new float[4];
        private static readonly float[] _sv = new float[4];

        public static Color Mul(Color a, Color b)
        {
            return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
        }

        public static GUIStyle MakeLabel(bool bold, TextAnchor anchor)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.font = bold ? Bold : SemiBold;
            style.fontStyle = (bold && Bold == null) ? FontStyle.Bold : FontStyle.Normal;
            style.alignment = anchor;
            style.normal.textColor = bold ? Color.white : LabelBlue;
            return style;
        }

        /// <summary>Draws a texture as a 9-slice. texBorder is in source pixels, screenBorder in GUI pixels.</summary>
        public static void DrawSliced(Rect r, Texture2D tex, float texBorder, float screenBorder, Color tint)
        {
            if (tex == null || Event.current.type != EventType.Repaint) return;

            float b = Mathf.Min(screenBorder, r.width * 0.5f, r.height * 0.5f);
            float u = texBorder / tex.width;
            float v = texBorder / tex.height;

            _sx[0] = r.xMin; _sx[1] = r.xMin + b; _sx[2] = r.xMax - b; _sx[3] = r.xMax;
            _sy[0] = r.yMin; _sy[1] = r.yMin + b; _sy[2] = r.yMax - b; _sy[3] = r.yMax;
            _su[0] = 0f; _su[1] = u; _su[2] = 1f - u; _su[3] = 1f;
            // GUI y runs top-down while texture v runs bottom-up.
            _sv[0] = 1f; _sv[1] = 1f - v; _sv[2] = v; _sv[3] = 0f;

            Color prev = GUI.color;
            GUI.color = Mul(prev, tint);
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Rect dst = Rect.MinMaxRect(_sx[col], _sy[row], _sx[col + 1], _sy[row + 1]);
                    Rect uv = Rect.MinMaxRect(_su[col], _sv[row + 1], _su[col + 1], _sv[row]);
                    GUI.DrawTextureWithTexCoords(dst, tex, uv);
                }
            }
            GUI.color = prev;
        }

        public static void DrawPanel(Rect r, float ui, Color tint)
        {
            DrawSliced(r, CyberArtGenerator.LoadArtTexture("hud_panel"), PanelTexBorder, 12f * ui, tint);
        }

        public static void DrawTex(Rect r, Texture2D tex, Color tint)
        {
            if (tex == null || Event.current.type != EventType.Repaint) return;
            Color prev = GUI.color;
            GUI.color = Mul(prev, tint);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        /// <summary>Label with a soft dark drop shadow so it stays readable on any panel.</summary>
        public static void Text(Rect r, string text, GUIStyle style, Color color, float ui)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.7f * color.a);
            GUI.Label(new Rect(r.x + ui, r.y + 1.4f * ui, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        /// <summary>Sliced pill button (pill_*.png) with hover swell, press squash, drop shadow and a label.</summary>
        public static bool PillButton(Rect r, string face, string label, GUIStyle textStyle, float ui)
        {
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (Event.current.type != EventType.Repaint) return clicked;

            bool hover = r.Contains(Event.current.mousePosition);
            bool down = hover && Input.GetMouseButton(0);
            float s = down ? 0.97f : (hover ? 1.03f : 1f);
            Rect dr = new Rect(r.center.x - r.width * s * 0.5f,
                r.center.y - r.height * s * 0.5f + (down ? 1.5f * ui : 0f),
                r.width * s, r.height * s);

            Texture2D tex = CyberArtGenerator.LoadArtTexture(face);
            if (tex != null)
            {
                // Corner radius scales with button height so the ends stay perfectly round.
                float border = PillTexBorder * (dr.height / tex.height);
                if (!down)
                    DrawSliced(new Rect(dr.x, dr.y + 3f * ui, dr.width, dr.height), tex, PillTexBorder, border, new Color(0f, 0f, 0f, 0.4f));
                DrawSliced(dr, tex, PillTexBorder, border, Color.white);
            }

            textStyle.fontSize = Mathf.RoundToInt(dr.height * 0.46f);
            Text(new Rect(dr.x, dr.y - dr.height * 0.03f, dr.width, dr.height), label, textStyle, Color.white, ui);
            return clicked;
        }
    }
}
