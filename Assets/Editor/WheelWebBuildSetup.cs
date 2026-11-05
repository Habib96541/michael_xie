using System;
using System.IO;
using SpinTheWheel;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WheelWebBuildSetup
{
    private const string AudioPath = "Assets/Audio/";

    [MenuItem("Tools/Spin the Wheel/Build Web Player")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        if (PlayerSettings.WebGL.template != "PROJECT:Responsive")
            throw new InvalidOperationException("Run Prepare Web Build first.");

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SpinTheWheel.unity" },
            locationPathName = "Builds/WebGL-Responsive",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        });
        Directory.CreateDirectory("SprintDocs");
        File.WriteAllText("SprintDocs/Sprint_06_Build_Validation.txt",
            $"Result: {report.summary.result}\nDuration: {report.summary.totalTime}\n" +
            $"Bytes: {report.summary.totalSize}\nErrors: {report.summary.totalErrors}\nWarnings: {report.summary.totalWarnings}\n");
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException("The Web build failed. Check the Console and build report.");
        Debug.Log("Responsive Web player built in Builds/WebGL-Responsive.");
    }

    [MenuItem("Tools/Spin the Wheel/Prepare Web Build")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        AudioClip click = PaddedClip("ui_click");
        AudioClip tick = PaddedClip("wheel_tick");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SpinTheWheel.unity");
        WheelController controller = Object.FindFirstObjectByType<WheelController>(FindObjectsInactive.Include);
        SerializedObject settings = new SerializedObject(controller);
        settings.FindProperty("clickClip").objectReferenceValue = click;
        settings.FindProperty("tickClip").objectReferenceValue = tick;
        settings.ApplyModifiedPropertiesWithoutUndo();
        PlayerSettings.WebGL.template = "PROJECT:Responsive";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Responsive Web template selected and padded click/tick WAV clips assigned.");
    }

    private static AudioClip PaddedClip(string name)
    {
        string destination = AudioPath + name + "_web.wav";
        if (!File.Exists(destination))
            WritePaddedWave(AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + name + ".ogg"), destination);

        AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        AudioImporter importer = (AudioImporter)AssetImporter.GetAtPath(destination);
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        settings.preloadAudioData = true;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<AudioClip>(destination);
    }

    private static void WritePaddedWave(AudioClip source, string destination)
    {
        float[] samples = new float[source.samples * source.channels];
        if (!source.GetData(samples, 0))
            throw new InvalidOperationException("Could not decode source effect: " + source.name);

        int leadingSamples = Mathf.RoundToInt(source.frequency * 0.05f) * source.channels;
        int trailingSamples = Mathf.RoundToInt(source.frequency * 0.075f) * source.channels;
        int dataBytes = (leadingSamples + samples.Length + trailingSamples) * sizeof(short);
        using BinaryWriter writer = new BinaryWriter(File.Create(destination));
        WriteWaveHeader(writer, source, dataBytes);
        for (int index = 0; index < leadingSamples; index++)
            writer.Write((short)0);
        foreach (float sample in samples)
            writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        for (int index = 0; index < trailingSamples; index++)
            writer.Write((short)0);
    }

    private static void WriteWaveHeader(BinaryWriter writer, AudioClip source, int dataBytes)
    {
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataBytes);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)source.channels);
        writer.Write(source.frequency);
        writer.Write(source.frequency * source.channels * sizeof(short));
        writer.Write((short)(source.channels * sizeof(short)));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataBytes);
    }
}
