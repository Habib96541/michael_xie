using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BottleFlip.EditorTools
{
    /// <summary>
    /// One-shot project setup + WebGL build. Run from the menu or in batch mode:
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod BottleFlip.EditorTools.ProjectBuilder.BuildWebGL
    /// </summary>
    public static class ProjectBuilder
    {
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string BuildPath = "Builds/WebGL";

        [MenuItem("Bottle Flip/Build WebGL")]
        public static void BuildWebGLMenu()
        {
            BuildWebGL();
        }

        [MenuItem("Bottle Flip/Setup Project")]
        public static void SetupOnly()
        {
            ImportTmpEssentials();
            ConfigureProject();
            EnsureAlwaysIncludedShaders();
            CreateMainScene();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Entry point for batch mode builds.</summary>
        public static void BuildWebGL()
        {
            SetupOnly();

            Directory.CreateDirectory(BuildPath);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"BOTTLEFLIP_BUILD_OK size={summary.totalSize} time={summary.totalTime}");
                PackSingleFileHtml();
            }
            else
            {
                Debug.LogError($"BOTTLEFLIP_BUILD_FAILED {summary.result} errors={summary.totalErrors}");
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        /// <summary>
        /// Turns the multi-file WebGL build into one self-contained .html file by embedding
        /// every build asset as base64 and swapping the loader bootstrap for a blob-URL one.
        /// </summary>
        static void PackSingleFileHtml()
        {
            string script = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "tools", "pack_single_html.py"));
            if (!File.Exists(script))
            {
                Debug.LogWarning("Single-file packer not found: " + script);
                return;
            }

            var starter = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{script}\"",
                WorkingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..")),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using (var process = System.Diagnostics.Process.Start(starter))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    if (process.ExitCode == 0) Debug.Log("BOTTLEFLIP_PACK_OK " + output.Trim());
                    else Debug.LogError("BOTTLEFLIP_PACK_FAILED " + error.Trim());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Single-file pack failed: " + e.Message);
            }
        }

        static void ImportTmpEssentials()
        {
            const string settingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
            if (File.Exists(settingsPath)) return;

            Debug.Log("Importing TextMesh Pro essentials...");

            // TMP's own importer cannot locate the embedded uGUI package,
            // so import the bundled .unitypackage directly.
            string packageRoot = null;
            if (Directory.Exists("Library/PackageCache"))
            {
                foreach (string dir in Directory.GetDirectories("Library/PackageCache"))
                {
                    if (Path.GetFileName(dir).StartsWith("com.unity.ugui@"))
                    {
                        packageRoot = dir;
                        break;
                    }
                }
            }

            string packageFile = packageRoot == null
                ? null
                : Path.Combine(packageRoot, "Package Resources", "TMP Essential Resources.unitypackage");

            if (packageFile != null && File.Exists(packageFile))
                AssetDatabase.ImportPackage(packageFile, false);
            else
                TMP_PackageResourceImporter.ImportResources(true, false, false);

            AssetDatabase.Refresh();

            for (int i = 0; i < 40 && !File.Exists(settingsPath); i++)
            {
                System.Threading.Thread.Sleep(200);
                AssetDatabase.Refresh();
            }

            if (File.Exists(settingsPath)) Debug.Log("TMP essentials ready.");
            else Debug.LogError("TMP essentials import failed - text will not render.");
        }

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "Freebuff";
            PlayerSettings.productName = "Bottle Flip";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.runInBackground = true;
            PlayerSettings.defaultScreenWidth = 900;
            PlayerSettings.defaultScreenHeight = 1600;
            PlayerSettings.defaultWebScreenWidth = 900;
            PlayerSettings.defaultWebScreenHeight = 1600;
            PlayerSettings.resizableWindow = true;

            // WebGL specifics: tiny, dependency-free build. Gzip is decompressed in the browser
            // by the single-file packer, so no server headers are needed either way.
            PlayerSettings.WebGL.template = "PROJECT:Portrait";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.WebGL, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSize);
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;
        }

        static void EnsureAlwaysIncludedShaders()
        {
            // Shaders referenced only via Shader.Find at runtime would otherwise be stripped.
            var graphicsSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphicsSettings == null || graphicsSettings.Length == 0)
            {
                Debug.LogWarning("Could not open GraphicsSettings.asset");
                return;
            }

            var so = new SerializedObject(graphicsSettings[0]);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;

            string[] needed =
            {
                "Standard",
                "Sprites/Default",
                "BottleFlip/Additive",
                "UI/Default"
            };

            foreach (string shaderName in needed)
            {
                var shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    Debug.LogWarning("Missing shader: " + shaderName);
                    continue;
                }

                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        present = true;
                        break;
                    }
                }

                if (!present)
                {
                    list.InsertArrayElementAtIndex(list.arraySize);
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                }
            }

            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        static void CreateMainScene()
        {
            if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Bake the render settings into the scene: fog must be enabled at build time or
            // Unity strips the fog shader variants and the floor never fades out.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.039f, 0.047f, 0.125f, 1f);
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 26f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.063f, 0.078f, 0.173f, 1f);
            RenderSettings.skybox = null;

            var go = new GameObject("Bootstrap");
            go.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}
