using SpinTheWheel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class WheelMenuSetup
{
    private const string ScenePath = "Assets/Scenes/SpinTheWheel.unity";
    private const string ArtPath = "Assets/Art/KenneyUI/";
    private static readonly Color Ink = new Color32(35, 64, 94, 255);
    private static readonly Color Muted = new Color32(75, 106, 134, 255);
    private static readonly Color Orange = new Color32(255, 166, 72, 255);

    [MenuItem("Tools/Spin the Wheel/Apply Main Menu")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.OpenScene(ScenePath);
        WheelResponsiveLayout gameLayout = Object.FindFirstObjectByType<WheelResponsiveLayout>();
        if (Object.FindFirstObjectByType<WheelMenuController>() != null)
        {
            Debug.Log("The main menu is already installed.");
            return;
        }
        Transform gamePage = gameLayout.transform.Find("Safe Area/Viewport/Game");
        GameObject gameRoot = gameLayout.transform.Find("Safe Area").gameObject;
        WheelController wheel = gamePage.GetComponent<WheelController>();
        TMP_FontAsset font = gamePage.Find("Title").GetComponent<TMP_Text>().font;
        Button back = Button("Menu Button", gamePage, "MENU", Color.white, font);
        SerializedObject gameSettings = new SerializedObject(gameLayout);
        Set(gameSettings, "menuButton", back.transform);
        gameSettings.ApplyModifiedPropertiesWithoutUndo();

        RectTransform root = Rect("Main Menu Canvas", null);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        root.gameObject.AddComponent<GraphicRaycaster>();
        RectTransform safeArea = Rect("Safe Area", root);
        Image surface = safeArea.gameObject.AddComponent<Image>();
        surface.color = Color.clear;
        surface.raycastTarget = true;
        ScrollRect scroll = safeArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        RectTransform viewport = Rect("Viewport", safeArea);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Rect("Menu", viewport);
        scroll.viewport = viewport;
        scroll.content = content;

        TMP_Text eyebrow = Label("Eyebrow", content, "MULTIPLIER WHEEL", font, 14f, Muted);
        TMP_Text title = Label("Title", content, "Spin the Wheel", font, 48f, Ink, true);
        TMP_Text subtitle = Label("Subtitle", content, "One spin. Twenty possibilities.", font, 17f, Muted);
        TMP_Text footer = Label("Footer", content, "Every outcome has a 5% chance.", font, 14f, Muted);
        RectTransform artwork = Object.Instantiate(gamePage.Find("Wheel Assembly").gameObject, content)
            .GetComponent<RectTransform>();
        artwork.name = "Menu Wheel";
        Transform wheelArt = artwork.Find("Wheel");
        wheelArt.localRotation = Quaternion.identity;
        for (int index = wheelArt.childCount - 1; index >= 0; index--)
            if (wheelArt.GetChild(index).name.StartsWith("Multiplier "))
                Object.DestroyImmediate(wheelArt.GetChild(index).gameObject);

        RectTransform panel = Rect("Play Panel", content);
        Image shadow = Image("Panel Shadow", panel, new Color32(154, 192, 212, 255), "Panel.png");
        Image background = Image("Panel Background", panel, Color.white, "Panel.png");
        TMP_Text heading = Label("Panel Heading", panel, "Ready to spin?", font, 30f, Ink, true);
        TMP_Text description = Label("Panel Description", panel, "20 equal chances.\nMultipliers from 0.1x to 50x.", font, 17f, Muted);
        Button play = Button("Play Button", panel, "PLAY", Orange, font);
        Button quit = Button("Quit Button", panel, "QUIT", new Color32(225, 242, 251, 255), font);

        WheelMenuLayout menuLayout = root.gameObject.AddComponent<WheelMenuLayout>();
        SerializedObject settings = new SerializedObject(menuLayout);
        Set(settings, "canvas", canvas);
        Set(settings, "canvasScaler", scaler);
        Set(settings, "safeArea", safeArea);
        Set(settings, "scrollView", scroll);
        Set(settings, "content", content);
        Set(settings, "wheelGroup", artwork);
        Set(settings, "panel", panel);
        Set(settings, "eyebrow", eyebrow);
        Set(settings, "title", title);
        Set(settings, "subtitle", subtitle);
        Set(settings, "footer", footer);
        Set(settings, "panelHeading", heading);
        Set(settings, "panelDescription", description);
        Set(settings, "panelBackground", background.rectTransform);
        Set(settings, "panelShadow", shadow.rectTransform);
        Set(settings, "playButton", play.transform);
        Set(settings, "quitButton", quit.transform);
        settings.ApplyModifiedPropertiesWithoutUndo();

        WheelMenuController controller = new GameObject("Menu Controller").AddComponent<WheelMenuController>();
        settings = new SerializedObject(controller);
        Set(settings, "menuRoot", root.gameObject);
        Set(settings, "gameRoot", gameRoot);
        Set(settings, "wheel", wheel);
        Set(settings, "gameLayout", gameLayout);
        Set(settings, "playButton", play);
        Set(settings, "quitButton", quit);
        Set(settings, "menuButton", back);
        Set(settings, "spinButton", gamePage.Find("Result Panel/Spin Button").GetComponent<Button>());
        Set(settings, "eventSystem", Object.FindFirstObjectByType<EventSystem>());
        settings.ApplyModifiedPropertiesWithoutUndo();
        gameRoot.SetActive(false);
        menuLayout.RefreshLayout();
        gameLayout.RefreshLayout();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Main menu saved. Play starts at the menu; Quit stops Play mode in the Editor.");
    }

    private static void Set(SerializedObject settings, string property, Object value)
    {
        settings.FindProperty(property).objectReferenceValue = value;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image Image(string name, Transform parent, Color color, string spriteName)
    {
        Image image = Rect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + spriteName);
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, string text, TMP_FontAsset font,
        float fontSize, Color color, bool bold = false)
    {
        TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(string name, Transform parent, string text, Color color, TMP_FontAsset font)
    {
        Image image = Image(name, parent, color, "Button.png");
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        TMP_Text label = Label("Button Label", image.transform, text, font, 21f, Ink, true);
        Stretch(label.rectTransform);
        return button;
    }
}
