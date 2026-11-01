using System;
using System.IO;
using PegRush;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class PegRushAuthoring
{
    private const string Root = "Assets/PegRush/Generated";

    [MenuItem("PegRush/1. Create connected scene (one time)")]
    public static void Setup()
    {
        Directory.CreateDirectory(Root);
        if (File.Exists(Root + "/PegRush.unity")) throw new InvalidOperationException("Scene already exists. Setup will not overwrite authored assets.");
        GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
        for (int rows = 8; rows <= 16; rows++)
        {
            for (int profile = 0; profile < 3; profile++)
            {
                int[] values = new int[rows + 1];
                for (int slot = 0; slot <= rows; slot++)
                {
                    float edge = Mathf.Abs(2f * slot / rows - 1f);
                    values[slot] = Mathf.RoundToInt(profile == 0 ? 70 + 130 * edge * edge : profile == 1 ? 40 + 760 * Mathf.Pow(edge, 3) : 15 + 2485 * Mathf.Pow(edge, 5));
                }
                config.Tables.Add(new RewardTable { Rows = rows, Profile = profile, Hundredths = values });
            }
        }
        config.Validate();
        AssetDatabase.CreateAsset(config, Root + "/GameConfig.asset");
        Sprite circle = CreateSprite("Orb", false);
        Sprite rounded = CreateSprite("Rounded", true);
        PhysicsMaterial2D material = new PhysicsMaterial2D("Arcade bounce");
        material.friction = 0.05f;
        material.bounciness = 0.58f;
        AssetDatabase.CreateAsset(material, Root + "/Bounce.physicsMaterial2D");
        GameObject peg = new GameObject("Peg");
        SpriteRenderer pegVisual = peg.AddComponent<SpriteRenderer>();
        pegVisual.sprite = circle;
        pegVisual.color = new Color(0.35f, 0.88f, 0.94f);
        peg.transform.localScale = Vector3.one * 0.21f;
        peg.AddComponent<CircleCollider2D>().sharedMaterial = material;
        peg.AddComponent<PegFlash>();
        GameObject pegPrefab = PrefabUtility.SaveAsPrefabAsset(peg, Root + "/Peg.prefab");
        UnityEngine.Object.DestroyImmediate(peg);
        GameObject pocket = new GameObject("Pocket");
        BoxCollider2D sensor = pocket.AddComponent<BoxCollider2D>();
        sensor.isTrigger = true;
        sensor.size = new Vector2(0.92f, 0.4f);
        pocket.AddComponent<Pocket>();
        GameObject pocketPrefab = PrefabUtility.SaveAsPrefabAsset(pocket, Root + "/Pocket.prefab");
        UnityEngine.Object.DestroyImmediate(pocket);
        GameObject ballObject = new GameObject("Ball");
        ballObject.layer = 8;
        ballObject.transform.localScale = Vector3.one * 0.28f;
        SpriteRenderer ballVisual = ballObject.AddComponent<SpriteRenderer>();
        ballVisual.sprite = circle;
        ballVisual.sortingOrder = 2;
        Rigidbody2D body = ballObject.AddComponent<Rigidbody2D>();
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        ballObject.AddComponent<CircleCollider2D>().sharedMaterial = material;
        Ball ball = ballObject.AddComponent<Ball>();
        ball.Wire(body, ballVisual);
        Ball ballPrefab = PrefabUtility.SaveAsPrefabAsset(ballObject, Root + "/Ball.prefab").GetComponent<Ball>();
        UnityEngine.Object.DestroyImmediate(ballObject);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = new GameObject("Orthographic Board Camera").AddComponent<Camera>();
        Camera background = new GameObject("Background Camera").AddComponent<Camera>();
        background.depth = -10;
        background.clearFlags = CameraClearFlags.SolidColor;
        background.cullingMask = 0;
        background.backgroundColor = new Color(0.025f, 0.08f, 0.13f);
        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.08f, 0.13f);
        camera.gameObject.AddComponent<AudioListener>();
        Board board = new GameObject("Generated 2D Board").AddComponent<Board>();
        board.Wire(pegPrefab, pocketPrefab, material);
        GameObject canvasObject = new GameObject("PegRush UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        GameUI ui = canvasObject.AddComponent<GameUI>();
        ui.Wire(rounded, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
        GameObject uiPrefab = PrefabUtility.SaveAsPrefabAsset(canvasObject, Root + "/UI.prefab");
        UnityEngine.Object.DestroyImmediate(canvasObject);
        ui = ((GameObject)PrefabUtility.InstantiatePrefab(uiPrefab)).GetComponent<GameUI>();
        GameObject events = new GameObject("Input System Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        GameSession session = new GameObject("PegRush Session").AddComponent<GameSession>();
        AudioEffects audio = session.gameObject.AddComponent<AudioEffects>();
        session.gameObject.AddComponent<BrowserStatus>();
        session.Wire(config, board, ballPrefab, camera, ui, audio);
        EditorSceneManager.SaveScene(scene, Root + "/PegRush.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Root + "/PegRush.unity", true) };
        PlayerSettings.companyName = "PegRush Arcade";
        PlayerSettings.productName = "PegRush";
        PlayerSettings.runInBackground = false;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template = "PROJECT:PegRush";
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        AssetDatabase.SaveAssets();
        Verify();
    }

    private static Sprite CreateSprite(string name, bool rounded)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = rounded ? new Vector2(Mathf.Max(Mathf.Abs(x - 31.5f) - 19.5f, 0), Mathf.Max(Mathf.Abs(y - 31.5f) - 19.5f, 0)).magnitude - 11f : Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) - 30f;
                float alpha = Mathf.Clamp01(0.5f - distance);
                float brightness = rounded ? 1f : Mathf.Lerp(0.65f, 1f, Mathf.Clamp01((y + 15f) / 64f));
                texture.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
            }
        }
        texture.Apply();
        string path = Root + "/" + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = rounded ? new Vector4(14, 14, 14, 14) : Vector4.zero;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("PegRush/2. Validate configuration and transactions")]
    public static void Verify()
    {
        AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/GameConfig.asset").Validate();
        string stored = "";
        Wallet wallet = new Wallet("", json => stored = json);
        Assert(wallet.Data.Coins == 100000, "Starting wallet");
        DropTransaction pending = wallet.Reserve(10, 8, 0, new[] { 125 });
        Assert(wallet.Data.Coins == 99000, "Reserve");
        Assert(wallet.Resolve(pending.Id, 0) == 1250, "Reward");
        Assert(wallet.Resolve(pending.Id, 0) == -1, "Duplicate reward");
        Assert(wallet.Data.Coins == 100250, "Final balance");
        pending = wallet.Reserve(50, 8, 0, new[] { 125 });
        Wallet recovered = new Wallet(stored, json => stored = json);
        Assert(recovered.Data.Coins == 100250 && recovered.Data.Pending.Count == 0, "Recovery");
        recovered = new Wallet(stored, json => stored = json);
        Assert(recovered.Data.Coins == 100250, "Repeated recovery");
        Assert(recovered.Resolve(pending.Id, 0) == -1, "No reward after recovery");
        Assert(Wallet.Reward(101, 150) == 152, "Half-up rounding");
        Assert(new Wallet("invalid", json => { }).Data.Coins == 100000, "Invalid save");
        Assert(new Wallet("{\"Version\":9}", json => { }).Data.Coins == 100000, "Outdated save");
        int[] snapshot = { 100 };
        DropTransaction isolated = recovered.Reserve(1, 8, 0, snapshot);
        snapshot[0] = 999;
        Assert(isolated.Table[0] == 100, "Snapshot isolation");
        Assert(recovered.Resolve(isolated.Id, -1) == 100, "Refund");
        Assert(recovered.Resolve(isolated.Id, -1) == -1, "Duplicate refund");
        Wallet poor = new Wallet("{\"Version\":1,\"Coins\":50,\"Pending\":[]}", json => { });
        Assert(poor.Reserve(1, 8, 0, snapshot) == null && poor.Data.Coins == 50, "Insufficient funds");
        Assert(poor.Refill() && poor.Data.Coins == 100000 && !poor.Refill(), "Refill");
        Wallet blocked = new Wallet("", json => { throw new Exception("Blocked storage"); });
        Assert(blocked.Reserve(1, 8, 0, snapshot) == null && blocked.Data.Coins == 100000, "Storage failure");
        Directory.CreateDirectory("SprintDoc");
        File.WriteAllText("SprintDoc/DeterministicTests.txt", "PASS: 27 symmetric tables; reserve; reward; duplicate resolve; recovery; repeated recovery; no reward after refund; half-up rounding; invalid and outdated save; snapshot isolation; duplicate refund; insufficient funds; refill; blocked storage.\n");
        Debug.Log("PEGRUSH VALIDATION PASSED");
    }
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception("FAILED: " + message);
    }

    public static void RepairAndRunEngine()
    {
        RepairSpriteReferences();
        PegRushEngineChecks.Run();
    }

    public static void RepairSpriteReferences()
    {
        foreach (string name in new[] { "Orb", "Rounded" })
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/" + name + ".png");
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        Sprite orb = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Orb.png");
        Sprite rounded = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Rounded.png");
        foreach (string name in new[] { "Peg", "Ball", "UI" })
        {
            string path = Root + "/" + name + ".prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            if (name == "UI") prefab.GetComponent<GameUI>().Wire(rounded, Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            else prefab.GetComponent<SpriteRenderer>().sprite = orb;
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        var scene = EditorSceneManager.OpenScene(Root + "/PegRush.unity");
        GameObject.Find("Background Camera").GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Verify();
    }

    public static void AddDiagnostics()
    {
        var scene = EditorSceneManager.OpenScene(Root + "/PegRush.unity");
        GameSession session = UnityEngine.Object.FindFirstObjectByType<GameSession>();
        if (session.GetComponent<BrowserStatus>() == null) session.gameObject.AddComponent<BrowserStatus>();
        EditorSceneManager.SaveScene(scene);
        Build();
    }

    [MenuItem("PegRush/3. Build WebGL")]
    public static void Build()
    {
        Verify();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Root + "/PegRush.unity" },
            locationPathName = "Builds/WebGL",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("WebGL build failed");
        Debug.Log("PEGRUSH WEBGL BUILD SUCCEEDED");
    }
}






