using System.Runtime.InteropServices;
using UnityEngine;
using TMPro;

namespace BrickRush
{
    public sealed class GameManager : MonoBehaviour
    {
        public GameConfig Config {get;private set;}
        public SessionState State {get;private set;}=SessionState.Menu;
        public Camera Camera {get;private set;}
        public Transform ArenaRoot {get;private set;}
        public PaddleController Paddle {get;private set;}
        public BrickWaveManager Waves {get;private set;}
        public PowerUpManager Powers {get;private set;}
        public ScoreManager Score {get;}=new ScoreManager();
        public VFXManager VFX {get;private set;}
        public AudioManager Audio {get;private set;}
        public UIManager UI {get;private set;}
        public InputManager Controls {get;}=new InputManager();
        public BallController[] Balls {get;private set;}
        public int Lives {get;private set;}
        public float Remaining {get;private set;}
        public float Elapsed => Config.duration-Remaining;
        public bool ReducedEffects {get;set;}
        public bool ScreenShake {get;set;}
        public int FinalScore {get;private set;}
        public bool NewBest {get;private set;}
        public bool QA {get;private set;}
        public bool AutoPaddle {get;private set;}
        public int PaddleReturns {get;private set;}
        public int ShieldSaves {get;private set;}
        public int ActiveBalls {get {int n=0;foreach(var b in Balls)if(b.Active)n++;return n;}}
        SessionState beforePause;float countdown,waveDelay,reportClock,uiClock;int lastCount;SpriteRenderer shield;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void BrickRushReport(string json);
#endif
        void Awake()
        {
            Application.targetFrameRate=Application.platform==RuntimePlatform.WebGLPlayer?-1:60;Application.runInBackground=true;Time.fixedDeltaTime=1f/120;
            Config=Resources.Load<GameConfig>("GameConfig");Art.Load();
            ReducedEffects=SaveManager.Volume("reduced",0)>0;ScreenShake=SaveManager.Volume("shake",1)>0;
            QA=Application.absoluteURL.Contains("qa=1");
            Camera=GetComponentInChildren<Camera>();if(Camera==null){var c=new GameObject("Arena Camera");c.transform.SetParent(transform);Camera=c.AddComponent<Camera>();c.AddComponent<AudioListener>();}
            Camera.orthographic=true;Camera.orthographicSize=7.3f;Camera.transform.position=new Vector3(0,0,-10);Camera.backgroundColor=Art.Ink;Camera.clearFlags=CameraClearFlags.SolidColor;
            ArenaRoot=new GameObject("Arena").transform;ArenaRoot.SetParent(transform);
            BuildArena();var font=Resources.Load<TMP_FontAsset>("DisplayFont");
            Audio=new AudioManager(gameObject);Paddle=new PaddleController(ArenaRoot);Waves=new BrickWaveManager(ArenaRoot);VFX=new VFXManager(this,ArenaRoot,font);Powers=new PowerUpManager(this,ArenaRoot,font);
            Balls=new BallController[Config.maxBalls];for(int i=0;i<Balls.Length;i++)Balls[i]=new BallController(this,ArenaRoot,i);
            UI=gameObject.AddComponent<UIManager>();UI.Initialize(this);
            ShowMenu();
        }
        void BuildArena()
        {
            var bg=Art.Sprite("Animated arena",Art.Panel,Vector2.zero,new Vector2(9.9f,14.6f),Color.white,-10,ArenaRoot);bg.sharedMaterial=Resources.Load<Material>("ArenaMaterial");
            for(int side=-1;side<=1;side+=2)
            {
                Art.Sprite("Boundary halo",Art.Glow,new Vector2(side*4.76f,0),new Vector2(.65f,16),new Color(.12f,.8f,.9f,.19f),-2,ArenaRoot);
                Art.Sprite("Boundary",Art.Panel,new Vector2(side*4.75f,0),new Vector2(.025f,13.65f),new Color(.24f,.7f,.8f,.65f),1,ArenaRoot);
                for(int j=0;j<7;j++)Art.Sprite("Rail accent",Art.Panel,new Vector2(side*4.75f,5.6f-j*1.85f),new Vector2(.07f,.35f),j%2==0?Art.Cyan:Art.Violet,2,ArenaRoot);
            }
            Art.Sprite("Top rail",Art.Panel,new Vector2(0,6.87f),new Vector2(9.5f,.025f),new Color(.3f,.7f,.8f,.7f),1,ArenaRoot);
            Art.Sprite("Loss line",Art.Panel,new Vector2(0,-6.67f),new Vector2(9.4f,.016f),new Color(1,.4f,.5f,.16f),0,ArenaRoot);
            shield=Art.Sprite("Shield field",Art.Panel,new Vector2(0,-6.5f),new Vector2(9.35f,.055f),Art.Cyan,10,ArenaRoot);shield.enabled=false;
        }
        public void ShowMenu()
        {
            State=SessionState.Menu;beforePause=State;Remaining=Config.duration;Lives=Config.lives;Score.Reset();Waves.Reset();Waves.Generate(0);Powers.Reset();Paddle.Reset();Controls.Reset();VFX.Reset();foreach(var b in Balls)b.Hide();
            Balls[0].Spawn(new Vector2(0,-5.55f),new Vector2(.4f,1),true);Audio.SetGameplay(false);UI.ShowMenu();
        }
        public void StartRun()
        {
            if(State==SessionState.Playing||State==SessionState.Countdown)return;
            Audio.Unlock();Score.Reset();Remaining=Config.duration;Lives=Config.lives;FinalScore=0;NewBest=false;PaddleReturns=ShieldSaves=0;Powers.Reset();VFX.Reset();Waves.Reset();Waves.Generate(0);Paddle.Reset();Controls.Reset();waveDelay=0;
            foreach(var b in Balls)b.Hide();Balls[0].Spawn(new Vector2(0,-5.55f),new Vector2(.38f,1),true);
            countdown=3;lastCount=4;State=SessionState.Countdown;Audio.SetGameplay(true);UI.ShowGame();
        }
        void Update()
        {
            float clockDt=Time.unscaledDeltaTime;float dt=Mathf.Min(clockDt,.1f);Controls.Tick(Camera,dt);
            if(Controls.Pause&&(State==SessionState.Playing||State==SessionState.Countdown||State==SessionState.Paused))TogglePause();
            if(State==SessionState.Countdown)
            {
                countdown-=clockDt;int n=Mathf.CeilToInt(countdown);if(n!=lastCount){lastCount=n;if(n>0)Audio.Play("countdown");UI.Countdown(n>0?n.ToString():"GO!");}
                if(countdown<=0){State=SessionState.Playing;foreach(var b in Balls)if(b.Active)b.Launch();UI.HideCountdown();Audio.Play("launch");}
            }
            if(State==SessionState.Playing)
            {
                Remaining=Mathf.Max(0,Remaining-clockDt);
                if(Remaining<=0){Finish();}
                else
                {
                    Powers.Tick(dt);
                    if(Waves.Evolve(Elapsed)){UI.Toast(Elapsed>=70?"FINAL FREQUENCY!":"TEMPO UP",Art.Orange);Audio.Play("combo",.4f);}
                    if(waveDelay>0){waveDelay-=dt;if(waveDelay<=0){Waves.Generate(Elapsed);for(int i=0;i<Balls.Length;i++)if(Balls[i].Active)Balls[i].Spawn(new Vector2(Paddle.X+(i%3-1)*.25f,-5.35f),new Vector2(i%2==0?.4f:-.4f,1));UI.Toast("WAVE "+Waves.Wave+"  /  "+BrickWaveManager.LayoutNames[(Waves.Wave-1)%8],Art.Gold);}}
                    if(Controls.Launch)foreach(var b in Balls)if(b.Active&&b.Docked)b.Launch();
                }
            }
            if(State==SessionState.Playing||State==SessionState.Countdown||State==SessionState.Menu)
            {
                float target=Controls.Target;
                if(QA&&AutoPaddle&&State==SessionState.Playing){float best=100;foreach(var b in Balls)if(b.Active&&!b.Docked&&b.Direction.y<0&&b.Position.y<best){best=b.Position.y;target=b.Position.x;}Controls.SetTarget(target);}
                Paddle.Tick(target,Config.paddleWidth*(Powers.Has(PowerKind.Wide)?1.55f:1),dt);
                if(State!=SessionState.Playing)foreach(var b in Balls)if(b.Active&&b.Docked)b.Step(0,0);
                Waves.Tick(dt);
            }
            if(State!=SessionState.Paused)VFX.Tick(dt);
            shield.enabled=Powers.Shield;uiClock-=dt;if(uiClock<=0){uiClock=.05f;UI.Refresh();}UI.Animate(dt);
            if(QA){reportClock-=dt;if(reportClock<=0){reportClock=.2f;Report();}}
        }
        void FixedUpdate()
        {
            if(State!=SessionState.Playing||waveDelay>0)return;
            float speed=Mathf.Min(17,Mathf.Lerp(Config.initialSpeed,Config.finalSpeed,Elapsed/Config.duration)+(Waves.Wave-1)*.3f)*(Powers.Has(PowerKind.Slow)?.7f:1);
            Physics2D.SyncTransforms();
            foreach(var ball in Balls)ball.Step(Time.fixedDeltaTime*VFX.SimulationScale,speed);
            if(ActiveBalls==0)
            {
                Lives--;Score.Return();Audio.Play("lost");VFX.Shake(.09f);UI.Toast(Lives>0?"RESET. YOU'VE GOT THIS.":"LAST BALL",Art.Pink);
                if(Lives<=0){Finish();return;}
                Balls[0].Spawn(new Vector2(Paddle.X,-5.55f),new Vector2(-.36f,1),true);countdown=1.2f;lastCount=3;State=SessionState.Countdown;
            }
            if(Waves.Remaining==0&&waveDelay<=0&&State==SessionState.Playing){waveDelay=.55f;VFX.Burst(Vector2.zero,Art.Gold,24);Audio.Play("combo");UI.Toast("WAVE CLEAR!",Art.Gold);}
        }
        public void HitBrick(BrickController brick,Vector2 position,bool fire)
        {
            if(State!=SessionState.Playing||!brick.Alive)return;
            var kind=brick.Kind;var color=brick.Color;
            bool destroyed=brick.Damage();
            if(fire&&!destroyed&&kind==BrickKind.Armored)destroyed=brick.Damage();
            if(!destroyed){Audio.Play("hit",.45f,true);VFX.Burst(position,color,3);return;}
            Waves.Destroyed();int oldMultiplier=Score.Multiplier;int points=Score.Award(kind,Powers.Has(PowerKind.Double));VFX.Burst(brick.Position,color,kind==BrickKind.Explosive?24:10);VFX.Number(brick.Position,points,color);Audio.Play(kind==BrickKind.Explosive?"explosion":"break",kind==BrickKind.Explosive?.7f:.65f,true);
            Powers.Offer(brick.Position,kind==BrickKind.Bonus);VFX.Shake(kind==BrickKind.Explosive?.075f:.012f);
            if(Score.Multiplier>oldMultiplier){string[] calls={"","","NICE!","GREAT!","AMAZING!","INSANE COMBO!"};UI.Toast(calls[Score.Multiplier]+"  x"+Score.Multiplier,Art.Gold);Audio.Play("combo",.55f);if(Score.Multiplier==5)VFX.SlowMotion();}
            if(Score.Combo>0&&Score.Combo%20==0){UI.Toast("BRICK FRENZY!",Art.Pink);VFX.SlowMotion();}
            if(kind==BrickKind.Explosive)
            {
                // Mark dead before recursion. Adjacent explosive bricks can chain exactly once.
                for(int i=0;i<Waves.Bricks.Length;i++){var other=Waves.Bricks[i];if(other.Alive&&other.Kind!=BrickKind.Obstacle&&Mathf.Abs(other.Position.x-brick.Position.x)<1.1f&&Mathf.Abs(other.Position.y-brick.Position.y)<.63f){HitBrick(other,other.Position,true);}}
            }
        }
        public void PaddleReturn(Vector2 position){PaddleReturns++;Score.Return();Paddle.Hit();VFX.Burst(position,Art.Cyan,5);Audio.Play("paddle",.7f,true);}
        public void WallHit(Vector2 position){VFX.Burst(position,Art.Cyan,2);Audio.Play("wall",.25f,true);}
        public void ShieldReturn(Vector2 position){ShieldSaves++;VFX.Burst(position,Art.Cyan,18);Audio.Play("collect");UI.Toast("SHIELD SAVE",Art.Cyan);}
        public void Multiball()
        {
            Vector2 position=new Vector2(Paddle.X,-5.3f);Vector2 direction=Vector2.up;
            foreach(var b in Balls)if(b.Active){position=b.Position;direction=b.Direction;break;}
            int added=0;for(int i=0;i<Balls.Length&&added<2;i++)if(!Balls[i].Active){float a=(added==0?-28:28)*Mathf.Deg2Rad;Vector2 dir=new Vector2(direction.x*Mathf.Cos(a)-direction.y*Mathf.Sin(a),direction.x*Mathf.Sin(a)+direction.y*Mathf.Cos(a));Balls[i].Spawn(position+dir*.18f,dir);added++;}
        }
        public void TogglePause()
        {
            if(State==SessionState.Paused){State=beforePause;Audio.Pause(false);UI.ShowGame();}
            else if(State==SessionState.Playing||State==SessionState.Countdown){beforePause=State;State=SessionState.Paused;Audio.Pause(true);UI.ShowPause();}
        }
        void OnApplicationFocus(bool focus){if(!focus&&(State==SessionState.Playing||State==SessionState.Countdown))TogglePause();}
        [UnityEngine.Scripting.Preserve] public void BrowserHidden(){if(State==SessionState.Playing||State==SessionState.Countdown)TogglePause();}
        void OnApplicationPause(bool paused){if(paused&&(State==SessionState.Playing||State==SessionState.Countdown))TogglePause();}
        public void Finish()
        {
            if(State!=SessionState.Playing&&State!=SessionState.Countdown)return;
            State=SessionState.Results;FinalScore=GameRules.FinalScore(Score.Points,Lives,Config.lifeBonus);NewBest=SaveManager.SetBest(FinalScore);Audio.SetGameplay(false);Audio.Play(NewBest?"highscore":"gameover");UI.ShowResults();
        }
        public void SetPreferences(bool reduced,bool shake){ReducedEffects=reduced;ScreenShake=shake;SaveManager.Set("reduced",reduced?1:0);SaveManager.Set("shake",shake?1:0);}
        [System.Serializable] sealed class Snapshot {public string state;public float remaining,elapsed,fps,musicVolume,sfxVolume,paddleWidth;public int score,lives,combo,multiplier,best,bricks,wave,completed,balls,remainingBricks,finalScore,collected,paddleReturns,shieldSaves;public bool shield,qa,reduced,shake;public float paddle;public string powers;}
        void Report()
        {
            var s=new Snapshot{state=State.ToString(),remaining=Remaining,elapsed=Elapsed,score=Score.Points,lives=Lives,combo=Score.Combo,multiplier=Score.Multiplier,best=SaveManager.Best,bricks=Score.Destroyed,wave=Waves.Wave,completed=Waves.Completed,balls=ActiveBalls,remainingBricks=Waves.Remaining,finalScore=FinalScore,shield=Powers.Shield,qa=QA,paddle=Paddle.X,fps=1/Time.unscaledDeltaTime,powers=string.Join(",",Powers.Durations),collected=Powers.Collected,paddleReturns=PaddleReturns,shieldSaves=ShieldSaves,musicVolume=Audio.MusicVolume,sfxVolume=Audio.SfxVolume,reduced=ReducedEffects,shake=ScreenShake,paddleWidth=Paddle.Width};
#if UNITY_WEBGL && !UNITY_EDITOR
            BrickRushReport(JsonUtility.ToJson(s));
#endif
        }
        // Optional query-gated test surface; normal runs never enable automation.
        [UnityEngine.Scripting.Preserve] public void QACommand(string command)
        {
            if(!QA)return;
            if(command=="auto")AutoPaddle=true;
            else if(command=="manual")AutoPaddle=false;
            else if(command=="expire"){Remaining=.01f;}
            else if(command=="lose"){foreach(var b in Balls)b.Hide();}
            else if(command=="wave"){foreach(var b in Waves.Bricks)if(b.Alive&&b.Kind!=BrickKind.Obstacle)HitBrick(b,b.Position,true);}
            else if(command.StartsWith("power:")){if(int.TryParse(command.Substring(6),out int n)&&n>=0&&n<7)Powers.Activate((PowerKind)n);}
            else if(command.StartsWith("drop:")){if(int.TryParse(command.Substring(5),out int n)&&n>=0&&n<7)Powers.DropForValidation((PowerKind)n);}
            else if(command=="shield-test"){foreach(var b in Balls)b.Hide();Balls[0].Spawn(new Vector2(0,-6.75f),new Vector2(.4f,-1));}
            else if(command=="paddle-test"){foreach(var b in Balls)b.Hide();Balls[0].Spawn(new Vector2(Paddle.X,-5),new Vector2(.03f,-1));}
            else if(command=="chain"){foreach(var b in Waves.Bricks)if(b.Alive&&b.Kind==BrickKind.Explosive){HitBrick(b,b.Position,true);break;}}
            else if(command=="late"){Remaining=38;}
            else if(command=="pause")TogglePause();
            else if(command=="start")StartRun();
            else if(command=="menu")ShowMenu();
        }
    }
}
