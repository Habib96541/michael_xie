using System.Reflection;
using CyberWhack.Art;
using CyberWhack.Gameplay;
using CyberWhack.VFX;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.UI
{
    /// <summary>
    /// Sunny burrow presentation. Swaps in the painted desert art, readable
    /// parchment UI, and the pause card. Rules and object references stay put.
    /// </summary>
    public static class NeonBurrowTheme
    {
        public static void Apply(CyberHUD hud, HatchNode[] nodes)
        {
            Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            CartoonArtLibrary.EnsureLoaded();
            ConfigureCanvas(canvas);
            ConfigureBackground(canvas);

            RectTransform safeArea = FindRect(canvas.transform, "SafeAreaContainer");
            if (safeArea == null) safeArea = canvas.transform as RectTransform;
            RectTransform grid = FindRect(canvas.transform, "HatchGridContainer");
            RectTransform menu = FindRect(canvas.transform, "MainMenuPanel");
            RectTransform header = FindRect(canvas.transform, "InGameHeader");
            RectTransform intel = FindRect(canvas.transform, "TargetIntelModal");
            RectTransform gameOver = FindRect(canvas.transform, "GameOverModal");

            ConfigureTargets(nodes);
            ConfigureMenu(menu);
            ConfigureHeader(header, hud);
            ConfigureModal(intel, false);
            ConfigureModal(gameOver, true);
            ConfigureCountdown(canvas, hud);
            GameObject scrim = EnsureScrim(safeArea);
            GameObject pause = EnsurePause(safeArea);
            WirePause(hud, pause, scrim);

            if (hud != null)
            {
                hud.ConfigureAudioSprites(CartoonArtLibrary.AudioOn, CartoonArtLibrary.AudioOff);
                SetPrivateField(hud, "modalScrim", scrim);
            }

            EffectPoolManager pool = EffectPoolManager.Instance;
            if (pool != null) pool.ConfigureVisuals(CartoonArtLibrary.Sunburst);

            ResponsiveArcadeLayout responsive = canvas.GetComponent<ResponsiveArcadeLayout>();
            if (responsive == null) responsive = canvas.gameObject.AddComponent<ResponsiveArcadeLayout>();
            responsive.Configure(grid, menu, header, intel, gameOver, nodes,
                pause != null ? pause.transform.Find("PauseCard") as RectTransform : null);

            if (safeArea != null && safeArea.GetComponent<SafeAreaFitter>() == null)
            {
                safeArea.gameObject.AddComponent<SafeAreaFitter>();
            }

            ArcadeFeel.Attach(grid, hud);
            AddSunnyPolish(canvas, safeArea, menu, header, hud, nodes);
            StyleButtons(canvas, menu);
            if (hud != null) hud.RefreshBindings();
        }

        /// <summary>
        /// The finishing pass: living desert air behind everything, a sticker
        /// shadow under the logo, a beacon under Start, and glow beds behind
        /// the combo bar and multiplier so the HUD breathes with the streak.
        /// </summary>
        private static void AddSunnyPolish(Canvas canvas, RectTransform safeArea, RectTransform menu, RectTransform header, CyberHUD hud, HatchNode[] nodes)
        {
            DesertAtmosphere.Install(canvas, safeArea, nodes);

            if (menu != null)
            {
                AddTitleShadow(menu.Find("HeroTitleBanner"));
                AddStartBeacon(menu.Find("StartButton"));
            }

            if (header == null) return;

            Transform track = header.Find("ComboPanel/ComboTrack");
            if (track != null)
            {
                Image glow = AddGlow(track, "ComboGlow", PolishSprites.SoftDot,
                    new Color(1f, 0.78f, 0.28f, 0.08f),
                    new Vector2(0.01f, -0.55f), new Vector2(0.99f, 1.35f), false);
                SetPrivateField(hud, "comboGlow", glow);
            }

            Transform badge = header.Find("ComboPanel/MultiplierBadge");
            if (badge != null)
            {
                Image halo = AddGlow(badge, "BadgeHalo", PolishSprites.SunRays,
                    new Color(1f, 0.86f, 0.42f, 0.10f),
                    new Vector2(-0.45f, -0.45f), new Vector2(1.45f, 1.45f), false);
                SetPrivateField(hud, "multiplierGlow", halo);
            }
        }

        private static Image AddGlow(Transform parent, string name, Sprite sprite, Color tint, Vector2 min, Vector2 max, bool pulse)
        {
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(parent, false);
                obj.transform.SetAsFirstSibling();
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.color = tint;
            image.raycastTarget = false;
            image.preserveAspect = false;

            if (pulse && obj.GetComponent<PulseGlow>() == null) obj.AddComponent<PulseGlow>();
            return image;
        }

        private static void AddStartBeacon(Transform start)
        {
            if (start == null) return;

            Image glow = AddGlow(start, "StartBeacon", PolishSprites.SoftDot,
                new Color(1f, 0.86f, 0.36f, 0.16f),
                new Vector2(-0.10f, -0.55f), new Vector2(1.10f, 1.55f), true);

            PulseGlow pulse = glow != null ? glow.GetComponent<PulseGlow>() : null;
            if (pulse != null) pulse.Configure(1.9f, 0.10f, 0.16f);
        }

        /// <summary>
        /// A sticker-style drop shadow baked into the banner mesh. Using the
        /// Graphic effect keeps the shadow under the art automatically, and it
        /// follows the logo's idle sway without a second transform to babysit.
        /// </summary>
        private static void AddTitleShadow(Transform title)
        {
            if (title == null) return;

            Shadow shadow = title.GetComponent<Shadow>();
            if (shadow == null) shadow = title.gameObject.AddComponent<Shadow>();

            shadow.effectColor = new Color(0.28f, 0.12f, 0.03f, 0.38f);
            shadow.effectDistance = new Vector2(10f, -12f);
            shadow.useGraphicAlpha = true;
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            canvas.transform.localScale = Vector3.one;
            Application.targetFrameRate = 60;

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = CartoonArtLibrary.Sand;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void ConfigureBackground(Canvas canvas)
        {
            Transform background = canvas.transform.Find("FullscreenBackground");
            if (background == null)
            {
                GameObject backgroundObject = new GameObject("FullscreenBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backgroundObject.transform.SetParent(canvas.transform, false);
                RectTransform rect = backgroundObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                background = backgroundObject.transform;
                background.SetAsFirstSibling();
            }

            Image image = background.GetComponent<Image>();
            if (image == null) image = background.gameObject.AddComponent<Image>();
            image.sprite = CartoonArtLibrary.Background;
            image.color = CartoonArtLibrary.Background != null ? Color.white : CartoonArtLibrary.Sand;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static void ConfigureTargets(HatchNode[] nodes)
        {
            if (nodes == null) return;
            for (int i = 0; i < nodes.Length; i++)
            {
                HatchNode node = nodes[i];
                if (node == null) continue;
                node.ConfigureSprites(
                    CartoonArtLibrary.StandardMole,
                    CartoonArtLibrary.GoldMole,
                    CartoonArtLibrary.HardHatMole,
                    CartoonArtLibrary.HardHatCracked,
                    CartoonArtLibrary.Bomb);
                node.UseArcadeMotion(0.12f);

                Image ring = node.GetComponent<Image>();
                if (ring != null)
                {
                    ring.sprite = CartoonArtLibrary.Hatch;
                    ring.color = Color.white;
                    ring.preserveAspect = false;
                    ring.raycastTarget = true;
                }

                Transform face = node.transform.Find("MaskContainer/TargetSprite");
                Image faceImage = face != null ? face.GetComponent<Image>() : null;
                if (faceImage != null) faceImage.preserveAspect = true;

                ConfigureHatchFrontMound(node, CartoonArtLibrary.DirtMound);
            }
        }

        private static void ConfigureHatchFrontMound(HatchNode node, Sprite sprite)
        {
            if (node == null || sprite == null) return;

            Transform existing = node.transform.Find("HatchFrontMound");
            GameObject moundObject;
            if (existing != null)
            {
                moundObject = existing.gameObject;
            }
            else
            {
                moundObject = new GameObject("HatchFrontMound", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                moundObject.transform.SetParent(node.transform, false);
            }

            RectTransform rect = moundObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.07f, 0.07f);
            rect.anchorMax = new Vector2(0.93f, 0.36f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = moundObject.GetComponent<Image>();
            image.sprite = sprite;
            image.color = new Color32(220, 166, 119, 255);
            image.preserveAspect = true;
            image.raycastTarget = false;
            moundObject.transform.SetAsLastSibling();
        }

        private static void ConfigureMenu(RectTransform menu)
        {
            if (menu == null) return;

            ThemePanel(menu.Find("RecordCard"), CartoonArtLibrary.BestScorePlaque != null ? CartoonArtLibrary.BestScorePlaque : CartoonArtLibrary.Plaque);
            ThemePanel(menu.Find("SeedBox"), CartoonArtLibrary.SeedBar != null ? CartoonArtLibrary.SeedBar : CartoonArtLibrary.Plaque);
            ThemePanel(menu.Find("IntelButton"), CartoonArtLibrary.ButtonGreen);
            ThemePanel(menu.Find("StartButton"), CartoonArtLibrary.ButtonGold);
            ThemePanel(menu.Find("SeedBox/RndBtn"), CartoonArtLibrary.ButtonWood);

            Transform audio = menu.Find("AudioToggleBtn");
            if (audio != null)
            {
                AspectRatioFitter fitter = audio.GetComponent<AspectRatioFitter>();
                if (fitter != null) Object.Destroy(fitter);

                Image audioImage = audio.GetComponent<Image>();
                if (audioImage != null)
                {
                    audioImage.sprite = CartoonArtLibrary.AudioOn;
                    audioImage.preserveAspect = true;
                    audioImage.color = Color.white;
                }
            }

            Transform hero = menu.Find("HeroTitleBanner");
            if (hero != null)
            {
                Image heroImage = hero.GetComponent<Image>();
                if (heroImage != null)
                {
                    heroImage.sprite = CartoonArtLibrary.Title;
                    heroImage.color = CartoonArtLibrary.Title != null ? Color.white : CartoonArtLibrary.Ink;
                    heroImage.preserveAspect = true;
                    heroImage.raycastTarget = false;
                }

                HideIfPresent(hero.Find("TitleText"));
                HideIfPresent(hero.Find("SubtitleText"));
                HideIfPresent(hero.Find("FallbackTitle"));
                if (CartoonArtLibrary.Title == null)
                {
                    TextMeshProUGUI title = EnsureText(hero, "TitleText", "WHACK-A-MOLE", 52f, FontStyles.Bold, CartoonArtLibrary.Cream, TextAlignmentOptions.Center);
                    SetRect(title.rectTransform, Vector2.zero, Vector2.one);
                    title.gameObject.SetActive(true);
                    Outline(title, CartoonArtLibrary.Outline, 0.22f);
                }
            }

            HideIfPresent(menu.Find("HeroTitle"));
            HideIfPresent(menu.Find("HeroSub"));
            HideIfPresent(menu.Find("KickerText"));

            Transform startBtn = menu.Find("StartButton");
            if (startBtn != null)
            {
                Image startImg = startBtn.GetComponent<Image>();
                if (startImg != null)
                {
                    startImg.sprite = CartoonArtLibrary.ButtonGold;
                    startImg.preserveAspect = true;
                    startImg.color = Color.white;
                }
                TextMeshProUGUI startTmp = startBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (startTmp != null)
                {
                    startTmp.gameObject.SetActive(true);
                    startTmp.rectTransform.anchorMin = new Vector2(0.34f, 0f);
                    startTmp.rectTransform.anchorMax = new Vector2(0.96f, 1f);
                    startTmp.rectTransform.offsetMin = Vector2.zero;
                    startTmp.rectTransform.offsetMax = Vector2.zero;
                    Fit(startTmp, "Start", 42f, CartoonArtLibrary.Cream, TextAlignmentOptions.Center, false);
                    startTmp.outlineColor = CartoonArtLibrary.Outline;
                    startTmp.outlineWidth = 0.08f;
                }
            }

            Transform intelBtn = menu.Find("IntelButton");
            if (intelBtn != null)
            {
                Image intelImg = intelBtn.GetComponent<Image>();
                if (intelImg != null)
                {
                    intelImg.sprite = CartoonArtLibrary.ButtonGreen;
                    intelImg.preserveAspect = true;
                    intelImg.color = Color.white;
                }
                TextMeshProUGUI intelTmp = intelBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (intelTmp != null)
                {
                    intelTmp.gameObject.SetActive(true);
                    intelTmp.rectTransform.anchorMin = new Vector2(0.30f, 0f);
                    intelTmp.rectTransform.anchorMax = new Vector2(0.96f, 1f);
                    intelTmp.rectTransform.offsetMin = Vector2.zero;
                    intelTmp.rectTransform.offsetMax = Vector2.zero;
                    Fit(intelTmp, "Rules", 34f, CartoonArtLibrary.Cream, TextAlignmentOptions.Center, false);
                    intelTmp.outlineColor = new Color32(24, 64, 20, 255);
                    intelTmp.outlineWidth = 0.15f;
                }
            }

            PaintButton(menu.Find("SeedBox/RndBtn"), "Random", 30f);
            PaintInk(menu.Find("RecordCard"));
            PaintInk(menu.Find("SeedBox"));

            Transform recordCard = menu.Find("RecordCard");
            if (recordCard != null)
            {
                TextMeshProUGUI recordTmp = recordCard.GetComponentInChildren<TextMeshProUGUI>(true);
                if (recordTmp != null)
                {
                    recordTmp.rectTransform.anchorMin = new Vector2(0.30f, 0.08f);
                    recordTmp.rectTransform.anchorMax = new Vector2(0.96f, 0.92f);
                    recordTmp.rectTransform.offsetMin = new Vector2(4f, 2f);
                    recordTmp.rectTransform.offsetMax = new Vector2(-4f, -2f);
                    recordTmp.enableAutoSizing = false;
                    recordTmp.fontSize = 52f;
                    recordTmp.fontStyle = FontStyles.Bold;
                    recordTmp.alignment = TextAlignmentOptions.Center;
                    recordTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                    recordTmp.textWrappingMode = TextWrappingModes.Normal;
                    recordTmp.overflowMode = TextOverflowModes.Truncate;
                    recordTmp.text = "<size=72%><color=#7a4018>PERSONAL BEST</color></size>\n<size=112%><b><color=#3a1e0c>500</color></b></size>";
                }
            }

            Transform seedBox = menu.Find("SeedBox");
            if (seedBox != null)
            {
                Transform lbl = seedBox.Find("Lbl");
                if (lbl != null)
                {
                    RectTransform lblRt = lbl as RectTransform;
                    lblRt.anchorMin = new Vector2(0.04f, 0.08f);
                    lblRt.anchorMax = new Vector2(0.22f, 0.92f);
                    lblRt.offsetMin = Vector2.zero;
                    lblRt.offsetMax = Vector2.zero;
                    TextMeshProUGUI lblTmp = lbl.GetComponent<TextMeshProUGUI>();
                    if (lblTmp != null)
                    {
                        Fit(lblTmp, "SEED", 30f, CartoonArtLibrary.Caramel, TextAlignmentOptions.MidlineLeft, false);
                    }
                }

                Transform seedInput = seedBox.Find("SeedInput");
                if (seedInput != null)
                {
                    RectTransform inputRt = seedInput as RectTransform;
                    inputRt.anchorMin = new Vector2(0.24f, 0.12f);
                    inputRt.anchorMax = new Vector2(0.72f, 0.88f);
                    inputRt.offsetMin = Vector2.zero;
                    inputRt.offsetMax = Vector2.zero;

                    Image inputImage = seedInput.GetComponent<Image>();
                    if (inputImage != null)
                    {
                        inputImage.sprite = null;
                        inputImage.color = new Color32(255, 245, 218, 255);
                    }
                    TextMeshProUGUI[] inputText = seedInput.GetComponentsInChildren<TextMeshProUGUI>(true);
                    for (int i = 0; i < inputText.Length; i++)
                    {
                        RectTransform textRect = inputText[i].rectTransform;
                        textRect.offsetMin = new Vector2(12f, 2f);
                        textRect.offsetMax = new Vector2(-12f, -2f);
                        inputText[i].color = CartoonArtLibrary.Ink;
                        inputText[i].outlineWidth = 0f;
                        inputText[i].fontSize = 32f;
                        inputText[i].fontSizeMin = 22f;
                        inputText[i].fontSizeMax = 32f;
                        inputText[i].enableAutoSizing = true;
                        inputText[i].textWrappingMode = TextWrappingModes.NoWrap;
                        inputText[i].overflowMode = TextOverflowModes.Ellipsis;
                        inputText[i].alignment = TextAlignmentOptions.Center;
                        inputText[i].verticalAlignment = VerticalAlignmentOptions.Middle;
                    }
                }

                Transform rndBtn = seedBox.Find("RndBtn");
                if (rndBtn != null)
                {
                    RectTransform rndRt = rndBtn as RectTransform;
                    rndRt.anchorMin = new Vector2(0.74f, 0.12f);
                    rndRt.anchorMax = new Vector2(0.96f, 0.88f);
                    rndRt.offsetMin = Vector2.zero;
                    rndRt.offsetMax = Vector2.zero;
                    PaintButton(rndBtn, "Random", 30f);
                }
            }

            EnsureLevelBadge(menu);

            TextMeshProUGUI footer = EnsureText(menu, "Footer", "60 SEC  ·  TAP MOLES  ·  AVOID BOMBS", 32f, FontStyles.Bold, CartoonArtLibrary.Cream, TextAlignmentOptions.Center);
            SetRect(footer.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.14f));
            Fit(footer, "60 SEC  ·  TAP MOLES  ·  AVOID BOMBS", 32f, CartoonArtLibrary.Cream, TextAlignmentOptions.Center, false);
            footer.outlineColor = CartoonArtLibrary.Outline;
            footer.outlineWidth = 0.08f;

            ApplyPreview(menu.Find("PreviewShowcaseRow"));
        }

        private static void EnsureLevelBadge(RectTransform menu)
        {
            if (menu == null) return;
            Transform existing = menu.Find("LevelBadge");
            GameObject badgeObj;
            if (existing != null)
            {
                badgeObj = existing.gameObject;
            }
            else
            {
                badgeObj = new GameObject("LevelBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                badgeObj.transform.SetParent(menu, false);
            }
            badgeObj.SetActive(true);
            // Strip any fitter or pulse glow so size is strictly driven by layout anchors
            AspectRatioFitter fitter = badgeObj.GetComponent<AspectRatioFitter>();
            if (fitter != null) Object.Destroy(fitter);

            PulseGlow pulse = badgeObj.GetComponent<PulseGlow>();
            if (pulse != null) Object.Destroy(pulse);

            badgeObj.transform.localScale = Vector3.one;

            Image image = badgeObj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = CartoonArtLibrary.Badge != null ? CartoonArtLibrary.Badge : CartoonArtLibrary.Sunburst;
                image.color = Color.white;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            // Ensure Level Text "1" on Main Menu LevelBadge
            TextMeshProUGUI lvlText = EnsureText(badgeObj.transform, "LevelText", "1", 72f, FontStyles.Bold, new Color32(48, 18, 6, 255), TextAlignmentOptions.Center);
            SetRect(lvlText.rectTransform, Vector2.zero, Vector2.one);
            lvlText.rectTransform.offsetMin = Vector2.zero;
            lvlText.rectTransform.offsetMax = Vector2.zero;
            lvlText.enableAutoSizing = true;
            lvlText.fontSizeMin = 48f;
            lvlText.fontSizeMax = 72f;
        }

        private static void ConfigureHeader(RectTransform header, CyberHUD hud)
        {
            if (header == null) return;

            ThemePanel(header.Find("SeedBadge"), CartoonArtLibrary.ParchmentRow != null ? CartoonArtLibrary.ParchmentRow : CartoonArtLibrary.Plaque);
            ThemePanel(header.Find("TimerBox"), CartoonArtLibrary.TimeCard != null ? CartoonArtLibrary.TimeCard : CartoonArtLibrary.Plaque);
            ThemePanel(header.Find("ScoreBox"), CartoonArtLibrary.ScoreCard != null ? CartoonArtLibrary.ScoreCard : CartoonArtLibrary.ParchmentRow);
            ThemePanel(header.Find("AbortBtn"), CartoonArtLibrary.ButtonPause != null ? CartoonArtLibrary.ButtonPause : CartoonArtLibrary.ButtonWood);

            Transform abort = header.Find("AbortBtn");
            if (abort != null)
            {
                Image abortImg = abort.GetComponent<Image>();
                if (abortImg != null)
                {
                    abortImg.sprite = CartoonArtLibrary.ButtonPause != null ? CartoonArtLibrary.ButtonPause : CartoonArtLibrary.ButtonWood;
                    abortImg.preserveAspect = true;
                    abortImg.color = Color.white;
                }
                TextMeshProUGUI abortTxt = abort.GetComponentInChildren<TextMeshProUGUI>(true);
                if (abortTxt != null)
                {
                    abortTxt.gameObject.SetActive(true);
                    abortTxt.rectTransform.anchorMin = new Vector2(0f, 0.04f);
                    abortTxt.rectTransform.anchorMax = new Vector2(1f, 0.42f);
                    abortTxt.rectTransform.offsetMin = Vector2.zero;
                    abortTxt.rectTransform.offsetMax = Vector2.zero;
                    Fit(abortTxt, "Pause", 28f, CartoonArtLibrary.Cream, TextAlignmentOptions.Center, false);
                    abortTxt.outlineColor = CartoonArtLibrary.Outline;
                    abortTxt.outlineWidth = 0.08f;
                }
            }

            Transform audio = header.Find("InGameAudioToggle");
            if (audio != null)
            {
                AspectRatioFitter fitter = audio.GetComponent<AspectRatioFitter>();
                if (fitter != null) Object.Destroy(fitter);

                Image audioImage = audio.GetComponent<Image>();
                if (audioImage != null)
                {
                    audioImage.sprite = CartoonArtLibrary.AudioOn;
                    audioImage.preserveAspect = true;
                    audioImage.color = Color.white;
                }
            }

            PaintInk(header.Find("SeedBadge"));

            Transform timerBox = header.Find("TimerBox");
            Transform scoreBox = header.Find("ScoreBox");
            Fit(timerBox != null ? timerBox.Find("TimerTitle")?.GetComponent<TextMeshProUGUI>() : null, "TIME", 36f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
            Fit(scoreBox != null ? scoreBox.Find("ScoreTitle")?.GetComponent<TextMeshProUGUI>() : null, "SCORE", 36f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
            Fit(timerBox != null ? timerBox.Find("TimerDigits")?.GetComponent<TextMeshProUGUI>() : null, "1:00", 54f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
            Fit(scoreBox != null ? scoreBox.Find("ScoreDigits")?.GetComponent<TextMeshProUGUI>() : null, "0", 56f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
            HideIfPresent(timerBox != null ? timerBox.Find("ClockIcon") : null);
            HideIfPresent(scoreBox != null ? scoreBox.Find("CoinIcon") : null);
            AssignIcon(scoreBox, "CoinIcon", CartoonArtLibrary.Coin);

            if (timerBox != null)
            {
                Image timerGauge = EnsureProgressBar(timerBox, "TimerProgressTrack", "TimerProgressFill");
                SetPrivateField(hud, "timerCircleGauge", timerGauge);
            }

            Transform combo = header.Find("ComboPanel");
            if (combo != null)
            {
                Transform badge = combo.Find("MultiplierBadge");
                if (badge == null)
                {
                    GameObject badgeObject = new GameObject("MultiplierBadge", typeof(RectTransform));
                    badgeObject.transform.SetParent(combo, false);
                    badge = badgeObject.transform;
                }

                RectTransform badgeRect = badge as RectTransform;
                // Keep the trophy artwork clear and place the active multiplier in the open center lane.
                SetRect(badgeRect, new Vector2(0.30f, 0.38f), new Vector2(0.73f, 0.94f));
                badge.SetAsFirstSibling();

                Image badgeImage = badge.GetComponent<Image>();
                if (badgeImage != null)
                {
                    badgeImage.color = Color.clear;
                    badgeImage.raycastTarget = false;
                }

                Transform multiplierTransform = badge.Find("MultiplierText");
                if (multiplierTransform == null) multiplierTransform = combo.Find("MultiplierText");
                TextMeshProUGUI multiplier = multiplierTransform != null
                    ? multiplierTransform.GetComponent<TextMeshProUGUI>()
                    : null;
                if (multiplier != null)
                {
                    if (multiplier.transform.parent != badge) multiplier.transform.SetParent(badge, false);
                    SetRect(multiplier.rectTransform, Vector2.zero, Vector2.one);
                    Fit(multiplier, "1x", 46f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
                }
                SetPrivateField(hud, "multiplierBadgeTransform", badgeRect);

                Image progressTrack = EnsureComboProgress(combo, out Image progressFill);
                if (progressTrack != null)
                {
                    progressTrack.transform.SetAsFirstSibling();
                    SetPrivateField(hud, "comboStreakFill", progressFill);
                }

                TextMeshProUGUI streakLabel = FindTextRecursive(combo, "StreakText");
                Fit(streakLabel, "Combo", 34f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);

                Transform track = combo.Find("ComboTrack");
                if (track != null)
                {
                    Image trackImage = track.GetComponent<Image>();
                    if (trackImage != null && CartoonArtLibrary.ComboTrack != null)
                    {
                        trackImage.sprite = CartoonArtLibrary.ComboTrack;
                        trackImage.color = Color.white;
                    }
                    Transform fill = track.Find("ComboFill");
                    if (fill != null)
                    {
                        Image fillImage = fill.GetComponent<Image>();
                        if (fillImage != null && CartoonArtLibrary.ComboFill != null)
                        {
                            fillImage.sprite = CartoonArtLibrary.ComboFill;
                            fillImage.color = Color.white;
                            fillImage.type = Image.Type.Filled;
                            fillImage.fillMethod = Image.FillMethod.Horizontal;
                            SetPrivateField(hud, "comboStreakFill", fillImage);
                        }
                    }
                    TextMeshProUGUI streak = track.Find("StreakText")?.GetComponent<TextMeshProUGUI>();
                    Fit(streak, "Combo", 34f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
                }
            }

            SetPrivateField(hud, "inGameSeedText", FindTextRecursive(header, "InGameSeed"));
            SetPrivateField(hud, "comboStreakCountText", FindTextRecursive(header, "StreakText"));
        }

        private static void ConfigureModal(RectTransform modal, bool gameOver)
        {
            if (modal == null) return;
            Image background = modal.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = CartoonArtLibrary.Parchment != null ? CartoonArtLibrary.Parchment : CartoonArtLibrary.Plaque;
                background.color = Color.white;
                background.type = Image.Type.Simple;
            }

            if (gameOver)
            {
                ThemePanel(modal.Find("RetrySameBtn"), CartoonArtLibrary.ButtonGold);
                ThemePanel(modal.Find("RetryNewBtn"), CartoonArtLibrary.ButtonGreen);
                ThemePanel(modal.Find("ReturnMenuBtn"), CartoonArtLibrary.ButtonWood);
                SetRect(modal.Find("RetrySameBtn") as RectTransform, new Vector2(0.10f, 0.24f), new Vector2(0.90f, 0.32f));
                SetRect(modal.Find("RetryNewBtn") as RectTransform, new Vector2(0.10f, 0.15f), new Vector2(0.90f, 0.23f));
                SetRect(modal.Find("ReturnMenuBtn") as RectTransform, new Vector2(0.10f, 0.06f), new Vector2(0.90f, 0.14f));
                PaintButton(modal.Find("RetrySameBtn"), "Play again", 38f);
                PaintButton(modal.Find("RetryNewBtn"), "New burrow", 36f);
                PaintButton(modal.Find("ReturnMenuBtn"), "Main menu", 34f);
            }
            else
            {
                ThemePanel(modal.Find("CloseBtn"), CartoonArtLibrary.ButtonGold);
                PaintButton(modal.Find("CloseBtn"), "Back to game", 34f);
                Sprite[] icons = { CartoonArtLibrary.StandardMole, CartoonArtLibrary.GoldMole, CartoonArtLibrary.HardHatMole, CartoonArtLibrary.Bomb };
                string[] titles = { "Mole", "Crown mole", "Hard hat", "Bomb" };
                string[] blurbs = { "One tap", "A quick tap", "Tap twice", "Do not tap" };
                string[] points = { "+100", "+300", "+250", "−200" };
                Color[] pointColors = { CartoonArtLibrary.Caramel, CartoonArtLibrary.Gold, CartoonArtLibrary.Ink, CartoonArtLibrary.Danger };
                for (int i = 0; i < icons.Length; i++)
                {
                    Transform row = modal.Find("Row" + (i + 1));
                    ThemePanel(row, CartoonArtLibrary.SeedBar != null ? CartoonArtLibrary.SeedBar : CartoonArtLibrary.Plaque);
                    Image icon = row != null ? row.Find("Icon")?.GetComponent<Image>() : null;
                    if (icon != null)
                    {
                        icon.sprite = icons[i];
                        icon.preserveAspect = true;
                        icon.color = Color.white;
                    }
                    float rowTop = 0.89f - i * 0.17f;
                    float rowBottom = rowTop - 0.16f;
                    SetRect(row as RectTransform, new Vector2(0.04f, rowBottom), new Vector2(0.96f, rowTop));
                    SetRect(row != null ? row.Find("Icon") as RectTransform : null, new Vector2(0.03f, 0.12f), new Vector2(0.22f, 0.88f));
                    SetRect(row != null ? row.Find("Title") as RectTransform : null, new Vector2(0.26f, 0.50f), new Vector2(0.70f, 0.90f));
                    SetRect(row != null ? row.Find("Desc") as RectTransform : null, new Vector2(0.26f, 0.10f), new Vector2(0.70f, 0.50f));
                    SetRect(row != null ? row.Find("Pts") as RectTransform : null, new Vector2(0.70f, 0.15f), new Vector2(0.97f, 0.85f));
                    Fit(row != null ? row.Find("Title")?.GetComponent<TextMeshProUGUI>() : null, titles[i], 36f, CartoonArtLibrary.Ink, TextAlignmentOptions.MidlineLeft, false);
                    Fit(row != null ? row.Find("Desc")?.GetComponent<TextMeshProUGUI>() : null, blurbs[i], 28f, CartoonArtLibrary.Caramel, TextAlignmentOptions.MidlineLeft, false);
                    Fit(row != null ? row.Find("Pts")?.GetComponent<TextMeshProUGUI>() : null, points[i], 36f, pointColors[i], TextAlignmentOptions.MidlineRight, false);
                }
            }

            TextMeshProUGUI[] labels = modal.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (IsInsideButton(labels[i].transform)) continue;
                if (labels[i].gameObject.name == "Pts" || labels[i].gameObject.name == "Desc") continue;
                labels[i].color = CartoonArtLibrary.Ink;
                labels[i].raycastTarget = false;
            }

            if (!gameOver)
            {
                SetRect(modal.Find("Title") as RectTransform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.97f));
                Fit(modal.Find("Title")?.GetComponent<TextMeshProUGUI>(), "How to play", 48f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
                Fit(modal.Find("ComboRules/Txt")?.GetComponent<TextMeshProUGUI>(), "Build a streak: 5 hits = 2x  ·  10 = 3x  ·  15 = 4x", 28f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, true);
            }
            else
            {
                Image scoreCard = EnsureModalImage(modal, "FinalScoreCard", CartoonArtLibrary.SeedBar);
                if (scoreCard != null)
                {
                    SetRect(scoreCard.rectTransform, new Vector2(0.13f, 0.67f), new Vector2(0.87f, 0.82f));
                    scoreCard.transform.SetAsFirstSibling();
                }

                PlaceResultCard(modal, "BestResultCard", new Vector2(0.06f, 0.51f), new Vector2(0.48f, 0.59f));
                PlaceResultCard(modal, "StreakResultCard", new Vector2(0.52f, 0.51f), new Vector2(0.94f, 0.59f));
                PlaceResultCard(modal, "AccuracyResultCard", new Vector2(0.06f, 0.42f), new Vector2(0.48f, 0.50f));
                PlaceResultCard(modal, "StatsResultCard", new Vector2(0.52f, 0.42f), new Vector2(0.94f, 0.50f));

                PlaceResultLine(modal, "Title", 0.92f, 0.98f);
                PlaceResultLine(modal, "RankBadge", 0.85f, 0.91f);
                SetRect(modal.Find("FinalScore") as RectTransform, new Vector2(0.13f, 0.67f), new Vector2(0.87f, 0.76f));
                PlaceResultLine(modal, "NewRecord", 0.605f, 0.65f);
                SetRect(modal.Find("HighScore") as RectTransform, new Vector2(0.06f, 0.51f), new Vector2(0.48f, 0.59f));
                SetRect(modal.Find("MaxStreak") as RectTransform, new Vector2(0.52f, 0.51f), new Vector2(0.94f, 0.59f));
                SetRect(modal.Find("Accuracy") as RectTransform, new Vector2(0.06f, 0.42f), new Vector2(0.48f, 0.50f));
                SetRect(modal.Find("Stats") as RectTransform, new Vector2(0.52f, 0.42f), new Vector2(0.94f, 0.50f));
                PlaceResultLine(modal, "FinalSeed", 0.355f, 0.405f);

                TextMeshProUGUI scoreCaption = EnsureText(modal, "FinalScoreCaption", "FINAL SCORE", 32f, FontStyles.Bold, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center);
                SetRect(scoreCaption.rectTransform, new Vector2(0.13f, 0.76f), new Vector2(0.87f, 0.82f));

                Fit(modal.Find("Title")?.GetComponent<TextMeshProUGUI>(), "Time's up!", 56f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
                Fit(modal.Find("RankBadge")?.GetComponent<TextMeshProUGUI>(), "Dirt rookie", 38f, CartoonArtLibrary.Gold, TextAlignmentOptions.Center, false);
                Fit(modal.Find("FinalScore")?.GetComponent<TextMeshProUGUI>(), "0", 90f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
                Fit(scoreCaption, "FINAL SCORE", 32f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
                Fit(modal.Find("NewRecord")?.GetComponent<TextMeshProUGUI>(), "New best!", 32f, CartoonArtLibrary.Gold, TextAlignmentOptions.Center, false);
                Fit(modal.Find("HighScore")?.GetComponent<TextMeshProUGUI>(), "Best  0", 38f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
                Fit(modal.Find("MaxStreak")?.GetComponent<TextMeshProUGUI>(), "Streak  0", 38f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
                Fit(modal.Find("Accuracy")?.GetComponent<TextMeshProUGUI>(), "Accuracy  0%", 36f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);
                Fit(modal.Find("Stats")?.GetComponent<TextMeshProUGUI>(), "Hits  0  ·  Bombs  0", 34f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
                Fit(modal.Find("FinalSeed")?.GetComponent<TextMeshProUGUI>(), "Code  000000", 32f, CartoonArtLibrary.Caramel, TextAlignmentOptions.Center, false);
            }
        }

        private static void PlaceResultCard(RectTransform modal, string name, Vector2 min, Vector2 max)
        {
            Image card = EnsureModalImage(modal, name, CartoonArtLibrary.SeedBar);
            if (card == null) return;
            SetRect(card.rectTransform, min, max);
            card.transform.SetAsFirstSibling();
        }

        private static Image EnsureModalImage(Transform parent, string name, Sprite sprite)
        {
            if (parent == null) return null;
            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(parent, false);
            }

            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
            return image;
        }

        private static void PlaceResultLine(RectTransform modal, string childName, float yMin, float yMax)
        {
            SetRect(modal.Find(childName) as RectTransform, new Vector2(0.10f, yMin), new Vector2(0.90f, yMax));
        }

        private static void ConfigureCountdown(Canvas canvas, CyberHUD hud)
        {
            Transform ready = canvas.transform.Find("SafeAreaContainer/ReadyOverlay");
            if (ready == null) ready = canvas.transform.Find("ReadyOverlay");
            if (ready == null) return;

            Image frame = ready.GetComponent<Image>();
            if (frame != null)
            {
                frame.sprite = null;
                frame.color = new Color(1f, 1f, 1f, 0f);
                frame.raycastTarget = false;
            }

            Transform burst = ready.Find("BurstImage");
            if (burst == null)
            {
                GameObject burstObject = new GameObject("BurstImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                burstObject.transform.SetParent(ready, false);
                burst = burstObject.transform;
                burst.SetAsFirstSibling();
            }

            RectTransform burstRect = burst as RectTransform;
            SetRect(burstRect, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.95f));
            Image burstImage = burst.GetComponent<Image>();
            if (burstImage != null)
            {
                burstImage.sprite = CartoonArtLibrary.Sunburst;
                burstImage.color = Color.white;
                burstImage.preserveAspect = true;
                burstImage.raycastTarget = false;
            }

            TextMeshProUGUI count = ready.Find("CountdownText")?.GetComponent<TextMeshProUGUI>();
            Fit(count, "3", 84f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);

            SetPrivateField(hud, "countdownBurstTransform", burstRect);
            SetPrivateField(hud, "readyCountdownText", count);
        }

        private static GameObject EnsureScrim(Transform parent)
        {
            if (parent == null) return null;
            Transform existing = parent.Find("ModalScrim");
            GameObject scrim = existing != null ? existing.gameObject : new GameObject("ModalScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (existing == null) scrim.transform.SetParent(parent, false);
            RectTransform rect = scrim.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = scrim.GetComponent<Image>();
            image.sprite = null;
            image.color = new Color(0.28f, 0.12f, 0.04f, 0.46f);
            image.raycastTarget = true;
            scrim.SetActive(false);
            return scrim;
        }

        private static GameObject EnsurePause(Transform parent)
        {
            if (parent == null) return null;
            Transform existing = parent.Find("PauseOverlay");
            if (existing != null) return existing.gameObject;

            GameObject overlay = new GameObject("PauseOverlay", typeof(RectTransform));
            overlay.transform.SetParent(parent, false);
            RectTransform overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            GameObject card = new GameObject("PauseCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(overlay.transform, false);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            bool landscape = Screen.width > Screen.height * 1.12f;
            cardRect.anchorMin = landscape ? new Vector2(0.25f, 0.08f) : new Vector2(0.10f, 0.18f);
            cardRect.anchorMax = landscape ? new Vector2(0.75f, 0.92f) : new Vector2(0.90f, 0.82f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
            Image cardImage = card.GetComponent<Image>();
            cardImage.sprite = CartoonArtLibrary.Parchment != null ? CartoonArtLibrary.Parchment : CartoonArtLibrary.Plaque;
            cardImage.color = Color.white;
            cardImage.raycastTarget = true;

            TextMeshProUGUI title = CreateLabel(card, "Title", "Paused", 50f, CartoonArtLibrary.Ink);
            SetRect(title.rectTransform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f));
            Fit(title, "Paused", 50f, CartoonArtLibrary.Ink, TextAlignmentOptions.Center, false);

            Button resume = CreateButton(card, "ResumeButton", CartoonArtLibrary.ButtonGold, "RESUME", new Vector2(0.12f, 0.54f), new Vector2(0.88f, 0.72f));
            Button shakes = CreateButton(card, "ShakeButton", CartoonArtLibrary.ButtonWood, "Shakes on", new Vector2(0.12f, 0.32f), new Vector2(0.88f, 0.50f));
            Button menu = CreateButton(card, "PauseMenuButton", CartoonArtLibrary.ButtonGreen, "MAIN MENU", new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.28f));
            shakes.name = "ShakeButton";
            resume.name = "ResumeButton";
            menu.name = "PauseMenuButton";

            overlay.SetActive(false);
            return overlay;
        }

        private static void WirePause(CyberHUD hud, GameObject pause, GameObject scrim)
        {
            if (hud == null || pause == null) return;
            SetPrivateField(hud, "pauseOverlay", pause);
            SetPrivateField(hud, "modalScrim", scrim);
            SetPrivateField(hud, "resumeButton", pause.GetComponentInChildren<Button>(true) != null ? FindButton(pause.transform, "ResumeButton") : null);
            SetPrivateField(hud, "pauseMenuButton", FindButton(pause.transform, "PauseMenuButton"));
            SetPrivateField(hud, "shakeToggleButton", FindButton(pause.transform, "ShakeButton"));
            SetPrivateField(hud, "shakeToggleLabel", FindTextRecursive(pause.transform, "Txt"));
            Transform shake = pause.transform.Find("PauseCard/ShakeButton/Txt");
            if (shake != null) SetPrivateField(hud, "shakeToggleLabel", shake.GetComponent<TextMeshProUGUI>());
        }

        private static void StyleButtons(Canvas canvas, RectTransform menu)
        {
            Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
            Transform start = menu != null ? menu.Find("StartButton") : null;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool breathe = start != null && buttons[i].transform == start;
                ButtonPop.Attach(buttons[i], breathe);
            }
        }

        private static void ApplyPreview(Transform preview)
        {
            if (preview == null) return;
            Sprite[] sprites = { CartoonArtLibrary.StandardMole, CartoonArtLibrary.GoldMole, CartoonArtLibrary.Bomb };
            for (int i = 0; i < sprites.Length; i++)
            {
                Transform card = preview.Find(i == 0 ? "LeftPreviewMound" : i == 1 ? "CenterPreviewMound" : "RightPreviewMound");
                if (card == null) continue;
                Transform entity = card.Find("MaskContainer/Entity");
                Image entityImage = entity != null ? entity.GetComponent<Image>() : null;
                if (entityImage != null)
                {
                    entityImage.sprite = sprites[i];
                    entityImage.color = Color.white;
                    entityImage.preserveAspect = true;
                }
                RectTransform entityRect = entity as RectTransform;
                if (entityRect != null)
                {
                    entityRect.anchorMin = new Vector2(0.5f, 0f);
                    entityRect.anchorMax = new Vector2(0.5f, 0f);
                    entityRect.pivot = new Vector2(0.5f, 0.5f);
                    entityRect.anchoredPosition = new Vector2(0f, entityRect.sizeDelta.y * 0.48f);
                }
                if (entity != null && entity.GetComponent<PreviewMascotMotion>() == null)
                {
                    entity.gameObject.AddComponent<PreviewMascotMotion>();
                }

                Image hole = EnsurePreviewImage(card, "HoleBackground");
                if (hole != null)
                {
                    hole.sprite = CartoonArtLibrary.Hatch;
                    hole.color = Color.white;
                    hole.preserveAspect = true;
                    hole.raycastTarget = false;
                    hole.transform.SetAsFirstSibling();
                }

                Image ring = card.Find("DirtMound")?.GetComponent<Image>();
                if (ring != null)
                {
                    ring.sprite = CartoonArtLibrary.DirtMound != null ? CartoonArtLibrary.DirtMound : CartoonArtLibrary.Hatch;
                    ring.color = new Color32(220, 166, 119, 255);
                    ring.preserveAspect = true;
                    ring.raycastTarget = false;
                    SetRect(ring.rectTransform, new Vector2(0.07f, 0.08f), new Vector2(0.93f, 0.36f));
                }
            }
        }

        private static Image EnsurePreviewImage(Transform parent, string name)
        {
            if (parent == null) return null;

            Transform existing = parent.Find(name);
            GameObject obj;
            if (existing != null)
            {
                obj = existing.gameObject;
            }
            else
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                obj.transform.SetParent(parent, false);
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, Vector2.one);
            return obj.GetComponent<Image>();
        }

        private static Image EnsureProgressBar(Transform parent, string trackName, string fillName)
        {
            Transform track = parent.Find(trackName);
            if (track == null)
            {
                GameObject trackObject = new GameObject(trackName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                trackObject.transform.SetParent(parent, false);
                track = trackObject.transform;
            }
            SetRect(track as RectTransform, new Vector2(0.34f, 0.08f), new Vector2(0.94f, 0.18f));
            Image trackImage = track.GetComponent<Image>();
            trackImage.color = new Color(0.45f, 0.24f, 0.08f, 0.35f);
            trackImage.raycastTarget = false;

            Transform fill = track.Find(fillName);
            if (fill == null)
            {
                GameObject fillObject = new GameObject(fillName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObject.transform.SetParent(track, false);
                fill = fillObject.transform;
            }
            SetRect(fill as RectTransform, Vector2.zero, Vector2.one);
            Image fillImage = fill.GetComponent<Image>();
            if (CartoonArtLibrary.ComboFill != null) fillImage.sprite = CartoonArtLibrary.ComboFill;
            fillImage.color = Color.white;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private static Image EnsureComboProgress(Transform parent, out Image fillImage)
        {
            fillImage = null;
            if (parent == null) return null;

            Transform track = parent.Find("ComboProgressTrack");
            if (track == null)
            {
                GameObject trackObject = new GameObject("ComboProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                trackObject.transform.SetParent(parent, false);
                track = trackObject.transform;
            }

            RectTransform trackRect = track as RectTransform;
            SetRect(trackRect, new Vector2(0.30f, 0.14f), new Vector2(0.73f, 0.30f));
            Image trackImage = track.GetComponent<Image>();
            trackImage.sprite = CartoonArtLibrary.ComboFill;
            trackImage.color = new Color32(242, 219, 166, 80);
            trackImage.type = Image.Type.Simple;
            trackImage.raycastTarget = false;

            Transform fill = track.Find("ComboProgressFill");
            if (fill == null)
            {
                GameObject fillObject = new GameObject("ComboProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                fillObject.transform.SetParent(track, false);
                fill = fillObject.transform;
            }

            SetRect(fill as RectTransform, Vector2.zero, Vector2.one);
            fillImage = fill.GetComponent<Image>();
            fillImage.sprite = CartoonArtLibrary.ComboFill;
            fillImage.color = Color.white;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
            return trackImage;
        }

        private static void ThemePanel(Transform target, Sprite sprite)
        {
            if (target == null || sprite == null) return;
            Image image = target.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static void PaintButton(Transform parent, string text, float size)
        {
            if (parent == null) return;
            TextMeshProUGUI label = parent.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return;
            RectTransform rect = label.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(16f, 8f);
            rect.offsetMax = new Vector2(-16f, -8f);
            Fit(label, text, size, CartoonArtLibrary.Cream, TextAlignmentOptions.Center, false);
            label.outlineColor = CartoonArtLibrary.Outline;
            label.outlineWidth = 0.08f;
        }

        private static void PaintInk(Transform parent)
        {
            if (parent == null) return;
            TextMeshProUGUI[] labels = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (IsInsideButton(labels[i].transform) && labels[i].transform.parent != parent) continue;
                labels[i].color = CartoonArtLibrary.Ink;
                labels[i].outlineWidth = 0f;
            }
        }

        private static bool IsInsideButton(Transform target)
        {
            while (target != null)
            {
                if (target.GetComponent<Button>() != null) return true;
                target = target.parent;
            }
            return false;
        }

        private static void AssignIcon(Transform box, string childName, Sprite sprite)
        {
            if (box == null || sprite == null) return;
            Transform icon = box.Find(childName);
            if (icon == null) return;
            Image image = icon.GetComponent<Image>();
            if (image == null) return;
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
        }

        private static void Outline(TextMeshProUGUI label, Color color, float width)
        {
            if (label == null) return;
            label.outlineColor = color;
            label.outlineWidth = width;
        }

        private static void HideIfPresent(Transform target)
        {
            if (target != null) target.gameObject.SetActive(false);
        }

        private static TextMeshProUGUI EnsureText(Transform parent, string name, string text, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            TextMeshProUGUI result = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            TextMeshProUGUI template = parent.GetComponentInChildren<TextMeshProUGUI>(true);
            if (result == null)
            {
                GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                obj.transform.SetParent(parent, false);
                result = obj.GetComponent<TextMeshProUGUI>();
                if (template != null && template != result)
                {
                    result.font = template.font;
                    result.fontSharedMaterial = template.fontSharedMaterial;
                }
            }
            result.text = text;
            result.fontSize = size;
            result.fontStyle = style;
            result.color = color;
            result.alignment = alignment;
            result.raycastTarget = false;
            result.gameObject.SetActive(true);
            return result;
        }

        private static TextMeshProUGUI CreateLabel(GameObject parent, string name, string text, float size, Color color)
        {
            return EnsureText(parent.transform, name, text, size, FontStyles.Bold, color, TextAlignmentOptions.Center);
        }

        private static Button CreateButton(GameObject parent, string name, Sprite sprite, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = obj.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            TextMeshProUGUI label = CreateLabel(obj, "Txt", text, 34f, CartoonArtLibrary.Cream);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(16f, 8f);
            label.rectTransform.offsetMax = new Vector2(-16f, -8f);
            Fit(label, text, 34f, CartoonArtLibrary.Cream, TextAlignmentOptions.Center, false);
            label.outlineColor = CartoonArtLibrary.Outline;
            label.outlineWidth = 0.08f;
            return obj.GetComponent<Button>();
        }

        private static void Fit(TextMeshProUGUI label, string text, float maxSize, Color color, TextAlignmentOptions alignment, bool wrap)
        {
            if (label == null) return;
            if (text != null) label.text = text;
            label.color = color;
            label.fontStyle = FontStyles.Bold;
            label.alignment = alignment;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMin = Mathf.Clamp(maxSize * 0.68f, 16f, maxSize);
            label.fontSizeMax = maxSize;
            label.fontSize = maxSize;
            label.margin = new Vector4(6f, 2f, 6f, 2f);
            label.outlineWidth = 0f;
            label.raycastTarget = false;
            label.gameObject.SetActive(true);
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            Transform direct = root.Find(name);
            if (direct != null) return direct;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform nested = FindNamed(root.GetChild(i), name);
                if (nested != null) return nested;
            }
            return null;
        }

        private static Button FindButton(Transform root, string name)
        {
            Transform found = FindNamed(root, name);
            return found != null ? found.GetComponent<Button>() : null;
        }

        private static TextMeshProUGUI FindTextRecursive(Transform root, string name)
        {
            Transform found = FindNamed(root, name);
            return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root as RectTransform;
            Transform child = root.Find(name);
            if (child != null) return child as RectTransform;
            for (int i = 0; i < root.childCount; i++)
            {
                RectTransform nested = FindRect(root.GetChild(i), name);
                if (nested != null) return nested;
            }
            return null;
        }

        private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            if (target == null) return;
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null) field.SetValue(target, value);
        }
    }
}
