using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DiceGame.Loading
{
    public sealed class LoadingSceneBootstrapper : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string targetSceneName = "DiceOverdrive";
        [SerializeField] private float minLoadingDuration = 1.6f;
        [SerializeField] private bool autoStartWhenReady = false;

        private Sprite _roundedSprite;
        private Sprite _circleSprite;
        private TMP_FontAsset _defaultFont;
        private readonly List<Object> _createdProceduralAssets = new List<Object>(8);

        private void Awake()
        {
            GenerateProceduralSprites();
            FindOrLoadFont();
            EnsureEventSystem();
            SetupEnvironment();
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
        }

        private void FindOrLoadFont()
        {
            _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_defaultFont == null)
            {
                TMP_FontAsset[] allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                if (allFonts != null && allFonts.Length > 0)
                {
                    _defaultFont = allFonts[0];
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

        private void SetupEnvironment()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f, 1.0f);
            }

            // Canvas
            GameObject canvasObj = new GameObject("LoadingCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = (Screen.width < Screen.height) ? new Vector2(1080, 1920) : new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = (Screen.width < Screen.height) ? 0.0f : 1.0f;

            canvasObj.AddComponent<GraphicRaycaster>();
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();

            // Background
            GameObject bgObj = CreateUIElement("Background", canvasRect);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.04f, 0.06f, 0.09f, 1.0f);

            GameObject glow = CreateUIElement("VignetteGlow", bgRect);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.12f, 0.12f);
            glowRect.anchorMax = new Vector2(0.88f, 0.88f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            Image glowImg = glow.AddComponent<Image>();
            glowImg.sprite = _circleSprite;
            glowImg.color = new Color(0.0f, 0.5f, 1.0f, 0.07f);

            // Safe Area Root
            GameObject safeRootObj = CreateUIElement("SafeAreaRoot", canvasRect);
            RectTransform safeRootRect = safeRootObj.GetComponent<RectTransform>();
            safeRootRect.anchorMin = Vector2.zero;
            safeRootRect.anchorMax = Vector2.one;
            safeRootRect.offsetMin = Vector2.zero;
            safeRootRect.offsetMax = Vector2.zero;

            // Center Content
            GameObject centerObj = CreateUIElement("CenterContent", safeRootRect);
            RectTransform centerRect = centerObj.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = Vector2.zero;
            centerRect.sizeDelta = new Vector2(900.0f, 650.0f);

            // Animated Dice Icons (Dual Tumbling Dice)
            GameObject diceLeftObj = CreateUIElement("AnimatedDiceLeft", centerRect);
            RectTransform diceLeftRect = diceLeftObj.GetComponent<RectTransform>();
            diceLeftRect.anchoredPosition = new Vector2(-65.0f, 175.0f);
            diceLeftRect.sizeDelta = new Vector2(105.0f, 105.0f);

            Image diceLeftImg = diceLeftObj.AddComponent<Image>();
            diceLeftImg.sprite = _roundedSprite;
            diceLeftImg.type = Image.Type.Sliced;
            diceLeftImg.color = new Color(0.14f, 0.19f, 0.28f, 1.0f);

            TextMeshProUGUI diceLeftPip = CreateText("DicePipLeft", diceLeftRect, "7", 60, FontStyles.Bold, TextAlignmentOptions.Center);
            diceLeftPip.color = new Color(0.0f, 0.9f, 1.0f, 1.0f);

            GameObject diceRightObj = CreateUIElement("AnimatedDiceRight", centerRect);
            RectTransform diceRightRect = diceRightObj.GetComponent<RectTransform>();
            diceRightRect.anchoredPosition = new Vector2(65.0f, 175.0f);
            diceRightRect.sizeDelta = new Vector2(105.0f, 105.0f);

            Image diceRightImg = diceRightObj.AddComponent<Image>();
            diceRightImg.sprite = _roundedSprite;
            diceRightImg.type = Image.Type.Sliced;
            diceRightImg.color = new Color(0.14f, 0.23f, 0.22f, 1.0f);

            TextMeshProUGUI diceRightPip = CreateText("DicePipRight", diceRightRect, "7", 60, FontStyles.Bold, TextAlignmentOptions.Center);
            diceRightPip.color = new Color(0.0f, 0.95f, 0.6f, 1.0f);

            // Title
            TextMeshProUGUI titleText = CreateText("GameTitle", centerRect, "DICE OVERDRIVE", 56, FontStyles.Bold, TextAlignmentOptions.Center);
            titleText.rectTransform.anchoredPosition = new Vector2(0.0f, 75.0f);
            titleText.rectTransform.sizeDelta = new Vector2(850.0f, 70.0f);
            titleText.color = Color.white;

            // Subtitle Badge
            GameObject subtitleObj = CreateUIElement("SubtitleBadge", centerRect);
            RectTransform subRect = subtitleObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0.0f, 25.0f);
            subRect.sizeDelta = new Vector2(260.0f, 36.0f);

            Image subBg = subtitleObj.AddComponent<Image>();
            subBg.sprite = _roundedSprite;
            subBg.type = Image.Type.Sliced;
            subBg.color = new Color(0.0f, 0.85f, 0.5f, 0.18f);

            TextMeshProUGUI subText = CreateText("SubText", subRect, "20s BLITZ RUSH", 18, FontStyles.Bold, TextAlignmentOptions.Center);
            subText.color = new Color(0.0f, 0.95f, 0.6f, 1.0f);

            // Progress Frame
            GameObject progFrameObj = CreateUIElement("ProgressBarFrame", centerRect);
            RectTransform frameRect = progFrameObj.GetComponent<RectTransform>();
            frameRect.anchoredPosition = new Vector2(0.0f, -50.0f);
            frameRect.sizeDelta = new Vector2(650.0f, 30.0f);

            Image frameImg = progFrameObj.AddComponent<Image>();
            frameImg.sprite = _roundedSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = new Color(0.10f, 0.13f, 0.19f, 0.95f);

            // Progress Fill
            GameObject progFillObj = CreateUIElement("ProgressBarFill", frameRect);
            RectTransform fillRect = progFillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3, 3);
            fillRect.offsetMax = new Vector2(-3, -3);

            Image fillImg = progFillObj.AddComponent<Image>();
            fillImg.sprite = _roundedSprite;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.color = new Color(0.0f, 0.9f, 0.6f, 1.0f);

            // Percent Text
            TextMeshProUGUI percentText = CreateText("PercentText", centerRect, "0%", 20, FontStyles.Bold, TextAlignmentOptions.Center);
            percentText.rectTransform.anchoredPosition = new Vector2(0.0f, -85.0f);
            percentText.rectTransform.sizeDelta = new Vector2(200.0f, 30.0f);
            percentText.color = Color.white;

            // Status Text
            TextMeshProUGUI statusText = CreateText("StatusText", centerRect, "INITIALIZING DICE ENGINE...", 16, FontStyles.Normal, TextAlignmentOptions.Center);
            statusText.rectTransform.anchoredPosition = new Vector2(0.0f, -118.0f);
            statusText.rectTransform.sizeDelta = new Vector2(750.0f, 30.0f);
            statusText.color = new Color(0.6f, 0.72f, 0.88f, 0.85f);

            // Gameplay Tips Marquee
            GameObject tipsBoxObj = CreateUIElement("TipsBox", centerRect);
            RectTransform tipsBoxRect = tipsBoxObj.GetComponent<RectTransform>();
            tipsBoxRect.anchoredPosition = new Vector2(0.0f, -156.0f);
            tipsBoxRect.sizeDelta = new Vector2(760.0f, 36.0f);

            Image tipsBoxBg = tipsBoxObj.AddComponent<Image>();
            tipsBoxBg.sprite = _roundedSprite;
            tipsBoxBg.type = Image.Type.Sliced;
            tipsBoxBg.color = new Color(0.07f, 0.10f, 0.16f, 0.85f);

            TextMeshProUGUI tipsText = CreateText("TipsText", tipsBoxRect, "TIP: Land in the Golden Crit Zone for a massive 3.0x Jackpot Payout!", 14, FontStyles.Italic, TextAlignmentOptions.Center);
            tipsText.color = new Color(0.85f, 0.88f, 0.95f, 0.9f);

            // Start Button
            GameObject startBtnObj = CreateUIElement("StartButton", centerRect);
            RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchoredPosition = new Vector2(0.0f, -225.0f);
            startBtnRect.sizeDelta = new Vector2(340.0f, 65.0f);

            Image startBtnImg = startBtnObj.AddComponent<Image>();
            startBtnImg.sprite = _roundedSprite;
            startBtnImg.type = Image.Type.Sliced;
            startBtnImg.color = new Color(0.0f, 0.85f, 0.5f, 1.0f);
            Button startBtn = startBtnObj.AddComponent<Button>();
            CanvasGroup startBtnGroup = startBtnObj.AddComponent<CanvasGroup>();

            ColorBlock cb = startBtn.colors;
            cb.highlightedColor = new Color(0.2f, 1.0f, 0.65f, 1.0f);
            cb.pressedColor = new Color(0.0f, 0.7f, 0.4f, 1.0f);
            startBtn.colors = cb;

            TextMeshProUGUI startText = CreateText("StartText", startBtnRect, "ENTER THE ARENA", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            startText.color = new Color(0.04f, 0.15f, 0.08f, 1.0f);

            // Screen Fade Overlay
            GameObject fadeObj = CreateUIElement("ScreenFadeOverlay", canvasRect);
            RectTransform fadeRect = fadeObj.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;

            Image fadeImg = fadeObj.AddComponent<Image>();
            fadeImg.color = Color.black;
            fadeImg.raycastTarget = false;
            CanvasGroup fadeGroup = fadeObj.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0.0f;

            // Loading Controller
            GameObject ctrlObj = new GameObject("LoadingController");
            DiceLoadingController ctrl = ctrlObj.AddComponent<DiceLoadingController>();
            SetPrivateField(ctrl, "progressBarFill", fillImg);
            SetPrivateField(ctrl, "progressPercentText", percentText);
            SetPrivateField(ctrl, "statusMessageText", statusText);
            SetPrivateField(ctrl, "tipsText", tipsText);
            SetPrivateField(ctrl, "animatedDiceIcon", diceLeftRect);
            SetPrivateField(ctrl, "animatedDiceIconRight", diceRightRect);
            SetPrivateField(ctrl, "diceLeftPipText", diceLeftPip);
            SetPrivateField(ctrl, "diceRightPipText", diceRightPip);
            SetPrivateField(ctrl, "buttonStart", startBtn);
            SetPrivateField(ctrl, "startButtonCanvasGroup", startBtnGroup);
            SetPrivateField(ctrl, "screenFadeCanvasGroup", fadeGroup);
            SetPrivateField(ctrl, "canvasScaler", scaler);
            SetPrivateField(ctrl, "centerContent", centerRect);
            SetPrivateField(ctrl, "safeAreaRoot", safeRootRect);
            SetPrivateField(ctrl, "targetSceneName", targetSceneName);
            SetPrivateField(ctrl, "minLoadingDuration", minLoadingDuration);
            SetPrivateField(ctrl, "autoStartWhenReady", autoStartWhenReady);
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
            if (_defaultFont != null) tmp.font = _defaultFont;
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
