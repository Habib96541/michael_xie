#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CyberScratch.Editor
{
    public static class WebGLBuilder
    {
        public static readonly string[] BuildScenes = new string[]
        {
            "Assets/Scenes/MainMenuScene.unity",
            "Assets/Scenes/GameScene.unity"
        };

        [MenuItem("CyberScratch/Build WebGL", false, 100)]
        public static void Build()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string buildFolder = Path.Combine(projectRoot, "Build", "WebGL");

            if (Directory.Exists(buildFolder))
            {
                try
                {
                    Directory.Delete(buildFolder, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[WebGLBuilder] Could not clean folder before build: {ex.Message}");
                }
            }
            Directory.CreateDirectory(buildFolder);

            Debug.Log($"[WebGLBuilder] Starting WebGL build to: {buildFolder}");

            // Configure WebGL player settings
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "PROJECT:CyberScratchTemplate";
            PlayerSettings.WebGL.dataCaching = true;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = BuildScenes,
                locationPathName = buildFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuilder] WebGL build succeeded! Total size: {summary.totalSize} bytes, Time: {summary.totalTime}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            else
            {
                Debug.LogError($"[WebGLBuilder] WebGL build failed with result: {summary.result}, Errors: {summary.totalErrors}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
#endif