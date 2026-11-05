using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths.Editor
{
    /// <summary>Runs inside Unity Play mode without producing a player build.</summary>
    [InitializeOnLoad]
    public static class FishingSmokeTests
    {
        const string Active="LittleDepths.Smoke.Active";
        static double next;
        static int stage;
        static FishingGame game;
        static bool caughtForCheck;
        static int errors;
        static FishingSmokeTests()
        {
            EditorApplication.update+=Tick;
            Application.logMessageReceived+=OnLog;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode){PlayerPrefs.DeleteKey("LittleDepths.SmokeOnly");PlayerPrefs.Save();}};
        }
        static void OnLog(string condition,string trace,LogType type)
        {
            // Unity 6's search indexer can throw on first editor startup in batch mode.
            // Keep editor search diagnostics separate from errors in the running game.
            if(trace.Contains("UnityEditor.Search."))return;
            if(SessionState.GetBool(Active,false)&&(type==LogType.Exception||type==LogType.Error))errors++;
        }

        public static void Run()
        {
            stage=0;game=null;errors=0;next=0;caughtForCheck=false;
            PlayerPrefs.DeleteKey("LittleDepths.SmokeOnly");
            WindowAndDailyChecks.Rules();
            FishingActionChecks.Artwork();
            var config=AssetDatabase.LoadAssetAtPath<FishingConfig>(FishingSceneSetup.Root+"/Data/FishingBalance.asset");
            Check(config && config.species.Count==10,"10 configured species");
            var p=new GameProgress();p.discovered.Add("minnow");p.incomeLevel=1;p.lastSeen=1000;
            Check(p.AccruePassive(config,1060)==6,"Aquarium and income accrue exactly per minute");
            p.lastSeen=1000;Check(p.AccruePassive(config,1000+20*3600)==2880,"Offline income capped at eight hours");
            p.lastSeen=2000;Check(p.AccruePassive(config,1000)==0,"Clock rollback never creates negative income");
            p.Save("LittleDepths.SmokeOnly");Check(GameProgress.Load("LittleDepths.SmokeOnly").incomeLevel==1,"Save round trip");
            var oldPaint=JsonUtility.FromJson<GameProgress>("{\"coins\":321,\"boatStyle\":2,\"boatPaint\":7}");
            Check(oldPaint.coins==321&&oldPaint.boatStyle==2&&!JsonUtility.ToJson(oldPaint).Contains("boatPaint"),"Old paint selections are discarded without losing saved money or boat choice");
            PlayerPrefs.DeleteKey("LittleDepths.SmokeOnly");
            EditorSceneManager.OpenScene(FishingSceneSetup.ScenePath);
            SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;
        }

        public static void RebuildAndRun() { FishingSceneSetup.Setup(); Run(); }

        static void Tick()
        {
            if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
            try
            {
                if(!game||game.Progress==null)
                {
                    game=UnityEngine.Object.FindFirstObjectByType<FishingGame>();
                    if(!game||game.Progress==null)return;
                    Check(game.SaveSlot=="LittleDepths.SmokeOnly","Play test uses isolated save slot");
                    next=EditorApplication.timeSinceStartup+2;return;
                }
                switch(stage++)
                {
                    case 0:
                        Check(game.Phase==FishingPhase.Harbor,"Scene enters the harbor");
                        Check(game.world.Fish.Count>5,"Fish populate");
                        Check(game.config.species.All(s=>s.swimFrames!=null&&s.swimFrames.Length==4&&s.swimController),"All species have four-frame swim animations");
                        Check(game.world.angler&&game.world.angler.animator,"Whole angler-and-boat sprite has a real Animator");
                        Check(game.config.completeAnglers!=null&&game.config.completeAnglers.Length==108&&game.config.completeAnglers.All(s=>s),"Every outfit, hair, hat and boat combination has a complete painted sprite");
                        Check(game.world.angler.GetComponentsInChildren<SpriteRenderer>().Length==1,"One renderer contains the seated girl, worn hat and boat together");
                        CleanAppearanceChecks.ValidateAll(game);CleanAppearanceChecks.CaptureSheet(game);
                        Capture("01-Harbor");
                        game.hud.castButton.onClick.Invoke();Check(game.Phase==FishingPhase.Timing,"Play control opens timing gauge after scene load");FishingActionChecks.BeginTiming(game);break;
                    case 1:
                        FishingActionChecks.Timing(game);Capture("02-Casting");Capture("28-CraftedCastMeter",1920,1080);game.Launch(1);
                        ProvidedSheetChecks.Line(game);
                        Check(game.Phase==FishingPhase.Descending&&game.Progress.sessionCasts==1,"Launch consumes one session cast");
                        Check(game.Progress.dailyQuests.casts==1,"Real launch increments today's casts");
                        Check(Mathf.Approximately(game.TargetDepth,game.config.MaxDepth(game.Progress)),"Perfect cast reaches upgraded max depth");break;
                    case 2:
                        Check(game.Depth>0,"Hook descends in Play mode");
                        Check(game.Progress.dailyQuests.deepest>0,"Real hook travel increments today's deepest depth");
                        ProvidedSheetChecks.Line(game);
                        Check(game.world.angler.CastAnimations>0,"Launching triggers the character cast animation");
                        Check(game.world.Fish.Where(f=>!f.isTrash).All(f=>f.animator&&f.animator.GetCurrentAnimatorStateInfo(0).normalizedTime>0),"Live fish Animators advance their swim frames");
                        if(caughtForCheck)
                        {
                            CleanAppearanceChecks.CheckMouths(game);CleanAppearanceChecks.CaptureMouths(game);
                            FishingActionChecks.Dive(game,false);
                            CollectionHudChecks.AnimatedReel(game);Capture("30-AnimatedReelResults",1920,1080);break;
                        }
                        FishingActionChecks.Dive(game,true);CollectionHudChecks.Sides(game);Capture("03-Underwater");Capture("29-CraftedDiveHUD",1920,1080);
                        CollectionHudChecks.DeepSeams(game);
                        // Controlled contacts verify actual collection, capacity, payout and discovery code.
                        foreach(var f in game.world.Fish.Take(5).ToArray())game.Catch(f);
                        Check(game.HaulCount==5,"Hook collects up to capacity");
                        game.Catch(game.world.Fish.Last());Check(game.HaulCount==5,"Capacity prevents overflow");
                        caughtForCheck=true;stage--;next=EditorApplication.timeSinceStartup+.20;return;
                    case 3:
                        Check(game.Phase==FishingPhase.Results,"Reel returns and sells the catch");
                        Check(game.Progress.coins>=game.HaulValue&&game.Progress.discovered.Count>0,"Sale pays wallet and registers discoveries");
                        Check(game.Progress.dailyQuests.fish==game.Progress.totalFish&&game.Progress.dailyQuests.fish>0,"Real landed fish increment today's catch");
                        Check(game.hud.IsModalOpen,"Catch results are shown");Capture("04-CatchResults");WindowAndDailyChecks.Close(game);
                        Check(game.Phase==FishingPhase.Harbor,"Closing results returns to the harbor");break;
                    case 4:
                        game.ReturnToHarbor();game.Progress.coins=10000;
                        int previous=game.config.Capacity(game.Progress);Check(game.BuyUpgrade(0),"Capacity upgrade succeeds");
                        Check(game.config.Capacity(game.Progress)==previous+1,"Capacity upgrade changes gameplay");
                        Check(game.BuyUpgrade(1)&&game.BuyUpgrade(2),"Depth and income upgrades succeed");
                        Check(game.UnlockHook()&&game.Progress.unlockedHooks.Count==2,"Random cosmetic hook unlocks and equips");
                        int before=game.Progress.coins;int prize=game.ClaimPrize();Check(prize>=50&&game.Progress.coins==before+prize,"Prize pays coins");
                        Check(game.ClaimPrize()==0,"Gift cooldown prevents repeat claims");
                        game.hud.ShowJournal();break;
                    case 5:CollectionHudChecks.Collection(game,false);Capture("05-FieldGuide");WindowAndDailyChecks.Close(game);game.hud.ShowHooks();break;
                    case 6:CollectionHudChecks.Collection(game,true);Capture("06-Hooks");WindowAndDailyChecks.Close(game);game.Progress.prizeReadyAt=0;game.hud.ShowPrizes();break;
                    case 7:
                        Capture("07-Prizes");
                        WindowAndDailyChecks.Cards(game,false);
                        game.hud.modalLayer.GetComponentsInChildren<Button>().First(b=>b.name=="Prize card 0").onClick.Invoke();break;
                    case 8:
                        Check(game.hud.PrizeRevealed,"Prize card flip reveals the reward");WindowAndDailyChecks.Cards(game,true);Capture("07b-PrizeRevealed");WindowAndDailyChecks.Close(game);
                        game.hud.ShowPrizes();WindowAndDailyChecks.Cards(game,true);
                        Check(game.ClaimPrize(0)==0,"Reopening an event cannot pay its prize again");WindowAndDailyChecks.Close(game);
                        while(game.Progress.sessionCasts<game.config.castsPerSession)
                        {
                            game.ReturnToHarbor();game.CastButton();game.Launch(.95f);
                            game.Catch(game.world.Fish.First());game.Land();
                        }
                        Check(game.Progress.sessionCasts==5,"Five real casts exhaust the session");
                        game.NextCast();Check(game.hud.IsModalOpen,"Fifth cast opens the session summary");break;
                    case 9:
                        Capture("08-Session");WindowAndDailyChecks.Close(game);Check(game.Progress.sessionCasts==5,"Closing the session summary does not silently reset casts");game.NewSession();
                        Check(game.Progress.sessionCasts==0&&game.Phase==FishingPhase.Harbor,"Fish again starts a fresh session");
                        game.hud.ShowCharacterCreator();break;
                    case 10:
                        CleanAppearanceChecks.ValidatePreview(game);
                        game.hud.modalLayer.GetComponentsInChildren<Button>().First(b=>b.name=="Boat • cycle complete seated sprite").onClick.Invoke();
                        CleanAppearanceChecks.ValidatePreview(game);
                        Capture("09-CharacterCreator");
                        int original=game.Progress.outfit;
                        game.hud.SetCharacterDraft(1,1,2,1);WindowAndDailyChecks.Close(game);
                        Check(game.Progress.outfit==original,"Canceling customization preserves saved appearance");
                        game.hud.ShowCharacterCreator();game.hud.SetCharacterDraft(2,2,3,2);game.hud.SaveCharacter();
                        Check(game.Progress.outfit==2&&game.Progress.hat==3&&game.Progress.boatStyle==2,"Customization saves selected sprites");
                        Check(game.world.angler.visual.sprite==game.config.CompleteSprite(2,2,3,2),"Saved appearance applies the whole composite sprite to the gameplay rig");
                        Check(GameProgress.Load("LittleDepths.SmokeOnly").hairstyle==2,"Customization survives save/load");
                        game.hud.ShowCharacterCreator();break;
                    case 11:
                        Capture("10-CustomizedAngler");WindowAndDailyChecks.Close(game);
                        SetSeatedAppearance(0);break;
                    case 12:
                        CaptureAngler("11-SeatedSailor");SetSeatedAppearance(1);break;
                    case 13:
                        CaptureAngler("12-SeatedRaincoat");SetSeatedAppearance(2);break;
                    case 14:
                        CaptureAngler("13-SeatedRanger");
                        CleanAppearanceChecks.ValidateAll(game);
                        game.hud.ShowCharacterCreator();game.hud.SetCharacterDraft(0,1,2,0);break;
                    case 15:
                        Capture("16-CleanBobBucketPreview");
                        Check(!game.hud.modalLayer.GetComponentsInChildren<Text>().Any(t=>t.text=="BOAT PAINT"),"Character creator contains no paint palette");
                        game.hud.CloseModal();
                        CleanAppearanceChecks.CheckAllMouthFrames(game);
                        ProvidedSheetChecks.Validate(game);
                        Capture("21-ProvidedHarbor16x9",1920,1080);ProvidedSheetChecks.Edges("21-ProvidedHarbor16x9");
                        game.hud.ShowSettings();break;
                    case 16:
                        CollectionHudChecks.Audio(game);Capture("22-ProvidedPause");WindowAndDailyChecks.Close(game);
                        game.hud.harborRoot.GetComponentsInChildren<Button>().First(b=>b.name=="Upgrades").onClick.Invoke();break;
                    case 17:
                        Check(game.hud.IsModalOpen&&game.hud.modalLayer.GetComponentsInChildren<Image>().Any(i=>i.sprite&&i.sprite.name=="ref_upgrades"),"Runtime upgrades navigation opens the supplied panel");
                        Capture("23-ProvidedUpgrades");
                        int oldSpeed=game.Progress.speedLevel;Check(game.BuyUpgrade(3)&&game.Progress.speedLevel==oldSpeed+1,"Supplied boat speed upgrade applies and persists");
                        WindowAndDailyChecks.Close(game);game.hud.harborRoot.GetComponentsInChildren<Button>().First(b=>b.name=="Missions").onClick.Invoke();break;
                    case 18:
                        Check(game.hud.IsModalOpen&&game.hud.modalLayer.GetComponentsInChildren<Image>().Any(i=>i.sprite&&i.sprite.name=="ref_quests"),"Runtime missions navigation opens the supplied panel");
                        Capture("24-ProvidedMissions");WindowAndDailyChecks.Cross(game);
                        var claim=game.hud.modalLayer.GetComponentsInChildren<Button>().First(b=>b.name=="Claim daily quest 3");
                        Check(game.hud.modalLayer.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Claim daily quest")).All(b=>b.colors.disabledColor.a==1&&b.colors.highlightedColor.a==1),"Daily reward controls remain opaque in disabled and hover states");
                        Check(claim.interactable,"Five real casts enable the daily claim button");
                        int wallet=game.Progress.coins;claim.onClick.Invoke();
                        Check(game.Progress.coins==wallet+100&&!claim.interactable,"Daily claim button pays once and disables");
                        Check(game.ClaimDailyQuest(3)==0&&GameProgress.Load(game.SaveSlot).dailyQuests.claimed.Contains(3),"Paid daily reward stays claimed after reload");break;
                    case 19:
                        Capture("26-DailyQuestClaimed");game.Progress.dailyQuests.day=GameProgress.Now/86400-1;break;
                    case 20:
                        Check(game.Progress.dailyQuests.casts==0&&game.Progress.dailyQuests.fish==0&&game.Progress.dailyQuests.claimed.Count==0,"Daily reset happens while the quest window remains open");
                        Check(game.hud.modalLayer.GetComponentsInChildren<Button>().Where(b=>b.name.StartsWith("Claim daily quest")).All(b=>!b.interactable),"Visible quest claim controls refresh after daily reset");
                        Capture("27-DailyQuestReset");WindowAndDailyChecks.Close(game);
                        Capture("25-ProvidedHarbor21x9",2560,1080);ProvidedSheetChecks.Edges("25-ProvidedHarbor21x9");game.hud.ShowWelcome(100);break;
                    case 21:
                        WindowAndDailyChecks.Close(game);
                        game.NewSession();game.CastButton();game.Launch(1);game.Catch(game.world.Fish.First(f=>!f.isTrash));CollectionHudChecks.AnimatedReel(game);
                        Check(game.world.angler.ManualReelAnimations==1,"Manual reel animation starts once; automatic and repeated returns do not restart it");
                        Check(errors==0,"No runtime errors or exceptions");
                        SessionState.SetBool(Active,false);PlayerPrefs.DeleteKey("LittleDepths.SmokeOnly");PlayerPrefs.Save();
                        Debug.Log("LITTLE_DEPTHS_SMOKE_OK: Runtime loop, capacity, sale, progression, cosmetics, rewards, offline cap, persistence and session reset passed.");
                        File.WriteAllText("Logs/LittleDepths-Validation.status","PASSED: New standalone trees/grass, generated cast meter/bobber with accurate gold target, live depth/haul HUD and working reel control, cross buttons, daily quests, event cards, 16:9/21:9 edges, 108 looks, 40 fish mouth frames, economy and persistence. No player build.");
                        EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(0);return;
                }
                next=EditorApplication.timeSinceStartup+1;
            }
            catch(Exception e)
            {
                SessionState.SetBool(Active,false);Debug.LogException(e);File.WriteAllText("Logs/LittleDepths-Validation.status","FAILED: "+e);
                EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.Exit(1);
            }
        }
        static void Check(bool success,string what)
        { if(!success)throw new Exception("SMOKE FAILED: "+what);Debug.Log("SMOKE PASS: "+what); }
        static void SetSeatedAppearance(int outfit)
        {
            var p=game.Progress;p.outfit=outfit;p.hairstyle=2;p.hat=0;p.boatStyle=0;game.world.angler.Apply(p);
        }
        static void CaptureAngler(string name)
        {
            var camera=game.gameCamera;var position=camera.transform.position;float size=camera.orthographicSize;
            var canvas=game.hud.GetComponent<Canvas>();bool enabled=canvas.enabled;canvas.enabled=false;
            camera.orthographicSize=1.9f;camera.transform.position=new Vector3(1.05f,2.05f,-10);
            Capture(name);camera.transform.position=position;camera.orthographicSize=size;canvas.enabled=enabled;
        }
        public static void Capture(string name,int width=1600,int height=1000)
        {
            Directory.CreateDirectory("Logs/Previews");
            var camera=game.gameCamera;var target=new RenderTexture(width,height,24);
            var old=camera.targetTexture;var active=RenderTexture.active;
            var canvas=game.hud.GetComponent<Canvas>();var scaler=game.hud.GetComponent<CanvasScaler>();
            float oldScale=canvas.scaleFactor;scaler.enabled=false;camera.targetTexture=target;canvas.scaleFactor=Mathf.Min(width/1600f,height/1000f);
            foreach(var text in game.hud.GetComponentsInChildren<Text>(true))text.SetAllDirty();
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes("Logs/Previews/"+name+".png",image.EncodeToPNG());
            camera.targetTexture=old;canvas.scaleFactor=oldScale;scaler.enabled=true;
            RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
