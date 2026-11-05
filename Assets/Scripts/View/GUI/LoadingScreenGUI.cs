using UnityEngine;
using System.Collections.Generic;

namespace com.javierquevedo
{
    /// <summary>
    /// Cyber Orbs system boot screen.
    ///
    /// Same visual language as the menu and HUD: authored PNG panels, neon chrome, chroma
    /// wordmark and a scan sweep, laid out in a 900x640 design space that scales uniformly
    /// inside the safe area. Progress is real - the controller advances it while the art,
    /// orb sprites, VFX and synthesised audio are warmed up.
    /// </summary>
    public class LoadingScreenGUI : MonoBehaviour
    {
        private struct LogEntry
        {
            public string Text;
            public bool Ok;
            public float At;
        }

        public static readonly BubbleColor[] Cores =
        {
            BubbleColor.Red, BubbleColor.Black, BubbleColor.Green,
            BubbleColor.Blue, BubbleColor.Yellow, BubbleColor.White
        };

        public static readonly string[] Tips =
        {
            "MATCH THREE OR MORE CORES TO SHATTER THE CLUSTER.",
            "CUT A CLUSTER LOOSE FROM THE CEILING FOR AN AVALANCHE BONUS.",
            "RIGHT-CLICK OR PRESS S TO SWAP THE LOADED CORE.",
            "EVERY MISS DRAINS A SHIELD CELL. AT ZERO, A ROW DROPS IN.",
            "NINETY-SECOND BLITZ: CHAINS BUY BACK TIME.",
            "SIX CORES: FIRE, STORM, CIRCUIT, SOLAR, VOID AND CRYSTAL."
        };

        private static readonly Color TextBlue = new Color(0.78f, 0.90f, 0.98f, 1f);
        private static readonly Color Muted = new Color(0.44f, 0.58f, 0.70f, 1f);
        private static readonly Color Cyan = ArtUI.Cyan;

        private GUIStyle _title;
        private GUIStyle _kicker;
        private GUIStyle _small;
        private GUIStyle _tiny;
        private GUIStyle _status;
        private bool _stylesReady;

        private Texture2D _bg;
        private Texture2D _vignette;
        private Texture2D _ring;
        private Texture2D _white;

        private readonly List<LogEntry> _log = new List<LogEntry>(8);
        private float _progress;
        private float _targetProgress;
        private string _stageLabel = "BOOTING";
        private bool _complete;
        private float _completeAt = -1f;
        private int _tipIndex;
        private float _tipAt;
        private float _scale = 1f;
        private Rect _stage;

        public bool IsComplete { get { return _complete; } }

        void Start()
        {
            _tipIndex = Random.Range(0, Tips.Length);
            _tipAt = Time.unscaledTime;
        }

        public void SetStage(string label, float progress)
        {
            _stageLabel = label;
            _targetProgress = Mathf.Clamp01(progress);
        }

        public void AddLog(string text, bool ok)
        {
            _log.Add(new LogEntry { Text = text, Ok = ok, At = Time.unscaledTime });
            if (_log.Count > 6) _log.RemoveAt(0);
        }

        public void MarkReady()
        {
            _targetProgress = 1f;
            _complete = true;
            _completeAt = Time.unscaledTime;
        }

        void Update()
        {
            _progress = Mathf.MoveTowards(_progress, _targetProgress, Time.unscaledDeltaTime * 1.25f);
            if (_complete && _progress > 0.995f) _progress = 1f;

            if (Tips.Length > 1 && Time.unscaledTime - _tipAt > 4.6f)
            {
                _tipAt = Time.unscaledTime;
                _tipIndex = (_tipIndex + 1) % Tips.Length;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Styles and layout
        // ---------------------------------------------------------------------------------------

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _title = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _kicker = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _small = ArtUI.MakeLabel(false, TextAnchor.MiddleLeft);
            _tiny = ArtUI.MakeLabel(false, TextAnchor.MiddleCenter);
            _status = ArtUI.MakeLabel(true, TextAnchor.MiddleRight);

            _bg = CyberArtGenerator.LoadArtTexture("background");
            _ring = CyberArtGenerator.LoadArtTexture("vfx_ring");
            _white = CyberArtGenerator.WhiteTexture;
            _vignette = BuildVignette();
        }

        private static Texture2D BuildVignette()
        {
            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "CyberBootVignette";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                    float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                    float d = Mathf.Clamp01(Mathf.Max(nx, ny));
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.50f, 1f, d)) * 0.86f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void ComputeStage(Rect safe)
        {
            const float designW = 900f;
            const float designH = 640f;
            const float margin = 18f;

            float s = Mathf.Min(
                (safe.width - margin * 2f) / designW,
                (safe.height - margin * 2f) / designH);
            _scale = Mathf.Clamp(s, 0.40f, 1.85f);

            _stage = new Rect(
                safe.x + (safe.width - designW * _scale) * 0.5f,
                safe.y + (safe.height - designH * _scale) * 0.5f,
                designW * _scale,
                designH * _scale);
        }

