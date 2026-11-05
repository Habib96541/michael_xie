using NeonDash.Core;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Title screen: name, seed entry, best distance for the seed, and PLAY.</summary>
    public sealed class UiLobby : UiScreen
    {
        RectTransform title;
        Text seedStatus;
        Text recordLine;
        InputField nameField;
        InputField seedField;
        RectTransform card;
        readonly System.Collections.Generic.List<CanvasGroup> chips = new System.Collections.Generic.List<CanvasGroup>();
        float time;
        bool portrait;
        readonly System.Collections.Generic.List<(RectTransform Item, Rect Wide, Rect Tall)> placements =
            new System.Collections.Generic.List<(RectTransform, Rect, Rect)>();

        protected override void Construct()
        {
            // The track stays visible behind the launch console.
            var shade = UiKit.Sprite9(Root, "Shade", new Color(0.024f, 0.012f, 0.075f, 0.44f), UiKit.Chamfered);
            UiKit.Stretch(shade.rectTransform);

            var logoTexture = Resources.Load<Texture2D>("Brand/GeometryDashLogo");
            if (logoTexture != null)
            {
                title = UiKit.Container(Root, "Geometry Dash Logo");
                var logo = title.gameObject.AddComponent<RawImage>();
                logo.texture = logoTexture;
                logo.uvRect = new Rect(0f, 0.21f, 1f, 0.55f);
                logo.raycastTarget = false;
            }
            else
            {
                title = UiKit.Label(Root, "GEOMETRY DASH", 86, Palette.Cyan, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform;
            }
            Adapt(title, 0f, 292f, 900f, 165f, 0f, 553f, 960f, 175f);

            var subtitle = UiKit.Label(Root, "SEED CHALLENGE  /  ARCADE TIME TRIAL", 23, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Adapt(subtitle.rectTransform, 0f, 187f, 900f, 32f, 0f, 455f, 950f, 36f);

            card = UiKit.Frame(Root, "Launch console", Palette.Cyan);
            Put(card, 0f, -48f, 1040f, 398f);
            var divider = UiKit.Sprite9(card, "Console divider", Palette.PanelLine, UiKit.Chamfered);
            divider.raycastTarget = false;
            Adapt(divider.rectTransform, 92f, 25f, 2f, 245f, 0f, -21f, 820f, 2f);

            var setupHeading = UiKit.Label(card, "COURSE SETUP", 32, Palette.Cyan, TextAnchor.MiddleLeft, FontStyle.Bold);
            Adapt(setupHeading.rectTransform, -199f, 144f, 552f, 48f, 0f, 372f, 820f, 52f);
            var launchHeading = UiKit.Label(card, "30 SECOND RUN", 32, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            Adapt(launchHeading.rectTransform, 297f, 144f, 352f, 48f, 0f, -75f, 820f, 48f);

            var nameLabel = UiKit.Label(card, "PLAYER", 20, Palette.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            Adapt(nameLabel.rectTransform, -199f, 88f, 552f, 25f, 0f, 300f, 820f, 30f);
            nameField = UiKit.Field(card, "YOUR NAME", App.PlayerName, out _);
            Adapt((RectTransform)nameField.transform, -199f, 46f, 552f, 54f, 0f, 235f, 820f, 88f);
            nameField.characterLimit = 12;
            nameField.onValueChanged.AddListener(App.SetPlayerName);
            nameField.onEndEdit.AddListener(_ => App.SaveProfile());

            var seedLabel = UiKit.Label(card, "COURSE SEED", 20, Palette.TextDim, TextAnchor.MiddleLeft, FontStyle.Bold);
            Adapt(seedLabel.rectTransform, -199f, -8f, 552f, 25f, 0f, 167f, 820f, 30f);
            seedField = UiKit.Field(card, "ENTER A SEED", App.Seed, out Text seedEntry);
            seedEntry.fontSize = 24;
            Adapt((RectTransform)seedField.transform, -323f, -48f, 304f, 54f, -192f, 103f, 436f, 88f);
            seedField.characterLimit = 64;
            seedField.onEndEdit.AddListener(ApplySeed);

            Button randomButton = UiKit.Button(card, "NEW", Palette.PanelSoft, out Text randomLabel, 21);
            Adapt((RectTransform)randomButton.transform, -110f, -48f, 106f, 54f, 119f, 103f, 164f, 88f);
            UiKit.AddButtonIcon(randomLabel, IconKind.Shuffle);
            randomButton.onClick.AddListener(delegate
            {
                App.RandomSeed();
                Refresh();
                SetSeedStatus("NEW COURSE GENERATED", Palette.Cyan);
            });

            Button copyButton = UiKit.Button(card, "COPY", Palette.PanelSoft, out Text copyLabel, 21);
            Adapt((RectTransform)copyButton.transform, 7f, -48f, 106f, 54f, 314f, 103f, 192f, 88f);
            UiKit.AddButtonIcon(copyLabel, IconKind.Copy);
            copyButton.onClick.AddListener(CopySeed);

            seedStatus = UiKit.Label(card, "", 19, Palette.TextDim, TextAnchor.MiddleLeft);
            Adapt(seedStatus.rectTransform, -199f, -94f, 552f, 27f, 0f, 30f, 820f, 33f);

            Button playButton = UiKit.Button(card, "PLAY", Palette.Action, out Text playLabel, 40);
            Adapt((RectTransform)playButton.transform, 297f, 48f, 352f, 84f, 0f, -165f, 820f, 126f);
            UiKit.AddButtonIcon(playLabel, IconKind.Play);
            playButton.onClick.AddListener(App.StartRun);

            Button recordsButton = UiKit.Button(card, "RECORDS", Palette.PanelSoft, out Text recordsLabel, 24);
            Adapt((RectTransform)recordsButton.transform, 262f, -48f, 282f, 54f, -65f, -285f, 690f, 91f);
            UiKit.AddButtonIcon(recordsLabel, IconKind.Records);
            recordsButton.onClick.AddListener(App.OpenRecords);

            var soundButton = UiKit.Button(card, "", Palette.PanelSoft, out _, 20, playClick: false);
            Adapt((RectTransform)soundButton.transform, 446f, -48f, 54f, 54f, 365f, -285f, 90f, 91f);
            var soundIcon = UiIcon.Add(soundButton.transform, IconKind.Sound, Palette.Cyan, 28f);
            soundButton.onClick.AddListener(delegate
            {
                App.ToggleMute();
                if (!App.Audio.Muted) App.Audio.PlayClick();
            });
            App.Audio.MuteChanged += muted => soundIcon.SetKind(muted ? IconKind.Muted : IconKind.Sound);
            soundIcon.SetKind(App.Audio.Muted ? IconKind.Muted : IconKind.Sound);

            var recordStrip = UiKit.Sprite9(card, "Seed record strip", Palette.PanelSoft, UiKit.Chamfered);
            Adapt(recordStrip.rectTransform, 0f, -147f, 950f, 49f, 0f, -386f, 820f, 62f);
            var recordCaption = UiKit.Label(recordStrip.transform, "SEED RECORD", 19, Palette.Violet, TextAnchor.MiddleLeft, FontStyle.Bold);
            Adapt(recordCaption.rectTransform, -376f, 0f, 150f, 30f, -315f, 0f, 150f, 30f);
            recordLine = UiKit.Label(recordStrip.transform, "", 23, Palette.Gold, TextAnchor.MiddleLeft);
            Adapt(recordLine.rectTransform, 85f, 0f, 746f, 34f, 71f, 0f, 610f, 34f);

            BuildChips();
            RefreshLayout();
            Refresh();
        }

        void Adapt(RectTransform item, float x, float y, float w, float h, float tallX, float tallY, float tallW, float tallH)
        {
            placements.Add((item, new Rect(x, y, w, h), new Rect(tallX, tallY, tallW, tallH)));
            Put(item, x, y, w, h);
        }

        void RefreshLayout()
        {
            bool tall = Root.rect.height > Root.rect.width * 1.35f;
            if (tall == portrait) return;
            portrait = tall;
            card.sizeDelta = portrait ? new Vector2(960f, 920f) : new Vector2(1040f, 398f);
            foreach (var placement in placements)
            {
                Rect frame = portrait ? placement.Tall : placement.Wide;
                Put(placement.Item, frame.x, frame.y, frame.width, frame.height);
            }
            nameField.textComponent.fontSize = portrait ? 34 : 26;
            seedField.textComponent.fontSize = portrait ? 32 : 24;
        }

        static void Put(RectTransform item, float x, float y, float width, float height)
        {
            var center = new Vector2(0.5f, 0.5f);
            UiKit.Place(item, center, center, new Vector2(x, y), new Vector2(width, height));
        }

        void ApplySeed(string value)
        {
            string previous = App.Seed;
            bool valid = App.SetSeed(value);
            Refresh();
            if (!valid) SetSeedStatus("ENTER AN ND1 CODE OR A NUMERIC SEED", Palette.Gold);
            else if (App.Seed != previous) SetSeedStatus("SHARED COURSE LOADED", Palette.Cyan);
        }

        public void PasteSeed(string value)
        {
            if (!seedField.isFocused) return;
            seedField.SetTextWithoutNotify(value);
            seedField.DeactivateInputField(); // Commits through the same validation as typed codes.
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern int NeonDash_CopySeed(string value);
#endif

        void CopySeed()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (NeonDash_CopySeed(App.Seed) == 0)
            {
                SetSeedStatus("COPY BLOCKED BY THE BROWSER. SHARE THE CODE SHOWN ABOVE.", Palette.Gold);
                return;
            }
#else
            GUIUtility.systemCopyBuffer = App.Seed;
#endif
            SetSeedStatus("SEED COPIED", Palette.Cyan);
        }

        void SetSeedStatus(string message, Color color)
        {
            seedStatus.text = message;
            seedStatus.color = color;
        }

        /// <summary>Three quick rule chips under the card: the whole game explained in one line.</summary>
        void BuildChips()
        {
            string[] titles = { "TAP TO JUMP", "DODGE EVERY SPIKE", "GEMS = MULTIPLIER" };
            Color[] accents = { Palette.Cyan, Palette.Magenta, Palette.Gold };
            float[] offsets = { -270f, 0f, 270f };

            for (int i = 0; i < titles.Length; i++)
            {
                var chipImage = UiKit.Sprite9(Root, "Chip " + i, Palette.Alpha(Palette.Panel, 0.9f), UiKit.Chamfered);
                Adapt(chipImage.rectTransform, offsets[i], -303f, 250f, 46f, offsets[i], -560f, 250f, 46f);

                var accent = UiKit.Sprite9(chipImage.transform, "Accent", accents[i], UiKit.Chamfered);
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
            seedField.SetTextWithoutNotify(App.Seed);
            SetSeedStatus("PASTE A SHARED SEED TO PLAY THE SAME COURSE", Palette.TextDim);
        }

        public override void Show()
        {
            base.Show();
            RefreshLayout();
            Refresh();
            time = 0f;
            UiKit.ScaleIn(title, 0.5f, 0.02f);
            UiKit.SlideIn(card, new Vector2(0f, -100f), new Vector2(0f, -48f), 0.52f, 0.05f);
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
            RefreshLayout();
            time += Time.unscaledDeltaTime;
            float pulse = 1f + Mathf.Sin(time * 2.1f) * 0.012f;
            if (title != null) title.localScale = new Vector3(pulse, pulse, 1f);
        }
    }
}
