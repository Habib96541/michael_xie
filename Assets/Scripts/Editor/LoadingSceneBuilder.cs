using System.IO;
using DiceGame.Loading;
using DiceGame.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DiceGame.Editor
{
    public static class LoadingSceneBuilder
    {
        private static TMP_FontAsset _defaultFont;
        private static Sprite _roundedSprite;
        private static Sprite _circleSprite;
        private static Sprite _backgroundSprite;
        private static Sprite _panelSprite;
        private static Sprite _buttonSprite;
        private static Sprite _primaryButtonSprite;

        [MenuItem("DiceGame/Build Loading Scene", false, 11)]
        public static void BuildScene()
        {
            LoadResources();

            string scenePath = "Assets/Scenes/LoadingScene.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.09f, 1.0f);
            cam.orthographic = true;
            cam.orthographicSize = 5.0f;
            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();

            GameObject canvasObj = new GameObject("LoadingCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();

            GameObject bgObj = CreateUIElement("Background", canvasRect);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.sprite = _backgroundSprite;
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            GameObject glow = CreateUIElement("VignetteGlow", bgRect);
            RectTransform glowRect = glow.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.15f, 0.15f);
            glowRect.anchorMax = new Vector2(0.85f, 0.85f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;

            Image glowImg = glow.AddComponent<Image>();
            glowImg.sprite = _circleSprite;
            glowImg.color = new Color(0.0f, 0.45f, 0.9f, 0.06f);

            GameObject safeRootObj = CreateUIElement("SafeAreaRoot", canvasRect);
            RectTransform safeRootRect = safeRootObj.GetComponent<RectTransform>();
            safeRootRect.anchorMin = Vector2.zero;
            safeRootRect.anchorMax = Vector2.one;
            safeRootRect.offsetMin = Vector2.zero;
            safeRootRect.offsetMax = Vector2.zero;

            GameObject centerObj = CreateUIElement("CenterContent", safeRootRect);
            RectTransform centerRect = centerObj.GetComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = Vector2.zero;
            centerRect.sizeDelta = new Vector2(900.0f, 640.0f);

            GameObject diceLeftObj = CreateUIElement("AnimatedDiceLeft", centerRect);
            RectTransform diceLeftRect = diceLeftObj.GetComponent<RectTransform>();
            diceLeftRect.anchoredPosition = new Vector2(-78.0f, 228.0f);
            diceLeftRect.sizeDelta = new Vector2(108.0f, 108.0f);

            Image diceLeftImg = diceLeftObj.AddComponent<Image>();
            diceLeftImg.sprite = _buttonSprite;
            diceLeftImg.type = Image.Type.Sliced;
            diceLeftImg.color = Color.white;

            TextMeshProUGUI diceLeftPip = CreateText("DicePipLeft", diceLeftRect, "7", 60, FontStyles.Bold, TextAlignmentOptions.Center);
            diceLeftPip.color = new Color(0.0f, 0.9f, 1.0f, 1.0f);

            GameObject diceRightObj = CreateUIElement("AnimatedDiceRight", centerRect);
            RectTransform diceRightRect = diceRightObj.GetComponent<RectTransform>();
            diceRightRect.anchoredPosition = new Vector2(78.0f, 228.0f);
            diceRightRect.sizeDelta = new Vector2(108.0f, 108.0f);

            Image diceRightImg = diceRightObj.AddComponent<Image>();
            diceRightImg.sprite = _buttonSprite;
            diceRightImg.type = Image.Type.Sliced;
            diceRightImg.color = Color.white;

            TextMeshProUGUI diceRightPip = CreateText("DicePipRight", diceRightRect, "7", 60, FontStyles.Bold, TextAlignmentOptions.Center);
            diceRightPip.color = new Color(0.0f, 0.95f, 0.6f, 1.0f);

            TextMeshProUGUI titleText = CreateText("GameTitle", centerRect, "DICE OVERDRIVE", 52, FontStyles.Bold, TextAlignmentOptions.Center);
            PinCenter(titleText.rectTransform, new Vector2(0.0f, 118.0f), new Vector2(820.0f, 60.0f));
            titleText.color = Color.white;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 28.0f;
            titleText.fontSizeMax = 52.0f;

            GameObject subtitleObj = CreateUIElement("SubtitleBadge", centerRect);
            RectTransform subRect = subtitleObj.GetComponent<RectTransform>();
            subRect.anchoredPosition = new Vector2(0.0f, 54.0f);
            subRect.sizeDelta = new Vector2(340.0f, 34.0f);

            Image subBg = subtitleObj.AddComponent<Image>();
            subBg.sprite = _panelSprite;
            subBg.type = Image.Type.Sliced;
            subBg.color = Color.white;

            TextMeshProUGUI subText = CreateText("SubText", subRect, "20s BLITZ RUSH", 22, FontStyles.Bold, TextAlignmentOptions.Center);
            subText.color = new Color(0.0f, 0.95f, 0.6f, 1.0f);

            GameObject progFrameObj = CreateUIElement("ProgressBarFrame", centerRect);
            RectTransform frameRect = progFrameObj.GetComponent<RectTransform>();
            frameRect.anchoredPosition = new Vector2(0.0f, 0.0f);
            frameRect.sizeDelta = new Vector2(640.0f, 26.0f);

            Image frameImg = progFrameObj.AddComponent<Image>();
            frameImg.sprite = _panelSprite;
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.white;

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

            TextMeshProUGUI percentText = CreateText("PercentText", centerRect, "0%", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            PinCenter(percentText.rectTransform, new Vector2(0.0f, -40.0f), new Vector2(220.0f, 28.0f));
            percentText.color = Color.white;

            TextMeshProUGUI statusText = CreateText("StatusText", centerRect, "INITIALIZING DICE ENGINE...", 18, FontStyles.Normal, TextAlignmentOptions.Center);
            PinCenter(statusText.rectTransform, new Vector2(0.0f, -82.0f), new Vector2(760.0f, 28.0f));
            statusText.color = new Color(0.6f, 0.72f, 0.88f, 0.85f);

            GameObject tipsBoxObj = CreateUIElement("TipsBox", centerRect);
            RectTransform tipsBoxRect = tipsBoxObj.GetComponent<RectTransform>();
            tipsBoxRect.anchoredPosition = new Vector2(0.0f, -132.0f);
            tipsBoxRect.sizeDelta = new Vector2(800.0f, 46.0f);

            Image tipsBoxBg = tipsBoxObj.AddComponent<Image>();
            tipsBoxBg.sprite = _panelSprite;
            tipsBoxBg.type = Image.Type.Sliced;
            tipsBoxBg.color = Color.white;

            TextMeshProUGUI tipsText = CreateText("TipsText", tipsBoxRect, "TIP: Land in the Golden Crit Zone for a massive 3.0x Jackpot Payout!", 19, FontStyles.Italic, TextAlignmentOptions.Center);
            tipsText.color = new Color(0.9f, 0.95f, 1.0f, 1.0f);
            tipsText.enableAutoSizing = true;
            tipsText.fontSizeMin = 15.0f;
            tipsText.fontSizeMax = 19.0f;

            GameObject startBtnObj = CreateUIElement("StartButton", centerRect);
            RectTransform startBtnRect = startBtnObj.GetComponent<RectTransform>();
            startBtnRect.anchoredPosition = new Vector2(0.0f, -208.0f);
            startBtnRect.sizeDelta = new Vector2(360.0f, 64.0f);

            Image startBtnImg = startBtnObj.AddComponent<Image>();
            startBtnImg.sprite = _primaryButtonSprite;
            startBtnImg.type = Image.Type.Sliced;
            startBtnImg.color = Color.white;
            Button startBtn = startBtnObj.AddComponent<Button>();
            startBtnObj.AddComponent<UIButtonFeel>();
            CanvasGroup startBtnGroup = startBtnObj.AddComponent<CanvasGroup>();

            ColorBlock cb = startBtn.colors;
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.75f, 0.9f, 0.84f, 1.0f);
            startBtn.colors = cb;

            TextMeshProUGUI startText = CreateText("StartText", startBtnRect, "ENTER THE ARENA", 28, FontStyles.Bold, TextAlignmentOptions.Center);
            startText.color = new Color(0.035f, 0.14f, 0.12f, 1.0f);

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
            SetPrivateField(ctrl, "targetSceneName", "DiceOverdrive");
            SetPrivateField(ctrl, "minLoadingDuration", 1.5f);
            SetPrivateField(ctrl, "autoStartWhenReady", false);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[DiceGame] Successfully built and saved LoadingScene at " + scenePath);

            RegisterBuildScenes(scenePath, "Assets/Scenes/DiceOverdrive.unity");
        }

        private static void RegisterBuildScenes(string loadingPath, string mainPath)
        {
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(loadingPath, true),
                new EditorBuildSettingsScene(mainPath, true)
            };
            EditorBuildSettings.scenes = newScenes;
            Debug.Log("[DiceGame] Configured EditorBuildSettings with LoadingScene (index 0) and DiceOverdrive (index 1).");
        }

        private static void LoadResources()
        {
            DiceSceneBuilder.GenerateUISprites();

            _defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (_defaultFont == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _defaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                }
            }

            _roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UIRoundedBox.png");
            _circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UICircle.png");
            _backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DiceNeonBackground.png");
            _panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DiceGlassPanel.png");
            _buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DiceButtonSurface.png");
            _primaryButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/DiceButtonPrimary.png");
        }

        private static GameObject CreateUIElement(string name, RectTransform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void PinCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, string content, float fontSize, FontStyles style, TextAlignmentOptions alignment)
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

