using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Crash.EditorTools
{
    public static class BuildGame
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string OutPath = "Build/WebGL";

        [MenuItem("Crash/Setup Project")]
        public static void Setup()
        {
            EnsureScene();
            ConfigurePlayer();
            AssetDatabase.SaveAssets();
        }

        static void EnsureScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("SceneCamera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.012f, 0.04f, 1f);
            cam.orthographic = true;
            cam.cullingMask = 0;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 10f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Crash Games";
            PlayerSettings.productName = "Crash";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.stripEngineCode = true;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Release);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:Crash";
            PlayerSettings.WebGL.linkerTarget = WebGLLinkerTarget.Wasm;
        }

        [MenuItem("Crash/Build WebGL")]
        public static void BuildWeb()
        {
            Setup();

            if (Directory.Exists(OutPath))
            {
                try { Directory.Delete(OutPath, true); } catch { }
            }

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log("[BUILD] " + summary.result + " size=" + summary.totalSize + " time=" + summary.totalTime);
            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        [MenuItem("Crash/Compile Check")]
        public static void CompileCheck()
        {
            Debug.Log("[COMPILE] project loaded, scripts compiled");
            EditorApplication.Exit(0);
        }

        [MenuItem("Crash/Run Self Test")]
        public static void RunSelfTest()
        {
            SelfTest.Run();
        }
    }
}
