using UnityEngine;

namespace com.javierquevedo
{
    /// <summary>
    /// Main menu / title screen for Cyber Orbs.
    ///
    /// Laid out in a design space (1180x700 when wide, 520x960 when tall) that is scaled
    /// uniformly to fit the safe area, so the same composition works from a phone in
    /// portrait to a 4K desktop window. All surfaces come from the authored art in
    /// Resources/Art/Sprites; only the labels use the Rajdhani font, exactly like the HUD.
    /// </summary>
    public class SplashScreenGUI : MonoBehaviour
    {
        public delegate void StartGameSelectionDelegate();

        private StartGameSelectionDelegate startGameDelegate;

        public StartGameSelectionDelegate StartGameDelegate
        {
            set { this.startGameDelegate = value; }
        }

        public static bool SelectedTimedMode = true;

        // Red, Void, Circuit, Storm, Solar, Crystal - the order the roster reads in.
        private static readonly BubbleColor[] ShowcaseOrder =
        {
            BubbleColor.Red, BubbleColor.Black, BubbleColor.Green,
            BubbleColor.Blue, BubbleColor.Yellow, BubbleColor.White
        };

        private static readonly Color TextBlue = new Color(0.78f, 0.90f, 0.98f, 1f);
        private static readonly Color Muted = new Color(0.50f, 0.66f, 0.78f, 1f);
        private static readonly Color Online = new Color(0.45f, 1.00f, 0.60f, 1f);
        private static readonly Color Cyan = ArtUI.Cyan;
        private static readonly Color Magenta = ArtUI.Magenta;
        private static readonly Color LabelBlue = ArtUI.LabelBlue;

        private GUIStyle _title;
        private GUIStyle _kicker;
        private GUIStyle _tagline;
        private GUIStyle _small;
        private GUIStyle _label;
        private GUIStyle _centerLabel;
        private GUIStyle _body;
        private GUIStyle _pill;
        private GUIStyle _cardTitle;
        private GUIStyle _cardValue;
        private bool _stylesReady;

        private Texture2D _vignette;
        private Texture2D _ring;
        private Texture2D _dot;
        private Texture2D _badge;
        private Texture2D[] _digits;

        private float _scale = 1f;
        private Rect _stage;
        private bool _wide = true;

        private bool _showHelp;
        private int _best;
        private string _bestTier = "NOVICE";

        /// <summary>Set by the boot stage so the menu can cross-fade in from black.</summary>
        public static float BootHandoffTime = -1f;

        private const float LaunchDuration = 0.38f;
        private bool _launching;
        private bool _handedOff;
        private float _launchAt;
        private float _openedAt;

        private float IntroStart
        {
            get { return Mathf.Max(_openedAt, BootHandoffTime); }
        }

        void Start()
        {
            _openedAt = Time.unscaledTime;
            RefreshBest();
        }

        private void RefreshBest()
        {
            _best = PlayerPrefs.GetInt("CyberOrbs_HighScore", 0);
            _bestTier = TierFor(_best);
        }

        void Update()
        {
            if (LoadingScreenController.BootActive) return;

            if (_launching)
            {
                if (!_handedOff && Time.unscaledTime - _launchAt >= LaunchDuration)
                {
                    _handedOff = true;
                    startGame();
                }
                return;
            }

            // Hold input until the boot hand-off has faded in.
            if (Time.unscaledTime - IntroStart < 0.42f) return;

            if (_showHelp)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    _showHelp = false;
                    PlayClick();
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                BeginLaunch();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMute();
                PlayClick();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                SelectedTimedMode = !SelectedTimedMode;
                PlayClick();
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _title = ArtUI.MakeLabel(true, TextAnchor.MiddleLeft);
            _kicker = ArtUI.MakeLabel(true, TextAnchor.MiddleLeft);
            _tagline = ArtUI.MakeLabel(false, TextAnchor.MiddleLeft);
            _small = ArtUI.MakeLabel(false, TextAnchor.MiddleCenter);
            _label = ArtUI.MakeLabel(false, TextAnchor.MiddleLeft);
            _centerLabel = ArtUI.MakeLabel(false, TextAnchor.MiddleCenter);
            _pill = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _cardTitle = ArtUI.MakeLabel(true, TextAnchor.MiddleLeft);
            _cardValue = ArtUI.MakeLabel(true, TextAnchor.MiddleRight);

            _body = ArtUI.MakeLabel(false, TextAnchor.UpperLeft);
            _body.wordWrap = true;
            _body.normal.textColor = new Color(0.82f, 0.92f, 1.00f, 1f);

            _ring = CyberArtGenerator.LoadArtTexture("vfx_ring");
            _dot = CyberArtGenerator.LoadArtTexture("aim_dot");
            _badge = CyberArtGenerator.LoadArtTexture("rank_badge");

            _digits = new Texture2D[10];
            for (int i = 0; i < 10; i++)
                _digits[i] = CyberArtGenerator.LoadArtTexture("digit_cyan_" + i);

            _vignette = BuildVignette();
        }

