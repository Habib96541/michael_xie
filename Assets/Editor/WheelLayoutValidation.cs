using System;
using System.IO;
using System.Reflection;
using System.Text;
using SpinTheWheel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WheelLayoutValidation
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const string RunningKey = "WheelLayoutValidation";
    private static readonly Vector2Int[] Resolutions =
    {
        new Vector2Int(320, 568), new Vector2Int(390, 844), new Vector2Int(1080, 1920),
        new Vector2Int(768, 1024), new Vector2Int(1024, 768), new Vector2Int(1024, 1024),
        new Vector2Int(1920, 1080), new Vector2Int(2560, 1080),
        new Vector2Int(568, 320), new Vector2Int(320, 320)
    };
    private static EditorWindow gameView;
    private static PropertyInfo sizeIndex;
    private static object sizeGroup;
    private static WheelResponsiveLayout layout;
    private static WheelMenuController menu;
    private static WheelMenuLayout menuLayout;
    private static RectTransform content;
    private static ScrollRect scrollView;
    private static int current;
    private static int stage;
    private static int previousIndex;
    private static double deadline;
    private static double settleTime;
    private static StringBuilder report;

    static WheelLayoutValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/Spin the Wheel/Run Layout Validation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene("Assets/Scenes/SpinTheWheel.unity");
        SessionState.SetBool(RunningKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(RunningKey, false))
            return;
        try
        {
            report = new StringBuilder("Sprint 03: real Game view resolution checks\n");
            menu = Object.FindFirstObjectByType<WheelMenuController>();
            if (menu != null)
            {
                menuLayout = Object.FindFirstObjectByType<WheelMenuLayout>();
                report = new StringBuilder("Sprint 04: menu and game resolution checks\n");
                menu.PlayGame();
            }
            layout = Object.FindFirstObjectByType<WheelResponsiveLayout>();
            Require(layout != null, "Responsive layout is attached to the scene");
            content = Field<RectTransform>("content");
            scrollView = Field<ScrollRect>("scrollView");
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView", true);
            gameView = EditorWindow.GetWindow(gameViewType);
            sizeIndex = gameViewType.GetProperty("selectedSizeIndex", Members);
            previousIndex = (int)sizeIndex.GetValue(gameView);
            Type sizesType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizes", true);
            object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
                .GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            sizeGroup = sizesType.GetProperty("currentGroup", Members).GetValue(sizes);
            current = stage = 0;
            EditorApplication.update += Tick;
        }
        catch (Exception exception)
        {
            Finish("FAIL: " + exception);
        }
    }

    private static void Tick()
    {
        try
        {
            Require(EditorApplication.isPlaying, "Play mode remains active during validation");
            EditorApplication.QueuePlayerLoopUpdate();
            gameView.Repaint();
            if (current >= Resolutions.Length)
            {
                Finish("PASS: 10 actual Game view sizes, two simulated notch layouts, and scroll reachability.\n");
                return;
            }
            Vector2Int resolution = Resolutions[current];
            if (stage == 0)
            {
                SelectResolution(resolution);
                deadline = EditorApplication.timeSinceStartup + 15f;
                settleTime = EditorApplication.timeSinceStartup + 0.5f;
                stage = 1;
                return;
            }
            Require(EditorApplication.timeSinceStartup < deadline, "Resolution settled before timeout: " + resolution);
            if (Screen.width != resolution.x || Screen.height != resolution.y || EditorApplication.timeSinceStartup < settleTime)
                return;
            if (stage == 1)
            {
                layout.RefreshLayout();
                ValidateBounds();
                if (menu != null)
                    ValidateMenu(resolution);
                if (resolution == new Vector2Int(390, 844))
                    ValidateSafeArea(new Rect(0f, 34f, 390f, 763f));
                if (resolution == new Vector2Int(568, 320))
                    ValidateSafeArea(new Rect(30f, 12f, 508f, 308f));
                report.AppendLine("PASS: " + resolution.x + "x" + resolution.y +
                    (scrollView.vertical ? " with vertical scrolling" : " fits without scrolling"));
                if (resolution == new Vector2Int(390, 844) || resolution == new Vector2Int(1920, 1080) ||
                    resolution == new Vector2Int(568, 320))
                {
                    Directory.CreateDirectory("SprintDocs");
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath($"SprintDocs/Sprint_{(menu == null ? "03" : "04")}_Game_{resolution.x}x{resolution.y}.png"));
                }
                settleTime = EditorApplication.timeSinceStartup + 0.7f;
                stage = 2;
                return;
            }
            if (stage == 2 && menu != null)
            {
                menu.ShowMenu();
                menuLayout.RefreshLayout();
                Canvas.ForceUpdateCanvases();
                if (resolution == new Vector2Int(390, 844) || resolution == new Vector2Int(1920, 1080) ||
                    resolution == new Vector2Int(568, 320))
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath($"SprintDocs/Sprint_04_Menu_{resolution.x}x{resolution.y}.png"));
                settleTime = EditorApplication.timeSinceStartup + 0.7f;
                stage = 3;
                return;
            }
            if (menu != null)
                menu.PlayGame();
            current++;
            stage = 0;
        }
        catch (Exception exception)
        {
            Finish("FAIL: " + exception);
        }
    }

    private static void ValidateBounds()
    {
        Canvas.ForceUpdateCanvases();
        RectTransform wheel = Field<RectTransform>("wheelGroup");
        RectTransform panel = Field<RectTransform>("panel");
        Bounds wheelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, wheel);
        Bounds panelBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, panel);
        Require(!Overlaps(wheelBounds, panelBounds), "Wheel and panel do not overlap");
        foreach (Bounds bounds in new[] { wheelBounds, panelBounds })
        {
            Require(bounds.min.x >= content.rect.xMin - 1f && bounds.max.x <= content.rect.xMax + 1f,
                "Body remains inside the content width");
            Require(bounds.min.y >= content.rect.yMin - 1f && bounds.max.y <= content.rect.yMax + 1f,
                "Body remains inside the scrollable content height");
        }
        foreach (TMP_Text text in content.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate();
            Require(!text.isTextOverflowing, "Text fits its rectangle: " + text.name);
        }
        foreach (TMP_Text label in Field<TMP_Text[]>("segmentLabels"))
        {
            float radius = Vector3.Distance(label.transform.position, label.transform.parent.position);
            float segmentWidth = 2f * radius * Mathf.Sin(Mathf.PI / 20f);
            Require(label.preferredWidth * label.transform.lossyScale.x + 4f <= segmentWidth,
                "Multiplier labels retain spacing: " + label.text);
        }
        float buttonHeight = Field<RectTransform>("spinButton").rect.height * layout.GetComponent<Canvas>().scaleFactor;
        Require(buttonHeight >= 44f, "Spin button remains at least 44 pixels tall");
        if (scrollView.vertical)
        {
            scrollView.verticalNormalizedPosition = 0f;
            Canvas.ForceUpdateCanvases();
            Bounds button = RectTransformUtility.CalculateRelativeRectTransformBounds(scrollView.viewport, Field<RectTransform>("spinButton"));
            Require(button.min.y >= scrollView.viewport.rect.yMin - 1f && button.max.y <= scrollView.viewport.rect.yMax + 1f,
                "Spin button can be reached by scrolling to the bottom");
            scrollView.verticalNormalizedPosition = 1f;
        }
    }

    private static bool Overlaps(Bounds first, Bounds second)
    {
        return first.min.x < second.max.x - 1f && first.max.x > second.min.x + 1f &&
            first.min.y < second.max.y - 1f && first.max.y > second.min.y + 1f;
    }

    private static void ValidateMenu(Vector2Int resolution)
    {
        menu.ShowMenu();
        menuLayout.RefreshLayout();
        ValidateMenuBounds();
        if (resolution == new Vector2Int(390, 844) || resolution == new Vector2Int(568, 320))
        {
            Rect area = resolution.x == 390 ? new Rect(0f, 34f, 390f, 763f) : new Rect(30f, 12f, 508f, 308f);
            typeof(WheelMenuLayout).GetMethod("ApplyLayout", Members).Invoke(menuLayout,
                new object[] { resolution.x, resolution.y, area });
            RectTransform safe = MenuField<RectTransform>("safeArea");
            Require(Mathf.Approximately(safe.anchorMin.x, area.xMin / resolution.x) &&
                Mathf.Approximately(safe.anchorMax.x, area.xMax / resolution.x) &&
                Mathf.Approximately(safe.anchorMin.y, area.yMin / resolution.y) &&
                Mathf.Approximately(safe.anchorMax.y, area.yMax / resolution.y), "Menu respects simulated safe area");
            ValidateMenuBounds();
            menuLayout.RefreshLayout();
        }
        report.AppendLine("PASS: menu " + resolution.x + "x" + resolution.y + ", readable text and reachable Play/Quit");
        menu.PlayGame();
    }

    private static void ValidateMenuBounds()
    {
        Canvas.ForceUpdateCanvases();
        RectTransform page = MenuField<RectTransform>("content");
        Bounds wheel = RectTransformUtility.CalculateRelativeRectTransformBounds(page, MenuField<RectTransform>("wheelGroup"));
        Bounds panel = RectTransformUtility.CalculateRelativeRectTransformBounds(page, MenuField<RectTransform>("panel"));
        Require(!Overlaps(wheel, panel), "Menu wheel and panel do not overlap");
        foreach (Bounds bounds in new[] { wheel, panel })
            Require(bounds.min.x >= page.rect.xMin - 1f && bounds.max.x <= page.rect.xMax + 1f &&
                bounds.min.y >= page.rect.yMin - 1f && bounds.max.y <= page.rect.yMax + 1f, "Menu body fits content bounds");
        foreach (TMP_Text text in page.GetComponentsInChildren<TMP_Text>())
        {
            text.ForceMeshUpdate();
            Require(!text.isTextOverflowing, "Menu text fits: " + text.name);
        }
        ScrollRect scroll = MenuField<ScrollRect>("scrollView");
        if (scroll.vertical)
            scroll.verticalNormalizedPosition = 0f;
        Canvas.ForceUpdateCanvases();
        foreach (string name in new[] { "playButton", "quitButton" })
        {
            RectTransform button = MenuField<RectTransform>(name);
            Require(button.rect.height * menuLayout.GetComponent<Canvas>().scaleFactor >= 44f, "Menu controls retain touch size");
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(scroll.viewport, button);
            Require(bounds.min.y >= scroll.viewport.rect.yMin - 1f && bounds.max.y <= scroll.viewport.rect.yMax + 1f,
                "Play and Quit can both be reached");
        }
        scroll.verticalNormalizedPosition = 1f;
    }

    private static T MenuField<T>(string name)
    {
        return (T)typeof(WheelMenuLayout).GetField(name, Members).GetValue(menuLayout);
    }

    private static void ValidateSafeArea(Rect area)
    {
        typeof(WheelResponsiveLayout).GetMethod("ApplyLayout", Members).Invoke(layout,
            new object[] { Screen.width, Screen.height, area });
        RectTransform safe = Field<RectTransform>("safeArea");
        Require(Mathf.Approximately(safe.anchorMin.x, area.xMin / Screen.width) &&
            Mathf.Approximately(safe.anchorMax.x, area.xMax / Screen.width) &&
            Mathf.Approximately(safe.anchorMin.y, area.yMin / Screen.height) &&
            Mathf.Approximately(safe.anchorMax.y, area.yMax / Screen.height), "Safe-area anchors follow the notch insets");
        ValidateBounds();
        RectTransform menuButton = Field<RectTransform>("menuButton");
        if (menuButton != null)
            Require(menuButton.rect.height * layout.GetComponent<Canvas>().scaleFactor >= 44f, "Menu return button retains touch size");
        report.AppendLine("PASS: simulated safe area " + area);
        layout.RefreshLayout();
        Canvas.ForceUpdateCanvases();
    }

    private static void SelectResolution(Vector2Int resolution)
    {
        Type groupType = sizeGroup.GetType();
        int count = (int)groupType.GetMethod("GetTotalCount").Invoke(sizeGroup, null);
        for (int index = 0; index < count; index++)
        {
            object size = groupType.GetMethod("GetGameViewSize").Invoke(sizeGroup, new object[] { index });
            if ((int)size.GetType().GetProperty("width").GetValue(size) == resolution.x &&
                (int)size.GetType().GetProperty("height").GetValue(size) == resolution.y)
            {
                sizeIndex.SetValue(gameView, index);
                return;
            }
        }
        Type sizeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSize", true);
        Type modeType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameViewSizeType", true);
        object customSize = Activator.CreateInstance(sizeType,
            new[] { Enum.Parse(modeType, "FixedResolution"), (object)resolution.x, resolution.y, "Wheel layout check" });
        groupType.GetMethod("AddCustomSize").Invoke(sizeGroup, new[] { customSize });
        sizeIndex.SetValue(gameView, count);
    }

    private static T Field<T>(string name)
    {
        return (T)typeof(WheelResponsiveLayout).GetField(name, Members).GetValue(layout);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void Finish(string message)
    {
        EditorApplication.update -= Tick;
        SessionState.SetBool(RunningKey, false);
        report ??= new StringBuilder();
        report.AppendLine(message);
        Directory.CreateDirectory("SprintDocs");
        File.WriteAllText($"SprintDocs/Sprint_{(menu == null ? "03" : "04")}_Layout_Validation.txt", report.ToString());
        Debug.Log(report.ToString());
        if (gameView != null && sizeIndex != null)
            sizeIndex.SetValue(gameView, previousIndex);
        EditorApplication.ExitPlaymode();
    }
}
