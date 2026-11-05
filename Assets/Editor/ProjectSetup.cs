#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BullRide.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/BullRide.unity";

        static ProjectSetup()
        {
            EditorApplication.delayCall += AutoSetup;
        }

        static void AutoSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EnsureMaterial();
            if (!File.Exists(ScenePath))
            {
                CreateScene();
                return;
            }
            EnsureBuildSettings();
        }

        [MenuItem("Bull Ride/Open Main Scene")]
        public static void OpenScene()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Bull Ride/Setup Scene")]
        public static void SetupScene()
        {
            EnsureMaterial();
            CreateScene();
        }

        public static void EnsureScene()
        {
            EnsureMaterial();
            if (!File.Exists(ScenePath)) CreateScene();
        }

        static void CreateScene()
        {
            EnsureMaterial();
            Directory.CreateDirectory("Assets/Scenes");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 50f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.07f, 0.16f, 1f);
            camera.transform.position = new Vector3(0f, 4.2f, -9.5f);
            camera.transform.rotation = Quaternion.Euler(18f, 0f, 0f);

            var app = new GameObject("Bull Ride");
            app.AddComponent<BullRideApp>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            EnsureBuildSettings();
            Debug.Log("Bull Ride: created " + ScenePath);
        }

        static void EnsureMaterial()
        {
            const string materialPath = "Assets/Resources/BullRideUnlit.mat";
            if (File.Exists(materialPath)) return;
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return;
            Directory.CreateDirectory("Assets/Resources");
            var material = new Material(shader);
            material.name = "BullRide Unlit";
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
