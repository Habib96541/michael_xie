using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace com.javierquevedo.Editor
{
    /// <summary>
    /// Authors the presentation as real scene assets instead of runtime-only stages:
    ///   Assets/Scenes/Boot.unity      -> "Loading Screen" object
    ///   Assets/Scenes/MainMenu.unity  -> "Main UI" object
    ///
    /// The new scenes are created additively, saved, then closed, so the scene the user
    /// is currently editing is never replaced. Runs once after a script reload and can
    /// be re-run from the CyberOrbs menu.
    /// </summary>
    public static class CyberOrbsSceneSetup
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/Bust a Move.unity";

        private const string SessionKey = "CyberOrbs.SceneSetup.v2";
        private const double RetrySeconds = 20.0;
        private static double _retryUntil;

        [InitializeOnLoadMethod]
        private static void AutoSetup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += SetupWhenReady;
        }

        [MenuItem("CyberOrbs/Setup Loading + Main UI Scenes")]
        public static void SetupScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[CyberOrbsSetup] Stop Play Mode before creating the loading and main UI scenes.");
                return;
            }

            Directory.CreateDirectory("Assets/Scenes");

            bool created = false;
            if (!File.Exists(BootScenePath))
            {
                CreateBootScene();
                created = true;
            }
            if (!File.Exists(MenuScenePath))
            {
                CreateMenuScene();
                created = true;
            }

            EnsureBuildSettings();
            AssetDatabase.SaveAssets();

            // Make the Editor's Play button start at the boot scene even when the gameplay
            // scene is the one currently open. Builds already start at build index 0.
            SceneAsset bootSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
            if (bootSceneAsset != null)
                EditorSceneManager.playModeStartScene = bootSceneAsset;

            if (created)
            {
                AssetDatabase.Refresh();
                Object sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath);
                if (sceneAsset == null)
                    sceneAsset = AssetDatabase.LoadMainAssetAtPath(MenuScenePath);
                EditorGUIUtility.PingObject(sceneAsset);
            }

            Debug.Log("[CyberOrbsSetup] Boot scene: " + BootScenePath +
                      " | Main UI scene: " + MenuScenePath +
                      " | Build order: Boot, MainMenu, Bust a Move." +
                      " | Play Mode starts at Boot.unity.");
        }

        [MenuItem("CyberOrbs/Open Loading Scene")]
        public static void OpenBootScene()
        {
            OpenScene(BootScenePath);
        }

        [MenuItem("CyberOrbs/Open Main UI Scene")]
        public static void OpenMenuScene()
        {
            OpenScene(MenuScenePath);
        }

        private static void OpenScene(string path)
        {
            if (!File.Exists(path))
                SetupScenes();

            if (!File.Exists(path)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }

        private static void SetupWhenReady()
        {
            if (SessionState.GetBool(SessionKey, false)) return;

            // Never rewrite scenes while entering or during Play Mode.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (_retryUntil <= 0.0)
                    _retryUntil = EditorApplication.timeSinceStartup + RetrySeconds;

                if (EditorApplication.timeSinceStartup < _retryUntil)
                {
                    EditorApplication.delayCall += SetupWhenReady;
                    return;
                }
            }

            SessionState.SetBool(SessionKey, true);
            SetupScenes();
        }

        private static void CreateBootScene()
        {
            CreateSceneWithContent(BootScenePath, delegate
            {
                CreateCamera("Camera", new Color(0.02f, 0.03f, 0.05f, 1f));

                GameObject loader = new GameObject("Loading Screen");
                LoadingScreenController controller = loader.AddComponent<LoadingScreenController>();
                controller.NextSceneName = "MainMenu";
                loader.AddComponent<LoadingScreenGUI>();
            });
        }

        private static void CreateMenuScene()
        {
            CreateSceneWithContent(MenuScenePath, delegate
            {
                CreateCamera("Camera", new Color(0.035f, 0.045f, 0.075f, 1f));

                GameObject ui = new GameObject("Main UI");
                SplashScreenController controller = ui.AddComponent<SplashScreenController>();
                controller.GameSceneName = "Bust a Move";
                ui.AddComponent<SplashScreenGUI>();
            });
        }

        private static void CreateSceneWithContent(string path, System.Action build)
        {
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                // New GameObjects are created in the active scene.
                SceneManager.SetActiveScene(scene);
                build();

                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene, path))
                    Debug.LogError("[CyberOrbsSetup] Could not save " + path);
            }
            finally
            {
                if (previous.IsValid())
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void CreateCamera(string name, Color background)
        {
            GameObject go = new GameObject(name);
            go.tag = "MainCamera";

            Camera cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureBuildSettings()
        {
            string[] front =
            {
                BootScenePath,
                MenuScenePath,
                GameScenePath
            };

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            for (int i = 0; i < front.Length; i++)
                scenes.Add(new EditorBuildSettingsScene(front[i], true));

            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                bool already = false;
                for (int j = 0; j < front.Length; j++)
                {
                    if (existing[i].path == front[j])
                    {
                        already = true;
                        break;
                    }
                }
                if (!already)
                    scenes.Add(existing[i]);
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
