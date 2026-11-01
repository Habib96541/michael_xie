using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MineGrid;
using UnityEditor;
using UnityEngine;

public static class MineGridCapture
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

    public static void Resize(int width, int height)
    {
        var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var window = EditorWindow.GetWindow(type);
        window.Repaint();
        // Unity's Game View size API is internal; confine its use to QA tooling.
        var sizesType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes = singleton.GetProperty("instance").GetValue(null);
        var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
        var sizeType = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSize");
        var kind = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
        var size = Activator.CreateInstance(sizeType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { Enum.ToObject(kind, 1), width, height, "MineGrid QA" }, null);
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
        int total = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        type.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(window, total - 1);
    }

    private static IEnumerator Capture(GameUI ui)
    {
        Directory.CreateDirectory("SprintDoc/Phase1Sprint5-BaselineCaptures");
        Application.runInBackground = true;
        File.WriteAllText("Library/MineGrid-capturing.flag", "baseline");
        var save = MineGridSmoke.Field<SaveService>(ui, "saves");
        string backup = JsonUtility.ToJson(save.Data);
        save.Data.reducedEffects = false;
        int checks = 0, textChecks = 0;
        try
        {
            ui.ClosePopup();
            ui.ShowTitle();
            Resize(720, 1280);
            yield return new WaitForSecondsRealtime(.4f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Title.png");
            textChecks += MineGridVisualAudit.Inspect(ui);
            ui.Home();
            yield return new WaitForSecondsRealtime(.3f);
            ui.Play();
            MineGridSmoke.Field<UnityEngine.UI.Slider>(ui, "mineSlider").value = 3;
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
                Resize(sizes[i, 0], sizes[i, 1]);
                yield return new WaitForSecondsRealtime(.7f);
                yield return new WaitForEndOfFrame();
                textChecks += MineGridVisualAudit.Inspect(ui);
                var tiles = MineGridSmoke.Field<TileView[]>(ui, "tiles");
                var canvas = ui.GetComponent<Canvas>();
                foreach (var tile in tiles)
                {
                    var rect = (RectTransform)tile.transform;
                    if (Mathf.Abs(rect.rect.width - rect.rect.height) > .5f)
                    {
                        throw new Exception("Non-square tile");
                    }

                    var corners = new Vector3[4];
                    rect.GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        if (corner.x < 0 || corner.y < 0 || corner.x > Screen.width || corner.y > Screen.height)
                        {
                            throw new Exception("Tile outside screen");
                        }
                    }

                    checks++;
                }

                ScreenCapture.CaptureScreenshot($"SprintDoc/Phase1Sprint5-BaselineCaptures/Gameplay-{sizes[i, 0]}x{sizes[i, 1]}.png");
                yield return new WaitForSecondsRealtime(.3f);
            }

            Resize(720, 1280);
            ui.Home();
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Home.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.Wheel();
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Wheel.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.Home();
            ui.Progression();
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Milestones.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.Home();
            ui.Settings();
            yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Settings.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.ClosePopup();
            ui.Play();
            MineGridSmoke.Field<UnityEngine.UI.Slider>(ui, "mineSlider").value = 3;
            MineGridSmoke.Field<UnityEngine.UI.Button>(ui, "start").onClick.Invoke();
            ui.Home();
            yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/LeaveConfirmation.png");
            yield return new WaitForSecondsRealtime(.2f);
            MineGridSmoke.Field<UnityEngine.UI.Button>(ui, "popupAction").onClick.Invoke();
            var round = MineGridSmoke.Field<Round>(ui, "round");
            int mine = 0;
            while (!round.Board.IsMine(mine))
            {
                mine++;
            }

            MineGridSmoke.Field<TileView[]>(ui, "tiles")[mine].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(1.3f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Loss.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.Again();
            ui.Home();
            save.Data.nextSpin = 0;
            save.Data.pendingCoins = 0;
            ui.Wheel();
            MineGridSmoke.Field<UnityEngine.UI.Button>(ui, "spin").onClick.Invoke();
            yield return new WaitForSecondsRealtime(4.5f);
            if (!MineGridSmoke.Field<GameObject>(ui, "popup").activeSelf || save.Data.pendingCoins == 0)
            {
                throw new Exception("Wheel presentation failed");
            }

            checks++;
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/DailyClaim.png");
            yield return new WaitForSecondsRealtime(.2f);
            int before = save.Data.coins;
            int reward = save.Data.pendingCoins;
            MineGridSmoke.Field<UnityEngine.UI.Button>(ui, "popupAction").onClick.Invoke();
            if (save.Data.coins != before + reward || save.Data.pendingCoins != 0)
            {
                throw new Exception("Wheel UI claim failed");
            }

            checks++;
            yield return new WaitForSecondsRealtime(.8f);
            ui.Home();
            ui.Play();
            MineGridSmoke.Field<UnityEngine.UI.Slider>(ui, "mineSlider").value = 3;
            MineGridSmoke.Field<UnityEngine.UI.Button>(ui, "start").onClick.Invoke();
            var active = MineGridSmoke.Field<Round>(ui, "round");
            var activeTiles = MineGridSmoke.Field<TileView[]>(ui, "tiles");
            int reveals = 0;
            for (int i = 0; i < 25 && reveals < 4; i++)
            {
                if (!active.Board.IsMine(i))
                {
                    activeTiles[i].GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                    reveals++;
                }
            }

            yield return new WaitForSecondsRealtime(.7f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Gameplay-Active.png");
            yield return new WaitForSecondsRealtime(.2f);
            MineGridSmoke.Field<UnityEngine.UI.Button>(ui, "finish").onClick.Invoke();
            yield return new WaitForSecondsRealtime(.8f);
            yield return new WaitForEndOfFrame();
            textChecks += MineGridVisualAudit.Inspect(ui);
            ScreenCapture.CaptureScreenshot("SprintDoc/Phase1Sprint5-BaselineCaptures/Banked.png");
            yield return new WaitForSecondsRealtime(.2f);
            ui.Again();
            ui.Home();
            int assetChecks = MineGridVisualAudit.CheckAssets();
            File.WriteAllText("MineGrid-sprint5-visual-verification.txt", $"PASS: {textChecks} active text checks (overflow and glyph coverage), {assetChecks} transparent artwork alpha checks. Actual Play Mode screens captured and inspected.\n");
            JsonUtility.FromJsonOverwrite(backup, save.Data);
            save.Save();
            ui.ClosePopup();
            ui.Home();
            File.WriteAllText("MineGrid-sprint5-layout-verification.txt", $"PASS: {checks} checks. 25 square onscreen tiles at 360x800, 720x1280, 1280x720, 1600x900; normal-animation daily reward popup and claim. Captured home, wheel, milestones, settings, leave confirmation and animated loss.\n");
            Debug.Log("MineGrid QA captures complete.");
        }
        finally
        {
            File.Delete("Library/MineGrid-capturing.flag");
            JsonUtility.FromJsonOverwrite(backup, save.Data);
            save.Save();
            ui.ClosePopup();
            ui.Home();
        }
    }
}
