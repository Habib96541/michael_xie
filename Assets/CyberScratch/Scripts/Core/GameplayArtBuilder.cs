using CyberScratch.Scratch;
using CyberScratch.UI;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.Core
{
    public static partial class CyberScratchBootstrap
    {
        /// <summary>Normalized layout tuned to the portrait 900 × 1600 canvas.</summary>
        private static class GameSceneLayout
        {
            public const float LogoXMin = 0.08f;
            public const float LogoYMin = 0.870f;
            public const float LogoXMax = 0.92f;
            public const float LogoYMax = 0.940f;

            public const float BalanceXMin = 0.040f;
            public const float BalanceYMin = 0.800f;
            public const float BalanceXMax = 0.360f;
            public const float BalanceYMax = 0.856f;

            public const float PlusXMin = 0.375f;
            public const float PlusYMin = 0.803f;
            public const float PlusXMax = 0.455f;
            public const float PlusYMax = 0.853f;

            public const float TimerXMin = 0.525f;
            public const float TimerYMin = 0.800f;
            public const float TimerXMax = 0.845f;
            public const float TimerYMax = 0.856f;

            public const float SettingsXMin = 0.865f;
            public const float SettingsYMin = 0.802f;
            public const float SettingsXMax = 0.960f;
            public const float SettingsYMax = 0.854f;

            public const float StatusYMin = 0.758f;
            public const float StatusYMax = 0.788f;

            public const float BoardFrameXMin = 0.012f;
            public const float BoardFrameYMin = 0.128f;
            public const float BoardFrameXMax = 0.988f;
            public const float BoardFrameYMax = 0.755f;

            public const float BoardContentXMin = 0.065f;
            public const float BoardContentYMin = 0.158f;
            public const float BoardContentXMax = 0.935f;
            public const float BoardContentYMax = 0.722f;

            public const float RevealBtnXMin = 0.040f;
            public const float RevealBtnYMin = 0.045f;
            public const float RevealBtnXMax = 0.485f;
            public const float RevealBtnYMax = 0.125f;

            public const float BuyBtnXMin = 0.515f;
            public const float BuyBtnYMin = 0.045f;
            public const float BuyBtnXMax = 0.960f;
            public const float BuyBtnYMax = 0.125f;

            public const float RowLabelXMin = 0.025f;
            public const float RowLabelXMax = 0.160f;
            public const float CellsXMin = 0.170f;
            public const float CellsXMax = 0.730f;
            public const float PrizeXMin = 0.740f;
            public const float PrizeXMax = 0.975f;
        }

        private struct GameplayRefs
        {
            public CanvasGroup Group;
            public ScratchCardController Scratch;
            public RowView[] Rows;
            public ScratchHUDView Hud;
        }

        private static Sprite Art(string fileName, Sprite fallback)
        {
            Sprite sprite = GameSceneSprites.Get(fileName);
            return sprite != null ? sprite : fallback;
        }

        private static GameplayRefs BuildArtGameplay(Transform canvas, Font font)
        {
            GameObject stageObj = CreateUI("GameSceneArt_v6", canvas);
            RectTransform stageRect = stageObj.GetComponent<RectTransform>();
            Stretch(stageRect);

            GameObject gameplayObj = CreateUI("GameplayContainer", stageObj.transform);
            CanvasGroup gameplayGroup = gameplayObj.AddComponent<CanvasGroup>();
            gameplayGroup.alpha = 1f;
            gameplayGroup.interactable = true;
            gameplayGroup.blocksRaycasts = true;
            Stretch(gameplayObj.GetComponent<RectTransform>());

            Image background = CreateImage("Background", gameplayObj.transform, Art("01_fullscreen_cyber_background.png", null), false);
            background.color = background.sprite != null ? Color.white : new Color(0.03f, 0.05f, 0.08f, 1f);
            Stretch(background.rectTransform);

            Image boardFrame = CreateImage("BoardFrame", gameplayObj.transform, Art("10_board_panel_blank.png", null), false);
            boardFrame.type = Image.Type.Sliced;
            boardFrame.pixelsPerUnitMultiplier = 3.0f;
            Place(boardFrame.rectTransform, GameSceneLayout.BoardFrameXMin, GameSceneLayout.BoardFrameYMin,
                GameSceneLayout.BoardFrameXMax, GameSceneLayout.BoardFrameYMax);
            boardFrame.color = Color.white;
            boardFrame.raycastTarget = false;

            GameObject boardObj = CreateUI("ArtBoard", gameplayObj.transform);
            Place(boardObj.GetComponent<RectTransform>(),
                GameSceneLayout.BoardContentXMin, GameSceneLayout.BoardContentYMin,
                GameSceneLayout.BoardContentXMax, GameSceneLayout.BoardContentYMax);

            Image logo = CreateImage("Logo", gameplayObj.transform, Art("01_CyberScratch_5X_Logo.png", null), true);
            Place(logo.rectTransform, GameSceneLayout.LogoXMin, GameSceneLayout.LogoYMin, GameSceneLayout.LogoXMax, GameSceneLayout.LogoYMax);

            Image balancePlate = CreateImage("BalancePlate", gameplayObj.transform, Art("03_balance_hud_blank.png", null), true);
            Place(balancePlate.rectTransform, GameSceneLayout.BalanceXMin, GameSceneLayout.BalanceYMin,
                GameSceneLayout.BalanceXMax, GameSceneLayout.BalanceYMax);

            Image coinIcon = CreateImage("CoinIcon", balancePlate.transform, Art("02_Gold_Coins.png", CyberScratchArt.GoldCoreSprite), true);
            Place(coinIcon.rectTransform, 0.07f, 0.15f, 0.24f, 0.85f);

            Text balanceText = CreateLabel("BalanceText", balancePlate.transform, font, "$1,250.00", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Place(balanceText.rectTransform, 0.26f, 0.10f, 0.94f, 0.90f);
            EnableBestFit(balanceText, 20, 32);
            AddOutline(balanceText, new Color(0f, 0.05f, 0.08f, 0.85f), new Vector2(1.2f, -1.2f));

            Button plusButton = CreateArtButton("PlusButton", gameplayObj.transform, Art("09_plus_button_plate_blank.png", CyberScratchArt.ButtonEmeraldSprite), out _, stretch: false);
            Place(plusButton.GetComponent<RectTransform>(), GameSceneLayout.PlusXMin, GameSceneLayout.PlusYMin,
                GameSceneLayout.PlusXMax, GameSceneLayout.PlusYMax);
            Image plusIcon = CreateImage("PlusIcon", plusButton.transform, Art("11_Plus_Icon.png", null), true);
            Place(plusIcon.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);

            Image timerPlate = CreateImage("TimerPlate", gameplayObj.transform, Art("04_timer_hud_blank.png", null), true);
            Place(timerPlate.rectTransform, GameSceneLayout.TimerXMin, GameSceneLayout.TimerYMin,
                GameSceneLayout.TimerXMax, GameSceneLayout.TimerYMax);

            Image timerIcon = CreateImage("TimerIcon", timerPlate.transform, Art("03_Gold_Timer.png", null), true);
            Place(timerIcon.rectTransform, 0.08f, 0.16f, 0.26f, 0.84f);

            Text timerText = CreateLabel("TimerText", timerPlate.transform, font, "90s", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Place(timerText.rectTransform, 0.28f, 0.10f, 0.92f, 0.90f);
            EnableBestFit(timerText, 20, 32);
            AddOutline(timerText, new Color(0f, 0.05f, 0.08f, 0.85f), new Vector2(1f, -1f));

            Button settingsButton = CreateArtButton("SettingsButton", gameplayObj.transform, Art("08_settings_plate_blank.png", CyberScratchArt.ButtonCyanSprite), out _, stretch: false);
            Place(settingsButton.GetComponent<RectTransform>(), GameSceneLayout.SettingsXMin, GameSceneLayout.SettingsYMin,
                GameSceneLayout.SettingsXMax, GameSceneLayout.SettingsYMax);
            Image gearIcon = CreateImage("GearIcon", settingsButton.transform, Art("10_Settings_Icon.png", null), true);
            Place(gearIcon.rectTransform, 0.22f, 0.22f, 0.78f, 0.78f);

            Text statusText = CreateLabel("StatusText", gameplayObj.transform, font, string.Empty, 20, FontStyle.Bold,
                new Color(0.45f, 0.92f, 1f, 0.95f), TextAnchor.MiddleCenter);
            Place(statusText.rectTransform, 0.06f, GameSceneLayout.StatusYMin, 0.94f, GameSceneLayout.StatusYMax);
            EnableBestFit(statusText, 18, 24);
            AddOutline(statusText, new Color(0f, 0.02f, 0.05f, 0.75f), new Vector2(0.8f, -0.8f));

            Sprite cellFrame = Art("12_scratch_cell_frame_blank.png", CyberScratchArt.CellFrameSprite);
            Sprite winFrame = Art("13_winning_cell_gold_frame_blank.png", CyberScratchArt.RowBorderWinSprite);
            Sprite foilSprite = Art("11_scratch_foil_blank.png", null);
            Sprite prizeIdle = Art("06_prize_tag_blank.png", CyberScratchArt.PrizeTagSprite);
            Sprite prizeWin = Art("07_prize_win_tag_blank.png", CyberScratchArt.PrizeTagSprite);
            Sprite rowLabelSprite = Art("05_row_label_blank.png", null);
            Sprite laurelSprite = Art("12_Gold_Laurel.png", null);
            Sprite starSprite = Art("13_Gold_Star.png", CyberScratchArt.SparkleSprite);
            Sprite haloSprite = Art("15_Pink_Neon_Ring.png", CyberScratchArt.SparkleSprite);
            Sprite rowWinBorder = CyberScratchArt.RowBorderWinSprite != null ? CyberScratchArt.RowBorderWinSprite : winFrame;

            RowView[] rows = new RowView[5];
            Image[] foilImages = new Image[15];

            for (int r = 0; r < 5; r++)
            {
                float rowH = 1f / 5f;
                float pad = 0.015f;
                float yMax = 1f - r * rowH - pad;
                float yMin = yMax - rowH + pad * 2f;

                GameObject rowObj = CreateUI("Row_" + (r + 1), boardObj.transform);
                Place(rowObj.GetComponent<RectTransform>(), 0f, yMin, 1f, yMax);
                RowView rowView = rowObj.AddComponent<RowView>();
                rows[r] = rowView;

                Image winGlow = CreateImage("WinGlow", rowObj.transform, rowWinBorder, false);
                Place(winGlow.rectTransform, GameSceneLayout.CellsXMin - 0.005f, 0.02f, GameSceneLayout.CellsXMax + 0.005f, 0.98f);
                winGlow.color = new Color(1f, 0.82f, 0.2f, 0.88f);
                winGlow.gameObject.SetActive(false);

                Image labelPlate = CreateImage("RowLabel", rowObj.transform, rowLabelSprite, true);
                Place(labelPlate.rectTransform, GameSceneLayout.RowLabelXMin, 0.16f, GameSceneLayout.RowLabelXMax, 0.84f);

                Text labelText = CreateLabel("Label", labelPlate.transform, font, "ROW " + (r + 1), 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                Place(labelText.rectTransform, 0.05f, 0.05f, 0.95f, 0.95f);
                EnableBestFit(labelText, 20, 28);
                AddOutline(labelText, new Color(0f, 0.08f, 0.14f, 0.95f), new Vector2(1.2f, -1.2f));

                ScratchCellView[] cells = new ScratchCellView[3];
                float cellsWidth = GameSceneLayout.CellsXMax - GameSceneLayout.CellsXMin;
                float cellGap = 0.010f;
                float cellW = (cellsWidth - cellGap * 2f) / 3f;
                for (int c = 0; c < 3; c++)
                {
                    float xMin = GameSceneLayout.CellsXMin + c * (cellW + cellGap);
                    float xMax = xMin + cellW;

                    GameObject cellObj = CreateUI("Cell_" + (c + 1), rowObj.transform);
                    RectTransform cellRect = cellObj.GetComponent<RectTransform>();
                    Place(cellRect, xMin, 0.04f, xMax, 0.96f);

                    Image backing = CreateImage("Backing", cellObj.transform, null, false);
                    Place(backing.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
                    backing.color = new Color(0.02f, 0.04f, 0.06f, 1f);

                    Image symbol = CreateImage("Icon", cellObj.transform, null, true);
                    Place(symbol.rectTransform, 0.14f, 0.14f, 0.86f, 0.86f);

                    Image foil = CreateImage("Foil", cellObj.transform, foilSprite, true);
                    Place(foil.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);
                    foilImages[r * 3 + c] = foil;

                    Image frame = CreateImage("Frame", cellObj.transform, cellFrame, true);
                    Place(frame.rectTransform, 0f, 0f, 1f, 1f);

                    Image halo = CreateImage("Halo", cellObj.transform, haloSprite, true);
                    Place(halo.rectTransform, -0.02f, -0.02f, 1.02f, 1.02f);
                    halo.color = new Color(1f, 0.45f, 0.85f, 0.85f);
                    halo.gameObject.SetActive(false);

                    ScratchCellView cellView = cellObj.AddComponent<ScratchCellView>();
                    SetField(cellView, "_symbolIcon", symbol);
                    SetField(cellView, "_frameImage", frame);
                    SetField(cellView, "_targetHalo", halo);
                    SetField(cellView, "_rectTransform", cellRect);
                    cellView.ConfigureFrames(cellFrame, winFrame);
                    cells[c] = cellView;
                }

                Image sparkleLeft = CreateImage("SparkleLeft", rowObj.transform, starSprite, true);
                Place(sparkleLeft.rectTransform, GameSceneLayout.CellsXMin + 0.005f, 0.50f, GameSceneLayout.CellsXMin + 0.065f, 0.94f);
                sparkleLeft.gameObject.SetActive(false);
                Image sparkleRight = CreateImage("SparkleRight", rowObj.transform, starSprite, true);
                Place(sparkleRight.rectTransform, GameSceneLayout.CellsXMax - 0.065f, 0.50f, GameSceneLayout.CellsXMax - 0.005f, 0.94f);
                sparkleRight.gameObject.SetActive(false);

                Image prizePlate = CreateImage("PrizeTag", rowObj.transform, prizeIdle, true);
                Place(prizePlate.rectTransform, GameSceneLayout.PrizeXMin, 0.08f, GameSceneLayout.PrizeXMax, 0.92f);

                Text prizeCaption = CreateLabel("PrizeCaption", prizePlate.transform, font, "PRIZE", 22, FontStyle.Bold, new Color(0.35f, 0.95f, 1f, 1f), TextAnchor.MiddleCenter);
                Place(prizeCaption.rectTransform, 0.08f, 0.58f, 0.92f, 0.92f);
                EnableBestFit(prizeCaption, 18, 24);
                AddOutline(prizeCaption, new Color(0f, 0.05f, 0.1f, 0.95f), new Vector2(1.2f, -1.2f));

                Text prizeAmount = CreateLabel("PrizeAmount", prizePlate.transform, font, "$0", 34, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                Place(prizeAmount.rectTransform, 0.06f, 0.08f, 0.94f, 0.60f);
                EnableBestFit(prizeAmount, 24, 40);
                AddOutline(prizeAmount, new Color(0f, 0.04f, 0.08f, 0.95f), new Vector2(1.5f, -1.5f));

                Image laurelLeft = CreateImage("LaurelLeft", prizePlate.transform, laurelSprite, true);
                Place(laurelLeft.rectTransform, 0.03f, 0.20f, 0.20f, 0.80f);
                laurelLeft.gameObject.SetActive(false);
                Image laurelRight = CreateImage("LaurelRight", prizePlate.transform, laurelSprite, true);
                Place(laurelRight.rectTransform, 0.80f, 0.20f, 0.97f, 0.80f);
                laurelRight.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
                laurelRight.gameObject.SetActive(false);

                SetField(rowView, "_rowIndex", r);
                SetField(rowView, "_cells", cells);
                SetField(rowView, "_prizeTagBg", prizePlate);
                SetField(rowView, "_prizeCaption", prizeCaption);
                SetField(rowView, "_prizeText", prizeAmount);
                SetField(rowView, "_idlePrizeSprite", prizeIdle);
                SetField(rowView, "_winPrizeSprite", prizeWin);
                SetField(rowView, "_winGlow", winGlow);
                SetField(rowView, "_laurelLeft", laurelLeft);
                SetField(rowView, "_laurelRight", laurelRight);
                SetField(rowView, "_sparkleLeft", sparkleLeft);
                SetField(rowView, "_sparkleRight", sparkleRight);
            }

            GameObject catcherObj = CreateUI("ScratchCatcher", boardObj.transform);
            RectTransform catcherRect = catcherObj.GetComponent<RectTransform>();
            Place(catcherRect, GameSceneLayout.CellsXMin, 0f, GameSceneLayout.CellsXMax, 1f);
            Image catcherImage = catcherObj.AddComponent<Image>();
            catcherImage.color = new Color(1f, 1f, 1f, 0f);
            catcherImage.raycastTarget = true;

            GameObject dustObj = new GameObject("DustParticles");
            dustObj.transform.SetParent(catcherObj.transform, false);
            ParticleSystem dust = dustObj.AddComponent<ParticleSystem>();
            ConfigureParticleSystem(dust, starSprite != null ? starSprite : CyberScratchArt.SparkleSprite);

            ScratchCardController scratch = catcherObj.AddComponent<ScratchCardController>();
            SetField(scratch, "_foilRawImage", null);
            Texture2D foilTex = Art("11_scratch_foil_blank.png", null) != null ? Art("11_scratch_foil_blank.png", null).texture : CyberScratchArt.FoilHexTexture;
            scratch.Initialize(
                Shader.Find("CyberScratch/FoilScratchMask"),
                Shader.Find("CyberScratch/BrushErase"),
                CyberScratchArt.SoftBrushTexture,
                foilTex);
            SetField(scratch, "_dustParticles", dust);
            scratch.ConfigureCellFoils(foilImages);

            Sprite goldButton = Art("14_reveal_button_gold_blank.png", CyberScratchArt.ButtonAmberSprite);
            Sprite greenButton = Art("15_new_card_button_green_blank.png", CyberScratchArt.ButtonEmeraldSprite);
            Sprite grayButton = Art("17_disabled_button_gray_blank.png", null);

            Button revealButton = CreateArtButton("RevealAllBtn", gameplayObj.transform, goldButton, out Image revealImage, stretch: true);
            Place(revealButton.GetComponent<RectTransform>(), GameSceneLayout.RevealBtnXMin, GameSceneLayout.RevealBtnYMin,
                GameSceneLayout.RevealBtnXMax, GameSceneLayout.RevealBtnYMax);
            Text revealText = CreateLabel("Text", revealButton.transform, font, "REVEAL ALL", 26, FontStyle.Bold,
                new Color(0.25f, 0.10f, 0.02f, 1f), TextAnchor.MiddleCenter);
            Place(revealText.rectTransform, 0.05f, 0.15f, 0.73f, 0.85f);
            EnableBestFit(revealText, 18, 28);
            Image chevrons = CreateImage("Chevrons", revealButton.transform, Art("17_Reveal_Double_Chevrons.png", null), true);
            Place(chevrons.rectTransform, 0.74f, 0.20f, 0.92f, 0.80f);

            Button buyButton = CreateArtButton("BuyTicketBtn", gameplayObj.transform, greenButton, out Image buyImage, stretch: true);
            Place(buyButton.GetComponent<RectTransform>(), GameSceneLayout.BuyBtnXMin, GameSceneLayout.BuyBtnYMin,
                GameSceneLayout.BuyBtnXMax, GameSceneLayout.BuyBtnYMax);
            Text buyText = CreateLabel("Text", buyButton.transform, font, "NEW CARD ($2)", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Place(buyText.rectTransform, 0.05f, 0.15f, 0.73f, 0.85f);
            EnableBestFit(buyText, 18, 26);
            AddOutline(buyText, new Color(0f, 0.18f, 0.06f, 0.65f), new Vector2(0.8f, -0.8f));
            Image refreshIcon = CreateImage("Refresh", buyButton.transform, Art("16_Refresh_Reload_Icon.png", null), true);
            Place(refreshIcon.rectTransform, 0.74f, 0.20f, 0.92f, 0.80f);

            GameObject settingsObj = CreateUI("SettingsSheet", gameplayObj.transform);
            CanvasGroup settingsGroup = settingsObj.AddComponent<CanvasGroup>();
            Stretch(settingsObj.GetComponent<RectTransform>());

            GameObject dimObj = CreateUI("Dim", settingsObj.transform);
            Image dimImage = dimObj.AddComponent<Image>();
            dimImage.color = new Color(0f, 0.02f, 0.05f, 0.72f);
            Stretch(dimObj.GetComponent<RectTransform>());
            Button dimButton = dimObj.AddComponent<Button>();
            dimButton.transition = Selectable.Transition.None;
            dimButton.targetGraphic = dimImage;

            GameObject sheetObj = CreateUI("Sheet", settingsObj.transform);
            RectTransform sheetRect = sheetObj.GetComponent<RectTransform>();
            sheetRect.anchorMin = new Vector2(0.04f, 0.04f);
            sheetRect.anchorMax = new Vector2(0.96f, 0.96f);
            sheetRect.offsetMin = Vector2.zero;
            sheetRect.offsetMax = Vector2.zero;

            Image sheetImage = sheetObj.AddComponent<Image>();
            sheetImage.sprite = Art("10_board_panel_blank.png", null);
            sheetImage.color = Color.white;
            sheetImage.type = Image.Type.Sliced;
            sheetImage.pixelsPerUnitMultiplier = 2.5f;

            Text sheetTitle = CreateLabel("Title", sheetObj.transform, font, "SETTINGS", 42, FontStyle.Bold, new Color(0.7f, 0.95f, 1f, 1f), TextAnchor.MiddleCenter);
            Place(sheetTitle.rectTransform, 0.12f, 0.865f, 0.88f, 0.920f);
            EnableBestFit(sheetTitle, 30, 44);
            var sheetTitleShadow = sheetTitle.gameObject.AddComponent<Shadow>();
            sheetTitleShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.95f);
            sheetTitleShadow.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject setDivObj = CreateUI("Divider", sheetObj.transform);
            Image setDivImg = setDivObj.AddComponent<Image>();
            setDivImg.sprite = Art("18_horizontal_divider_cyan.png", null);
            setDivImg.type = Image.Type.Sliced;
            setDivImg.color = Color.white;
            Place(setDivImg.rectTransform, 0.15f, 0.840f, 0.85f, 0.850f);

            Text priceLabel = CreateLabel("PriceLabel", sheetObj.transform, font, "NEW CARD PRICE", 24, FontStyle.Bold, new Color(1f, 0.86f, 0.35f, 1f), TextAnchor.MiddleCenter);
            Place(priceLabel.rectTransform, 0.12f, 0.770f, 0.88f, 0.815f);
            EnableBestFit(priceLabel, 20, 28);

            Button[] wagerButtons = new Button[4];
            Text[] wagerTexts = new Text[4];
            float[] tiers = { 1f, 2f, 5f, 10f };
            Sprite blueButton = Art("16_blue_action_button_blank.png", CyberScratchArt.ButtonCyanSprite);
            for (int w = 0; w < 4; w++)
            {
                float xMin = 0.14f + w * 0.185f;
                Button wagerButton = CreateArtButton("Wager_" + tiers[w], sheetObj.transform, blueButton, out _, stretch: true);
                Place(wagerButton.GetComponent<RectTransform>(), xMin, 0.675f, xMin + 0.165f, 0.750f);
                Text wagerText = CreateLabel("Text", wagerButton.transform, font, "$" + tiers[w].ToString("0"), 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                Stretch(wagerText.rectTransform);
                EnableBestFit(wagerText, 20, 28);
                var wShadow = wagerText.gameObject.AddComponent<Shadow>();
                wShadow.effectColor = new Color(0f, 0.15f, 0.35f, 0.85f);
                wShadow.effectDistance = new Vector2(1.5f, -1.5f);
                wagerButtons[w] = wagerButton;
                wagerTexts[w] = wagerText;
            }

            Button soundButton = CreateArtButton("SoundButton", sheetObj.transform, blueButton, out _, stretch: true);
            Place(soundButton.GetComponent<RectTransform>(), 0.18f, 0.535f, 0.82f, 0.635f);
            Text soundText = CreateLabel("Text", soundButton.transform, font, "SOUND [ON]", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(soundText.rectTransform);
            EnableBestFit(soundText, 20, 28);
            var sndShadow = soundText.gameObject.AddComponent<Shadow>();
            sndShadow.effectColor = new Color(0f, 0.15f, 0.35f, 0.85f);
            sndShadow.effectDistance = new Vector2(1.5f, -1.5f);

            Button rulesButton = CreateArtButton("RulesButton", sheetObj.transform, blueButton, out _, stretch: true);
            Place(rulesButton.GetComponent<RectTransform>(), 0.18f, 0.410f, 0.82f, 0.510f);
            Text rulesText = CreateLabel("Text", rulesButton.transform, font, "HOW TO PLAY", 26, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(rulesText.rectTransform);
            EnableBestFit(rulesText, 20, 28);
            var rShadow = rulesText.gameObject.AddComponent<Shadow>();
            rShadow.effectColor = new Color(0f, 0.15f, 0.35f, 0.85f);
            rShadow.effectDistance = new Vector2(1.5f, -1.5f);

            Button menuButton = CreateArtButton("MenuButton", sheetObj.transform, goldButton, out _, stretch: true);
            Place(menuButton.GetComponent<RectTransform>(), 0.18f, 0.285f, 0.82f, 0.385f);
            Text menuText = CreateLabel("Text", menuButton.transform, font, "MAIN MENU", 26, FontStyle.Bold, new Color(0.22f, 0.08f, 0.02f, 1f), TextAnchor.MiddleCenter);
            Stretch(menuText.rectTransform);
            EnableBestFit(menuText, 20, 28);
            var mShadow = menuText.gameObject.AddComponent<Shadow>();
            mShadow.effectColor = new Color(0.35f, 0.18f, 0.02f, 0.85f);
            mShadow.effectDistance = new Vector2(1.5f, -1.5f);

            Button closeButton = CreateArtButton("CloseButton", sheetObj.transform, greenButton, out _, stretch: true);
            Place(closeButton.GetComponent<RectTransform>(), 0.24f, 0.080f, 0.76f, 0.165f);
            Text closeText = CreateLabel("Text", closeButton.transform, font, "CLOSE", 28, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(closeText.rectTransform);
            EnableBestFit(closeText, 20, 30);
            var cShadow = closeText.gameObject.AddComponent<Shadow>();
            cShadow.effectColor = new Color(0f, 0.25f, 0.08f, 0.85f);
            cShadow.effectDistance = new Vector2(1.5f, -1.5f);
            ScratchHUDView hud = gameplayObj.AddComponent<ScratchHUDView>();
            SetField(hud, "_menuButton", menuButton);
            SetField(hud, "_balanceText", balanceText);
            SetField(hud, "_statusText", statusText);
            SetField(hud, "_soundButton", soundButton);
            SetField(hud, "_soundButtonText", soundText);
            SetField(hud, "_infoButton", rulesButton);
            SetField(hud, "_plusButton", plusButton);
            SetField(hud, "_settingsButton", settingsButton);
            SetField(hud, "_timerText", timerText);
            SetField(hud, "_settingsGroup", settingsGroup);
            SetField(hud, "_wagerTierButtons", wagerButtons);
            SetField(hud, "_wagerTierTexts", wagerTexts);
            SetField(hud, "_wagerSelectedColor", new Color(1f, 0.82f, 0.25f, 1f));
            SetField(hud, "_wagerUnselectedColor", Color.white);
            SetField(hud, "_revealAllButton", revealButton);
            SetField(hud, "_buyTicketButton", buyButton);
            SetField(hud, "_buyTicketButtonText", buyText);
            SetField(hud, "_revealAllButtonText", revealText);
            SetField(hud, "_revealAllImage", revealImage);
            SetField(hud, "_buyTicketImage", buyImage);
            SetField(hud, "_revealEnabledSprite", goldButton);
            SetField(hud, "_buyEnabledSprite", greenButton);
            SetField(hud, "_disabledButtonSprite", grayButton);
            SetField(hud, "_winCounterText", null);

            dimButton.onClick.AddListener(() => hud.SetSettingsOpen(false));
            closeButton.onClick.AddListener(() =>
            {
                CyberScratch.Audio.CyberAudioService.Instance?.PlayButtonClick();
                hud.SetSettingsOpen(false);
            });
            hud.SetSettingsOpen(false);

            return new GameplayRefs
            {
                Group = gameplayGroup,
                Scratch = scratch,
                Rows = rows,
                Hud = hud
            };
        }

        private static void Place(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite, bool preserveAspect)
        {
            GameObject obj = CreateUI(name, parent);
            Image image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = false;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            return image;
        }

        private static Text CreateLabel(string name, Transform parent, Font font, string value, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            GameObject obj = CreateUI(name, parent);
            Text text = obj.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static void EnableBestFit(Text text, int minSize, int maxSize)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minSize;
            text.resizeTextMaxSize = maxSize;
        }

        private static Button CreateArtButton(string name, Transform parent, Sprite sprite, out Image image, bool stretch)
        {
            GameObject obj = CreateUI(name, parent);
            image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            Button button = obj.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            obj.AddComponent<UIButtonPress>();
            obj.AddComponent<CanvasGroup>();
            return button;
        }

        private static void AddOutline(Text text, Color color, Vector2 distance)
        {
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }
    }
}
