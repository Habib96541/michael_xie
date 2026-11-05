using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Watermelon;

public static class WatermelonSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Watermelon.unity";
    private const string ArtPath = "Assets/WatermelonArt";
    private static readonly Color Ink = new(0.055f, 0.09f, 0.105f);
    private static readonly Color Cream = new(0.94f, 0.96f, 0.85f);
    private static readonly Color Coral = new(1f, 0.32f, 0.35f);

    [InitializeOnLoadMethod]
    private static void ScheduleInitialBuild()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                CreateScene();
        };
    }

    [MenuItem("Watermelon/Create Playable Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Directory.CreateDirectory(ArtPath);
        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.57f);
        Material rind = CreateMaterial("Watermelon Rind", Color.white, 0.32f);
        rind.mainTexture = CreateRindTexture();
        Material flesh = CreateMaterial("Watermelon Flesh", Coral, 0.2f);
        Material paleRind = CreateMaterial("Pale Rind", new Color(0.78f, 0.92f, 0.52f), 0.15f);
        Material dark = CreateMaterial("Stem", new Color(0.15f, 0.24f, 0.08f), 0.15f);
        Material platform = CreateMaterial("Platform", new Color(0.13f, 0.22f, 0.2f), 0.1f);
        Color[] bandColors = { new(1f, 0.77f, 0.28f), new(1f, 0.37f, 0.42f), new(0.36f, 0.84f, 0.88f), new(0.77f, 0.58f, 0.98f) };
        var bandMaterials = new Material[bandColors.Length];
        for (int index = 0; index < bandColors.Length; index++)
            bandMaterials[index] = CreateMaterial($"Band {index + 1}", bandColors[index], 0.3f);

        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(UniversalAdditionalCameraData));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 3.4f, -10f);
        cameraObject.transform.LookAt(new Vector3(0f, 1.5f, 0f));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Ink;
        camera.orthographic = true;
        camera.orthographicSize = 4.6f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 40f;
        Light key = new GameObject("Key Light", typeof(Light)).GetComponent<Light>();
        key.type = LightType.Directional;
        key.intensity = 2.2f;
        key.color = new Color(1f, 0.94f, 0.77f);
        key.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
        key.shadows = LightShadows.Soft;
        Light fill = new GameObject("Fill Light", typeof(Light)).GetComponent<Light>();
        fill.type = LightType.Directional;
        fill.intensity = 0.8f;
        fill.color = new Color(0.56f, 0.8f, 1f);
        fill.transform.rotation = Quaternion.Euler(25f, 150f, 0f);

        GameObject stand = Primitive("Display Plinth", PrimitiveType.Cylinder, null, platform);
        stand.transform.position = new Vector3(0f, -0.05f, 0f);
        stand.transform.localScale = new Vector3(3.1f, 0.14f, 2.8f);

        GameObject root = new("Watermelon");
        root.transform.position = new Vector3(0f, 1.33f, 0f);
        WatermelonVisual visual = root.AddComponent<WatermelonVisual>();
        Transform whole = new GameObject("Whole Watermelon").transform;
        whole.SetParent(root.transform, false);
        GameObject body = Primitive("Striped Body", PrimitiveType.Sphere, whole, rind);
        body.transform.localScale = new Vector3(2f, 2.46f, 1.9f);
        GameObject stem = Primitive("Stem", PrimitiveType.Capsule, whole, dark);
        stem.transform.localPosition = new Vector3(0f, 1.25f, 0f);
        stem.transform.localScale = new Vector3(0.09f, 0.13f, 0.09f);
        stem.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
        Transform bandsRoot = new GameObject("Bands").transform;
        bandsRoot.SetParent(whole, false);

        var ring = new GameObject("Rubber Band", typeof(MeshFilter), typeof(MeshRenderer));
        Mesh ringMesh = CreateRingMesh();
        string ringPath = $"{ArtPath}/Rubber Band.asset";
        Mesh savedRing = AssetDatabase.LoadAssetAtPath<Mesh>(ringPath);
        if (savedRing == null)
            AssetDatabase.CreateAsset(ringMesh, ringPath);
        else
        {
            EditorUtility.CopySerialized(ringMesh, savedRing);
            Object.DestroyImmediate(ringMesh);
            ringMesh = savedRing;
        }
        ring.GetComponent<MeshFilter>().sharedMesh = ringMesh;
        ring.GetComponent<MeshRenderer>().sharedMaterial = bandMaterials[0];
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ring, "Assets/Prefabs/RubberBand.prefab");
        Object.DestroyImmediate(ring);

        Transform fragmentsRoot = new GameObject("Burst Fragments").transform;
        fragmentsRoot.SetParent(root.transform, false);
        var fragments = new Rigidbody[18];
        for (int index = 0; index < fragments.Length; index++)
        {
            float angle = index * 2.399963f;
            float height = Mathf.Lerp(-0.75f, 0.75f, (index + 0.5f) / fragments.Length);
            float radius = Mathf.Sqrt(1f - height * height) * 0.7f;
            Transform chunk = new GameObject($"Fragment {index + 1:00}").transform;
            chunk.SetParent(fragmentsRoot, false);
            chunk.localPosition = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            chunk.localRotation = Quaternion.Euler(index * 37f, index * 73f, index * 19f);
            GameObject piece = Primitive("Flesh", PrimitiveType.Cube, chunk, flesh);
            piece.transform.localScale = new Vector3(0.37f, 0.43f, 0.34f);
            GameObject pith = Primitive("Pith", PrimitiveType.Cube, chunk, paleRind);
            pith.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            pith.transform.localScale = new Vector3(0.39f, 0.06f, 0.36f);
            GameObject skin = Primitive("Skin", PrimitiveType.Cube, chunk, rind);
            skin.transform.localPosition = new Vector3(0f, -0.245f, 0f);
            skin.transform.localScale = new Vector3(0.39f, 0.04f, 0.36f);
            BoxCollider collider = chunk.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.39f, 0.5f, 0.36f);
            Rigidbody rigidbody = chunk.gameObject.AddComponent<Rigidbody>();
            rigidbody.mass = 0.1f;
            rigidbody.isKinematic = true;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            fragments[index] = rigidbody;
        }
        fragmentsRoot.gameObject.SetActive(false);

        GameObject controller = new("Round Controller");
        WatermelonGame game = controller.AddComponent<WatermelonGame>();
        ConfigureAudio(game);
        WatermelonUI ui = CreateInterface(game);
        var visualData = new SerializedObject(visual);
        SetReference(visualData, "wholeWatermelon", whole);
        SetReference(visualData, "bandsRoot", bandsRoot);
        SetReference(visualData, "bandPrefab", prefab);
        SetReference(visualData, "fragmentsRoot", fragmentsRoot);
        SetArray(visualData, "fragments", fragments);
        SetArray(visualData, "bandMaterials", bandMaterials);
        visualData.ApplyModifiedPropertiesWithoutUndo();
        var gameData = new SerializedObject(game);
        SetReference(gameData, "visual", visual);
        SetReference(gameData, "userInterface", ui);
        gameData.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = controller;
        Debug.Log("Watermelon scene is ready. Open Assets/Scenes/Watermelon.unity and press Play.");
    }

    [MenuItem("Watermelon/Set Up Sound Effects")]
    public static void SetUpSoundEffects()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("Leave Play Mode before setting up sound effects.");
            return;
        }

        WatermelonGame game = Object.FindFirstObjectByType<WatermelonGame>();
        if (game == null)
        {
            Debug.LogError("Open the Watermelon scene before setting up sound effects.");
            return;
        }

        ConfigureAudio(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        EditorSceneManager.SaveScene(game.gameObject.scene);
        Debug.Log("Watermelon sound effects are connected and saved.");
    }

    private static void ConfigureAudio(WatermelonGame game)
    {
        AudioClip bandClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BandTwang.wav");
        AudioClip burstClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/WatermelonBurst.wav");
        if (bandClip == null || burstClip == null)
            throw new FileNotFoundException("The two watermelon WAV clips must be present in Assets/Audio.");

        if (!game.TryGetComponent(out WatermelonAudio feedback))
            feedback = Undo.AddComponent<WatermelonAudio>(game.gameObject);
        game.TryGetComponent(out AudioSource source);
        Undo.RecordObject(source, "Configure watermelon sound effects");
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;

        var audioData = new SerializedObject(feedback);
        SetReference(audioData, "bandClip", bandClip);
        SetReference(audioData, "burstClip", burstClip);
        audioData.ApplyModifiedProperties();
        var gameData = new SerializedObject(game);
        SetReference(gameData, "audioFeedback", feedback);
        gameData.ApplyModifiedProperties();
    }

    [MenuItem("Watermelon/Set Up Main Menu")]
    public static void SetUpMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        WatermelonUI ui = Object.FindFirstObjectByType<WatermelonUI>();
        if (ui == null)
        {
            Debug.LogError("Open the Watermelon scene before setting up its main menu.");
            return;
        }

        ConfigureMenu(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        EditorSceneManager.SaveScene(ui.gameObject.scene);
        Debug.Log("Watermelon main menu is connected and saved.");
    }

    private static void ConfigureMenu(WatermelonUI ui)
    {
        var data = new SerializedObject(ui);
        if (data.FindProperty("menuRoot").objectReferenceValue != null)
            return;

        Transform canvas = ui.transform;
        GameObject gameplay = CreateScreenRoot("Gameplay", canvas);
        string[] gameplayElements = { "Multiplier", "Multiplier Caption", "Band Count", "Round Status", "Instruction", "Add Band", "Restart" };
        foreach (string name in gameplayElements)
            canvas.Find(name).SetParent(gameplay.transform, false);

        Color green = new(0.16f, 0.27f, 0.23f);
        Button menu = CreateButton("Menu", "MENU", gameplay.transform, green);
        RectTransform menuRect = (RectTransform)menu.transform;
        menuRect.anchorMin = menuRect.anchorMax = new Vector2(0.88f, 0.85f);
        menuRect.sizeDelta = new Vector2(180f, 64f);
        Text menuCaption = menu.GetComponentInChildren<Text>();
        menuCaption.rectTransform.sizeDelta = menuRect.sizeDelta;
        menuCaption.fontSize = 24;
        menuCaption.color = Cream;

        GameObject mainMenu = CreateScreenRoot("Main Menu", canvas);
        Label("Menu Tagline", "ONE MORE BAND?", mainMenu.transform, 0.75f, 56, Coral, FontStyle.Bold);
        Label("Menu Hint", "STACK THE PRESSURE.", mainMenu.transform, 0.70f, 26, Cream);
        Label("Menu Instructions", "Add bands. Raise the multiplier.\nFind the breaking point.", mainMenu.transform, 0.26f, 31, new Color(0.67f, 0.77f, 0.68f));
        Button play = CreateButton("Play", "PLAY", mainMenu.transform, Coral);
        RectTransform playRect = (RectTransform)play.transform;
        playRect.anchorMin = playRect.anchorMax = new Vector2(0.5f, 0.16f);
        Button quit = CreateButton("Quit", "QUIT", mainMenu.transform, green);
        RectTransform quitRect = (RectTransform)quit.transform;
        quitRect.anchorMin = quitRect.anchorMax = new Vector2(0.5f, 0.075f);
        quitRect.sizeDelta = new Vector2(900f, 88f);
        Text quitCaption = quit.GetComponentInChildren<Text>();
        quitCaption.rectTransform.sizeDelta = quitRect.sizeDelta;
        quitCaption.fontSize = 30;
        quitCaption.color = Cream;

        SetReference(data, "menuRoot", mainMenu);
        SetReference(data, "gameplayRoot", gameplay);
        SetReference(data, "playButton", play);
        SetReference(data, "quitButton", quit);
        SetReference(data, "menuButton", menu);
        data.ApplyModifiedPropertiesWithoutUndo();
        gameplay.SetActive(false);
    }

    private static GameObject CreateScreenRoot(string name, Transform parent)
    {
        GameObject root = new(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        return root;
    }

    private static WatermelonUI CreateInterface(WatermelonGame game)
    {
        GameObject canvasObject = new("Game UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 1f;
        WatermelonUI ui = canvasObject.AddComponent<WatermelonUI>();
        Transform parent = canvasObject.transform;
        Label("Edition", "THE RUBBER BAND EXPERIMENT", parent, 0.94f, 25, new Color(0.48f, 0.66f, 0.58f));
        Label("Title", "WATERMELON", parent, 0.895f, 76, Cream, FontStyle.Bold);
        Label("Subtitle", "A LITTLE MORE PRESSURE.", parent, 0.85f, 27, new Color(0.67f, 0.77f, 0.68f));
        Text multiplier = Label("Multiplier", "1.00x", parent, 0.75f, 124, Coral, FontStyle.Bold);
        Label("Multiplier Caption", "MULTIPLIER", parent, 0.697f, 25, Cream);
        Text bandCount = Label("Band Count", "00 BANDS", parent, 0.315f, 35, Cream, FontStyle.Bold);
        Text status = Label("Round Status", "HOW FAR CAN YOU GO?", parent, 0.258f, 36, Cream, FontStyle.Bold);
        Text instruction = Label("Instruction", "Every band raises the multiplier.\nThe next one could be the last.", parent, 0.20f, 31, new Color(0.67f, 0.77f, 0.68f));
        Button add = CreateButton("Add Band", "ADD BAND", parent, Coral);
        Button restart = CreateButton("Restart", "TRY AGAIN", parent, new Color(0.63f, 0.87f, 0.43f));
        restart.gameObject.SetActive(false);
        Label("Footer", "ONE MORE BAND. ONE BIGGER MULTIPLIER.", parent, 0.035f, 22, new Color(0.48f, 0.66f, 0.58f));

        var data = new SerializedObject(ui);
        SetReference(data, "game", game);
        SetReference(data, "multiplier", multiplier);
        SetReference(data, "bandCount", bandCount);
        SetReference(data, "status", status);
        SetReference(data, "instruction", instruction);
        SetReference(data, "addBandButton", add);
        SetReference(data, "restartButton", restart);
        data.ApplyModifiedPropertiesWithoutUndo();
        ConfigureMenu(ui);
        var eventSystem = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        return ui;
    }

    private static Text Label(string name, string value, Transform parent, float height, int size, Color color, FontStyle style = FontStyle.Normal)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        label.transform.SetParent(parent, false);
        Text text = label.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, height);
        rect.sizeDelta = new Vector2(1000f, size * 3f);
        return text;
    }

    private static Button CreateButton(string name, string caption, Transform parent, Color color)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.105f);
        rect.sizeDelta = new Vector2(900f, 144f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.9f, 0.95f, 0.9f);
        colors.pressedColor = new Color(0.7f, 0.75f, 0.7f);
        button.colors = colors;
        Text label = Label("Caption", caption, buttonObject.transform, 0.5f, 44, Ink, FontStyle.Bold);
        label.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Material material)
    {
        GameObject result = GameObject.CreatePrimitive(type);
        result.name = name;
        result.transform.SetParent(parent, false);
        result.GetComponent<Renderer>().sharedMaterial = material;
        if (result.TryGetComponent(out Collider collider) && name != "Display Plinth")
            Object.DestroyImmediate(collider);
        return result;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness)
    {
        string path = $"{ArtPath}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.name = name;
        material.color = color;
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D CreateRindTexture()
    {
        var texture = new Texture2D(512, 256, TextureFormat.RGB24, false);
        var pixels = new Color[512 * 256];
        Color light = new(0.42f, 0.65f, 0.12f);
        Color dark = new(0.065f, 0.25f, 0.085f);
        for (int y = 0; y < 256; y++)
        for (int x = 0; x < 512; x++)
        {
            float u = x / 512f;
            float v = y / 256f;
            float wave = Mathf.Sin(u * Mathf.PI * 24f + Mathf.Sin(v * 21f) * 0.35f + Mathf.Sin(v * 51f + u * 12f) * 0.15f);
            float stripe = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.2f, 0.45f, wave));
            float speckle = Mathf.PerlinNoise(x * 0.17f, y * 0.19f) * 0.13f;
            pixels[y * 512 + x] = Color.Lerp(dark, light, stripe) * (0.9f + speckle);
        }
        texture.SetPixels(pixels);
        texture.Apply();
        string path = $"{ArtPath}/WatermelonRind.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Mesh CreateRingMesh()
    {
        const int segments = 96;
        const int sides = 8;
        var vertices = new Vector3[(segments + 1) * (sides + 1)];
        var triangles = new int[segments * sides * 6];
        var uv = new Vector2[vertices.Length];
        int triangle = 0;
        for (int segment = 0; segment <= segments; segment++)
        for (int side = 0; side <= sides; side++)
        {
            float angle = segment * Mathf.PI * 2f / segments;
            float tube = side * Mathf.PI * 2f / sides;
            float radius = 1.018f + Mathf.Cos(tube) * 0.025f;
            int index = segment * (sides + 1) + side;
            vertices[index] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(tube) * 0.025f, Mathf.Sin(angle) * radius);
            uv[index] = new Vector2(segment / (float)segments, side / (float)sides);
            if (segment == segments || side == sides)
                continue;
            int next = index + sides + 1;
            triangles[triangle++] = index;
            triangles[triangle++] = index + 1;
            triangles[triangle++] = next;
            triangles[triangle++] = index + 1;
            triangles[triangle++] = next + 1;
            triangles[triangle++] = next;
        }
        var mesh = new Mesh { name = "Rubber Band", vertices = vertices, triangles = triangles, uv = uv };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void SetReference(SerializedObject data, string property, Object value)
    {
        data.FindProperty(property).objectReferenceValue = value;
    }

    private static void SetArray<T>(SerializedObject data, string property, T[] values) where T : Object
    {
        SerializedProperty array = data.FindProperty(property);
        array.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
            array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
    }
}
