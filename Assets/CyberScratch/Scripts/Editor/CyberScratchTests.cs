#if UNITY_EDITOR
using System.IO;
using CyberScratch.Audio;
using CyberScratch.Core;
using CyberScratch.Data;
using CyberScratch.Scratch;
using CyberScratch.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.Tests
{
    [UnityEditor.InitializeOnLoad]
    public class CyberScratchTests
    {
        static CyberScratchTests()
        {
            UnityEditor.EditorApplication.delayCall += RunSafeTests;
        }

        private static void RunSafeTests()
        {
            var tests = new CyberScratchTests();
            tests.Test_TicketModel_GenerationAndWinEvaluation();
            tests.Test_TicketModel_NearMissEvaluation();
            tests.Test_SymbolHierarchyAndMultipliers();
            tests.Test_ProceduralArtAndSpritesGenerated();
            tests.Test_ProceduralAudioClipsSynthesis();
        }

        [UnityEditor.MenuItem("CyberScratch/Run Automated Tests", false, 3)]
        public static void RunAllTests()
        {
            var tests = new CyberScratchTests();
            tests.Test_TicketModel_GenerationAndWinEvaluation();
            tests.Test_TicketModel_NearMissEvaluation();
            tests.Test_SymbolHierarchyAndMultipliers();
            tests.Test_ProceduralArtAndSpritesGenerated();
            tests.Test_ProceduralAudioClipsSynthesis();
            tests.Test_MainMenuAndHowToPlayModalFlow();
            tests.Test_ModalLayoutsAndBoundingBoxesContained();
            tests.Test_MainMenuSceneBootstrap_CreatesValidHierarchy();
            tests.Test_GameSceneBootstrap_CreatesValidHierarchy();
            Debug.Log("<color=#00FFAA><b>[CyberScratch Tests]</b> All 9 test suites passed successfully! 100% verified!</color>");
        }

        [Test]
        public void Test_MainMenuAndHowToPlayModalFlow()
        {
            var gm = CyberScratchBootstrap.BuildCompleteGameHierarchy();
            Assert.IsNotNull(gm, "GameManager should be built by Bootstrap");

            var mainMenuView = Object.FindAnyObjectByType<MainMenuView>();
            Assert.IsNotNull(mainMenuView, "MainMenuView should exist in game hierarchy");
            Assert.IsTrue(mainMenuView.IsVisible, "Main menu should be the first screen in play mode");

            var htpModal = Object.FindAnyObjectByType<HowToPlayModalView>();
            Assert.IsNotNull(htpModal, "HowToPlayModalView should exist in game hierarchy");
            Assert.IsFalse(htpModal.IsVisible, "HowToPlayModalView should start hidden");

            htpModal.Show();
            Assert.IsTrue(htpModal.IsVisible, "HowToPlayModalView should become visible on Show()");
            htpModal.HideInstant();
            Assert.IsFalse(htpModal.IsVisible, "HowToPlayModalView should be hidden after HideInstant()");

            // Clean up test hierarchy
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            if (gm != null) Object.DestroyImmediate(gm.gameObject);
            var audioService = Object.FindAnyObjectByType<CyberAudioService>();
            if (audioService != null) Object.DestroyImmediate(audioService.gameObject);
        }

        [Test]
        public void Test_TicketModel_GenerationAndWinEvaluation()
        {
            var config = ScriptableObject.CreateInstance<CyberScratchConfig>();
            config.InitializeDefaultsIfNeeded();

            TicketModel ticket = new TicketModel();
            ticket.Generate(2.0f, config.Symbols, rowWinChance: 1.0f, nearMissRate: 0f);

            // With rowWinChance = 1.0, all 5 rows must win
            for (int r = 0; r < 5; r++)
            {
                Assert.IsTrue(ticket.IsRowWin(r), $"Row {r} should be a win when win chance is 100%");
                Assert.AreEqual(ticket.GetSymbol(r, 0), ticket.GetSymbol(r, 1));
                Assert.AreEqual(ticket.GetSymbol(r, 1), ticket.GetSymbol(r, 2));
                Assert.Greater(ticket.GetRowPrize(r), 0f);
            }

            Assert.AreEqual(5, ticket.GetWinningRowCount());
            Assert.Greater(ticket.CalculateTotalWinnings(), 0f);
        }

        [Test]
        public void Test_TicketModel_NearMissEvaluation()
        {
            var config = ScriptableObject.CreateInstance<CyberScratchConfig>();
            config.InitializeDefaultsIfNeeded();

            TicketModel ticket = new TicketModel();
            // Force 0% win chance, 100% near miss rate
            ticket.Generate(2.0f, config.Symbols, rowWinChance: 0f, nearMissRate: 1.0f);

            int nearMissCount = 0;
            for (int r = 0; r < 5; r++)
            {
                Assert.IsFalse(ticket.IsRowWin(r), $"Row {r} must not win");
                if (ticket.IsRowNearMiss(r))
                {
                    nearMissCount++;
                    Assert.AreEqual(ticket.GetSymbol(r, 0), ticket.GetSymbol(r, 1));
                    Assert.AreNotEqual(ticket.GetSymbol(r, 1), ticket.GetSymbol(r, 2));
                }
            }

            Assert.Greater(nearMissCount, 0, "At least one near-miss row should be generated when nearMissRate is 100%");
        }

        [Test]
        public void Test_SymbolHierarchyAndMultipliers()
        {
            var config = ScriptableObject.CreateInstance<CyberScratchConfig>();
            config.InitializeDefaultsIfNeeded();

            Assert.AreEqual(6, config.Symbols.Length);
            Assert.AreEqual(1f, config.Symbols[(int)SymbolType.BronzeGear].Multiplier);
            Assert.AreEqual(2f, config.Symbols[(int)SymbolType.SilverIngot].Multiplier);
            Assert.AreEqual(5f, config.Symbols[(int)SymbolType.GoldCore].Multiplier);
            Assert.AreEqual(15f, config.Symbols[(int)SymbolType.PlasmaGem].Multiplier);
            Assert.AreEqual(50f, config.Symbols[(int)SymbolType.CyberCrown].Multiplier);
            Assert.AreEqual(250f, config.Symbols[(int)SymbolType.Lucky777].Multiplier);
        }

        [Test]
        public void Test_ProceduralArtAndSpritesGenerated()
        {
            Assert.IsNotNull(CyberScratchArt.BronzeGearSprite);
            Assert.IsNotNull(CyberScratchArt.SilverIngotSprite);
            Assert.IsNotNull(CyberScratchArt.GoldCoreSprite);
            Assert.IsNotNull(CyberScratchArt.PlasmaGemSprite);
            Assert.IsNotNull(CyberScratchArt.CyberCrownSprite);
            Assert.IsNotNull(CyberScratchArt.Lucky777Sprite);

            Assert.IsNotNull(CyberScratchArt.FoilHexTexture);
            Assert.IsNotNull(CyberScratchArt.SoftBrushTexture);
            Assert.IsNotNull(CyberScratchArt.CellFrameSprite);
            Assert.IsNotNull(CyberScratchArt.RowBorderIdleSprite);
            Assert.IsNotNull(CyberScratchArt.RowBorderWinSprite);
            Assert.IsNotNull(CyberScratchArt.PrizeTagSprite);
            Assert.IsNotNull(CyberScratchArt.ButtonAmberSprite);
            Assert.IsNotNull(CyberScratchArt.ButtonEmeraldSprite);
            Assert.IsNotNull(CyberScratchArt.ButtonCyanSprite);
            Assert.IsNotNull(CyberScratchArt.SparkleSprite);
        }

        [Test]
        public void Test_ProceduralAudioClipsSynthesis()
        {
            GameObject audioObj = new GameObject("Test_AudioService");
            var audioService = audioObj.AddComponent<CyberAudioService>();

            Assert.IsNotNull(audioService);
            Assert.IsFalse(audioService.IsMuted);

            audioService.ToggleMute();
            Assert.IsTrue(audioService.IsMuted);

            audioService.ToggleMute();
            Assert.IsFalse(audioService.IsMuted);

            Object.DestroyImmediate(audioObj);
        }

        [Test]
        public void Test_ModalLayoutsAndBoundingBoxesContained()
        {
            var gm = CyberScratchBootstrap.BuildCompleteGameHierarchy();
            Assert.IsNotNull(gm);

            // 1. Verify HowToPlayModal fills canvas gracefully
            var htpModal = Object.FindAnyObjectByType<HowToPlayModalView>();
            Assert.IsNotNull(htpModal);
            Transform htpPanel = htpModal.transform.Find("DialogPanel");
            Assert.IsNotNull(htpPanel, "HowToPlayModal should contain DialogPanel");
            RectTransform htpRect = htpPanel.GetComponent<RectTransform>();
            Assert.LessOrEqual(htpRect.anchorMin.x, 0.05f);
            Assert.GreaterOrEqual(htpRect.anchorMax.x, 0.95f);
            Assert.LessOrEqual(htpRect.anchorMin.y, 0.05f);
            Assert.GreaterOrEqual(htpRect.anchorMax.y, 0.95f);
            Image htpImg = htpPanel.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced, htpImg.type, "Dialog panel should be 9-sliced to fill screen");

            Transform htpTitle = htpPanel.Find("Title");
            Assert.IsNotNull(htpTitle);
            RectTransform titleRt = htpTitle.GetComponent<RectTransform>();
            Assert.GreaterOrEqual(titleRt.anchorMin.y, 0.88f);

            Transform htpRules = htpPanel.Find("RulesText");
            Assert.IsNotNull(htpRules);
            RectTransform rulesRt = htpRules.GetComponent<RectTransform>();
            Assert.GreaterOrEqual(rulesRt.anchorMin.x, 0.10f);
            Assert.LessOrEqual(rulesRt.anchorMax.x, 0.90f);

            Transform htpClose = htpPanel.Find("CloseButton");
            Assert.IsNotNull(htpClose);

            // 2. Verify WinModalDialog fills canvas gracefully
            var winModal = Object.FindAnyObjectByType<WinModalView>();
            Assert.IsNotNull(winModal);
            Transform winPanel = winModal.transform.Find("DialogPanel");
            Assert.IsNotNull(winPanel, "WinModalDialog should contain DialogPanel");
            Image winImg = winPanel.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced, winImg.type, "Win dialog panel should be 9-sliced to fill screen");

            // 3. Verify SettingsSheet fills canvas gracefully
            GameObject settingsObj = GameObject.Find("SettingsSheet");
            Assert.IsNotNull(settingsObj);
            Transform sheet = settingsObj.transform.Find("Sheet");
            Assert.IsNotNull(sheet, "SettingsSheet should contain Sheet");
            RectTransform sheetRt = sheet.GetComponent<RectTransform>();
            Assert.LessOrEqual(sheetRt.anchorMin.x, 0.05f);
            Assert.GreaterOrEqual(sheetRt.anchorMax.x, 0.95f);
            Image sheetImg = sheet.GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced, sheetImg.type, "Settings sheet should be 9-sliced to fill screen");

            // Clean up test hierarchy
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            if (gm != null) Object.DestroyImmediate(gm.gameObject);
            var audioService = Object.FindAnyObjectByType<CyberAudioService>();
            if (audioService != null) Object.DestroyImmediate(audioService.gameObject);
        }

        [Test]
        public void Test_MainMenuSceneBootstrap_CreatesValidHierarchy()
        {
            var controller = CyberScratchBootstrap.BuildMainMenuSceneHierarchy();
            Assert.IsNotNull(controller, "MainMenuController should be instantiated");

            var mainMenuView = Object.FindAnyObjectByType<MainMenuView>();
            Assert.IsNotNull(mainMenuView, "MainMenuView should exist in MainMenuScene");
            Assert.IsTrue(mainMenuView.IsVisible, "MainMenuView should be visible");

            var htpModal = Object.FindAnyObjectByType<HowToPlayModalView>();
            Assert.IsNotNull(htpModal, "HowToPlayModalView should exist in MainMenuScene");
            Assert.IsFalse(htpModal.IsVisible, "HowToPlayModalView should start hidden");

            var audioService = Object.FindAnyObjectByType<CyberAudioService>();
            Assert.IsNotNull(audioService, "CyberAudioService should exist in MainMenuScene");

            // Clean up
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            if (controller != null) Object.DestroyImmediate(controller.gameObject);
            if (audioService != null) Object.DestroyImmediate(audioService.gameObject);
        }

        [Test]
        public void Test_GameSceneBootstrap_CreatesValidHierarchy()
        {
            var gm = CyberScratchBootstrap.BuildGameSceneHierarchy();
            Assert.IsNotNull(gm, "CyberScratchGameManager should be instantiated");

            // In GameScene, MainMenuView does not exist in the scene hierarchy!
            var mainMenuView = Object.FindAnyObjectByType<MainMenuView>();
            Assert.IsNull(mainMenuView, "MainMenuView should NOT exist in GameScene hierarchy (dedicated 2-scene separation)");

            var hudView = Object.FindAnyObjectByType<ScratchHUDView>();
            Assert.IsNotNull(hudView, "ScratchHUDView should exist in GameScene");

            var htpModal = Object.FindAnyObjectByType<HowToPlayModalView>();
            Assert.IsNotNull(htpModal, "HowToPlayModalView should exist in GameScene");

            var winModal = Object.FindAnyObjectByType<WinModalView>();
            Assert.IsNotNull(winModal, "WinModalView should exist in GameScene");

            // Clean up
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            if (gm != null) Object.DestroyImmediate(gm.gameObject);
            var audioService = Object.FindAnyObjectByType<CyberAudioService>();
            if (audioService != null) Object.DestroyImmediate(audioService.gameObject);
        }
    }
}
#endif
