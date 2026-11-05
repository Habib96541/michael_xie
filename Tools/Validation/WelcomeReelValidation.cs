using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using LittleDepths;

// Run against the saved scene in the isolated deployment copy. Never regenerates it.
[InitializeOnLoad]
public static class WelcomeReelValidation
{
    const string Active="LittleDepths.WelcomeReelValidation";
    static FishingGame game;
    static int stage,landings,liveWallet,runtimeErrors;
    static float liveY;
    static double next;
    static readonly List<string> checks=new List<string>();
    static readonly MethodInfo tick=typeof(FishingGame).GetMethod("TickFishing",BindingFlags.Instance|BindingFlags.NonPublic);
    static readonly MethodInfo line=typeof(FishingGame).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
    static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../.."));
    static WelcomeReelValidation()
    {
        EditorApplication.update+=Update;
        Application.logMessageReceived+=(message,trace,type)=>
        {
            if(SessionState.GetBool(Active,false)&&!trace.Contains("UnityEditor.Search.")&&(type==LogType.Error||type==LogType.Exception))runtimeErrors++;
        };
    }
    public static void Run()
    {
        PlayerPrefs.DeleteKey("LittleDepths.SmokeOnly");
        EditorSceneManager.OpenScene("Assets/Scenes/LittleDepths.unity");
        SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;
    }
    static void Require(bool value,string message)
    {
        if(!value)throw new Exception(message);
        checks.Add(message);Debug.Log("WELCOME_REEL_PASS: "+message);
    }
    static void Step(float dt){tick.Invoke(game,new object[]{dt});line.Invoke(game,null);}
    static void Trip(int speed)
    {
        game.NewSession();game.Progress.speedLevel=speed;
        game.CastButton();game.Launch(1);
        foreach(var fish in game.world.Fish)fish.transform.position=new Vector3(100,-100,0);
        game.hook.position=new Vector3(0,game.config.surfaceY-6*game.config.metersToWorld,0);
        game.hook.rotation=Quaternion.identity;line.Invoke(game,null);
    }
    static void Ratio(int speed,bool full)
    {
        Trip(speed);float before=game.hook.position.y;Step(.02f);float down=before-game.hook.position.y;
        int wallet=game.Progress.coins,animations=game.world.angler.ManualReelAnimations;
        before=game.hook.position.y;Vector3 camera=game.gameCamera.transform.position;
        game.hud.reelButton.onClick.Invoke();
        Require(game.Phase==FishingPhase.Ascending&&game.hook.position.y==before&&game.gameCamera.transform.position==camera,"Reel click starts motion without teleporting or camera snapping (speed "+speed+").");
        Require(game.Progress.coins==wallet&&!game.hud.IsModalOpen,"No payout or results before the hook returns.");
        game.ReelIn();Require(game.world.angler.ManualReelAnimations==animations+1,"Repeated reel clicks do not restart retrieval.");
        if(full)foreach(var fish in game.world.Fish.Take(game.config.Capacity(game.Progress)).ToArray())game.Catch(fish);
        else game.Catch(game.world.Fish.First(f=>!f.isTrash));
        float length=Vector3.Distance(game.fishingLine.GetPosition(0),game.fishingLine.GetPosition(1));
        Step(.02f);float ratio=(game.hook.position.y-before)/down;
        Require(Mathf.Abs(ratio-1.4f)<.0001f,"Ascent/descent ratio is 1.4 at speed "+speed+", full haul="+full+" (actual "+ratio.ToString("0.0000")+").");
        Require(Vector3.Distance(game.fishingLine.GetPosition(0),game.fishingLine.GetPosition(1))<length,"Visible fishing line shortens during retrieval.");
        for(int i=0;game.IsFishing&&i<2000;i++)Step(.02f);
        Require(game.Phase==FishingPhase.Results&&Vector3.Distance(game.HookEye,game.world.angler.rodTip.position)<.001f,"Hook reaches the rod tip before results.");
        Require(game.Progress.coins==wallet+game.HaulValue+game.LastCollectionBonus,"Landing pays the completed haul exactly once.");
        wallet=game.Progress.coins;game.ReelIn();game.Land();
        Require(game.Progress.coins==wallet,"Repeated landing cannot duplicate coins.");
    }
    static void Update()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        try
        {
            if(EditorApplication.timeSinceStartup>240)throw new Exception("Validation timed out.");
            if(!game)
            {
                game=UnityEngine.Object.FindFirstObjectByType<FishingGame>();
                if(!game||game.Progress==null)return;
                Require(game.SaveSlot=="LittleDepths.SmokeOnly","Validation uses an isolated save slot.");
            }
            switch(stage)
            {
                case 0:
                    var scaler=game.hud.GetComponent<CanvasScaler>();
                    Require(scaler.referenceResolution==new Vector2(1360,780)&&scaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,"HUD uses the larger fit-to-viewport layout.");
                    var play=game.hud.castButton.image.rectTransform;
                    Require(play.anchorMin==new Vector2(.5f,0)&&play.rect.width>240,"PLAY stays large and bottom-anchored with the dock during resize.");
                    Require(game.hud.journalButton.image.rectTransform.rect.width>110&&game.hud.reelButton.image.raycastPadding.y<0,"Navigation and Reel In have enlarged touch targets.");
                    game.enabled=false;Ratio(0,false);Ratio(4,false);Ratio(4,true);game.enabled=true;
                    game.hud.ShowWelcome(498);stage++;next=EditorApplication.timeSinceStartup+.6;return;
                case 1:
                    var images=game.hud.modalLayer.GetComponentsInChildren<Image>();
                    Require(images.Any(i=>i.sprite&&i.sprite.name=="ref_collection_panel"&&i.type==Image.Type.Sliced),"Welcome uses the complete dock-style aquatic frame.");
                    Require(images.Any(i=>i.sprite&&i.sprite.name=="ref_collection_card"),"Offline earnings use an equipment-style reward card.");
                    Require(game.hud.modalLayer.GetComponentsInChildren<Text>().Any(t=>t.text=="+ $ 498"),"Welcome displays the actual earnings amount.");
                    Capture("WelcomeReel-01-Welcome");
                    int wallet=game.Progress.coins;
                    game.hud.modalLayer.GetComponentsInChildren<Button>().First(b=>b.name=="LOVELY, THANK YOU").onClick.Invoke();
                    Require(!game.hud.IsModalOpen&&game.Progress.coins==wallet,"Welcome dismisses without crediting offline money twice.");
                    game.hud.ShowSettings();stage++;next=EditorApplication.timeSinceStartup+.6;return;
                case 2:
                    var buttons=game.hud.modalLayer.GetComponentsInChildren<Button>();
                    Require(buttons.Length==6&&buttons.Count(b=>b.name.StartsWith("Toggle "))==2,"Pause has only audio, resume, restart, quit and close controls.");
                    var sound=buttons.First(b=>b.name=="Toggle sound");var music=buttons.First(b=>b.name=="Toggle music");
                    float sx=((RectTransform)sound.transform.parent).anchoredPosition.x,mx=((RectTransform)music.transform.parent).anchoredPosition.x;
                    Require(Mathf.Abs(sx+mx)<.01f&&sx<0&&mx>0,"Two audio buttons are centered symmetrically.");
                    bool wasSound=game.Progress.sound,wasMusic=game.Progress.music;
                    sound.onClick.Invoke();music.onClick.Invoke();
                    var saved=GameProgress.Load(game.SaveSlot);
                    Require(saved.sound!=wasSound&&saved.music!=wasMusic,"Both audio toggles still persist independently.");
                    sound.onClick.Invoke();music.onClick.Invoke();
                    Capture("WelcomeReel-02-Pause");game.hud.CloseModal();
                    Trip(0);game.Catch(game.world.Fish.First(f=>!f.isTrash));
                    liveWallet=game.Progress.coins;liveY=game.hook.position.y;game.Landed+=()=>landings++;
                    game.hud.reelButton.onClick.Invoke();stage++;next=EditorApplication.timeSinceStartup+.45;return;
                case 3:
                    Require(game.Phase==FishingPhase.Ascending&&game.hook.position.y>liveY&&!game.hud.IsModalOpen,"Real Play-mode frames visibly move the hook upward before results.");
                    Require(!game.hud.reelButton.interactable&&game.hud.reelActionLabel.text=="REELING IN…","Reel button shows retrieval progress and prevents repeated presses.");
                    Capture("WelcomeReel-03-Reeling");stage++;next=EditorApplication.timeSinceStartup+.15;return;
                case 4:
                    if(game.IsFishing){next=EditorApplication.timeSinceStartup+.15;return;}
                    Require(game.Phase==FishingPhase.Results&&landings==1&&game.Progress.coins==liveWallet+game.HaulValue+game.LastCollectionBonus,"Live return lands and pays exactly once.");
                    stage++;next=EditorApplication.timeSinceStartup+.5;return;
                case 5:
                    Require(Vector3.Distance(game.HookEye,game.world.angler.rodTip.position)<.02f,"Live hook finishes and stays at the player during celebration.");
                    game.hud.CloseModal(true);Capture("WelcomeReel-04-ReturnedToPlayer");
                    Require(runtimeErrors==0,"No runtime errors or exceptions.");
                    Finish("PASSED",0);return;
            }
        }
        catch(Exception error){Debug.LogException(error);Finish("FAILED: "+error,1);}
    }
    static void Finish(string result,int code)
    {
        SessionState.SetBool(Active,false);
        File.WriteAllText(Path.Combine(Output,"WelcomeReel-Validation.txt"),result+"\n"+string.Join("\n",checks));
        EditorApplication.Exit(code);
    }
    static void Capture(string name)
    {
        const int width=1920,height=1080;
        string directory=Path.Combine(Output,"Previews");Directory.CreateDirectory(directory);
        var camera=game.gameCamera;var target=new RenderTexture(width,height,24);
        var old=camera.targetTexture;var active=RenderTexture.active;
        var canvas=game.hud.GetComponent<Canvas>();var scaler=game.hud.GetComponent<CanvasScaler>();
        float oldScale=canvas.scaleFactor;scaler.enabled=false;camera.targetTexture=target;canvas.scaleFactor=Mathf.Min(width/scaler.referenceResolution.x,height/scaler.referenceResolution.y);
        foreach(var text in game.hud.GetComponentsInChildren<Text>(true))text.SetAllDirty();
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
        File.WriteAllBytes(Path.Combine(directory,name+".png"),image.EncodeToPNG());
        camera.targetTexture=old;canvas.scaleFactor=oldScale;scaler.enabled=true;RenderTexture.active=active;
        target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
    }
}
