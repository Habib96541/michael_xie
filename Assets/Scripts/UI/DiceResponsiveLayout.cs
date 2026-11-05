using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DiceGame.Effects;

namespace DiceGame.UI
{
    [ExecuteAlways]
    public sealed class DiceResponsiveLayout : MonoBehaviour
    {
        [Header("Canvas & Scaler")]
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform safeAreaRoot;

        [Header("Containers to Position")]
        [SerializeField] private RectTransform headerContainer;
        [SerializeField] private RectTransform timerContainer;
        [SerializeField] private RectTransform arenaContainer;
        [SerializeField] private RectTransform historyContainer;
        [SerializeField] private RectTransform sliderContainer;
        [SerializeField] private RectTransform betControlsContainer;

        // Modal Container (Discovered at runtime without scene serialization)
        private RectTransform _modalContainer;

        // Arena Internal Children (Discovered at runtime without scene serialization)
        private RectTransform _dieLeft;
        private RectTransform _dieRight;
        private RectTransform _shadowLeft;
        private RectTransform _shadowRight;
        private RectTransform _resultNumberContainer;

        // Bet Controls Internal Children (Discovered at runtime without scene serialization)
        private RectTransform _betBoxRect;
        private RectTransform _quickBtnRowRect;
        private RectTransform _rollBtnRect;
        private RectTransform _autoBtnRect;

        // Slider Internal Children
        private RectTransform _sliderModeBtn;
        private RectTransform _sliderMult;
        private RectTransform _sliderTarget;
        private RectTransform _sliderChance;
        private RectTransform _sliderTrack;
        private RectTransform _sliderPresets;

        // Fever Banner
        private RectTransform _feverBanner;

        private int _lastWidth = -1;
        private int _lastHeight = -1;
        private Rect _lastSafeArea;
        private bool _isPortrait;
        private bool _applyingLayout;

        public bool IsPortrait => _isPortrait;

        private void Awake()
        {
            if (canvasScaler == null)
            {
                canvasScaler = GetComponentInParent<CanvasScaler>();
            }

            AutoDiscoverReferences();
            ApplyLayout(force: true);
        }

        private void OnEnable()
        {
            AutoDiscoverReferences();
            ApplyLayout(force: true);
        }

        private void Start()
        {
            AutoDiscoverReferences();
            ApplyLayout(force: true);
        }

        private void OnRectTransformDimensionsChange()
        {
            AutoDiscoverReferences();
            ApplyLayout(force: true);
        }

        private void Update()
        {
            int w = Screen.width;
            int h = Screen.height;
            Rect sa = Screen.safeArea;

            if (w != _lastWidth || h != _lastHeight || sa != _lastSafeArea)
            {
                _lastWidth = w;
                _lastHeight = h;
                _lastSafeArea = sa;
                ApplyLayout(force: false);
            }
        }

        public void AutoDiscoverReferences()
        {
            if (safeAreaRoot == null)
            {
                Transform shake = transform.Find("ShakeRoot");
                safeAreaRoot = (shake != null) ? (shake as RectTransform) : (transform as RectTransform);
            }

            if (safeAreaRoot != null)
            {
                if (headerContainer == null) headerContainer = safeAreaRoot.Find("StatsHeader") as RectTransform;
                if (timerContainer == null) timerContainer = safeAreaRoot.Find("RoundTimer") as RectTransform;
                if (arenaContainer == null) arenaContainer = safeAreaRoot.Find("DiceArena") as RectTransform;
                if (historyContainer == null) historyContainer = safeAreaRoot.Find("HistoryBar") as RectTransform;
                if (sliderContainer == null) sliderContainer = safeAreaRoot.Find("SliderContainer") as RectTransform;
                if (betControlsContainer == null) betControlsContainer = safeAreaRoot.Find("BetControls") as RectTransform;
                if (_modalContainer == null)
                {
                    Transform modal = safeAreaRoot.Find("GameOverModal");
                    if (modal != null)
                    {
                        Transform card = modal.Find("ModalCard");
                        _modalContainer = (card != null) ? (card as RectTransform) : (modal as RectTransform);
                    }
                }
            }

            if (arenaContainer != null)
            {
                if (_dieLeft == null) _dieLeft = arenaContainer.Find("DieLeft") as RectTransform;
                if (_dieRight == null) _dieRight = arenaContainer.Find("DieRight") as RectTransform;
                if (_shadowLeft == null) _shadowLeft = arenaContainer.Find("ShadowLeft") as RectTransform;
                if (_shadowRight == null) _shadowRight = arenaContainer.Find("ShadowRight") as RectTransform;
                if (_resultNumberContainer == null) _resultNumberContainer = arenaContainer.Find("ResultNumberContainer") as RectTransform;
            }

            if (betControlsContainer != null)
            {
                if (_betBoxRect == null) _betBoxRect = betControlsContainer.Find("BetAmountBox") as RectTransform;
                if (_quickBtnRowRect == null) _quickBtnRowRect = betControlsContainer.Find("QuickBtnRow") as RectTransform;
                if (_rollBtnRect == null) _rollBtnRect = betControlsContainer.Find("RollButton") as RectTransform;
                if (_autoBtnRect == null) _autoBtnRect = betControlsContainer.Find("AutoRollButton") as RectTransform;
            }

            if (sliderContainer != null)
            {
                if (_sliderModeBtn == null) _sliderModeBtn = sliderContainer.Find("ModeToggleBtn") as RectTransform;
                if (_sliderMult == null) _sliderMult = sliderContainer.Find("MultiplierDisplay") as RectTransform;
                if (_sliderTarget == null) _sliderTarget = sliderContainer.Find("TargetDisplay") as RectTransform;
                if (_sliderChance == null) _sliderChance = sliderContainer.Find("ChanceDisplay") as RectTransform;
                if (_sliderTrack == null) _sliderTrack = sliderContainer.Find("Track") as RectTransform;
                if (_sliderPresets == null) _sliderPresets = sliderContainer.Find("PresetsRow") as RectTransform;
            }

            if (headerContainer != null && _feverBanner == null)
            {
                Transform fb = headerContainer.Find("FeverBanner");
                if (fb != null) _feverBanner = fb as RectTransform;
            }

            // Fix background vignette glow if present (prevent awkward stretched oval)
            Transform bg = transform.Find("Background");
            if (bg != null)
            {
                Transform glow = bg.Find("VignetteGlow");
                if (glow != null)
                {
                    Image glowImg = glow.GetComponent<Image>();
                    if (glowImg != null && glowImg.enabled)
                    {
                        glowImg.enabled = false;
                    }
                }
            }
        }

