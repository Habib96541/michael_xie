using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EggDrop.Editor
{
    public static class EggDropWebGLBuilder
    {
        [MenuItem("Egg Drop/Build WebGL")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play mode before building WebGL.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
                throw new InvalidOperationException("Install WebGL Build Support for this Unity editor.");

            string output = Environment.GetEnvironmentVariable("EGGDROP_WEBGL_OUTPUT");
            if (string.IsNullOrEmpty(output)) output = "Builds/WebGL";
            output = Path.GetFullPath(output);
            WebGLCompressionFormat compression = PlayerSettings.WebGL.compressionFormat;
            bool fallback = PlayerSettings.WebGL.decompressionFallback;
            int initialMemory = PlayerSettings.WebGL.initialMemorySize;
            try
            {
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.initialMemorySize = 128;
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { EggDropSceneBuilder.ScenePath },
                    locationPathName = output,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None
                });
                BuildSummary summary = report.summary;
                string message = "WebGL build " + summary.result + Environment.NewLine
                    + "Output: " + output + Environment.NewLine
                    + "Size: " + summary.totalSize + " bytes" + Environment.NewLine
                    + "Duration: " + summary.totalTime + Environment.NewLine
                    + "Errors: " + summary.totalErrors + Environment.NewLine
                    + "Warnings: " + summary.totalWarnings;
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/EggDrop-WebGL-Summary.txt", message);
                if (summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(message);
                ConfigureBrowserPage(output);
                Debug.Log(message);
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = compression;
                PlayerSettings.WebGL.decompressionFallback = fallback;
                PlayerSettings.WebGL.initialMemorySize = initialMemory;
            }
        }

        private static void ConfigureBrowserPage(string output)
        {
            string path = Path.Combine(output, "index.html");
            string page = File.ReadAllText(path);
            const string marker = "arguments: [],";
            if (!page.Contains(marker))
                throw new InvalidOperationException("The WebGL template is missing its player configuration.");
            page = page.Replace(marker, marker + Environment.NewLine + "        devicePixelRatio: 1,");
            File.WriteAllText(path, page);
        }
    }
}