        private int FontSize(float designSize)
        {
            return Mathf.Max(8, Mathf.RoundToInt(designSize * _scale));
        }

        // ---------------------------------------------------------------------------------------
        // Draw
        // ---------------------------------------------------------------------------------------

        void OnGUI()
        {
            EnsureStyles();

            Rect screen = ResponsiveLayout.ScreenGui;
            ComputeStage(ResponsiveLayout.SafeAreaGui);

            DrawBackdrop(screen);
            DrawDust(screen);
            DrawChrome();
            DrawEmblem();
            DrawWordmark();
            DrawProgress();
            DrawBootLog();
            DrawTip();
            DrawOutro(screen);
        }

        private void DrawBackdrop(Rect screen)
        {
            if (_bg != null)
            {
                float k = Mathf.Max(screen.width / _bg.width, screen.height / _bg.height) * 1.06f;
                float w = _bg.width * k;
                float h = _bg.height * k;
                float dx = Mathf.Sin(Time.unscaledTime * 0.05f) * 0.010f * w;
                float dy = Mathf.Cos(Time.unscaledTime * 0.04f) * 0.010f * h;
                ArtUI.DrawTex(new Rect(screen.x + (screen.width - w) * 0.5f + dx,
                                       screen.y + (screen.height - h) * 0.5f + dy, w, h),
                    _bg, new Color(0.52f, 0.61f, 0.76f, 1f));
            }

            ArtUI.DrawTex(screen, _white, new Color(0.012f, 0.018f, 0.032f, 0.58f));
            if (_vignette != null)
                ArtUI.DrawTex(screen, _vignette, Color.white);

            // Slow radar sweep. It gives the boot screen a heartbeat without any noise.
            float sweep = Mathf.Repeat(Time.unscaledTime * 0.12f, 1f);
            float sy = screen.y + screen.height * sweep;
            ArtUI.DrawTex(new Rect(screen.x, sy, screen.width, Mathf.Max(1f, 2f * _scale)),
                _white, new Color(0.35f, 0.92f, 1f, 0.045f));
        }

        private void DrawDust(Rect screen)
        {
            if (Event.current.type != EventType.Repaint) return;

            float t = Time.unscaledTime;
            for (int i = 0; i < 26; i++)
            {
                float seed = i * 7.13f;
                float x = screen.x + Mathf.Repeat(seed * 0.137f + Mathf.Sin(t * 0.10f + seed) * 0.02f, 1f) * screen.width;
                float y = screen.y + Mathf.Repeat(seed * 0.219f - t * (0.010f + (i % 5) * 0.003f), 1f) * screen.height;
                float a = 0.08f + 0.10f * Mathf.Abs(Mathf.Sin(t * 0.7f + seed));
                float s = (i % 3 == 0) ? 2f : 1.4f;
                ArtUI.DrawTex(new Rect(x, y, s, s), _white, new Color(0.55f, 0.90f, 1f, a));
            }
        }

        private void DrawChrome()
        {
            float u = _scale;
            Texture2D white = _white;
            Rect s = _stage;
            Color bracket = new Color(0.30f, 0.86f, 1f, 0.20f);
            float len = 24f * u;
            float th = Mathf.Max(1f, 1.5f * u);

            ArtUI.DrawTex(new Rect(s.x, s.y, len, th), white, bracket);
            ArtUI.DrawTex(new Rect(s.x, s.y, th, len), white, bracket);
            ArtUI.DrawTex(new Rect(s.xMax - len, s.y, len, th), white, bracket);
            ArtUI.DrawTex(new Rect(s.xMax - th, s.y, th, len), white, bracket);
            ArtUI.DrawTex(new Rect(s.x, s.yMax - th, len, th), white, bracket);
            ArtUI.DrawTex(new Rect(s.x, s.yMax - len, th, len), white, bracket);
            ArtUI.DrawTex(new Rect(s.xMax - len, s.yMax - th, len, th), white, bracket);
            ArtUI.DrawTex(new Rect(s.xMax - th, s.yMax - len, th, len), white, bracket);

            ArtUI.DrawTex(new Rect(s.x, s.y + 30f * u, s.width, Mathf.Max(1f, u)),
                white, new Color(0.35f, 0.72f, 0.88f, 0.18f));

            _small.alignment = TextAnchor.MiddleLeft;
            _small.fontSize = FontSize(10.5f);
            ArtUI.Text(new Rect(s.x, s.y + 6f * u, s.width * 0.62f, 18f * u),
                "ARENA NETWORK   //   BOOT SEQUENCE", _small, new Color(0.52f, 0.72f, 0.82f, 0.92f), u);

            _small.alignment = TextAnchor.MiddleRight;
            ArtUI.Text(new Rect(s.x + s.width * 0.38f, s.y + 6f * u, s.width * 0.62f, 18f * u),
                "NODE S07   //   BUILD 1.0", _small, new Color(0.40f, 0.56f, 0.68f), u);
        }

