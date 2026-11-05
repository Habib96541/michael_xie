using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EggDrop.Editor
{
    public static class EggDropSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/EggDrop.unity";
        private const string ArtFolder = "Assets/Art";

        [InitializeOnLoadMethod]
        private static void ScheduleSetup()
        {
            if (!File.Exists(ScenePath)) EditorApplication.delayCall += BuildWhenReady;
        }

        private static void BuildWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += BuildWhenReady;
                return;
            }
            if (File.Exists(ScenePath)) return;
            try { CreateScene(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        [MenuItem("Egg Drop/Open Game Scene")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            else if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        public static void CreateScene()
        {
            if (File.Exists(ScenePath)) return;
            Directory.CreateDirectory(ArtFolder);
            Directory.CreateDirectory("Assets/UI");
            Directory.CreateDirectory("Assets/Scenes");
            EggDropSoundBuilder.CreateSounds();
            Scene previousScene = SceneManager.GetActiveScene();
            bool previousWasDirty = previousScene.isDirty;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                CreateContents();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            if (!previousWasDirty && !Application.isBatchMode) EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("Egg Drop scene created. Open Assets/Scenes/EggDrop.unity and press Play.");
        }

        private static void CreateContents()
        {
            Material cream = Material("Shell", new Color(0.98f, 0.91f, 0.75f));
            Material dark = Material("Ink", new Color(0.10f, 0.18f, 0.15f));
            Material coral = Material("Coral", new Color(0.88f, 0.40f, 0.28f));
            Material sage = Material("Sage", new Color(0.52f, 0.68f, 0.57f));
            Material moss = Material("Moss", new Color(0.25f, 0.40f, 0.31f));
            Material mint = Material("Mint", new Color(0.72f, 0.81f, 0.67f));
            Material gold = Material("Yolk", new Color(1f, 0.65f, 0.12f));
            Material ivory = Material("Platform", new Color(0.88f, 0.86f, 0.72f));
            Material sky = Material("Backdrop", new Color(0.72f, 0.81f, 0.73f));
            cream.SetFloat("_Smoothness", 0.34f);
            cream.SetFloat("_Cull", 0f);
            gold.SetFloat("_Smoothness", 0.65f);

            GameObject environment = new GameObject("Environment");
            Primitive("Ground", PrimitiveType.Cube, new Vector3(0, -0.8f, 1), new Vector3(100, 0.2f, 100), sky, environment.transform);
            Primitive("Platform base", PrimitiveType.Cylinder, new Vector3(0, -0.4f, 0), new Vector3(4.5f, 0.3f, 3.9f), moss, environment.transform);
            Primitive("Landing pad", PrimitiveType.Cylinder, new Vector3(0, -0.08f, 0), new Vector3(4.5f, 0.05f, 3.9f), ivory, environment.transform);
            GameObject surface = new GameObject("Landing Surface");
            surface.transform.SetParent(environment.transform);
            surface.transform.position = new Vector3(0, -0.08f, 0);
            BoxCollider landingCollider = surface.AddComponent<BoxCollider>();
            landingCollider.size = new Vector3(4.5f, 0.1f, 3.9f);
            for (int index = 0; index < 3; index++)
            {
                Primitive("Pad stripe " + index, PrimitiveType.Cube, new Vector3(-1.2f + index * 1.2f, -0.025f, -1.25f),
                    new Vector3(0.6f, 0.012f, 0.07f), coral, environment.transform);
            }

            GameObject tower = new GameObject("Floor Ruler");
            tower.transform.SetParent(environment.transform);
            Primitive("Ruler spine", PrimitiveType.Cube, new Vector3(-2.65f, 7.1f, 1.4f), new Vector3(0.12f, 15.7f, 0.12f), sage, tower.transform);
            Transform[] floorMarkers = new Transform[EggDropRound.MaximumFloor];
            for (int index = 0; index < floorMarkers.Length; index++)
            {
                float height = EggDropStage.HeightFor(index + 1);
                floorMarkers[index] = Primitive("Floor " + (index + 1).ToString("00"), PrimitiveType.Cube,
                    new Vector3(-2.35f, height, 1.4f), new Vector3(0.65f, 0.065f, 0.13f), mint, tower.transform).transform;
            }
            GameObject marker = new GameObject("Launch Bracket");
            marker.transform.position = new Vector3(0, EggDropStage.HeightFor(1) - 0.7f, 0);
            marker.transform.SetParent(environment.transform);
            Primitive("Left bracket", PrimitiveType.Cube, marker.transform.position + new Vector3(-0.85f, 0, 0),
                new Vector3(0.45f, 0.08f, 0.9f), coral, marker.transform);
            Primitive("Right bracket", PrimitiveType.Cube, marker.transform.position + new Vector3(0.85f, 0, 0),
                new Vector3(0.45f, 0.08f, 0.9f), coral, marker.transform);

            CreateScenery(environment.transform, sage, mint, ivory);
            Camera camera = CreateCamera();
            CreateLighting();
            EggBody egg = CreateEgg(cream, dark, coral, gold, landingCollider);
            GameObject systems = new GameObject("Game");
            EggDropGame game = systems.AddComponent<EggDropGame>();
            EggDropAudio audio = CreateAudio(systems.transform);
            EggDropStage stage = systems.AddComponent<EggDropStage>();
            Reference(stage, "stageCamera", camera);
            Reference(stage, "launchMarker", marker.transform);
            Reference(stage, "upcomingMaterial", mint);
            Reference(stage, "clearedMaterial", moss);
            Reference(stage, "currentMaterial", coral);
            var stageObject = new SerializedObject(stage);
            SerializedProperty markers = stageObject.FindProperty("floorMarkers");
            markers.arraySize = floorMarkers.Length;
            for (int index = 0; index < floorMarkers.Length; index++) markers.GetArrayElementAtIndex(index).objectReferenceValue = floorMarkers[index];
            stageObject.ApplyModifiedPropertiesWithoutUndo();
            EggDropUI gameInterface = CreateInterface(game, audio);
            Reference(game, "egg", egg);
            Reference(game, "gameInterface", gameInterface);
            Reference(game, "gameAudio", audio);
            Reference(game, "stage", stage);
        }

        private static EggBody CreateEgg(Material shellMaterial, Material dark, Material coral, Material gold, Collider surface)
        {
            GameObject egg = new GameObject("Egg");
            egg.transform.position = new Vector3(0, EggDropStage.HeightFor(1), 0);
            Rigidbody body = egg.AddComponent<Rigidbody>();
            body.mass = 0.07f;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            SphereCollider collider = egg.AddComponent<SphereCollider>();
            collider.radius = 0.46f;
            collider.center = new Vector3(0, -0.15f, 0);
            EggBody behaviour = egg.AddComponent<EggBody>();
            GameObject intact = MeshObject("Intact shell", EggMesh("Egg", 0f, Mathf.PI), shellMaterial, egg.transform);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive("Eye", PrimitiveType.Sphere, egg.transform.position + new Vector3(side * 0.14f, 0.10f, -0.437f),
                    new Vector3(0.07f, 0.09f, 0.065f), dark, intact.transform);
                Primitive("Cheek", PrimitiveType.Sphere, egg.transform.position + new Vector3(side * 0.265f, -0.035f, -0.394f),
                    new Vector3(0.10f, 0.048f, 0.022f), coral, intact.transform);
            }
            GameObject smile = new GameObject("Smile");
            smile.transform.SetParent(intact.transform, false);
            LineRenderer line = smile.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = dark;
            line.startWidth = 0.019f;
            line.endWidth = 0.019f;
            line.numCapVertices = 4;
            line.positionCount = 9;
            for (int index = 0; index < 9; index++)
            {
                float fraction = index / 8f;
                line.SetPosition(index, new Vector3(Mathf.Lerp(-0.075f, 0.075f, fraction), -0.025f - Mathf.Sin(fraction * Mathf.PI) * 0.04f, -0.468f));
            }
            Reference(behaviour, "landingSurface", surface);
            Reference(behaviour, "intactShell", intact.transform);
            Reference(behaviour, "fracture", EggDropFractureBuilder.CreateEffect(egg.transform));
            return behaviour;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            camera.orthographic = true;
            camera.orthographicSize = 3.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.72f, 0.81f, 0.73f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 150;
            camera.rect = new Rect(0, 0, 1, 1);
            Vector3 focus = new Vector3(1.5f, 2.45f, 0);
            camera.transform.position = focus + new Vector3(5, 3.2f, -18);
            camera.transform.LookAt(focus);
            return camera;
        }

        private static void CreateLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.83f, 0.91f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.72f, 0.64f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.49f, 0.39f);
            RenderSettings.skybox = null;
            GameObject lightObject = new GameObject("Sun");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.92f, 0.79f);
            light.intensity = 1.8f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            lightObject.transform.rotation = Quaternion.Euler(38, -32, 0);
            RenderSettings.sun = light;
        }

        private static void CreateScenery(Transform parent, Material sage, Material mint, Material ivory)
        {
            for (int index = 0; index < 5; index++)
            {
                float x = -8 + index * 4.5f;
                float height = index % 2 == 0 ? 2.5f : 4f;
                Primitive("Distant block " + index, PrimitiveType.Cube, new Vector3(x, height * 0.5f - 0.8f, 8),
                    new Vector3(2.4f, height, 2), index % 2 == 0 ? sage : mint, parent);
                for (int window = 0; window < 3; window++)
                    Primitive("Window " + window, PrimitiveType.Cube, new Vector3(x - 0.6f + window * 0.6f, height - 1.4f, 6.98f),
                        new Vector3(0.22f, 0.45f, 0.025f), ivory, parent);
            }
        }

        private static EggDropAudio CreateAudio(Transform parent)
        {
            GameObject audioObject = new GameObject("Audio");
            audioObject.transform.SetParent(parent);
            EggDropAudio audio = audioObject.AddComponent<EggDropAudio>();
            Reference(audio, "effectsSource", Source(audioObject, null, false));
            Reference(audio, "musicSource", Source(audioObject, "Music", true));
            Reference(audio, "fallingSource", Source(audioObject, "Falling", true));
            Reference(audio, "dropClip", Clip("Drop"));
            Reference(audio, "landingClip", Clip("Landing"));
            Reference(audio, "successClip", Clip("Success"));
            Reference(audio, "collectClip", Clip("Collect"));
            Reference(audio, "crackClip", Clip("Crack"));
            Reference(audio, "clickClip", Clip("Click"));
            return audio;
        }

        private static AudioSource Source(GameObject target, string clip, bool loop)
        {
            AudioSource source = target.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0;
            source.dopplerLevel = 0;
            if (clip != null) source.clip = Clip(clip);
            return source;
        }

        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + name + ".wav");

        private static EggDropUI CreateInterface(EggDropGame game, EggDropAudio audio)
        {
            const string panelPath = "Assets/UI/EggDropPanelSettings.asset";
            PanelSettings panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1440, 960);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = 1;
                AssetDatabase.CreateAsset(panel, panelPath);
            }
            GameObject interfaceObject = new GameObject("Interface");
            UIDocument document = interfaceObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/EggDrop.uxml");
            EggDropUI gameInterface = interfaceObject.AddComponent<EggDropUI>();
            Reference(gameInterface, "game", game);
            Reference(gameInterface, "gameAudio", audio);
            return gameInterface;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            GameObject instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = scale;
            instance.TryGetComponent(out Renderer renderer);
            renderer.sharedMaterial = material;
            if (instance.TryGetComponent(out Collider collider)) UnityEngine.Object.DestroyImmediate(collider);
            return instance;
        }

        private static Material Material(string name, Color color)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color.linear };
            material.SetFloat("_Smoothness", 0.18f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject MeshObject(string name, Mesh mesh, Material material, Transform parent)
        {
            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<MeshRenderer>().sharedMaterial = material;
            return instance;
        }

        private static Mesh EggMesh(string name, float startAngle, float endAngle)
        {
            string path = ArtFolder + "/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            const int rings = 32;
            const int segments = 48;
            Vector3[] vertices = new Vector3[(rings + 1) * (segments + 1)];
            Vector2[] textureCoordinates = new Vector2[vertices.Length];
            int[] triangles = new int[rings * segments * 6];
            for (int ring = 0; ring <= rings; ring++)
            {
                float theta = Mathf.Lerp(startAngle, endAngle, ring / (float)rings);
                float radius = Mathf.Sin(theta) * 0.46f * (1 - 0.21f * Mathf.Cos(theta));
                for (int segment = 0; segment <= segments; segment++)
                {
                    float phi = segment / (float)segments * Mathf.PI * 2;
                    int index = ring * (segments + 1) + segment;
                    vertices[index] = new Vector3(Mathf.Cos(phi) * radius, Mathf.Cos(theta) * 0.65f, Mathf.Sin(phi) * radius);
                    textureCoordinates[index] = new Vector2(segment / (float)segments, ring / (float)rings);
                    if (ring == rings || segment == segments) continue;
                    int triangle = (ring * segments + segment) * 6;
                    triangles[triangle] = index;
                    triangles[triangle + 1] = index + 1;
                    triangles[triangle + 2] = index + segments + 1;
                    triangles[triangle + 3] = index + 1;
                    triangles[triangle + 4] = index + segments + 2;
                    triangles[triangle + 5] = index + segments + 1;
                }
            }
            Mesh mesh = new Mesh { name = name, vertices = vertices, uv = textureCoordinates, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static void Reference(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
