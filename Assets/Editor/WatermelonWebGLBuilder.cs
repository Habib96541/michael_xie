using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WatermelonWebGLBuilder
{
    private const string ScenePath = "Assets/Scenes/Watermelon.unity";
    private const string OutputPath = "Builds/WebGL";

    [MenuItem("Watermelon/Build WebGL")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new BuildFailedException("Exit Play Mode before building.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        if (!File.Exists(ScenePath))
            throw new BuildFailedException($"Missing game scene: {ScenePath}");

        PlayerSettings.defaultWebScreenWidth = 450;
        PlayerSettings.defaultWebScreenHeight = 800;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(OutputPath);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"WebGL build failed: {report.summary.result}");

        Debug.Log($"WebGL build succeeded: {Path.GetFullPath(OutputPath)} " +
                  $"({report.summary.totalSize} bytes, {report.summary.totalTime})");
    }
}
