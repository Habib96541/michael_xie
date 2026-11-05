using System;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ChickenRush
{
    public sealed class ChickenRushGame : MonoBehaviour
    {
        [SerializeField] RushDifficulty[] difficulties;
        [SerializeField] GameObject chickenPrefab;
        [SerializeField] Sprite roundedSprite,discSprite,squareSprite,arrowSprite,asphaltSprite,uiSprite,gearSprite,uiArrowSprite,coinSprite;
        [SerializeField] Sprite[] vehicleSprites;
        [SerializeField] Sprite landingPadSprite,barrierSprite,shrubSprite,sidewalkSprite;
        [SerializeField] TMP_FontAsset font;
        [SerializeField] Material spriteMaterial;
        [SerializeField] Camera gameCamera;
        RoundRules round;
        RoadWorld road;
        RushHUD hud;
        RushEffects effects;
        Transform worldRoot,chicken;
        ChickenAnimationController animationView;
        SpriteRenderer shadow;
        int difficultyIndex,roundsPlayed,totalCrossings,highestReward,nearMisses,sessionSeed;
        float best=1,stateClock,hopFrom,shake,telemetryClock,cameraX,passingCooldown;
        bool paused,soundEnabled=true,restartReady=true,qa,nearMissThisHop,warned;
        string notice="One hop. One choice.";
        RushDifficulty Config=>difficulties[difficultyIndex];
        public RoundRules Rules=>round;
        public ChickenAnimationController AnimationView=>animationView;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int RushIsQA();
        [DllImport("__Internal")] static extern void RushSnapshot(string json);
        [DllImport("__Internal")] static extern void RushFullscreen();
#endif
        [Serializable] sealed class Snapshot
        {
            public string version="3.0.0",state,animation,difficulty;
            public int row,balance,stake,reward,rounds,nearMisses;
            public float multiplier,animationTime,fps,x,y,cameraX,cameraY,worldWidth,worldHeight,hopSeconds,nextVehicleSeconds;
            public bool paused,hopEnabled,bankEnabled,impactContact;
            public float nextChance;
            public RushHUD.UISnapshot ui;
            public RoadWorld.VehicleSnapshot[] vehicles;
            public RoadWorld.LaneSnapshot[] lanes;
        }
        readonly Snapshot snapshot=new Snapshot();

        void Start()
        {
            Time.timeScale=1;AudioListener.pause=false;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            difficultyIndex=Mathf.Clamp(PlayerPrefs.GetInt("rush.difficulty",1),0,difficulties.Length-1);
            roundsPlayed=PlayerPrefs.GetInt("rush.rounds",0);totalCrossings=PlayerPrefs.GetInt("rush.crossings",0);highestReward=PlayerPrefs.GetInt("rush.reward",0);
            best=PlayerPrefs.GetFloat("rush.best",1);soundEnabled=PlayerPrefs.GetInt("rush.sound",1)==1;
            sessionSeed=PlayerPrefs.GetInt("rush.seed",Guid.NewGuid().GetHashCode());PlayerPrefs.SetInt("rush.seed",sessionSeed);
            round=new RoundRules(PlayerPrefs.GetInt("rush.balance",10),Config.multipliers,Config.successChances);CreateRoad();
            chicken=Instantiate(chickenPrefab,transform).transform;chicken.name="Animated Chicken";chicken.localScale=Vector3.one*1.02f;
            animationView=chicken.GetComponent<ChickenAnimationController>();
            shadow=road.Rect("Chicken contact shadow",chicken,new Vector2(.04f,-.025f),new Vector2(.85f,.24f),new Color(.04f,.08f,.12f,.29f),5,true);
            effects=new RushEffects(transform,discSprite,spriteMaterial);
            hud=new RushHUD(transform,uiSprite,gearSprite,uiArrowSprite,coinSprite,font,Primary,CashOut,ChangeStake,CycleDifficulty,TogglePause,ToggleSound,Fullscreen);
            hud.Sound(soundEnabled);UpdateCameraLayout();SnapCamera();
#if UNITY_WEBGL && !UNITY_EDITOR
            qa=RushIsQA()==1;
#endif
            Refresh();
        }
        void CreateRoad()
        {
            if(worldRoot)Destroy(worldRoot.gameObject);worldRoot=new GameObject("Vertical roads and pooled traffic").transform;worldRoot.SetParent(transform,false);
            road=new RoadWorld(worldRoot,roundedSprite,discSprite,squareSprite,arrowSprite,asphaltSprite,font,spriteMaterial,Config,vehicleSprites,landingPadSprite,barrierSprite,shrubSprite,sidewalkSprite);
        }
        void Update()
        {
            if(hud==null)return;if(hud.Resize())UpdateCameraLayout();
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.pKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)TogglePause();
                if(keyboard.mKey.wasPressedThisFrame)ToggleSound();
                if(!paused)
                {
                    if(keyboard.spaceKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)Primary();
                    if(keyboard.cKey.wasPressedThisFrame)CashOut();
                }
            }
            if(!paused)
            {
                float remaining=Time.deltaTime;
                while(remaining>0){float step=Mathf.Min(remaining,1f/120);Simulate(step);remaining-=step;}
                effects.Tick(Time.deltaTime);passingCooldown=Mathf.Max(0,passingCooldown-Time.deltaTime);
                bool danger=round.State==RoundState.Safe && round.Row>=4;
                if(danger && !warned)effects.Play(6,soundEnabled);warned=danger;
            }
            hud.Tick(Time.unscaledDeltaTime);
            if(qa && (telemetryClock+=Time.unscaledDeltaTime)>.08f){telemetryClock=0;PublishSnapshot();}
        }
        void Simulate(float dt)
        {
            bool playing=round.State==RoundState.Safe || round.State==RoundState.Hopping;
            var feedback=road.Tick(dt,chicken.position,round.State==RoundState.Hopping,playing);stateClock+=dt;
            if(feedback.nearMiss)nearMissThisHop=true;
            if(feedback.pass && passingCooldown<=0)
            {passingCooldown=.30f;shake=Mathf.Max(shake,.016f);effects.Pass(feedback.passSpeed,soundEnabled);}
            if(round.State==RoundState.Preparing)
            {
                float t=Mathf.Clamp01(stateClock/.32f);chicken.position=new Vector3(Mathf.Lerp(-.45f,0,t),0,0);
                if(t>=1){round.Ready();animationView.Walk(false);notice="Hop, or hold your coins.";Refresh();}
            }
            else if(round.State==RoundState.Hopping)
            {
                Vector2 previous=chicken.position;float progress=TrafficPattern.HopProgress(stateClock,Config.hopSeconds);
                chicken.position=new Vector3(hopFrom+RoadWorld.Step*progress,0,0);
                float arc=Mathf.Sin(progress*Mathf.PI)*.18f;shadow.transform.localScale=new Vector3(.85f*(1-arc),.24f*(1-arc),1);
                if(road.Collides(chicken.position,previous))
                {
                    if(round.Hit())
                    {
                        animationView.Lose();stateClock=0;restartReady=false;shake=.06f;notice="Ouch! "+round.Stake+(round.Stake==1?" coin lost.":" coins lost.");
                        effects.Play(2,soundEnabled);effects.Burst(chicken.position,false);Save();Refresh();
                    }
                }
                else if(stateClock>=Config.hopSeconds && round.PlannedSuccess)
                {
                    round.Land();road.Secure(round.Row);totalCrossings++;best=Mathf.Max(best,round.Multiplier);effects.Play(1,soundEnabled);effects.Burst(chicken.position,false);
                    shadow.transform.localScale=new Vector3(.85f,.24f,1);
                    if(nearMissThisHop){nearMisses++;effects.NearMiss(chicken.position);shake=Mathf.Max(shake,.016f);}
                    notice=round.Row>=Config.multipliers.Length?"Every lane cleared!":"SAFE · LANE "+round.Row+" / "+Config.multipliers.Length;
                    Save();Refresh();if(round.Row>=Config.multipliers.Length)CashOut();
                }
            }
            else if(round.Finished && !restartReady && stateClock>=.95f){restartReady=true;Refresh();}
        }
        void UpdateCameraLayout()
        {
            gameCamera.rect=hud.CameraRect;gameCamera.ResetAspect();
            var layout=hud.Layout;float height=layout.Compact?5.3f:layout.mode==RushDisplay.Portrait?9.2f/gameCamera.aspect:layout.mode==RushDisplay.Tablet?10:8.8f;
            gameCamera.orthographicSize=height*.5f;
        }
        void SnapCamera()
        {
            cameraX=chicken.position.x+gameCamera.orthographicSize*2*gameCamera.aspect*.24f;
            gameCamera.transform.position=new Vector3(cameraX,hud.Layout.Compact?.85f:1.45f,-10);
        }
        void LateUpdate()
        {
            if(!chicken || hud==null)return;
            float target=chicken.position.x+gameCamera.orthographicSize*2*gameCamera.aspect*.24f;
            cameraX=Mathf.Lerp(cameraX,target,1-Mathf.Exp(-Time.deltaTime*11));shake*=Mathf.Exp(-Time.deltaTime*15);
            gameCamera.transform.position=new Vector3(cameraX+Mathf.Sin(Time.time*94)*shake,(hud.Layout.Compact?.85f:1.45f)+Mathf.Cos(Time.time*83)*shake*.4f,-10);
            road.Present(gameCamera.orthographicSize,round.Row,Time.time,cameraX,gameCamera.orthographicSize*gameCamera.aspect);
        }
        public void Primary()
        {
            if(paused || !restartReady)return;
            if(round.CanStart)
            {
                if(round.Balance==0){round.Refill();notice="10 free coins. A fresh crossing.";Save();Refresh();return;}
                int seed=unchecked(sessionSeed+(roundsPlayed+1)*7919);
                round.SetStake(Mathf.Min(round.Stake,round.Balance));if(!round.Start(seed))return;
                effects.Clear();animationView.ResetForRound();animationView.Walk(true);chicken.position=new Vector3(-.45f,0,0);stateClock=0;roundsPlayed++;
                road.Reset(seed);SnapCamera();notice="One lane at a time.";effects.Play(4,soundEnabled);Save();Refresh();return;
            }
            if(round.Hop())
            {
                hopFrom=round.Row*RoadWorld.Step;stateClock=0;nearMissThisHop=false;road.BeginAttempt(round.Row+1,round.PlannedSuccess,Config.hopSeconds);
                animationView.Jump(Config.hopSeconds);effects.Play(0,soundEnabled);notice="Crossing...";Refresh();
            }
        }
        public void CashOut()
        {
            if(paused || !round.CashOut())return;stateClock=0;restartReady=false;highestReward=Mathf.Max(highestReward,round.Reward);
            animationView.Win();effects.Play(3,soundEnabled);effects.Burst(chicken.position,true);notice="+"+round.Reward+" COINS SECURED";Save();Refresh();
        }
        void ChangeStake(int change)
        {
            if(paused || !round.CanStart)return;round.SetStake(round.Stake+change);effects.Play(4,soundEnabled);Refresh();
        }
        void CycleDifficulty()
        {
            if(paused || !round.CanStart || !restartReady)return;int coins=round.Balance,stake=round.Stake;
            difficultyIndex=(difficultyIndex+1)%difficulties.Length;round=new RoundRules(coins,Config.multipliers,Config.successChances);round.SetStake(stake);
            CreateRoad();effects.Clear();animationView.ResetForRound();chicken.position=Vector3.zero;SnapCamera();notice="One hop. One choice.";Save();Refresh();
        }
        public void TogglePause()
        {
            if(hud==null)return;paused=!paused;Time.timeScale=paused?0:1;AudioListener.pause=paused;hud.Pause(paused);
        }
        void ToggleSound(){soundEnabled=!soundEnabled;hud.Sound(soundEnabled);effects.Tension(round.Row,round.State==RoundState.Safe || round.State==RoundState.Hopping,soundEnabled);PlayerPrefs.SetInt("rush.sound",soundEnabled?1:0);PlayerPrefs.Save();}
        void Fullscreen()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            RushFullscreen();
