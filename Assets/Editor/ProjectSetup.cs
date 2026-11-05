#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeonDash.EditorTools
{
    /// <summary>
    /// Zero-click project setup: on first open (and from the menu) this creates the single scene
    /// the game needs - a camera plus one <see cref="NeonDashApp"/> object. Everything else is
    /// generated at runtime, so the scene stays tiny and merge-friendly.
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/NeonDash.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            EnsureNeonMaterial();

            if (!File.Exists(ScenePath))
            {
                CreateScene();
                return;
            }
            EnsureBuildSettings();
        }

        [MenuItem("Geometry Dash/Open Main Scene")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Geometry Dash/Setup Scene")]
        public static void SetupScene()
        {
            CreateScene();
        }

        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath)) CreateScene();
        }

        static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.016f, 0.026f, 0.070f, 1f);
            camera.transform.position = new Vector3(0f, 5.2f, -10f);

            var appObject = new GameObject("Geometry Dash");
            appObject.AddComponent<NeonDashApp>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EnsureBuildSettings();

            Debug.Log("Geometry Dash: created " + ScenePath + " and added it to the build settings.");
        }

        /// <summary>
        /// Stores the unlit shader in a material asset under Resources. A material asset keeps the
        /// shader in the build; Shader.Find alone can be stripped in a WebGL player.
        /// </summary>
        static void EnsureNeonMaterial()
        {
            const string materialPath = "Assets/Resources/NeonUnlit.mat";
            if (File.Exists(materialPath)) return;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) return;

            Directory.CreateDirectory("Assets/Resources");
            var material = new Material(shader);
            material.name = "Neon Unlit";
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
        }

        static void EnsureBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
                if (scenes[i].path == ScenePath) return;

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
#endif
