using System.IO;
using SpinTheWheel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class WheelSceneSetup
{
    private const string ScenePath = "Assets/Scenes/SpinTheWheel.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string PanelPath = "Assets/Art/KenneyUI/Panel.png";
    private const string ButtonPath = "Assets/Art/KenneyUI/Button.png";
    private const string FramePath = "Assets/Art/BrightArcade/WheelFrame.png";
    private const string PointerPath = "Assets/Art/BrightArcade/Pointer.png";
    private const string AudioPath = "Assets/Audio/";
    private static readonly Color Background = new Color32(219, 242, 255, 255);
    private static readonly Color Ink = new Color32(35, 64, 94, 255);
    private static readonly Color Muted = new Color32(75, 106, 134, 255);
    private static readonly Color Accent = new Color32(17, 120, 168, 255);
    private static readonly Color Orange = new Color32(255, 166, 72, 255);
    private static readonly Color Divider = new Color32(175, 211, 230, 255);

    [MenuItem("Tools/Spin the Wheel/Create Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
        {
            AssetDatabase.importPackageCompleted -= OnResourcesImported;
            AssetDatabase.importPackageCompleted += OnResourcesImported;
            string packagePath = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly).resolvedPath;
            AssetDatabase.ImportPackage(Path.Combine(packagePath, "Package Resources/TMP Essential Resources.unitypackage"), false);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (File.Exists(ScenePath))
        {
            EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Opened the existing SpinTheWheel scene. It was not overwritten.");
            return;
        }

        ConfigureArtwork();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();
        RectTransform canvas = CreateCanvas();
        BuildInterface(canvas);
        StyleInterface(canvas.Find("Game"));
        ConfigureResponsiveLayout(canvas.GetComponent<Canvas>());
        CreateEventSystem();

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("SpinTheWheel scene created. Press Play, then click SPIN.");
    }

    [MenuItem("Tools/Spin the Wheel/Apply Bright Artwork")]
    public static void ApplyBrightArtwork()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        ConfigureArtwork();
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.TryGetComponent(out Camera camera))
                camera.backgroundColor = Background;
            if (root.TryGetComponent(out Canvas canvas))
            {
                StyleInterface(FindPage(canvas));
                ConfigureResponsiveLayout(canvas);
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Bright artwork applied. The scene is ready to play.");
    }

    [MenuItem("Tools/Spin the Wheel/Apply Responsive Layout")]
    public static void ApplyResponsiveLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.TryGetComponent(out Canvas canvas))
                ConfigureResponsiveLayout(canvas);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Responsive layout applied. Resize the Game view to check it.");
    }

    private static Transform FindPage(Canvas canvas)
    {
        Transform page = canvas.transform.Find("Game");
        return page != null ? page : canvas.transform.Find("Safe Area/Viewport/Game");
    }

    private static void ConfigureResponsiveLayout(Canvas canvas)
    {
        RectTransform page = FindPage(canvas).GetComponent<RectTransform>();
        RectTransform safeArea = canvas.transform.Find("Safe Area") as RectTransform;
        if (safeArea == null)
        {
            UnityEngine.UI.Image surface = Image("Safe Area", canvas.transform, Vector2.zero, Vector2.zero, Color.clear);
            surface.raycastTarget = true;
            safeArea = surface.rectTransform;
            RectTransform viewport = Rect("Viewport", safeArea, Vector2.zero, Vector2.zero);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            ScrollRect scroll = safeArea.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = page;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45f;
            page.SetParent(viewport, false);
        }

        RectTransform wheelGroup = page.Find("Wheel Assembly") as RectTransform;
        if (wheelGroup == null)
        {
            wheelGroup = Rect("Wheel Assembly", page, Vector2.zero, new Vector2(600f, 640f));
            foreach (string name in new[] { "Wheel", "Hub", "Hub Caption", "Fixed Pointer" })
            {
                RectTransform child = page.Find(name).GetComponent<RectTransform>();
                child.SetParent(wheelGroup, false);
                child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
                child.anchoredPosition = name == "Hub" ? new Vector2(0f, 10f)
                    : name == "Hub Caption" ? new Vector2(0f, -21f)
                    : name == "Fixed Pointer" ? new Vector2(0f, 273f) : Vector2.zero;
            }
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        if (!canvas.TryGetComponent(out WheelResponsiveLayout layout))
            layout = canvas.gameObject.AddComponent<WheelResponsiveLayout>();
        SerializedObject settings = new SerializedObject(layout);
        Reference(settings, "canvas", canvas);
        Reference(settings, "canvasScaler", scaler);
        Reference(settings, "scrollView", safeArea.GetComponent<ScrollRect>());
        Reference(settings, "safeArea", safeArea);
        Reference(settings, "content", page);
        Reference(settings, "wheelGroup", wheelGroup);
        Reference(settings, "panel", page.Find("Result Panel"));
        foreach (string[] entry in new[]
        {
            new[] { "eyebrow", "Eyebrow" }, new[] { "title", "Title" }, new[] { "subtitle", "Subtitle" },
            new[] { "footer", "Footer" }, new[] { "resultHeading", "Result Panel/Result Heading" },
            new[] { "result", "Result Panel/Result" }, new[] { "status", "Result Panel/Status" },
            new[] { "buttonLabel", "Result Panel/Spin Button/Button Label" },
            new[] { "odds", "Result Panel/Odds" }, new[] { "range", "Result Panel/Range" }
        })
            Reference(settings, entry[0], page.Find(entry[1]).GetComponent<TMP_Text>());
        foreach (string[] entry in new[]
        {
            new[] { "headerDivider", "Header Divider" }, new[] { "footerDivider", "Footer Divider" },
            new[] { "panelBackground", "Result Panel/Panel Background" }, new[] { "panelShadow", "Result Panel/Panel Shadow" },
            new[] { "panelAccent", "Result Panel/Accent" }, new[] { "panelDivider", "Result Panel/Panel Divider" },
            new[] { "spinButton", "Result Panel/Spin Button" }
        })
            Reference(settings, entry[0], page.Find(entry[1]));
        SerializedProperty labels = settings.FindProperty("segmentLabels");
        labels.arraySize = 20;
        for (int index = 0; index < 20; index++)
            labels.GetArrayElementAtIndex(index).objectReferenceValue = wheelGroup.Find("Wheel/Multiplier " + index).GetComponent<TMP_Text>();
        settings.ApplyModifiedPropertiesWithoutUndo();
        layout.RefreshLayout();
    }

    private static void Reference(SerializedObject settings, string name, Object value)
    {
        settings.FindProperty(name).objectReferenceValue = value;
    }

    [MenuItem("Tools/Spin the Wheel/Capture Preview")]
    public static void CapturePreview()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("Enter Play mode before capturing a preview.");
            return;
        }
        Directory.CreateDirectory("SprintDocs");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath($"SprintDocs/Sprint_03_{Screen.width}x{Screen.height}.png"));
    }

    private static void ConfigureArtwork()
    {
        ImportSprite(PanelPath, new Vector4(16f, 16f, 16f, 16f));
        ImportSprite(ButtonPath, new Vector4(16f, 20f, 16f, 16f));
        ImportSprite(FramePath, Vector4.zero);
        ImportSprite(PointerPath, Vector4.zero);
    }

    private static void ImportSprite(string path, Vector4 border)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = border;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static void StyleInterface(Transform page)
    {
        foreach (TMP_Text label in page.GetComponentsInChildren<TMP_Text>(true))
            label.color = Ink;
        foreach (string path in new[] { "Subtitle", "Footer", "Result Panel/Odds", "Result Panel/Range", "Result Panel/Result Heading" })
            page.Find(path).GetComponent<TMP_Text>().color = Muted;
        foreach (string path in new[] { "Eyebrow", "Hub Caption", "Result Panel/Result" })
            page.Find(path).GetComponent<TMP_Text>().color = Accent;

        SetImage(page, "Header Divider", Divider);
        SetImage(page, "Footer Divider", Divider);
        SetImage(page, "Result Panel/Panel Divider", Divider);
        SetImage(page, "Result Panel/Accent", Accent);
        SetImage(page, "Result Panel/Panel Background", Color.white, PanelPath);
        SetImage(page, "Result Panel/Spin Button", Orange, ButtonPath);
        Transform assembly = page.Find("Wheel Assembly");
        if (assembly == null)
            assembly = page;
        RectTransform wheel = assembly.Find("Wheel").GetComponent<RectTransform>();
        UnityEngine.UI.Image pointer = SetImage(assembly, "Fixed Pointer", Color.white, PointerPath);
        pointer.rectTransform.sizeDelta = new Vector2(70f, 86f);
        pointer.rectTransform.anchoredPosition = wheel.anchoredPosition + new Vector2(0f, 273f);

        if (wheel.Find("Wheel Frame") == null)
            Image("Wheel Frame", wheel, Vector2.zero, new Vector2(600f, 600f), Color.white);
        SetImage(wheel, "Wheel Frame", Color.white, FramePath);
        wheel.GetComponent<WheelGraphic>().SetAllDirty();

        RectTransform card = page.Find("Result Panel").GetComponent<RectTransform>();
        if (card.Find("Panel Shadow") == null)
        {
            UnityEngine.UI.Image shadow = Image("Panel Shadow", card, new Vector2(0f, -7f),
                card.sizeDelta, new Color32(154, 192, 212, 255));
            shadow.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelPath);
            shadow.type = UnityEngine.UI.Image.Type.Sliced;
            shadow.transform.SetAsFirstSibling();
        }
    }

    private static UnityEngine.UI.Image SetImage(Transform parent, string path, Color tint, string spritePath = null)
    {
        UnityEngine.UI.Image image = parent.Find(path).GetComponent<UnityEngine.UI.Image>();
        image.color = tint;
        if (spritePath != null)
        {
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            image.type = spritePath == PanelPath || spritePath == ButtonPath
                ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = image.type == UnityEngine.UI.Image.Type.Simple;
        }
        return image;
    }

    private static void OnResourcesImported(string packageName)
    {
        if (packageName != "TMP Essential Resources")
            return;

        AssetDatabase.importPackageCompleted -= OnResourcesImported;
        EditorApplication.delayCall += CreateScene;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;
    }

    private static RectTransform CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Wheel Canvas", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        return canvasObject.GetComponent<RectTransform>();
    }

    private static void BuildInterface(RectTransform canvas)
    {
        RectTransform page = Rect("Game", canvas, Vector2.zero, new Vector2(1280f, 900f));
        Label("Eyebrow", page, "MULTIPLIER WHEEL", new Vector2(-530f, 378f), new Vector2(550f, 28f),
            16f, Accent, TextAlignmentOptions.MidlineLeft);
        Label("Title", page, "Spin the Wheel", new Vector2(-530f, 326f), new Vector2(850f, 72f),
            52f, Ink, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        Label("Subtitle", page, "One spin. Twenty possibilities. Find your multiplier.",
            new Vector2(-530f, 269f), new Vector2(1000f, 40f), 19f, Muted, TextAlignmentOptions.MidlineLeft);
        Image("Header Divider", page, new Vector2(0f, 225f), new Vector2(1060f, 1f), Divider);

        Vector2 wheelPosition = new Vector2(-247f, -105f);
        RectTransform wheel = Rect("Wheel", page, wheelPosition, new Vector2(580f, 580f));
        WheelGraphic wheelGraphic = wheel.gameObject.AddComponent<WheelGraphic>();
        wheelGraphic.raycastTarget = false;

        WheelController controller = page.gameObject.AddComponent<WheelController>();
        AudioSource sfx = page.gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.loop = false;
        sfx.spatialBlend = 0f;
        sfx.volume = 1f;
        SerializedObject settings = new SerializedObject(controller);
        SerializedProperty values = settings.FindProperty("multipliers");
        TMP_Text[] labels = new TMP_Text[values.arraySize];
        for (int index = 0; index < labels.Length; index++)
        {
            float degrees = 90f - index * 360f / labels.Length;
            float radians = degrees * Mathf.Deg2Rad;
            Vector2 position = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * 222f;
            float multiplier = values.GetArrayElementAtIndex(index).floatValue;
            labels[index] = Label("Multiplier " + index, wheel, WheelController.FormatMultiplier(multiplier),
                position, new Vector2(82f, 28f), 21f,
                Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            labels[index].rectTransform.localRotation = Quaternion.Euler(0f, 0f, degrees - 90f);
        }

        Label("Hub", page, "20", wheelPosition + new Vector2(0f, 10f), new Vector2(90f, 45f),
            36f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Label("Hub Caption", page, "OUTCOMES", wheelPosition + new Vector2(0f, -21f), new Vector2(100f, 20f),
            11f, Accent, TextAlignmentOptions.Center);

        UnityEngine.UI.Image pointer = Image("Fixed Pointer", page, wheelPosition + new Vector2(0f, 273f),
            new Vector2(70f, 86f), Color.white);
        pointer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PointerPath);

        RectTransform card = Rect("Result Panel", page, new Vector2(345f, -90f), new Vector2(350f, 490f));
        Image("Panel Background", card, Vector2.zero, card.sizeDelta, Color.white);
        Image("Accent", card, new Vector2(0f, 244f), new Vector2(350f, 2f), Accent);
        Label("Result Heading", card, "YOUR MULTIPLIER", new Vector2(0f, 184f), new Vector2(300f, 30f),
            15f, Muted, TextAlignmentOptions.Center);
        TMP_Text result = Label("Result", card, "--", new Vector2(0f, 102f), new Vector2(310f, 110f),
            78f, Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        TMP_Text status = Label("Status", card, "Press SPIN to reveal your multiplier.",
            new Vector2(0f, 6f), new Vector2(280f, 65f), 19f, Ink, TextAlignmentOptions.Center);
        Image("Panel Divider", card, new Vector2(0f, -55f), new Vector2(286f, 1f), Divider);

        UnityEngine.UI.Image buttonImage = Image("Spin Button", card, new Vector2(0f, -111f), new Vector2(286f, 68f), Orange);
        buttonImage.raycastTarget = true;
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.85f, 1f, 0.95f);
        colors.pressedColor = new Color(0.6f, 0.8f, 0.75f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.4f, 0.5f, 0.5f, 0.6f);
        button.colors = colors;
        TMP_Text buttonLabel = Label("Button Label", buttonImage.rectTransform, "SPIN", Vector2.zero,
            new Vector2(270f, 60f), 21f, Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        Label("Odds", card, "20 outcomes  /  5% chance each", new Vector2(0f, -183f), new Vector2(310f, 30f),
            15f, Muted, TextAlignmentOptions.Center);
        Label("Range", card, "Multipliers from 0.1x to 50x", new Vector2(0f, -211f), new Vector2(310f, 28f),
            14f, Muted, TextAlignmentOptions.Center);

        Image("Footer Divider", page, new Vector2(0f, -410f), new Vector2(1060f, 1f), Divider);
        Label("Footer", page, "Every segment has the same chance. Your result appears when the wheel stops.",
            new Vector2(0f, -433f), new Vector2(1100f, 28f), 14f, Muted, TextAlignmentOptions.Center);

        settings.FindProperty("wheel").objectReferenceValue = wheel;
        settings.FindProperty("spinButton").objectReferenceValue = button;
        settings.FindProperty("buttonLabel").objectReferenceValue = buttonLabel;
        settings.FindProperty("resultLabel").objectReferenceValue = result;
        settings.FindProperty("statusLabel").objectReferenceValue = status;
        SerializedProperty serializedLabels = settings.FindProperty("segmentLabels");
        serializedLabels.arraySize = labels.Length;
        for (int index = 0; index < labels.Length; index++)
            serializedLabels.GetArrayElementAtIndex(index).objectReferenceValue = labels[index];
        settings.FindProperty("sfxSource").objectReferenceValue = sfx;
        settings.FindProperty("clickClip").objectReferenceValue = Clip("ui_click");
        settings.FindProperty("spinClip").objectReferenceValue = Clip("wheel_spin");
        settings.FindProperty("tickClip").objectReferenceValue = Clip("wheel_tick");
        settings.FindProperty("winClip").objectReferenceValue = Clip("wheel_win");
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        InputSystemUIInputModule module = eventSystem.GetComponent<InputSystemUIInputModule>();
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        module.actionsAsset = actions;
        module.point = InputActionReference.Create(actions.FindAction("UI/Point", true));
        module.leftClick = InputActionReference.Create(actions.FindAction("UI/Click", true));
        module.scrollWheel = InputActionReference.Create(actions.FindAction("UI/ScrollWheel", true));
        module.move = InputActionReference.Create(actions.FindAction("UI/Navigate", true));
        module.submit = InputActionReference.Create(actions.FindAction("UI/Submit", true));
        module.cancel = InputActionReference.Create(actions.FindAction("UI/Cancel", true));
    }

    private static AudioClip Clip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + name + ".ogg");
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 position, Vector2 size, Color tint)
    {
        UnityEngine.UI.Image image = Rect(name, parent, position, size).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = tint;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text Label(string name, Transform parent, string text, Vector2 position, Vector2 size,
        float fontSize, Color tint, TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI label = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = tint;
        label.alignment = alignment;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.Normal;
        if (alignment == TextAlignmentOptions.MidlineLeft)
            label.rectTransform.pivot = new Vector2(0f, 0.5f);
        return label;
    }

}
