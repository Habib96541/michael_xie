using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace FlapDash.Editor
{
    public static class FlapDashProjectSetup
    {
        private const string ScenePath = "Assets/Scenes/FlapDash.unity";
        private const string FontPath = "Assets/ThirdParty/Tuffy/Tuffy SDF.asset";
        private static readonly Color Background = new Color32(207, 239, 252, 255);
        private static readonly Color Panel = Color.white;
        private static readonly Color Mint = new Color32(12, 136, 185, 255);
        private static readonly Color Muted = new Color32(56, 86, 110, 255);
        private static readonly Color Ink = new Color32(25, 54, 82, 255);

        [MenuItem("Flap Dash/Create Playable Scene")]
        public static void Create()
        {
            try
            {
                EnsureFolders();
                ConfigureProject();
                TMP_FontAsset font = LoadFont();
                if (font == null)
                    return;
                CreateScene(font);
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                FailSetup(exception);
            }
        }

        private static void CreateScene(TMP_FontAsset font)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            FlapDashArtSetup.Import();
            Material material = CreateSpriteMaterial();
            Sprite square = CreateSprite("Square", false);
            Sprite circle = CreateSprite("Circle", true);
            InputActionAsset controls = CreateControls();
            PipeGate pipePrefab = CreatePipePrefab(square, material);
            FlapPlayer playerPrefab = CreatePlayerPrefab(circle, material, controls);

            CreateCamera();
            CreateBoundary("Ground", -7.45f, square, material);
            CreateBoundary("Ceiling", 7.45f, square, material);

            RoundController round = new GameObject("Round").AddComponent<RoundController>();
            PipeCourse course = new GameObject("Pipe Course").AddComponent<PipeCourse>();
            var playerObject = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject);
            if (playerObject == null || !playerObject.TryGetComponent(out FlapPlayer player))
                throw new InvalidOperationException("Player prefab did not instantiate with a FlapPlayer component.");
            player.name = "Player";
            player.transform.position = new Vector3(-2f, 0f, 0f);
            SetReference(player, "round", round);
            SetReference(course, "round", round);
            SetReference(course, "pipePrefab", pipePrefab);
            SetFloat(course, "moveSpeed", 2.65f);
            SetInteger(course, "pipesPerSpeedIncrease", 10);
            SetFloat(course, "speedIncrease", 0.2f);
            SetFloat(course, "maximumSpeed", 3.65f);
            SetFloat(course, "acceleration", 0.2f);
            SetFloat(course, "spawnInterval", 1.48f);
            SetFloat(course, "firstSpawnDelay", 1.1f);
            SetFloatArray(course, "gapCenters", new[] { 0f, 1.8f, -1.8f, 2.4f, -2.2f, 1.6f, -2.6f, 2f, -1.4f, 2.6f, -2f, 1.4f });
            SetReference(round, "player", player);
            SetReference(round, "course", course);
            SetFloat(round, "roundDuration", 60f);
            CreateInterface(round, font, controls, player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeObject = round;
            AssetDatabase.SaveAssets();
            foreach (string obsoleteContent in new[] { "Assets/TextMesh Pro/Fonts", "Assets/TextMesh Pro/Resources/Fonts & Materials", "Assets/TextMesh Pro/Sprites", "Assets/TextMesh Pro/Resources/Sprite Assets" })
                if (AssetDatabase.IsValidFolder(obsoleteContent)) AssetDatabase.DeleteAsset(obsoleteContent);
            Debug.Log("FLAP_SETUP_PASS: Created scene, prefabs, controls, and project settings.");
        }

        private static void EnsureFolders()
        {
            foreach (string folder in new[] { "Scenes", "Input", "Prefabs", "Art", "Materials", "Scripts" })
                Directory.CreateDirectory(Path.Combine(Application.dataPath, folder));
            AssetDatabase.Refresh();
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "Flap Dash Prototype";
            PlayerSettings.productName = "Flap Dash";
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.runInBackground = false;

            UnityEngine.Object settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var serializedSettings = new SerializedObject(settings);
            serializedSettings.FindProperty("activeInputHandler").intValue = 1;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();

            var tagSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tags = tagSettings.FindProperty("tags");
            if (!Enumerable.Range(0, tags.arraySize).Any(index => tags.GetArrayElementAtIndex(index).stringValue == "Obstacle"))
            {
                tags.InsertArrayElementAtIndex(tags.arraySize);
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = "Obstacle";
                tagSettings.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static TMP_FontAsset LoadFont()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/ThirdParty/Tuffy/Tuffy.ttf");
                if (source == null) throw new InvalidOperationException("Public-domain Tuffy font is missing.");
                font = TMP_FontAsset.CreateFontAsset(source, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                font.name = "Tuffy SDF";
                font.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:;!?%/-()", out string missing);
                AssetDatabase.CreateAsset(font, FontPath);
                AssetDatabase.AddObjectToAsset(font.material, font);
                foreach (Texture2D atlas in font.atlasTextures) AssetDatabase.AddObjectToAsset(atlas, font);
            }
            font.fallbackFontAssetTable?.Clear();
            var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset"));
            settings.FindProperty("m_defaultFontAsset").objectReferenceValue = font;
            settings.FindProperty("m_fallbackFontAssets").arraySize = 0;
            settings.FindProperty("m_defaultSpriteAsset").objectReferenceValue = null;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return font;
        }

        private static void FailSetup(Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static Material CreateSpriteMaterial()
        {
            const string path = "Assets/Materials/FlatSprite.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                throw new InvalidOperationException("URP 2D unlit shader is unavailable.");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Sprite CreateSprite(string name, bool circular)
        {
            string path = $"Assets/Art/{name}.png";
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color32[128 * 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(64f, 64f));
                    byte alpha = circular ? (byte)Mathf.RoundToInt(Mathf.Clamp01(64f - distance) * 255f) : (byte)255;
                    pixels[y * 128 + x] = new Color32(255, 255, 255, alpha);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 128f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static InputActionAsset CreateControls()
        {
            const string path = "Assets/Input/FlapControls.inputactions";
            var controls = ScriptableObject.CreateInstance<InputActionAsset>();
            controls.name = "FlapControls";
            InputActionMap gameplay = controls.AddActionMap("Gameplay");
            InputAction flap = gameplay.AddAction("Flap", InputActionType.Button);
            flap.AddBinding("<Keyboard>/space");
            flap.AddBinding("<Mouse>/leftButton");
            flap.AddBinding("<Touchscreen>/primaryTouch/press");

            InputActionMap menu = controls.AddActionMap("UI");
            InputAction point = menu.AddAction("Point", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            point.AddBinding("<Pointer>/position");
            point.AddBinding("<Touchscreen>/touch*/position");
            InputAction click = menu.AddAction("Click", InputActionType.PassThrough, expectedControlLayout: "Button");
            click.AddBinding("<Mouse>/leftButton");
            click.AddBinding("<Touchscreen>/touch*/press");
            menu.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            menu.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
            InputAction navigate = menu.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            navigate.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            File.WriteAllText(path, controls.ToJson());
            UnityEngine.Object.DestroyImmediate(controls);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static FlapPlayer CreatePlayerPrefab(Sprite circle, Material material, InputActionAsset controls)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            SpritePart("Halo", visual, CreateGlowSprite(), material, new Color32(255, 253, 206, 130), Vector2.zero, new Vector2(1.15f, 1.15f), 4);
            Sprite[] flightFrames = FlapDashArtSetup.BirdFrames();
            GameObject bird = Artwork("Bird", visual, flightFrames[0], material, Vector2.zero, new Vector2(0.96f, 0.72f), 5);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 2f;
            body.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.simulated = false;
            player.AddComponent<CircleCollider2D>().radius = 0.3f;
            FlapPlayer controller = player.AddComponent<FlapPlayer>();
            SetReference(controller, "controls", controls);
            SetFloat(controller, "flapSpeed", 8f);
            const string chirpPath = "Assets/ThirdParty/AjFlapChirp/FlapChirp.wav";
            var audioImporter = (AudioImporter)AssetImporter.GetAtPath(chirpPath);
            AudioImporterSampleSettings sampleSettings = audioImporter.defaultSampleSettings;
            sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
            sampleSettings.compressionFormat = AudioCompressionFormat.PCM;
            sampleSettings.preloadAudioData = true;
            audioImporter.defaultSampleSettings = sampleSettings;
            audioImporter.SaveAndReimport();
            AudioSource flapAudio = player.AddComponent<AudioSource>();
            flapAudio.playOnAwake = false;
            flapAudio.loop = false;
            flapAudio.spatialBlend = 0f;
            flapAudio.volume = 0.3f;
            flapAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(chirpPath);
            SetReference(controller, "flapAudio", flapAudio);
            PlayerFeedback feedback = player.AddComponent<PlayerFeedback>();
            SetReference(feedback, "visual", visual);
            SetReference(feedback, "bird", bird.GetComponent<SpriteRenderer>());
            SetReferenceArray(feedback, "flightFrames", flightFrames);
            ParticleSystem sparks = CreateParticles("Flap Sparks", player.transform, material, circle.texture);
            SetReference(feedback, "sparks", sparks);
            var trailObject = new GameObject("Neon Trail");
            trailObject.transform.SetParent(player.transform, false);
            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.sharedMaterial = sparks.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            trail.time = 0.25f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.2f;
            trail.startColor = new Color(0.6f, 0.3f, 1f, 0.6f);
            trail.endColor = new Color(0.2f, 1f, 1f, 0f);
            trail.sortingOrder = 3;
            trail.emitting = false;
            SetReference(feedback, "trail", trail);
            SetReference(controller, "feedback", feedback);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, "Assets/Prefabs/Player.prefab");
            UnityEngine.Object.DestroyImmediate(player);
            return prefab.GetComponent<FlapPlayer>();
        }

        private static PipeGate CreatePipePrefab(Sprite square, Material material)
        {
            var pipe = new GameObject("Pipe Pair");
            Rigidbody2D body = pipe.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            BoxCollider2D gate = pipe.AddComponent<BoxCollider2D>();
            gate.isTrigger = true;
            gate.offset = new Vector2(1.35f, 0f);
            gate.size = new Vector2(0.15f, 3.6f);
            pipe.AddComponent<PipeGate>();
            CreatePipeSection("Upper Pipe", pipe.transform, 6.8f, square, material);
            CreatePipeSection("Lower Pipe", pipe.transform, -6.8f, square, material);
            CreatePipeCap("Upper Rim", pipe.transform, 1.94f, square, material);
            CreatePipeCap("Lower Rim", pipe.transform, -1.94f, square, material);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(pipe, "Assets/Prefabs/PipePair.prefab");
            UnityEngine.Object.DestroyImmediate(pipe);
            return prefab.GetComponent<PipeGate>();
        }

        private static void CreatePipeSection(string name, Transform parent, float height, Sprite square, Material material)
        {
            var pipe = new GameObject(name);
            pipe.transform.SetParent(parent, false);
            pipe.transform.localPosition = new Vector2(0f, height);
            pipe.transform.localScale = new Vector3(1f, 10f, 1f);
            pipe.tag = "Obstacle";
            pipe.AddComponent<BoxCollider2D>().size = Vector2.one;
            Artwork("Pipe Artwork", pipe.transform, FlapDashArtSetup.PipeBody, material, Vector2.zero, Vector2.one, 1);
        }

        private static void CreatePipeCap(string name, Transform parent, float height, Sprite square, Material material)
        {
            var cap = new GameObject(name);
            cap.transform.SetParent(parent, false);
            cap.transform.localPosition = new Vector2(0f, height);
            cap.transform.localScale = new Vector3(1.2f, 0.28f, 1f);
            cap.tag = "Obstacle";
            cap.AddComponent<BoxCollider2D>().size = Vector2.one;
            GameObject artwork = Artwork("Rim Artwork", cap.transform, FlapDashArtSetup.PipeRim, material, Vector2.zero, Vector2.one, 2);
            artwork.GetComponent<SpriteRenderer>().flipY = height > 0f;
        }

        private static GameObject Artwork(string name, Transform parent, Sprite sprite, Material material, Vector2 position, Vector2 size, int order)
        {
            Vector2 scale = new(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y);
            return SpritePart(name, parent, sprite, material, Color.white, position, scale, order);
        }

        private static void CreateBackdrop(RoundController round, VisualEffectsSettings settings, Material material)
        {
            var backdrop = new GameObject("Sky Backdrop");
            BackdropMotion motion = backdrop.AddComponent<BackdropMotion>();
            SetReference(motion, "round", round);
            SetReference(motion, "settings", settings);
            var layers = new Transform[4];
            for (int index = 0; index < 4; index++)
            {
                bool distant = index < 2;
                string spriteName = distant ? "cloudLayer2" : "cloudLayer1";
                float x = index % 2 == 0 ? 0f : 12f;
                layers[index] = Artwork($"Cloud Layer {index}", backdrop.transform,
                    FlapDashArtSetup.Background(spriteName), material,
                    new Vector2(x, distant ? 5.3f : 3.6f), new Vector2(12f, 4.7f), distant ? -12 : -11).transform;
            }
            Artwork("Distant Hills", backdrop.transform, FlapDashArtSetup.Background("hills"), material,
                new Vector2(0f, -5.5f), new Vector2(12f, 4.7f), -10).GetComponent<SpriteRenderer>().color = new Color32(166, 232, 186, 255);
            Artwork("Green Hills", backdrop.transform, FlapDashArtSetup.Background("groundLayer1"), material,
                new Vector2(0f, -6.8f), new Vector2(12f, 4.7f), -9).GetComponent<SpriteRenderer>().color = new Color32(84, 208, 129, 255);
            Artwork("Foreground Grass", backdrop.transform, FlapDashArtSetup.Background("groundLayer2"), material,
                new Vector2(0f, -8.4f), new Vector2(12f, 2.35f), 3).GetComponent<SpriteRenderer>().color = new Color32(64, 175, 108, 255);
            SetReferenceArray(motion, "layers", layers);
            SetFloatArray(motion, "speeds", new[] { 0.1f, 0.1f, 0.2f, 0.2f });
        }

        private static GameObject SpritePart(string name, Transform parent, Sprite sprite, Material material, Color color, Vector2 position, Vector2 scale, int order = 0)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = order;
            return part;
        }

        private static Sprite CreateGlowSprite()
        {
            const string path = "Assets/Art/Glow.png";
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float radius = Vector2.Distance(new Vector2(x, y), new Vector2(63.5f, 63.5f)) / 64f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - radius), 2f)));
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            ObjectDestroy(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 128f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ObjectDestroy(UnityEngine.Object value) => UnityEngine.Object.DestroyImmediate(value);

        private static ParticleSystem CreateParticles(string name, Transform parent, Material baseMaterial, Texture texture)
        {
            const string materialPath = "Assets/Materials/Particles.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(baseMaterial);
                material.SetTexture("_MainTex", texture);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var owner = new GameObject(name);
            owner.transform.SetParent(parent, false);
            ParticleSystem particles = owner.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.loop = false; main.startSpeed = 0f;
            main.useUnscaledTime = true;
            main.startLifetime = 0.45f; main.startSize = 0.1f; main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.3f, 1f, 0.9f, 1f);
            var emission = particles.emission; emission.enabled = false;
            var shape = particles.shape; shape.enabled = false;
            var velocity = particles.velocityOverLifetime; velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-2f, 2f);
            velocity.y = new ParticleSystem.MinMaxCurve(-2f, 2f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var colors = particles.colorOverLifetime; colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(new Color(0.4f, 1f, 1f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.9f), 1f) }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colors.color = gradient;
            ParticleSystemRenderer renderer = owner.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material; renderer.sortingOrder = 7;
            return particles;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
        }

        private static void CreateBoundary(string name, float height, Sprite square, Material material)
        {
            GameObject boundary = SpritePart(name, null, square, material, Color.clear, new Vector2(0f, height), new Vector2(12f, 0.45f), 3);
            boundary.tag = "Obstacle";
            boundary.AddComponent<BoxCollider2D>();
        }

        private static void CreateInterface(RoundController round, TMP_FontAsset font, InputActionAsset controls, FlapPlayer player)
        {
            var canvasObject = new GameObject("Round View", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540f, 960f);
            scaler.matchWidthOrHeight = 1f;
            RoundView view = canvasObject.AddComponent<RoundView>();
            SetReference(view, "round", round);
            VisualEffectsSettings settings = canvasObject.AddComponent<VisualEffectsSettings>();
            RoundFeedback feedback = canvasObject.AddComponent<RoundFeedback>();
            SetReference(settings, "round", round);
            SetReference(feedback, "settings", settings);
            SetReference(round, "feedback", feedback);
            SetReference(feedback, "player", player.transform);
            PlayerFeedback playerFeedback = player.GetComponent<PlayerFeedback>();
            SetReference(playerFeedback, "round", round);
            SetReference(playerFeedback, "settings", settings);
            SetReference(feedback, "playerFeedback", playerFeedback);
            SetReference(view, "feedback", feedback);
            CreateBackdrop(round, settings, AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FlatSprite.mat"));
            SetReference(feedback, "celebration", CreateParticles("Pass Sparks", null, AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FlatSprite.mat"), AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Circle.png")));

            RectTransform header = PanelRect("Header", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(484f, 112f), Panel);
            Label("Brand", header, font, "FLAP DASH", 17f, Mint, new Vector2(0f, 34f), new Vector2(440f, 24f), TextAlignmentOptions.Left);
            TMP_Text score = Label("Score", header, font, "PIPES  0", 31f, Ink, new Vector2(-105f, -12f), new Vector2(220f, 48f), TextAlignmentOptions.Left);
            score.rectTransform.pivot = new Vector2(0f, 0.5f);
            score.rectTransform.anchoredPosition = new Vector2(-215f, -12f);
            TMP_Text time = Label("Timer", header, font, "60s", 31f, Ink, new Vector2(140f, -12f), new Vector2(150f, 48f), TextAlignmentOptions.Right);
            RectTransform footer = PanelRect("Controls", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(484f, 40f), Panel);
            Label("Controls Hint", footer, font, "SPACE / CLICK / TAP TO FLAP", 15f, Muted, Vector2.zero, new Vector2(470f, 32f), TextAlignmentOptions.Center);

            RectTransform overlay = PanelRect("Round Panel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -5f), new Vector2(456f, 370f), Panel);
            var portrait = new GameObject("Bird Portrait", typeof(RectTransform), typeof(Image));
            portrait.transform.SetParent(overlay, false);
            RectTransform portraitRect = (RectTransform)portrait.transform;
            portraitRect.sizeDelta = new Vector2(150f, 112f);
            portraitRect.anchoredPosition = new Vector2(0f, 180f);
            Image portraitImage = portrait.GetComponent<Image>();
            portraitImage.sprite = FlapDashArtSetup.BirdFrames()[0];
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
            TMP_Text title = Label("Title", overlay, font, "FLAP DASH", 38f, Ink, new Vector2(0f, 80f), new Vector2(408f, 60f), TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            TMP_Text message = Label("Message", overlay, font, "Time your taps.\nPass the pipes.\n1 point per pipe.", 21f, Muted, new Vector2(0f, -10f), new Vector2(394f, 112f), TextAlignmentOptions.Center);
            RectTransform buttonRect = PanelRect("Primary Button", overlay, new Vector2(0.5f, 0.5f), new Vector2(0f, -127f), new Vector2(368f, 68f), Color.white);
            Button button = buttonRect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color32(255, 249, 219, 255);
            colors.pressedColor = new Color32(225, 191, 132, 255);
            button.colors = colors;
            TMP_Text buttonLabel = Label("Label", buttonRect, font, "START ROUND", 22f, Ink, new Vector2(0f, 3f), new Vector2(340f, 52f), TextAlignmentOptions.Center);
            buttonLabel.fontStyle = FontStyles.Bold;

            SetReference(view, "scoreLabel", score);
            SetReference(view, "timeLabel", time);
            SetReference(view, "titleLabel", title);
            SetReference(view, "messageLabel", message);
            SetReference(view, "buttonLabel", buttonLabel);
            SetReference(view, "primaryButton", button);
            SetReference(view, "overlayPanel", overlay.gameObject);
            SetReference(feedback, "score", score.rectTransform);
            SetReference(feedback, "panel", overlay);
            SetReference(feedback, "panelGroup", overlay.gameObject.AddComponent<CanvasGroup>());
            ButtonFeedback buttonFeedback = button.gameObject.AddComponent<ButtonFeedback>();
            SetReference(buttonFeedback, "settings", settings);
            RectTransform energy = PanelRect("Effects Settings", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0f, 137f), new Vector2(484f, 90f), Panel);
            TMP_Text energyLabel = Label("Energy Label", energy, font, "VISUAL ENERGY  100%", 15f, Muted, new Vector2(0f, 23f), new Vector2(440f, 25f), TextAlignmentOptions.Center);
            RectTransform track = PanelRect("Energy Slider", energy, new Vector2(0.5f, 0.5f), new Vector2(0f, -15f), new Vector2(400f, 20f), Color.white);
            RectTransform fill = PanelRect("Slider Fill", track, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 20f), Color.white);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().raycastTarget = false;
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            RectTransform handle = PanelRect("Handle", track, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(27f, 36f), Color.white);
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            SetReference(settings, "slider", slider);
            SetReference(settings, "label", energyLabel);
            SetReference(settings, "panel", energy.gameObject);
            CreateEventSystem(controls, button.gameObject);
        }

        private static RectTransform PanelRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.GetComponent<Image>();
            image.color = color;
            image.sprite = name switch
            {
                "Primary Button" => FlapDashArtSetup.Button,
                "Energy Slider" => FlapDashArtSetup.SliderTrack,
                "Slider Fill" => FlapDashArtSetup.SliderFill,
                "Handle" => FlapDashArtSetup.SliderHandle,
                "Controls" => FlapDashArtSetup.FooterPanel,
                _ => FlapDashArtSetup.Panel
            };
            image.type = name == "Handle" ? Image.Type.Simple : Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2f;
            if (name == "Header" || name == "Round Panel" || name == "Effects Settings" || name == "Primary Button")
            {
                Shadow shadow = panel.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.1f, 0.25f, 0.35f, 0.22f);
                shadow.effectDistance = new Vector2(0f, -6f);
            }
            return rect;
        }

        private static TMP_Text Label(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color, Vector2 position, Vector2 bounds, TextAlignmentOptions alignment, Vector2? anchor = null)
        {
            var label = new GameObject(name, typeof(RectTransform));
            label.transform.SetParent(parent, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = anchor ?? new Vector2(0.5f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.anchoredPosition = position;
            rect.sizeDelta = bounds;
            TextMeshProUGUI text = label.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateEventSystem(InputActionAsset controls, GameObject firstSelected)
        {
            var system = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            EventSystem eventSystem = system.GetComponent<EventSystem>();
            eventSystem.firstSelectedGameObject = firstSelected;
            InputSystemUIInputModule module = system.GetComponent<InputSystemUIInputModule>();
            module.actionsAsset = controls;
            InputActionReference[] references = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controls)).OfType<InputActionReference>().ToArray();
            module.point = references.First(reference => reference.action.name == "Point");
            module.leftClick = references.First(reference => reference.action.name == "Click");
            module.submit = references.First(reference => reference.action.name == "Submit");
            module.cancel = references.First(reference => reference.action.name == "Cancel");
            module.move = references.First(reference => reference.action.name == "Navigate");
        }

        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null)
                throw new InvalidOperationException($"Missing serialized field {target.GetType().Name}.{name}");
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInteger(UnityEngine.Object target, string name, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(UnityEngine.Object target, string name, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloatArray(UnityEngine.Object target, string name, float[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(name);
            array.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                array.GetArrayElementAtIndex(index).floatValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReferenceArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty array = serialized.FindProperty(name);
            array.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
                array.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
