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
            html = System.Text.RegularExpressions.Regex.Replace(html, @"<title>.*?</title>",
                "<title>Geometry Dash</title>");
            html = System.Text.RegularExpressions.Regex.Replace(html,
                @"<meta\s+name=[""']viewport[""'][^>]*>", "");
            html = html.Replace("</head>",
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, viewport-fit=cover\">" +
                "<style>" + File.ReadAllText("Tools/web_viewport.css") + "</style></head>");
            html = html.Replace("var config = {",
                "var config = { matchWebGLToCanvasSize: true, devicePixelRatio: Math.min(window.devicePixelRatio || 1, 1.5),");
            html = html.Replace("document.getElementsByTagName('head')[0].appendChild(meta);",
                "if (!document.querySelector('meta[name=viewport]')) document.head.appendChild(meta);");
            html = html.Replace("</body>", "<script>" + File.ReadAllText("Tools/web_viewport.js") + "</script></body>");
            File.WriteAllText(indexPath, html);

        }
    }
}
#endif
