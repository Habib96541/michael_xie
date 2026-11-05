using CyberScratch.Audio;
using CyberScratch.Data;
using CyberScratch.Scratch;
using CyberScratch.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CyberScratch.Core
{
    public static partial class CyberScratchBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeGameAtRuntime()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;

            string activeSceneName = SceneManager.GetActiveScene().name;

            if (activeSceneName == "MainMenuScene")
            {
                if (Object.FindAnyObjectByType<MainMenuController>() != null && Object.FindAnyObjectByType<CyberScratchGameManager>() == null)
                {
                    return;
                }
                ReplaceLegacyHierarchy();
                Debug.Log("<color=#00FFAA><b>[CyberScratch]</b> Initializing MainMenuScene hierarchy...</color>");
                BuildMainMenuSceneHierarchy();
                return;
            }

            if (activeSceneName == "GameScene")
            {
                GameObject boardFrameObj = GameObject.Find("BoardFrame");
                RectTransform bfRt = boardFrameObj != null ? boardFrameObj.GetComponent<RectTransform>() : null;
                Image bfImg = boardFrameObj != null ? boardFrameObj.GetComponent<Image>() : null;
                bool isLayoutOutdated = bfRt == null || bfRt.anchorMin.y > 0.129f || bfImg == null || bfImg.pixelsPerUnitMultiplier < 2.9f;

                if (GameObject.Find("GameSceneArt_v6") != null && Object.FindAnyObjectByType<CyberScratchGameManager>() != null && Object.FindAnyObjectByType<MainMenuController>() == null && !isLayoutOutdated)
                {
                    return;
                }
                ReplaceLegacyHierarchy();
                Debug.Log("<color=#00FFAA><b>[CyberScratch]</b> Initializing GameScene hierarchy with fitted super-board...</color>");
                BuildGameSceneHierarchy();
                return;
            }

            // Fallback for SampleScene or custom scenes
            bool hasArtBoard = GameObject.Find("GameSceneArt_v6") != null;
            if (hasArtBoard && Object.FindAnyObjectByType<CyberScratchGameManager>() != null)
            {
                return;
            }

            if (!hasArtBoard)
            {
                ReplaceLegacyHierarchy();
            }

            Debug.Log("<color=#00FFAA><b>[CyberScratch]</b> Building full CyberScratch 5X fallback hierarchy...</color>");
            BuildCompleteGameHierarchy();
        }

        public static void ReplaceLegacyHierarchy()
        {
            CyberScratchGameManager[] managers = Object.FindObjectsByType<CyberScratchGameManager>(FindObjectsInactive.Include);
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                {
                    Object.DestroyImmediate(managers[i].gameObject);
                }
            }

            MainMenuController[] controllers = Object.FindObjectsByType<MainMenuController>(FindObjectsInactive.Include);
            for (int i = 0; i < controllers.Length; i++)
            {
                if (controllers[i] != null)
                {
                    Object.DestroyImmediate(controllers[i].gameObject);
                }
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null && canvases[i].name == "CyberScratch_Canvas")
                {
                    Object.DestroyImmediate(canvases[i].gameObject);
                }
            }
        }

        public static Camera EnsureCameraAndAudioListener()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
                cam.orthographicSize = 9.6f;
                camObj.transform.position = new Vector3(0, 0, -10);
                camObj.AddComponent<AudioListener>();
            }
            else if (cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }
            return cam;
        }

        public static EventSystem EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<EventSystem>();
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    esObj.AddComponent(inputModuleType);
                }
                else
                {
                    esObj.AddComponent<StandaloneInputModule>();
                }
            }
            return es;
        }

        public static GameObject CreateStandardCanvas()
        {
            GameObject canvasObj = new GameObject("CyberScratch_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1600);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj;
        }

        public static CyberAudioService EnsureAudioService()
        {
            var audio = Object.FindAnyObjectByType<CyberAudioService>();
            if (audio == null)
            {
                GameObject audioObj = new GameObject("CyberScratch_AudioService");
                audio = audioObj.AddComponent<CyberAudioService>();
            }
            return audio;
        }

        public static Font GetDefaultFont()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }
            return defaultFont;
        }

        public static CyberScratchConfig CreateConfig()
        {
            CyberScratchConfig config = ScriptableObject.CreateInstance<CyberScratchConfig>();
            config.InitializeDefaultsIfNeeded();
            config.Symbols[0].SetIcon(Art("04_Bronze_Gear.png", CyberScratchArt.BronzeGearSprite));
            config.Symbols[1].SetIcon(Art("05_Silver_Ingots.png", CyberScratchArt.SilverIngotSprite));
            config.Symbols[2].SetIcon(Art("07_Gold_Power_Core.png", CyberScratchArt.GoldCoreSprite));
            config.Symbols[3].SetIcon(Art("06_Plasma_Gem.png", CyberScratchArt.PlasmaGemSprite));
            config.Symbols[4].SetIcon(Art("08_Cyber_Crown.png", CyberScratchArt.CyberCrownSprite));
            config.Symbols[5].SetIcon(Art("09_Lucky_777.png", CyberScratchArt.Lucky777Sprite));
            return config;
        }

        public static MainMenuController BuildMainMenuSceneHierarchy()
        {
            EnsureCameraAndAudioListener();
            EnsureEventSystem();
            EnsureAudioService();

            GameObject canvasObj = CreateStandardCanvas();
            Font defaultFont = GetDefaultFont();

            MainMenuView mainMenuView = BuildMenuScreen(canvasObj.transform, defaultFont);
            HowToPlayModalView howToPlayModal = BuildHowToPlayModal(canvasObj.transform, defaultFont);

            GameObject controllerObj = new GameObject("CyberScratch_MainMenuController");
            MainMenuController controller = controllerObj.AddComponent<MainMenuController>();
            SetField(controller, "_mainMenuView", mainMenuView);
            SetField(controller, "_howToPlayModal", howToPlayModal);

            return controller;
        }

        public static CyberScratchGameManager BuildGameSceneHierarchy()
        {
            EnsureCameraAndAudioListener();
            EnsureEventSystem();
            EnsureAudioService();

            GameObject canvasObj = CreateStandardCanvas();
            Font defaultFont = GetDefaultFont();

            GameplayRefs gameplay = BuildArtGameplay(canvasObj.transform, defaultFont);
            HowToPlayModalView howToPlayModal = BuildHowToPlayModal(canvasObj.transform, defaultFont);
            WinModalView winModal = BuildWinModal(canvasObj.transform, defaultFont);

            GameObject gmObj = new GameObject("CyberScratch_GameManager");
            CyberScratchGameManager gm = gmObj.AddComponent<CyberScratchGameManager>();

            CyberScratchConfig config = CreateConfig();
            SetField(gm, "_config", config);
            SetField(gm, "_mainMenuView", null);
            SetField(gm, "_howToPlayModal", howToPlayModal);
            SetField(gm, "_gameplayContainerGroup", gameplay.Group);
            SetField(gm, "_scratchCard", gameplay.Scratch);
            SetField(gm, "_rowViews", gameplay.Rows);
            SetField(gm, "_hudView", gameplay.Hud);
            SetField(gm, "_winModal", winModal);

            gm.InitializeGame();
            return gm;
        }

        public static CyberScratchGameManager BuildCompleteGameHierarchy()
        {
            EnsureCameraAndAudioListener();
            EnsureEventSystem();
            EnsureAudioService();

            GameObject canvasObj = CreateStandardCanvas();
            Font defaultFont = GetDefaultFont();

            GameplayRefs gameplay = BuildArtGameplay(canvasObj.transform, defaultFont);
            MainMenuView mainMenuView = BuildMenuScreen(canvasObj.transform, defaultFont);
            HowToPlayModalView howToPlayModal = BuildHowToPlayModal(canvasObj.transform, defaultFont);
            WinModalView winModal = BuildWinModal(canvasObj.transform, defaultFont);

            GameObject gmObj = new GameObject("CyberScratch_GameManager");
            CyberScratchGameManager gm = gmObj.AddComponent<CyberScratchGameManager>();

            CyberScratchConfig config = CreateConfig();
            SetField(gm, "_config", config);
            SetField(gm, "_mainMenuView", mainMenuView);
            SetField(gm, "_howToPlayModal", howToPlayModal);
            SetField(gm, "_gameplayContainerGroup", gameplay.Group);
            SetField(gm, "_scratchCard", gameplay.Scratch);
            SetField(gm, "_rowViews", gameplay.Rows);
            SetField(gm, "_hudView", gameplay.Hud);
            SetField(gm, "_winModal", winModal);

            gm.InitializeGame();
            return gm;
        }

        public static MainMenuView BuildMenuScreen(Transform parent, Font defaultFont)
        {
            GameObject mainMenuObj = CreateUI("MainMenu_Screen", parent);
            CanvasGroup menuCg = mainMenuObj.AddComponent<CanvasGroup>();
            Stretch(mainMenuObj.GetComponent<RectTransform>());

            GameObject menuBgObj = CreateUI("MenuBackground", mainMenuObj.transform);
            Image menuBgImg = menuBgObj.AddComponent<Image>();
            menuBgImg.sprite = Art("01_fullscreen_cyber_background.png", null);
            menuBgImg.color = menuBgImg.sprite != null ? Color.white : new Color(0.03f, 0.05f, 0.08f, 1f);
            Stretch(menuBgObj.GetComponent<RectTransform>());

            GameObject menuSafeObj = CreateUI("MenuSafeArea", mainMenuObj.transform);
            RectTransform menuSafeRect = menuSafeObj.GetComponent<RectTransform>();
            Stretch(menuSafeRect);
            menuSafeRect.offsetMin = new Vector2(40, 50);
            menuSafeRect.offsetMax = new Vector2(-40, -60);

            GameObject mmLogoObj = CreateUI("LogoTitle", menuSafeObj.transform);
            Image mmLogoImg = mmLogoObj.AddComponent<Image>();
            mmLogoImg.sprite = Art("01_CyberScratch_5X_Logo.png", null);
            mmLogoImg.preserveAspect = true;
            mmLogoImg.raycastTarget = false;
            mmLogoImg.color = mmLogoImg.sprite != null ? Color.white : new Color(0.15f, 0.90f, 1.0f);
            RectTransform mmLogoRect = mmLogoObj.GetComponent<RectTransform>();
            mmLogoRect.anchorMin = new Vector2(0.04f, 0.78f);
            mmLogoRect.anchorMax = new Vector2(0.96f, 0.96f);
            mmLogoRect.offsetMin = Vector2.zero;
            mmLogoRect.offsetMax = Vector2.zero;

            GameObject mmSubObj = CreateUI("Subtitle", menuSafeObj.transform);
            Text mmSubText = mmSubObj.AddComponent<Text>();
            mmSubText.font = defaultFont;
            mmSubText.text = "INDUSTRIAL CYBERPUNK 2D WEBGL EDITION";
            mmSubText.fontSize = 22;
            mmSubText.resizeTextForBestFit = true;
            mmSubText.resizeTextMinSize = 20;
            mmSubText.resizeTextMaxSize = 24;
            var mmSubShadow = mmSubObj.AddComponent<Shadow>();
            mmSubShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            mmSubShadow.effectDistance = new Vector2(1.5f, -1.5f);
            mmSubText.fontStyle = FontStyle.Bold;
            mmSubText.alignment = TextAnchor.MiddleCenter;
            mmSubText.color = new Color(1.0f, 0.85f, 0.30f, 1f);
            RectTransform mmSubRect = mmSubObj.GetComponent<RectTransform>();
            mmSubRect.anchorMin = new Vector2(0, 0.68f);
            mmSubRect.anchorMax = new Vector2(1, 0.72f);
            mmSubRect.offsetMin = Vector2.zero;
            mmSubRect.offsetMax = Vector2.zero;

            // Balance readout card on Menu
            GameObject mmBalBox = CreateUI("MenuBalanceBox", menuSafeObj.transform);
            RectTransform mmBalBoxRect = mmBalBox.GetComponent<RectTransform>();
            mmBalBoxRect.anchorMin = new Vector2(0.1f, 0.58f);
            mmBalBoxRect.anchorMax = new Vector2(0.9f, 0.66f);
            mmBalBoxRect.offsetMin = Vector2.zero;
            mmBalBoxRect.offsetMax = Vector2.zero;

            Image mmBalImg = mmBalBox.AddComponent<Image>();
            mmBalImg.sprite = Art("03_balance_hud_blank.png", CyberScratchArt.RowBorderIdleSprite);
            mmBalImg.type = Image.Type.Simple;
            mmBalImg.preserveAspect = true;
            mmBalImg.color = Color.white;

            GameObject mmBalTextObj = CreateUI("BalanceText", mmBalBox.transform);
            Text mmBalText = mmBalTextObj.AddComponent<Text>();
            mmBalText.font = defaultFont;
            mmBalText.text = "BALANCE: $1,250.00";
            mmBalText.fontSize = 32;
            mmBalText.fontStyle = FontStyle.Bold;
            mmBalText.alignment = TextAnchor.MiddleCenter;
            mmBalText.color = new Color(0.95f, 0.95f, 1f);
            mmBalText.resizeTextForBestFit = true;
            mmBalText.resizeTextMinSize = 20;
            mmBalText.resizeTextMaxSize = 34;
            var mmBalShadow = mmBalTextObj.AddComponent<Shadow>();
            mmBalShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            mmBalShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform mmBalTxtRect = mmBalTextObj.GetComponent<RectTransform>();
            Stretch(mmBalTxtRect);
            mmBalTxtRect.offsetMin = new Vector2(24, 10);
            mmBalTxtRect.offsetMax = new Vector2(-24, -10);

            // [ PLAY GAME ] Button
            GameObject playBtnObj = CreateUI("PlayButton", menuSafeObj.transform);
            RectTransform playBtnRect = playBtnObj.GetComponent<RectTransform>();
            playBtnRect.anchorMin = new Vector2(0.12f, 0.44f);
            playBtnRect.anchorMax = new Vector2(0.88f, 0.54f);
            playBtnRect.offsetMin = Vector2.zero;
            playBtnRect.offsetMax = Vector2.zero;

            Image playBtnImg = playBtnObj.AddComponent<Image>();
            playBtnImg.sprite = Art("15_new_card_button_green_blank.png", CyberScratchArt.ButtonEmeraldSprite);
            playBtnImg.type = Image.Type.Simple;
            playBtnImg.preserveAspect = true;
            Button playBtn = playBtnObj.AddComponent<Button>();

            GameObject playBtnTxtObj = CreateUI("Text", playBtnObj.transform);
            Text playBtnTxt = playBtnTxtObj.AddComponent<Text>();
            playBtnTxt.font = defaultFont;
            playBtnTxt.text = "PLAY GAME";
            playBtnTxt.fontSize = 38;
            playBtnTxt.fontStyle = FontStyle.Bold;
            playBtnTxt.alignment = TextAnchor.MiddleCenter;
            playBtnTxt.color = Color.white;
            playBtnTxt.resizeTextForBestFit = true;
            playBtnTxt.resizeTextMinSize = 20;
            playBtnTxt.resizeTextMaxSize = 38;
            var playBtnShadow = playBtnTxtObj.AddComponent<Shadow>();
            playBtnShadow.effectColor = new Color(0f, 0.25f, 0.08f, 0.85f);
            playBtnShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform playBtnTxtRect = playBtnTxtObj.GetComponent<RectTransform>();
            Stretch(playBtnTxtRect);
            playBtnTxtRect.offsetMin = new Vector2(20, 10);
            playBtnTxtRect.offsetMax = new Vector2(-20, -10);

            // [ HOW TO PLAY ] Button
            GameObject rulesBtnObj = CreateUI("HowToPlayButton", menuSafeObj.transform);
            RectTransform rulesBtnRect = rulesBtnObj.GetComponent<RectTransform>();
            rulesBtnRect.anchorMin = new Vector2(0.12f, 0.32f);
            rulesBtnRect.anchorMax = new Vector2(0.88f, 0.41f);
            rulesBtnRect.offsetMin = Vector2.zero;
            rulesBtnRect.offsetMax = Vector2.zero;

            Image rulesBtnImg = rulesBtnObj.AddComponent<Image>();
            rulesBtnImg.sprite = Art("16_blue_action_button_blank.png", CyberScratchArt.ButtonCyanSprite);
            rulesBtnImg.type = Image.Type.Simple;
            rulesBtnImg.preserveAspect = true;
            Button rulesBtn = rulesBtnObj.AddComponent<Button>();

            GameObject rulesBtnTxtObj = CreateUI("Text", rulesBtnObj.transform);
            Text rulesBtnTxt = rulesBtnTxtObj.AddComponent<Text>();
            rulesBtnTxt.font = defaultFont;
            rulesBtnTxt.text = "HOW TO PLAY & PAYTABLE";
            rulesBtnTxt.fontSize = 28;
            rulesBtnTxt.fontStyle = FontStyle.Bold;
            rulesBtnTxt.alignment = TextAnchor.MiddleCenter;
            rulesBtnTxt.color = Color.white;
            rulesBtnTxt.resizeTextForBestFit = true;
            rulesBtnTxt.resizeTextMinSize = 20;
            rulesBtnTxt.resizeTextMaxSize = 28;
            var rulesBtnShadow = rulesBtnTxtObj.AddComponent<Shadow>();
            rulesBtnShadow.effectColor = new Color(0f, 0.15f, 0.35f, 0.85f);
            rulesBtnShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform rulesBtnTxtRect = rulesBtnTxtObj.GetComponent<RectTransform>();
            Stretch(rulesBtnTxtRect);
            rulesBtnTxtRect.offsetMin = new Vector2(20, 10);
            rulesBtnTxtRect.offsetMax = new Vector2(-20, -10);

            // [ SOUND TOGGLE ] Button
            GameObject mSoundBtnObj = CreateUI("SoundToggleButton", menuSafeObj.transform);
            RectTransform mSoundBtnRect = mSoundBtnObj.GetComponent<RectTransform>();
            mSoundBtnRect.anchorMin = new Vector2(0.12f, 0.21f);
            mSoundBtnRect.anchorMax = new Vector2(0.48f, 0.29f);
            mSoundBtnRect.offsetMin = Vector2.zero;
            mSoundBtnRect.offsetMax = Vector2.zero;

            Image mSoundImg = mSoundBtnObj.AddComponent<Image>();
            mSoundImg.sprite = Art("16_blue_action_button_blank.png", CyberScratchArt.ButtonCyanSprite);
            mSoundImg.type = Image.Type.Simple;
            mSoundImg.preserveAspect = true;
            Button mSoundBtn = mSoundBtnObj.AddComponent<Button>();

            GameObject mSoundTxtObj = CreateUI("Text", mSoundBtnObj.transform);
            Text mSoundTxt = mSoundTxtObj.AddComponent<Text>();
            mSoundTxt.font = defaultFont;
            mSoundTxt.text = "SOUND: [ON]";
            mSoundTxt.fontSize = 24;
            mSoundTxt.fontStyle = FontStyle.Bold;
            mSoundTxt.alignment = TextAnchor.MiddleCenter;
            mSoundTxt.color = Color.white;
            mSoundTxt.resizeTextForBestFit = true;
            mSoundTxt.resizeTextMinSize = 18;
            mSoundTxt.resizeTextMaxSize = 24;
            var mSoundShadow = mSoundTxtObj.AddComponent<Shadow>();
            mSoundShadow.effectColor = new Color(0f, 0.15f, 0.35f, 0.85f);
            mSoundShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform mSoundTxtRect = mSoundTxtObj.GetComponent<RectTransform>();
            Stretch(mSoundTxtRect);
            mSoundTxtRect.offsetMin = new Vector2(16, 8);
            mSoundTxtRect.offsetMax = new Vector2(-16, -8);

            // [ REFILL / RESET BALANCE ] Button
            GameObject refillBtnObj = CreateUI("RefillBalanceButton", menuSafeObj.transform);
            RectTransform refillBtnRect = refillBtnObj.GetComponent<RectTransform>();
            refillBtnRect.anchorMin = new Vector2(0.52f, 0.21f);
            refillBtnRect.anchorMax = new Vector2(0.88f, 0.29f);
            refillBtnRect.offsetMin = Vector2.zero;
            refillBtnRect.offsetMax = Vector2.zero;

            Image refillImg = refillBtnObj.AddComponent<Image>();
            refillImg.sprite = Art("14_reveal_button_gold_blank.png", CyberScratchArt.ButtonAmberSprite);
            refillImg.type = Image.Type.Simple;
            refillImg.preserveAspect = true;
            Button refillBtn = refillBtnObj.AddComponent<Button>();

            GameObject refillTxtObj = CreateUI("Text", refillBtnObj.transform);
            Text refillTxt = refillTxtObj.AddComponent<Text>();
            refillTxt.font = defaultFont;
            refillTxt.text = "REFILL $1,250";
            refillTxt.fontSize = 24;
            refillTxt.fontStyle = FontStyle.Bold;
            refillTxt.alignment = TextAnchor.MiddleCenter;
            refillTxt.color = Color.white;
            refillTxt.resizeTextForBestFit = true;
            refillTxt.resizeTextMinSize = 18;
            refillTxt.resizeTextMaxSize = 24;
            var refillShadow = refillTxtObj.AddComponent<Shadow>();
            refillShadow.effectColor = new Color(0.35f, 0.18f, 0.02f, 0.85f);
            refillShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform refillTxtRect = refillTxtObj.GetComponent<RectTransform>();
            Stretch(refillTxtRect);
            refillTxtRect.offsetMin = new Vector2(16, 8);
            refillTxtRect.offsetMax = new Vector2(-16, -8);

            // Menu Footer
            GameObject mmFooterObj = CreateUI("MenuFooter", menuSafeObj.transform);
            Text mmFooterText = mmFooterObj.AddComponent<Text>();
            mmFooterText.font = defaultFont;
            mmFooterText.text = "5 HORIZONTAL ROWS · 3 COLUMNS · 250X JACKPOT";
            mmFooterText.fontSize = 22;
            mmFooterText.resizeTextForBestFit = true;
            mmFooterText.resizeTextMinSize = 20;
            mmFooterText.resizeTextMaxSize = 22;
            mmFooterText.fontStyle = FontStyle.Bold;
            var mmFootShadow = mmFooterObj.AddComponent<Shadow>();
            mmFootShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            mmFootShadow.effectDistance = new Vector2(1.5f, -1.5f);
            mmFooterText.alignment = TextAnchor.MiddleCenter;
            mmFooterText.color = new Color(0.35f, 0.90f, 1.0f, 0.95f);
            RectTransform mmFooterRect = mmFooterObj.GetComponent<RectTransform>();
            mmFooterRect.anchorMin = new Vector2(0, 0.05f);
            mmFooterRect.anchorMax = new Vector2(1, 0.12f);
            mmFooterRect.offsetMin = Vector2.zero;
            mmFooterRect.offsetMax = Vector2.zero;

            MainMenuView mainMenuView = mainMenuObj.AddComponent<MainMenuView>();
            SetField(mainMenuView, "_canvasGroup", menuCg);
            SetField(mainMenuView, "_balanceText", mmBalText);
            SetField(mainMenuView, "_playButton", playBtn);
            SetField(mainMenuView, "_howToPlayButton", rulesBtn);
            SetField(mainMenuView, "_soundButton", mSoundBtn);
            SetField(mainMenuView, "_soundButtonText", mSoundTxt);
            SetField(mainMenuView, "_resetBalanceButton", refillBtn);

            return mainMenuView;
        }

        public static HowToPlayModalView BuildHowToPlayModal(Transform parent, Font defaultFont)
        {
            GameObject howToPlayObj = CreateUI("HowToPlayModal", parent);
            CanvasGroup htpCg = howToPlayObj.AddComponent<CanvasGroup>();
            Stretch(howToPlayObj.GetComponent<RectTransform>());

            Image htpBg = howToPlayObj.AddComponent<Image>();
            htpBg.color = new Color(0, 0, 0, 0.88f);

            GameObject htpPanelObj = CreateUI("DialogPanel", howToPlayObj.transform);
            RectTransform htpPanelRect = htpPanelObj.GetComponent<RectTransform>();
            htpPanelRect.anchorMin = new Vector2(0.04f, 0.04f);
            htpPanelRect.anchorMax = new Vector2(0.96f, 0.96f);
            htpPanelRect.offsetMin = Vector2.zero;
            htpPanelRect.offsetMax = Vector2.zero;

            Image htpPanelImg = htpPanelObj.AddComponent<Image>();
            htpPanelImg.sprite = Art("10_board_panel_blank.png", CyberScratchArt.RowBorderWinSprite);
            htpPanelImg.type = Image.Type.Sliced;
            htpPanelImg.pixelsPerUnitMultiplier = 2.5f;
            htpPanelImg.color = Color.white;

            GameObject htpTitleObj = CreateUI("Title", htpPanelObj.transform);
            Text htpTitle = htpTitleObj.AddComponent<Text>();
            htpTitle.font = defaultFont;
            htpTitle.text = "HOW TO PLAY";
            htpTitle.fontSize = 42;
            htpTitle.resizeTextForBestFit = true;
            htpTitle.resizeTextMinSize = 30;
            htpTitle.resizeTextMaxSize = 42;
            htpTitle.fontStyle = FontStyle.Bold;
            htpTitle.alignment = TextAnchor.MiddleCenter;
            htpTitle.color = new Color(1.0f, 0.85f, 0.2f);
            var htpTitleShadow = htpTitleObj.AddComponent<Shadow>();
            htpTitleShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            htpTitleShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform htpTitleRect = htpTitleObj.GetComponent<RectTransform>();
            htpTitleRect.anchorMin = new Vector2(0.12f, 0.865f);
            htpTitleRect.anchorMax = new Vector2(0.88f, 0.920f);
            htpTitleRect.offsetMin = Vector2.zero;
            htpTitleRect.offsetMax = Vector2.zero;

            GameObject htpDivObj = CreateUI("Divider", htpPanelObj.transform);
            Image htpDivImg = htpDivObj.AddComponent<Image>();
            htpDivImg.sprite = Art("18_horizontal_divider_cyan.png", null);
            htpDivImg.type = Image.Type.Sliced;
            htpDivImg.color = Color.white;
            RectTransform htpDivRect = htpDivObj.GetComponent<RectTransform>();
            htpDivRect.anchorMin = new Vector2(0.15f, 0.840f);
            htpDivRect.anchorMax = new Vector2(0.85f, 0.850f);
            htpDivRect.offsetMin = Vector2.zero;
            htpDivRect.offsetMax = Vector2.zero;

            GameObject htpRulesObj = CreateUI("RulesText", htpPanelObj.transform);
            Text htpRules = htpRulesObj.AddComponent<Text>();
            htpRules.font = defaultFont;
            htpRules.text =
                "<size=30><b><color=#00FFAA>1. HOW TO WIN:</color></b></size>\n" +
                "Uncover 3 identical symbols across any row to win that row's cash prize!\n\n" +
                "<size=30><b><color=#00FFAA>2. MULTI-ROW PAYOUTS:</color></b></size>\n" +
                "All 5 rows evaluate independently. Win multiple rows simultaneously!\n\n" +
                "<size=30><b><color=#00FFAA>3. NEAR-MISS PULSE:</color></b></size>\n" +
                "Match the first 2 cells to ignite an anticipation glow on the 3rd cell!\n\n" +
                "<size=30><b><color=#00FFAA>4. QUICK REVEAL:</color></b></size>\n" +
                "Tap [REVEAL ALL] anytime to scratch all 5 rows instantly.\n\n" +
                "<size=30><b><color=#FFD700>\u25C6 SYMBOL PAYTABLE:</color></b></size>\n" +
                "  \u2022 Bronze Gear: 1x (35%)       \u2022 Plasma Gem: 15x (1.5%)\n" +
                "  \u2022 Silver Ingot: 2x (22%)      \u2022 Cyber Crown: 50x (1.0%)\n" +
                "  \u2022 Gold Core: 5x (12%)         \u2022 Lucky 777: 250x JACKPOT (0.1%)";
            htpRules.fontSize = 22;
            htpRules.lineSpacing = 1.14f;
            htpRules.resizeTextForBestFit = true;
            htpRules.resizeTextMinSize = 20;
            htpRules.resizeTextMaxSize = 28;
            htpRules.alignment = TextAnchor.UpperLeft;
            htpRules.color = new Color(0.90f, 0.95f, 1.0f);
            RectTransform htpRulesRect = htpRulesObj.GetComponent<RectTransform>();
            htpRulesRect.anchorMin = new Vector2(0.10f, 0.175f);
            htpRulesRect.anchorMax = new Vector2(0.90f, 0.825f);
            htpRulesRect.offsetMin = Vector2.zero;
            htpRulesRect.offsetMax = Vector2.zero;

            GameObject htpCloseObj = CreateUI("CloseButton", htpPanelObj.transform);
            RectTransform htpCloseRect = htpCloseObj.GetComponent<RectTransform>();
            htpCloseRect.anchorMin = new Vector2(0.24f, 0.080f);
            htpCloseRect.anchorMax = new Vector2(0.76f, 0.155f);
            htpCloseRect.offsetMin = Vector2.zero;
            htpCloseRect.offsetMax = Vector2.zero;

            Image htpCloseImg = htpCloseObj.AddComponent<Image>();
            htpCloseImg.sprite = Art("15_new_card_button_green_blank.png", CyberScratchArt.ButtonEmeraldSprite);
            htpCloseImg.type = Image.Type.Simple;
            htpCloseImg.preserveAspect = true;
            Button htpCloseBtn = htpCloseObj.AddComponent<Button>();

            GameObject htpCloseTxtObj = CreateUI("Text", htpCloseObj.transform);
            Text htpCloseTxt = htpCloseTxtObj.AddComponent<Text>();
            htpCloseTxt.font = defaultFont;
            htpCloseTxt.text = "GOT IT / CLOSE";
            htpCloseTxt.fontSize = 28;
            htpCloseTxt.fontStyle = FontStyle.Bold;
            htpCloseTxt.alignment = TextAnchor.MiddleCenter;
            htpCloseTxt.color = Color.white;
            htpCloseTxt.resizeTextForBestFit = true;
            htpCloseTxt.resizeTextMinSize = 20;
            htpCloseTxt.resizeTextMaxSize = 28;
            var htpCloseShadow = htpCloseTxtObj.AddComponent<Shadow>();
            htpCloseShadow.effectColor = new Color(0f, 0.25f, 0.08f, 0.85f);
            htpCloseShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform htpCloseTxtRect = htpCloseTxtObj.GetComponent<RectTransform>();
            Stretch(htpCloseTxtRect);
            htpCloseTxtRect.offsetMin = new Vector2(16, 8);
            htpCloseTxtRect.offsetMax = new Vector2(-16, -8);

            HowToPlayModalView howToPlayModal = howToPlayObj.AddComponent<HowToPlayModalView>();
            SetField(howToPlayModal, "_canvasGroup", htpCg);
            SetField(howToPlayModal, "_dialogPanel", htpPanelRect);
            SetField(howToPlayModal, "_closeButton", htpCloseBtn);
            howToPlayModal.HideInstant();

            return howToPlayModal;
        }

        public static WinModalView BuildWinModal(Transform parent, Font defaultFont)
        {
            GameObject modalObj = CreateUI("WinModalDialog", parent);
            CanvasGroup modalCg = modalObj.AddComponent<CanvasGroup>();
            Stretch(modalObj.GetComponent<RectTransform>());

            Image modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0, 0, 0, 0.85f);

            GameObject panelObj = CreateUI("DialogPanel", modalObj.transform);
            RectTransform pRect = panelObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.06f, 0.15f);
            pRect.anchorMax = new Vector2(0.94f, 0.85f);
            pRect.offsetMin = Vector2.zero;
            pRect.offsetMax = Vector2.zero;

            Image pImg = panelObj.AddComponent<Image>();
            pImg.sprite = Art("10_board_panel_blank.png", CyberScratchArt.RowBorderWinSprite);
            pImg.type = Image.Type.Sliced;
            pImg.color = Color.white;
            pImg.pixelsPerUnitMultiplier = 2.5f;

            GameObject mTitleObj = CreateUI("Title", panelObj.transform);
            Text mTitle = mTitleObj.AddComponent<Text>();
            mTitle.font = defaultFont;
            mTitle.text = "CYBER VICTORY!";
            mTitle.fontSize = 46;
            mTitle.resizeTextForBestFit = true;
            mTitle.resizeTextMinSize = 32;
            mTitle.resizeTextMaxSize = 46;
            mTitle.fontStyle = FontStyle.Bold;
            mTitle.alignment = TextAnchor.MiddleCenter;
            mTitle.color = new Color(1.0f, 0.85f, 0.2f);
            var mTitleShadow = mTitleObj.AddComponent<Shadow>();
            mTitleShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            mTitleShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform mtRect = mTitleObj.GetComponent<RectTransform>();
            mtRect.anchorMin = new Vector2(0.12f, 0.830f);
            mtRect.anchorMax = new Vector2(0.88f, 0.895f);
            mtRect.offsetMin = Vector2.zero;
            mtRect.offsetMax = Vector2.zero;

            GameObject winDivObj = CreateUI("Divider", panelObj.transform);
            Image winDivImg = winDivObj.AddComponent<Image>();
            winDivImg.sprite = Art("18_horizontal_divider_cyan.png", null);
            winDivImg.type = Image.Type.Sliced;
            winDivImg.color = Color.white;
            RectTransform winDivRect = winDivObj.GetComponent<RectTransform>();
            winDivRect.anchorMin = new Vector2(0.15f, 0.805f);
            winDivRect.anchorMax = new Vector2(0.85f, 0.815f);
            winDivRect.offsetMin = Vector2.zero;
            winDivRect.offsetMax = Vector2.zero;

            GameObject rWonObj = CreateUI("RowsWon", panelObj.transform);
            Text rWonText = rWonObj.AddComponent<Text>();
            rWonText.font = defaultFont;
            rWonText.text = "3 WINNING ROWS!";
            rWonText.fontSize = 32;
            rWonText.resizeTextForBestFit = true;
            rWonText.resizeTextMinSize = 24;
            rWonText.resizeTextMaxSize = 32;
            rWonText.fontStyle = FontStyle.Bold;
            rWonText.alignment = TextAnchor.MiddleCenter;
            rWonText.color = new Color(0.2f, 0.88f, 1.0f);
            var rWonShadow = rWonObj.AddComponent<Shadow>();
            rWonShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            rWonShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform rwRect = rWonObj.GetComponent<RectTransform>();
            rwRect.anchorMin = new Vector2(0.12f, 0.690f);
            rwRect.anchorMax = new Vector2(0.88f, 0.770f);
            rwRect.offsetMin = Vector2.zero;
            rwRect.offsetMax = Vector2.zero;

            GameObject amtObj = CreateUI("WinAmount", panelObj.transform);
            Text amtText = amtObj.AddComponent<Text>();
            amtText.font = defaultFont;
            amtText.text = "$150.00";
            amtText.fontSize = 76;
            amtText.resizeTextForBestFit = true;
            amtText.resizeTextMinSize = 40;
            amtText.resizeTextMaxSize = 76;
            amtText.fontStyle = FontStyle.Bold;
            amtText.alignment = TextAnchor.MiddleCenter;
            amtText.color = new Color(0.1f, 0.96f, 0.45f);
            var amtShadow = amtObj.AddComponent<Shadow>();
            amtShadow.effectColor = new Color(0f, 0.20f, 0.08f, 0.95f);
            amtShadow.effectDistance = new Vector2(2f, -2f);
            RectTransform amtRect = amtObj.GetComponent<RectTransform>();
            amtRect.anchorMin = new Vector2(0.10f, 0.350f);
            amtRect.anchorMax = new Vector2(0.90f, 0.640f);
            amtRect.offsetMin = Vector2.zero;
            amtRect.offsetMax = Vector2.zero;

            GameObject cBtnObj = CreateUI("CollectBtn", panelObj.transform);
            RectTransform cbRect = cBtnObj.GetComponent<RectTransform>();
            cbRect.anchorMin = new Vector2(0.20f, 0.090f);
            cbRect.anchorMax = new Vector2(0.80f, 0.190f);
            cbRect.offsetMin = Vector2.zero;
            cbRect.offsetMax = Vector2.zero;

            Image cbImg = cBtnObj.AddComponent<Image>();
            cbImg.sprite = Art("15_new_card_button_green_blank.png", CyberScratchArt.ButtonEmeraldSprite);
            cbImg.type = Image.Type.Simple;
            cbImg.preserveAspect = true;
            Button colBtn = cBtnObj.AddComponent<Button>();

            GameObject colTxtObj = CreateUI("Text", cBtnObj.transform);
            Text colText = colTxtObj.AddComponent<Text>();
            colText.font = defaultFont;
            colText.text = "COLLECT & PLAY AGAIN";
            colText.fontSize = 28;
            colText.fontStyle = FontStyle.Bold;
            colText.alignment = TextAnchor.MiddleCenter;
            colText.color = Color.white;
            colText.resizeTextForBestFit = true;
            colText.resizeTextMinSize = 20;
            colText.resizeTextMaxSize = 28;
            var colShadow = colTxtObj.AddComponent<Shadow>();
            colShadow.effectColor = new Color(0f, 0.25f, 0.08f, 0.85f);
            colShadow.effectDistance = new Vector2(1.5f, -1.5f);
            RectTransform colTxtRect = colTxtObj.GetComponent<RectTransform>();
            Stretch(colTxtRect);
            colTxtRect.offsetMin = new Vector2(16, 8);
            colTxtRect.offsetMax = new Vector2(-16, -8);

            WinModalView winModal = modalObj.AddComponent<WinModalView>();
            SetField(winModal, "_canvasGroup", modalCg);
            SetField(winModal, "_dialogPanel", pRect);
            SetField(winModal, "_titleText", mTitle);
            SetField(winModal, "_rowsWonText", rWonText);
            SetField(winModal, "_totalWinAmountText", amtText);
            SetField(winModal, "_collectButton", colBtn);
            winModal.HideInstant();

            return winModal;
        }

        private static GameObject CreateUI(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            target.GetType().GetField(fieldName, flags)?.SetValue(target, value);
        }

        private static void ConfigureParticleSystem(ParticleSystem ps, Sprite particleSprite)
        {
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = 80f;
            main.startSize = 35f;
            main.startColor = new Color(0.2f, 0.85f, 1.0f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 20f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (particleSprite != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default"));
                m.mainTexture = particleSprite.texture;
                renderer.material = m;
            }
        }
    }
}
