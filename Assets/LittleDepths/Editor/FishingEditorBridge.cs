using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LittleDepths.Editor
{
    // Allows scene generation and Play mode validation in the editor the user already has open.
    [InitializeOnLoad]
    public static class FishingEditorBridge
    {
        const string Command="Logs/LittleDepths.command";
        static double next;
        static FishingEditorBridge(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(Application.isBatchMode)return;
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            next=EditorApplication.timeSinceStartup+.75;
            if(!File.Exists(Command))return;
            string command=File.ReadAllText(Command).Trim();
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
            File.Delete(Command);
            try
            {
                if(command=="setup-and-test")FishingSmokeTests.RebuildAndRun();
                else if(command=="setup")FishingSceneSetup.Setup();
                else if(command=="test")FishingSmokeTests.Run();
                else if(command=="open")
                {
                    var current=EditorSceneManager.GetActiveScene();
                    if(current.IsValid()&&current.isDirty)
                    {
                        Directory.CreateDirectory("Assets/Scenes/Backups");
                        EditorSceneManager.SaveScene(current,"Assets/Scenes/Backups/BeforeAppearanceRefresh_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
                    }
                    AssetDatabase.Refresh();
                    EditorSceneManager.OpenScene(FishingSceneSetup.ScenePath);
                }
                File.WriteAllText("Logs/LittleDepths-Bridge.status","STARTED "+command);
            }
            catch(Exception e){Debug.LogException(e);File.WriteAllText("Logs/LittleDepths-Bridge.status","FAILED "+e);}
        }
    }
}