#else
            Screen.fullScreen=!Screen.fullScreen;
#endif
        }
        void OnApplicationFocus(bool focus){if(!focus && hud!=null && !paused && !round.CanStart)TogglePause();}
        void OnApplicationPause(bool pause){if(pause && hud!=null && !paused && !round.CanStart)TogglePause();}
        void OnDestroy(){Time.timeScale=1;AudioListener.pause=false;}
        void Refresh()
        {
            animationView.ReactToDanger(round.State==RoundState.Safe && round.Row>=3);
            effects.Tension(round.Row,round.State==RoundState.Safe || round.State==RoundState.Hopping,soundEnabled);
            hud.Refresh(round,Config,best,notice,restartReady);
        }
        void Save()
        {
            PlayerPrefs.SetInt("rush.balance",round.Balance);PlayerPrefs.SetInt("rush.rounds",roundsPlayed);PlayerPrefs.SetInt("rush.crossings",totalCrossings);
            PlayerPrefs.SetInt("rush.reward",highestReward);PlayerPrefs.SetFloat("rush.best",best);PlayerPrefs.SetInt("rush.difficulty",difficultyIndex);PlayerPrefs.Save();
        }
        void PublishSnapshot()
        {
            var state=animationView.Animator.GetCurrentAnimatorStateInfo(0);
            snapshot.animation=state.IsName("Walk")?"Walk":state.IsName("Hop")?"Hop":state.IsName("Hit")?"Hit":state.IsName("Celebrate")?"Celebrate":state.IsName("Danger")?"Danger":"Idle";
            snapshot.state=round.State.ToString();snapshot.row=round.Row;snapshot.balance=round.Balance;snapshot.stake=round.Stake;snapshot.reward=round.Reward;
            snapshot.multiplier=round.Multiplier;snapshot.rounds=roundsPlayed;snapshot.nearMisses=nearMisses;snapshot.paused=paused;snapshot.difficulty=Config.displayName;
            snapshot.animationTime=state.normalizedTime;snapshot.fps=1/Mathf.Max(Time.unscaledDeltaTime,.0001f);snapshot.x=chicken.position.x;snapshot.y=chicken.position.y;
            snapshot.cameraX=gameCamera.transform.position.x;snapshot.cameraY=gameCamera.transform.position.y;snapshot.worldHeight=gameCamera.orthographicSize*2;snapshot.worldWidth=snapshot.worldHeight*gameCamera.aspect;
            snapshot.hopSeconds=Config.hopSeconds;snapshot.nextVehicleSeconds=road.TimeToVehicle(round.Row+1);
            snapshot.hopEnabled=!paused && restartReady && (round.CanStart || round.State==RoundState.Safe);snapshot.bankEnabled=!paused && round.State==RoundState.Safe && round.Row>0;
            snapshot.impactContact=road.ImpactContact;snapshot.nextChance=round.NextChance;
            snapshot.ui=hud.Snapshot();road.FillSnapshots();snapshot.vehicles=road.vehicleSnapshots;snapshot.lanes=road.laneSnapshots;
#if UNITY_WEBGL && !UNITY_EDITOR
            RushSnapshot(JsonUtility.ToJson(snapshot));
#endif
        }
    }
}