        private void DrawEmblem()
        {
            float u = _scale;
            float t = Time.unscaledTime;
            float cx = _stage.center.x;
            float cy = _stage.y + 158f * u;

            // Halo, then two counter-rotating reticle rings.
            ArtUI.DrawTex(new Rect(cx - 132f * u, cy - 132f * u, 264f * u, 264f * u),
                _ring, new Color(0.25f, 0.85f, 1f, 0.055f));
            RotatedTex(new Rect(cx - 112f * u, cy - 112f * u, 224f * u, 224f * u),
                _ring, new Color(0.30f, 0.90f, 1f, 0.17f), t * 9f);
            RotatedTex(new Rect(cx - 78f * u, cy - 78f * u, 156f * u, 156f * u),
                _ring, new Color(0.35f, 0.95f, 1f, 0.26f), -t * 15f);

            // Tick marks ride the outer ring.
            for (int i = 0; i < 24; i++)
            {
                float a = i * 15f + t * 6f;
                float rad = a * Mathf.Deg2Rad;
                float x = cx + Mathf.Cos(rad) * 100f * u;
                float y = cy + Mathf.Sin(rad) * 100f * u;
                float len = (i % 3 == 0 ? 6f : 3f) * u;
                float th = Mathf.Max(1f, 1.4f * u);
                RotatedTex(new Rect(x - len * 0.5f, y - th * 0.5f, len, th), _white,
                    new Color(0.42f, 0.94f, 1f, i % 3 == 0 ? 0.46f : 0.20f), a + 90f);
            }

            // Crosshair arms and a pulsing storm core.
            Color arm = new Color(0.35f, 0.92f, 1f, 0.20f);
            ArtUI.DrawTex(new Rect(cx - 70f * u, cy, 24f * u, Mathf.Max(1f, u)), _white, arm);
            ArtUI.DrawTex(new Rect(cx + 46f * u, cy, 24f * u, Mathf.Max(1f, u)), _white, arm);
            ArtUI.DrawTex(new Rect(cx, cy - 70f * u, Mathf.Max(1f, u), 24f * u), _white, arm);
            ArtUI.DrawTex(new Rect(cx, cy + 46f * u, Mathf.Max(1f, u), 24f * u), _white, arm);

            float pulse = 0.94f + 0.06f * Mathf.Sin(t * 2.4f);
            Sprite core = CyberArtGenerator.GetOrbSprite(BubbleColor.Blue);
            if (core != null)
            {
                float size = 58f * u * pulse;
                ArtUI.DrawTex(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), core.texture, Color.white);
            }