        public void ApplyLayout(bool force = false)
        {
            if (_applyingLayout) return;

            _applyingLayout = true;
            try
            {
                int screenW = Screen.width;
                int screenH = Screen.height;
                if (screenW <= 0 || screenH <= 0) return;

                _isPortrait = (screenH >= screenW);

                ApplyCanvasScaler();
                ApplySafeArea();

                Color cardBg = Color.white;
                Color cardOutline = new Color(0.18f, 0.58f, 0.70f, 0.72f);
                StyleCard(headerContainer, cardBg, cardOutline);
                StyleCard(arenaContainer, cardBg, cardOutline);
                StyleCard(sliderContainer, cardBg, cardOutline);
                StyleCard(betControlsContainer, cardBg, cardOutline);

                ApplyStackedLayout();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[DiceResponsiveLayout] Warning during layout apply: " + ex.Message);
            }
            finally
            {
                _applyingLayout = false;
            }
        }

        private static void StyleCard(RectTransform rt, Color bgColor, Color outlineColor)
        {
            if (rt == null) return;
            Image img = rt.GetComponent<Image>();
            if (img != null)
            {
                img.color = bgColor;
            }
            Outline outline = rt.GetComponent<Outline>();
            if (outline == null)
            {
                outline = rt.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void ApplyCanvasScaler()
        {
            if (canvasScaler == null) return;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            if (_isPortrait)
            {
                canvasScaler.referenceResolution = new Vector2(1080.0f, 1920.0f);
                canvasScaler.matchWidthOrHeight = 0.0f; // Strict width matching in portrait
            }
            else
            {
                canvasScaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
                float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
                canvasScaler.matchWidthOrHeight = (aspect >= 1.6f) ? 1.0f : 0.65f;
            }
        }

        private void ApplySafeArea()
        {
            if (safeAreaRoot == null) return;

            float screenW = Mathf.Max(1, Screen.width);
            float screenH = Mathf.Max(1, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (safeArea.width <= 0 || safeArea.height <= 0)
            {
                safeArea = new Rect(0, 0, screenW, screenH);
            }

            Vector2 minAnchor = new Vector2(safeArea.xMin / screenW, safeArea.yMin / screenH);
            Vector2 maxAnchor = new Vector2(safeArea.xMax / screenW, safeArea.yMax / screenH);

            safeAreaRoot.anchorMin = minAnchor;
            safeAreaRoot.anchorMax = maxAnchor;
            safeAreaRoot.offsetMin = Vector2.zero;
            safeAreaRoot.offsetMax = Vector2.zero;
        }

        private void ApplyStackedLayout()
        {
            GetSafeCanvasSize(out float safeW, out float safeH);

            float side = _isPortrait ? 16f : 24f;
            float contentW = Mathf.Clamp(safeW - side * 2f, 280f, _isPortrait ? 1040f : 1160f);

            float headerH = _isPortrait ? 124f : 104f;
            float feverH = 38f;
            float timerH = _isPortrait ? 44f : 40f;
            float arenaH = _isPortrait ? 360f : 200f;
            float historyH = _isPortrait ? 52f : 46f;
            float sliderH = _isPortrait ? 220f : 150f;
            float betH = _isPortrait ? 236f : 112f;

            const int gapCount = 7;
            const float minGap = 8f;
            float BlockSum()
            {
                return headerH + feverH + timerH + arenaH + historyH + sliderH + betH;
            }

            float needed = BlockSum() + minGap * gapCount;
            float gap;
            if (safeH < needed)
            {
                float scale = safeH / Mathf.Max(1f, needed);
                headerH *= scale;
                feverH *= scale;
                timerH *= scale;
                arenaH *= scale;
                historyH *= scale;
                sliderH *= scale;
                betH *= scale;
                gap = minGap * scale;
            }
            else
            {
                float free = safeH - BlockSum();
                float arenaMax = _isPortrait ? 560f : 300f;
                float sliderMax = _isPortrait ? 268f : 176f;
                float betMax = _isPortrait ? 268f : 136f;
                float arenaRoom = Mathf.Max(0f, arenaMax - arenaH);
                float sliderRoom = Mathf.Max(0f, sliderMax - sliderH);
                float betRoom = Mathf.Max(0f, betMax - betH);
                float room = arenaRoom + sliderRoom + betRoom;
                float grow = Mathf.Min(free, room);
                if (room > 0.01f)
                {
                    arenaH += grow * (arenaRoom / room);
                    sliderH += grow * (sliderRoom / room);
                    betH += grow * (betRoom / room);
                }

                gap = Mathf.Max(minGap, (free - grow) / gapCount);
            }

            float y = -gap;
            PlaceTop(headerContainer, contentW, headerH, y);
            FormatHeader(contentW, headerH);

            y -= headerH + feverH + gap;
            PlaceTop(timerContainer, contentW, timerH, y);
            FitTimerText(timerH);

            y -= timerH + gap;
            PlaceTop(arenaContainer, contentW, arenaH, y);
            LayoutArena(arenaH, contentW);

            y -= arenaH + gap;
            PlaceTop(historyContainer, contentW, historyH, y);
            FormatHistory(historyH);

            y -= historyH + gap;
            PlaceTop(sliderContainer, contentW, sliderH, y);
            LayoutSlider(sliderH);

            PlaceBottom(betControlsContainer, contentW, betH, gap);
            LayoutBet(betH);
            FormatModal(safeW, safeH);
        }

        private void GetSafeCanvasSize(out float safeW, out float safeH)
        {
            float screenW = Mathf.Max(1f, Screen.width);
            float screenH = Mathf.Max(1f, Screen.height);
            Vector2 reference = _isPortrait ? new Vector2(1080f, 1920f) : new Vector2(1920f, 1080f);
            float match = 0f;
            if (!_isPortrait)
            {
                float aspect = screenW / screenH;
                match = aspect >= 1.6f ? 1f : 0.65f;
            }

            float logWidth = Mathf.Log(screenW / reference.x, 2f);
            float logHeight = Mathf.Log(screenH / reference.y, 2f);
            float scale = Mathf.Max(0.0001f, Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match)));
            float canvasW = screenW / scale;
            float canvasH = screenH / scale;

            Rect safe = Screen.safeArea;
            if (safe.width <= 1f || safe.height <= 1f)
            {
                safe = new Rect(0f, 0f, screenW, screenH);
            }

            safeW = (safe.width / screenW) * canvasW;
            safeH = (safe.height / screenH) * canvasH;
        }

        private void FormatHeader(float width, float height)
        {
            if (headerContainer == null) return;

            float pad = Mathf.Clamp(width * 0.018f, 12f, 24f);
            float icon = Mathf.Clamp(height * 0.52f, 36f, 64f);
            float innerTop = height * 0.5f - 8f;
            float innerH = height - 16f;
            const float rowGap = 4f;
            float titleH = innerH * 0.36f;
            float subH = innerH * 0.22f;
            float modeH = Mathf.Max(22f, innerH - titleH - subH - rowGap * 2f);
            float titleY = innerTop - titleH * 0.5f;
            float subY = titleY - titleH * 0.5f - rowGap - subH * 0.5f;
            float modeY = subY - subH * 0.5f - rowGap - modeH * 0.5f;

            float textX = pad + icon + 10f;
            float textW = Mathf.Max(64f, width * 0.34f - textX - 6f);

            RectTransform logo = headerContainer.Find("LogoBadge") as RectTransform;
            Place(logo, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(pad, 0f), new Vector2(icon, icon));

            RectTransform brand = headerContainer.Find("BrandTitle") as RectTransform;
            Place(brand, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, titleY), new Vector2(textW, titleH));
            FitChildText(brand, "BrandName", titleH * 0.72f, 16f, false, TextAlignmentOptions.MidlineLeft);

            RectTransform subtitle = headerContainer.Find("BrandSubtitle") as RectTransform;
            Place(subtitle, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, subY), new Vector2(textW, subH));
            FitChildText(subtitle, "BrandSub", subH * 0.7f, 12f, false, TextAlignmentOptions.MidlineLeft);

