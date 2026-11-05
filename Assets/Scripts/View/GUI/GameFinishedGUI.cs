using UnityEngine;
using System;
using System.Collections;

namespace com.javierquevedo.gui
{
    /// <summary>
    /// Match results card: rank badge, victory/defeat title, a YOU vs OPPONENT score comparison,
    /// three summary rows and the two action buttons. Drawn from the same authored art as the HUD.
    /// </summary>
    public class GameFinishedGUI : MenuGUI
    {
        GameFinishGUIDelegate startNewGameSelectedDelegate;
        public delegate void GameFinishGUIDelegate();

        public GameFinishGUIDelegate StartNewGameSelectedDelegate
        {
            set { this.startNewGameSelectedDelegate = value; }
        }

        public Action ReturnToLobbyDelegate;
        public Game game;

        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bigNumber;
        private GUIStyle _smallLabel;
        private GUIStyle _rowLabel;
        private GUIStyle _pillText;
        private bool _stylesReady = false;
        private float _openedAt = -1f;
        private float _ui = 1f;

        private static readonly Color Win = new Color(1.00f, 0.82f, 0.30f, 1f);
        private static readonly Color Lose = new Color(1.00f, 0.42f, 0.55f, 1f);
        private static readonly Color You = new Color(0.50f, 1.00f, 0.62f, 1f);
        private static readonly Color Rival = new Color(0.66f, 0.74f, 0.84f, 1f);

        protected override void Start()
        {
            base.Start();
            _openedAt = Time.unscaledTime;
        }

        protected override void Update()
        {
            base.Update();
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;

            _titleStyle = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _headerStyle = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _bigNumber = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
            _smallLabel = ArtUI.MakeLabel(false, TextAnchor.MiddleCenter);
            _rowLabel = ArtUI.MakeLabel(false, TextAnchor.MiddleLeft);
            _rowLabel.normal.textColor = new Color(0.82f, 0.92f, 1.0f, 1f);
            _pillText = ArtUI.MakeLabel(true, TextAnchor.MiddleCenter);
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

        protected override void OnGUI()
        {
            if (game == null) return;
            EnsureStyles();

            float sw = Screen.width;
            float sh = Screen.height;
            float uiScale = ResponsiveLayout.FitScale(400f, 480f, 22f, 0.55f, 1.4f);
            _ui = uiScale;

            // Card eases in so the result lands instead of snapping on.
            float t = _openedAt < 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - _openedAt) / 0.28f);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            if (Event.current.type == EventType.Repaint)
            {
                Color prev = GUI.color;
                GUI.color = new Color(0.01f, 0.02f, 0.04f, 0.72f * ease);
                GUI.DrawTexture(new Rect(0, 0, sw, sh), CyberArtGenerator.WhiteTexture);
                GUI.color = prev;
            }

            float cardW = 400f * uiScale;
            float cardH = 480f * uiScale;
            float slide = (1f - ease) * 24f * uiScale;
            Rect cardBase = ResponsiveLayout.Centered(cardW, cardH);
            Rect cardRect = new Rect(cardBase.x, cardBase.y + slide, cardW, cardH);

            Color oldColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, ease);
            ArtUI.DrawPanel(cardRect, uiScale, Color.white);

            bool won = (game.state == GameState.Win) || (game.score >= game.opponentScore && game.score > 0);
            int tier = TierIndex(game.tierName);

            // Rank badge with the tier numeral
            float badge = 84f * uiScale;
            Rect badgeRect = new Rect(cardRect.center.x - badge * 0.5f, cardRect.y + 14f * uiScale, badge, badge);
            Texture2D badgeTex = CyberArtGenerator.LoadArtTexture("rank_badge");
            ArtUI.DrawTex(new Rect(badgeRect.x, badgeRect.y + 3f * uiScale, badge, badge), badgeTex, new Color(0f, 0f, 0f, 0.35f));
            ArtUI.DrawTex(badgeRect, badgeTex, Color.white);
            _bigNumber.fontSize = Mathf.RoundToInt(badge * 0.30f);
            string[] numerals = { "I", "II", "III", "IV", "V" };
            ArtUI.Text(new Rect(badgeRect.x + badge * 0.2f, badgeRect.y + badge * 0.31f, badge * 0.6f, badge * 0.38f),
                numerals[tier], _bigNumber, won ? Win : ArtUI.Cyan, uiScale);

            _smallLabel.fontSize = Mathf.RoundToInt(15f * uiScale);
            ArtUI.Text(new Rect(cardRect.x, badgeRect.yMax + 2f * uiScale, cardW, 20f * uiScale),
                "RANK  " + game.tierName, _smallLabel, ArtUI.LabelBlue, uiScale);

            // Title
            _titleStyle.fontSize = Mathf.RoundToInt(44f * uiScale);
            string titleText = (game.state == GameState.Win) ? "VICTORY" : (won ? "TOURNAMENT WIN" : "DEFEAT");
            if (titleText.Length > 8) _titleStyle.fontSize = Mathf.RoundToInt(34f * uiScale);
            ArtUI.Text(new Rect(cardRect.x, cardRect.y + 124f * uiScale, cardW, 48f * uiScale), titleText, _titleStyle, won ? Win : Lose, uiScale);