            // The orb roster orbits the core, so the six identities are on screen from boot.
            for (int i = 0; i < Cores.Length; i++)
            {
                Sprite orb = CyberArtGenerator.GetOrbSprite(Cores[i]);
                if (orb == null) continue;

                float ang = (t * 15f + i * 60f) * Mathf.Deg2Rad;
                float ox = cx + Mathf.Cos(ang) * 124f * u;
                float oy = cy + Mathf.Sin(ang) * 84f * u;
                float size = 27f * u;
                float alpha = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(t * 1.4f + i));
                ArtUI.DrawTex(new Rect(ox - size * 0.5f, oy - size * 0.5f, size, size), orb.texture,
                    new Color(1f, 1f, 1f, alpha));
            }
        }

        private void DrawWordmark()
        {
            float u = _scale;

            _kicker.fontSize = FontSize(10.5f);
            _kicker.alignment = TextAnchor.MiddleCenter;
            ArtUI.Text(new Rect(_stage.x, _stage.y + 276f * u, _stage.width, 18f * u),
                "NEO-KYOTO ARENA   //   INITIALISING COMBAT SYSTEMS", _kicker,
                new Color(0.50f, 0.84f, 0.95f, 0.92f), u);

            _title.fontSize = FontSize(50f);
            _title.alignment = TextAnchor.MiddleCenter;
            DrawChromaTitle(new Rect(_stage.x, _stage.y + 292f * u, _stage.width, 56f * u),
                "CYBER ORBS", Color.white);
        }

        private void DrawProgress()
        {
            float u = _scale;
            float barW = Mathf.Min(_stage.width - 60f * u, 560f * u);
            float barH = 18f * u;
            float barX = _stage.center.x - barW * 0.5f;
            float barY = _stage.y + 366f * u;

            ArtUI.DrawTex(new Rect(barX, barY - 7f * u, barW, barH + 14f * u), _white,
                new Color(0.20f, 0.85f, 1f, 0.035f));
            ArtUI.DrawTex(new Rect(barX - Mathf.Max(1f, u), barY - Mathf.Max(1f, u),
                    barW + Mathf.Max(2f, 2f * u), barH + Mathf.Max(2f, 2f * u)),
                _white, new Color(0.25f, 0.55f, 0.70f, 0.32f));

            const int segments = 32;
            float gap = Mathf.Max(1.5f, 2.2f * u);
            float segW = (barW - gap * (segments - 1)) / segments;
            float shown = _progress * segments;

            for (int i = 0; i < segments; i++)
            {
                Rect seg = new Rect(barX + i * (segW + gap), barY, segW, barH);
                ArtUI.DrawTex(seg, _white, new Color(0.055f, 0.09f, 0.145f, 0.92f));

                float fill = Mathf.Clamp01(shown - i);
                if (fill <= 0f) continue;

                float heat = Mathf.Clamp01(i / (float)segments * 0.7f + 0.3f);
                Color fillColor = Color.Lerp(new Color(0.05f, 0.70f, 0.92f, 1f),
                    new Color(0.70f, 1f, 1f, 1f), heat);
                ArtUI.DrawTex(new Rect(seg.x, seg.y, seg.width * fill, seg.height), _white, fillColor);
                ArtUI.DrawTex(new Rect(seg.x, seg.y, seg.width * fill, Mathf.Max(1f, seg.height * 0.18f)),
                    _white, new Color(1f, 1f, 1f, 0.30f));
            }

            // Leading edge glow, brighter while work is still happening.
            float headX = barX + Mathf.Clamp01(_progress) * barW;
            float headPulse = _complete ? 0.35f : 0.55f + 0.35f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            ArtUI.DrawTex(new Rect(headX - 1f, barY - 5f * u, 2f, barH + 10f * u), _white,
                new Color(0.85f, 1f, 1f, headPulse));

            if (_complete && _completeAt > 0f && Time.unscaledTime - _completeAt < 0.45f)
            {
                float flash = 1f - Mathf.Clamp01((Time.unscaledTime - _completeAt) / 0.45f);
                ArtUI.DrawTex(new Rect(barX, barY, barW, barH), _white,
                    new Color(0.75f, 1f, 1f, 0.35f * flash));
            }

            string active = _complete ? "ALL SYSTEMS NOMINAL" : _stageLabel + Dots();
            Color activeColor = _complete ? new Color(0.55f, 1f, 0.70f, 1f) : TextBlue;

            _small.alignment = TextAnchor.MiddleLeft;
            _small.fontSize = FontSize(11f);
            ArtUI.Text(new Rect(barX, barY + barH + 6f * u, barW * 0.7f, 16f * u),
                "> " + active, _small, activeColor, u);

            _status.alignment = TextAnchor.MiddleRight;
            _status.fontSize = FontSize(13f);
            ArtUI.Text(new Rect(barX + barW * 0.7f, barY + barH + 4f * u, barW * 0.3f, 18f * u),
                Mathf.RoundToInt(_progress * 100f) + "%", _status, Cyan, u);
        }

        private void DrawBootLog()
        {
            float u = _scale;
            float barW = Mathf.Min(_stage.width - 60f * u, 560f * u);
            float barX = _stage.center.x - barW * 0.5f;
            float logY = _stage.y + 424f * u;
            float rowH = 17f * u;
            float lineTh = Mathf.Max(1f, u);

            int activeRows = _complete ? 0 : 1;
            int logRows = Mathf.Min(_log.Count, 3 - activeRows);
            int first = Mathf.Max(0, _log.Count - logRows);

            for (int i = 0; i < logRows; i++)
            {
                LogEntry e = _log[first + i];
                float age = Mathf.Clamp01((Time.unscaledTime - e.At) / 0.25f);
                float y = logY + i * rowH;

                _small.alignment = TextAnchor.MiddleLeft;
                _small.fontSize = FontSize(9.5f);
                ArtUI.Text(new Rect(barX, y, barW - 46f * u, rowH),
                    "> " + e.Text, _small, new Color(Muted.r, Muted.g, Muted.b, 0.55f + 0.4f * age), u);

                _status.alignment = TextAnchor.MiddleRight;
                _status.fontSize = FontSize(9.5f);
                ArtUI.Text(new Rect(barX + barW - 46f * u, y, 46f * u, rowH),
                    e.Ok ? "OK" : "WARN", _status,
                    e.Ok ? new Color(0.42f, 0.96f, 0.64f, 0.45f + 0.5f * age)
                         : new Color(1f, 0.72f, 0.32f, 0.5f + 0.5f * age), u);

                ArtUI.DrawTex(new Rect(barX, y + rowH - lineTh, barW, lineTh), _white,
                    new Color(0.40f, 0.62f, 0.78f, 0.10f));
            }

            if (!_complete)
            {
                float y = logY + logRows * rowH;
                float blink = 0.45f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f));
                _small.alignment = TextAnchor.MiddleLeft;
                _small.fontSize = FontSize(9.5f);
                ArtUI.Text(new Rect(barX, y, barW * 0.8f, rowH),
                    "> " + _stageLabel + Dots(), _small, new Color(0.62f, 0.90f, 1f, blink), u);

                _status.alignment = TextAnchor.MiddleRight;
                _status.fontSize = FontSize(9.5f);
                ArtUI.Text(new Rect(barX + barW * 0.8f, y, barW * 0.2f, rowH),
                    "RUN", _status, new Color(0.45f, 0.85f, 1f, blink), u);

                ArtUI.DrawTex(new Rect(barX, y + rowH - lineTh, barW, lineTh), _white,
                    new Color(0.40f, 0.62f, 0.78f, 0.10f));
            }
        }

        private void DrawTip()
        {
            float u = _scale;
            float age = Time.unscaledTime - _tipAt;
            float fade = Mathf.Clamp01(age / 0.30f) * Mathf.Clamp01((4.6f - age) / 0.35f);

            _tiny.alignment = TextAnchor.MiddleCenter;
            _tiny.fontSize = FontSize(10f);
            ArtUI.Text(new Rect(_stage.x + 16f * u, _stage.y + 548f * u, _stage.width - 32f * u, 18f * u),
                "TIP   //   " + Tips[_tipIndex], _tiny, new Color(Muted.r, Muted.g, Muted.b, Mathf.Clamp01(fade)), u);

            _tiny.fontSize = FontSize(9f);
            ArtUI.Text(new Rect(_stage.x, _stage.y + 600f * u, _stage.width, 16f * u),
                "SIX CORES. ONE ARENA.", _tiny, new Color(0.28f, 0.40f, 0.50f, 0.85f), u);
        }

        private void DrawOutro(Rect screen)
        {
            if (!_complete || _completeAt < 0f) return;

            float t = Mathf.Clamp01((Time.unscaledTime - _completeAt - 0.14f) / 0.30f);
            if (t <= 0f) return;

            ArtUI.DrawTex(screen, _white, new Color(0.01f, 0.015f, 0.028f, Mathf.SmoothStep(0f, 1f, t)));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        private static void RotatedTex(Rect r, Texture2D tex, Color tint, float angle)
        {
            if (tex == null || Event.current.type != EventType.Repaint) return;

            Matrix4x4 prevMatrix = GUI.matrix;
            Color prevColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, r.center);
            GUI.color = ArtUI.Mul(prevColor, tint);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
            GUI.color = prevColor;
            GUI.matrix = prevMatrix;
        }

        private void DrawChromaTitle(Rect r, string text, Color main)
        {
            float u = _scale;
            RawText(new Rect(r.x + 2.4f * u, r.y + 2.4f * u, r.width, r.height), text, new Color(0f, 0f, 0f, 0.55f));
            RawText(new Rect(r.x - 1.8f * u, r.y, r.width, r.height), text, new Color(1f, 0.30f, 0.85f, 0.26f));
            RawText(new Rect(r.x + 1.8f * u, r.y, r.width, r.height), text, new Color(0.10f, 0.90f, 1f, 0.30f));
            RawText(r, text, main);
        }

        private void RawText(Rect r, string text, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = _title.normal.textColor;
            _title.normal.textColor = color;
            GUI.Label(r, text, _title);
            _title.normal.textColor = old;
        }

        private static string Dots()
        {
            int dots = 1 + (int)(Time.unscaledTime * 2.6f) % 3;
            return dots == 1 ? "." : (dots == 2 ? ".." : "...");
        }
    }
}
