#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonDash.EditorTools
{
    /// <summary>One-command WebGL build: Geometry Dash -> Build WebGL.</summary>
    public static class BuildWebGL
    {
        const string OutputPath = "Build/WebGL";

        [MenuItem("Geometry Dash/Build WebGL")]
        public static void Build()
        {
            ProjectSetup.EnsureScene();

            PlayerSettings.companyName = "Geometry Dash Studio";
            PlayerSettings.productName = "Geometry Dash";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.memorySize = 256;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectSetup.ScenePath, true) };

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("WebGL build failed: " + report.summary.result
                                    + " (" + report.summary.totalErrors + " errors)");

            PatchWebPage(OutputPath);
            Debug.Log("Geometry Dash WebGL build complete: " + report.summary.outputPath
                      + " (" + report.summary.totalSize + " bytes)");
        }

        /// <summary>Make the generated page fill the browser and work on phones.</summary>
        static void PatchWebPage(string outputPath)
        {
            string indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath)) return;

            string html = File.ReadAllText(indexPath);
            html = html.Replace("fullscreenContainer.style.width = \"960px\";",
                                "fullscreenContainer.style.width = window.innerWidth + \"px\";");
            html = html.Replace("fullscreenContainer.style.height = \"600px\";",
                                "fullscreenContainer.style.height = window.innerHeight + \"px\";");
            html = html.Replace("<title>Unity Web Player | Geometry Dash</title>",
                                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, maximum-scale=1, user-scalable=no\"><title>Geometry Dash</title>");
            File.WriteAllText(indexPath, html);

            string cssPath = Path.Combine(outputPath, "TemplateData", "style.css");
            if (File.Exists(cssPath))
            {
                File.AppendAllText(cssPath,
                    "\nhtml,body{width:100%;height:100%;margin:0;padding:0;overflow:hidden;background:#050817;" +
                    "touch-action:manipulation}#unity-container,#unity-container.unity-desktop," +
                    "#unity-container.unity-mobile{position:fixed;inset:0;width:100vw;height:100vh;left:0;top:0;transform:none}" +
                    "#unity-fullscreen-container,.unity-mobile #unity-fullscreen-container{position:absolute;inset:0;" +
                    "width:100vw!important;height:100vh!important}#unity-canvas,#unity-canvas.unity-mobile{position:absolute;" +
                    "inset:0;display:block;width:100%;height:100%;background:#050817;outline:none}#unity-footer{display:none}" +
                    "#unity-warning{z-index:20}\n");
            }
        }
    }
}
#endif