        private static Texture2D BuildVignette()
        {
            const int size = 96;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "CyberMenuVignette";
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
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1f, d)) * 0.82f;
                    pixels[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // ---------------------------------------------------------------------------------------
        // Layout
        // ---------------------------------------------------------------------------------------

        private void ComputeStage(Rect safe)
        {
            float aspect = safe.width / Mathf.Max(1f, safe.height);
            _wide = aspect >= 1.22f;

            float designW = _wide ? 1180f : 520f;
            float designH = _wide ? 700f : 960f;
            float margin = _wide ? 24f : 12f;

            float s = Mathf.Min(
                (safe.width - margin * 2f) / designW,
                (safe.height - margin * 2f) / designH);
            _scale = Mathf.Clamp(s, 0.30f, 1.55f);

            _stage = new Rect(
                safe.x + (safe.width - designW * _scale) * 0.5f,
                safe.y + (safe.height - designH * _scale) * 0.5f,
                designW * _scale,
                designH * _scale);
        }

        void OnGUI()
        {
            if (LoadingScreenController.BootActive) return;

            EnsureStyles();

            Rect screen = ResponsiveLayout.ScreenGui;
            Rect safe = ResponsiveLayout.SafeAreaGui;
            ComputeStage(safe);

            float introT = Mathf.Clamp01((Time.unscaledTime - IntroStart) / 0.55f);
            float introEase = 1f - Mathf.Pow(1f - introT, 3f);

            DrawBackdrop(screen);
            DrawAmbientOrbs(screen);

            if (_launching)
            {
                DrawLaunchOverlay(screen);
                return;
            }

            // Slide the whole composition up as the boot black lifts.
            _stage = new Rect(_stage.x, _stage.y + (1f - introEase) * 26f * _scale, _stage.width, _stage.height);

            DrawBrackets();
            DrawHeader();

            if (_showHelp)
            {
                DrawHelpModal(screen, safe);
                DrawEntryOverlay(screen, introEase);
                return;
            }

            DrawTitleBlock();
            DrawShowcase();

            Rect card = _wide
                ? new Rect(_stage.x + 650f * _scale, _stage.y + 108f * _scale, 500f * _scale, 460f * _scale)
                : new Rect(_stage.x + 8f * _scale, _stage.y + 396f * _scale, _stage.width - 16f * _scale, 500f * _scale);
            DrawConfigCard(card);

            DrawFooter();
            DrawEntryOverlay(screen, introEase);
        }

        /// <summary>
        /// A few showcase orbs drifting up the outer margins. Very low alpha, so they add
        /// parallax to the arena without ever competing with the labels.
        /// </summary>
        private void DrawAmbientOrbs(Rect screen)
        {
            if (Event.current.type != EventType.Repaint) return;

            float t = Time.unscaledTime;
            for (int i = 0; i < ShowcaseOrder.Length; i++)
            {
                Sprite sp = CyberArtGenerator.GetOrbSprite(ShowcaseOrder[i]);
                if (sp == null) continue;

                float phase = i * 1.7f;
                float edge = (i % 2 == 0) ? 0.075f : 0.925f;
                float x = screen.x + screen.width * (edge + Mathf.Sin(t * 0.07f + phase) * 0.035f);
                float size = (26f + (i % 4) * 12f) * Mathf.Clamp(_scale, 0.7f, 1.6f);
                float travel = Mathf.Repeat(t * (0.010f + i * 0.0018f) + i * 0.167f, 1f);
                float y = screen.yMax + size - travel * (screen.height + size * 2f);
                float alpha = 0.08f + 0.05f * Mathf.Abs(Mathf.Sin(t * 0.9f + phase));

                ArtUI.DrawTex(new Rect(x - size * 0.5f, y - size * 0.5f, size, size), sp.texture,
                    new Color(1f, 1f, 1f, Mathf.Clamp01(alpha)));
            }
        }

        private void DrawEntryOverlay(Rect screen, float introEase)
        {
            float alpha = 1f - introEase;
            if (alpha <= 0.001f) return;
            ArtUI.DrawTex(screen, CyberArtGenerator.WhiteTexture,
                new Color(0.01f, 0.015f, 0.028f, Mathf.Clamp01(alpha)));
        }

        private void DrawLaunchOverlay(Rect screen)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - _launchAt) / LaunchDuration);
            float alpha = Mathf.SmoothStep(0f, 1f, t);

            ArtUI.DrawTex(screen, CyberArtGenerator.WhiteTexture,
                new Color(0.01f, 0.015f, 0.028f, alpha));

            float textAlpha = Mathf.Clamp01((alpha - 0.22f) / 0.78f);
            if (textAlpha <= 0f) return;

            int dots = 1 + (int)(Time.unscaledTime * 4f) % 3;
            string label = "DEPLOYING TO ARENA" + (dots == 1 ? "." : (dots == 2 ? ".." : "..."));

            _small.alignment = TextAnchor.MiddleCenter;
            _small.fontSize = Mathf.Max(9, Mathf.RoundToInt(12f * _scale));
            ArtUI.Text(new Rect(screen.x, screen.y + screen.height * 0.5f - 10f * _scale, screen.width, 20f * _scale),
                label, _small, new Color(0.60f, 0.95f, 1f, textAlpha), _scale);
        }

        // ---------------------------------------------------------------------------------------
        // Backdrop and chrome
        // ---------------------------------------------------------------------------------------

        private void DrawBackdrop(Rect screen)
        {
            Texture2D bg = CyberArtGenerator.LoadArtTexture("background");
            if (bg != null)
            {
                // Cover the window, then drift a couple of percent so the circuit traces feel alive.
                float k = Mathf.Max(screen.width / bg.width, screen.height / bg.height) * 1.06f;
                float w = bg.width * k;
                float h = bg.height * k;
                float dx = Mathf.Sin(Time.unscaledTime * 0.06f) * 0.012f * w;
                float dy = Mathf.Cos(Time.unscaledTime * 0.045f) * 0.012f * h;
                ArtUI.DrawTex(new Rect(screen.x + (screen.width - w) * 0.5f + dx,
                                       screen.y + (screen.height - h) * 0.5f + dy, w, h),
                    bg, new Color(0.62f, 0.70f, 0.84f, 1f));
            }

            ArtUI.DrawTex(screen, CyberArtGenerator.WhiteTexture, new Color(0.015f, 0.022f, 0.038f, 0.55f));
            if (_vignette != null)
                ArtUI.DrawTex(screen, _vignette, Color.white);

            // Slow scan sweep across the stage.
            float sweep = Mathf.Repeat(Time.unscaledTime * 0.055f, 1f);
            float sy = _stage.y + _stage.height * (0.04f + sweep * 0.92f);
            ArtUI.DrawTex(new Rect(_stage.x, sy, _stage.width, Mathf.Max(1f, _scale)),
                CyberArtGenerator.WhiteTexture, new Color(0.35f, 0.92f, 1f, 0.05f));
        }

        private void DrawBrackets()
        {
            float u = _scale;
            float len = 26f * u;
            float t = Mathf.Max(1f, 1.5f * u);
            Color c = new Color(0.30f, 0.85f, 1f, 0.26f);
            Texture2D white = CyberArtGenerator.WhiteTexture;
            Rect s = _stage;

            ArtUI.DrawTex(new Rect(s.x, s.y, len, t), white, c);
            ArtUI.DrawTex(new Rect(s.x, s.y, t, len), white, c);

            ArtUI.DrawTex(new Rect(s.xMax - len, s.y, len, t), white, c);
            ArtUI.DrawTex(new Rect(s.xMax - t, s.y, t, len), white, c);

            ArtUI.DrawTex(new Rect(s.x, s.yMax - t, len, t), white, c);
            ArtUI.DrawTex(new Rect(s.x, s.yMax - len, t, len), white, c);

            ArtUI.DrawTex(new Rect(s.xMax - len, s.yMax - t, len, t), white, c);
            ArtUI.DrawTex(new Rect(s.xMax - t, s.yMax - len, t, len), white, c);
        }

        private void DrawHeader()
        {
            float u = _scale;
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.3f));

            float dotSize = Mathf.Max(5f, 6f * u);
            Rect dotRect = new Rect(_stage.x + 2f * u, _stage.y + 10f * u, dotSize, dotSize);
            ArtUI.DrawTex(dotRect, _dot, new Color(0.40f, 1f, 0.55f, pulse));

            _small.alignment = TextAnchor.MiddleLeft;
            _small.fontSize = Mathf.RoundToInt(11f * u);
            ArtUI.Text(new Rect(dotRect.xMax + 7f * u, _stage.y + 6f * u, Mathf.Max(80f, 420f * u), 20f * u),
                "ARENA NETWORK   //   S07 ONLINE", _small, new Color(0.52f, 0.70f, 0.80f), u);

            float btn = Mathf.Max(40f, 46f * u);
            Rect btnRect = new Rect(_stage.xMax - btn, _stage.y + 2f * u, btn, btn);
            bool muted = CyberAudioSynthesizer.Instance.IsMuted;
            if (RoundButton(btnRect, "button_cyan", muted ? "icon_muted" : "icon_sound", muted))
            {
                ToggleMute();
                PlayClick();
            }
        }

        private void DrawFooter()
        {
            float u = _scale;
            float y = _wide ? _stage.y + 664f * u : _stage.y + 910f * u;

            _small.alignment = TextAnchor.MiddleLeft;
            _small.fontSize = Mathf.RoundToInt(10f * u);
            ArtUI.Text(new Rect(_stage.x + 4f * u, y, _stage.width * 0.6f, 18f * u),
                "CYBER ORBS   //   ARENA BUILD 1.0", _small, new Color(0.40f, 0.52f, 0.64f), u);

            if (_wide)
            {
                _small.alignment = TextAnchor.MiddleRight;
                ArtUI.Text(new Rect(_stage.x + _stage.width * 0.4f - 4f * u, y, _stage.width * 0.6f, 18f * u),
                    "SIX CORES. ONE ARENA.", _small, new Color(0.40f, 0.52f, 0.64f), u);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Title block
        // ---------------------------------------------------------------------------------------

        private void DrawTitleBlock()
        {
            float u = _scale;
            float x = _wide ? _stage.x + 4f * u : _stage.x + 18f * u;
            float w = _wide ? 620f * u : _stage.width - 36f * u;
            TextAnchor align = _wide ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

            float titleY = _wide ? _stage.y + 96f * u : _stage.y + 72f * u;
            float lineH = (_wide ? 100f : 70f) * u;

            // Soft arena halo behind the wordmark.
            if (_ring != null)
            {
                float ringSize = (_wide ? 560f : 430f) * u;
                float ringCx = _wide ? x + 250f * u : _stage.center.x;
                ArtUI.DrawTex(new Rect(ringCx - ringSize * 0.5f, titleY + lineH - ringSize * 0.5f, ringSize, ringSize),
                    _ring, new Color(0.28f, 0.90f, 1f, 0.05f));
            }

            _kicker.alignment = align;
            _kicker.fontSize = Mathf.RoundToInt((_wide ? 15f : 13f) * u);
            Rect kickerRect = new Rect(x, _wide ? _stage.y + 74f * u : _stage.y + 52f * u, w, 20f * u);
            ArtUI.Text(kickerRect, "SEASON 07   //   NEO-KYOTO ARENA", _kicker, new Color(0.55f, 0.90f, 1f, 0.92f), u);

            _title.alignment = align;
            _title.fontSize = Mathf.RoundToInt((_wide ? 92f : 64f) * u);
            DrawChromaTitle(new Rect(x, titleY, w, lineH), "CYBER", Color.white);
            DrawChromaTitle(new Rect(x, titleY + lineH, w, lineH), "ORBS", Cyan);

            // Divider: two cyan rails and a magenta pip.
            float divY = _wide ? _stage.y + 302f * u : _stage.y + 220f * u;
            float divW = (_wide ? 320f : 190f) * u;
            float divX = _wide ? x : _stage.center.x - divW * 0.5f;
            Texture2D white = CyberArtGenerator.WhiteTexture;
            ArtUI.DrawTex(new Rect(divX, divY, divW * 0.42f, Mathf.Max(1f, 1.5f * u)), white, new Color(0.30f, 0.90f, 1f, 0.55f));
            ArtUI.DrawTex(new Rect(divX + divW * 0.58f, divY, divW * 0.42f, Mathf.Max(1f, 1.5f * u)), white, new Color(0.30f, 0.90f, 1f, 0.55f));
            ArtUI.DrawTex(new Rect(divX + divW * 0.5f - 3f * u, divY - 2.4f * u, 6f * u, 6f * u), white, Magenta);

            _tagline.alignment = align;
            _tagline.fontSize = Mathf.RoundToInt((_wide ? 19f : 15f) * u);
            float tagY = _wide ? _stage.y + 318f * u : _stage.y + 234f * u;
            ArtUI.Text(new Rect(x, tagY, w, 24f * u),
                "SIX ELEMENTAL CORES. ONE RAILGUN. ZERO MERCY.", _tagline, TextBlue, u);
            ArtUI.Text(new Rect(x, tagY + (_wide ? 25f : 21f) * u, w, 24f * u),
                "POP, CHAIN AND AVALANCHE YOUR WAY UP THE RANKS.", _tagline,
                new Color(0.62f, 0.78f, 0.90f), u);

            _small.alignment = align;
            _small.fontSize = Mathf.RoundToInt(11f * u);
            ArtUI.Text(new Rect(x, _wide ? _stage.y + 366f * u : _stage.y + 286f * u, w, 18f * u),
                "ARSENAL   //   SIX ELEMENTAL CORES", _small, LabelBlue, u);
        }

        private void DrawChromaTitle(Rect r, string text, Color main)
        {
            float u = _scale;
            RawText(new Rect(r.x + 3f * u, r.y + 3f * u, r.width, r.height), text, _title, new Color(0f, 0f, 0f, 0.55f));
            RawText(new Rect(r.x - 2.2f * u, r.y, r.width, r.height), text, _title, new Color(1f, 0.30f, 0.85f, 0.30f));
            RawText(new Rect(r.x + 2.2f * u, r.y, r.width, r.height), text, _title, new Color(0.15f, 0.90f, 1f, 0.36f));
            RawText(r, text, _title, main);
        }

        private void DrawShowcase()
        {
            float u = _scale;
            bool names = _wide;
            float size = (_wide ? 84f : 66f) * u;
            float gap = size * 0.14f;
            float rowW = ShowcaseOrder.Length * size + (ShowcaseOrder.Length - 1) * gap;
            float cx = _wide ? _stage.x + 4f * u + 620f * u * 0.5f : _stage.center.x;
            float y = _wide ? _stage.y + 390f * u : _stage.y + 304f * u;

            for (int i = 0; i < ShowcaseOrder.Length; i++)
            {
                Sprite sp = CyberArtGenerator.GetOrbSprite(ShowcaseOrder[i]);
                if (sp == null || sp.texture == null) continue;

                float bob = Mathf.Sin(Time.unscaledTime * 1.6f + i * 1.15f) * size * 0.045f;
                float x = cx - rowW * 0.5f + i * (size + gap);
                Rect orb = new Rect(x, y + bob, size, size);

                float squash = 1f - bob / (size * 1.6f);
                ArtUI.DrawTex(new Rect(orb.x + size * 0.14f, y + size * 1.02f, size * 0.72f * squash, Mathf.Max(2f, 5f * u)),
                    CyberArtGenerator.WhiteTexture, new Color(0f, 0f, 0f, 0.35f));
                ArtUI.DrawTex(orb, sp.texture, Color.white);

                if (names)
                {
                    _centerLabel.fontSize = Mathf.RoundToInt(11f * u);
                    ArtUI.Text(new Rect(x, y + size + 5f * u, size, 16f * u),
                        CyberArtGenerator.GetOrbName(ShowcaseOrder[i]), _centerLabel, LabelBlue, u);
                }
            }

            if (_wide)
            {
                _label.fontSize = Mathf.RoundToInt(11f * u);
                ArtUI.Text(new Rect(_stage.x + 4f * u, _stage.y + 522f * u, 620f * u, 18f * u),
                    "RIVAL PROTOCOL   //   CYBER-BOT v6", _label, new Color(0.48f, 0.62f, 0.74f), u);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Match config card
        // ---------------------------------------------------------------------------------------

        private void DrawConfigCard(Rect card)
        {
            float u = _scale;
            float pad = 26f * u;
            float innerX = card.x + pad;
            float innerW = card.width - pad * 2f;
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 1.8f));

            // Offset silhouette reads as a drop shadow on the art's rounded corners.
            ArtUI.DrawPanel(new Rect(card.x + 5f * u, card.y + 8f * u, card.width, card.height), u, new Color(0f, 0f, 0f, 0.55f));
            ArtUI.DrawPanel(card, u, Color.white);
            ArtUI.DrawTex(new Rect(card.x + 18f * u, card.y + 1f * u, card.width - 36f * u, Mathf.Max(1f, 2f * u)),
                CyberArtGenerator.WhiteTexture, new Color(0.30f, 0.95f, 1f, 0.30f * pulse));

            // Header (the help modal re-centres this shared style, so set it every frame)
            _cardTitle.alignment = TextAnchor.MiddleLeft;
            _cardTitle.fontSize = Mathf.RoundToInt(16f * u);
            ArtUI.Text(new Rect(innerX, card.y + 26f * u, innerW * 0.6f, 22f * u), "MATCH CONFIG", _cardTitle, Cyan, u);

            float dotSize = Mathf.Max(6f, 8f * u);
            ArtUI.DrawTex(new Rect(card.xMax - pad - 54f * u, card.y + 31f * u, dotSize, dotSize),
                _dot, new Color(0.40f, 1f, 0.55f, pulse));
            _small.alignment = TextAnchor.MiddleRight;
            _small.fontSize = Mathf.RoundToInt(11f * u);
            ArtUI.Text(new Rect(card.xMax - pad - 44f * u, card.y + 26f * u, 44f * u, 20f * u), "LIVE", _small, Online, u);

            // Mode selector
            float modeY = card.y + (_wide ? 62f : 66f) * u;
            float modeH = (_wide ? 54f : 64f) * u;
            float segGap = 10f * u;
            float segW = (innerW - segGap) * 0.5f;

            if (SegmentButton(new Rect(innerX, modeY, segW, modeH), "pill_cyan", "90s BLITZ", SelectedTimedMode))
            {
                SelectedTimedMode = true;
                PlayClick();
            }
            if (SegmentButton(new Rect(innerX + segW + segGap, modeY, segW, modeH), "pill_blue", "ENDLESS", !SelectedTimedMode))
            {
                SelectedTimedMode = false;
                PlayClick();
            }

            _small.alignment = TextAnchor.MiddleLeft;
            _small.fontSize = Mathf.RoundToInt(11.5f * u);
            string modeDesc = SelectedTimedMode
                ? "90-SECOND SCORE ATTACK   //   COMBO ADDS TIME"
                : "NO CLOCK   //   THE STACK NEVER STOPS";
            ArtUI.Text(new Rect(innerX + 2f * u, card.y + (_wide ? 124f : 136f) * u, innerW - 4f * u, 20f * u),
                modeDesc, _small, new Color(0.55f, 0.72f, 0.84f), u);

            // Primary action with a breathing halo
            float startY = card.y + (_wide ? 158f : 170f) * u;
            float startH = (_wide ? 70f : 84f) * u;
            Rect startRect = new Rect(innerX, startY, innerW, startH);

            float breath = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.1f);
            float grow = (3f + 2f * breath) * u;
            DrawPillRaw(new Rect(startRect.x - grow, startRect.y - grow, startRect.width + grow * 2f, startRect.height + grow * 2f),
                "pill_green", new Color(0.45f, 1f, 0.62f, 0.05f + 0.05f * breath));

            if (ArtUI.PillButton(startRect, "pill_green", "START MATCH", _pill, u))
            {
                BeginLaunch();
            }

            float helpY = card.y + (_wide ? 240f : 264f) * u;
            float helpH = (_wide ? 50f : 64f) * u;
            if (ArtUI.PillButton(new Rect(innerX, helpY, innerW, helpH), "pill_magenta", "HOW TO PLAY", _pill, u))
            {
                PlayClick();
                _showHelp = true;
            }

            // Best score strip
            float divY = card.y + (_wide ? 308f : 340f) * u;
            ArtUI.DrawTex(new Rect(innerX, divY, innerW, Mathf.Max(1f, u)),
                CyberArtGenerator.WhiteTexture, new Color(0.40f, 0.65f, 0.80f, 0.25f));

            float bestY = card.y + (_wide ? 322f : 356f) * u;
            float badgeSize = 48f * u;
            Rect badge = new Rect(innerX, bestY, badgeSize, badgeSize);
            ArtUI.DrawTex(new Rect(badge.x, badge.y + 2f * u, badge.width, badge.height), _badge, new Color(0f, 0f, 0f, 0.40f));
            ArtUI.DrawTex(badge, _badge, Color.white);
            _cardValue.alignment = TextAnchor.MiddleCenter;
            _cardValue.fontSize = Mathf.RoundToInt(badgeSize * 0.30f);
            RawText(new Rect(badge.x + badge.width * 0.2f, badge.y + badge.height * 0.31f, badge.width * 0.6f, badge.height * 0.38f),
                TierNumeral(_bestTier), _cardValue, TierColor(_bestTier));

            _small.alignment = TextAnchor.MiddleLeft;
            _small.fontSize = Mathf.RoundToInt(12f * u);
            ArtUI.Text(new Rect(badge.xMax + 12f * u, bestY + 1f * u, innerW - badgeSize - 130f * u, 16f * u),
                "PERSONAL BEST", _small, LabelBlue, u);
            DrawLed(new Rect(badge.xMax + 12f * u, bestY + 18f * u, 150f * u, 28f * u),
                Mathf.Clamp(_best, 0, 999999).ToString("D6"), 1f);

            _small.alignment = TextAnchor.MiddleRight;
            ArtUI.Text(new Rect(card.xMax - pad - 112f * u, bestY + 1f * u, 112f * u, 16f * u),
                "BEST RANK", _small, LabelBlue, u);
            _cardValue.alignment = TextAnchor.MiddleRight;
            _cardValue.fontSize = Mathf.RoundToInt(16f * u);
            ArtUI.Text(new Rect(card.xMax - pad - 112f * u, bestY + 18f * u, 112f * u, 24f * u),
                _bestTier, _cardValue, TierColor(_bestTier), u);

            _small.alignment = TextAnchor.MiddleCenter;
            _small.fontSize = Mathf.RoundToInt(11f * u);
            ArtUI.Text(new Rect(innerX, card.y + (_wide ? 398f : 430f) * u, innerW, 18f * u),
                "ENTER / TAP TO DEPLOY", _small, new Color(0.45f, 0.60f, 0.72f), u);
        }

        private bool SegmentButton(Rect r, string face, string label, bool selected)
        {
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (Event.current.type != EventType.Repaint) return clicked;

            bool hover = r.Contains(Event.current.mousePosition);
            float s = hover ? 1.02f : 1f;
            Rect dr = new Rect(r.center.x - r.width * s * 0.5f, r.center.y - r.height * s * 0.5f,
                r.width * s, r.height * s);

            Color tint = selected ? Color.white : new Color(0.48f, 0.54f, 0.62f, 1f);
            DrawPillRaw(new Rect(dr.x, dr.y + 3f * _scale, dr.width, dr.height), face, new Color(0f, 0f, 0f, 0.35f));
            DrawPillRaw(dr, face, tint);

            _pill.fontSize = Mathf.RoundToInt(dr.height * 0.40f);
            ArtUI.Text(new Rect(dr.x, dr.y - dr.height * 0.03f, dr.width, dr.height), label, _pill,
                selected ? Color.white : new Color(0.78f, 0.85f, 0.92f, 1f), _scale);
            return clicked;
        }

        private void DrawPillRaw(Rect r, string face, Color tint)
        {
            Texture2D tex = CyberArtGenerator.LoadArtTexture(face);
            if (tex == null) return;
            float border = ArtUI.PillTexBorder * (r.height / Mathf.Max(1f, tex.height));
            ArtUI.DrawSliced(r, tex, ArtUI.PillTexBorder, border, tint);
        }

        private void DrawLed(Rect area, string text, float alpha)
        {
            if (Event.current.type != EventType.Repaint || _digits == null) return;

            float h = area.height;
            float digitW = h * (64f / 96f);
            float gap = h * 0.05f;

            float total = 0f;
            for (int i = 0; i < text.Length; i++)
                total += digitW + (i < text.Length - 1 ? gap : 0f);

            float s = total > area.width ? area.width / total : 1f;
            h *= s;
            digitW *= s;
            gap *= s;
            total *= s;

            float x = area.x + (area.width - total) * 0.5f;
            float y = area.y + (area.height - h) * 0.5f;
            Color tint = new Color(1f, 1f, 1f, alpha);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c < '0' || c > '9') continue;
                Texture2D tex = _digits[c - '0'];
                if (tex != null)
                    ArtUI.DrawTex(new Rect(x, y, digitW, h), tex, tint);
                x += digitW + gap;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Help overlay
        // ---------------------------------------------------------------------------------------

        private void DrawHelpModal(Rect screen, Rect safe)
        {
            float u = _scale;
            ArtUI.DrawTex(screen, CyberArtGenerator.WhiteTexture, new Color(0.005f, 0.008f, 0.015f, 0.86f));

            float s = ResponsiveLayout.FitScale(470f, 496f, 18f, 0.45f, 1.25f);
            float w = 470f * s;
            float h = 496f * s;
            Rect r = ResponsiveLayout.Centered(w, h);

            ArtUI.DrawPanel(r, s, Color.white);
            ArtUI.DrawTex(new Rect(r.x + 16f * s, r.y + 1f * s, r.width - 32f * s, Mathf.Max(1f, 2f * s)),
                CyberArtGenerator.WhiteTexture, new Color(0.30f, 0.95f, 1f, 0.45f));

            _cardTitle.alignment = TextAnchor.MiddleCenter;
            _cardTitle.fontSize = Mathf.RoundToInt(28f * s);
            ArtUI.Text(new Rect(r.x, r.y + 18f * s, r.width, 34f * s), "HOW TO PLAY", _cardTitle, Cyan, s);

            _small.alignment = TextAnchor.MiddleCenter;
            _small.fontSize = Mathf.RoundToInt(11f * s);
            ArtUI.Text(new Rect(r.x, r.y + 52f * s, r.width, 18f * s), "COMBAT MANUAL   //   v1.0", _small, LabelBlue, s);

            string[] keys = { "AIM", "SWAP", "MATCH", "AVALANCHE", "SHIELD", "CORES" };
            string[] body =
            {
                "Point with mouse or touch, then click to fire. Arrow keys and Space work too.",
                "Right-click, press S or Tab to swap the loaded core with the next chamber.",
                "Connect three or more matching cores to shatter the whole cluster.",
                "Cut a cluster loose from the ceiling for a heavy fall bonus.",
                "Misses drain shield cells. At zero a fresh row drops into the stack.",
                "Fire, Storm, Circuit, Solar, Void and Crystal. All six hit hard."
            };

            float rowY = r.y + 82f * s;
            float rowH = 54f * s;
            for (int i = 0; i < keys.Length; i++)
            {
                ArtUI.DrawTex(new Rect(r.x + 24f * s, rowY + rowH - Mathf.Max(1f, s), r.width - 48f * s, Mathf.Max(1f, s)),
                    CyberArtGenerator.WhiteTexture, new Color(0.40f, 0.62f, 0.78f, 0.14f));

                _label.alignment = TextAnchor.MiddleLeft;
                _label.fontSize = Mathf.RoundToInt(13f * s);
                ArtUI.Text(new Rect(r.x + 24f * s, rowY + 4f * s, 92f * s, rowH - 8f * s), keys[i], _label, Magenta, s);

                _body.fontSize = Mathf.RoundToInt(13f * s);
                GUI.Label(new Rect(r.x + 120f * s, rowY + 4f * s, r.width - 144f * s, rowH - 8f * s), body[i], _body);
                rowY += rowH;
            }

            if (ArtUI.PillButton(new Rect(r.x + 44f * s, r.yMax - 58f * s, r.width - 88f * s, 46f * s),
                "pill_cyan", "BACK TO ARENA", _pill, s))
            {
                PlayClick();
                _showHelp = false;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Small helpers
        // ---------------------------------------------------------------------------------------

        private bool RoundButton(Rect r, string face, string icon, bool dim)
        {
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (Event.current.type != EventType.Repaint) return clicked;

            bool hover = r.Contains(Event.current.mousePosition);
            bool down = hover && Input.GetMouseButton(0);
            float s = down ? 0.92f : (hover ? 1.08f : 1f);
            float cx = r.center.x;
            float cy = r.center.y + (down ? 1.5f * _scale : 0f);
            Rect dr = new Rect(cx - r.width * s * 0.5f, cy - r.height * s * 0.5f, r.width * s, r.height * s);

            Texture2D faceTex = CyberArtGenerator.LoadArtTexture(face);
            Texture2D iconTex = CyberArtGenerator.LoadArtTexture(icon);

            if (!down)
                ArtUI.DrawTex(new Rect(dr.x, dr.y + 3f * _scale, dr.width, dr.height), faceTex, new Color(0f, 0f, 0f, 0.40f));

            ArtUI.DrawTex(dr, faceTex, dim ? new Color(0.62f, 0.66f, 0.74f, 1f) : Color.white);

            float iconSize = dr.width * 0.58f;
            ArtUI.DrawTex(new Rect(dr.center.x - iconSize * 0.5f, dr.center.y - iconSize * 0.5f - dr.height * 0.01f, iconSize, iconSize),
                iconTex, new Color(1f, 1f, 1f, dim ? 0.80f : 1f));
            return clicked;
        }

        private void RawText(Rect r, string text, GUIStyle style, Color color)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color old = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        private static void PlayClick()
        {
            CyberAudioSynthesizer.Instance.Play("click");
        }

        private static void ToggleMute()
        {
            CyberAudioSynthesizer.Instance.IsMuted = !CyberAudioSynthesizer.Instance.IsMuted;
        }

        private static string TierFor(int score)
        {
            if (score >= 12000) return "ELITE";
            if (score >= 7500) return "MASTER";
            if (score >= 4000) return "VANGUARD";
            if (score >= 1500) return "OPERATIVE";
            return "NOVICE";
        }

        private static string TierNumeral(string tier)
        {
            switch (tier)
            {
                case "OPERATIVE": return "I";
                case "VANGUARD":  return "II";
                case "MASTER":    return "III";
                case "ELITE":     return "IV";
                default:          return "-";
            }
        }

        private static Color TierColor(string tier)
        {
            switch (tier)
            {
                case "OPERATIVE": return new Color(0.50f, 0.96f, 0.58f, 1f);
                case "VANGUARD":  return new Color(0.45f, 0.90f, 1.00f, 1f);
                case "MASTER":    return new Color(1.00f, 0.78f, 0.34f, 1f);
                case "ELITE":     return new Color(1.00f, 0.55f, 0.86f, 1f);
                default:          return new Color(0.78f, 0.86f, 0.95f, 1f);
            }
        }

        /// <summary>Fades to black, then hands control to the game controller.</summary>
        private void BeginLaunch()
        {
            if (_launching) return;
            _launching = true;
            _launchAt = Time.unscaledTime;
            PlayClick();
        }

        private void startGame()
        {
            if (this.startGameDelegate != null)
                this.startGameDelegate();
        }
    }
}
