using System.Collections.Generic;
using DiceGame.Audio;
using DiceGame.Config;
using DiceGame.Effects;
using DiceGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DiceGame.Core
{
    public sealed class DiceGameBootstrapper : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private DiceGameConfig config;

        [Header("Procedural Styling")]
        [SerializeField] private TMP_FontAsset customFont;

        private Sprite _roundedSprite;
        private Sprite _circleSprite;
        private Sprite _octagonSprite;
        private Sprite _panelSprite;
        private Sprite _buttonSprite;
        private Sprite _bgTextureSprite;
        private Sprite _rollPadSprite;
        private Sprite _sliderRingSprite;
        private Sprite _tickerDotSprite;
        private readonly List<Object> _createdProceduralAssets = new List<Object>(8);

        private static readonly Color Carbon = new Color(0.035f, 0.045f, 0.055f);
        private static readonly Color CarbonPanel = new Color(0.07f, 0.10f, 0.13f);
        private static readonly Color CyanGlow = new Color(0.0f, 0.92f, 1.0f);
        private static readonly Color GreenNeon = new Color(0.02f, 1.0f, 0.45f);
        private static readonly Color GreenHot = new Color(0.20f, 1.0f, 0.55f);
        private static readonly Color RedNeon = new Color(1.0f, 0.15f, 0.22f);
        private static readonly Color PinkFrame = new Color(1.0f, 0.18f, 0.55f);
        private static readonly Color OrangeFrame = new Color(1.0f, 0.55f, 0.10f);
        private static readonly Color SteelText = new Color(0.75f, 0.85f, 0.92f);

        private void Awake()
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DiceGameConfig>();
                _createdProceduralAssets.Add(config);
            }

            GenerateProceduralSprites();
            FindOrLoadFont();
            EnsureEventSystem();
            SetupGameEnvironment();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _createdProceduralAssets.Count; i++)
            {
                if (_createdProceduralAssets[i] != null)
                {
                    Destroy(_createdProceduralAssets[i]);
                }
            }
            _createdProceduralAssets.Clear();
        }

        private void GenerateProceduralSprites()
        {
            int size = 64;
            int radius = 16;
            Texture2D texBox = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color trans = new Color(0, 0, 0, 0);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = (x < radius) ? radius : ((x >= size - radius) ? size - radius - 1 : x);
                    int cy = (y < radius) ? radius : ((y >= size - radius) ? size - radius - 1 : y);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist <= radius - 0.5f)
                        texBox.SetPixel(x, y, white);
                    else if (dist <= radius + 0.5f)
                        texBox.SetPixel(x, y, new Color(1, 1, 1, radius + 0.5f - dist));
                    else
                        texBox.SetPixel(x, y, trans);
                }
            }
            texBox.Apply();
            _roundedSprite = Sprite.Create(texBox, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            _createdProceduralAssets.Add(texBox);
            _createdProceduralAssets.Add(_roundedSprite);

            Texture2D texCircle = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cRadius = size * 0.5f;
            Vector2 center = new Vector2(cRadius, cRadius);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    if (dist <= cRadius - 0.75f)
                        texCircle.SetPixel(x, y, white);
                    else if (dist <= cRadius + 0.75f)
                        texCircle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(cRadius + 0.75f - dist)));
                    else
                        texCircle.SetPixel(x, y, trans);
                }
            }
            texCircle.Apply();
            _circleSprite = Sprite.Create(texCircle, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100.0f);
            _createdProceduralAssets.Add(texCircle);
            _createdProceduralAssets.Add(_circleSprite);

            int octSize = 256;
            Texture2D octTex = new Texture2D(octSize, octSize, TextureFormat.RGBA32, false);
            float cut = octSize * 0.12f;
            for (int y = 0; y < octSize; y++)
            {
                for (int x = 0; x < octSize; x++)
                {
                    float u = x / (float)octSize;
                    float v = y / (float)octSize;
                    bool inside = true;
                    if (x < cut && y < cut && (cut - x) + (cut - y) > cut) inside = false;
                    if (x >= octSize - cut && y < cut && (x - (octSize - cut)) + (cut - y) > cut) inside = false;
                    if (x < cut && y >= octSize - cut && (cut - x) + (y - (octSize - cut)) > cut) inside = false;
                    if (x >= octSize - cut && y >= octSize - cut && (x - (octSize - cut)) + (y - (octSize - cut)) > cut) inside = false;
                    if (!inside) { octTex.SetPixel(x, y, trans); continue; }
                    float edgeDist = Mathf.Min(Mathf.Min(x, octSize - 1 - x), Mathf.Min(y, octSize - 1 - y));
                    Color c;
                    if (edgeDist < 6) c = new Color(0.12f, 0.16f, 0.20f, 1);
                    else if (edgeDist < 10) c = new Color(0.0f, 0.85f, 0.95f, 0.9f);
                    else if (edgeDist < 18) c = new Color(0.06f, 0.10f, 0.13f, 0.98f);
                    else c = Color.white;
                    octTex.SetPixel(x, y, c);
                }
            }
            octTex.Apply();
            _octagonSprite = Sprite.Create(octTex, new Rect(0, 0, octSize, octSize), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4(36, 36, 36, 36));
            _createdProceduralAssets.Add(octTex);
            _createdProceduralAssets.Add(_octagonSprite);

            int bgW = 512, bgH = 512;
            Texture2D bgTex = new Texture2D(bgW, bgH, TextureFormat.RGBA32, false);
            for (int y = 0; y < bgH; y++)
            {
                for (int x = 0; x < bgW; x++)
                {
                    float u = x / (float)bgW;
                    float v = y / (float)bgH;
                    Color baseCol = Color.Lerp(new Color(0.045f, 0.055f, 0.065f), new Color(0.02f, 0.03f, 0.04f), v);
                    bool weave = ((x + y) / 8) % 2 == 0;
                    float weaveLight = weave ? 0.012f : 0.0f;
                    baseCol += new Color(weaveLight, weaveLight, weaveLight);
                    float d1 = Vector2.Distance(new Vector2(u, v), new Vector2(0.15f, 0.85f));
                    float d2 = Vector2.Distance(new Vector2(u, v), new Vector2(0.85f, 0.15f));
                    baseCol += CyanGlow * Mathf.Pow(Mathf.Clamp01(1 - d1 / 0.6f), 3) * 0.06f;
                    baseCol += PinkFrame * Mathf.Pow(Mathf.Clamp01(1 - d2 / 0.6f), 3) * 0.05f;
                    bgTex.SetPixel(x, y, baseCol);
                }
            }
            bgTex.Apply();
            _bgTextureSprite = Sprite.Create(bgTex, new Rect(0, 0, bgW, bgH), new Vector2(0.5f, 0.5f), 100.0f);
            _createdProceduralAssets.Add(bgTex);
            _createdProceduralAssets.Add(_bgTextureSprite);

            int padW = 512, padH = 256;
            Texture2D padTex = new Texture2D(padW, padH, TextureFormat.RGBA32, false);
            for (int y = 0; y < padH; y++)
            {
                for (int x = 0; x < padW; x++)
                {
                    float nx = (x - padW * 0.5f) / (padW * 0.48f);
                    float ny = (y - padH * 0.5f) / (padH * 0.48f);
                    float d = nx * nx + ny * ny;
                    if (d > 1.0f) { padTex.SetPixel(x, y, trans); continue; }
                    Color c;
                    if (d > 0.92f) c = new Color(0.10f, 0.13f, 0.16f);
                    else if (d > 0.78f) c = Color.Lerp(new Color(0.18f, 0.22f, 0.26f), GreenNeon, 0.25f);
                    else c = new Color(0.08f, 0.12f, 0.10f);
                    if (d > 0.6f && d < 0.78f && (x % 32 < 6) && Mathf.Abs(nx) > 0.3f)
                        c = GreenNeon;
                    padTex.SetPixel(x, y, c);
                }
            }
            padTex.Apply();
            _rollPadSprite = Sprite.Create(padTex, new Rect(0, 0, padW, padH), new Vector2(0.5f, 0.5f), 100.0f);
            _createdProceduralAssets.Add(padTex);
            _createdProceduralAssets.Add(_rollPadSprite);

            int ringSize = 256;
            Texture2D ringTex = new Texture2D(ringSize, ringSize, TextureFormat.RGBA32, false);
            float rc = ringSize * 0.5f;
            for (int y = 0; y < ringSize; y++)
            {
                for (int x = 0; x < ringSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(rc, rc));
                    float r = dist / rc;
                    if (r > 1.0f) { ringTex.SetPixel(x, y, trans); continue; }
                    Color c;
                    if (r > 0.95f) c = new Color(0.15f, 0.20f, 0.24f);
                    else if (r > 0.88f) c = CyanGlow;
                    else if (r > 0.72f) c = new Color(0.10f, 0.14f, 0.16f, 0.95f);
                    else if (r > 0.68f) c = GreenNeon;
                    else if (r > 0.45f) c = new Color(0.07f, 0.10f, 0.12f, 0.98f);
                    else if (r > 0.40f) c = new Color(0.0f, 0.9f, 0.6f);
                    else c = Color.white;
                    ringTex.SetPixel(x, y, c);
                }
            }
            ringTex.Apply();
            _sliderRingSprite = Sprite.Create(ringTex, new Rect(0, 0, ringSize, ringSize), new Vector2(0.5f, 0.5f), 100.0f);
            _createdProceduralAssets.Add(ringTex);
            _createdProceduralAssets.Add(_sliderRingSprite);

            int tickW = 128, tickH = 64;
            Texture2D tickTex = new Texture2D(tickW, tickH, TextureFormat.RGBA32, false);
            for (int y = 0; y < tickH; y++)
            {
                for (int x = 0; x < tickW; x++)
                {
                    bool inside = x >= 6 && x < tickW - 6 && y >= 4 && y < tickH - 4;
                    if (!inside) { tickTex.SetPixel(x, y, trans); continue; }
                    tickTex.SetPixel(x, y, Color.white);
                }
            }
            tickTex.Apply();
            _tickerDotSprite = Sprite.Create(tickTex, new Rect(0, 0, tickW, tickH), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4(16, 16, 16, 16));
            _createdProceduralAssets.Add(tickTex);
            _createdProceduralAssets.Add(_tickerDotSprite);
        }

        private void FindOrLoadFont()
        {
            if (customFont == null)
            {
                customFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }
            if (customFont == null)
            {
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (allFonts != null && allFonts.Length > 0)
                {
                    customFont = allFonts[0];
                }
            }
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
                esObj.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void SetupGameEnvironment()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Carbon;
            }

            GameObject canvasObj = new GameObject("DiceCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.0f;

            canvasObj.AddComponent<GraphicRaycaster>();
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();

            GameObject shakeRootObj = new GameObject("ShakeRoot", typeof(RectTransform));
            shakeRootObj.transform.SetParent(canvasRect, false);
            RectTransform shakeRect = shakeRootObj.GetComponent<RectTransform>();
            shakeRect.anchorMin = Vector2.zero;
            shakeRect.anchorMax = Vector2.one;
            shakeRect.offsetMin = Vector2.zero;
            shakeRect.offsetMax = Vector2.zero;

            ScreenShakeController screenShake = shakeRootObj.AddComponent<ScreenShakeController>();

            SetupBackground(canvasRect);
            shakeRootObj.transform.SetAsLastSibling();
            DiceStatsHeaderView statsHeader = SetupHeader(shakeRect);
            DiceRoundTimerView timerView = SetupTimer(shakeRect);
            VisualDiceRoller diceRoller = SetupDiceArena(shakeRect);
            DiceHistoryView historyView = SetupHistoryBar(shakeRect);
            DiceSliderView sliderView = SetupSlider(shakeRect);
            DiceBetControlsView betControls = SetupBetControls(shakeRect);
            DiceGameOverModalView gameOverModal = SetupGameOverModal(shakeRect);

            FloatingTextController floatingText = SetupFloatingText(shakeRect);
            ParticleFxController particleFx = SetupParticleSystems(shakeRect);
            DiceAudioSynthesizer audioSynth = SetupAudioSynthesizer();

            GameObject mgrObj = new GameObject("DiceGameManager");
            DiceGameManager mgr = mgrObj.AddComponent<DiceGameManager>();

            SetPrivateField(mgr, "config", config);
            SetPrivateField(mgr, "audioSynth", audioSynth);
            SetPrivateField(mgr, "screenShake", screenShake);
            SetPrivateField(mgr, "floatingText", floatingText);
            SetPrivateField(mgr, "particleFx", particleFx);
            SetPrivateField(mgr, "diceRoller", diceRoller);
            SetPrivateField(mgr, "statsHeader", statsHeader);
            SetPrivateField(mgr, "timerView", timerView);
            SetPrivateField(mgr, "sliderView", sliderView);
            SetPrivateField(mgr, "betControls", betControls);
            SetPrivateField(mgr, "gameOverModal", gameOverModal);
            SetPrivateField(mgr, "historyView", historyView);

            DiceResponsiveLayout responsiveLayout = canvasObj.AddComponent<DiceResponsiveLayout>();
            SetPrivateField(responsiveLayout, "canvasScaler", scaler);
            SetPrivateField(responsiveLayout, "safeAreaRoot", shakeRect);
            SetPrivateField(responsiveLayout, "headerContainer", statsHeader.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "timerContainer", timerView.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "arenaContainer", diceRoller.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "historyContainer", historyView.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "sliderContainer", sliderView.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "betControlsContainer", betControls.GetComponent<RectTransform>());
            SetPrivateField(responsiveLayout, "_modalContainer", gameOverModal.GetComponentInChildren<Image>().rectTransform);

            responsiveLayout.AutoDiscoverReferences();
            responsiveLayout.ApplyLayout(force: true);
        }

        private void SetupBackground(RectTransform parent)
        {
            GameObject bgObj = CreateUIElement("Background", parent);
            RectTransform rect = bgObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = bgObj.AddComponent<Image>();
            img.sprite = _bgTextureSprite;
            img.color = Color.white;
            img.raycastTarget = false;
        }

        private DiceStatsHeaderView SetupHeader(RectTransform parent)
        {
            GameObject headerObj = CreateUIElement("StatsHeader", parent);
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0.5f, 1.0f);
            headerRect.anchorMax = new Vector2(0.5f, 1.0f);
            headerRect.pivot = new Vector2(0.5f, 1.0f);
            headerRect.anchoredPosition = new Vector2(0.0f, -12.0f);
            headerRect.sizeDelta = new Vector2(1020.0f, 110.0f);

            Image bg = headerObj.AddComponent<Image>();
            bg.sprite = _octagonSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.06f, 0.09f, 0.12f, 0.98f);

            CreateCornerStrip(headerRect, new Vector2(0, 1), PinkFrame);
            CreateCornerStrip(headerRect, new Vector2(1, 1), CyanGlow);
            CreateCornerStrip(headerRect, new Vector2(0, 0), CyanGlow);
            CreateCornerStrip(headerRect, new Vector2(1, 0), OrangeFrame);

            GameObject creditsLabelObj = CreateUIElement("CreditsLabel", headerRect);
            RectTransform clRect = creditsLabelObj.GetComponent<RectTransform>();
            clRect.anchorMin = new Vector2(0, 1);
            clRect.anchorMax = new Vector2(0, 1);
            clRect.pivot = new Vector2(0, 1);
            clRect.anchoredPosition = new Vector2(28, -8);
            clRect.sizeDelta = new Vector2(240, 28);
            TextMeshProUGUI creditsLabel = CreateText("CreditsLabelText", clRect, "CREDITS:", 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            creditsLabel.color = SteelText;
            creditsLabel.characterSpacing = 1.2f;

            GameObject bankrollObj = CreateUIElement("BankrollContainer", headerRect);
            RectTransform bankrollRect = bankrollObj.GetComponent<RectTransform>();
            bankrollRect.anchorMin = new Vector2(0, 1);
            bankrollRect.anchorMax = new Vector2(0, 1);
            bankrollRect.pivot = new Vector2(0, 1);
            bankrollRect.anchoredPosition = new Vector2(28, -32);
            bankrollRect.sizeDelta = new Vector2(300, 60);
            TextMeshProUGUI bankrollVal = CreateText("BankrollValue", bankrollRect, "1,420", 44, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            bankrollVal.color = CyanGlow;

            GameObject modeBtnObj = CreateUIElement("ModeBadgeButton", headerRect);
            RectTransform modeBtnRect = modeBtnObj.GetComponent<RectTransform>();
            modeBtnRect.anchorMin = new Vector2(1.0f, 0.5f);
            modeBtnRect.anchorMax = new Vector2(1.0f, 0.5f);
            modeBtnRect.pivot = new Vector2(1.0f, 0.5f);
            modeBtnRect.anchoredPosition = new Vector2(-24.0f, 6.0f);
            modeBtnRect.sizeDelta = new Vector2(72, 72);

            Image modeBtnBg = modeBtnObj.AddComponent<Image>();
            modeBtnBg.sprite = _circleSprite;
            modeBtnBg.type = Image.Type.Sliced;
            modeBtnBg.color = new Color(0.12f, 0.16f, 0.20f, 0.95f);
            Button modeBtn = modeBtnObj.AddComponent<Button>();

            TextMeshProUGUI modeText = CreateText("ModeText", modeBtnRect, "⚙", 36, FontStyles.Bold, TextAlignmentOptions.Center);
            modeText.color = SteelText;

            GameObject tickerTitleObj = CreateUIElement("TickerTitle", headerRect);
            RectTransform ttRect = tickerTitleObj.GetComponent<RectTransform>();
            ttRect.anchorMin = new Vector2(0.5f, 1);
            ttRect.anchorMax = new Vector2(0.5f, 1);
            ttRect.pivot = new Vector2(0.5f, 1);
            ttRect.anchoredPosition = new Vector2(40, -8);
            ttRect.sizeDelta = new Vector2(280, 30);
            TextMeshProUGUI tickerTitle = CreateText("TickerTitleText", ttRect, "LIVE TICKER", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            tickerTitle.color = SteelText;

            GameObject tickerRow = CreateUIElement("TickerRow", headerRect);
            RectTransform tickerRect = tickerRow.GetComponent<RectTransform>();
            tickerRect.anchorMin = new Vector2(0.5f, 0.5f);
            tickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            tickerRect.pivot = new Vector2(0.5f, 0.5f);
            tickerRect.anchoredPosition = new Vector2(40, -8);
            tickerRect.sizeDelta = new Vector2(560, 52);
            HorizontalLayoutGroup tickerLayout = tickerRow.AddComponent<HorizontalLayoutGroup>();
            tickerLayout.spacing = 10;
            tickerLayout.childAlignment = TextAnchor.MiddleCenter;
            tickerLayout.childForceExpandWidth = false;
            tickerLayout.childForceExpandHeight = false;

            DiceHistoryView.HistoryPill[] tickerPills = new DiceHistoryView.HistoryPill[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject pillObj = CreateUIElement("Pill_" + i, tickerRect);
                RectTransform pillRect = pillObj.GetComponent<RectTransform>();
                pillRect.sizeDelta = new Vector2(130, 48);
                Image pillBg = pillObj.AddComponent<Image>();
                pillBg.sprite = _tickerDotSprite;
                pillBg.type = Image.Type.Sliced;
                pillBg.color = i < 2 ? GreenNeon * 0.9f : RedNeon * 0.9f;
                TextMeshProUGUI pillText = CreateText("Val", pillRect, i < 2 ? "[14.05]" : "[5.00]", 20, FontStyles.Bold, TextAlignmentOptions.Center);
                pillText.color = Color.white;
                tickerPills[i] = new DiceHistoryView.HistoryPill { root = pillObj, background = pillBg, valueText = pillText };
            }

            GameObject streakObj = CreateUIElement("StreakContainer", headerRect);
            RectTransform streakRect = streakObj.GetComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(0.5f, 0);
            streakRect.anchorMax = new Vector2(0.5f, 0);
            streakRect.pivot = new Vector2(0.5f, 0);
            streakRect.anchoredPosition = new Vector2(40, 6);
            streakRect.sizeDelta = new Vector2(300, 24);

            TextMeshProUGUI streakVal = CreateText("StreakValue", streakRect, "STREAK x0", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            streakVal.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);

            GameObject feverBarBg = CreateUIElement("FeverBarBg", streakRect);
            RectTransform feverBarBgRect = feverBarBg.GetComponent<RectTransform>();
            feverBarBgRect.anchorMin = new Vector2(0, 1);
            feverBarBgRect.anchorMax = new Vector2(1, 1);
            feverBarBgRect.pivot = new Vector2(0.5f, 1);
            feverBarBgRect.anchoredPosition = new Vector2(0, -4);
            feverBarBgRect.sizeDelta = new Vector2(0, 6);

            Image feverBgImg = feverBarBg.AddComponent<Image>();
            feverBgImg.sprite = _roundedSprite;
            feverBgImg.type = Image.Type.Sliced;
            feverBgImg.color = new Color(0.15f, 0.18f, 0.25f, 1.0f);

            GameObject feverFill = CreateUIElement("FeverFill", feverBarBgRect);
            RectTransform feverFillRect = feverFill.GetComponent<RectTransform>();
            feverFillRect.anchorMin = Vector2.zero;
            feverFillRect.anchorMax = Vector2.one;
            feverFillRect.offsetMin = Vector2.zero;
            feverFillRect.offsetMax = Vector2.zero;

            Image feverFillImg = feverFill.AddComponent<Image>();
            feverFillImg.sprite = _roundedSprite;
            feverFillImg.type = Image.Type.Filled;
            feverFillImg.fillMethod = Image.FillMethod.Horizontal;
            feverFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            feverFillImg.color = PinkFrame;

            GameObject feverBanner = CreateUIElement("FeverBanner", headerRect);
            RectTransform bannerRect = feverBanner.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.0f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.0f);
            bannerRect.pivot = new Vector2(0.5f, 1.0f);
            bannerRect.anchoredPosition = new Vector2(0.0f, -4.0f);
            bannerRect.sizeDelta = new Vector2(320, 28);

            Image bannerBg = feverBanner.AddComponent<Image>();
            bannerBg.sprite = _roundedSprite;
            bannerBg.type = Image.Type.Sliced;
            bannerBg.color = PinkFrame;

            TextMeshProUGUI bannerText = CreateText("BannerText", bannerRect, "FEVER SURGE! x1.5 WIN", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            bannerText.color = Color.white;

            DiceStatsHeaderView view = headerObj.AddComponent<DiceStatsHeaderView>();
            SetPrivateField(view, "bankrollText", bankrollVal);
            SetPrivateField(view, "bankrollContainer", bankrollRect);
            SetPrivateField(view, "streakText", streakVal);
            SetPrivateField(view, "streakContainer", streakRect);
            SetPrivateField(view, "feverMeterFill", feverFillImg);
            SetPrivateField(view, "feverActiveBanner", feverBanner);
            SetPrivateField(view, "modeBadgeText", modeText);
            SetPrivateField(view, "buttonModeSelect", modeBtn);

            headerObj.name = "StatsHeader";
            var factory = new GameObject("TickerPillFactory");
            factory.transform.SetParent(headerObj.transform, false);
            factory.transform.name = "TickerPills";
            for (int i = 0; i < tickerPills.Length; i++)
            {
                tickerPills[i].root.transform.SetParent(tickerRow.transform, false);
            }
            Destroy(factory);

            return view;
        }

        private GameObject CreateCornerStrip(RectTransform parent, Vector2 anchor, Color col)
        {
            GameObject strip = CreateUIElement("CornerStrip", parent);
            RectTransform r = strip.GetComponent<RectTransform>();
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = anchor;
            float ox = anchor.x == 0 ? 34 : -34;
            float oy = anchor.y == 0 ? 16 : -16;
            r.anchoredPosition = new Vector2(ox, oy);
            r.sizeDelta = new Vector2(54, 6);
            Image img = strip.AddComponent<Image>();
            img.sprite = _roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = col;
            img.raycastTarget = false;
            return strip;
        }

        private DiceRoundTimerView SetupTimer(RectTransform parent)
        {
            GameObject timerObj = CreateUIElement("RoundTimer", parent);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.5f, 1.0f);
            timerRect.anchorMax = new Vector2(0.5f, 1.0f);
            timerRect.pivot = new Vector2(0.5f, 1.0f);
            timerRect.anchoredPosition = new Vector2(0.0f, -128.0f);
            timerRect.sizeDelta = new Vector2(1020.0f, 10.0f);

            Image bg = timerObj.AddComponent<Image>();
            bg.sprite = _roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.12f, 0.15f, 0.9f);

            GameObject fillObj = CreateUIElement("TimerFill", timerRect);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3, 3);
            fillRect.offsetMax = new Vector2(-3, -3);

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.sprite = _roundedSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = CyanGlow;

            TextMeshProUGUI timerText = CreateText("TimerText", timerRect, "20.0s", 14, FontStyles.Bold, TextAlignmentOptions.Center);
            timerText.color = Color.white;

            GameObject urgency = CreateUIElement("UrgencyPulse", timerRect);
            RectTransform urgRect = urgency.GetComponent<RectTransform>();
            urgRect.anchorMin = Vector2.zero;
            urgRect.anchorMax = Vector2.one;
            urgRect.offsetMin = Vector2.zero;
            urgRect.offsetMax = Vector2.zero;

            Image urgImg = urgency.AddComponent<Image>();
            urgImg.sprite = _roundedSprite;
            urgImg.type = Image.Type.Sliced;
            urgImg.color = new Color(1.0f, 0.1f, 0.2f, 0.35f);

            DiceRoundTimerView view = timerObj.AddComponent<DiceRoundTimerView>();
            SetPrivateField(view, "timerFillBar", fillImg);
            SetPrivateField(view, "timerText", timerText);
            SetPrivateField(view, "timerContainer", timerRect);
            SetPrivateField(view, "urgencyOverlay", urgency);

            return view;
        }

        private VisualDiceRoller SetupDiceArena(RectTransform parent)
        {
            GameObject arenaObj = CreateUIElement("DiceArena", parent);
            RectTransform arenaRect = arenaObj.GetComponent<RectTransform>();
            arenaRect.anchorMin = new Vector2(0.5f, 0.5f);
            arenaRect.anchorMax = new Vector2(0.5f, 0.5f);
            arenaRect.pivot = new Vector2(0.5f, 0.5f);
            arenaRect.anchoredPosition = new Vector2(0.0f, 320.0f);
            arenaRect.sizeDelta = new Vector2(1020.0f, 120.0f);

            Image arenaBg = arenaObj.AddComponent<Image>();
            arenaBg.sprite = _roundedSprite;
            arenaBg.type = Image.Type.Sliced;
            arenaBg.color = new Color(0.08f, 0.11f, 0.16f, 0.0001f);
            arenaBg.raycastTarget = false;

            GameObject shadowL = CreateUIElement("ShadowLeft", arenaRect);
            RectTransform shadowLRect = shadowL.GetComponent<RectTransform>();
            shadowLRect.anchoredPosition = new Vector2(-110.0f, -45.0f);
            shadowLRect.sizeDelta = new Vector2(140.0f, 30.0f);
            Image shadowLImg = shadowL.AddComponent<Image>();
            shadowLImg.sprite = _circleSprite;
            shadowLImg.color = new Color(0.0f, 0.0f, 0.0f, 0.45f);

            GameObject shadowR = CreateUIElement("ShadowRight", arenaRect);
            RectTransform shadowRRect = shadowR.GetComponent<RectTransform>();
            shadowRRect.anchoredPosition = new Vector2(110.0f, -45.0f);
            shadowRRect.sizeDelta = new Vector2(140.0f, 30.0f);
            Image shadowRImg = shadowR.AddComponent<Image>();
            shadowRImg.sprite = _circleSprite;
            shadowRImg.color = new Color(0.0f, 0.0f, 0.0f, 0.45f);

            GameObject dieL = CreateUIElement("DieLeft", arenaRect);
            RectTransform dieLRect = dieL.GetComponent<RectTransform>();
            dieLRect.anchoredPosition = new Vector2(-110.0f, 10.0f);
            dieLRect.sizeDelta = new Vector2(130.0f, 130.0f);

            Image dieLBg = dieL.AddComponent<Image>();
            dieLBg.sprite = _roundedSprite;
            dieLBg.type = Image.Type.Sliced;
            dieLBg.color = new Color(0.14f, 0.18f, 0.26f, 1.0f);

            TextMeshProUGUI textL = CreateText("DigitL", dieLRect, "5", 72, FontStyles.Bold, TextAlignmentOptions.Center);
            textL.color = Color.white;

            GameObject dieR = CreateUIElement("DieRight", arenaRect);
            RectTransform dieRRect = dieR.GetComponent<RectTransform>();
            dieRRect.anchoredPosition = new Vector2(110.0f, 10.0f);
            dieRRect.sizeDelta = new Vector2(130.0f, 130.0f);

            Image dieRBg = dieR.AddComponent<Image>();
            dieRBg.sprite = _roundedSprite;
            dieRBg.type = Image.Type.Sliced;
            dieRBg.color = new Color(0.14f, 0.18f, 0.26f, 1.0f);

            TextMeshProUGUI textR = CreateText("DigitR", dieRRect, "0", 72, FontStyles.Bold, TextAlignmentOptions.Center);
            textR.color = Color.white;

            GameObject resultObj = CreateUIElement("ResultNumberContainer", arenaRect);
            RectTransform resultRect = resultObj.GetComponent<RectTransform>();
            resultRect.anchoredPosition = new Vector2(0.0f, -35.0f);
            resultRect.sizeDelta = new Vector2(300.0f, 36.0f);

            TextMeshProUGUI resultText = CreateText("ResultNumberText", resultRect, "15.00", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            resultText.color = GreenHot;

            VisualDiceRoller roller = arenaObj.AddComponent<VisualDiceRoller>();
            SetPrivateField(roller, "dieLeft", dieLRect);
            SetPrivateField(roller, "dieRight", dieRRect);
            SetPrivateField(roller, "dieLeftBg", dieLBg);
            SetPrivateField(roller, "dieRightBg", dieRBg);
            SetPrivateField(roller, "dieLeftText", textL);
            SetPrivateField(roller, "dieRightText", textR);
            SetPrivateField(roller, "shadowLeft", shadowLRect);
            SetPrivateField(roller, "shadowRight", shadowRRect);
            SetPrivateField(roller, "rollResultNumberText", resultText);
            SetPrivateField(roller, "rollResultContainer", resultRect);

            return roller;
        }

        private DiceHistoryView SetupHistoryBar(RectTransform parent)
        {
            GameObject barObj = CreateUIElement("HistoryBar", parent);
            RectTransform barRect = barObj.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.5f, 1.0f);
            barRect.anchorMax = new Vector2(0.5f, 1.0f);
            barRect.pivot = new Vector2(0.5f, 1.0f);
            barRect.anchoredPosition = new Vector2(0.0f, -88.0f);
            barRect.sizeDelta = new Vector2(1020.0f, 52.0f);

            HorizontalLayoutGroup layout = barObj.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12.0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            DiceHistoryView.HistoryPill[] pills = new DiceHistoryView.HistoryPill[4];

            for (int i = 0; i < 4; i++)
            {
                GameObject pillObj = CreateUIElement("Pill_" + i, barRect);
                RectTransform pillRect = pillObj.GetComponent<RectTransform>();
                pillRect.sizeDelta = new Vector2(210.0f, 52.0f);

                Image bg = pillObj.AddComponent<Image>();
                bg.sprite = _tickerDotSprite;
                bg.type = Image.Type.Sliced;
                bg.color = i < 2 ? GreenNeon * 0.9f : RedNeon * 0.9f;

                TextMeshProUGUI val = CreateText("Val", pillRect, i < 2 ? "[14.05]" : "[LOSS]", 20, FontStyles.Bold, TextAlignmentOptions.Center);
                val.color = Color.white;

                pills[i] = new DiceHistoryView.HistoryPill
                {
                    root = pillObj,
                    background = bg,
                    valueText = val
                };
            }

            DiceHistoryView view = barObj.AddComponent<DiceHistoryView>();
            SetPrivateField(view, "pills", pills);
            return view;
        }

        private DiceSliderView SetupSlider(RectTransform parent)
        {
            GameObject container = CreateUIElement("SliderContainer", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = new Vector2(0.0f, 80.0f);
            containerRect.sizeDelta = new Vector2(1020.0f, 460.0f);

            Image containerBg = container.AddComponent<Image>();
            containerBg.sprite = _octagonSprite;
            containerBg.type = Image.Type.Sliced;
            containerBg.color = new Color(0.07f, 0.10f, 0.13f, 0.98f);

            CreateCornerStrip(containerRect, new Vector2(0, 1), PinkFrame);
            CreateCornerStrip(containerRect, new Vector2(1, 1), CyanGlow);
            CreateCornerStrip(containerRect, new Vector2(0, 0), CyanGlow);
            CreateCornerStrip(containerRect, new Vector2(1, 0), OrangeFrame);

            GameObject winHeaderObj = CreateUIElement("WinZoneHeader", containerRect);
            RectTransform whRect = winHeaderObj.GetComponent<RectTransform>();
            whRect.anchorMin = new Vector2(0, 1);
            whRect.anchorMax = new Vector2(0, 1);
            whRect.pivot = new Vector2(0, 1);
            whRect.anchoredPosition = new Vector2(28, -18);
            whRect.sizeDelta = new Vector2(300, 26);
            TextMeshProUGUI winHeaderText = CreateText("WinZoneHeaderText", whRect, "0 WIN ZONE < 20.00", 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            winHeaderText.color = SteelText;

            GameObject lossHeaderObj = CreateUIElement("LossZoneHeader", containerRect);
            RectTransform lhRect = lossHeaderObj.GetComponent<RectTransform>();
            lhRect.anchorMin = new Vector2(1, 1);
            lhRect.anchorMax = new Vector2(1, 1);
            lhRect.pivot = new Vector2(1, 1);
            lhRect.anchoredPosition = new Vector2(-28, -18);
            lhRect.sizeDelta = new Vector2(200, 26);
            TextMeshProUGUI lossHeaderText = CreateText("LossZoneHeaderText", lhRect, "LOSS ZONE 100", 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            lossHeaderText.color = RedNeon;

            GameObject lossLabelObj = CreateUIElement("LossZoneLabel", containerRect);
            RectTransform llRect = lossLabelObj.GetComponent<RectTransform>();
            llRect.anchorMin = new Vector2(0.5f, 1);
            llRect.anchorMax = new Vector2(0.5f, 1);
            llRect.pivot = new Vector2(0.5f, 1);
            llRect.anchoredPosition = new Vector2(180, -70);
            llRect.sizeDelta = new Vector2(240, 28);
            TextMeshProUGUI lossLabelText = CreateText("LossZoneLabelText", llRect, "LOSS ZONE", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            lossLabelText.color = RedNeon;

            GameObject lossLabel2Obj = CreateUIElement("LossZoneLabel2", containerRect);
            RectTransform ll2Rect = lossLabel2Obj.GetComponent<RectTransform>();
            ll2Rect.anchorMin = new Vector2(0.5f, 1);
            ll2Rect.anchorMax = new Vector2(0.5f, 1);
            ll2Rect.pivot = new Vector2(0.5f, 1);
            ll2Rect.anchoredPosition = new Vector2(180, -280);
            ll2Rect.sizeDelta = new Vector2(240, 28);
            TextMeshProUGUI lossLabel2Text = CreateText("LossZoneLabel2Text", ll2Rect, "LOSS ZONE", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            lossLabel2Text.color = RedNeon;

            GameObject trackObj = CreateUIElement("Track", containerRect);
            RectTransform trackRect = trackObj.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0.5f);
            trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.pivot = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = new Vector2(0.0f, 20.0f);
            trackRect.sizeDelta = new Vector2(940.0f, 260.0f);

            Image trackBg = trackObj.AddComponent<Image>();
            trackBg.sprite = _roundedSprite;
            trackBg.type = Image.Type.Sliced;
            trackBg.color = new Color(0.18f, 0.08f, 0.10f, 0.90f);

            GameObject crossV = CreateUIElement("CrossV", trackRect);
            RectTransform crossVRect = crossV.GetComponent<RectTransform>();
            crossVRect.anchorMin = new Vector2(0.5f, 0);
            crossVRect.anchorMax = new Vector2(0.5f, 1);
            crossVRect.pivot = new Vector2(0.5f, 0.5f);
            crossVRect.anchoredPosition = Vector2.zero;
            crossVRect.sizeDelta = new Vector2(2, 0);
            Image crossVImg = crossV.AddComponent<Image>();
            crossVImg.sprite = _roundedSprite;
            crossVImg.type = Image.Type.Sliced;
            crossVImg.color = new Color(0, 0, 0, 0.25f);

            GameObject winZone = CreateUIElement("WinZone", trackRect);
            RectTransform winRect = winZone.GetComponent<RectTransform>();
            Image winImg = winZone.AddComponent<Image>();
            winImg.sprite = _roundedSprite;
            winImg.type = Image.Type.Sliced;
            winImg.color = GreenNeon;

            GameObject lossZone = CreateUIElement("LossZone", trackRect);
            RectTransform lossRect = lossZone.GetComponent<RectTransform>();
            Image lossImg = lossZone.AddComponent<Image>();
            lossImg.sprite = _roundedSprite;
            lossImg.type = Image.Type.Sliced;
            lossImg.color = RedNeon;

            GameObject critZone = CreateUIElement("CritZone", trackRect);
            RectTransform critRect = critZone.GetComponent<RectTransform>();
            Image critImg = critZone.AddComponent<Image>();
            critImg.sprite = _roundedSprite;
            critImg.type = Image.Type.Sliced;
            critImg.color = new Color(1.0f, 0.84f, 0.0f, 0.35f);

            GameObject pinObj = CreateUIElement("ResultPin", trackRect);
            RectTransform pinRect = pinObj.GetComponent<RectTransform>();
            pinRect.anchorMin = new Vector2(0.5f, 0.5f);
            pinRect.anchorMax = new Vector2(0.5f, 0.5f);
            pinRect.pivot = new Vector2(0.5f, 0.5f);
            pinRect.anchoredPosition = Vector2.zero;
            pinRect.sizeDelta = new Vector2(22.0f, 44.0f);

            Image pinImg = pinObj.AddComponent<Image>();
            pinImg.sprite = _roundedSprite;
            pinImg.type = Image.Type.Sliced;
            pinImg.color = Color.white;

            GameObject beamObj = CreateUIElement("SliderBeam", trackRect);
            RectTransform beamRect = beamObj.GetComponent<RectTransform>();
            beamRect.anchorMin = new Vector2(0, 0.5f);
            beamRect.anchorMax = new Vector2(1, 0.5f);
            beamRect.pivot = new Vector2(0.5f, 0.5f);
            beamRect.anchoredPosition = Vector2.zero;
            beamRect.sizeDelta = new Vector2(0, 42);
            Image beamImg = beamObj.AddComponent<Image>();
            beamImg.sprite = _roundedSprite;
            beamImg.type = Image.Type.Sliced;
            beamImg.color = new Color(0.02f, 0.03f, 0.04f, 0.98f);

            GameObject handleObj = CreateUIElement("Handle", trackRect);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(130.0f, 130.0f);

            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.sprite = _sliderRingSprite;
            handleImg.type = Image.Type.Sliced;
            handleImg.color = Color.white;

            GameObject tickRow = CreateUIElement("TickRow", containerRect);
            RectTransform tickRect = tickRow.GetComponent<RectTransform>();
            tickRect.anchorMin = new Vector2(0.5f, 0);
            tickRect.anchorMax = new Vector2(0.5f, 0);
            tickRect.pivot = new Vector2(0.5f, 0);
            tickRect.anchoredPosition = new Vector2(0, 16);
            tickRect.sizeDelta = new Vector2(880, 24);
            HorizontalLayoutGroup tickLayout = tickRow.AddComponent<HorizontalLayoutGroup>();
            tickLayout.spacing = 18;
            tickLayout.childAlignment = TextAnchor.MiddleCenter;
            tickLayout.childForceExpandWidth = false;
            for (int i = 0; i <= 10; i++)
            {
                GameObject tickObj = CreateUIElement("Tick" + i, tickRect);
                RectTransform tickR = tickObj.GetComponent<RectTransform>();
                tickR.sizeDelta = new Vector2(50, 22);
                TextMeshProUGUI tickText = CreateText("TickText", tickR, (i * 10).ToString(), 16, FontStyles.Bold, TextAlignmentOptions.Center);
                tickText.color = SteelText;
            }

            GameObject winFlashObj = CreateUIElement("WinFlash", containerRect);
            RectTransform wfRect = winFlashObj.GetComponent<RectTransform>();
            wfRect.anchorMin = new Vector2(0.5f, 0.5f);
            wfRect.anchorMax = new Vector2(0.5f, 0.5f);
            wfRect.pivot = new Vector2(0.5f, 0.5f);
            wfRect.anchoredPosition = new Vector2(-90, -100);
            wfRect.sizeDelta = new Vector2(160, 60);
            TextMeshProUGUI winFlashText = CreateText("WinFlashText", wfRect, "WIN!\n+4.95x", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            winFlashText.color = GreenHot;
            winFlashObj.SetActive(false);

            GameObject toggleBtnObj = CreateUIElement("ModeToggleBtn", containerRect);
            RectTransform toggleBtnRect = toggleBtnObj.GetComponent<RectTransform>();
            toggleBtnRect.anchorMin = new Vector2(0.05f, 1.0f);
            toggleBtnRect.anchorMax = new Vector2(0.05f, 1.0f);
            toggleBtnRect.pivot = new Vector2(0.0f, 1.0f);
            toggleBtnRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            toggleBtnRect.sizeDelta = new Vector2(180.0f, 36.0f);

            Image toggleBtnBg = toggleBtnObj.AddComponent<Image>();
            toggleBtnBg.sprite = _roundedSprite;
            toggleBtnBg.type = Image.Type.Sliced;
            toggleBtnBg.color = new Color(0.18f, 0.22f, 0.32f, 1.0f);
            Button toggleBtn = toggleBtnObj.AddComponent<Button>();

            TextMeshProUGUI toggleText = CreateText("ToggleText", toggleBtnRect, "ROLL UNDER", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            toggleText.color = CyanGlow;

            GameObject multObj = CreateUIElement("MultiplierDisplay", containerRect);
            RectTransform multRect = multObj.GetComponent<RectTransform>();
            multRect.anchorMin = new Vector2(0.38f, 1.0f);
            multRect.anchorMax = new Vector2(0.38f, 1.0f);
            multRect.pivot = new Vector2(0.5f, 1.0f);
            multRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            multRect.sizeDelta = new Vector2(160.0f, 36.0f);

            TextMeshProUGUI multText = CreateText("MultVal", multRect, "4.95x", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            multText.color = GreenHot;

            GameObject targetObj = CreateUIElement("TargetDisplay", containerRect);
            RectTransform targetRect = targetObj.GetComponent<RectTransform>();
            targetRect.anchorMin = new Vector2(0.62f, 1.0f);
            targetRect.anchorMax = new Vector2(0.62f, 1.0f);
            targetRect.pivot = new Vector2(0.5f, 1.0f);
            targetRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            targetRect.sizeDelta = new Vector2(160.0f, 36.0f);

            TextMeshProUGUI targetText = CreateText("TargetVal", targetRect, "20", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            targetText.color = Color.white;

            GameObject chanceObj = CreateUIElement("ChanceDisplay", containerRect);
            RectTransform chanceRect = chanceObj.GetComponent<RectTransform>();
            chanceRect.anchorMin = new Vector2(0.95f, 1.0f);
            chanceRect.anchorMax = new Vector2(0.95f, 1.0f);
            chanceRect.pivot = new Vector2(1.0f, 1.0f);
            chanceRect.anchoredPosition = new Vector2(0.0f, -14.0f);
            chanceRect.sizeDelta = new Vector2(160.0f, 36.0f);

            TextMeshProUGUI chanceText = CreateText("ChanceVal", chanceRect, "20.00%", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            chanceText.color = Color.white;

            GameObject presetsRow = CreateUIElement("PresetsRow", containerRect);
            RectTransform presetsRect = presetsRow.GetComponent<RectTransform>();
            presetsRect.anchorMin = new Vector2(0.05f, 0.0f);
            presetsRect.anchorMax = new Vector2(0.95f, 0.0f);
            presetsRect.pivot = new Vector2(0.5f, 0.0f);
            presetsRect.anchoredPosition = new Vector2(0.0f, 10.0f);
            presetsRect.sizeDelta = new Vector2(0.0f, 32.0f);

            var (btn25, img25) = CreatePresetButton("Preset25", presetsRect, new Vector2(0.00f, 0.0f), new Vector2(0.23f, 1.0f), "25% SAFE");
            var (btn50, img50) = CreatePresetButton("Preset50", presetsRect, new Vector2(0.25f, 0.0f), new Vector2(0.48f, 1.0f), "50% BAL");
            var (btn75, img75) = CreatePresetButton("Preset75", presetsRect, new Vector2(0.51f, 0.0f), new Vector2(0.74f, 1.0f), "75% RISK");
            var (btn90, img90) = CreatePresetButton("Preset90", presetsRect, new Vector2(0.76f, 0.0f), new Vector2(1.00f, 1.0f), "90% WILD");

            DiceSliderView view = container.AddComponent<DiceSliderView>();
            SetPrivateField(view, "trackRect", trackRect);
            SetPrivateField(view, "winZoneRect", winRect);
            SetPrivateField(view, "lossZoneRect", lossRect);
            SetPrivateField(view, "critZoneRect", critRect);
            SetPrivateField(view, "handleRect", handleRect);
            SetPrivateField(view, "resultPinRect", pinRect);
            SetPrivateField(view, "winZoneImage", winImg);
            SetPrivateField(view, "lossZoneImage", lossImg);
            SetPrivateField(view, "critZoneImage", critImg);
            SetPrivateField(view, "resultPinImage", pinImg);
            SetPrivateField(view, "targetValueText", targetText);
            SetPrivateField(view, "winChanceText", chanceText);
            SetPrivateField(view, "multiplierText", multText);
            SetPrivateField(view, "modeToggleButtonText", toggleText);

            SetPrivateField(view, "buttonPreset25", btn25);
            SetPrivateField(view, "buttonPreset50", btn50);
            SetPrivateField(view, "buttonPreset75", btn75);
            SetPrivateField(view, "buttonPreset90", btn90);
            SetPrivateField(view, "bgPreset25", img25);
            SetPrivateField(view, "bgPreset50", img50);
            SetPrivateField(view, "bgPreset75", img75);
            SetPrivateField(view, "bgPreset90", img90);

            toggleBtn.onClick.AddListener(view.ToggleModeClicked);

            return view;
        }

        private (Button btn, Image img) CreatePresetButton(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, string label)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = _roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.13f, 0.17f, 0.24f, 0.85f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(0.0f, 0.85f, 1.0f, 0.5f);
            cb.pressedColor = new Color(0.0f, 0.5f, 0.8f, 0.6f);
            btn.colors = cb;

            TextMeshProUGUI txt = CreateText("Label", rect, label, 13, FontStyles.Bold, TextAlignmentOptions.Center);
            txt.color = new Color(0.85f, 0.9f, 0.98f, 0.95f);

            return (btn, img);
        }

        private DiceBetControlsView SetupBetControls(RectTransform parent)
        {
            GameObject container = CreateUIElement("BetControls", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.0f);
            containerRect.anchorMax = new Vector2(0.5f, 0.0f);
            containerRect.pivot = new Vector2(0.5f, 0.0f);
            containerRect.anchoredPosition = new Vector2(0.0f, 300.0f);
            containerRect.sizeDelta = new Vector2(1020.0f, 380.0f);

            Image bg = container.AddComponent<Image>();
            bg.sprite = _roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.09f, 0.12f, 0.17f, 0.0001f);
            bg.raycastTarget = false;

            string[] panelTitles = { "BET AMOUNT", "MULTIPLIER", "WIN CHANCE" };
            string[] panelValues = { "50.00", "4.95x", "20.00%" };
            Color[] panelColors = { CyanGlow, GreenHot, Color.white };
            GameObject[] panelObjs = new GameObject[3];
            TextMeshProUGUI[] panelValTexts = new TextMeshProUGUI[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject panel = CreateUIElement("StatPanel_" + i, containerRect);
                RectTransform pr = panel.GetComponent<RectTransform>();
                float xPos = -340 + i * 340;
                pr.anchorMin = new Vector2(0.5f, 1);
                pr.anchorMax = new Vector2(0.5f, 1);
                pr.pivot = new Vector2(0.5f, 1);
                pr.anchoredPosition = new Vector2(xPos, -10);
                pr.sizeDelta = new Vector2(320, 110);

                Image panelBg = panel.AddComponent<Image>();
                panelBg.sprite = _roundedSprite;
                panelBg.type = Image.Type.Sliced;
                panelBg.color = new Color(0.05f, 0.12f, 0.16f, 0.98f);
                Outline outline = panel.AddComponent<Outline>();
                outline.effectColor = CyanGlow;
                outline.effectDistance = new Vector2(2, -2);

                GameObject titleObj = CreateUIElement("PanelTitle", pr);
                RectTransform titleRect = titleObj.GetComponent<RectTransform>();
                titleRect.anchorMin = new Vector2(0.5f, 1);
                titleRect.anchorMax = new Vector2(0.5f, 1);
                titleRect.pivot = new Vector2(0.5f, 1);
                titleRect.anchoredPosition = new Vector2(0, -8);
                titleRect.sizeDelta = new Vector2(280, 24);
                TextMeshProUGUI titleText = CreateText("TitleText", titleRect, panelTitles[i], 18, FontStyles.Bold, TextAlignmentOptions.Center);
                titleText.color = SteelText;

                GameObject valObj = CreateUIElement("PanelValue", pr);
                RectTransform valRect = valObj.GetComponent<RectTransform>();
                valRect.anchorMin = new Vector2(0.5f, 0);
                valRect.anchorMax = new Vector2(0.5f, 0);
                valRect.pivot = new Vector2(0.5f, 0);
                valRect.anchoredPosition = new Vector2(0, 16);
                valRect.sizeDelta = new Vector2(300, 50);
                TextMeshProUGUI valText = CreateText("ValueText", valRect, panelValues[i], 36, FontStyles.Bold, TextAlignmentOptions.Center);
                valText.color = panelColors[i];

                panelObjs[i] = panel;
                panelValTexts[i] = valText;
            }

            GameObject betBox = CreateUIElement("BetAmountBox", containerRect);
            RectTransform betBoxRect = betBox.GetComponent<RectTransform>();
            betBoxRect.anchorMin = new Vector2(0.5f, 1);
            betBoxRect.anchorMax = new Vector2(0.5f, 1);
            betBoxRect.pivot = new Vector2(0.5f, 1);
            betBoxRect.anchoredPosition = new Vector2(-340, -10);
            betBoxRect.sizeDelta = new Vector2(320, 85);
            Image betBoxBg = betBox.AddComponent<Image>();
            betBoxBg.sprite = _roundedSprite;
            betBoxBg.type = Image.Type.Sliced;
            betBoxBg.color = new Color(0.05f, 0.12f, 0.16f, 0.0f);
            betBoxBg.raycastTarget = false;

            TextMeshProUGUI betLabel = CreateText("BetLabel", betBoxRect, "", 12, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            TextMeshProUGUI betVal = panelValTexts[0];
            betVal.text = "50.00";

            GameObject quickBtnRow = CreateUIElement("QuickBtnRow", betBoxRect);
            RectTransform quickRect = quickBtnRow.GetComponent<RectTransform>();
            quickRect.anchorMin = new Vector2(0, 0);
            quickRect.anchorMax = new Vector2(1, 0);
            quickRect.pivot = new Vector2(0.5f, 0);
            quickRect.anchoredPosition = new Vector2(0, 10);
            quickRect.sizeDelta = new Vector2(300, 36);

            Button btnHalf = CreateButton("BtnHalf", quickRect, new Vector2(-110, 0), new Vector2(64, 36), "1/2", 16);
            Button btnDouble = CreateButton("BtnDouble", quickRect, new Vector2(-30, 0), new Vector2(64, 36), "2x", 16);
            Button btnMin = CreateButton("BtnMin", quickRect, new Vector2(50, 0), new Vector2(64, 36), "MIN", 14);
            Button btnMax = CreateButton("BtnMax", quickRect, new Vector2(130, 0), new Vector2(64, 36), "MAX", 14);

            GameObject rollPadObj = CreateUIElement("RollPadBase", containerRect);
            RectTransform padRect = rollPadObj.GetComponent<RectTransform>();
            padRect.anchorMin = new Vector2(0.5f, 0.5f);
            padRect.anchorMax = new Vector2(0.5f, 0.5f);
            padRect.pivot = new Vector2(0.5f, 0.5f);
            padRect.anchoredPosition = new Vector2(0, -60);
            padRect.sizeDelta = new Vector2(620, 210);
            Image padImg = rollPadObj.AddComponent<Image>();
            padImg.sprite = _rollPadSprite;
            padImg.color = Color.white;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject piston = CreateUIElement("Piston_" + side, padRect);
                RectTransform pr = piston.GetComponent<RectTransform>();
                pr.anchorMin = new Vector2(0.5f, 0.5f);
                pr.anchorMax = new Vector2(0.5f, 0.5f);
                pr.pivot = new Vector2(0.5f, 0.5f);
                pr.anchoredPosition = new Vector2(side * 330, 0);
                pr.sizeDelta = new Vector2(70, 36);
                Image pistonImg = piston.AddComponent<Image>();
                pistonImg.sprite = _roundedSprite;
                pistonImg.type = Image.Type.Sliced;
                pistonImg.color = new Color(0.12f, 0.16f, 0.20f, 1);
            }

            GameObject rollBtnObj = CreateUIElement("RollButton", padRect);
            RectTransform rollRect = rollBtnObj.GetComponent<RectTransform>();
            rollRect.anchorMin = new Vector2(0.5f, 0.5f);
            rollRect.anchorMax = new Vector2(0.5f, 0.5f);
            rollRect.pivot = new Vector2(0.5f, 0.5f);
            rollRect.anchoredPosition = Vector2.zero;
            rollRect.sizeDelta = new Vector2(360, 110);

            Image rollImg = rollBtnObj.AddComponent<Image>();
            rollImg.sprite = _circleSprite;
            rollImg.type = Image.Type.Sliced;
            rollImg.color = GreenNeon;
            Button rollBtn = rollBtnObj.AddComponent<Button>();

            ColorBlock cb = rollBtn.colors;
            cb.highlightedColor = GreenHot;
            cb.pressedColor = new Color(0, 0.7f, 0.35f, 1);
            rollBtn.colors = cb;

            TextMeshProUGUI rollText = CreateText("RollText", rollRect, "ROLL DICE", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            rollText.color = new Color(0.02f, 0.10f, 0.05f, 1);

            GameObject autoBtnObj = CreateUIElement("AutoRollButton", containerRect);
            RectTransform autoRect = autoBtnObj.GetComponent<RectTransform>();
            autoRect.anchorMin = new Vector2(0.5f, 0);
            autoRect.anchorMax = new Vector2(0.5f, 0);
            autoRect.pivot = new Vector2(0.5f, 0);
            autoRect.anchoredPosition = new Vector2(0, 18);
            autoRect.sizeDelta = new Vector2(180, 42);

            Image autoImg = autoBtnObj.AddComponent<Image>();
            autoImg.sprite = _roundedSprite;
            autoImg.type = Image.Type.Sliced;
            autoImg.color = new Color(0.08f, 0.55f, 0.65f, 0.98f);
            Button autoBtn = autoBtnObj.AddComponent<Button>();

            TextMeshProUGUI autoText = CreateText("AutoText", autoRect, "AUTO ROLL", 16, FontStyles.Bold, TextAlignmentOptions.Center);
            autoText.color = Color.white;

            GameObject modeLabelObj = CreateUIElement("ModeLabel", containerRect);
            RectTransform modeLabelRect = modeLabelObj.GetComponent<RectTransform>();
            modeLabelRect.anchorMin = new Vector2(0.5f, 0);
            modeLabelRect.anchorMax = new Vector2(0.5f, 0);
            modeLabelRect.pivot = new Vector2(0.5f, 0);
            modeLabelRect.anchoredPosition = new Vector2(0, -8);
            modeLabelRect.sizeDelta = new Vector2(340, 24);
            TextMeshProUGUI modeLabelText = CreateText("ModeLabelText", modeLabelRect, "ROLL OVER / ROLL UNDER", 15, FontStyles.Bold, TextAlignmentOptions.Center);
            modeLabelText.color = SteelText;

            DiceBetControlsView view = container.AddComponent<DiceBetControlsView>();
            SetPrivateField(view, "betAmountText", betVal);
            SetPrivateField(view, "buttonHalf", btnHalf);
            SetPrivateField(view, "buttonDouble", btnDouble);
            SetPrivateField(view, "buttonMin", btnMin);
            SetPrivateField(view, "buttonMax", btnMax);
            SetPrivateField(view, "buttonRoll", rollBtn);
            SetPrivateField(view, "rollButtonText", rollText);
            SetPrivateField(view, "buttonAutoRoll", autoBtn);
            SetPrivateField(view, "autoRollIndicator", autoImg);
            SetPrivateField(view, "autoRollText", autoText);

            return view;
        }

        private DiceGameOverModalView SetupGameOverModal(RectTransform parent)
        {
            GameObject root = CreateUIElement("GameOverModal", parent);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Image backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.88f);

            GameObject cardObj = CreateUIElement("ModalCard", rootRect);
            RectTransform cardRect = cardObj.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(650.0f, 540.0f);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.sprite = _octagonSprite;
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.07f, 0.10f, 0.13f, 0.98f);

            TextMeshProUGUI headerTitle = CreateText("HeaderTitle", cardRect, "BLITZ RUN COMPLETE!", 28, FontStyles.Bold, TextAlignmentOptions.Top);
            headerTitle.rectTransform.anchoredPosition = new Vector2(0.0f, -25.0f);
            headerTitle.color = Color.white;

            GameObject rankBadgeObj = CreateUIElement("RankBadge", cardRect);
            RectTransform rankBadgeRect = rankBadgeObj.GetComponent<RectTransform>();
            rankBadgeRect.anchoredPosition = new Vector2(0.0f, 130.0f);
            rankBadgeRect.sizeDelta = new Vector2(170.0f, 90.0f);

            Image rankBg = rankBadgeObj.AddComponent<Image>();
            rankBg.sprite = _roundedSprite;
            rankBg.type = Image.Type.Sliced;
            rankBg.color = new Color(1.0f, 0.84f, 0.0f, 0.2f);

            TextMeshProUGUI rankText = CreateText("RankText", rankBadgeRect, "SSS", 52, FontStyles.Bold, TextAlignmentOptions.Center);
            rankText.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);

            TextMeshProUGUI rankTitle = CreateText("RankTitle", cardRect, "HIGH ROLLER LEGEND", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            rankTitle.rectTransform.anchoredPosition = new Vector2(0.0f, 65.0f);
            rankTitle.color = new Color(1.0f, 0.84f, 0.0f, 1.0f);

            TextMeshProUGUI finalScore = CreateStatRow(cardRect, "Final Bankroll:", "$5,420", new Vector2(0.0f, 25.0f));
            TextMeshProUGUI netProfit = CreateStatRow(cardRect, "Net Profit:", "+$4,420", new Vector2(0.0f, -10.0f));
            TextMeshProUGUI totalRolls = CreateStatRow(cardRect, "Total Rolls:", "38", new Vector2(0.0f, -45.0f));
            TextMeshProUGUI winRate = CreateStatRow(cardRect, "Win Rate:", "68.4%", new Vector2(0.0f, -80.0f));
            TextMeshProUGUI bestStreak = CreateStatRow(cardRect, "Best Streak:", "x7", new Vector2(0.0f, -115.0f));
            TextMeshProUGUI critsHit = CreateStatRow(cardRect, "Jackpot Crits:", "3", new Vector2(0.0f, -150.0f));

            Button btnPlayAgain = CreateButton("BtnPlayAgain", cardRect, new Vector2(-130.0f, -210.0f), new Vector2(220.0f, 52.0f), "PLAY AGAIN (20s)", 18);
            Image playAgainImg = btnPlayAgain.GetComponent<Image>();
            playAgainImg.color = new Color(0.0f, 0.85f, 0.5f, 1.0f);
            btnPlayAgain.GetComponentInChildren<TextMeshProUGUI>().color = new Color(0.04f, 0.15f, 0.08f, 1.0f);

            Button btnEndless = CreateButton("BtnEndless", cardRect, new Vector2(130.0f, -210.0f), new Vector2(220.0f, 52.0f), "FREE PLAY MODE", 18);
            Image endlessImg = btnEndless.GetComponent<Image>();
            endlessImg.color = new Color(0.2f, 0.25f, 0.35f, 1.0f);
            btnEndless.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;

            DiceGameOverModalView view = root.AddComponent<DiceGameOverModalView>();
            SetPrivateField(view, "modalRoot", root);
            SetPrivateField(view, "cardRect", cardRect);
            SetPrivateField(view, "rankBadgeText", rankText);
            SetPrivateField(view, "rankTitleText", rankTitle);
            SetPrivateField(view, "rankBgImage", rankBg);
            SetPrivateField(view, "finalScoreText", finalScore);
            SetPrivateField(view, "netProfitText", netProfit);
            SetPrivateField(view, "totalRollsText", totalRolls);
            SetPrivateField(view, "winRateText", winRate);
            SetPrivateField(view, "bestStreakText", bestStreak);
            SetPrivateField(view, "critsHitText", critsHit);
            SetPrivateField(view, "buttonPlayAgain", btnPlayAgain);
            SetPrivateField(view, "buttonEndlessMode", btnEndless);

            return view;
        }

        private TextMeshProUGUI CreateStatRow(RectTransform parent, string label, string defaultValue, Vector2 pos)
        {
            GameObject row = CreateUIElement("Row_" + label.Replace(" ", "").Replace(":", ""), parent);
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.anchoredPosition = pos;
            rowRect.sizeDelta = new Vector2(460.0f, 28.0f);

            TextMeshProUGUI labelText = CreateText("Label", rowRect, label, 18, FontStyles.Normal, TextAlignmentOptions.Left);
            labelText.color = new Color(0.7f, 0.78f, 0.9f, 0.9f);

            TextMeshProUGUI valText = CreateText("Value", rowRect, defaultValue, 20, FontStyles.Bold, TextAlignmentOptions.Right);
            valText.color = Color.white;

            return valText;
        }

        private FloatingTextController SetupFloatingText(RectTransform parent)
        {
            GameObject container = CreateUIElement("FloatingTextContainer", parent);
            RectTransform containerRect = container.GetComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            GameObject itemObj = CreateUIElement("FloatingTextPrefab", containerRect);
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(400.0f, 60.0f);

            TextMeshProUGUI itemText = CreateText("Text", itemRect, "+$500", 38, FontStyles.Bold, TextAlignmentOptions.Center);
            FloatingTextItem item = itemObj.AddComponent<FloatingTextItem>();
            SetPrivateField(item, "textMesh", itemText);
            SetPrivateField(item, "rectTransform", itemRect);

            itemObj.SetActive(false);

            FloatingTextController controller = container.AddComponent<FloatingTextController>();
            SetPrivateField(controller, "prefab", item);
            SetPrivateField(controller, "container", containerRect);
            SetPrivateField(controller, "poolSize", 16);

            return controller;
        }

        private ParticleFxController SetupParticleSystems(RectTransform parent)
        {
            GameObject fxContainer = new GameObject("ParticleSystems");
            fxContainer.transform.SetParent(parent, false);

            ParticleSystem winSparkles = CreateProceduralParticleSystem(fxContainer, "WinSparkles", new Color(0.0f, 1.0f, 0.6f, 1.0f), 25);
            ParticleSystem confetti = CreateProceduralParticleSystem(fxContainer, "ConfettiBurst", new Color(1.0f, 0.85f, 0.0f, 1.0f), 60);
            ParticleSystem jackpotCrit = CreateProceduralParticleSystem(fxContainer, "JackpotCrit", new Color(1.0f, 0.9f, 0.2f, 1.0f), 40);
            ParticleSystem diceImpact = CreateProceduralParticleSystem(fxContainer, "DiceImpact", new Color(0.3f, 0.4f, 0.6f, 1.0f), 15);
            ParticleSystem feverAura = CreateProceduralParticleSystem(fxContainer, "FeverAura", new Color(1.0f, 0.2f, 0.7f, 1.0f), 30);

            ParticleFxController controller = fxContainer.AddComponent<ParticleFxController>();
            SetPrivateField(controller, "winSparkleParticles", winSparkles);
            SetPrivateField(controller, "confettiBurstParticles", confetti);
            SetPrivateField(controller, "jackpotCritParticles", jackpotCrit);
            SetPrivateField(controller, "diceImpactParticles", diceImpact);
            SetPrivateField(controller, "feverAuraParticles", feverAura);

            return controller;
        }

        private ParticleSystem CreateProceduralParticleSystem(GameObject parent, string name, Color color, int maxParticles)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 8.0f;
            main.startSize = 0.25f;
            main.startColor = color;
            main.maxParticles = maxParticles;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0.0f, (short)maxParticles) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.8f;

            ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            return ps;
        }

        private DiceAudioSynthesizer SetupAudioSynthesizer()
        {
            GameObject audioObj = new GameObject("AudioSynthesizer");
            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            DiceAudioSynthesizer synth = audioObj.AddComponent<DiceAudioSynthesizer>();
            SetPrivateField(synth, "audioSource", source);
            return synth;
        }

        private GameObject CreateUIElement(string name, RectTransform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private TextMeshProUGUI CreateText(string name, RectTransform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment)
        {
            GameObject obj = CreateUIElement(name, parent);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            if (customFont != null) tmp.font = customFont;
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return tmp;
        }

        private Button CreateButton(string name, RectTransform parent, Vector2 anchoredPos, Vector2 size, string text, float fontSize)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            img.sprite = _roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.18f, 0.22f, 0.32f, 1.0f);

            Button btn = obj.AddComponent<Button>();
            TextMeshProUGUI tmp = CreateText("Label", rect, text, fontSize, FontStyles.Bold, TextAlignmentOptions.Center);
            tmp.color = Color.white;

            return btn;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
            else
            {
                Debug.LogWarning("[DiceGame] Field not found: " + fieldName + " on " + target.GetType().Name);
            }
        }
    }
}

