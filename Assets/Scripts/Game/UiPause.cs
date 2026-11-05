using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Pause overlay: resume, restart the seed, or return to the lobby.</summary>
    public sealed class UiPause : UiScreen
    {
        protected override Vector2 PortraitSize => new Vector2(600f, 480f);
        RectTransform card;
        Text seedText;

        protected override void Construct()
        {
            var shade = UiKit.Sprite9(Root, "Shade", new Color(0.01f, 0.02f, 0.05f, 0.7f), UiKit.Glow, false);
            UiKit.Stretch(shade.rectTransform);
            shade.raycastTarget = true;

            card = UiKit.Frame(Root, "Pause console", Palette.Cyan);
            UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(560f, 440f));

            var title = UiKit.Label(card, "PAUSED", 52, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(500f, 64f));

            seedText = UiKit.Label(card, "", 22, Palette.TextDim);
            UiKit.Place(seedText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(400f, 30f));

            Text resumeLabel;
            Button resume = UiKit.Button(card, "RESUME", Palette.Action, out resumeLabel, 30);
            UiKit.Place((RectTransform)resume.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(420f, 80f));
            UiKit.AddButtonIcon(resumeLabel, IconKind.Play);
            resume.onClick.AddListener(delegate
            {
                App.ResumeRun();
            });

            Text restartLabel;
            Button restart = UiKit.Button(card, "RESTART SEED", Palette.PanelSoft, out restartLabel, 26);
            UiKit.Place((RectTransform)restart.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -246f), new Vector2(420f, 80f));
            UiKit.AddButtonIcon(restartLabel, IconKind.Retry);
            restart.onClick.AddListener(delegate
            {
                App.RetryRun();
            });

            Text lobbyLabel;
            Button lobby = UiKit.Button(card, "LOBBY", Palette.PanelSoft, out lobbyLabel, 26);
            UiKit.Place((RectTransform)lobby.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -342f), new Vector2(420f, 80f));
            UiKit.AddButtonIcon(lobbyLabel, IconKind.Home);
            lobby.onClick.AddListener(delegate
            {
                App.ReturnToLobby();
            });
        }

        public override void Show()
        {
            base.Show();
            seedText.text = "SEED " + App.Seed;
            card.localScale = Vector3.one * 0.9f;
            TweenRunner.Run(0.28f, k =>
            {
                if (card != null) card.localScale = Vector3.one * k;
            }, 0f, Ease.OutBack);
        }
    }
}
