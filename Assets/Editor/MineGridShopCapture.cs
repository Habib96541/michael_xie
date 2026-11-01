using System;
using System.Collections;
using System.IO;
using MineGrid;
using UnityEditor;
using UnityEngine;

public static class MineGridShopCapture
{
    public static void Run()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new Exception("Enter Play Mode first.");
        }

        var ui = UnityEngine.Object.FindFirstObjectByType<GameUI>();
        ui.StartCoroutine(Capture(ui));
    }

    private static IEnumerator Capture(GameUI ui)
    {
        const string folder = "SprintDoc/Phase1Sprint5-Captures";
        Directory.CreateDirectory(folder);
        Application.runInBackground = true;
        File.WriteAllText("Library/MineGrid-capturing.flag", "shop");
        SaveService saves = ui.Persistence;
        string backup = JsonUtility.ToJson(saves.Data);
        int checks = 0, textChecks = 0;
        Action<bool, string> check = (valid, message) =>
        {
            if (!valid)
            {
                throw new Exception(message);
            }

            checks++;
        };
        try
        {
            ui.ClosePopup();
            ui.Home();
            saves.Data.coins = 10000;
            saves.Data.reducedEffects = true;
            saves.Data.ownedCosmetics.Clear();
            saves.Data.equippedCosmetics.Clear();
            ui.Cosmetics.Normalize();
            saves.Data.reducedEffects = false;
            ui.Shop();
            ui.ShopScreen.SelectCategory(CosmeticCategory.Gem);
            string before = JsonUtility.ToJson(saves.Data);
            ui.ShopScreen.SelectItem("gem.ruby");
            check(JsonUtility.ToJson(saves.Data) == before, "preview does not mutate save");
            int[, ] sizes =
            {
                {
                    360,
                    800
                },
                {
                    720,
                    1280
                },
                {
                    1280,
                    720
                },
                {
                    1600,
                    900
                }
            };
            for (int i = 0; i < 4; i++)
            {
                MineGridCapture.Resize(sizes[i, 0], sizes[i, 1]);
                yield return new WaitForSecondsRealtime(.6f);
                yield return new WaitForEndOfFrame();
                textChecks += MineGridVisualAudit.Inspect(ui);
                ScreenCapture.CaptureScreenshot(folder + $"/Shop-{sizes[i, 0]}x{sizes[i, 1]}.png");
                check(ui.ShopPage.activeSelf, "shop at target resolution");
                yield return new WaitForSecondsRealtime(.2f);
            }

            MineGridCapture.Resize(720, 1280);
            yield return new WaitForSecondsRealtime(.5f);
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                ui.ShopScreen.SelectCategory(category);
                yield return new WaitForSecondsRealtime(.2f);
                yield return new WaitForEndOfFrame();
                textChecks += MineGridVisualAudit.Inspect(ui);
                ScreenCapture.CaptureScreenshot(folder + "/Category-" + category + ".png");
                check(ui.ShopScreen.Selected.Category == category, "category selection");
            }

            ui.ShopScreen.SelectCategory(CosmeticCategory.Gem);
            ui.ShopScreen.SelectItem("gem.ruby");
            ui.ShopScreen.ActionControl.onClick.Invoke();
            check(saves.Data.coins == 9500 && ui.Cosmetics.Owns("gem.ruby") && ui.IsPopupOpen, "purchase popup and exact spend");
            ui.ShopScreen.ActionControl.onClick.Invoke();
            check(saves.Data.coins == 9500, "duplicate UI buy blocked");
            ui.ShopScreen.SelectItem("gem.emerald");
            check(ui.ShopScreen.Selected.Id == "gem.ruby", "modal blocks selection");
            yield return new WaitForSecondsRealtime(.8f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot(folder + "/Purchase.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.PopupPrimary.onClick.Invoke();
            check(ui.Cosmetics.Equipped(CosmeticCategory.Gem).Id == "gem.ruby" && saves.Data.coins == 9500, "equip callback free");
            foreach (TileView tile in ui.TileViews)
            {
                check(tile.GemMaterial == ui.Cosmetics.Equipped(CosmeticCategory.Gem).Material, "gem applied");
            }

            foreach (string id in new[]
            {
                "board.frozen",
                "tile.frosted",
                "reveal.stars"
            }

            )
            {
                CosmeticItem item = ui.Cosmetics.Catalog.Find(id);
                ui.ShopScreen.SelectCategory(item.Category);
                ui.ShopScreen.SelectItem(id);
                ui.ShopScreen.ActionControl.onClick.Invoke();
                ui.PopupPrimary.onClick.Invoke();
                check(ui.Cosmetics.Equipped(item.Category).Id == id, "category equipped");
            }

            before = JsonUtility.ToJson(saves.Data);
            ui.ShopScreen.Preview.PlayEffect();
            yield return new WaitForSecondsRealtime(.15f);
            yield return new WaitForEndOfFrame();
            check(JsonUtility.ToJson(saves.Data) == before, "effect preview is presentation only");
            ScreenCapture.CaptureScreenshot(folder + "/EffectPreview.png");
            yield return new WaitForSecondsRealtime(.7f);
            ui.Home();
            ui.Play();
            ui.MineSelector.value = 3;
            ui.StartControl.onClick.Invoke();
            int reveals = 0;
            for (int i = 0; i < 25 && reveals < 4; i++)
            {
                if (!ui.CurrentRound.Board.IsMine(i))
                {
                    ui.TileViews[i].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    reveals++;
                }
            }

            check(ui.CurrentRound.Score == 120, "cosmetic gameplay score");
            check(ui.TileViews[0].HiddenSprite == ui.Cosmetics.Equipped(CosmeticCategory.Tile).Artwork, "tile applied");
            check(ui.TileViews[0].ActiveRevealStyle == RevealStyle.Starburst, "effect applied");
            yield return new WaitForSecondsRealtime(.8f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot(folder + "/CustomizedGameplay.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.FinishControl.onClick.Invoke();
            ui.Again();
            ui.Home();
            int ownership = saves.Data.ownedCosmetics.Count;
            ui.Cosmetics.RestoreDefaults();
            check(saves.Data.ownedCosmetics.Count == ownership, "restore retains ownership");
            foreach (CosmeticCategory category in Enum.GetValues(typeof(CosmeticCategory)))
            {
                check(ui.Cosmetics.Equipped(category).IsDefault, "default category restored");
            }

            File.WriteAllText("MineGrid-shop-ui-verification.txt", $"PASS: {checks} shop UI/application checks and {textChecks} active text checks. Four screen sizes, categories, pure previews, exact purchase, duplicate/modal blocking, free equip, all tile material bindings, board/tile/effect application, scoring and restore. Save restored.\n");
            Debug.Log("MineGrid shop capture and UI checks complete.");
        }
        finally
        {
            File.Delete("Library/MineGrid-capturing.flag");
            ui.ClosePopup();
            if (ui.CurrentRound.State == RoundState.Playing)
            {
                ui.FinishControl.onClick.Invoke();
            }

            ui.Again();
            ui.ClosePopup();
            JsonUtility.FromJsonOverwrite(backup, saves.Data);
            ui.Cosmetics.Normalize();
            ui.Audio.Mute(saves.Data.muted);
            ui.MuteToggle.SetIsOnWithoutNotify(saves.Data.muted);
            ui.ReducedToggle.SetIsOnWithoutNotify(saves.Data.reducedEffects);
            ui.Home();
        }
    }
}
