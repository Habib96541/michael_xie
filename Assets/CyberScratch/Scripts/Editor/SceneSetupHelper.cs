#if UNITY_EDITOR
using System.IO;
using CyberScratch.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CyberScratch.Editor
{
    [InitializeOnLoad]
    public static class SceneSetupHelper
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
        public const string GameScenePath = "Assets/Scenes/GameScene.unity";
        public const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        static SceneSetupHelper()
        {
            EditorApplication.delayCall += AutoSetupScenesIfNeeded;
            EditorApplication.update += OnceUpdate;
        }

        private static void OnceUpdate()
        {
            EditorApplication.update -= OnceUpdate;
            AutoSetupScenesIfNeeded();
        }

        private static void AutoSetupScenesIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.playModeStateChanged -= RebuildWhenEditMode;
                EditorApplication.playModeStateChanged += RebuildWhenEditMode;
                return;
            }

            bool mainMenuReady = File.Exists(MainMenuScenePath);
            bool gameSceneReady = File.Exists(GameScenePath);

            if (!mainMenuReady || !gameSceneReady)
            {
                SetupTwoScenes();
            }
        }

        private static void RebuildWhenEditMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= RebuildWhenEditMode;
            EditorApplication.delayCall += AutoSetupScenesIfNeeded;
        }

                [MenuItem("CyberScratch/Refresh Active Scene", false, 0)]
        public static void RefreshActiveScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[CyberScratch] Stop Play mode first.");
                return;
            }

            Scene activeScene = EditorSceneManager.GetActiveScene();
            string sceneName = activeScene.name;

            CyberArtGenerator.GenerateAllAssets();

            GameObject[] rootObjects = activeScene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                Object.DestroyImmediate(rootObjects[i]);
            }

            if (sceneName == "MainMenuScene")
            {
                CyberScratchBootstrap.BuildMainMenuSceneHierarchy();
            }
            else
            {
                CyberScratchBootstrap.BuildGameSceneHierarchy();
            }

            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.SaveScene(activeScene);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject canvas = GameObject.Find("CyberScratch_Canvas");
            if (canvas != null)
            {
                Selection.activeGameObject = canvas;
                EditorGUIUtility.PingObject(canvas);
            }

            Debug.Log($"[CyberScratch] Refreshed {sceneName}.");
        }

        [MenuItem("CyberScratch/Setup Two Scenes (MainMenu & GameScene)", false, 1)]
        public static void SetupTwoScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[CyberScratch] Stop Play mode first.");
                return;
            }

            CyberArtGenerator.GenerateAllAssets();

            // 1. Build and save GameScene
            Scene gameScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CyberScratchBootstrap.BuildGameSceneHierarchy();
            EditorSceneManager.MarkSceneDirty(gameScene);
            EditorSceneManager.SaveScene(gameScene, GameScenePath);

            // 2. Build and save MainMenuScene
            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CyberScratchBootstrap.BuildMainMenuSceneHierarchy();
            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);

            // 3. Register both scenes in EditorBuildSettings
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            GameObject canvas = GameObject.Find("CyberScratch_Canvas");
            if (canvas != null)
            {
                Selection.activeGameObject = canvas;
                EditorGUIUtility.PingObject(canvas);
            }

            Debug.Log("[CyberScratch] Configured scenes: MainMenuScene (0) and GameScene (1).");
        }

        [MenuItem("CyberScratch/Setup CyberScratch Scene (Legacy / Single)", false, 10)]
        public static void SetupCompleteGameScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[CyberScratch] Stop Play mode first.");
                return;
            }

            CyberArtGenerator.GenerateAllAssets();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != SampleScenePath)
            {
                if (File.Exists(SampleScenePath))
                {
                    scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
                }
                else
                {
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null)
                {
                    Object.DestroyImmediate(roots[i]);
                }
            }

            CyberScratchBootstrap.BuildCompleteGameHierarchy();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, SampleScenePath);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