            RectTransform mode = headerContainer.Find("ModeBadgeButton") as RectTransform;
            Place(mode, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(textX, modeY), new Vector2(Mathf.Min(textW, 230f), modeH));
            FitChildText(mode, "ModeText", modeH * 0.58f, 12f, false, TextAlignmentOptions.Center);

            RectTransform bankroll = headerContainer.Find("BankrollContainer") as RectTransform;
            Place(bankroll, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(width * 0.03f, 0f), new Vector2(width * 0.26f, height - 18f));
            SplitLabeledValue(bankroll, "BankrollLabel", "BankrollValue", 0.30f, 16f, 36f);

            RectTransform streak = headerContainer.Find("StreakContainer") as RectTransform;
            Place(streak, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-pad, 0f), new Vector2(width * 0.23f, height - 22f));
            if (streak != null)
            {
                RectTransform streakText = streak.Find("StreakValue") as RectTransform;
                if (streakText != null)
                {
                    streakText.anchorMin = new Vector2(0f, 0f);
                    streakText.anchorMax = new Vector2(1f, 1f);
                    streakText.offsetMin = new Vector2(0f, 18f);
                    streakText.offsetMax = new Vector2(-2f, -2f);
                    TextMeshProUGUI streakLabel = streakText.GetComponent<TextMeshProUGUI>();
                    if (streakLabel != null) streakLabel.alignment = TextAlignmentOptions.MidlineRight;
                    SetReadableFont(streakLabel, Mathf.Clamp((height - 22f) * 0.32f, 15f, 26f), 12f, false);
                }

                RectTransform fever = streak.Find("FeverBarBg") as RectTransform;
                if (fever != null)
                {
                    fever.anchorMin = new Vector2(0f, 0f);
                    fever.anchorMax = new Vector2(1f, 0f);
                    fever.pivot = new Vector2(0.5f, 0f);
                    fever.offsetMin = new Vector2(4f, 4f);
                    fever.offsetMax = new Vector2(-4f, 16f);
                }
            }

