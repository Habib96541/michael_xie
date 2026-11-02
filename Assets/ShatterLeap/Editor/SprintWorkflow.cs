using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace ShatterLeap.Editor
{
    [InitializeOnLoad]
    public static class SprintWorkflow
    {
        private const string Root = "Assets/ShatterLeap";
        public const string ScenePath = Root + "/Scenes/ShatterLeap.unity";
        private static double _nextPoll;

        static SprintWorkflow()
        {
            EditorApplication.update += Poll;
        }
        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll || EditorApplication.isCompiling || EditorApplication.isUpdating) { return; }
            _nextPoll = EditorApplication.timeSinceStartup + 1;
            const string commandPath = "Tools/editor-command.txt";
            if (!File.Exists(commandPath)) { return; }
            string command = File.ReadAllText(commandPath).Trim();
            File.Delete(commandPath);
            try
            {
                if (command == "inspect") { Inspect(); }
                else if (command == "setup") { Setup(); }
                else if (command == "tests") { RuleChecks.Run(); }
                else if (command == "integration") { PlayChecks.Begin(); }
                else if (command == "refresh") { AssetDatabase.Refresh(); }
                else if (command == "scenery") { UpdateScenery(); }
                else if (command == "wire") { RepairGeneratedReferences(); }
                else if (command == "polish") { CreateUiPanel(); }
                else if (command == "webprofile") { CreateWebProfile(); }
                else if (command == "restorequality") { RestoreProjectQualityReferences(); }
                else if (command == "build") { BuildWeb(); }
                else if (command == "play") { EditorSceneManager.OpenScene(ScenePath); EditorApplication.isPlaying = true; }
                else if (command == "stop") { EditorApplication.isPlaying = false; }
                else { throw new InvalidOperationException("Unknown editor command: " + command); }
                File.WriteAllText("Tools/editor-result.txt", command + " completed " + DateTime.UtcNow.ToString("O"));
            }
            catch (Exception exception)
            {
                File.WriteAllText("Tools/editor-result.txt", command + " FAILED\n" + exception);
                Debug.LogException(exception);
            }
        }
        [MenuItem("ShatterLeap/Inspect Supplied Assets")]
        public static void Inspect()
        {
            Directory.CreateDirectory("SprintDoc");
            using StreamWriter output = new StreamWriter("SprintDoc/AssetInspection.txt");
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Imports" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                output.WriteLine(path);
                if (!model) { continue; }
                output.WriteLine("  ROOT " + model.transform.localScale);
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    output.WriteLine($"  {renderer.name} bounds={renderer.bounds} vertices={mesh?.vertexCount} materials={string.Join(",", renderer.sharedMaterials.Select(material => material ? material.name + ":" + material.shader.name : "NULL"))}");
                }
            }
            output.WriteLine("Audio:");
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Imports" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                output.WriteLine($"  {path} duration={clip.length:0.00}s channels={clip.channels}");
            }
        }
        [MenuItem("ShatterLeap/Setup Sprint 1 (Preserve Existing Assets)")]
        public static void Setup()
        {
            foreach (string directory in new[] { "Materials", "Prefabs", "Config", "Scenes" })
            {
                Directory.CreateDirectory(Root + "/" + directory);
            }
            AssetDatabase.Refresh();
            Material steel = Material("Midnight steel", new Color(0.045f, 0.09f, 0.13f), false);
            Material glass = Material("Teal glass", new Color(0.19f, 0.52f, 0.57f), false, 0.85f);
            Material edge = Material("Glass edge", new Color(0.38f, 0.87f, 0.87f), true);
            Material pink = Material("Pink light", new Color(0.95f, 0.12f, 0.35f), true);
            Material marker = Material("Memory marker", new Color(0.75f, 1, 0.4f), true);
            Material green = Material("Contestant tracksuit", new Color(0.025f, 0.35f, 0.25f), false);
            Material white = Material("Ivory stripe", new Color(0.87f, 0.92f, 0.86f), false);
            Material skin = Material("Warm skin", new Color(0.64f, 0.39f, 0.25f), false);
            Material hair = Material("Hair and shoes", new Color(0.035f, 0.025f, 0.03f), false);
            GameObject contestant = CreateContestant(green, white, skin, hair);
            GameObject panel = CreatePanel(glass, edge, marker);
            string configPath = Root + "/Config/Sprint1.asset";
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(configPath);
            if (!config)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                config.ContestantPrefab = contestant;
                config.PanelPrefab = panel;
                config.Logo = Sprite("Assets/ShatterLeap/Art/ShatterLeapLogo.png", 1024);
                config.CoinIcon = Sprite("Assets/StrategyGameResourceIcons/Icons/gold-coin.png", 128);
                config.Click = Clip("Assets/Imports/Audios/UI_Button.mp3");
                config.Success = Clip("Assets/Imports/Audios/Success.mp3");
                config.Glass = Clip("Assets/Imports/Audios/GlassBreaking.mp3");
                config.Failure = Clip("Assets/Imports/Audios/GameOver.mp3");
                config.Coin = Clip("Assets/Imports/Audios/CoinSpill.mp3");
                config.Music = Clip("Assets/Imports/Audios/Background Sound/luis_humanoide-macabre-choir-inspired-by-squid-game-287425.mp3");
                AssetDatabase.CreateAsset(config, configPath);
            }
            AssetDatabase.SaveAssets();
            config = AssetDatabase.LoadAssetAtPath<GameConfig>(configPath);
            config.Validate();
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.35f, 0.43f, 0.52f);
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.015f, 0.035f, 0.06f);
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.008f;
                GameObject cameraObject = new GameObject("Gameplay camera", typeof(Camera), typeof(AudioListener), typeof(BridgeCamera));
                cameraObject.tag = "MainCamera";
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.015f, 0.035f, 0.06f);
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 160;
                Set(cameraObject.GetComponent<BridgeCamera>(), "_camera", camera);
                Light light = new GameObject("Soft key light", typeof(Light)).GetComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.6f;
                light.color = new Color(0.74f, 0.88f, 1);
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(45, -25, 0);
                GameObject bridgeObject = new GameObject("Bridge", typeof(BridgeView));
                BridgeView bridge = bridgeObject.GetComponent<BridgeView>();
                Set(bridge, "_config", config);
                Set(bridge, "_structure", steel);
                Set(bridge, "_pink", pink);
                GameObject audioObject = new GameObject("Audio and pooled glass", typeof(AudioEffects));
                AudioEffects audio = audioObject.GetComponent<AudioEffects>();
                AudioSource music = audioObject.AddComponent<AudioSource>();
                AudioSource sound = audioObject.AddComponent<AudioSource>();
                music.playOnAwake = false;
                sound.playOnAwake = false;
                Set(audio, "_config", config);
                Set(audio, "_music", music);
                Set(audio, "_sound", sound);
                Set(audio, "_shardMaterial", glass);
                GameObject canvasObject = new GameObject("Responsive UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(GameUi));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                GameUi ui = canvasObject.GetComponent<GameUi>();
                Set(ui, "_canvas", canvas);
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                GameObject root = new GameObject("ShatterLeap", typeof(RunController), typeof(ChoiceInput));
                RunController run = root.GetComponent<RunController>();
                Set(run, "_config", config);
                Set(run, "_bridge", bridge);
                Set(run, "_camera", cameraObject.GetComponent<BridgeCamera>());
                Set(run, "_ui", ui);
                Set(run, "_effects", audio);
                Set(root.GetComponent<ChoiceInput>(), "_run", run);
                Set(root.GetComponent<ChoiceInput>(), "_camera", camera);
                AddSuppliedScenery(steel);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "ShatterLeap";
            PlayerSettings.companyName = "ShatterLeap";
            PlayerSettings.bundleVersion = "0.1.0";
            AssetDatabase.SaveAssets();
            CreateUiPanel();
            CreateWebProfile();
            Inspect();
        }
        private static void AddSuppliedScenery(Material steel)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Imports/GlassBridgeLevel/source/Squid Game.blend");
            if (source)
            {
                GameObject environment = (GameObject)PrefabUtility.InstantiatePrefab(source);
                environment.name = "Supplied arena (decorative)";
                // Keep the arena far beneath the playable bridge, avoiding support leaks.
                Bounds bounds = BoundsOf(environment);
                float scale = 65 / Mathf.Max(bounds.size.x, bounds.size.z, 1);
                environment.transform.localScale *= scale;
                bounds = BoundsOf(environment);
                environment.transform.position += new Vector3(-bounds.center.x, -14.63f * scale - 0.65f, 20 - bounds.center.z);
                RemoveOriginalBridge(environment);
                foreach (Collider collider in environment.GetComponentsInChildren<Collider>()) { UnityEngine.Object.DestroyImmediate(collider); }
                foreach (Camera camera in environment.GetComponentsInChildren<Camera>()) { UnityEngine.Object.DestroyImmediate(camera.gameObject); }
                foreach (Light light in environment.GetComponentsInChildren<Light>()) { light.enabled = false; }
            }
            for (int i = 0; i < 3; i++)
            {
                string file = i == 0 ? "Squid" : "Squid " + i;
                GameObject guardAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Imports/squid-game-man/source/{file}.fbx");
                if (!guardAsset) { continue; }
                GameObject guard = (GameObject)PrefabUtility.InstantiatePrefab(guardAsset);
                guard.name = "Supplied guard " + i;
                Bounds bounds = BoundsOf(guard);
                guard.transform.localScale *= 2.2f / Mathf.Max(0.01f, bounds.size.y);
                bounds = BoundsOf(guard);
                Vector3 destination = new Vector3(i == 1 ? 4.8f : -4.8f, -0.05f, 3 + i * 7);
                guard.transform.position += destination - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                guard.transform.rotation = Quaternion.Euler(0, i == 1 ? -90 : 90, 0);
                Shape("Guard balcony", PrimitiveType.Cube, null, destination + Vector3.down * 0.3f, new Vector3(2, 0.5f, 3), steel);
                foreach (Collider collider in guard.GetComponentsInChildren<Collider>()) { UnityEngine.Object.DestroyImmediate(collider); }
            }
        }
        private static void RemoveOriginalBridge(GameObject environment)
        {
            foreach (Renderer renderer in environment.GetComponentsInChildren<Renderer>())
            {
                string lower = renderer.name.ToLowerInvariant();
                if (lower.Contains("glass_") || lower.Contains("glass_right") || lower.Contains("bridge_metal") || lower.Contains("bridge_bulb") ||
                    lower.Contains("metal_structure") || lower.Contains("chandelier") || lower.Contains("lights1_") || lower.Contains("pillar_light") ||
                    lower.Contains("wall_golden_structure") || lower.Contains("ground_lights") || lower.Contains("entrance_curtains") || lower.StartsWith("door"))
                {
                    renderer.enabled = false;
                }
                if (lower.Contains("ground_checkers"))
                {
                    renderer.sharedMaterial = Material("Arena floor", new Color(0.025f, 0.045f, 0.075f), false, 0.15f);
                }
                else if (lower.Contains("wall_golden_structure"))
                {
                    renderer.sharedMaterial = Material("Arena wall trim", new Color(0.06f, 0.17f, 0.2f), true);
                }
                else if (lower.Contains("ground_lights"))
                {
                    renderer.sharedMaterial = Material("Arena light strips", new Color(0.10f, 0.32f, 0.34f), true);
                }
                else if (lower.Contains("bg_wall"))
                {
                    renderer.sharedMaterial = Material("Arena backdrop", new Color(0.025f, 0.035f, 0.055f), false);
                }
            }
        }
        private static void UpdateScenery()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameObject environment = scene.GetRootGameObjects().First(item => item.name == "Supplied arena (decorative)");
            environment.transform.position = new Vector3(0, -14.63f * environment.transform.localScale.y - 0.65f, 20);
            RemoveOriginalBridge(environment);
            Material glass = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Teal glass.mat");
            glass.SetColor("_BaseColor", new Color(0.12f, 0.48f, 0.55f, 0.66f));
            glass.SetFloat("_Surface", 1);
            glass.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            glass.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            glass.SetFloat("_ZWrite", 0);
            glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            glass.SetOverrideTag("RenderType", "Transparent");
            glass.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(glass);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
        }
        private static void RepairGeneratedReferences()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/Config/Sprint1.asset");
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component is RunController || component is BridgeView || component is AudioEffects)
                    {
                        Set(component, "_config", config);
                    }
                }
            }
            EditorSceneManager.SaveScene(scene);
            Validate();
        }
        private static void CreateUiPanel()
        {
            const string path = Root + "/Art/ScalablePanel.png";
            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        float dx = Mathf.Max(7.5f - x, 0, x - 23.5f);
                        float dy = Mathf.Max(7.5f - y, 0, y - 23.5f);
                        float alpha = Mathf.Clamp01(8.2f - Mathf.Sqrt(dx * dx + dy * dy));
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(9, 9, 9, 9);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/Config/Sprint1.asset");
            if (!config.UiPanel)
            {
                config.UiPanel = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }
        private static void CreateWebProfile()
        {
            const string pipelinePath = Root + "/Config/WebPipeline.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath)) { return; }
            UniversalRendererData sourceRenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/Mobile_Renderer.asset");
            UniversalRendererData renderer = UnityEngine.Object.Instantiate(sourceRenderer);
            renderer.name = "ShatterLeap Web Forward Renderer";
            renderer.rendererFeatures.Clear();
            SerializedObject rendererSettings = new SerializedObject(renderer);
            rendererSettings.FindProperty("m_RenderingMode").intValue = 0;
            rendererSettings.FindProperty("m_UseNativeRenderPass").boolValue = false;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(renderer, Root + "/Config/WebRenderer.asset");
            UniversalRenderPipelineAsset pipeline = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset"));
            pipeline.name = "ShatterLeap Web Pipeline";
            SerializedObject settings = new SerializedObject(pipeline);
            settings.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            settings.FindProperty("m_SupportsHDR").boolValue = false;
            settings.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            settings.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            settings.FindProperty("m_SoftShadowsSupported").boolValue = false;
            settings.FindProperty("m_SupportsLightCookies").boolValue = false;
            settings.FindProperty("m_RenderScale").floatValue = 1;
            settings.FindProperty("m_MSAA").intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipeline, pipelinePath);
            AssetDatabase.SaveAssets();
        }
        private static void RestoreProjectQualityReferences()
        {
            UnityEngine.Object quality = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            SerializedObject serialized = new SerializedObject(quality);
            SerializedProperty levels = serialized.FindProperty("m_QualitySettings");
            for (int i = 0; i < levels.arraySize; i++)
            {
                SerializedProperty level = levels.GetArrayElementAtIndex(i);
                SerializedProperty pipeline = level.FindPropertyRelative("customRenderPipeline");
                string name = level.FindPropertyRelative("name").stringValue;
                if (pipeline.objectReferenceValue == null && (name == "Mobile" || name == "PC"))
                {
                    pipeline.objectReferenceValue = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>($"Assets/Settings/{name}_RPAsset.asset");
                    level.FindPropertyRelative("antiAliasing").intValue = 0;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(quality);
            AssetDatabase.SaveAssets();
        }
        private static Bounds BoundsOf(GameObject item)
        {
            Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.one);
            foreach (Renderer renderer in renderers) { bounds.Encapsulate(renderer.bounds); }
            return bounds;
        }
        private static GameObject CreatePanel(Material glass, Material edge, Material marker)
        {
            string path = Root + "/Prefabs/GlassPanel.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) { return existing; }
            GameObject root = new GameObject("GlassPanel", typeof(GlassPanel));
            GameObject top = Shape("Glass support", PrimitiveType.Cube, root.transform, Vector3.zero, new Vector3(1.9f, 0.2f, 2.4f), glass);
            top.layer = 8;
            BoxCollider collider = top.AddComponent<BoxCollider>();
            for (int side = -1; side <= 1; side += 2)
            {
                Shape("Edge", PrimitiveType.Cube, root.transform, new Vector3(side * 0.94f, 0.08f, 0), new Vector3(0.035f, 0.035f, 2.4f), edge);
                Shape("Edge", PrimitiveType.Cube, root.transform, new Vector3(0, 0.08f, side * 1.18f), new Vector3(1.9f, 0.035f, 0.035f), edge);
            }
            GameObject reveal = new GameObject("Preview only marker");
            reveal.transform.SetParent(root.transform, false);
            Shape("Safe stripe", PrimitiveType.Cube, reveal.transform, new Vector3(0, 0.16f, 0), new Vector3(1.55f, 0.05f, 0.45f), marker);
            Shape("Safe dot", PrimitiveType.Sphere, reveal.transform, new Vector3(0, 0.28f, 0), new Vector3(0.3f, 0.15f, 0.3f), marker);
            Set(root.GetComponent<GlassPanel>(), "_previewMarker", reveal);
            Set(root.GetComponent<GlassPanel>(), "_support", collider);
            Set(root.GetComponent<GlassPanel>(), "_glass", top.GetComponent<Renderer>());
            reveal.SetActive(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }
        private static GameObject CreateContestant(Material green, Material white, Material skin, Material hair)
        {
            string path = Root + "/Prefabs/Contestant.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing) { return existing; }
            GameObject root = new GameObject("Contestant 017", typeof(Rigidbody), typeof(CapsuleCollider), typeof(ContestantMotor));
            Rigidbody body = root.GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            CapsuleCollider collider = root.GetComponent<CapsuleCollider>();
            collider.height = 1.85f;
            collider.radius = 0.28f;
            collider.center = new Vector3(0, 0.94f, 0);
            Transform visual = new GameObject("Procedural pose rig").transform;
            visual.SetParent(root.transform, false);
            Shape("Jacket", PrimitiveType.Capsule, visual, new Vector3(0, 1.12f, 0), new Vector3(0.61f, 0.39f, 0.39f), green);
            Shape("Collar", PrimitiveType.Cylinder, visual, new Vector3(0, 1.47f, 0), new Vector3(0.24f, 0.045f, 0.24f), white);
            Shape("Neck", PrimitiveType.Cylinder, visual, new Vector3(0, 1.51f, 0), new Vector3(0.15f, 0.08f, 0.15f), skin);
            Shape("Head", PrimitiveType.Sphere, visual, new Vector3(0, 1.73f, 0), new Vector3(0.43f, 0.47f, 0.41f), skin);
            Shape("Hair", PrimitiveType.Sphere, visual, new Vector3(0, 1.89f, -0.025f), new Vector3(0.44f, 0.22f, 0.41f), hair);
            Shape("Nose", PrimitiveType.Sphere, visual, new Vector3(0, 1.73f, 0.21f), new Vector3(0.09f, 0.10f, 0.09f), skin);
            Shape("Jacket zip", PrimitiveType.Cube, visual, new Vector3(0, 1.14f, 0.199f), new Vector3(0.025f, 0.55f, 0.016f), white);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Shape("Eye", PrimitiveType.Sphere, visual, new Vector3(sign * 0.09f, 1.77f, 0.19f), new Vector3(0.04f, 0.04f, 0.025f), hair);
                Transform arm = new GameObject(sign < 0 ? "Left shoulder" : "Right shoulder").transform;
                arm.SetParent(visual, false);
                arm.localPosition = new Vector3(sign * 0.36f, 1.37f, 0);
                Shape("Sleeve", PrimitiveType.Capsule, arm, new Vector3(0, -0.25f, 0), new Vector3(0.22f, 0.29f, 0.23f), green);
                Shape("Sleeve stripe", PrimitiveType.Cube, arm, new Vector3(sign * 0.11f, -0.23f, 0), new Vector3(0.018f, 0.43f, 0.08f), white);
                Shape("Hand", PrimitiveType.Sphere, arm, new Vector3(0, -0.58f, 0), new Vector3(0.18f, 0.22f, 0.18f), skin);
                Transform leg = new GameObject(sign < 0 ? "Left hip" : "Right hip").transform;
                leg.SetParent(visual, false);
                leg.localPosition = new Vector3(sign * 0.16f, 0.78f, 0);
                Shape("Trousers", PrimitiveType.Capsule, leg, new Vector3(0, -0.30f, 0), new Vector3(0.25f, 0.34f, 0.27f), green);
                Shape("Trouser stripe", PrimitiveType.Cube, leg, new Vector3(sign * 0.126f, -0.3f, 0), new Vector3(0.018f, 0.56f, 0.06f), white);
                Shape("Shoe", PrimitiveType.Cube, leg, new Vector3(0, -0.68f, 0.065f), new Vector3(0.27f, 0.16f, 0.42f), hair);
                Shape("Sole", PrimitiveType.Cube, leg, new Vector3(0, -0.75f, 0.065f), new Vector3(0.28f, 0.04f, 0.43f), white);
                Set(root.GetComponent<ContestantMotor>(), sign < 0 ? "_leftArm" : "_rightArm", arm);
                Set(root.GetComponent<ContestantMotor>(), sign < 0 ? "_leftLeg" : "_rightLeg", leg);
            }
            Shape("Number patch", PrimitiveType.Cube, visual, new Vector3(-0.16f, 1.26f, 0.198f), new Vector3(0.18f, 0.11f, 0.012f), white);
            GameObject number = new GameObject("017", typeof(TextMesh));
            number.transform.SetParent(visual, false);
            number.transform.localPosition = new Vector3(-0.16f, 1.26f, 0.21f);
            number.transform.localRotation = Quaternion.Euler(0, 0, 0);
            TextMesh text = number.GetComponent<TextMesh>();
            text.text = "017";
            text.anchor = TextAnchor.MiddleCenter;
            text.characterSize = 0.025f;
            text.fontSize = 36;
            text.color = Color.black;
            Set(root.GetComponent<ContestantMotor>(), "_body", body);
            Set(root.GetComponent<ContestantMotor>(), "_visual", visual);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }
        private static GameObject Shape(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localScale = scale;
            shape.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(shape.GetComponent<Collider>());
            return shape;
        }
        private static Material Material(string name, Color color, bool unlit, float smoothness = 0.35f)
        {
            string path = Root + "/Materials/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing) { return existing; }
            Material material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.name = name;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static Sprite Sprite(string path, int maxSize)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer) { throw new InvalidOperationException("Missing texture: " + path); }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = maxSize;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static AudioClip Clip(string path) => AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        public static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        [MenuItem("ShatterLeap/Validate Existing Game")]
        public static void Validate()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(Root + "/Config/Sprint1.asset");
            if (!config) { throw new InvalidOperationException("Run setup first."); }
            config.Validate();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects().Concat(new[] { config.ContestantPrefab, config.PanelPrefab }))
            {
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (!component) { throw new InvalidOperationException("Missing script in " + root.name); }
                    if (!(component is MonoBehaviour) || component.GetType().Namespace?.StartsWith("ShatterLeap") != true) { continue; }
                    SerializedObject serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.name.StartsWith("_") && property.objectReferenceValue == null)
                        {
                            throw new InvalidOperationException(component.name + " missing " + property.name);
                        }
                    }
                }
            }
        }
        [MenuItem("ShatterLeap/Build Existing Web Game")]
        public static void BuildWeb()
        {
            Validate();
            RuleChecks.Run();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.template = "PROJECT:ShatterLeap";
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 1024;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Directory.CreateDirectory("Builds/Web");
            File.Copy(Root + "/Art/ShatterLeapLogo.png", "Assets/WebGLTemplates/ShatterLeap/logo.png", true);
            AssetDatabase.Refresh();
            UniversalRenderPipelineAsset webPipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root + "/Config/WebPipeline.asset");
            if (!webPipeline) { throw new InvalidOperationException("Generate the Web profile using the webprofile editor command before building."); }
            int previousQuality = QualitySettings.GetQualityLevel();
            UnityEngine.Object qualityObject = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            string previousQualityJson = EditorJsonUtility.ToJson(qualityObject);
            RenderPipelineAsset previousDefault = GraphicsSettings.defaultRenderPipeline;
            RenderPipelineAsset[] previousPipelines = new RenderPipelineAsset[QualitySettings.names.Length];
            BuildReport report;
            try
            {
                GraphicsSettings.defaultRenderPipeline = webPipeline;
                for (int i = 0; i < previousPipelines.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    previousPipelines[i] = QualitySettings.renderPipeline;
                    QualitySettings.renderPipeline = webPipeline;
                }
                QualitySettings.SetQualityLevel(0, false);
                AssetDatabase.SaveAssets();
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, locationPathName = "Builds/Web", target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
            }
            finally
            {
                for (int i = 0; i < previousPipelines.Length; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = previousPipelines[i];
                }
                GraphicsSettings.defaultRenderPipeline = previousDefault;
                QualitySettings.SetQualityLevel(previousQuality, false);
                // Switching URP assets can mutate quality overrides. Restore the complete native snapshot.
                EditorJsonUtility.FromJsonOverwrite(previousQualityJson, qualityObject);
                EditorUtility.SetDirty(qualityObject);
                AssetDatabase.SaveAssets();
            }
            File.WriteAllText("SprintDoc/BuildReport.txt", $"Result: {report.summary.result}\nBytes: {report.summary.totalSize}\nDuration: {report.summary.totalTime}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n");
            if (report.summary.result != BuildResult.Succeeded) { throw new Exception("Web build failed. See Unity log."); }
        }
    }
}
