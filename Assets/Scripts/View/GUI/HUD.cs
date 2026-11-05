using UnityEngine;
using System;
using System.Collections;

namespace com.javierquevedo
{
    /// <summary>
    /// In-game HUD. Everything on screen is drawn from the authored PNGs in Resources/Art/Sprites
    /// (9-sliced steel panels, LED digit sprites, battery cells, rank badge, round buttons);
    /// only the small text labels come from the Rajdhani font.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public Game game;
        public Action OnRestartRequested;
        public Action OnSwapRequested;
        public Action OnReturnToLobbyRequested;

        private bool _showInfoModal = false;
        private static Rect _lastTopBarRect;
        private static Rect _lastButtonsRect;
        private static Rect _lastDeckRect;
        private static bool _modalOpen;

        public static bool IsScreenPointOverHUD(Vector3 screenPosBottomLeft)
        {
            if (_modalOpen) return true;
            // Convert bottom-left screen coordinates to top-left GUI coordinates
            Vector2 guiPt = new Vector2(screenPosBottomLeft.x, Screen.height - screenPosBottomLeft.y);
            return _lastTopBarRect.Contains(guiPt) || _lastButtonsRect.Contains(guiPt) || _lastDeckRect.Contains(guiPt);
        }

        private static readonly Color LabelBlue = ArtUI.LabelBlue;
        private static readonly Color Cyan = ArtUI.Cyan;
        private static readonly Color Danger = ArtUI.Danger;
        private static readonly Color Magenta = ArtUI.Magenta;

        private static readonly string[] Tones = { "cyan", "amber", "red" };
        private const int ToneCyan = 0, ToneAmber = 1, ToneRed = 2;

        private GUIStyle _label;
        private GUIStyle _labelBold;
        private GUIStyle _heading;
        private GUIStyle _body;
        private GUIStyle _pillText;
        private bool _stylesInitialized = false;

        private Texture2D _badgeTex;
        private Texture2D _batteryFull;
        private Texture2D _batteryEmpty;
        private Texture2D[][] _digits;
        private Texture2D[] _colons;

        private float _ui = 1f;
        private float _displayScore;
        private float _matchOpenedAt;
        private int _lastTier = -1;
        private float _tierPunchStart = -10f;

        void Start()
        {
            _matchOpenedAt = Time.unscaledTime;
        }

        void Update()
        {
            if (game != null)
            {
                // Odometer-style count-up so a big combo reads as a payoff instead of a jump cut.
                if (game.score < _displayScore)
                    _displayScore = game.score;
                else
                    _displayScore = Mathf.MoveTowards(_displayScore, game.score,
                        Mathf.Max(80f, (game.score - _displayScore) * 6f) * Time.unscaledDeltaTime);

                int tier = TierIndex(game.tierName);
                if (tier != _lastTier)
                {
                    if (_lastTier >= 0 && tier > _lastTier) _tierPunchStart = Time.unscaledTime;
                    _lastTier = tier;
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                CyberAudioSynthesizer.Instance.IsMuted = !CyberAudioSynthesizer.Instance.IsMuted;
                CyberAudioSynthesizer.Instance.Play("click");
            }
            if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (game != null && game.state == GameState.Playing)
                {
                    game.isPaused = !game.isPaused;
                    CyberAudioSynthesizer.Instance.Play("click");
                }
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                _showInfoModal = !_showInfoModal;
                if (game != null) game.isPaused = _showInfoModal;
                CyberAudioSynthesizer.Instance.Play("click");
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (OnRestartRequested != null)
                {
                    CyberAudioSynthesizer.Instance.Play("click");
                    OnRestartRequested();
                }
            }
        }

        private static int TierIndex(string tierName)
        {
            switch (tierName)
            {
                case "OPERATIVE": return 1;
                case "VANGUARD":  return 2;
                case "MASTER":    return 3;
                case "ELITE":     return 4;
                default:          return 0;
            }
        }

        private static Color TierColor(int tier)
        {
            switch (tier)
            {
                case 1:  return new Color(0.50f, 0.96f, 0.58f, 1f);
                case 2:  return new Color(0.45f, 0.90f, 1.00f, 1f);
                case 3:  return new Color(1.00f, 0.78f, 0.34f, 1f);
                case 4:  return new Color(1.00f, 0.55f, 0.86f, 1f);
                default: return new Color(0.78f, 0.86f, 0.95f, 1f);
            }
        }

