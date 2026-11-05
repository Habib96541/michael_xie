using NeonDash.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>In-run HUD: seed, distance, timer, progress bar with the record marker, pause.</summary>
    public sealed class UiHud : UiScreen
    {
        Text seedText;
        Text distanceText;
        Text bestText;
        Text timeText;
        Text percentText;
        RectTransform fill;
        RectTransform bestMarker;
        Image fillImage;
        RectTransform seedPlate;
        RectTransform timerPlate;
        RectTransform pausePlate;
        RectTransform progressPlate;
        bool compact;
        float currentControlSize;
        bool layoutReady;
        float tutorialTop = -96f;
        float shownDistance;
        float barPulse;

        RectTransform tutorialPanel;
        CanvasGroup tutorialGroup;
        Image tutorialAccent;
        Text tutorialTitle;
        Text tutorialBody;
        int tutorialToken;

        Image multChip;
        Text multText;
        Text gainText;
        Text chainText;
        Image multGlow;
        int multSerial;
        float multPulse;
        float gainTimer;
        Vector2 gainHome;
        float shownMult = 1f;

        protected override void Construct()
        {
            var seedChip = UiKit.Sprite9(Root, "Seed Chip", Palette.Alpha(Palette.Panel, 0.92f), UiKit.Chamfered);
            UiKit.Place(seedChip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -22f), new Vector2(320f, 56f));
            seedPlate = seedChip.rectTransform;
            seedText = UiKit.Label(seedChip.transform, "SEED", 19, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);

            // ---- earned multiplier - done here so it sits under the seed chip ----
            var multChipImage = UiKit.Sprite9(Root, "Multiplier Chip", Palette.Alpha(Palette.Panel, 0.92f), UiKit.Chamfered);
            multChip = multChipImage;
            UiKit.Place(multChipImage.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -88f), new Vector2(250f, 56f));

            multGlow = UiKit.Sprite9(multChipImage.transform, "Glow", Palette.Alpha(Palette.Gold, 0f), UiKit.Glow, false);
            UiKit.Place(multGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(340f, 150f));

            multText = UiKit.Label(multChipImage.transform, "x1.00", 30, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(multText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230f, 46f));

            // Multiplier gains show up dead centre of the screen, a little above the middle so the
            // cube is never covered, and scaled up as they appear.
            gainText = UiKit.Label(Root, "", 38, Palette.Lime, TextAnchor.MiddleCenter, FontStyle.Bold);
            gainHome = new Vector2(0f, 74f);
            UiKit.Place(gainText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), gainHome, new Vector2(760f, 96f));

            chainText = UiKit.Label(Root, "", 18, Palette.Alpha(Palette.Cyan, 0.95f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(chainText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(26f, -176f), new Vector2(300f, 24f));

            var timerChip = UiKit.Sprite9(Root, "Timer Chip", Palette.Alpha(Palette.Panel, 0.92f), UiKit.Chamfered);
            UiKit.Place(timerChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-96f, -22f), new Vector2(190f, 56f));
            timerPlate = timerChip.rectTransform;
            timeText = UiKit.Label(timerChip.transform, "0:30.0", 26, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);

            var pauseImage = UiKit.Sprite9(Root, "Pause", Palette.Alpha(Palette.Panel, 0.92f), UiKit.Chamfered);
            UiKit.Place(pauseImage.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -22f), new Vector2(56f, 56f));
            pausePlate = pauseImage.rectTransform;
            var pauseButton = UiKit.AttachButton(pauseImage);
            UiIcon.Add(pauseImage.transform, IconKind.Pause, Palette.Cyan, 26f);
            pauseButton.onClick.AddListener(delegate
            {
                App.PauseRun();
            });

            // ---- progress bar ----
            var bar = UiKit.Sprite9(Root, "Progress Bar", Palette.Alpha(Palette.Panel, 0.88f), UiKit.Chamfered);
            UiKit.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(760f, 24f));

            progressPlate = bar.rectTransform;
            var fillImage = UiKit.Sprite9(bar.transform, "Fill", Palette.Cyan, UiKit.Chamfered);
            fill = fillImage.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(3f, 0f);
            fill.sizeDelta = new Vector2(0f, -6f);
            fillImage = fill.GetComponent<Image>();
            this.fillImage = fillImage;

            var markerImage = UiKit.Sprite9(bar.transform, "Best Marker", Palette.Gold, UiKit.Chamfered);
            bestMarker = markerImage.rectTransform;
            bestMarker.anchorMin = new Vector2(0f, 0.5f);
            bestMarker.anchorMax = new Vector2(0f, 0.5f);
            bestMarker.pivot = new Vector2(0.5f, 0.5f);
            bestMarker.sizeDelta = new Vector2(4f, 30f);

            percentText = UiKit.Label(bar.transform, "0%", 18, Palette.TextDim, TextAnchor.MiddleLeft);
            UiKit.Place(percentText.rectTransform, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(110f, 24f));

            distanceText = UiKit.Label(Root, "0 m", 66, Palette.Text, TextAnchor.LowerLeft, FontStyle.Bold);
            UiKit.Place(distanceText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 34f), new Vector2(520f, 84f));
            var distanceShadow = distanceText.gameObject.AddComponent<Shadow>();
            distanceShadow.effectColor = Palette.Alpha(Palette.Cyan, 0.4f);
            distanceShadow.effectDistance = new Vector2(3f, -3f);

            bestText = UiKit.Label(Root, "BEST 0 m", 24, Palette.Alpha(Palette.TextDim, 0.9f), TextAnchor.LowerRight);
            UiKit.Place(bestText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 42f), new Vector2(420f, 40f));

            // ---- mode tutorial banner (ship / gravity / cube) ----
            var panelImage = UiKit.Sprite9(Root, "Tutorial", Palette.Alpha(Palette.Panel, 0.97f), UiKit.Chamfered);
            tutorialPanel = panelImage.rectTransform;
            UiKit.Place(tutorialPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(660f, 84f));
            tutorialGroup = panelImage.gameObject.AddComponent<CanvasGroup>();
            tutorialGroup.blocksRaycasts = false;

            tutorialAccent = UiKit.Sprite9(tutorialPanel, "Accent", Palette.Cyan, UiKit.Chamfered);
            UiKit.Place(tutorialAccent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(7f, 54f));

            tutorialTitle = UiKit.Label(tutorialPanel, "CUBE MODE", 30, Palette.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(tutorialTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -8f), new Vector2(590f, 36f));

            tutorialBody = UiKit.Label(tutorialPanel, "TAP TO JUMP", 21, Palette.Text, TextAnchor.MiddleLeft);
            UiKit.Place(tutorialBody.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(38f, -48f), new Vector2(600f, 30f));
            panelImage.gameObject.SetActive(false);
            RefreshLayout();
        }

        void RefreshLayout()
        {
            bool narrow = Root.rect.width < 1450f || Root.rect.width < Root.rect.height * 1.45f;
            bool touch = ResponsiveCanvas.TouchLayout || narrow;
            float topHeight = Root.rect.height > Root.rect.width * 1.35f ? 126f : touch ? 92f : 56f;
            if (layoutReady && compact == narrow && currentControlSize == topHeight) return;
            layoutReady = true;
            compact = narrow;
            currentControlSize = topHeight;
            var topLeft = new Vector2(0f, 1f);
            var topRight = new Vector2(1f, 1f);
            var top = new Vector2(0.5f, 1f);
            UiKit.Place(pausePlate, topRight, topRight, new Vector2(-26f, -22f), Vector2.one * topHeight);
            UiKit.Place(timerPlate, topRight, topRight, new Vector2(-40f - topHeight, -22f), new Vector2(160f, topHeight));
            UiKit.Place(seedPlate, topLeft, topLeft, new Vector2(26f, -22f), new Vector2(320f, topHeight));
            UiKit.Place(multChip.rectTransform, topLeft, topLeft, compact ? new Vector2(366f, -22f) : new Vector2(26f, -32f - topHeight),
                new Vector2(compact ? 175f : 250f, compact ? topHeight : 56f));
            UiKit.Place(progressPlate, top, top, new Vector2(compact ? -28f : 0f, compact ? -34f - topHeight : -34f),
                new Vector2(compact ? 780f : 720f, 24f));
            UiKit.Place(chainText.rectTransform, topLeft, topLeft, new Vector2(26f, compact ? -70f - topHeight : -120f - topHeight), new Vector2(200f, 24f));
            tutorialTop = compact ? -112f - topHeight : (touch ? -124f : -96f);
            tutorialPanel.anchoredPosition = new Vector2(0f, tutorialTop);
            seedText.fontSize = touch ? 24 : 19;
            timeText.fontSize = touch ? 34 : 26;
        }

        /// <summary>
        /// Drops a short animated coaching card at the top of the screen whenever the run changes
        /// verb (ship, flipped gravity, back to cube) so the player is told what to do next.
        /// </summary>
        public void ShowTutorial(string title, string body, Color accent)
        {
            if (tutorialPanel == null) return;
            tutorialToken++;
            int token = tutorialToken;

            RefreshLayout();
            var target = new Vector2(0f, tutorialTop);
            tutorialPanel.gameObject.SetActive(true);
            tutorialTitle.text = title;
            tutorialTitle.color = accent;
            tutorialBody.text = body;
            tutorialAccent.color = accent;
            tutorialGroup.alpha = 0f;
            tutorialPanel.anchoredPosition = target + new Vector2(0f, 34f);
            tutorialPanel.localScale = Vector3.one * 0.92f;

            TweenRunner.Run(0.34f, k =>
            {
                if (tutorialPanel == null) return;
                tutorialPanel.anchoredPosition = new Vector2(0f, tutorialTop + 34f * (1f - k));
                tutorialPanel.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, k);
            }, 0f, Ease.OutBack);
            UiKit.FadeIn(tutorialGroup, 0.2f);

            TweenRunner.Delay(2.6f, delegate
            {
                if (token != tutorialToken || tutorialPanel == null) return;
                TweenRunner.Run(0.4f, k =>
                {
                    if (tutorialGroup != null) tutorialGroup.alpha = 1f - k;
                }, 0f, Ease.Linear, delegate
                {
                    if (token == tutorialToken && tutorialPanel != null) tutorialPanel.gameObject.SetActive(false);
                });
            });
        }

        public override void Hide()
        {
            base.Hide();
            tutorialToken++;
        }

        public void ResetForRun()
        {
            shownDistance = 0f;
            barPulse = 0f;
            multSerial = 0;
            multPulse = 0f;
            gainTimer = 0f;
            shownMult = MultiplierLedger.Base;
            if (gainText != null)
            {
                gainText.text = "";
                gainText.rectTransform.anchoredPosition = gainHome;
                gainText.rectTransform.localScale = Vector3.one;
            }
            if (chainText != null) chainText.text = "";
            if (multText != null) multText.text = "x1.00";
            if (multChip != null) multChip.rectTransform.localScale = Vector3.one;
            if (multGlow != null) multGlow.color = Palette.Alpha(Palette.Gold, 0f);
        }

        public override void Show()
        {
            base.Show();
            ResetForRun();
            UiKit.FadeIn(Group, 0.3f);
        }

        void Update()
        {
            RunSession session = App.Session;
            if (session == null) return;
            Level level = session.Level;
            float progress = level == null ? 0f : session.Progress;

            shownDistance = Mathf.Lerp(shownDistance, session.Distance, Time.unscaledDeltaTime * 12f);
            if (Mathf.Abs(shownDistance - session.Distance) < 0.4f) shownDistance = session.Distance;
            distanceText.text = Mathf.FloorToInt(shownDistance) + " m";

            UpdateMultiplier(session.Multiplier);

            seedText.text = "SEED " + App.Seed;

            float remaining = session.TimeLeft;
            int seconds = Mathf.FloorToInt(remaining);
            int tenths = Mathf.FloorToInt((remaining - seconds) * 10f);
            timeText.text = "0:" + seconds.ToString("00") + "." + tenths;
            timeText.color = remaining < 5f ? Palette.Magenta : Palette.Text;

            RefreshLayout();
            float barWidth = progressPlate.rect.width - 6f;
            fill.sizeDelta = new Vector2(Mathf.Max(0f, barWidth * progress), -6f);

            barPulse = Mathf.Max(0f, barPulse - Time.unscaledDeltaTime * 2.5f);
            if (fillImage != null)
                fillImage.color = Color.Lerp(Palette.Cyan, Color.white, barPulse * 0.7f);

            percentText.text = Mathf.FloorToInt(progress * 100f) + "%";

            SeedRecord record = App.Store.Table.Find(App.Seed);
            float bestProgress = record == null ? 0f : ScoreRules.Progress(record.BestDistance, GameConstants.CourseLength);
            bestMarker.anchorMin = new Vector2(bestProgress, 0.5f);
            bestMarker.anchorMax = new Vector2(bestProgress, 0.5f);
            bestMarker.gameObject.SetActive(record != null && record.BestDistance > 0);
            bestText.text = record == null || record.BestDistance <= 0
                ? "NO RECORD"
                : "BEST " + record.BestDistance + " m  BY " + record.Name;
        }

        /// <summary>Live earned multiplier: counts up to each new value and flashes on a pickup.</summary>
        void UpdateMultiplier(MultiplierLedger ledger)
        {
            if (ledger == null || multText == null) return;
            float dt = Time.unscaledDeltaTime;

            if (ledger.Serial != multSerial)
            {
                multSerial = ledger.Serial;
                multPulse = 1f;
                gainTimer = 0.9f;
                gainText.text = "+" + ledger.LastGain.ToString("0.00") + "  " + MultiplierLedger.NameOf(ledger.LastKind);
            }

            shownMult = Mathf.Lerp(shownMult, ledger.Value, 1f - Mathf.Exp(-11f * dt));
            if (Mathf.Abs(shownMult - ledger.Value) < 0.005f) shownMult = ledger.Value;

            multText.text = "x" + shownMult.ToString("0.00");
            multText.color = ColorFor(ledger.Value);

            multPulse = Mathf.Max(0f, multPulse - dt * 3.2f);
            if (multChip != null)
                multChip.rectTransform.localScale = Vector3.one * (1f + multPulse * 0.09f);
            if (multGlow != null)
                multGlow.color = Palette.Alpha(ColorFor(ledger.Value), multPulse * 0.35f);

            if (gainTimer > 0f)
            {
                gainTimer -= dt;
                float k = 1f - Mathf.Clamp01(gainTimer / 0.9f);
                gainText.rectTransform.anchoredPosition = gainHome + new Vector2(0f, 34f * k);

                // Punch in oversized, settle to normal, then drift up and fade out.
                float punch = 1f + 0.4f * Mathf.Clamp01(1f - k * 3f);
                gainText.rectTransform.localScale = Vector3.one * punch;
                gainText.color = Palette.Alpha(Palette.Lime, 1f - k * k * k);
            }
            else if (gainText.text.Length > 0)
            {
                gainText.text = "";
            }

            chainText.text = ledger.Chain > 0
                ? "CHAIN x" + (ledger.Chain + 1)
                : (ledger.Pickups == 0 ? "JUMP THROUGH GEMS" : "");
        }

        static Color ColorFor(float multiplier)
        {
            if (multiplier < 2f) return Palette.TextDim;
            if (multiplier < 4f) return Palette.Cyan;
            if (multiplier < 6f) return Palette.Magenta;
            return Palette.Gold;
        }

        public void PulseProgress()
        {
            barPulse = 1f;
        }

        public void FlashMode()
        {
            barPulse = 1f;
        }
    }
}