            if (_feverBanner != null)
            {
                float bannerW = Mathf.Clamp(width * 0.46f, 220f, 420f);
                float bannerH = 32f;
                _feverBanner.anchorMin = new Vector2(0.5f, 0f);
                _feverBanner.anchorMax = new Vector2(0.5f, 0f);
                _feverBanner.pivot = new Vector2(0.5f, 1f);
                _feverBanner.anchoredPosition = new Vector2(0f, -4f);
                _feverBanner.sizeDelta = new Vector2(bannerW, bannerH);
                FitChildText(_feverBanner, "BannerText", bannerH * 0.52f, 12f, false, TextAlignmentOptions.Center);
            }
        }

        private void FitTimerText(float timerH)
        {
            if (timerContainer == null) return;
            TextMeshProUGUI timer = timerContainer.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
            if (timer == null) return;

            RectTransform rt = timer.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8f, 2f);
            rt.offsetMax = new Vector2(-8f, -2f);
            timer.alignment = TextAlignmentOptions.Center;
            SetReadableFont(timer, Mathf.Clamp(timerH * 0.58f, 16f, 30f), 13f, false);
        }

        private void LayoutArena(float arenaH, float arenaW)
        {
            float widthCap = (arenaW - 64f) * 0.40f;
            float heightCap = (arenaH - 28f) * 0.58f;
            float dieCap = _isPortrait ? 260f : 190f;
            float dieSize = Mathf.Clamp(Mathf.Min(widthCap, heightCap), 88f, dieCap);
            float spacing = dieSize * 0.56f + 10f;
            float maxSpacing = arenaW * 0.5f - dieSize * 0.5f - 16f;
            if (maxSpacing < dieSize * 0.52f)
            {
                dieSize = Mathf.Max(80f, (arenaW - 48f) / 2.15f);
                spacing = dieSize * 0.54f + 8f;
                maxSpacing = arenaW * 0.5f - dieSize * 0.5f - 12f;
            }
            spacing = Mathf.Min(spacing, Mathf.Max(8f, maxSpacing));

            float resultH = Mathf.Clamp(dieSize * 0.22f, 40f, 62f);
            float clusterGap = Mathf.Clamp(dieSize * 0.10f, 14f, 26f);
            float clusterH = dieSize + clusterGap + resultH;
            float topLimit = arenaH * 0.5f - 10f;
            float bottomLimit = -arenaH * 0.5f + 10f;
            float available = topLimit - bottomLimit;
            if (clusterH > available && available > 40f)
            {
                float scale = available / clusterH;
                dieSize *= scale;
                resultH *= scale;
                clusterGap *= scale;
                spacing = Mathf.Min(dieSize * 0.56f + 8f, Mathf.Max(8f, arenaW * 0.5f - dieSize * 0.5f - 12f));
                clusterH = dieSize + clusterGap + resultH;
            }

            float dieY = (clusterGap + resultH) * 0.5f;
            float resultY = dieY - dieSize * 0.5f - clusterGap - resultH * 0.5f;
            float shadowH = Mathf.Max(10f, dieSize * 0.14f);
            float shadowY = dieY - dieSize * 0.46f;

            Vector2 center = new Vector2(0.5f, 0.5f);
            Place(_dieLeft, center, center, new Vector2(-spacing, dieY), new Vector2(dieSize, dieSize));
            Place(_dieRight, center, center, new Vector2(spacing, dieY), new Vector2(dieSize, dieSize));
            Place(_shadowLeft, center, center, new Vector2(-spacing, shadowY), new Vector2(dieSize * 0.92f, shadowH));
            Place(_shadowRight, center, center, new Vector2(spacing, shadowY), new Vector2(dieSize * 0.92f, shadowH));
            Place(_resultNumberContainer, center, center, new Vector2(0f, resultY), new Vector2(Mathf.Clamp(dieSize * 2.1f, 160f, arenaW - 40f), resultH));

            FitChildText(_dieLeft, "DigitL", dieSize * 0.42f, dieSize * 0.26f, false, TextAlignmentOptions.Center);
            FitChildText(_dieRight, "DigitR", dieSize * 0.42f, dieSize * 0.26f, false, TextAlignmentOptions.Center);
            FitChildText(_resultNumberContainer, "ResultNumberText", resultH * 0.62f, 18f, false, TextAlignmentOptions.Center);

            VisualDiceRoller roller = arenaContainer != null ? arenaContainer.GetComponent<VisualDiceRoller>() : null;
            if (roller != null) roller.UpdateOrigins();
        }

        private void FormatHistory(float height)
        {
            if (historyContainer == null) return;

            HorizontalLayoutGroup layout = historyContainer.GetComponent<HorizontalLayoutGroup>();
            const float spacing = 8f;
            if (layout != null)
            {
                layout.padding = new RectOffset(4, 4, 4, 4);
                layout.spacing = spacing;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            float width = Mathf.Max(10f, historyContainer.sizeDelta.x);
            int count = Mathf.Max(1, historyContainer.childCount);
            float itemW = (width - 8f - spacing * (count - 1)) / count;
            float itemH = Mathf.Max(26f, height - 10f);
            float font = Mathf.Clamp(itemH * 0.42f, 13f, 22f);

            for (int i = 0; i < historyContainer.childCount; i++)
            {
                RectTransform item = historyContainer.GetChild(i) as RectTransform;
                if (item == null) continue;

                LayoutElement element = item.GetComponent<LayoutElement>();
                if (element == null) element = item.gameObject.AddComponent<LayoutElement>();
                element.preferredWidth = itemW;
                element.preferredHeight = itemH;
                element.minWidth = itemW;
                element.flexibleWidth = 0f;
                element.flexibleHeight = 0f;

                TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.alignment = TextAlignmentOptions.Center;
                    SetReadableFont(label, font, 11f, false);
                }
            }
        }

        private void LayoutSlider(float sliderH)
        {
            if (sliderContainer == null) return;

            float side = 12f;
            float topInset = 10f;
            float bottomInset = 10f;
            float topRow = Mathf.Clamp(sliderH * 0.22f, 36f, 50f);
            float presetsH = Mathf.Clamp(sliderH * 0.22f, 36f, 50f);
            float trackH = Mathf.Clamp(sliderH * 0.12f, 24f, 38f);
            float needed = topInset + topRow + 16f + trackH + 16f + presetsH + bottomInset;
            if (needed > sliderH && needed > 1f)
            {
                float scale = sliderH / needed;
                topInset *= scale;
                bottomInset *= scale;
                topRow *= scale;
                presetsH *= scale;
                trackH *= scale;
            }

            PlaceColumn(_sliderModeBtn, 0, topInset, topRow, 8f, side);
            PlaceColumn(_sliderMult, 1, topInset, topRow, 8f, side);
            PlaceColumn(_sliderTarget, 2, topInset, topRow, 8f, side);
            PlaceColumn(_sliderChance, 3, topInset, topRow, 8f, side);

            FitButtonLabel(_sliderModeBtn, Mathf.Clamp(topRow * 0.46f, 13f, 22f), 11f, false);
            FitChildText(_sliderMult, "MultVal", topRow * 0.58f, 14f, false, TextAlignmentOptions.Center);
            FitChildText(_sliderTarget, "TargetVal", topRow * 0.58f, 14f, false, TextAlignmentOptions.Center);
            FitChildText(_sliderChance, "ChanceVal", topRow * 0.58f, 14f, false, TextAlignmentOptions.Center);

            if (_sliderPresets != null)
            {
                _sliderPresets.anchorMin = new Vector2(0f, 0f);
                _sliderPresets.anchorMax = new Vector2(1f, 0f);
                _sliderPresets.pivot = new Vector2(0.5f, 0f);
                _sliderPresets.offsetMin = new Vector2(side, bottomInset);
                _sliderPresets.offsetMax = new Vector2(-side, bottomInset + presetsH);
                LayoutPresetButtons(_sliderPresets, Mathf.Clamp(presetsH * 0.40f, 12f, 18f));
            }

            if (_sliderTrack != null)
            {
                float regionTop = sliderH * 0.5f - (topInset + topRow + 8f);
                float regionBottom = -sliderH * 0.5f + (bottomInset + presetsH + 8f);
                float trackY = (regionTop + regionBottom) * 0.5f;
                _sliderTrack.anchorMin = new Vector2(0f, 0.5f);
                _sliderTrack.anchorMax = new Vector2(1f, 0.5f);
                _sliderTrack.pivot = new Vector2(0.5f, 0.5f);
                _sliderTrack.offsetMin = new Vector2(side, trackY - trackH * 0.5f);
                _sliderTrack.offsetMax = new Vector2(-side, trackY + trackH * 0.5f);

                RectTransform handle = _sliderTrack.Find("Handle") as RectTransform;
                if (handle != null)
                {
                    handle.anchorMin = handle.anchorMax = new Vector2(0.5f, 0.5f);
                    handle.pivot = new Vector2(0.5f, 0.5f);
                    handle.sizeDelta = new Vector2(Mathf.Clamp(trackH * 0.85f, 20f, 34f), trackH + 16f);
                }
            }

            DiceSliderView view = sliderContainer.GetComponent<DiceSliderView>();
            if (view != null) view.RefreshLayout();
        }

        private void LayoutBet(float height)
        {
            if (betControlsContainer == null) return;

            float pad = 14f;
            float gap = 12f;
            float width = Mathf.Max(10f, betControlsContainer.sizeDelta.x);

            if (_isPortrait)
            {
                float rowH = Mathf.Max(44f, (height - pad * 2f - gap) * 0.5f);
                float inner = width - pad * 2f;
                float betW = (inner - gap) * 0.46f;
                float quickW = inner - gap - betW;
                float rollW = (inner - gap) * 0.68f;
                float autoW = inner - gap - rollW;

                Place(_betBoxRect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(pad, -pad), new Vector2(betW, rowH));
                Place(_quickBtnRowRect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-pad, -pad), new Vector2(quickW, rowH));
                Place(_rollBtnRect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(pad, pad), new Vector2(rollW, rowH));
                Place(_autoBtnRect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-pad, pad), new Vector2(autoW, rowH));

                LayoutBetAmount(rowH);
                ApplyQuickButtonsQuadrantLayout(_quickBtnRowRect);
                FitButtonsIn(_quickBtnRowRect, Mathf.Clamp(rowH * 0.22f, 13f, 22f), 11f, false);
                FitButtonLabel(_rollBtnRect, Mathf.Clamp(rowH * 0.30f, 18f, 34f), 15f, false);
                FitButtonLabel(_autoBtnRect, Mathf.Clamp(rowH * 0.18f, 12f, 20f), 11f, true);
            }
            else
            {
                float rowH = Mathf.Max(56f, height - pad * 2f);
                float inner = width - pad * 2f - gap * 3f;
                float betW = inner * 0.22f;
                float quickW = inner * 0.24f;
                float rollW = inner * 0.38f;
                float autoW = Mathf.Max(72f, inner - betW - quickW - rollW);
                float x = pad;

                Place(_betBoxRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(betW, rowH));
                x += betW + gap;
                Place(_quickBtnRowRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(quickW, rowH));
                x += quickW + gap;
                Place(_rollBtnRect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(rollW, rowH));
                Place(_autoBtnRect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-pad, 0f), new Vector2(autoW, rowH));

                LayoutBetAmount(rowH);
                ApplyQuickButtonsQuadrantLayout(_quickBtnRowRect);
                FitButtonsIn(_quickBtnRowRect, 16f, 11f, false);
                FitButtonLabel(_rollBtnRect, 26f, 15f, false);
                FitButtonLabel(_autoBtnRect, 15f, 11f, true);
            }
        }

        private void LayoutBetAmount(float boxH)
        {
            if (_betBoxRect == null) return;

            float labelH = Mathf.Clamp(boxH * 0.28f, 20f, 30f);
            RectTransform label = _betBoxRect.Find("BetLabel") as RectTransform;
            RectTransform value = _betBoxRect.Find("BetValue") as RectTransform;

            if (label != null)
            {
                label.anchorMin = new Vector2(0f, 1f);
                label.anchorMax = new Vector2(1f, 1f);
                label.pivot = new Vector2(0.5f, 1f);
                label.offsetMin = new Vector2(12f, -(8f + labelH));
                label.offsetMax = new Vector2(-10f, -8f);
                TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.MidlineLeft;
                SetReadableFont(tmp, Mathf.Clamp(labelH * 0.62f, 13f, 18f), 11f, false);
            }

            if (value != null)
            {
                float valueH = Mathf.Max(24f, boxH - labelH - 20f);
                value.anchorMin = new Vector2(0f, 0f);
                value.anchorMax = new Vector2(1f, 0f);
                value.pivot = new Vector2(0.5f, 0f);
                value.offsetMin = new Vector2(12f, 8f);
                value.offsetMax = new Vector2(-10f, 8f + valueH);
                TextMeshProUGUI tmp = value.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.MidlineLeft;
                SetReadableFont(tmp, Mathf.Clamp(valueH * 0.62f, 20f, 40f), 16f, false);
            }
        }

        private void FormatModal(float safeW, float safeH)
        {
            if (_modalContainer == null) return;

            float cardW = Mathf.Clamp(safeW - 48f, 300f, _isPortrait ? 760f : 820f);
            float cardH = Mathf.Clamp(safeH * (_isPortrait ? 0.62f : 0.78f), 360f, _isPortrait ? 680f : 600f);
            if (cardH > safeH - 24f) cardH = safeH - 24f;

            _modalContainer.anchorMin = _modalContainer.anchorMax = new Vector2(0.5f, 0.5f);
            _modalContainer.pivot = new Vector2(0.5f, 0.5f);
            _modalContainer.anchoredPosition = Vector2.zero;
            _modalContainer.sizeDelta = new Vector2(cardW, cardH);

            float headerBand = Mathf.Clamp(cardH * 0.08f, 34f, 46f);
            float badgeH = Mathf.Clamp(cardH * 0.13f, 64f, 86f);
            float titleBand = Mathf.Clamp(cardH * 0.06f, 26f, 36f);
            float buttonH = Mathf.Clamp(cardH * 0.10f, 48f, 62f);
            float pad = 22f;
            float cursor = 16f;

            RectTransform header = _modalContainer.Find("HeaderTitle") as RectTransform;
            PlaceTopBand(header, cursor, headerBand, pad);
            TextMeshProUGUI headerLabel = header != null ? header.GetComponent<TextMeshProUGUI>() : null;
            if (headerLabel != null) headerLabel.alignment = TextAlignmentOptions.Center;
            SetReadableFont(headerLabel, headerBand * 0.62f, 16f, false);
            cursor += headerBand + 8f;

            RectTransform badge = _modalContainer.Find("RankBadge") as RectTransform;
            if (badge != null)
            {
                badge.anchorMin = badge.anchorMax = new Vector2(0.5f, 1f);
                badge.pivot = new Vector2(0.5f, 1f);
                badge.anchoredPosition = new Vector2(0f, -cursor);
                badge.sizeDelta = new Vector2(168f, badgeH);
                FitChildText(badge, "RankText", badgeH * 0.62f, 28f, false, TextAlignmentOptions.Center);
            }
            cursor += badgeH + 6f;

            RectTransform rankTitle = _modalContainer.Find("RankTitle") as RectTransform;
            PlaceTopBand(rankTitle, cursor, titleBand, pad);
            TextMeshProUGUI rankLabel = rankTitle != null ? rankTitle.GetComponent<TextMeshProUGUI>() : null;
            if (rankLabel != null) rankLabel.alignment = TextAlignmentOptions.Center;
            SetReadableFont(rankLabel, titleBand * 0.62f, 14f, false);
            cursor += titleBand + 10f;

            float statsBottom = 16f + buttonH + 12f;
            float statsRegion = Mathf.Max(40f, cardH - cursor - statsBottom);
            int rowCount = 0;
            var rows = new RectTransform[8];
            for (int i = 0; i < _modalContainer.childCount && rowCount < rows.Length; i++)
            {
                if (_modalContainer.GetChild(i).name.StartsWith("Row_") && _modalContainer.GetChild(i) is RectTransform row)
                {
                    rows[rowCount++] = row;
                }
            }

            if (rowCount > 0)
            {
                float rowGap = 4f;
                float rowH = (statsRegion - rowGap * (rowCount - 1)) / rowCount;
                rowH = Mathf.Clamp(rowH, 22f, 38f);
                float used = rowH * rowCount + rowGap * (rowCount - 1);
                float start = cursor + Mathf.Max(0f, (statsRegion - used) * 0.5f);
                for (int i = 0; i < rowCount; i++)
                {
                    PlaceTopBand(rows[i], start + i * (rowH + rowGap), rowH, pad + 6f);
                    SplitStatRow(rows[i], rowH);
                }
            }

            float buttonW = (cardW - pad * 2f - 14f) * 0.5f;
            RectTransform play = _modalContainer.Find("BtnPlayAgain") as RectTransform;
            RectTransform endless = _modalContainer.Find("BtnEndless") as RectTransform;
            if (play != null)
            {
                play.anchorMin = play.anchorMax = new Vector2(0f, 0f);
                play.pivot = new Vector2(0f, 0f);
                play.anchoredPosition = new Vector2(pad, 16f);
                play.sizeDelta = new Vector2(buttonW, buttonH);
                FitButtonLabel(play, Mathf.Clamp(buttonH * 0.36f, 14f, 22f), 12f, false);
            }
            if (endless != null)
            {
                endless.anchorMin = endless.anchorMax = new Vector2(1f, 0f);
                endless.pivot = new Vector2(1f, 0f);
                endless.anchoredPosition = new Vector2(-pad, 16f);
                endless.sizeDelta = new Vector2(buttonW, buttonH);
                FitButtonLabel(endless, Mathf.Clamp(buttonH * 0.36f, 14f, 22f), 12f, false);
            }
        }

        private static void SplitStatRow(RectTransform row, float rowH)
        {
            if (row == null) return;
            RectTransform label = row.Find("Label") as RectTransform;
            RectTransform value = row.Find("Value") as RectTransform;
            if (label != null)
            {
                label.anchorMin = Vector2.zero;
                label.anchorMax = new Vector2(0.62f, 1f);
                label.offsetMin = new Vector2(2f, 0f);
                label.offsetMax = new Vector2(-6f, 0f);
                TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.MidlineLeft;
                SetReadableFont(tmp, Mathf.Clamp(rowH * 0.52f, 14f, 22f), 12f, false);
            }
            if (value != null)
            {
                value.anchorMin = new Vector2(0.62f, 0f);
                value.anchorMax = Vector2.one;
                value.offsetMin = new Vector2(6f, 0f);
                value.offsetMax = new Vector2(-2f, 0f);
                TextMeshProUGUI tmp = value.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.MidlineRight;
                SetReadableFont(tmp, Mathf.Clamp(rowH * 0.56f, 14f, 24f), 12f, false);
            }
        }

        private static void SplitLabeledValue(RectTransform parent, string labelName, string valueName, float labelPortion, float labelFont, float valueFont)
        {
            if (parent == null) return;
            float parentH = Mathf.Max(1f, parent.sizeDelta.y);
            float labelH = Mathf.Clamp(parentH * labelPortion, 18f, 32f);

            RectTransform label = parent.Find(labelName) as RectTransform;
            if (label != null)
            {
                label.anchorMin = new Vector2(0f, 1f);
                label.anchorMax = new Vector2(1f, 1f);
                label.pivot = new Vector2(0.5f, 1f);
                label.offsetMin = new Vector2(4f, -labelH);
                label.offsetMax = new Vector2(-4f, 0f);
                TextMeshProUGUI tmp = label.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.Center;
                SetReadableFont(tmp, labelFont, 11f, false);
            }

            RectTransform value = parent.Find(valueName) as RectTransform;
            if (value != null)
            {
                float valueH = Mathf.Max(22f, parentH - labelH - 4f);
                value.anchorMin = new Vector2(0f, 0f);
                value.anchorMax = new Vector2(1f, 0f);
                value.pivot = new Vector2(0.5f, 0f);
                value.offsetMin = new Vector2(4f, 2f);
                value.offsetMax = new Vector2(-4f, 2f + valueH);
                TextMeshProUGUI tmp = value.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.alignment = TextAlignmentOptions.Center;
                SetReadableFont(tmp, Mathf.Min(valueFont, valueH * 0.7f), 16f, false);
            }
        }

        private static void PlaceTop(RectTransform rt, float width, float height, float yFromTop)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, yFromTop);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceBottom(RectTransform rt, float width, float height, float bottomGap)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bottomGap);
            rt.sizeDelta = new Vector2(width, height);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            if (rt == null) return;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void PlaceTopBand(RectTransform rt, float top, float height, float side)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(side, -(top + height));
            rt.offsetMax = new Vector2(-side, -top);
        }

        private static void PlaceColumn(RectTransform rt, int index, float top, float height, float gap, float side)
        {
            if (rt == null) return;
            const int count = 4;
            float min = index / (float)count;
            float max = (index + 1) / (float)count;
            float left = index == 0 ? side : gap * 0.5f;
            float right = index == count - 1 ? side : gap * 0.5f;
            rt.anchorMin = new Vector2(min, 1f);
            rt.anchorMax = new Vector2(max, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -(top + height));
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void LayoutPresetButtons(RectTransform row, float font)
        {
            SetBand(row.Find("Preset25") as RectTransform, 0.00f, 0.235f);
            SetBand(row.Find("Preset50") as RectTransform, 0.255f, 0.490f);
            SetBand(row.Find("Preset75") as RectTransform, 0.510f, 0.745f);
            SetBand(row.Find("Preset90") as RectTransform, 0.765f, 1.00f);
            FitButtonsIn(row, font, 11f, false);
        }

        private static void SetBand(RectTransform rt, float minX, float maxX)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(minX, 0f);
            rt.anchorMax = new Vector2(maxX, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ApplyQuickButtonsQuadrantLayout(RectTransform quickRect)
        {
            if (quickRect == null) return;

            SetQuadrant(quickRect.Find("BtnHalf") as RectTransform, new Vector2(0.00f, 0.53f), new Vector2(0.47f, 1.00f));
            SetQuadrant(quickRect.Find("BtnDouble") as RectTransform, new Vector2(0.53f, 0.53f), new Vector2(1.00f, 1.00f));
            SetQuadrant(quickRect.Find("BtnMin") as RectTransform, new Vector2(0.00f, 0.00f), new Vector2(0.47f, 0.47f));
            SetQuadrant(quickRect.Find("BtnMax") as RectTransform, new Vector2(0.53f, 0.00f), new Vector2(1.00f, 0.47f));
        }

        private static void SetQuadrant(RectTransform rt, Vector2 minAnchor, Vector2 maxAnchor)
        {
            if (rt == null) return;
            rt.anchorMin = minAnchor;
            rt.anchorMax = maxAnchor;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void FitButtonsIn(RectTransform root, float font, float min, bool wrap)
        {
            if (root == null) return;
            for (int i = 0; i < root.childCount; i++)
            {
                FitButtonLabel(root.GetChild(i) as RectTransform, font, min, wrap);
            }
        }

        private static void FitButtonLabel(RectTransform button, float font, float min, bool wrap)
        {
            if (button == null) return;
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label == null) return;

            RectTransform rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 3f);
            rt.offsetMax = new Vector2(-6f, -3f);
            label.alignment = TextAlignmentOptions.Center;
            SetReadableFont(label, font, min, wrap);
        }

        private static void FitChildText(RectTransform host, string childName, float font, float min, bool wrap, TextAlignmentOptions alignment)
        {
            if (host == null) return;

            Transform child = string.IsNullOrEmpty(childName) ? null : host.Find(childName);
            TextMeshProUGUI tmp = child != null ? child.GetComponent<TextMeshProUGUI>() : host.GetComponent<TextMeshProUGUI>();
            RectTransform textRect = child as RectTransform;
            if (textRect != null && textRect != host)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(4f, 1f);
                textRect.offsetMax = new Vector2(-4f, -1f);
            }

            if (tmp != null)
            {
                tmp.alignment = alignment;
                SetReadableFont(tmp, font, min, wrap);
            }
        }

        private static void SetReadableFont(TextMeshProUGUI label, float size, float minimum, bool wrap)
        {
            if (label == null) return;
            float max = Mathf.Max(8f, size);
            float min = Mathf.Clamp(minimum, 8f, max);
            label.enableAutoSizing = true;
            label.fontSize = max;
            label.fontSizeMin = min;
            label.fontSizeMax = max;
            label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            label.overflowMode = wrap ? TextOverflowModes.Truncate : TextOverflowModes.Ellipsis;
            label.margin = new Vector4(2f, 1f, 2f, 1f);
        }
    }
}
