using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DiceGame.Editor
{
    public static class WebGLBuilder
    {
        [MenuItem("DiceGame/Build WebGL", false, 20)]
        public static void Build()
        {
            string projectDir = Directory.GetCurrentDirectory();
            string buildPath = Path.Combine(projectDir, "build");

            if (!Directory.Exists(buildPath))
            {
                Directory.CreateDirectory(buildPath);
            }

            string[] scenes = new string[]
            {
                "Assets/Scenes/LoadingScene.unity",
                "Assets/Scenes/DiceOverdrive.unity"
            };

            for (int i = 0; i < scenes.Length; i++)
            {
                if (!File.Exists(scenes[i]))
                {
                    throw new FileNotFoundException("Scene not found: " + scenes[i]);
                }
            }

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.threadsSupport = false;

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGL build succeeded: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s");
                PostProcessIndexHtml(buildPath);
            }
            else
            {
                Debug.LogError($"WebGL build failed with result: {summary.result}, errors: {summary.totalErrors}");
                throw new Exception($"Build failed with {summary.totalErrors} errors.");
            }
        }

        private static void PostProcessIndexHtml(string buildPath)
        {
            string indexPath = Path.Combine(buildPath, "index.html");
            if (!File.Exists(indexPath)) return;

            string html = File.ReadAllText(indexPath);

            if (!html.Contains("name=\"viewport\""))
            {
                html = html.Replace("<head>", "<head>\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes\">");
            }

            File.WriteAllText(indexPath, html);

            string stylePath = Path.Combine(buildPath, "TemplateData", "style.css");
            if (File.Exists(stylePath))
            {
                string css = File.ReadAllText(stylePath);
                if (!css.Contains("body, html"))
                {
                    css = "body, html { width: 100%; height: 100%; margin: 0; padding: 0; overflow: hidden; background: #080c10; }\n" + css;
                    File.WriteAllText(stylePath, css);
                }
            }
        }
    }
}
