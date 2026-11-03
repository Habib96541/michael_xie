using NeonDash.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Title screen: name, seed entry, best distance for the seed, and PLAY.</summary>
    public sealed class UiLobby : UiScreen
    {
        Text title;
        Text titleGhost;
        Text seedChip;
        Text bestText;
        Text recordLine;
        InputField nameField;
        InputField seedField;
        RectTransform card;
        Image cardGlow;
        readonly System.Collections.Generic.List<CanvasGroup> chips = new System.Collections.Generic.List<CanvasGroup>();
        float time;

        protected override void Construct()
        {
            // Backdrop shade so the 3D lobby background reads as a scene, not noise.
            var shade = UiKit.Sprite9(Root, "Shade", new Color(0.01f, 0.02f, 0.06f, 0.62f), UiKit.Glow, false);
            UiKit.Stretch(shade.rectTransform);

            title = UiKit.Label(Root, "GEOMETRY DASH", 106, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 292f), new Vector2(1300f, 150f));
            var titleShadow = title.gameObject.AddComponent<Shadow>();
            titleShadow.effectColor = Palette.Alpha(Palette.Magenta, 0.55f);
            titleShadow.effectDistance = new Vector2(5f, -6f);

            titleGhost = UiKit.Label(Root, "GEOMETRY DASH", 106, Palette.Alpha(Palette.Magenta, 0.22f), TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Place(titleGhost.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 292f), new Vector2(1300f, 150f));

            var subtitle = UiKit.Label(Root, "SEEDED 30 SECOND RUNS  /  COLLECT GEMS TO EARN THE MULTIPLIER", 22, Palette.TextDim);
            UiKit.Place(subtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 208f), new Vector2(1200f, 40f));

            // ---- main card ----
            cardGlow = UiKit.Sprite9(Root, "Card Glow", new Color(0.1f, 0.55f, 0.95f, 0.10f), UiKit.Glow, false);
            UiKit.Place(cardGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -87f), new Vector2(1080f, 760f));

            var cardImage = UiKit.Sprite9(Root, "Card", Palette.Panel, UiKit.Rounded);
            card = cardImage.rectTransform;
            UiKit.Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -87f), new Vector2(780f, 500f));

            var cardEdge = UiKit.Sprite9(card, "Card Edge", Palette.Alpha(Palette.Cyan, 0.16f), UiKit.Rounded);
            UiKit.Place(cardEdge.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(740f, 4f));

            var nameLabel = UiKit.Label(card, "PLAYER", 22, Palette.TextDim, TextAnchor.MiddleLeft);
            UiKit.Place(nameLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, 150f), new Vector2(140f, 50f));

            nameField = UiKit.Field(card, "YOUR NAME", App.PlayerName, out _);
            UiKit.Place((RectTransform)nameField.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 150f), new Vector2(440f, 62f));
            nameField.characterLimit = 12;
            nameField.onValueChanged.AddListener(delegate (string value) { App.SetPlayerName(value); });
            nameField.onEndEdit.AddListener(delegate (string value) { App.SaveProfile(); });

            var seedLabel = UiKit.Label(card, "SEED", 22, Palette.TextDim, TextAnchor.MiddleLeft);
            UiKit.Place(seedLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, 64f), new Vector2(140f, 50f));

            seedField = UiKit.Field(card, "SEED", App.Seed, out _);
            UiKit.Place((RectTransform)seedField.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-64f, 64f), new Vector2(250f, 62f));
            seedField.characterLimit = 10;
            seedField.contentType = InputField.ContentType.IntegerNumber;
            seedField.onEndEdit.AddListener(delegate (string value) { App.SetSeed(value); });
            seedField.onValueChanged.AddListener(delegate (string value)
            {
                if (value.Length >= 10) App.SetSeed(value);
            });

            Text randomLabel;
            Button randomButton = UiKit.Button(card, "RANDOM", Palette.PanelSoft, out randomLabel, 22);
            UiKit.Place((RectTransform)randomButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(210f, 64f), new Vector2(190f, 62f));
            randomButton.onClick.AddListener(delegate
            {
                App.RandomSeed();
                seedField.text = App.Seed;
                App.Audio.PlayClick();
            });

            recordLine = UiKit.Label(card, "NO RECORD ON THIS SEED YET", 24, Palette.Gold);
            UiKit.Place(recordLine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -16f), new Vector2(700f, 40f));

            Text playLabel;
            Button playButton = UiKit.Button(card, "PLAY", Palette.Cyan, out playLabel, 38);
            UiKit.Place((RectTransform)playButton.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -108f), new Vector2(560f, 92f));
            playLabel.color = new Color(0.02f, 0.08f, 0.14f, 1f);
            playButton.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.StartRun();
            });

            Text recordsLabel;
            Button recordsButton = UiKit.Button(card, "RECORDS", Palette.PanelSoft, out recordsLabel, 24);
            UiKit.Place((RectTransform)recordsButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(220f, 54f));
            recordsButton.onClick.AddListener(delegate
            {
                App.Audio.PlayClick();
                App.OpenRecords();
            });

            BuildChips();


            Refresh();
        }

        /// <summary>Three quick rule chips under the card: the whole game explained in one line.</summary>
        void BuildChips()
        {
            string[] titles = { "TAP TO JUMP", "DODGE EVERY SPIKE", "GEMS = MULTIPLIER" };
            Color[] accents = { Palette.Cyan, Palette.Magenta, Palette.Gold };
            float[] offsets = { -270f, 0f, 270f };

            for (int i = 0; i < titles.Length; i++)
            {
                var chipImage = UiKit.Sprite9(Root, "Chip " + i, Palette.Alpha(Palette.Panel, 0.9f), UiKit.Rounded);
                UiKit.Place(chipImage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(offsets[i], -362f), new Vector2(250f, 46f));

                var accent = UiKit.Sprite9(chipImage.transform, "Accent", accents[i], UiKit.Rounded);
                UiKit.Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(9f, 0f), new Vector2(4f, 24f));

                var label = UiKit.Label(chipImage.transform, titles[i], 17, Palette.Alpha(Palette.Text, 0.92f), TextAnchor.MiddleCenter, FontStyle.Bold);
                UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(3f, 0f), new Vector2(220f, 30f));

                chips.Add(chipImage.gameObject.AddComponent<CanvasGroup>());
            }
        }

        public void Refresh()
        {
            SeedRecord record = App.Store.Table.Find(App.Seed);
            if (record == null || record.BestDistance <= 0)
            {
                recordLine.text = "NO RECORD ON THIS SEED YET";
                recordLine.color = Palette.TextDim;
            }
            else
            {
                recordLine.text = "BEST " + record.BestDistance + " m   BY " + record.Name
                                  + "   /   " + record.BestMultiplier.ToString("0.00") + "x   /   SCORE " + record.BestPayout.ToString("N0");
                recordLine.color = Palette.Gold;
            }
            nameField.text = App.PlayerName;
            seedField.text = App.Seed;
        }

        public override void Show()
        {
            base.Show();
            Refresh();
            time = 0f;
            UiKit.ScaleIn(title.transform, 0.5f, 0.02f);
            UiKit.ScaleIn(titleGhost.transform, 0.5f, 0.02f);
            UiKit.SlideIn(card, new Vector2(0f, -170f), new Vector2(0f, -52f), 0.52f, 0.05f);
            Group.alpha = 1f;
            UiKit.FadeIn(Group, 0.28f);

            for (int i = 0; i < chips.Count; i++)
            {
                if (chips[i] == null) continue;
                var chipTransform = (RectTransform)chips[i].transform;
                Vector2 target = chipTransform.anchoredPosition;
                UiKit.SlideIn(chipTransform, target + new Vector2(0f, -18f), target, 0.36f, 0.22f + i * 0.05f);
                UiKit.FadeIn(chips[i], 0.3f, 0.22f + i * 0.05f);
            }
        }

        void Update()
        {
            time += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(time * 2.1f) * 0.012f;
            if (title != null) title.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            if (titleGhost != null) titleGhost.rectTransform.localScale = new Vector3(pulse * 1.01f, pulse * 1.01f, 1f);
            if (titleGhost != null) titleGhost.color = Palette.Alpha(Palette.Magenta, 0.16f + Mathf.Sin(time * 2.1f + 1.2f) * 0.06f);
            if (cardGlow != null) cardGlow.color = new Color(0.1f, 0.55f, 0.95f, 0.07f + Mathf.Sin(time * 1.4f) * 0.03f);
        }
    }
}
