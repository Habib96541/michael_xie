using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace PocketBorough
{
    public enum MatchStage { Menu, Countdown, Playing, Paused, Results }
    public class BoroughGame : MonoBehaviour
    {
        public BoroughCatalog catalog;
        public MatchStage Stage {get;private set;}=MatchStage.Menu;
        public ArenaWorld World {get;private set;}
        public readonly List<HoleAgent> Holes=new(8);
        public readonly List<HoleAgent> Rankings=new(8);
        public HoleAgent Player=>Holes[0];
        public float TimeLeft=120,Countdown=3,Elapsed,AverageFps;
        public int StreetDrops {get;private set;}
        float nextStreetDrop=40;
        public int Best=>PlayerPrefs.GetInt("Borough.Best",0);
        public int Skin {get;private set;}
        public bool AutoPilot {get;set;}
        public Camera Camera {get;private set;}
        public BoroughUI UI {get;private set;}
        public BoroughAudio Audio {get;private set;}
        public static readonly Color[] SkinColors={MeshWorkshop.C("FA896F"),MeshWorkshop.C("76C4C3"),MeshWorkshop.C("B69BDE"),MeshWorkshop.C("EDBC55")};
        Vector3 cameraFocus;
        float intakeClock,rankClock,telemetryClock,qualityClock,frameSeconds,shake;
        int frameCount,dragFinger=-1;
        bool mouseDragging;
        Vector2 dragOrigin,dragPosition;
        readonly Vector4[] holesShader=new Vector4[8];
        readonly List<RaycastResult> uiHits=new(10);
        readonly System.Random random=new(984);
        UniversalRenderPipelineAsset pipeline;
        BoroughVfx vfx;
        readonly Vector3[] starts={new(-38,0,-26),new(36,0,25),new(-12,0,26),new(12,0,-25),new(-36,0,0),new(36,0,-25),new(12,0,45),new(-12,0,-45)};
        void Awake()
        {
            Application.targetFrameRate=60;Application.runInBackground=true;QualitySettings.vSyncCount=0;
            if(catalog==null)catalog=Resources.Load<BoroughCatalog>("BoroughCatalog");
            Camera=UnityEngine.Camera.main;World=new ArenaWorld(transform,catalog);
            string[] names={"You","Mochi","Nimbus","Pickle","Orbit","Clover","Peach","Pebble"};
            string[] colors={"FA896F","8FADDF","B3A0CE","A0BD71","EDBB59","63B7AB","EAA3A9","B69C84"};
            for(int i=0;i<8;i++){var hole=new HoleAgent(names[i],MeshWorkshop.C(colors[i]),i==0,transform,catalog);hole.Reset(starts[i]);Holes.Add(hole);Rankings.Add(hole);}
            Skin=PlayerPrefs.GetInt("Borough.Skin",0)%4;SelectSkin(Skin);
            vfx=new BoroughVfx(transform,catalog);Audio=gameObject.AddComponent<BoroughAudio>();Audio.Initialize(catalog);
            UI=gameObject.AddComponent<BoroughUI>();UI.Initialize(this,catalog);
            bool mobile=Application.isMobilePlatform||(Touchscreen.current!=null&&Screen.height<700);
            QualitySettings.renderPipeline=mobile?catalog.mobilePipeline:catalog.desktopPipeline;
            pipeline=UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline){pipeline.renderScale=mobile?.8f:1;pipeline.shadowDistance=mobile?75:85;}
            if(mobile)Camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            cameraFocus=new(0,0,1);Camera.orthographic=true;Camera.orthographicSize=31;UpdateCamera(1);UI.ShowMenu();
        }
        public void SelectSkin(int skin)
        {
            Skin=skin%4;PlayerPrefs.SetInt("Borough.Skin",Skin);if(Holes.Count==0)return;Player.Color=SkinColors[Skin];
            var block=new MaterialPropertyBlock();block.SetColor("_Tint",Player.Color);Player.Root.Find("Coloured sculpted rim").GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
        public void StartMatch()
        {
            World.Reset();vfx.Clear();for(int i=0;i<8;i++)Holes[i].Reset(starts[i]);
            TimeLeft=BoroughRules.Duration;Elapsed=0;Countdown=3;StreetDrops=0;nextStreetDrop=40;intakeClock=rankClock=0;Stage=MatchStage.Countdown;
            mouseDragging=false;dragFinger=-1;AutoPilot=false;UI.ShowHUD();Audio.Begin();Audio.Play(5);cameraFocus=Player.Position;
        }
        public void TogglePause()
        {
            if(Stage==MatchStage.Playing||Stage==MatchStage.Countdown){Stage=MatchStage.Paused;mouseDragging=false;dragFinger=-1;UI.ShowPause();}
            else if(Stage==MatchStage.Paused)Resume();
        }
        public void Resume(){Stage=Countdown>0?MatchStage.Countdown:MatchStage.Playing;mouseDragging=false;dragFinger=-1;UI.ShowHUD();}
        public void MainMenu(){Stage=MatchStage.Menu;World.Reset();vfx.Clear();for(int i=0;i<8;i++)Holes[i].Reset(starts[i]);mouseDragging=false;dragFinger=-1;UI.ShowMenu();}
        public void BrowserBlur(){if(Stage==MatchStage.Playing||Stage==MatchStage.Countdown)TogglePause();}
        void OnApplicationFocus(bool focused){if(!focused)BrowserBlur();}
        void Update()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)TogglePause();
            if(Stage==MatchStage.Countdown)
            {
                int previous=Mathf.CeilToInt(Countdown);Countdown-=Time.unscaledDeltaTime;if(Mathf.CeilToInt(Countdown)!=previous)Audio.Play(5);
                if(Countdown<=0){Countdown=0;Stage=MatchStage.Playing;}
            }
            else if(Stage==MatchStage.Playing)
            {
                Elapsed+=Time.unscaledDeltaTime;TimeLeft=Mathf.Max(0,TimeLeft-Time.unscaledDeltaTime);UpdateHoles(dt);
                World.Animate(dt,Reward);vfx.Tick(dt);intakeClock-=dt;
                if(intakeClock<=0){intakeClock=.075f;foreach(var hole in Holes)if(hole.Alive)World.TryConsume(hole);CheckRivals();}
                if(Elapsed>=nextStreetDrop&&nextStreetDrop<120){int count=World.Restock(Holes);StreetDrops++;nextStreetDrop+=40;UI.Toast("FRESH STREET DROP  /  +"+count+" BITES");Audio.Play(2);}
                if(TimeLeft<=0)Finish();
            }
            foreach(var hole in Holes)hole.Draw(dt,Camera,Time.unscaledTime);
            for(int i=0;i<8;i++){var h=Holes[i];holesShader[i]=h.Alive?new(h.Position.x,h.Position.z,h.VisualRadius,0):new(1000,1000,0,0);}Shader.SetGlobalVectorArray("_BoroughHoles",holesShader);
            UpdateCamera(dt);rankClock-=dt;if(rankClock<=0){rankClock=.2f;SortRankings();UI.Refresh();}
            UI.Animate(dt);Audio.Tick(Stage==MatchStage.Paused);
            frameCount++;frameSeconds+=Time.unscaledDeltaTime;
            if(frameSeconds>=2){AverageFps=frameCount/frameSeconds;frameCount=0;frameSeconds=0;}
            if(Stage==MatchStage.Playing&&pipeline){qualityClock+=dt;if(qualityClock>5){qualityClock=0;if(AverageFps>1&&AverageFps<31)pipeline.renderScale=Mathf.Max(.65f,pipeline.renderScale-.08f);}}
            telemetryClock-=dt;if(telemetryClock<=0){telemetryClock=.5f;Report();}
        }
        void UpdateHoles(float dt)
        {
            foreach(var hole in Holes)
            {
                if(!hole.Alive)
                {if(Elapsed>=hole.RespawnAt){hole.Alive=true;hole.Position=starts[random.Next(8)];hole.Velocity=Vector3.zero;hole.Target=null;}else continue;}
                Vector2 direction;
                if(hole.IsPlayer&&!AutoPilot)direction=ReadInput();else direction=Think(hole);
                hole.Move(direction*(hole.IsPlayer?1:.9f),dt);
            }
        }
        Vector2 Think(HoleAgent hole)
        {
            if(Elapsed>=hole.DecisionAt)
            {hole.DecisionAt=Elapsed+.38f+(float)random.NextDouble()*.2f;hole.Target=World.Target(hole.Position,hole.Radius);}
            var target=hole.Target;Vector3 desired=target!=null&&target.available?target.home-hole.Position:Vector3.zero;
            float closestThreat=18;
            foreach(var other in Holes)
            {
                if(other==hole||!other.Alive)continue;var delta=other.Position-hole.Position;float dist=delta.magnitude;
                if(BoroughRules.CanEatHole(other.Radius,hole.Radius)&&dist<closestThreat){desired=-delta*2;closestThreat=dist;}
                else if(closestThreat==18&&BoroughRules.CanEatHole(hole.Radius,other.Radius)&&dist<hole.Radius+5)desired=delta;
            }
            return new Vector2(desired.x,desired.z).normalized;
        }
        bool OverUI(Vector2 p)
        {
            if(EventSystem.current==null)return false;var data=new PointerEventData(EventSystem.current){position=p};uiHits.Clear();EventSystem.current.RaycastAll(data,uiHits);return uiHits.Count>0;
        }
        Vector2 ReadInput()
        {
            Vector2 v=Vector2.zero;var kb=Keyboard.current;
            if(kb!=null){v.x=(kb.dKey.isPressed||kb.rightArrowKey.isPressed?1:0)-(kb.aKey.isPressed||kb.leftArrowKey.isPressed?1:0);v.y=(kb.wKey.isPressed||kb.upArrowKey.isPressed?1:0)-(kb.sKey.isPressed||kb.downArrowKey.isPressed?1:0);}
            var touch=Touchscreen.current;
            if(touch!=null)
            {
                foreach(var finger in touch.touches)
                {
                    int id=finger.touchId.ReadValue();
                    if(finger.press.wasPressedThisFrame&&dragFinger<0&&!OverUI(finger.position.ReadValue())){dragFinger=id;dragOrigin=finger.position.ReadValue();dragPosition=dragOrigin;}
                    if(id==dragFinger){dragPosition=finger.position.ReadValue();if(!finger.press.isPressed)dragFinger=-1;}
                }
            }
            var mouse=Mouse.current;
            if(mouse!=null&&dragFinger<0)
            {
                if(mouse.leftButton.wasPressedThisFrame&&!OverUI(mouse.position.ReadValue())){mouseDragging=true;dragOrigin=mouse.position.ReadValue();}
                if(!mouse.leftButton.isPressed)mouseDragging=false;
                if(mouseDragging)dragPosition=mouse.position.ReadValue();
            }
            if(dragFinger>=0||mouseDragging){v=Vector2.ClampMagnitude((dragPosition-dragOrigin)/Mathf.Max(45,Screen.height*.12f),1);UI.SetJoystick(dragOrigin,dragPosition,true);}else UI.SetJoystick(default,default,false);
            return Vector2.ClampMagnitude(v,1);
        }
        void Reward(HoleAgent hole,CityObject obj)
        {
            int before=hole.Level;hole.Reward(obj.model.mass,obj.model.points);
            if(hole.IsPlayer){UI.Popup("+"+obj.model.points,obj.home,Player.Color);vfx.Burst(hole.Position,Player.Color);Audio.Play(obj.model.category==0?1:0);shake=Mathf.Min(.18f,shake+.025f);if(hole.Level>before){vfx.Burst(hole.Position,MeshWorkshop.Gold,16);Audio.Play(3);UI.Toast("LEVEL "+hole.Level+"  /  BIGGER BITES");}}
        }
        void CheckRivals()
        {
            for(int i=0;i<8;i++)for(int j=i+1;j<8;j++)
            {
                var a=Holes[i];var b=Holes[j];if(!a.Alive||!b.Alive)continue;
                var big=a.Radius>=b.Radius?a:b;var small=big==a?b:a;
                if(!BoroughRules.CanEatHole(big.Radius,small.Radius)||(big.Position-small.Position).sqrMagnitude>Mathf.Pow(big.Radius-small.Radius*.35f,2))continue;
                big.Reward(8+small.Mass*.2f,100+small.Score/4);small.SinkInto(big);small.RespawnAt=Elapsed+3;small.Mass*=.65f;small.Radius=BoroughRules.Radius(small.Mass);small.Velocity=Vector3.zero;
                if(big.IsPlayer){UI.Toast("RIVAL SWALLOWED  +100");Audio.Play(2);}if(small.IsPlayer){UI.Toast("GULP!  BACK IN 3 SECONDS");Audio.Play(8);}
            }
        }
        void SortRankings()=>Rankings.Sort((a,b)=>{int score=b.Score.CompareTo(a.Score);return score!=0?score:string.CompareOrdinal(a.Name,b.Name);});
        public int PlayerRank=>Rankings.IndexOf(Player)+1;
        void Finish()
        {
            Stage=MatchStage.Results;SortRankings();if(Player.Score>Best){PlayerPrefs.SetInt("Borough.Best",Player.Score);PlayerPrefs.Save();}UI.ShowResults();Audio.Play(PlayerRank==1?6:7);
        }
        void UpdateCamera(float dt)
        {
            var desired=Stage==MatchStage.Menu?new Vector3(-2,0,5):Player.Position;
            cameraFocus=Vector3.Lerp(cameraFocus,desired,1-Mathf.Exp(-dt*4));
            float targetSize=Stage==MatchStage.Menu?32:Mathf.Clamp(12+Player.VisualRadius*1.4f,13.2f,31);
            targetSize*=Mathf.Max(1,1.5f/Mathf.Max(.6f,Camera.aspect));Camera.orthographicSize=Mathf.Lerp(Camera.orthographicSize,targetSize,1-Mathf.Exp(-dt*3));
            cameraFocus.x=Mathf.Clamp(cameraFocus.x,-55,55);cameraFocus.z=Mathf.Clamp(cameraFocus.z,-55,55);
            Camera.transform.rotation=Quaternion.Euler(56,0,0);Camera.transform.position=cameraFocus-Camera.transform.forward*65;
            if(shake>0&&Stage==MatchStage.Playing){shake=Mathf.Max(0,shake-dt);Camera.transform.position+=new Vector3(Mathf.Sin(Time.unscaledTime*75),Mathf.Cos(Time.unscaledTime*68),0)*shake*.3f;}
        }
        [System.Serializable] class Telemetry{public string stage;public float timeLeft,radius,fps;public int score,consumed,rank,available,falling,opponents,streetDrops;public long managedBytes;public int[] rivalScores;public float x,z,targetX,targetZ;}
        void Report()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var scores=new int[7];for(int i=1;i<8;i++)scores[i-1]=Holes[i].Score;
            var nearest=World.Target(Player.Position,Player.Radius);var target=nearest!=null?nearest.home:Player.Position;
            BoroughReport(JsonUtility.ToJson(new Telemetry{stage=Stage.ToString(),timeLeft=TimeLeft,radius=Player.Radius,score=Player.Score,consumed=Player.Consumed,rank=PlayerRank,available=World.Remaining,falling=World.falling.Count,opponents=7,streetDrops=StreetDrops,managedBytes=System.GC.GetTotalMemory(false),fps=AverageFps,rivalScores=scores,x=Player.Position.x,z=Player.Position.z,targetX=target.x,targetZ=target.z}));
#endif
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BoroughReport(string json);
#endif
    }
}