        private static readonly string[] Numerals = { "I", "II", "III", "IV", "V" };

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;

            _label = ArtUI.MakeLabel(false, TextAnchor.MiddleCenter);
            _labelBold = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _heading = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _pillText = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);

            _body = ArtUI.MakeLabel(false, TextAnchor.UpperLeft);
            _body.wordWrap = true;
            _body.normal.textColor = new Color(0.80f, 0.90f, 0.97f, 1f);

            _badgeTex = CyberArtGenerator.LoadArtTexture("rank_badge");
            _batteryFull = CyberArtGenerator.LoadArtTexture("battery_full");
            _batteryEmpty = CyberArtGenerator.LoadArtTexture("battery_empty");

            _digits = new Texture2D[Tones.Length][];
            _colons = new Texture2D[Tones.Length];
            for (int t = 0; t < Tones.Length; t++)
            {
                _digits[t] = new Texture2D[10];
                for (int d = 0; d < 10; d++)
                    _digits[t][d] = CyberArtGenerator.LoadArtTexture("digit_" + Tones[t] + "_" + d);
                _colons[t] = CyberArtGenerator.LoadArtTexture("colon_" + Tones[t]);
            }
        }

        // ---------------------------------------------------------------------------------------
        // Drawing helpers
        // ---------------------------------------------------------------------------------------

        private void DrawPanel(Rect r, Color tint)
        {
            ArtUI.DrawPanel(r, _ui, tint);
        }

        private void DrawTex(Rect r, Texture2D tex, Color tint)
        {
            ArtUI.DrawTex(r, tex, tint);
        }

        private void Text(Rect r, string text, GUIStyle style, Color color)
        {
            ArtUI.Text(r, text, style, color, _ui);
        }

        private void DrawLed(Rect area, string text, int tone, float alpha)
        {
            if (Event.current.type != EventType.Repaint || _digits == null) return;

            float h = area.height;
            float digitW = h * (64f / 96f);
            float colonW = h * (32f / 96f);
            float gap = h * 0.05f;

            float total = 0f;
            for (int i = 0; i < text.Length; i++)
                total += (text[i] == ':' ? colonW : digitW) + (i < text.Length - 1 ? gap : 0f);

            float scale = total > area.width ? area.width / total : 1f;
            h *= scale; digitW *= scale; colonW *= scale; gap *= scale; total *= scale;

            float x = area.x + (area.width - total) * 0.5f;
            float y = area.y + (area.height - h) * 0.5f;
            Color tint = new Color(1f, 1f, 1f, alpha);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ':')
                {
                    DrawTex(new Rect(x, y, colonW, h), _colons[tone], tint);
                    x += colonW + gap;
                }
                else if (c >= '0' && c <= '9')
                {
                    DrawTex(new Rect(x, y, digitW, h), _digits[tone][c - '0'], tint);
                    x += digitW + gap;
                }
            }
        }

        private void DrawBattery(Rect r, bool charged, float alpha)
        {
            DrawTex(r, charged ? _batteryFull : _batteryEmpty, new Color(1f, 1f, 1f, alpha));
        }

        /// <summary>Round sprite button with hover swell, press squash and a soft drop shadow.</summary>
        private bool DrawArtButton(Rect r, string face, string icon, bool dim)
        {
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (Event.current.type != EventType.Repaint) return clicked;

            bool hover = r.Contains(Event.current.mousePosition);
            bool down = hover && Input.GetMouseButton(0);
            float s = down ? 0.92f : (hover ? 1.08f : 1f);
            float cx = r.center.x;
            float cy = r.center.y + (down ? 1.5f * _ui : 0f);
            Rect dr = new Rect(cx - r.width * s * 0.5f, cy - r.height * s * 0.5f, r.width * s, r.height * s);

            Texture2D faceTex = CyberArtGenerator.LoadArtTexture(face);
            Texture2D iconTex = CyberArtGenerator.LoadArtTexture(icon);

            if (!down)
                DrawTex(new Rect(dr.x, dr.y + 3f * _ui, dr.width, dr.height), faceTex, new Color(0f, 0f, 0f, 0.4f));

            Color faceTint = dim ? new Color(0.62f, 0.66f, 0.74f, 1f) : Color.white;
            DrawTex(dr, faceTex, faceTint);

            float iconSize = dr.width * 0.58f;
            Rect ir = new Rect(dr.center.x - iconSize * 0.5f, dr.center.y - iconSize * 0.5f - dr.height * 0.01f, iconSize, iconSize);
            DrawTex(ir, iconTex, new Color(1f, 1f, 1f, dim ? 0.8f : 1f));
            return clicked;
        }

        /// <summary>Sliced pill button with a text label.</summary>
        private bool DrawPillButton(Rect r, string face, string label)
        {
            return ArtUI.PillButton(r, face, label, _pillText, _ui);
        }

        // ---------------------------------------------------------------------------------------
        // HUD
        // ---------------------------------------------------------------------------------------

        void OnGUI()
        {
            if (game == null) return;
            EnsureStyles();

            float sw = Screen.width;
            float sh = Screen.height;
            Rect safe = ResponsiveLayout.SafeAreaGui;
            float uiScale = ResponsiveLayout.FitScale(540f, 920f, 12f, 0.60f, 1.45f);
            _ui = uiScale;
            _lastDeckRect = new Rect(0f, 0f, 0f, 0f);

            // Top tournament bar. Tall enough for the panel bevel plus three text rows.
            float sideMargin = 8f * uiScale;
            float topBarW = Mathf.Min(safe.width - sideMargin * 2f, 680f * uiScale);
            float topBarH = 82f * uiScale;
            float topBarX = safe.x + (safe.width - topBarW) * 0.5f;
            float topBarY = safe.y + 8f * uiScale;
            _lastTopBarRect = new Rect(topBarX - 6f, 0f, topBarW + 12f, topBarY + topBarH + 6f);
            _modalOpen = _showInfoModal || (game != null && (game.isPaused || game.state != GameState.Playing));

            float pad = 6f * uiScale;

            // 1. Score readout
            float scoreW = topBarW * 0.28f;
            Rect scoreRect = new Rect(topBarX, topBarY, scoreW, topBarH);
            DrawPanel(scoreRect, Color.white);

            _label.fontSize = Mathf.RoundToInt(11f * uiScale);
            Text(new Rect(scoreRect.x, scoreRect.y + 12f * uiScale, scoreRect.width, 14f * uiScale), "SCORE", _label, LabelBlue);

            int shownScore = Mathf.Min(9999999, Mathf.RoundToInt(_displayScore));
            DrawLed(new Rect(scoreRect.x + 12f * uiScale, scoreRect.y + 28f * uiScale, scoreRect.width - 24f * uiScale, 27f * uiScale),
                shownScore.ToString("D7"), ToneCyan, 1f);

            _label.fontSize = Mathf.RoundToInt(10f * uiScale);
            Text(new Rect(scoreRect.x, scoreRect.y + 58f * uiScale, scoreRect.width, 14f * uiScale),
                "RIVAL  " + game.opponentScore.ToString("N0"), _label, LabelBlue);

            // 2. Clock
            float timerW = topBarW * 0.19f;
            Rect timerRect = new Rect(scoreRect.xMax + pad, topBarY, timerW, topBarH);
            DrawPanel(timerRect, Color.white);

            _label.fontSize = Mathf.RoundToInt(11f * uiScale);
            Text(new Rect(timerRect.x, timerRect.y + 12f * uiScale, timerRect.width, 14f * uiScale),
                game.isTimedMode ? "CLOCK" : "ELAPSED", _label, LabelBlue);

            string clockStr;
            int clockTone;
            float clockAlpha = 1f;
            if (game.isTimedMode)
            {
                int secs = Mathf.CeilToInt(game.timeRemaining);
                clockStr = (secs / 60).ToString("D2") + ":" + (secs % 60).ToString("D2");
                bool urgent = secs <= 15;
                clockTone = urgent ? ToneRed : ToneAmber;
                if (urgent) clockAlpha = 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f));
            }
            else
            {
                TimeSpan ts = TimeSpan.FromSeconds(game.timeElapsed);
                clockStr = ts.Minutes.ToString("D2") + ":" + ts.Seconds.ToString("D2");
                clockTone = ToneCyan;
            }
            DrawLed(new Rect(timerRect.x + 10f * uiScale, timerRect.y + 28f * uiScale, timerRect.width - 20f * uiScale, 27f * uiScale),
                clockStr, clockTone, clockAlpha);

            _label.fontSize = Mathf.RoundToInt(10f * uiScale);
            Text(new Rect(timerRect.x, timerRect.y + 58f * uiScale, timerRect.width, 14f * uiScale),
                "ACC  " + game.accuracyPercent + "%", _label, LabelBlue);

            // 3. Combo + shield cells
            float comboW = topBarW * 0.23f;
            Rect comboRect = new Rect(timerRect.xMax + pad, topBarY, comboW, topBarH);
            bool hot = game.combo >= 2;
            DrawPanel(comboRect, hot ? new Color(1f, 0.82f, 0.95f, 1f) : Color.white);

            _labelBold.fontSize = Mathf.RoundToInt(16f * uiScale);
            Text(new Rect(comboRect.x, comboRect.y + 10f * uiScale, comboRect.width, 20f * uiScale),
                "x" + game.combo + " COMBO", _labelBold, hot ? Magenta : Cyan);

            int cells = Mathf.Max(1, game.maxPowerCells);
            float cellH = 20f * uiScale;
            float cellW = cellH * 0.5f;
            float cellGap = 3.5f * uiScale;
            float cellsTotal = cells * cellW + (cells - 1) * cellGap;
            float cellX = comboRect.x + (comboRect.width - cellsTotal) * 0.5f;
            float cellY = comboRect.y + 34f * uiScale;
            bool critical = game.activePowerCells == 1;
            for (int i = 0; i < cells; i++)
            {
                bool active = i < game.activePowerCells;
                float a = 1f;
                if (active && critical) a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                DrawBattery(new Rect(cellX + i * (cellW + cellGap), cellY, cellW, cellH), active, a);
            }

            _label.fontSize = Mathf.RoundToInt(10f * uiScale);
            Text(new Rect(comboRect.x, comboRect.y + 58f * uiScale, comboRect.width, 14f * uiScale),
                "SHIELD  " + game.activePowerCells + "/" + cells, _label, critical ? Danger : LabelBlue);

            // 4. Rank badge
            float tierW = topBarW - (scoreW + timerW + comboW + 3f * pad);
            Rect tierRect = new Rect(comboRect.xMax + pad, topBarY, tierW, topBarH);
            DrawPanel(tierRect, Color.white);

            int tier = TierIndex(game.tierName);
            Color tierColor = TierColor(tier);

            float punchT = Time.unscaledTime - _tierPunchStart;
            float punch = punchT < 1f ? 1f + 0.28f * Mathf.Exp(-punchT * 6f) * Mathf.Cos(punchT * 20f) : 1f;
            float badgeSize = topBarH - 10f * uiScale;
            Rect badgeRect = new Rect(tierRect.x + 6f * uiScale, tierRect.y + 5f * uiScale, badgeSize, badgeSize);
            Rect drawnBadge = new Rect(badgeRect.center.x - badgeSize * punch * 0.5f, badgeRect.center.y - badgeSize * punch * 0.5f, badgeSize * punch, badgeSize * punch);
            DrawTex(new Rect(drawnBadge.x, drawnBadge.y + 2f * uiScale, drawnBadge.width, drawnBadge.height), _badgeTex, new Color(0f, 0f, 0f, 0.35f));
            DrawTex(drawnBadge, _badgeTex, Color.white);

            _labelBold.fontSize = Mathf.RoundToInt(drawnBadge.height * 0.30f);
            Text(new Rect(drawnBadge.x + drawnBadge.width * 0.2f, drawnBadge.y + drawnBadge.height * 0.31f, drawnBadge.width * 0.6f, drawnBadge.height * 0.38f),
                Numerals[tier], _labelBold, tierColor);

            float textX = badgeRect.xMax + 4f * uiScale;
            float textW = tierRect.xMax - textX - 8f * uiScale;
            _label.fontSize = Mathf.RoundToInt(10f * uiScale);
            Text(new Rect(textX, tierRect.y + 12f * uiScale, textW, 14f * uiScale), "RANK", _label, LabelBlue);

            _labelBold.fontSize = Mathf.RoundToInt(15f * uiScale);
            Text(new Rect(textX, tierRect.y + 26f * uiScale, textW, 22f * uiScale), game.tierName, _labelBold, tierColor);

            _label.fontSize = Mathf.RoundToInt(10f * uiScale);
            Text(new Rect(textX, tierRect.y + 58f * uiScale, textW, 14f * uiScale),
                "BEST  " + game.highScore.ToString("N0"), _label, LabelBlue);

            // 5. Round control buttons (Restart, Sound, Pause, Help, Swap)
            // Never shrink a tap target below ~40 px, even on a small phone.
            float btnSize = Mathf.Max(40f, 44f * uiScale);
            float btnGap = 8f * uiScale;
            bool placeOnRightSide = safe.width > safe.height * 0.95f; // Landscape / tablet gets a side column
            float columnHeight = 5 * btnSize + 4 * btnGap;
            if (placeOnRightSide && topBarY + topBarH + 18f * uiScale + columnHeight > safe.yMax - 4f)
                placeOnRightSide = false; // Short viewport: a row along the top is safer

            if (placeOnRightSide)
            {
                float colX = Mathf.Min(safe.xMax - btnSize - sideMargin, safe.x + (safe.width + topBarW) * 0.5f - btnSize);
                float startY = topBarY + topBarH + 18f * uiScale;
                _lastButtonsRect = new Rect(colX - 6f, startY - 6f, btnSize + 12f, columnHeight + 12f);
                DrawControlButtons(true, colX, startY, btnSize, btnGap);
            }
            else
            {
                float rowW = 5 * btnSize + 4 * btnGap;
                float startX = safe.x + (safe.width - rowW) * 0.5f;
                float rowY = topBarY + topBarH + 6f * uiScale;
                _lastButtonsRect = new Rect(startX - 6f, rowY - 6f, rowW + 12f, btnSize + 12f);
                DrawControlButtons(false, startX, rowY, btnSize, btnGap);
            }

            // Tall phones have a large dead band below the board. Fill it with a compact
            // mode / shield / control readout instead of leaving the bottom empty.
            if (safe.height > safe.width * 1.9f)
                DrawCommandDeck(topBarX, topBarW, safe, uiScale);

            // Pause or Info modal
            if (_showInfoModal)
            {
                DrawInfoModal(sw, sh);
            }
            else if (game.isPaused && game.state == GameState.Playing)
            {
                DrawPauseModal(sw, sh);
            }

            // Fade in from the launch black so a fresh match lands instead of cutting.
            float fadeT = Mathf.Clamp01((Time.unscaledTime - _matchOpenedAt) / 0.45f);
            if (fadeT < 1f)
            {
                DrawTex(new Rect(0f, 0f, sw, sh), CyberArtGenerator.WhiteTexture,
                    new Color(0.01f, 0.015f, 0.028f, 1f - Mathf.SmoothStep(0f, 1f, fadeT)));
            }
        }

        private void DrawCommandDeck(float topBarX, float topBarW, Rect safe, float u)
        {
            float deckH = Mathf.Clamp(safe.height * 0.13f, 96f, 132f);
            float deckY = safe.yMax - deckH - 6f * u;
            Rect deck = new Rect(topBarX, deckY, topBarW, deckH);
            _lastDeckRect = deck;

            DrawPanel(deck, Color.white);
            DrawTex(new Rect(deck.x + 16f * u, deck.y + 1f, deck.width - 32f * u, Mathf.Max(1f, 2f * u)),
                CyberArtGenerator.WhiteTexture, new Color(0.30f, 0.95f, 1f, 0.40f));

            int cells = Mathf.Max(1, game.maxPowerCells);
            int active = Mathf.Clamp(game.activePowerCells, 0, cells);
            bool critical = active <= 1;

            _label.alignment = TextAnchor.MiddleLeft;
            _label.fontSize = Mathf.RoundToInt(11f * u);
            ArtUI.Text(new Rect(deck.x + 16f * u, deck.y + 9f * u, deck.width * 0.66f, 16f * u),
                "COMMAND DECK   //   " + (game.isTimedMode ? "90s BLITZ" : "ENDLESS"),
                _label, LabelBlue, u);

            _label.alignment = TextAnchor.MiddleRight;
            ArtUI.Text(new Rect(deck.x + deck.width * 0.34f, deck.y + 9f * u, deck.width - 16f * u, 16f * u),
                "SHIELD  " + active + "/" + cells, _label, critical ? Danger : LabelBlue, u);

            // Timer / endurance rail.
            float railY = deck.y + Mathf.Max(30f * u, deckH * 0.28f);
            float railW = deck.width - 32f * u;
            float railH = Mathf.Max(3f, 4f * u);
            DrawTex(new Rect(deck.x + 16f * u, railY, railW, railH), CyberArtGenerator.WhiteTexture,
                new Color(0.06f, 0.10f, 0.15f, 0.95f));

            float fill = 1f;
            Color railColor = Cyan;
            if (game.isTimedMode)
            {
                fill = Mathf.Clamp01(game.timeRemaining / 90f);
                railColor = game.timeRemaining <= 15f ? Danger : Cyan;
            }
            DrawTex(new Rect(deck.x + 16f * u, railY, railW * fill, railH), CyberArtGenerator.WhiteTexture, railColor);

            // Large shield-cell readout (the same five cells, readable at a glance).
            float cellH = Mathf.Max(18f, deckH * 0.20f);
            float cellW = cellH * 0.5f;
            float cellGap = cellH * 0.16f;
            float cellsTotal = cells * cellW + (cells - 1) * cellGap;
            float cellX = deck.center.x - cellsTotal * 0.5f;
            float cellY = deck.y + Mathf.Max(45f * u, deckH * 0.40f);
            for (int i = 0; i < cells; i++)
            {
                bool on = i < active;
                float a = 1f;
                if (on && critical) a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
                DrawBattery(new Rect(cellX + i * (cellW + cellGap), cellY, cellW, cellH), on, a);
            }

            _label.alignment = TextAnchor.MiddleLeft;
            _label.fontSize = Mathf.RoundToInt(9.5f * u);
            ArtUI.Text(new Rect(deck.x + 16f * u, cellY + cellH * 0.5f - 8f * u, deck.width * 0.25f, 16f * u),
                "MISS SHIELD", _label, critical ? Danger : LabelBlue, u);

            _label.alignment = TextAnchor.MiddleRight;
            ArtUI.Text(new Rect(deck.x + deck.width * 0.75f - 16f * u, cellY + cellH * 0.5f - 8f * u, deck.width * 0.25f, 16f * u),
                "TAP TO REPAIR", _label, new Color(0.42f, 0.62f, 0.74f), u);

            _label.alignment = TextAnchor.MiddleCenter;
            _label.fontSize = Mathf.RoundToInt(10f * u);
            ArtUI.Text(new Rect(deck.x + 12f * u, deck.y + deckH - 22f * u, deck.width - 24f * u, 16f * u),
                "TAP TO AIM & FIRE   //   SWAP [S]   //   CELLS ARE MISSES",
                _label, new Color(0.48f, 0.66f, 0.78f), u);
        }

        private void DrawControlButtons(bool column, float x, float y, float size, float gap)
        {
            for (int i = 0; i < 5; i++)
            {
                Rect r = column
                    ? new Rect(x, y + i * (size + gap), size, size)
                    : new Rect(x + i * (size + gap), y, size, size);

                switch (i)
                {
                    case 0: // Restart
                        if (DrawArtButton(r, "button_green", "icon_restart", false))
                        {
                            CyberAudioSynthesizer.Instance.Play("click");
                            if (OnRestartRequested != null) OnRestartRequested();
                        }
                        break;
                    case 1: // Sound
                        bool muted = CyberAudioSynthesizer.Instance.IsMuted;
                        if (DrawArtButton(r, "button_cyan", muted ? "icon_muted" : "icon_sound", muted))
                        {
                            CyberAudioSynthesizer.Instance.IsMuted = !CyberAudioSynthesizer.Instance.IsMuted;
                            CyberAudioSynthesizer.Instance.Play("click");
                        }
                        break;
                    case 2: // Pause
                        bool paused = game != null && game.isPaused;
                        if (DrawArtButton(r, "button_yellow", paused ? "icon_play" : "icon_pause", false))
                        {
                            if (game != null && game.state == GameState.Playing)
                            {
                                game.isPaused = !game.isPaused;
                                CyberAudioSynthesizer.Instance.Play("click");
                            }
                        }
                        break;
                    case 3: // Help
                        if (DrawArtButton(r, "button_magenta", "icon_help", false))
                        {
                            _showInfoModal = !_showInfoModal;
                            if (game != null) game.isPaused = _showInfoModal;
                            CyberAudioSynthesizer.Instance.Play("click");
                        }
                        break;
                    case 4: // Swap orbs
                        if (DrawArtButton(r, "button_blue", "icon_swap", false))
                        {
                            if (OnSwapRequested != null) OnSwapRequested();
                        }
                        break;
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // Modals
        // ---------------------------------------------------------------------------------------

        private void DrawDim(float sw, float sh)
        {
            if (Event.current.type != EventType.Repaint) return;
            Color prev = GUI.color;
            GUI.color = new Color(0.01f, 0.02f, 0.05f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, sw, sh), CyberArtGenerator.WhiteTexture);
            GUI.color = prev;
        }

        private void DrawPauseModal(float sw, float sh)
        {
            DrawDim(sw, sh);

            Rect safe = ResponsiveLayout.SafeAreaGui;
            float mw = Mathf.Min(320f * _ui, safe.width - 24f);
            float mh = Mathf.Min(250f * _ui, safe.height - 24f);
            Rect r = new Rect(safe.x + (safe.width - mw) * 0.5f, safe.y + (safe.height - mh) * 0.5f, mw, mh);
            DrawPanel(r, Color.white);

            _heading.fontSize = Mathf.RoundToInt(28f * _ui);
            Text(new Rect(r.x, r.y + 16f * _ui, r.width, 36f * _ui), "PAUSED", _heading, Cyan);

            float bw = mw - 56f * _ui;
            float bh = 42f * _ui;
            float bx = r.x + 28f * _ui;

            if (DrawPillButton(new Rect(bx, r.y + 70f * _ui, bw, bh), "pill_cyan", "RESUME"))
            {
                game.isPaused = false;
                CyberAudioSynthesizer.Instance.Play("click");
            }

            string modeTxt = game.isTimedMode ? "MODE: 90s BLITZ" : "MODE: ENDLESS";
            if (DrawPillButton(new Rect(bx, r.y + 124f * _ui, bw, bh), "pill_blue", modeTxt))
            {
                game.isTimedMode = !game.isTimedMode;
                CyberAudioSynthesizer.Instance.Play("click");
            }

            if (DrawPillButton(new Rect(bx, r.y + 178f * _ui, bw, bh), "pill_green", "RESTART"))
            {
                game.isPaused = false;
                CyberAudioSynthesizer.Instance.Play("click");
                if (OnRestartRequested != null) OnRestartRequested();
            }
        }

        private void DrawInfoModal(float sw, float sh)
        {
            DrawDim(sw, sh);

            Rect safe = ResponsiveLayout.SafeAreaGui;
            float mw = Mathf.Min(400f * _ui, safe.width - 24f);
            float mh = Mathf.Min(370f * _ui, safe.height - 24f);
            Rect r = new Rect(safe.x + (safe.width - mw) * 0.5f, safe.y + (safe.height - mh) * 0.5f, mw, mh);
            DrawPanel(r, Color.white);

            _heading.fontSize = Mathf.RoundToInt(26f * _ui);
            Text(new Rect(r.x, r.y + 14f * _ui, r.width, 34f * _ui), "HOW TO PLAY", _heading, Cyan);

            _body.fontSize = Mathf.RoundToInt(15f * _ui);
            string rules =
                "- AIM & FIRE: point with mouse or touch and click (or arrow keys + space).\n" +
                "- SWAP: right-click, press S / Tab, or click the NEXT chamber.\n" +
                "- MATCH 3+: connect three or more of the same orb to shatter them.\n" +
                "- AVALANCHE: drop a cluster that lost its anchor for +150 x combo.\n" +
                "- SHIELD CELLS: every miss drains a cell. At zero, a new row drops in.\n" +
                "- THE SIX ORBS: Fire Core, Storm, Circuit, Solar Net, Void and Crystal.";
            GUI.Label(new Rect(r.x + 28f * _ui, r.y + 58f * _ui, r.width - 56f * _ui, 240f * _ui), rules, _body);

            if (DrawPillButton(new Rect(r.x + 44f * _ui, r.y + mh - 62f * _ui, mw - 88f * _ui, 42f * _ui), "pill_cyan", "BACK TO GAME"))
            {
                _showInfoModal = false;
                if (game != null) game.isPaused = false;
                CyberAudioSynthesizer.Instance.Play("click");
            }
        }
    }
}
