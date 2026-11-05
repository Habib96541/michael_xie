using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LittleDepths;

// Copied into the isolated validation project only; never rebuilds or saves scenes.
[InitializeOnLoad]
public static class MatchingActionValidation
{
    const string Active="LittleDepths.MatchingActionValidation";
    static FishingGame game;
    static int stage;
    static double next;
    static MatchingActionValidation(){EditorApplication.update+=Tick;}
    public static void Run()
    {
        PlayerPrefs.DeleteKey("LittleDepths.SmokeOnly");PlayerPrefs.Save();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene("Assets/Scenes/LittleDepths.unity");
        SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(!game)
            {
                game=UnityEngine.Object.FindFirstObjectByType<FishingGame>();
                if(!game||game.Progress==null)return;
                Require(game.SaveSlot=="LittleDepths.SmokeOnly","Use the isolated test save.");
                next=EditorApplication.timeSinceStartup+1;return;
            }
            switch(stage++)
            {
                case 0:
                    Require(game.Phase==FishingPhase.Harbor,"User scene opens in the harbor.");
                    Capture("31-MatchingEquipment");
                    game.hud.castButton.onClick.Invoke();
                    LittleDepths.Editor.FishingActionChecks.BeginTiming(game);
                    next=EditorApplication.timeSinceStartup+.65;return;
                case 1:
                    LittleDepths.Editor.FishingActionChecks.Timing(game);
                    Capture("32-MatchingCastMeter");
                    game.Launch(1);next=EditorApplication.timeSinceStartup+.65;return;
                case 2:
                    Require(game.Phase==FishingPhase.Descending,"Cast enters the dive.");
                    LittleDepths.Editor.FishingActionChecks.Dive(game,true);
                    Require(game.hud.depthLabel.text==game.Depth.ToString("0.0")+" m","Depth reading follows the hook.");
                    Capture("33-MatchingDiveHUD");
                    game.hud.reelButton.onClick.Invoke();
                    Require(game.Phase==FishingPhase.Ascending,"Reel button starts retrieval.");
                    LittleDepths.Editor.FishingActionChecks.Dive(game,false);
                    Capture("34-MatchingReelingHUD");
                    SessionState.SetBool(Active,false);
                    File.WriteAllText("Logs/MatchingAction-Validation.status","PASSED: Existing user scene loaded without setup; animated timing cursor and true gold threshold, live dive readings, reel button and retrieval states verified. No scene saved. No player build.");
                    Debug.Log("MATCHING_ACTION_UI_OK");
                    EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(0);return;
            }
        }
        catch(Exception error)
        {
            Debug.LogException(error);SessionState.SetBool(Active,false);
            File.WriteAllText("Logs/MatchingAction-Validation.status","FAILED: "+error);
            EditorApplication.Exit(1);
        }
    }
    static void Capture(string name)
    {
        const int width=1920,height=1080;
        Directory.CreateDirectory("Logs/Previews");
        var camera=game.gameCamera;var target=new RenderTexture(width,height,24);
        var old=camera.targetTexture;var active=RenderTexture.active;
        var canvas=game.hud.GetComponent<Canvas>();var scaler=game.hud.GetComponent<CanvasScaler>();
        float oldScale=canvas.scaleFactor;scaler.enabled=false;camera.targetTexture=target;canvas.scaleFactor=Mathf.Min(width/1600f,height/1000f);
        foreach(var text in game.hud.GetComponentsInChildren<Text>(true))text.SetAllDirty();
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        File.WriteAllBytes("Logs/Previews/"+name+".png",image.EncodeToPNG());
        camera.targetTexture=old;canvas.scaleFactor=oldScale;scaler.enabled=true;RenderTexture.active=active;
        target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
    }
}