            // YOU vs OPPONENT
            float colGap = 12f * uiScale;
            float colW = (cardW - 56f * uiScale - colGap) * 0.5f;
            float colH = 96f * uiScale;
            float colY = cardRect.y + 182f * uiScale;
            Rect youRect = new Rect(cardRect.x + 28f * uiScale, colY, colW, colH);
            Rect oppRect = new Rect(youRect.xMax + colGap, colY, colW, colH);

            ArtUI.DrawPanel(youRect, uiScale * 0.7f, new Color(0.80f, 1.0f, 0.86f, 1f));
            ArtUI.DrawPanel(oppRect, uiScale * 0.7f, Color.white);

            _headerStyle.fontSize = Mathf.RoundToInt(15f * uiScale);
            ArtUI.Text(new Rect(youRect.x, youRect.y + 8f * uiScale, youRect.width, 20f * uiScale), "YOU", _headerStyle, You, uiScale);
            ArtUI.Text(new Rect(oppRect.x, oppRect.y + 8f * uiScale, oppRect.width, 20f * uiScale), "OPPONENT", _headerStyle, Rival, uiScale);

            _bigNumber.fontSize = Mathf.RoundToInt(32f * uiScale);
            ArtUI.Text(new Rect(youRect.x, youRect.y + 28f * uiScale, youRect.width, 40f * uiScale), game.score.ToString("N0"), _bigNumber, You, uiScale);
            ArtUI.Text(new Rect(oppRect.x, oppRect.y + 28f * uiScale, oppRect.width, 40f * uiScale), game.opponentScore.ToString("N0"), _bigNumber, Rival, uiScale);

            _smallLabel.fontSize = Mathf.RoundToInt(13f * uiScale);
            ArtUI.Text(new Rect(youRect.x, youRect.y + 69f * uiScale, youRect.width, 18f * uiScale),
                game.bubblesDestroyed + " orbs popped", _smallLabel, ArtUI.LabelBlue, uiScale);
            ArtUI.Text(new Rect(oppRect.x, oppRect.y + 69f * uiScale, oppRect.width, 18f * uiScale),
                "CYBER-BOT v6", _smallLabel, ArtUI.LabelBlue, uiScale);

            // Summary rows
            TimeSpan elapsedSpan = TimeSpan.FromSeconds(game.timeElapsed);
            string timeFormatted = string.Format("{0:D2}:{1:D2}", elapsedSpan.Minutes, elapsedSpan.Seconds);

            float rowX = cardRect.x + 36f * uiScale;
            float rowW = cardW - 72f * uiScale;
            float rowY = colY + colH + 14f * uiScale;
            float rowH = 26f * uiScale;
            DrawSummaryRow(rowX, rowY, rowW, rowH, "TIME", timeFormatted);
            DrawSummaryRow(rowX, rowY + rowH, rowW, rowH, "MAX COMBO", "x" + game.maxCombo);
            DrawSummaryRow(rowX, rowY + 2 * rowH, rowW, rowH, "ACCURACY", game.accuracyPercent + "%");

            // Buttons
            float btnW = cardW - 56f * uiScale;
            float btnX = cardRect.x + 28f * uiScale;
            float btn1Y = cardRect.yMax - 112f * uiScale;
            float btn2Y = cardRect.yMax - 58f * uiScale;

            if (ArtUI.PillButton(new Rect(btnX, btn1Y, btnW, 48f * uiScale), "pill_green", "PLAY AGAIN", _pillText, uiScale))
            {
                CyberAudioSynthesizer.Instance.Play("click");
                this.newGameWasSelected();
            }

            if (ArtUI.PillButton(new Rect(btnX, btn2Y, btnW, 42f * uiScale), "pill_blue", "MAIN MENU", _pillText, uiScale))
            {
                CyberAudioSynthesizer.Instance.Play("click");
                if (ReturnToLobbyDelegate != null)
                    ReturnToLobbyDelegate();
                else
                    this.newGameWasSelected();
            }

            GUI.color = oldColor;
        }

        private void DrawSummaryRow(float x, float y, float w, float h, string label, string value)
        {
            _rowLabel.fontSize = Mathf.RoundToInt(16f * _ui);
            _rowLabel.alignment = TextAnchor.MiddleLeft;
            ArtUI.Text(new Rect(x, y, w * 0.6f, h), label, _rowLabel, ArtUI.LabelBlue, _ui);
            _rowLabel.alignment = TextAnchor.MiddleRight;
            ArtUI.Text(new Rect(x + w * 0.4f, y, w * 0.6f, h), value, _rowLabel, Color.white, _ui);

            ArtUI.DrawTex(new Rect(x, y + h - 1f, w, 1f), CyberArtGenerator.WhiteTexture, new Color(0.45f, 0.65f, 0.78f, 0.22f));
        }

        private void newGameWasSelected()
        {
            if (startNewGameSelectedDelegate != null)
                startNewGameSelectedDelegate();
        }
    }
}
