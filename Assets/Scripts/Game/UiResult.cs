using System;
using NeonDash.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>
    /// End-of-run panel: distance, progress, the multiplier reel and the final score. The reel
    /// spins fast then decelerates and snaps onto the rolled value, then the score counts up.
    /// </summary>
    public sealed class UiResult : UiScreen
    {
        Text titleText;
        Text subtitleText;
        Text distanceText;
        Text percentText;
        Text reelText;
        Text reelCaption;
        Text breakdownText;
        Text scoreText;
        Text seedText;
        Text badgeText;
        Image cardGlow;
        Image scoreGlow;
        RectTransform scorePanel;
        RectTransform card;
        RectTransform fill;
        RectTransform buttons;
        CanvasGroup buttonsGroup;
        Image reelGlow;
        Button retryButton;
        Button lobbyButton;
        Button recordsButton;

        RunResult result;
        Action onSettled;
        bool rolling;
        bool newBest;
        float rollElapsed;
        float rollDuration;
        float scoreShown;
        float scorePulse;
        float settleTime;
        float time;

        protected override void Construct()
        {
            var shade = UiKit.Sprite9(Root, "Shade", new Color(0.01f, 0.02f, 0.05f, 0.66f), UiKit.Glow, false);
            UiKit.Stretch(shade.rectTransform);

            cardGlow = UiKit.Sprite9(Root, "Card Glow", new Color(0.1f, 0.55f, 0.95f, 0.10f), UiKit.Glow, false);
            UiKit.Place(cardGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1180f, 900f));

            var cardImage = UiKit.Sprite9(Root, "Card", Palette.Panel, UiKit.Rounded);
            card = cardImage.rectTransform;
            UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(820f, 610f));

            var topEdge = UiKit.Sprite9(card, "Top Edge", Palette.Alpha(Palette.Cyan, 0.18f), UiKit.Rounded);
            UiKit.Place(topEdge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(780f, 4f));

            titleText = UiKit.Label(card, "RUN ENDED", 44, Palette.Magenta, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 56f));

            subtitleText = UiKit.Label(card, "DISTANCE REACHED", 20, Palette.TextDim);
            UiKit.Place(subtitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(600f, 30f));

            distanceText = UiKit.Label(card, "0 m", 86, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(distanceText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -104f), new Vector2(700f, 100f));

            var bar = UiKit.Sprite9(card, "Progress", new Color(0f, 0f, 0f, 0.35f), UiKit.Rounded);
            UiKit.Place(bar.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -206f), new Vector2(660f, 16f));

            var fillImage = UiKit.Sprite9(bar.transform, "Fill", Palette.Cyan, UiKit.Rounded);
            fill = fillImage.rectTransform;
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.anchoredPosition = new Vector2(2f, 0f);
            fill.sizeDelta = new Vector2(0f, -4f);

            percentText = UiKit.Label(card, "0%", 22, Palette.TextDim, TextAnchor.MiddleRight);
            UiKit.Place(percentText.rectTransform, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(330f, -232f), new Vector2(200f, 28f));

            // ---- multiplier reel ----
            var reelPanel = UiKit.Sprite9(card, "Reel", Palette.Alpha(Palette.PanelSoft, 0.55f), UiKit.Rounded);
            UiKit.Place(reelPanel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -262f), new Vector2(560f, 120f));

            reelGlow = UiKit.Sprite9(reelPanel.transform, "Reel Glow", Palette.Alpha(Palette.Cyan, 0.18f), UiKit.Glow, false);
            UiKit.Place(reelGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(520f, 190f));

            reelText = UiKit.Label(reelPanel.transform, "1.00x", 58, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(reelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 4f), new Vector2(540f, 76f));

            reelCaption = UiKit.Label(reelPanel.transform, "MULTIPLIER EARNED", 17, Palette.Alpha(Palette.TextDim, 0.9f));
            UiKit.Place(reelCaption.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(400f, 22f));

            breakdownText = UiKit.Label(reelPanel.transform, "", 15, Palette.Alpha(Palette.Cyan, 0.85f));
            UiKit.Place(breakdownText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 9f), new Vector2(530f, 20f));

            // ---- score ----
            var scorePanelImage = UiKit.Sprite9(card, "Score Panel", Palette.Alpha(Palette.PanelSoft, 0.5f), UiKit.Rounded);
            scorePanel = scorePanelImage.rectTransform;
            UiKit.Place(scorePanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -386f), new Vector2(560f, 78f));

            scoreGlow = UiKit.Sprite9(scorePanel, "Score Glow", Palette.Alpha(Palette.Gold, 0.10f), UiKit.Glow, false);
            UiKit.Place(scoreGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(540f, 150f));

            var scoreCaption = UiKit.Label(scorePanel, "SCORE", 18, Palette.Alpha(Palette.TextDim, 0.9f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(scoreCaption.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(160f, 28f));

            scoreText = UiKit.Label(scorePanel, "0", 48, Palette.Gold, TextAnchor.MiddleRight, FontStyle.Bold);
            UiKit.Place(scoreText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-26f, 0f), new Vector2(380f, 62f));

            seedText = UiKit.Label(card, "SEED 2026", 18, Palette.Alpha(Palette.TextDim, 0.9f), TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Place(seedText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -16f), new Vector2(170f, 26f));

            badgeText = UiKit.Label(card, "NEW BEST", 28, Palette.Lime, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(badgeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -470f), new Vector2(400f, 38f));
            badgeText.gameObject.SetActive(false);

            // ---- buttons ----
            var row = UiKit.Container(card, "Buttons");
            buttons = row;
            UiKit.Place(row, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(700f, 74f));
            buttonsGroup = row.gameObject.AddComponent<CanvasGroup>();

            Text retryLabel;
            retryButton = UiKit.Button(row, "RETRY", Palette.Cyan, out retryLabel, 30);
            UiKit.Place((RectTransform)retryButton.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(212f, 68f));
            retryLabel.color = new Color(0.02f, 0.08f, 0.14f, 1f);
            retryButton.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.RetryRun();
            });

            Text lobbyLabel;
            lobbyButton = UiKit.Button(row, "LOBBY", Palette.PanelSoft, out lobbyLabel, 26);
            UiKit.Place((RectTransform)lobbyButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(212f, 68f));
            lobbyButton.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.ReturnToLobby();
            });

            Text recordsLabel;
            recordsButton = UiKit.Button(row, "RECORDS", Palette.PanelSoft, out recordsLabel, 26);
            UiKit.Place((RectTransform)recordsButton.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(212f, 68f));
            recordsButton.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.OpenRecords();
            });
        }

        public void Present(RunResult run, bool isNewBest, Action settled)
        {
            result = run;
            newBest = isNewBest;
            onSettled = settled;

            gameObject.SetActive(true);
            UiKit.FadeIn(Group, 0.2f);

            titleText.text = run.Completed ? "COURSE CLEARED" : "RUN ENDED";
            titleText.color = run.Completed ? Palette.Lime : Palette.Magenta;
            subtitleText.text = run.Completed ? "FULL 30 SECOND RUN" : "DISTANCE REACHED";
            distanceText.text = run.Distance + " m";
            seedText.text = "SEED " + run.Seed;
            percentText.text = Mathf.FloorToInt(run.Progress * 100f) + "%";
            fill.sizeDelta = new Vector2(Mathf.Max(0f, 656f * run.Progress), -4f);
            scoreText.text = "0";
            scoreText.color = Palette.Alpha(Palette.Gold, 0.45f);
            scoreGlow.color = Palette.Alpha(Palette.Gold, 0.06f);
            badgeText.gameObject.SetActive(false);

            rollElapsed = 0f;
            rollDuration = GameConstantsBridge.MultiplierRollSeconds;
            scoreShown = 0f;
            scorePulse = 0f;
            settleTime = 0f;
            rolling = true;

            buttonsGroup.alpha = 0f;
            buttonsGroup.blocksRaycasts = false;
            reelText.text = "1.00x";
            reelText.color = Palette.TextDim;
            reelText.transform.localScale = Vector3.one;
            breakdownText.text = run.Orbs + run.Pads + run.Gems == 0
                ? "NO PICKUPS - THE MULTIPLIER IS EARNED, NOT ROLLED"
                : run.Gems + " GEMS   ·   " + run.Orbs + " ORBS   ·   " + run.Pads
                  + " PADS   ·   CHAIN x" + Mathf.Max(1, run.BestChain + 1);

            card.anchoredPosition = new Vector2(0f, -70f);
            card.localScale = Vector3.one * 0.9f;
            TweenRunner.Run(0.5f, k =>
            {
                if (card == null) return;
                card.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(-70f, 0f, k));
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, k);
            }, 0f, Ease.OutBack);

            distanceText.transform.localScale = Vector3.one * 0.7f;
            TweenRunner.Run(0.45f, k =>
            {
                if (distanceText != null) distanceText.transform.localScale = Vector3.one * k;
            }, 0.08f, Ease.OutBack);
        }

        public override void Hide()
        {
            base.Hide();
            rolling = false;
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            float dt = Time.unscaledDeltaTime;

            if (cardGlow != null)
                cardGlow.color = new Color(0.1f, 0.55f, 0.95f, 0.07f + Mathf.Sin(time * 1.6f) * 0.03f);

            if (rolling)
            {
                rollElapsed += dt;
                float t = Mathf.Clamp01(rollElapsed / Mathf.Max(0.1f, rollDuration));
                // Deterministic tally: counts from 1.00x up to exactly what the run earned.
                float eased = 1f - Mathf.Pow(1f - t, 2.3f);
                float value = Mathf.LerpUnclamped(MultiplierLedger.Base, result.Multiplier, eased);
                reelText.text = value.ToString("0.00") + "x";
                reelText.color = ColorFor(value);
                float tick = 1f + Mathf.Sin(t * Mathf.PI) * 0.05f;
                reelText.transform.localScale = new Vector3(tick, tick, 1f);

                if (t >= 1f) Settle();
            }
            else if (result.Multiplier > 0f)
            {
                if (scoreShown < result.Payout)
                {
                    scoreShown = Mathf.Min(result.Payout, scoreShown + Mathf.Max(1f, result.Payout * dt * 2.2f));
                    scoreText.text = Mathf.RoundToInt(scoreShown).ToString("N0");
                    scorePulse = Mathf.Lerp(scorePulse, 0.16f, dt * 6f);
                }
                else
                {
                    scorePulse = Mathf.Lerp(scorePulse, 0f, dt * 5f);
                }

                scoreText.transform.localScale = Vector3.one * (1f + scorePulse * 0.35f);
                scoreGlow.color = Palette.Alpha(Palette.Gold, 0.10f + scorePulse * 1.6f);

                if (newBest && badgeText.gameObject.activeSelf)
                {
                    float pulse = 1f + Mathf.Sin(time * 6f) * 0.05f;
                    badgeText.transform.localScale = new Vector3(pulse, pulse, 1f);
                    badgeText.color = Palette.Alpha(Palette.Lime, 0.75f + Mathf.Sin(time * 6f) * 0.25f);
                }
            }
        }

        void Settle()
        {
            rolling = false;
            reelText.text = result.Multiplier.ToString("0.00") + "x";
            reelText.color = ColorFor(result.Multiplier);
            reelText.transform.localScale = Vector3.one * 1.25f;
            TweenRunner.Run(0.42f, k =>
            {
                if (reelText == null) return;
                reelText.transform.localScale = Vector3.one * Mathf.LerpUnclamped(1.25f, 1f, k);
            }, 0f, Ease.OutBack);

            if (reelGlow != null) reelGlow.color = Palette.Alpha(ColorFor(result.Multiplier), 0.3f);
            if (scoreGlow != null) scoreGlow.color = Palette.Alpha(Palette.Gold, 0.22f);
            App.Audio.PlayMultiply();

            if (scorePanel != null)
            {
                scorePanel.localScale = Vector3.one * 1.06f;
                TweenRunner.Run(0.4f, k =>
                {
                    if (scorePanel != null) scorePanel.localScale = Vector3.one * Mathf.LerpUnclamped(1.06f, 1f, k);
                }, 0f, Ease.OutBack);
            }

            if (newBest)
            {
                badgeText.gameObject.SetActive(true);
                badgeText.transform.localScale = Vector3.one * 0.6f;
                TweenRunner.Run(0.4f, k =>
                {
                    if (badgeText != null) badgeText.transform.localScale = Vector3.one * k;
                }, 0.1f, Ease.OutBack);
            }

            buttonsGroup.blocksRaycasts = true;
            UiKit.FadeIn(buttonsGroup, 0.3f, 0.25f);
            buttons.anchoredPosition = new Vector2(0f, 6f);
            TweenRunner.Run(0.35f, k =>
            {
                if (buttons != null) buttons.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(6f, 26f, k));
            }, 0.2f, Ease.OutBack);

            if (onSettled != null) TweenRunner.Delay(0.1f, onSettled);
        }

        static Color ColorFor(float multiplier)
        {
            if (multiplier < 1f) return Palette.TextDim;
            if (multiplier < 3f) return Palette.Cyan;
            if (multiplier < 6f) return Palette.Magenta;
            return Palette.Gold;
        }
    }
}
