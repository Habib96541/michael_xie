using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Explicit local request files let development tooling invoke a small, fixed
// set of checks in the already-open Editor without a second project instance.
[InitializeOnLoad]
public static class MineGridEditorTasks
{
    private const string Request = "Library/MineGrid-request.txt";
    private static double _ready;
    static MineGridEditorTasks()
    {
        EditorApplication.update += Tick;
        Application.logMessageReceived += RecordError;
    }

    private static void RecordError(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            File.AppendAllText("Library/MineGrid-errors.txt", message + "\n" + trace + "\n");
        }
    }

    private static void Tick()
    {
        if (EditorApplication.isPlaying && File.Exists("Library/MineGrid-capturing.flag"))
        {
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request) || EditorApplication.timeSinceStartup < _ready)
        {
            return;
        }

        string command = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        _ready = EditorApplication.timeSinceStartup + 2;
        try
        {
            switch (command)
            {
                case "generate":
                    if (EditorApplication.isPlaying)
                    {
                        throw new Exception("Exit Play Mode before generation.");
                    }

                    MineGridSetup.Generate();
                    break;
                case "shopcapture":
                    MineGridShopCapture.Run();
                    break;
                case "shopverify":
                    MineGridShopVerification.Run();
                    break;
                case "verify":
                    MineGridVerification.Run();
                    break;
                case "play":
                    EditorSceneManager.OpenScene("Assets/Scenes/MineGrid.unity");
                    EditorApplication.isPlaying = true;
                    break;
                case "smoke":
                    MineGridSmoke.Run();
                    break;
                case "stop":
                    EditorApplication.isPlaying = false;
                    break;
                case "capture":
                    MineGridCapture.Run();
                    break;
                default:
                    throw new Exception("Unknown MineGrid task.");
            }

            File.WriteAllText("Library/MineGrid-task-result.txt", "OK: " + command);
        }
        catch (Exception e)
        {
            File.WriteAllText("Library/MineGrid-task-result.txt", "FAIL: " + command + "\n" + e);
            Debug.LogException(e);
        }
    }
}
