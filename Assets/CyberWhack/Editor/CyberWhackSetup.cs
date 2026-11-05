using System;
using System.IO;
using CyberWhack.Art;
using CyberWhack.Audio;
using CyberWhack.Data;
using CyberWhack.Gameplay;
using CyberWhack.UI;
using CyberWhack.VFX;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CyberWhack.Editor
{
    public static class CyberWhackSetup
    {
        private const string ArtDir = "Assets/CyberWhack/Art";
        private const string ScenePath = "Assets/CyberWhack/CyberWhackScene.unity";
        private const string ConfigPath = "Assets/CyberWhack/GameConfig.asset";

        [MenuItem("CyberWhack/1. Generate Sprites and Art Assets")]
        public static void GenerateSprites()
        {
            if (!Directory.Exists(ArtDir))
            {
                Directory.CreateDirectory(ArtDir);
            }

            if (!File.Exists($"{ArtDir}/HatchRing.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonDirtMoundSprite(320, 180), $"{ArtDir}/HatchRing.png");
            if (!File.Exists($"{ArtDir}/StandardCyberMole.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Normal), $"{ArtDir}/StandardCyberMole.png");
            if (!File.Exists($"{ArtDir}/OverclockedGoldMole.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Golden), $"{ArtDir}/OverclockedGoldMole.png");
            if (!File.Exists($"{ArtDir}/ArmoredTankMole.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.Helmet), $"{ArtDir}/ArmoredTankMole.png");
            if (!File.Exists($"{ArtDir}/ArmoredTankMole_Cracked.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonMoleSprite(256, CartoonMoleVariant.HelmetCracked), $"{ArtDir}/ArmoredTankMole_Cracked.png");
            if (!File.Exists($"{ArtDir}/EmpDecoyMine.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonBombSprite(256), $"{ArtDir}/EmpDecoyMine.png");
            if (!File.Exists($"{ArtDir}/ShockwaveRing.png"))
                SaveSprite(CartoonAtlasGenerator.CreateCartoonStarburstSprite(256), $"{ArtDir}/ShockwaveRing.png");

            AssetDatabase.Refresh();

            string[] spriteFiles = new string[]
            {
                "HatchRing.png", "StandardCyberMole.png", "OverclockedGoldMole.png",
                "ArmoredTankMole.png", "ArmoredTankMole_Cracked.png", "EmpDecoyMine.png",
                "ShockwaveRing.png", "CartoonStatPlaque.png", "CartoonComboTrack.png",
                "CartoonComboFill.png", "CartoonBadge_Multiplier.png", "CartoonIcon_Clock.png",
                "CartoonIcon_StarCoin.png", "CartoonBtn_Exit.png", "CartoonBtn_AudioOn.png",
                "CartoonBtn_AudioOff.png", "CartoonParchmentModal.png", "CartoonParchmentRow.png",
                "CartoonRibbon_Header.png", "CartoonBtn_Gold.png", "CartoonBtn_Green.png",
                "CartoonBtn_Wood.png", "CartoonCountdownBurst.png", "WhackTitle.png",
                "CartoonBackground.png", "PanelFrame.png", "IntelCardFrame.png",
                "CyberButton.png", "CyberButton_Amber.png", "SoundIcon_On.png", "SoundIcon_Off.png"
            };

            foreach (string file in spriteFiles)
            {
                string path = $"{ArtDir}/{file}";
                if (File.Exists(path))
                {
                    ConfigureImporter(path);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=#00e5ff>[CyberWhack]</color> Art assets and sprite atlas successfully verified!");
        }

        private static void SaveSprite(Sprite sprite, string path)
        {
            byte[] bytes = sprite.texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void ConfigureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        [MenuItem("CyberWhack/2. Setup Complete Production Scene")]
        public static void SetupCompleteScene()
        {
            GenerateSprites();

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            Camera cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(237, 151, 62, 255); // Warm desert sand #ED973E
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            camObj.transform.position = new Vector3(0, 0, -10);

            // Event System
            GameObject eventSysObj = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // Load Sprites
            Sprite hatchSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/HatchRing.png");
            Sprite standardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/StandardCyberMole.png");
            Sprite goldSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/OverclockedGoldMole.png");
            Sprite armoredSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/ArmoredTankMole.png");
            Sprite armoredCrackedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/ArmoredTankMole_Cracked.png");
            Sprite empSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/EmpDecoyMine.png");
            Sprite shockwaveSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/ShockwaveRing.png");
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBackground.png");
            Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/WhackTitle.png");

            // Cartoon UI Sprites
            Sprite statPlaqueSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonStatPlaque.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/PanelFrame.png");
            Sprite comboTrackSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonComboTrack.png") ?? statPlaqueSprite;
            Sprite comboFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonComboFill.png") ?? statPlaqueSprite;
            Sprite multBadgeSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBadge_Multiplier.png") ?? statPlaqueSprite;
            Sprite clockIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonIcon_Clock.png");
            Sprite coinIconSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonIcon_StarCoin.png");
            Sprite exitBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_Pause.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_Exit.png") ?? statPlaqueSprite;
            Sprite soundOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_AudioOn.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/SoundIcon_On.png");
            Sprite soundOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_AudioOff.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/SoundIcon_Off.png");
            Sprite modalBoardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonParchmentModal.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/IntelCardFrame.png");
            Sprite parchmentRowSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonParchmentRow.png") ?? statPlaqueSprite;
            Sprite ribbonHeaderSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonRibbon_Header.png") ?? statPlaqueSprite;
            Sprite btnGoldSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_Gold.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CyberButton.png");
            Sprite btnGreenSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_Green.png") ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CyberButton_Amber.png");
            Sprite btnWoodSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonBtn_Wood.png") ?? statPlaqueSprite;
            Sprite countdownBurstSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/CartoonCountdownBurst.png") ?? shockwaveSprite;

            // Canvas
            GameObject canvasObj = new GameObject("CyberCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Strict 9:16 portrait
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // Width-Lock

            // Fullscreen Background
            if (bgSprite != null)
            {
                GameObject bgObj = CreatePanel(canvasObj, "FullscreenBackground", Vector2.zero, Vector2.one, bgSprite);
                bgObj.transform.SetAsFirstSibling();
                Image bgImg = bgObj.GetComponent<Image>();
                bgImg.raycastTarget = false;
            }

            // Safe Area Container
            GameObject safeAreaObj = new GameObject("SafeAreaContainer", typeof(RectTransform), typeof(SafeAreaFitter));
            safeAreaObj.transform.SetParent(canvasObj.transform, false);
            RectTransform safeRt = safeAreaObj.GetComponent<RectTransform>();
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;

            // 3x3 Grid Field Container
            GameObject gridContainer = new GameObject("HatchGridContainer", typeof(RectTransform));
            gridContainer.transform.SetParent(safeAreaObj.transform, false);
            RectTransform gridRt = gridContainer.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.04f, 0.12f);
            gridRt.anchorMax = new Vector2(0.96f, 0.68f);
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;

            HatchNode[] nodes = new HatchNode[9];
            float[] colX = new float[] { 0.17f, 0.50f, 0.83f };
            float[] rowY = new float[] { 0.83f, 0.50f, 0.17f };

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int index = r * 3 + c;
                    GameObject nodeObj = new GameObject($"HatchNode_{index}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HatchNode));
                    nodeObj.transform.SetParent(gridContainer.transform, false);

                    RectTransform nodeRt = nodeObj.GetComponent<RectTransform>();
                    nodeRt.anchorMin = new Vector2(colX[c], rowY[r]);
                    nodeRt.anchorMax = new Vector2(colX[c], rowY[r]);
                    nodeRt.sizeDelta = new Vector2(300, 300);

                    Image ringImg = nodeObj.GetComponent<Image>();
                    ringImg.sprite = hatchSprite;
                    ringImg.raycastTarget = true;

                    GameObject maskObj = new GameObject("MaskContainer", typeof(RectTransform), typeof(RectMask2D));
                    maskObj.transform.SetParent(nodeObj.transform, false);
                    RectTransform maskRt = maskObj.GetComponent<RectTransform>();
                    maskRt.anchorMin = new Vector2(0.12f, 0.18f);
                    maskRt.anchorMax = new Vector2(0.88f, 0.95f);
                    maskRt.offsetMin = Vector2.zero;
                    maskRt.offsetMax = Vector2.zero;

                    GameObject targetObj = new GameObject("TargetSprite", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    targetObj.transform.SetParent(maskObj.transform, false);
                    RectTransform targetRt = targetObj.GetComponent<RectTransform>();
                    targetRt.anchorMin = new Vector2(0.5f, 0f);
                    targetRt.anchorMax = new Vector2(0.5f, 0f);
                    targetRt.sizeDelta = new Vector2(230, 230);
                    targetRt.anchoredPosition = new Vector2(0, -140);

                    Image targetImg = targetObj.GetComponent<Image>();
                    targetImg.sprite = standardSprite;
                    targetImg.raycastTarget = false;

                    HatchNode node = nodeObj.GetComponent<HatchNode>();
                    node.ConfigureSprites(standardSprite, goldSprite, armoredSprite, armoredCrackedSprite, empSprite);
                    node.SetNodeIndex(index);

                    SerializedObject nodeSO = new SerializedObject(node);
                    nodeSO.FindProperty("nodeIndex").intValue = index;
                    nodeSO.FindProperty("maskedContainer").objectReferenceValue = maskRt;
                    nodeSO.FindProperty("targetTransform").objectReferenceValue = targetRt;
                    nodeSO.FindProperty("targetImage").objectReferenceValue = targetImg;
                    nodeSO.FindProperty("standardMoleSprite").objectReferenceValue = standardSprite;
                    nodeSO.FindProperty("goldMoleSprite").objectReferenceValue = goldSprite;
                    nodeSO.FindProperty("armoredMoleSprite").objectReferenceValue = armoredSprite;
                    nodeSO.FindProperty("armoredCrackedSprite").objectReferenceValue = armoredCrackedSprite;
                    nodeSO.FindProperty("empDecoySprite").objectReferenceValue = empSprite;
                    nodeSO.ApplyModifiedProperties();

                    nodes[index] = node;
                }
            }

            // Warm Cartoon Palette Matching Reference Game
            Color32 darkChocolate = new Color32(58, 30, 12, 255);
            Color32 warmCaramel = new Color32(109, 58, 22, 255);
            Color32 goldenText = new Color32(255, 224, 102, 255);
            Color32 goldenSub = new Color32(255, 238, 185, 255);
            Color32 hazardRed = new Color32(235, 55, 55, 255);

            // ==========================================
            // In-Game Top Header (Polished Cartoon Layout)
            // ==========================================
            GameObject headerObj = new GameObject("InGameHeader", typeof(RectTransform));
            headerObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 0.77f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;

            // 1. Top Utility Row (Exit, Seed Badge, In-game Audio)
            GameObject abortBtnObj = CreatePanel(headerObj, "AbortBtn", new Vector2(0.04f, 0.81f), new Vector2(0.20f, 0.98f), exitBtnSprite);
            Button abortBtn = abortBtnObj.AddComponent<Button>();
            TextMeshProUGUI abortTxt = CreateText(abortBtnObj, "Txt", "EXIT", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            abortTxt.rectTransform.anchorMin = Vector2.zero;
            abortTxt.rectTransform.anchorMax = Vector2.one;

            GameObject seedBadgeObj = CreatePanel(headerObj, "SeedBadge", new Vector2(0.23f, 0.83f), new Vector2(0.77f, 0.97f), parchmentRowSprite);
            TextMeshProUGUI igSeed = CreateText(seedBadgeObj, "InGameSeed", "SEED #429182", 18, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            igSeed.rectTransform.anchorMin = Vector2.zero;
            igSeed.rectTransform.anchorMax = Vector2.one;

            GameObject igAudioBtnObj = CreatePanel(headerObj, "InGameAudioToggle", new Vector2(0.80f, 0.81f), new Vector2(0.96f, 0.98f), soundOnSprite);
            Button igAudioBtn = igAudioBtnObj.AddComponent<Button>();
            Image igAudioIcon = igAudioBtnObj.GetComponent<Image>();
            igAudioIcon.preserveAspect = true;
            igAudioIcon.raycastTarget = true;

            // 2. Middle Row: Timer Plaque (Left) & Score Plaque (Right)
            GameObject timerBox = CreatePanel(headerObj, "TimerBox", new Vector2(0.04f, 0.38f), new Vector2(0.48f, 0.78f), statPlaqueSprite);
            if (clockIconSprite != null)
            {
                GameObject clockIconObj = CreatePanel(timerBox, "ClockIcon", new Vector2(0.05f, 0.12f), new Vector2(0.32f, 0.88f), clockIconSprite);
                clockIconObj.GetComponent<Image>().preserveAspect = true;
            }
            TextMeshProUGUI timerTitle = CreateText(timerBox, "TimerTitle", "TIME", 17, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Left);
            timerTitle.rectTransform.anchorMin = new Vector2(0.34f, 0.60f);
            timerTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);

            TextMeshProUGUI timerText = CreateText(timerBox, "TimerDigits", "00:60", 42, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Left);
            timerText.rectTransform.anchorMin = new Vector2(0.34f, 0.08f);
            timerText.rectTransform.anchorMax = new Vector2(0.95f, 0.65f);

            GameObject scoreBox = CreatePanel(headerObj, "ScoreBox", new Vector2(0.52f, 0.38f), new Vector2(0.96f, 0.78f), parchmentRowSprite);
            if (coinIconSprite != null)
            {
                GameObject coinIconObj = CreatePanel(scoreBox, "CoinIcon", new Vector2(0.05f, 0.12f), new Vector2(0.32f, 0.88f), coinIconSprite);
                coinIconObj.GetComponent<Image>().preserveAspect = true;
            }
            TextMeshProUGUI scoreTitle = CreateText(scoreBox, "ScoreTitle", "SCORE", 17, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Left);
            scoreTitle.rectTransform.anchorMin = new Vector2(0.34f, 0.60f);
            scoreTitle.rectTransform.anchorMax = new Vector2(0.95f, 0.92f);

            TextMeshProUGUI scoreText = CreateText(scoreBox, "ScoreDigits", "0", 42, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Left);
            scoreText.rectTransform.anchorMin = new Vector2(0.34f, 0.08f);
            scoreText.rectTransform.anchorMax = new Vector2(0.95f, 0.65f);

            // 3. Bottom Row: Juicy Combo & Multiplier Bar
            GameObject comboPanel = new GameObject("ComboPanel", typeof(RectTransform));
            comboPanel.transform.SetParent(headerObj.transform, false);
            RectTransform cpRt = comboPanel.GetComponent<RectTransform>();
            cpRt.anchorMin = new Vector2(0.04f, 0.02f);
            cpRt.anchorMax = new Vector2(0.96f, 0.34f);
            cpRt.offsetMin = Vector2.zero;
            cpRt.offsetMax = Vector2.zero;

            // Multiplier Starburst Badge on Left
            GameObject badgeObj = CreatePanel(comboPanel, "MultiplierBadge", new Vector2(0.00f, 0.00f), new Vector2(0.20f, 1.00f), multBadgeSprite);
            RectTransform badgeRt = badgeObj.GetComponent<RectTransform>();
            Image badgeImg = badgeObj.GetComponent<Image>();
            badgeImg.preserveAspect = true;

            TextMeshProUGUI multText = CreateText(badgeObj, "MultiplierText", "1x", 30, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            multText.rectTransform.anchorMin = Vector2.zero;
            multText.rectTransform.anchorMax = Vector2.one;

            // Sunken Combo Track
            GameObject comboTrackObj = CreatePanel(comboPanel, "ComboTrack", new Vector2(0.22f, 0.16f), new Vector2(0.99f, 0.84f), comboTrackSprite);
            Image trackImg = comboTrackObj.GetComponent<Image>();
            trackImg.type = Image.Type.Sliced;

            // Gradient Energy Fill Bar
            GameObject comboFillObj = CreatePanel(comboTrackObj, "ComboFill", Vector2.zero, Vector2.one, comboFillSprite);
            Image comboFillImg = comboFillObj.GetComponent<Image>();
            comboFillImg.type = Image.Type.Filled;
            comboFillImg.fillMethod = Image.FillMethod.Horizontal;
            comboFillImg.fillOrigin = 0;
            comboFillImg.fillAmount = 0f;

            // Streak Counter Text on Bar
            TextMeshProUGUI streakText = CreateText(comboTrackObj, "StreakText", "WHACK TO COMBO!", 20, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            streakText.rectTransform.anchorMin = Vector2.zero;
            streakText.rectTransform.anchorMax = Vector2.one;

            headerObj.SetActive(false);

            // ==========================================
            // Main Menu Panel (Warm Cartoon Arcade Style)
            // ==========================================
            GameObject menuObj = new GameObject("MainMenuPanel", typeof(RectTransform));
            menuObj.transform.SetParent(safeAreaObj.transform, false);
            RectTransform menuRt = menuObj.GetComponent<RectTransform>();
            menuRt.anchorMin = Vector2.zero;
            menuRt.anchorMax = Vector2.one;
            menuRt.offsetMin = Vector2.zero;
            menuRt.offsetMax = Vector2.zero;

            // Top Right Settings Audio Button
            GameObject audioBtnObj = CreatePanel(menuObj, "AudioToggleBtn", new Vector2(0.83f, 0.90f), new Vector2(0.96f, 0.97f), soundOnSprite);
            Button audioToggleBtn = audioBtnObj.AddComponent<Button>();
            Image audioIcon = audioBtnObj.GetComponent<Image>();
            audioIcon.preserveAspect = true;
            audioIcon.raycastTarget = true;

            // 3D Cartoon Wooden Title Banner: "WHACK-A-MOLE"
            GameObject titleObj = CreatePanel(menuObj, "HeroTitleBanner", new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.88f), titleSprite);
            Image titleImg = titleObj.GetComponent<Image>();
            titleImg.preserveAspect = true;
            titleImg.raycastTarget = false;

            // Reference Showcase Row: 3 Preview Mounds
            GameObject previewRow = new GameObject("PreviewShowcaseRow", typeof(RectTransform));
            previewRow.transform.SetParent(menuObj.transform, false);
            RectTransform prRt = previewRow.GetComponent<RectTransform>();
            prRt.anchorMin = new Vector2(0.04f, 0.48f);
            prRt.anchorMax = new Vector2(0.96f, 0.70f);
            prRt.offsetMin = Vector2.zero;
            prRt.offsetMax = Vector2.zero;

            CreateEditorPreviewMound(previewRow, "LeftPreviewMound", new Vector2(0.02f, 0.05f), new Vector2(0.33f, 0.95f), hatchSprite, standardSprite, -10f, 0.95f);
            CreateEditorPreviewMound(previewRow, "CenterPreviewMound", new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.95f), hatchSprite, goldSprite, -10f, 1.0f);
            CreateEditorPreviewMound(previewRow, "RightPreviewMound", new Vector2(0.67f, 0.05f), new Vector2(0.98f, 0.95f), hatchSprite, empSprite, 10f, 1.0f);

            // Glossy Golden Yellow Pill "Start" Button
            GameObject startBtnObj = CreatePanel(menuObj, "StartButton", new Vector2(0.20f, 0.35f), new Vector2(0.80f, 0.46f), btnGoldSprite);
            Button startBtn = startBtnObj.AddComponent<Button>();
            Image startBtnImg = startBtnObj.GetComponent<Image>();
            startBtnImg.preserveAspect = true;
            TextMeshProUGUI startTxt = CreateText(startBtnObj, "Txt", "START GAME", 46, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            startTxt.rectTransform.anchorMin = Vector2.zero;
            startTxt.rectTransform.anchorMax = Vector2.one;

            // Tournament High Score Card
            GameObject recordBox = CreatePanel(menuObj, "RecordCard", new Vector2(0.10f, 0.25f), new Vector2(0.90f, 0.33f), statPlaqueSprite);
            TextMeshProUGUI recordText = CreateText(recordBox, "RecordText", "TOURNAMENT RECORD: 0 PTS", 24, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            recordText.rectTransform.anchorMin = Vector2.zero;
            recordText.rectTransform.anchorMax = Vector2.one;

            // Seed Selector & Rules Row
            GameObject seedBox = CreatePanel(menuObj, "SeedBox", new Vector2(0.10f, 0.16f), new Vector2(0.62f, 0.23f), parchmentRowSprite);
            TextMeshProUGUI seedLbl = CreateText(seedBox, "Lbl", "SEED:", 17, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Left);
            seedLbl.rectTransform.anchorMin = new Vector2(0.06f, 0f);
            seedLbl.rectTransform.anchorMax = new Vector2(0.30f, 1f);

            GameObject inputObj = new GameObject("SeedInput", typeof(RectTransform), typeof(TMP_InputField));
            inputObj.transform.SetParent(seedBox.transform, false);
            RectTransform inRt = inputObj.GetComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.30f, 0.15f);
            inRt.anchorMax = new Vector2(0.68f, 0.85f);
            inRt.offsetMin = Vector2.zero;
            inRt.offsetMax = Vector2.zero;
            TMP_InputField seedInput = inputObj.GetComponent<TMP_InputField>();

            GameObject inTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            inTxtObj.transform.SetParent(inputObj.transform, false);
            RectTransform inTxtRt = inTxtObj.GetComponent<RectTransform>();
            inTxtRt.anchorMin = Vector2.zero;
            inTxtRt.anchorMax = Vector2.one;
            inTxtRt.offsetMin = Vector2.zero;
            inTxtRt.offsetMax = Vector2.zero;
            TextMeshProUGUI inTmp = inTxtObj.GetComponent<TextMeshProUGUI>();
            inTmp.fontSize = 22;
            inTmp.color = darkChocolate;
            inTmp.alignment = TextAlignmentOptions.MidlineLeft;
            seedInput.textComponent = inTmp;
            seedInput.text = "429182";

            GameObject rndBtnObj = CreatePanel(seedBox, "RndBtn", new Vector2(0.70f, 0.12f), new Vector2(0.96f, 0.88f), btnWoodSprite);
            Button rndBtn = rndBtnObj.AddComponent<Button>();
            TextMeshProUGUI rndTxt = CreateText(rndBtnObj, "Txt", "RND", 18, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            rndTxt.rectTransform.anchorMin = Vector2.zero;
            rndTxt.rectTransform.anchorMax = Vector2.one;

            GameObject intelBtnObj = CreatePanel(menuObj, "IntelButton", new Vector2(0.65f, 0.16f), new Vector2(0.90f, 0.23f), btnGreenSprite);
            Button intelBtn = intelBtnObj.AddComponent<Button>();
            TextMeshProUGUI intelTxt = CreateText(intelBtnObj, "Txt", "RULES", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            intelTxt.rectTransform.anchorMin = Vector2.zero;
            intelTxt.rectTransform.anchorMax = Vector2.one;

            // Footer info
            TextMeshProUGUI footerTxt = CreateText(menuObj, "Footer", "MOBILE ARCADE // DETERMINISTIC // 60 FPS", 16, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Center);
            footerTxt.rectTransform.anchorMin = new Vector2(0.05f, 0.08f);
            footerTxt.rectTransform.anchorMax = new Vector2(0.95f, 0.13f);

            // ==========================================
            // Target Intel Modal (Rules)
            // ==========================================
            GameObject intelModalObj = CreatePanel(safeAreaObj, "TargetIntelModal", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), modalBoardSprite);
            intelModalObj.SetActive(false);

            GameObject intelRibbonObj = CreatePanel(intelModalObj, "Ribbon", new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.98f), ribbonHeaderSprite);
            TextMeshProUGUI imTitle = CreateText(intelRibbonObj, "Title", "HOW TO PLAY & TARGETS", 26, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            imTitle.rectTransform.anchorMin = Vector2.zero;
            imTitle.rectTransform.anchorMax = Vector2.one;

            CreateEditorIntelRow(intelModalObj, "Row1", 0.72f, 0.87f, standardSprite, "STANDARD MOLE", "0.85s Active • 1 Tap Required\nCute subterranean burrower", "+100 PTS", goldenText, parchmentRowSprite);
            CreateEditorIntelRow(intelModalObj, "Row2", 0.55f, 0.70f, goldSprite, "GOLDEN MOLE", "0.50s Active • 1 Fast Tap\nRoyal golden crown bonus!", "+300 PTS", goldenText, parchmentRowSprite);
            CreateEditorIntelRow(intelModalObj, "Row3", 0.38f, 0.53f, armoredSprite, "HARD HAT MOLE", "1.10s Active • 2 Taps Required\nFirst cracks helmet, second whacks!", "+250 PTS", goldenText, parchmentRowSprite);
            CreateEditorIntelRow(intelModalObj, "Row4", 0.21f, 0.36f, empSprite, "CARTOON BOMB", "1.00s Active • DO NOT WHACK!\nExplodes and resets combo streak!", "DANGER!", hazardRed, parchmentRowSprite);

            GameObject comboRuleBox = CreatePanel(intelModalObj, "ComboRules", new Vector2(0.06f, 0.11f), new Vector2(0.94f, 0.19f), statPlaqueSprite);
            TextMeshProUGUI comboRules = CreateText(comboRuleBox, "Txt", "COMBO: 5 Streak = 2x  |  10 Streak = 3x  |  15+ Streak = 4x MAX!\nMissed Hole or Bomb Hit Resets Multiplier to 1x", 17, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            comboRules.rectTransform.anchorMin = Vector2.zero;
            comboRules.rectTransform.anchorMax = Vector2.one;

            GameObject closeIntelBtnObj = CreatePanel(intelModalObj, "CloseBtn", new Vector2(0.18f, 0.03f), new Vector2(0.82f, 0.095f), btnGreenSprite);
            Button closeIntelBtn = closeIntelBtnObj.AddComponent<Button>();
            TextMeshProUGUI closeTxt = CreateText(closeIntelBtnObj, "Txt", "GOT IT! RETURN TO GAME", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            closeTxt.rectTransform.anchorMin = Vector2.zero;
            closeTxt.rectTransform.anchorMax = Vector2.one;

            // ==========================================
            // Ready / Countdown Overlay
            // ==========================================
            GameObject readyOverlay = new GameObject("ReadyOverlay", typeof(RectTransform));
            readyOverlay.transform.SetParent(safeAreaObj.transform, false);
            RectTransform roRt = readyOverlay.GetComponent<RectTransform>();
            roRt.anchorMin = new Vector2(0.15f, 0.38f);
            roRt.anchorMax = new Vector2(0.85f, 0.62f);
            roRt.offsetMin = Vector2.zero;
            roRt.offsetMax = Vector2.zero;

            GameObject burstObj = CreatePanel(readyOverlay, "BurstImage", Vector2.zero, Vector2.one, countdownBurstSprite);
            RectTransform burstRt = burstObj.GetComponent<RectTransform>();
            Image burstImg = burstObj.GetComponent<Image>();
            burstImg.preserveAspect = true;

            TextMeshProUGUI readyCountdown = CreateText(readyOverlay, "CountdownText", "3", 88, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            readyCountdown.rectTransform.anchorMin = Vector2.zero;
            readyCountdown.rectTransform.anchorMax = Vector2.one;
            readyOverlay.SetActive(false);

            // ==========================================
            // Game Over Modal (Celebratory Cartoon Board)
            // ==========================================
            GameObject gameOverModal = CreatePanel(safeAreaObj, "GameOverModal", new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), modalBoardSprite);
            gameOverModal.SetActive(false);

            GameObject goRibbonObj = CreatePanel(gameOverModal, "Ribbon", new Vector2(0.08f, 0.89f), new Vector2(0.92f, 0.98f), ribbonHeaderSprite);
            TextMeshProUGUI goTitle = CreateText(goRibbonObj, "Title", "ROUND FINISHED!", 32, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            goTitle.rectTransform.anchorMin = Vector2.zero;
            goTitle.rectTransform.anchorMax = Vector2.one;

            TextMeshProUGUI rankBadge = CreateText(gameOverModal, "RankBadge", "MASTER MOLE WHACKER", 24, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Center);
            rankBadge.rectTransform.anchorMin = new Vector2(0, 0.81f);
            rankBadge.rectTransform.anchorMax = new Vector2(1, 0.88f);

            TextMeshProUGUI finalScore = CreateText(gameOverModal, "FinalScore", "0", 72, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            finalScore.rectTransform.anchorMin = new Vector2(0, 0.68f);
            finalScore.rectTransform.anchorMax = new Vector2(1, 0.81f);

            TextMeshProUGUI newRecord = CreateText(gameOverModal, "NewRecord", ">> NEW TOURNAMENT RECORD! <<", 22, FontStyles.Bold, hazardRed, TextAlignmentOptions.Center);
            newRecord.rectTransform.anchorMin = new Vector2(0, 0.63f);
            newRecord.rectTransform.anchorMax = new Vector2(1, 0.68f);

            TextMeshProUGUI highScore = CreateText(gameOverModal, "HighScore", "RECORD: 0 PTS", 22, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Center);
            highScore.rectTransform.anchorMin = new Vector2(0, 0.57f);
            highScore.rectTransform.anchorMax = new Vector2(1, 0.63f);

            GameObject statsCardObj = CreatePanel(gameOverModal, "StatsCard", new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.55f), parchmentRowSprite);
            TextMeshProUGUI maxStreakText = CreateText(statsCardObj, "MaxStreak", "MAX STREAK: 0", 22, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            maxStreakText.rectTransform.anchorMin = new Vector2(0, 0.66f);
            maxStreakText.rectTransform.anchorMax = new Vector2(1, 1f);

            TextMeshProUGUI accuracyText = CreateText(statsCardObj, "Accuracy", "ACCURACY: 100%", 22, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            accuracyText.rectTransform.anchorMin = new Vector2(0, 0.33f);
            accuracyText.rectTransform.anchorMax = new Vector2(1, 0.66f);

            TextMeshProUGUI statsText = CreateText(statsCardObj, "Stats", "WHACKED: 0   BOMBS: 0", 22, FontStyles.Bold, darkChocolate, TextAlignmentOptions.Center);
            statsText.rectTransform.anchorMin = new Vector2(0, 0f);
            statsText.rectTransform.anchorMax = new Vector2(1, 0.33f);

            TextMeshProUGUI finalSeed = CreateText(gameOverModal, "FinalSeed", "MATCH SEED: #429182", 20, FontStyles.Bold, warmCaramel, TextAlignmentOptions.Center);
            finalSeed.rectTransform.anchorMin = new Vector2(0, 0.27f);
            finalSeed.rectTransform.anchorMax = new Vector2(1, 0.32f);

            GameObject retrySameBtnObj = CreatePanel(gameOverModal, "RetrySameBtn", new Vector2(0.14f, 0.19f), new Vector2(0.86f, 0.26f), btnGoldSprite);
            Button retrySameBtn = retrySameBtnObj.AddComponent<Button>();
            TextMeshProUGUI r1Txt = CreateText(retrySameBtnObj, "Txt", "PLAY AGAIN (SAME SEED)", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            r1Txt.rectTransform.anchorMin = Vector2.zero;
            r1Txt.rectTransform.anchorMax = Vector2.one;

            GameObject retryNewBtnObj = CreatePanel(gameOverModal, "RetryNewBtn", new Vector2(0.14f, 0.11f), new Vector2(0.86f, 0.18f), btnGreenSprite);
            Button retryNewBtn = retryNewBtnObj.AddComponent<Button>();
            TextMeshProUGUI r2Txt = CreateText(retryNewBtnObj, "Txt", "NEW RANDOM MATCH", 24, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            r2Txt.rectTransform.anchorMin = Vector2.zero;
            r2Txt.rectTransform.anchorMax = Vector2.one;

            GameObject returnMenuBtnObj = CreatePanel(gameOverModal, "ReturnMenuBtn", new Vector2(0.14f, 0.03f), new Vector2(0.86f, 0.10f), btnWoodSprite);
            Button returnMenuBtn = returnMenuBtnObj.AddComponent<Button>();
            TextMeshProUGUI r3Txt = CreateText(returnMenuBtnObj, "Txt", "MAIN MENU", 22, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
            r3Txt.rectTransform.anchorMin = Vector2.zero;
            r3Txt.rectTransform.anchorMax = Vector2.one;

            // VFX Pool Manager
            GameObject effectPoolObj = new GameObject("EffectPoolManager", typeof(RectTransform), typeof(EffectPoolManager));
            effectPoolObj.transform.SetParent(canvasObj.transform, false);
            RectTransform effectRt = effectPoolObj.GetComponent<RectTransform>();
            effectRt.anchorMin = Vector2.zero;
            effectRt.anchorMax = Vector2.one;
            effectRt.offsetMin = Vector2.zero;
            effectRt.offsetMax = Vector2.zero;

            EffectPoolManager effectPool = effectPoolObj.GetComponent<EffectPoolManager>();
            SerializedObject effectSO = new SerializedObject(effectPool);
            effectSO.FindProperty("shockwaveSprite").objectReferenceValue = shockwaveSprite;
            effectSO.ApplyModifiedProperties();

            // Wire CyberHUD
            CyberHUD hud = canvasObj.AddComponent<CyberHUD>();
            SerializedObject hudSO = new SerializedObject(hud);
            hudSO.FindProperty("mainMenuPanel").objectReferenceValue = menuObj;
            hudSO.FindProperty("heroTitleTransform").objectReferenceValue = titleObj.GetComponent<RectTransform>();
            hudSO.FindProperty("menuHighScoreText").objectReferenceValue = recordText;
            hudSO.FindProperty("menuSeedInput").objectReferenceValue = seedInput;
            hudSO.FindProperty("menuStartButton").objectReferenceValue = startBtn;
            hudSO.FindProperty("menuIntelButton").objectReferenceValue = intelBtn;
            hudSO.FindProperty("menuRandomSeedButton").objectReferenceValue = rndBtn;
            hudSO.FindProperty("audioToggleButton").objectReferenceValue = audioToggleBtn;
            hudSO.FindProperty("audioToggleIcon").objectReferenceValue = audioIcon;
            hudSO.FindProperty("soundOnSprite").objectReferenceValue = soundOnSprite;
            hudSO.FindProperty("soundOffSprite").objectReferenceValue = soundOffSprite;

            hudSO.FindProperty("targetIntelModal").objectReferenceValue = intelModalObj;
            hudSO.FindProperty("closeIntelButton").objectReferenceValue = closeIntelBtn;

            hudSO.FindProperty("inGameHeader").objectReferenceValue = headerObj;
            hudSO.FindProperty("abortButton").objectReferenceValue = abortBtn;
            hudSO.FindProperty("inGameAudioToggleButton").objectReferenceValue = igAudioBtn;
            hudSO.FindProperty("inGameAudioToggleIcon").objectReferenceValue = igAudioIcon;
            hudSO.FindProperty("scoreText").objectReferenceValue = scoreText;
            hudSO.FindProperty("multiplierText").objectReferenceValue = multText;
            hudSO.FindProperty("comboStreakFill").objectReferenceValue = comboFillImg;
            hudSO.FindProperty("comboStreakCountText").objectReferenceValue = streakText;
            hudSO.FindProperty("multiplierBadgeTransform").objectReferenceValue = badgeRt;

            hudSO.FindProperty("timerText").objectReferenceValue = timerText;
            hudSO.FindProperty("inGameSeedText").objectReferenceValue = igSeed;

            hudSO.FindProperty("readyOverlay").objectReferenceValue = readyOverlay;
            hudSO.FindProperty("readyCountdownText").objectReferenceValue = readyCountdown;
            hudSO.FindProperty("countdownBurstTransform").objectReferenceValue = burstRt;

            hudSO.FindProperty("gameOverModal").objectReferenceValue = gameOverModal;
            hudSO.FindProperty("rankBadgeText").objectReferenceValue = rankBadge;
            hudSO.FindProperty("finalScoreText").objectReferenceValue = finalScore;
            hudSO.FindProperty("newRecordBadge").objectReferenceValue = newRecord;
            hudSO.FindProperty("highScoreText").objectReferenceValue = highScore;
            hudSO.FindProperty("maxStreakText").objectReferenceValue = maxStreakText;
            hudSO.FindProperty("accuracyText").objectReferenceValue = accuracyText;
            hudSO.FindProperty("destroyedStatsText").objectReferenceValue = statsText;
            hudSO.FindProperty("finalSeedText").objectReferenceValue = finalSeed;
            hudSO.FindProperty("restartSameSeedButton").objectReferenceValue = retrySameBtn;
            hudSO.FindProperty("restartNewSeedButton").objectReferenceValue = retryNewBtn;
            hudSO.FindProperty("returnToMenuButton").objectReferenceValue = returnMenuBtn;
            hudSO.ApplyModifiedProperties();

            // Audio Manager
            GameObject audioObj = new GameObject("CyberAudioManager", typeof(CyberAudioManager));

            // Game Director
            GameObject directorObj = new GameObject("CyberGameDirector", typeof(CyberGameDirector));
            CyberGameDirector director = directorObj.GetComponent<CyberGameDirector>();

            SerializedObject directorSO = new SerializedObject(director);
            directorSO.FindProperty("config").objectReferenceValue = config;
            directorSO.FindProperty("hud").objectReferenceValue = hud;

            SerializedProperty nodesProp = directorSO.FindProperty("hatchNodes");
            nodesProp.arraySize = 9;
            for (int i = 0; i < 9; i++)
            {
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];
            }
            directorSO.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            PlayerSettings.WebGL.template = "APPLICATION:CyberWhack";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            Debug.Log("<color=#00e5ff>[CyberWhack]</color> Complete production scene with polished Cartoon UI saved at: " + ScenePath);
        }

        [MenuItem("CyberWhack/3. Build WebGL Mobile Arcade")]
        public static void BuildWebGLMobile()
        {
            SetupCompleteScene();
            string buildPath = "Builds/WebGL";
            if (!Directory.Exists(buildPath)) Directory.CreateDirectory(buildPath);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"<color=#00e5ff>[CyberWhack]</color> WebGL mobile build succeeded! Output: {buildPath} ({report.summary.totalSize / (1024 * 1024):F1} MB)");
            }
            else
            {
                Debug.LogError($"<color=#ff0055>[CyberWhack]</color> WebGL build failed with {report.summary.totalErrors} errors!");
            }
        }

        private static void CreateEditorPreviewMound(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite moundSprite, Sprite entitySprite, float entityOffsetY, float entityScale)
        {
            GameObject moundContainer = new GameObject(name, typeof(RectTransform));
            moundContainer.transform.SetParent(parent.transform, false);
            RectTransform mcRt = moundContainer.GetComponent<RectTransform>();
            mcRt.anchorMin = anchorMin;
            mcRt.anchorMax = anchorMax;
            mcRt.offsetMin = Vector2.zero;
            mcRt.offsetMax = Vector2.zero;

            GameObject maskObj = new GameObject("MaskContainer", typeof(RectTransform), typeof(RectMask2D));
            maskObj.transform.SetParent(moundContainer.transform, false);
            RectTransform maskRt = maskObj.GetComponent<RectTransform>();
            maskRt.anchorMin = new Vector2(0.08f, 0.22f);
            maskRt.anchorMax = new Vector2(0.92f, 0.98f);
            maskRt.offsetMin = Vector2.zero;
            maskRt.offsetMax = Vector2.zero;

            GameObject entityObj = new GameObject("Entity", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            entityObj.transform.SetParent(maskObj.transform, false);
            RectTransform eRt = entityObj.GetComponent<RectTransform>();
            eRt.anchorMin = new Vector2(0.5f, 0f);
            eRt.anchorMax = new Vector2(0.5f, 0f);
            eRt.sizeDelta = new Vector2(210f * entityScale, 210f * entityScale);
            eRt.anchoredPosition = new Vector2(0f, entityOffsetY);
            Image eImg = entityObj.GetComponent<Image>();
            eImg.sprite = entitySprite;
            eImg.preserveAspect = true;
            eImg.raycastTarget = false;

            GameObject ringObj = new GameObject("DirtMound", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            ringObj.transform.SetParent(moundContainer.transform, false);
            RectTransform ringRt = ringObj.GetComponent<RectTransform>();
            ringRt.anchorMin = Vector2.zero;
            ringRt.anchorMax = Vector2.one;
            ringRt.offsetMin = Vector2.zero;
            ringRt.offsetMax = Vector2.zero;
            Image ringImg = ringObj.GetComponent<Image>();
            ringImg.sprite = moundSprite;
            ringImg.preserveAspect = true;
            ringImg.raycastTarget = false;
        }

        private static void CreateEditorIntelRow(GameObject parent, string name, float anchorMinY, float anchorMaxY, Sprite sprite, string title, string desc, string pts, Color ptsColor, Sprite bgSprite)
        {
            GameObject rowObj = CreatePanel(parent, name, new Vector2(0.04f, anchorMinY), new Vector2(0.96f, anchorMaxY), bgSprite);

            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObj.transform.SetParent(rowObj.transform, false);
            RectTransform iconRt = iconObj.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.03f, 0.1f);
            iconRt.anchorMax = new Vector2(0.22f, 0.9f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            Image img = iconObj.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;

            TextMeshProUGUI titleTmp = CreateText(rowObj, "Title", title, 20, FontStyles.Bold, new Color32(58, 30, 12, 255), TextAlignmentOptions.TopLeft);
            titleTmp.rectTransform.anchorMin = new Vector2(0.25f, 0.50f);
            titleTmp.rectTransform.anchorMax = new Vector2(0.70f, 0.92f);
            titleTmp.rectTransform.offsetMin = Vector2.zero;
            titleTmp.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI descTmp = CreateText(rowObj, "Desc", desc, 16, FontStyles.Normal, new Color32(109, 58, 22, 255), TextAlignmentOptions.TopLeft);
            descTmp.rectTransform.anchorMin = new Vector2(0.25f, 0.08f);
            descTmp.rectTransform.anchorMax = new Vector2(0.70f, 0.50f);
            descTmp.rectTransform.offsetMin = Vector2.zero;
            descTmp.rectTransform.offsetMax = Vector2.zero;

            TextMeshProUGUI ptsTmp = CreateText(rowObj, "Pts", pts, 22, FontStyles.Bold, ptsColor, TextAlignmentOptions.MidlineRight);
            ptsTmp.rectTransform.anchorMin = new Vector2(0.70f, 0.1f);
            ptsTmp.rectTransform.anchorMax = new Vector2(0.96f, 0.9f);
            ptsTmp.rectTransform.offsetMin = Vector2.zero;
            ptsTmp.rectTransform.offsetMax = Vector2.zero;
        }

        private static GameObject CreatePanel(GameObject parent, string name, Vector2 anchorMin, Vector2 anchorMax, Sprite sprite)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.transform.SetParent(parent.transform, false);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image img = obj.GetComponent<Image>();
            if (sprite != null)
            {
                img.sprite = sprite;
            }
            img.color = Color.white;
            return obj;
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, float size, FontStyles style, Color color, TextAlignmentOptions align)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent.transform, false);
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i].path == path) return;
            }

            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[original.Length + 1];
            updated[0] = new EditorBuildSettingsScene(path, true);
            for (int i = 0; i < original.Length; i++)
            {
                updated[i + 1] = original[i];
            }
            EditorBuildSettings.scenes = updated;
        }
    }

    [InitializeOnLoad]
    public static class CyberWhackAutoInitializer
    {
        static CyberWhackAutoInitializer()
        {
            EditorApplication.delayCall += CheckAndInitialize;
        }

        private static void CheckAndInitialize()
        {
            if (!File.Exists("Assets/CyberWhack/Art/HatchRing.png") || !File.Exists("Assets/CyberWhack/CyberWhackScene.unity"))
            {
                Debug.Log("<color=#00e5ff>[CyberWhack 9X]</color> Initializing assets & scene setup automatically...");
                CyberWhackSetup.SetupCompleteScene();
            }
        }
    }
}
