using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace com.javierquevedo.Editor
{
    /// <summary>
    /// One-click settings pass for the mobile WebGL build:
    /// - IL2CPP + high managed stripping + engine code stripping (smallest wasm).
    /// - Brotli compression with the JS decompression fallback, so the game loads even when
    ///   the host cannot set Content-Encoding headers.
    /// - Browser caching of the build, hash file names, no exception support.
    /// - The cyber full-bleed mobile template (safe-area aware, no footer).
    /// - Every quality level set up for flat 2D (no shadows, no AA, no aniso, no pixel lights).
    /// - Authored sprites imported compressed instead of as raw RGBA32.
    ///
    /// Run from the CyberOrbs menu or headless:
    /// Unity.exe -batchmode -nographics -quit -projectPath ... -executeMethod com.javierquevedo.Editor.WebGLMobileOptimizer.Apply
    /// </summary>
    public static class WebGLMobileOptimizer
    {
        private const string SpriteFolder = "Assets/Resources/Art/Sprites";
        private const string TemplateName = "PROJECT:CyberOrbsMobile";
        private const string BuildFolder = "Builds/WebGL";

        [MenuItem("CyberOrbs/Optimize WebGL Build Settings")]
        public static void Apply()
        {
            ConfigurePlayer();
            ConfigureQuality();
            ConfigureSprites();

            AssetDatabase.SaveAssets();
            Debug.Log("[WebGLMobileOptimizer] Mobile WebGL settings applied. " +
                      "Template: " + PlayerSettings.WebGL.template + ", " +
                      "compression: " + PlayerSettings.WebGL.compressionFormat + ".");
        }

        [MenuItem("CyberOrbs/Build Mobile WebGL")]
        public static void BuildWebGL()
        {
            Apply();

            List<string> scenes = new List<string>();
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled) scenes.Add(scene.path);
            }

            if (scenes.Count == 0)
            {
                Debug.LogError("[WebGLMobileOptimizer] No enabled scenes in Build Settings.");
                return;
            }

            Directory.CreateDirectory(BuildFolder);

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = BuildFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
                Debug.Log("[WebGLMobileOptimizer] Build succeeded: " + BuildFolder +
                          " (" + (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB, " +
                          summary.totalTime.TotalSeconds.ToString("0") + "s)");
            else
                Debug.LogError("[WebGLMobileOptimizer] Build " + summary.result + ": " + summary.totalErrors + " error(s).");
        }

        private static void ConfigurePlayer()
        {
            NamedBuildTarget webgl = NamedBuildTarget.WebGL;

            PlayerSettings.SetScriptingBackend(webgl, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(webgl, ManagedStrippingLevel.High);
            PlayerSettings.SetIl2CppCompilerConfiguration(webgl, Il2CppCompilerConfiguration.Release);

            PlayerSettings.stripEngineCode = true;
            PlayerSettings.stripUnusedMeshComponents = true;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
            PlayerSettings.WebGL.template = TemplateName;
        }

        private static void ConfigureQuality()
        {
            string[] levels = QualitySettings.names;
            int currentLevel = QualitySettings.GetQualityLevel();

            for (int i = 0; i < levels.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.antiAliasing = 0;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                QualitySettings.pixelLightCount = 0;
                QualitySettings.shadows = ShadowQuality.Disable;
                QualitySettings.softParticles = false;
                QualitySettings.realtimeReflectionProbes = false;
                QualitySettings.vSyncCount = 0;
                QualitySettings.lodBias = 1f;
                QualitySettings.maximumLODLevel = 0;
            }

            QualitySettings.SetQualityLevel(currentLevel, false);
        }

        private static void ConfigureSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteFolder });
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                if (importer.textureType != TextureImporterType.Sprite) continue;

                // The authored PNGs are currently imported uncompressed (RGBA32). Compressed HQ
                // keeps the art crisp while cutting texture data and load time by roughly 4x.
                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            Debug.Log("[WebGLMobileOptimizer] Sprite textures recompressed: " + changed + ".");
        }
    }
}
