using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Pause overlay: resume, restart the seed, or return to the lobby.</summary>
    public sealed class UiPause : UiScreen
    {
        RectTransform card;

        protected override void Construct()
        {
            var shade = UiKit.Sprite9(Root, "Shade", new Color(0.01f, 0.02f, 0.05f, 0.7f), UiKit.Glow, false);
            UiKit.Stretch(shade.rectTransform);
            shade.raycastTarget = true;

            var cardImage = UiKit.Sprite9(Root, "Card", Palette.Panel, UiKit.Rounded);
            card = cardImage.rectTransform;
            UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 400f));

            var title = UiKit.Label(card, "PAUSED", 52, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(500f, 64f));

            var seedText = UiKit.Label(card, "SEED " + App.Seed, 22, Palette.TextDim);
            UiKit.Place(seedText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(400f, 30f));

            Text resumeLabel;
            Button resume = UiKit.Button(card, "RESUME", Palette.Cyan, out resumeLabel, 30);
            UiKit.Place((RectTransform)resume.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(420f, 72f));
            resumeLabel.color = new Color(0.02f, 0.08f, 0.14f, 1f);
            resume.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.ResumeRun();
            });

            Text restartLabel;
            Button restart = UiKit.Button(card, "RESTART SEED", Palette.PanelSoft, out restartLabel, 26);
            UiKit.Place((RectTransform)restart.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -236f), new Vector2(420f, 66f));
            restart.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.RetryRun();
            });

            Text lobbyLabel;
            Button lobby = UiKit.Button(card, "LOBBY", Palette.PanelSoft, out lobbyLabel, 26);
            UiKit.Place((RectTransform)lobby.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -314f), new Vector2(420f, 66f));
            lobby.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.ReturnToLobby();
            });
        }

        public override void Show()
        {
            base.Show();
            card.localScale = Vector3.one * 0.9f;
            TweenRunner.Run(0.28f, k =>
            {
                if (card != null) card.localScale = Vector3.one * k;
            }, 0f, Ease.OutBack);
        }
    }
}
