using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackyardDojo
{
    public sealed class FruitNinjaGame : MonoBehaviour
    {
        public enum GameMode { Classic, Arcade, Zen }
        enum ScreenState { Loading, Menu, Playing, Paused, Results }
        sealed class Fruit
        {
            public RectTransform rect;
            public FruitSliceGraphic graphic;
            public Vector2 p,v;
            public float radius,spin,angle,trail,cutCooldown,life=8;
            public int kind,half;
            public List<Vector2> hitShape;
            public bool power;
        }
        sealed class Particle
        {
            public RectTransform rect;
            public Graphic graphic;
            public Vector2 p,v;
            public float life,maxLife,spin;
            public Vector2 size;
            public Color tint;
            public float endScale=1,drag,wobble,phase,fadeIn;
            public bool gravity;
        }
        sealed class FloatText { public RectTransform rect; public Text text; public Vector2 p; public float age; }
        sealed class Bob { public RectTransform rect; public Vector2 start; public float phase; }

        public static FruitNinjaGame Instance { get; private set; }
        public int Score => score;
        public int Misses => misses;
        public int ActiveFruitCount => fruits.FindAll(f=>f.half==0).Count;
        public string CurrentState => state.ToString();
        public GameMode Mode => mode;
        public int RoundCombo => roundCombo;
        public bool IsReady => state!=ScreenState.Loading && loadingLayer==null;

        static readonly Color Ink=Hex("3C2B1B"),Cream=Hex("FFF0BE"),Paper=Hex("D6AF70"),Green=Hex("83AE35");
        readonly Color[] juice={Hex("E94F5E"),Hex("FFB52E"),Hex("F34F38"),Hex("FFE56C"),Hex("F7CF43"),Hex("AD76E6"),Hex("FF5D63"),Hex("EEAC3E")};
        readonly Color[] blades={Hex("9BFFE0"),Hex("FFC267"),Hex("CEABFF")};
        readonly List<Fruit> fruits=new List<Fruit>();
        readonly List<Particle> particles=new List<Particle>();
        readonly List<FloatText> floats=new List<FloatText>();
        readonly List<Bob> bobs=new List<Bob>();
        readonly Queue<int> launchLanes=new Queue<int>();
        static readonly int[][] LanePatterns={
            new[]{1,1,0,2},new[]{0,1,2,1},new[]{2,2,1,0},new[]{0,0,2,1},
            new[]{1,0,0,2},new[]{2,1,1,0},new[]{0,2,1,1},new[]{1,2,0,1} };
        int previousPattern=-1;
        bool openingLaunches=true,pendingBomb;
        RectTransform frame,root,menu,gameLayer,fruitLayer,effectLayer,hud,modal,bladeLayer,ambientLayer,loadingLayer,loadingFill,loadingSpinner;
        CanvasGroup loadingGroup;
        RawImage orchardBackground;
        BladeGraphic blade;
        Font heading,body;
        Texture2D orchard,atlas,paperNoise,logo;
        Text loadingStatus,loadingPercent;
        Text scoreText,bestText,timeText,modeText,comboText,missText,noticeText;
        Image flash;
        AudioSource sfx,music;
        AudioClip sliceClip,bombClip,clickClip,whooshClip,comboClip;
        ScreenState state;
        GameMode mode;
        int score,misses,totalFruit,roundFruit,roundCombo,strokeCount,bladeIndex;
        float clock,elapsed,spawnTimer,countdown,comboWindow,strokeLastTime,shake,freeze,frenzy,ambientTimer,moteTimer,bladeSparkTimer;
        const int ParticleBudget=360;
        const int FragmentBudget=96;
        public const float StageWidth=1953,StageHeight=900,StageInset=(StageWidth-1600)*.5f;
        float uiScale=1;
        bool soundOn,musicOn,hadPointer,verification,landscapeBlocked;
        Vector2 lastPointer;
        Vector2 stageCenter;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindFirstObjectByType<FruitNinjaGame>()==null) new GameObject("Juice Dojo").AddComponent<FruitNinjaGame>();
        }

        void Awake()
        {
            Instance=this;
            Application.targetFrameRate=120;
            QualitySettings.vSyncCount=1;
            heading=Resources.Load<Font>("Fonts/LilitaOne");body=Resources.Load<Font>("Fonts/Lato");
            logo=Resources.Load<Texture2D>("Art/JuiceDojoLogo");
            soundOn=PlayerPrefs.GetInt("Dojo.Sound",1)==1;musicOn=PlayerPrefs.GetInt("Dojo.Music",1)==1;
            bladeIndex=PlayerPrefs.GetInt("Dojo.Blade",0);totalFruit=PlayerPrefs.GetInt("Dojo.TotalFruit",0);
            paperNoise=MakePaper();
            SetupCanvas();StartCoroutine(LoadGame());
        }
        void OnDestroy() { if(Instance==this) Instance=null; }
        void OnApplicationFocus(bool focus) { if(!focus && state==ScreenState.Playing && countdown<=0) Pause(); }
        void OnApplicationQuit() { SaveHarvest(); }
        void SaveHarvest()
        {
            if(verification) return;
            PlayerPrefs.SetInt("Dojo.TotalFruit",totalFruit);PlayerPrefs.Save();
        }

        void SetupCanvas()
        {
            var camera=Camera.main;
            if(camera!=null) { camera.backgroundColor=Hex("10291F");camera.clearFlags=CameraClearFlags.SolidColor; }
            var canvasObject=new GameObject("Backyard Dojo UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var c=canvasObject.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=10;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;
            frame=Rect(canvasObject.transform,"2.17 to 1 play area",0,0,StageWidth,StageHeight);
            frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
            frame.gameObject.AddComponent<RectMask2D>();
            root=Rect(frame,"Centered interface",0,0,1600,900);
            root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            orchardBackground=Raw(root,"Orchard",-StageInset,0,StageWidth,900,Texture2D.whiteTexture);orchardBackground.color=Hex("183D2C");
            Fill(root,"Garden shade",-StageInset,0,StageWidth,900,new Color(.02f,.08f,.045f,.13f));
            ambientLayer=Rect(root,"Garden fireflies",0,0,1600,900);
            gameLayer=Rect(root,"Game",0,0,1600,900);
            fruitLayer=Rect(gameLayer,"Flying fruit",0,0,1600,900);
            effectLayer=Rect(gameLayer,"Juice & sparks",0,0,1600,900);
            hud=Rect(gameLayer,"HUD",0,0,1600,900);
            menu=Rect(root,"Menu",0,0,1600,900);
            bladeLayer=Rect(root,"Blade layer",0,0,1600,900);
            blade=bladeLayer.gameObject.AddComponent<BladeGraphic>();blade.raycastTarget=false;blade.bladeColor=blades[bladeIndex];
            flash=Fill(root,"Flash",-StageInset,0,StageWidth,900,Color.clear);flash.raycastTarget=false;
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Resize();
        }

        System.Collections.IEnumerator LoadGame()
        {
            state=ScreenState.Loading;gameLayer.gameObject.SetActive(false);menu.gameObject.SetActive(false);
            ShowLoadingScreen();float start=Time.realtimeSinceStartup;
            yield return null;SetLoadingProgress(.08f,"PICKING A FRESH BACKDROP...");
            var garden=Resources.LoadAsync<Texture2D>("Art/Orchard");
            while(!garden.isDone) {SetLoadingProgress(.08f+garden.progress*.35f,"OPENING THE GARDEN...");yield return null;}
            orchard=garden.asset as Texture2D;orchardBackground.texture=orchard;orchardBackground.color=Color.white;
            // Cover the wider stage without stretching the painted orchard.
            float cropHeight=(orchard.width/(float)orchard.height)/(StageWidth/StageHeight);
            orchardBackground.uvRect=new Rect(0,(1-cropHeight)*.5f,1,cropHeight);
            SetLoadingProgress(.45f,"GATHERING THE JUICIEST FRUIT...");yield return null;
            var fruit=Resources.LoadAsync<Texture2D>("Art/FruitAtlas");
            while(!fruit.isDone) {SetLoadingProgress(.45f+fruit.progress*.35f,"GATHERING THE JUICIEST FRUIT...");yield return null;}
            atlas=fruit.asset as Texture2D;SetLoadingProgress(.84f,"TUNING THE GARDEN MELODY...");yield return null;
            SetupAudio();SetLoadingProgress(1,"FRESH FRUIT. SHARP BLADES. LET'S GO!");
            while(Time.realtimeSinceStartup-start<1.6f)yield return null;
            ShowMenu();
            while(loadingGroup!=null && loadingGroup.alpha>0)
            {loadingGroup.alpha=Mathf.Max(0,loadingGroup.alpha-Time.unscaledDeltaTime*3.5f);yield return null;}
            DismissLoading();
        }
        void ShowLoadingScreen()
        {
            DismissLoading();loadingLayer=Rect(root,"Loading screen",0,0,1600,900);
            loadingGroup=loadingLayer.gameObject.AddComponent<CanvasGroup>();loadingGroup.blocksRaycasts=true;
            var backdrop=Fill(loadingLayer,"Garden veil",-StageInset,0,StageWidth,900,new Color(.025f,.095f,.065f,.9f));backdrop.raycastTarget=true;
            Label(loadingLayer,"Dojo name","WELCOME TO THE DOJO",480,130,640,39,21,Hex("D7CA8C"));
            CreateLogo(loadingLayer,420,184,760,300);
            Label(loadingLayer,"Tagline","A LITTLE FOCUS. A LOT OF JUICE.",430,498,740,38,23,Cream);
            var card=Board(loadingLayer,"Loading card",510,578,580,119,Paper,-.8f,true);Tape(card,240,-11,99,27,3);
            loadingStatus=Label(card,"Loading stage","OPENING THE GARDEN...",22,15,536,33,21,Ink);
            Shape(card,"Progress track",27,63,470,19,Ink);
            loadingFill=Shape(card,"Fresh green progress",30,66,0,13,Green).rectTransform;
            loadingFill.pivot=new Vector2(0,.5f);loadingFill.anchoredPosition=new Vector2(30,-72.5f);
            loadingPercent=Label(card,"Percentage","0%",501,54,61,35,20,Ink);
            var spinner=Rect(loadingLayer,"Citrus spinner",777,739,46,46);loadingSpinner=spinner;
            var ring=spinner.gameObject.AddComponent<EffectGraphic>();ring.form=EffectGraphic.Form.Arc;ring.color=Hex("DDBE64");ring.raycastTarget=false;
            Circle(spinner,"Citrus core",23,23,10,Hex("A2CD52"));
            Label(loadingLayer,"Tip","DOJO TIP: Slice three fruit in one stroke for a combo bonus.",360,806,880,38,18,Hex("DDD3AF"),TextAnchor.MiddleCenter,false);
        }
        void SetLoadingProgress(float progress,string status)
        {
            if(loadingFill==null)return;
            loadingFill.sizeDelta=new Vector2(464*Mathf.Clamp01(progress),13);
            loadingStatus.text=status;loadingPercent.text=Mathf.RoundToInt(progress*100)+"%";
        }
        void DismissLoading()
        {if(loadingLayer!=null)Destroy(loadingLayer.gameObject);loadingLayer=null;loadingGroup=null;loadingSpinner=null;}
        void CreateLogo(Transform parent,float x,float y,float w,float h)
        {
            var container=Rect(parent,"Juice Dojo logo",x,y,w,h);
            if(logo!=null)
            {
                float aspect=logo.width/(float)logo.height;
                float drawW=Mathf.Min(w,h*aspect),drawH=drawW/aspect;
                Raw(container,"Painted logo",(w-drawW)*.5f,(h-drawH)*.5f,drawW,drawH,logo);
            }
            else Label(container,"Logo fallback","JUICE DOJO",0,0,w,h,96,Cream,TextAnchor.MiddleCenter,true,4);
        }

        void Update()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.04f);Resize();
#if UNITY_WEBGL && !UNITY_EDITOR
            // In portrait the web template hides the canvas and sizes it to the viewport.
            bool portrait=Screen.width<=Screen.height;
            if(portrait!=landscapeBlocked)SetLandscapeBlocked(portrait);
#endif
            if(landscapeBlocked)return;
            blade.Tick(dt);
            bladeSparkTimer-=dt;
            if(loadingSpinner!=null)loadingSpinner.Rotate(0,0,-160*Time.unscaledDeltaTime);
            if(flash.color.a>0) { Color c=flash.color;c.a=Mathf.Max(0,c.a-dt*1.7f);flash.color=c; }
            if(shake>0) { shake-=dt;root.anchoredPosition=UnityEngine.Random.insideUnitCircle*shake*15; }
            else root.anchoredPosition=Vector2.zero;
            foreach(var b in bobs) if(b.rect!=null) b.rect.anchoredPosition=b.start+new Vector2(0,Mathf.Sin(Time.unscaledTime*1.6f+b.phase)*6);
            if(state!=ScreenState.Loading && loadingGroup==null) {HandleKeyboard();HandlePointer();}
            TickRound(dt);
            if(state!=ScreenState.Paused) TickEffects(dt);
            ambientTimer-=dt;
            if(ambientTimer<=0 && state==ScreenState.Menu) { SpawnLeaf();ambientTimer=UnityEngine.Random.Range(.6f,1.6f); }
            moteTimer-=dt;
            if(moteTimer<=0 && state!=ScreenState.Paused && state!=ScreenState.Results)
            {SpawnFirefly();moteTimer=UnityEngine.Random.Range(.18f,.4f);}
        }

        void SetLandscapeBlocked(bool blocked)
        {
            landscapeBlocked=blocked;
            if(!landscapeBlocked)return;
            if(hadPointer && state==ScreenState.Playing)FinishStroke();
            hadPointer=false;blade.Clear();
        }

        void TickRound(float dt)
        {
            if(state==ScreenState.Playing)
            {
                if(countdown>0)
                {
                    countdown-=dt;
                    noticeText.text=countdown>2?"READY?":countdown>1?"STEADY...":"SLICE!";
                    noticeText.transform.localScale=Vector3.one*(1+Mathf.Sin(countdown*Mathf.PI)*.05f);
                    if(countdown<=0) { noticeText.text="";spawnTimer=.05f;Play(comboClip,.45f); }
                }
                else
                {
                    elapsed+=dt;
                    if(mode!=GameMode.Classic) { clock=Mathf.Max(0,clock-dt*(freeze>0?.5f:1));if(clock<=0) EndRound("Time's up. What a harvest!"); }
                    if(state==ScreenState.Playing)
                    {
                        freeze=Mathf.Max(0,freeze-dt);frenzy=Mathf.Max(0,frenzy-dt);
                        float speed=freeze>0?.5f:1;
                        spawnTimer-=dt*speed;
                        if(spawnTimer<=0)SpawnNextFruit();
                        TickFruit(dt*speed);
                        if(comboWindow>0) { comboWindow-=dt;if(comboWindow<=0) FinishStroke(); }
                        UpdateHud();
                    }
                }
            }
        }

        void Resize()
        {
            var safe=Screen.safeArea;
            if(safe.width<=0 || safe.height<=0)safe=new Rect(0,0,Screen.width,Screen.height);
            stageCenter=safe.center;
            uiScale=Mathf.Min(safe.width/StageWidth,safe.height/StageHeight);
            if(frame!=null)
            {
                frame.localScale=Vector3.one*uiScale;
                frame.anchoredPosition=stageCenter-new Vector2(Screen.width*.5f,Screen.height*.5f);
            }
        }
        void HandleKeyboard()
        {
            var k=Keyboard.current;if(k==null) return;
            if(k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame)
            {
                if(state==ScreenState.Playing) Pause();else if(state==ScreenState.Paused) Resume();
                else if(modal!=null) CloseModal();
            }
            if(k.spaceKey.wasPressedThisFrame && state==ScreenState.Paused) Resume();
            if(k.rKey.wasPressedThisFrame && state==ScreenState.Results) StartRound(mode);
        }
        void HandlePointer()
        {
            Vector2 screen=Vector2.zero;bool held=false;
            if(Touchscreen.current!=null && Touchscreen.current.primaryTouch.press.isPressed)
            { screen=Touchscreen.current.primaryTouch.position.ReadValue();held=true; }
            else if(Mouse.current!=null) { screen=Mouse.current.position.ReadValue();held=Mouse.current.leftButton.isPressed; }
            Vector2 p=new Vector2((screen.x-stageCenter.x)/uiScale+800,450-(screen.y-stageCenter.y)/uiScale);
            if(held && p.x>=-StageInset && p.x<=1600+StageInset && p.y>=0 && p.y<=900)
            {
                if(!hadPointer) { lastPointer=p;blade.Clear();strokeCount=0; }
                Vector2 delta=p-lastPointer;
                if(delta.sqrMagnitude>5)
                {
                    blade.Add(new Vector2(p.x-800,450-p.y));
                    if(state==ScreenState.Playing && countdown<=0 && p.y>138 && lastPointer.y>138)
                    {
                        if(bladeSparkTimer<=0) {BladeSpark(p,delta.normalized);bladeSparkTimer=.025f;}
                        if(delta.sqrMagnitude>4500 && Time.unscaledTime-strokeLastTime>.13f) { Play(whooshClip,.15f);strokeLastTime=Time.unscaledTime; }
                        SliceSegment(lastPointer,p);
                    }
                    else if(state==ScreenState.Menu && modal==null && hadPointer && delta.magnitude>12)
                    {
                        for(int i=0;i<3;i++) if(DistanceToSegment(new Vector2(410+i*390,501),lastPointer,p)<65)
                        { StartRound((GameMode)i);break; }
                    }
                }
                lastPointer=p;hadPointer=true;
            }
            else
            {
                if(hadPointer && state==ScreenState.Playing) FinishStroke();
                hadPointer=false;
            }
        }

        public void ShowMenu()
        {
            if(state==ScreenState.Playing || state==ScreenState.Paused) SaveHarvest();
            ClearRound();CloseModal();state=ScreenState.Menu;
            gameLayer.gameObject.SetActive(false);menu.gameObject.SetActive(true);ClearChildren(menu);bobs.Clear();
            // Garden station header and hand-cut title.
            var brand=Rect(menu,"Dojo stamp",64,38,310,38);
            var leaf=Circle(brand,"Leaf",13,14,22,Green);leaf.rectTransform.sizeDelta=new Vector2(25,13);leaf.rectTransform.localEulerAngles=new Vector3(0,0,32);
            Label(brand,"station","JUICE DOJO",38,0,275,34,19,Cream,TextAnchor.MiddleLeft,false);
            SmallButton(menu,"?",1312,24,114,105,ShowHelp);
            SmallButton(menu,soundOn?"SFX":"OFF",1443,24,130,105,()=>{ soundOn=!soundOn;SaveSettings();ShowMenu(); });
            CreateLogo(menu,395,17,810,290);
            var strip=Board(menu,"Choose a mode",590,316,420,51,Hex("EFCD8D"),1.2f,true);
            Label(strip,"Choose","PICK YOUR CHALLENGE",15,4,390,41,25,Ink);
            CreateModeCard(0,245,390,-2.3f,"CLASSIC","THE ORIGINAL",0,"3 misses. One sharp blade.","Watch out for bombs!",Hex("759D37"));
            CreateModeCard(1,635,390,1.2f,"ARCADE","FAST & FRUITY",1,"60 seconds. Make it juicy.","Combos, frenzy & freeze.",Hex("C8782B"));
            CreateModeCard(2,1025,390,-1.5f,"ZEN","FIND YOUR FLOW",5,"90 seconds. Just fruit.","No bombs. No worries.",Hex("6C8D77"));
            var drawer=Board(menu,"Workbench drawer",424,788,752,93,Hex("B68B50"),-.8f,false);
            TextButton(drawer,"YOUR HARVEST",16,5,230,80,ShowStats);
            TextButton(drawer,"BLADES",260,5,230,80,ShowBlades);
            TextButton(drawer,"SETTINGS",504,5,230,80,ShowSettings);
            Label(menu,"Edition","FRESHLY PICKED\nBACKYARD EDITION",70,811,240,57,17,Hex("FFF0C0"),TextAnchor.MiddleLeft,false);
            Label(menu,"Fruit counter",totalFruit+" FRUIT SLICED",1275,823,250,35,18,Hex("FFF0C0"),TextAnchor.MiddleRight,false);
        }

        void CreateModeCard(int index,float x,float y,float angle,string name,string kicker,int fruit,string line1,string line2,Color accent)
        {
            var card=Board(menu,name+" mode card",x,y,330,330,Paper,angle,true);
            Tape(card,119,-11,92,27,-7);
            var top=Board(card,"Painted label",20,20,290,39,accent,0,false);
            Label(top,"Mode tag",kicker,4,1,282,35,17,Cream);
            FruitIcon(card,fruit,165,111,122,-7,true);
            Label(card,"Mode name",name,20,161,290,55,48,Ink);
            Label(card,"Description",line1,18,217,294,29,18,Ink,TextAnchor.MiddleCenter,false);
            Label(card,"Rule",line2,18,244,294,25,16,Hex("705532"),TextAnchor.MiddleCenter,false);
            var play=Board(card,"Start strip",26,274,278,46,accent,0,false);
            Label(play,"Play","SLICE TO START  >",4,0,270,42,23,Cream);
            var target=card.gameObject.AddComponent<PaperButton>();target.action=()=>StartRound((GameMode)index);target.hoverSound=()=>Play(clickClip,.1f);
        }

        public void StartRound(GameMode chosen)
        {
            CloseModal();ClearRound();mode=chosen;state=ScreenState.Playing;
            menu.gameObject.SetActive(false);gameLayer.gameObject.SetActive(true);
            score=0;misses=0;roundFruit=0;roundCombo=0;elapsed=0;freeze=frenzy=0;strokeCount=0;comboWindow=0;
            clock=mode==GameMode.Arcade?60:90;countdown=2.7f;spawnTimer=.1f;
            ClearChildren(hud);CreateHud();UpdateHud();Play(clickClip,.5f);
        }
        void CreateHud()
        {
            var scoreBoard=Board(hud,"Score sign",45,31,276,115,Hex("D8B474"),-2,true);
            Label(scoreBoard,"Score caption","HARVEST",19,8,120,29,17,Hex("6B4D29"),TextAnchor.MiddleLeft);
            scoreText=Label(scoreBoard,"Score","0",18,33,170,65,61,Ink,TextAnchor.MiddleLeft);
            bestText=Label(scoreBoard,"Best score","BEST  0",148,51,112,34,16,Hex("765632"),TextAnchor.MiddleRight,false);
            var center=Board(hud,"Mode sign",629,26,342,65,Hex("6A4729"),1,false);
            modeText=Label(center,"Mode",mode.ToString().ToUpper()+" MODE",8,5,326,50,29,Cream);
            Label(hud,"Instructions",mode==GameMode.Classic?"SLICE FRUIT · AVOID BOMBS · DON'T MISS 3":mode==GameMode.Arcade?"CHASE COMBOS · CATCH THE GLOWING FRUIT":"TAKE A BREATH. FIND YOUR FLOW.",450,101,700,28,16,Cream);
            var status=Board(hud,"Round status",1181,32,240,86,Hex("D8B474"),1.4f,true);
            timeText=Label(status,"Time",mode==GameMode.Classic?"MISSES":"TIME LEFT",10,4,220,27,16,Hex("795A32"));
            missText=Label(status,"Status","",12,30,216,45,36,Ink);
            SmallButton(hud,"II",1440,20,137,116,Pause);
            comboText=Label(hud,"Combo status","",500,166,600,42,27,Hex("FFE08A"));
            noticeText=Label(hud,"Countdown","",350,319,900,160,99,Cream,TextAnchor.MiddleCenter,true,5);
        }
        void UpdateHud()
        {
            scoreText.text=score.ToString();bestText.text="BEST  "+PlayerPrefs.GetInt("Dojo.Best."+mode,0);
            if(mode==GameMode.Classic) { missText.text=(misses>0?"X":"_")+"  "+(misses>1?"X":"_")+"  "+(misses>2?"X":"_");missText.color=misses>0?Hex("B53F30"):Ink; }
            else { missText.text=Mathf.CeilToInt(clock).ToString()+"s";missText.color=clock<10?Hex("B53F30"):Ink; }
            comboText.text=freeze>0?"FROZEN TIME  "+freeze.ToString("0.0")+"s":frenzy>0?"FRUIT FRENZY!  "+frenzy.ToString("0.0")+"s":"";
        }

        void PlanLaunches()
        {
            if(openingLaunches)
            {
                launchLanes.Enqueue(1);launchLanes.Enqueue(1);
                int side=UnityEngine.Random.value<.5f?0:2;
                launchLanes.Enqueue(side);launchLanes.Enqueue(2-side);openingLaunches=false;
            }
            else
            {
                int pattern=UnityEngine.Random.Range(0,previousPattern<0?LanePatterns.Length:LanePatterns.Length-1);
                if(previousPattern>=0 && pattern>=previousPattern)pattern++;
                previousPattern=pattern;
                bool mirror=UnityEngine.Random.value<.5f;
                foreach(int lane in LanePatterns[pattern])launchLanes.Enqueue(mirror?2-lane:lane);
            }
            pendingBomb=mode!=GameMode.Zen && elapsed>3 && UnityEngine.Random.value<(mode==GameMode.Classic?.34f:.27f);
        }
        void SpawnNextFruit()
        {
            // One launch per beat, including bombs. Never release a whole row in one frame.
            if(launchLanes.Count==0 && pendingBomb)
            {
                pendingBomb=false;
                SpawnFruit(7,new Vector2(UnityEngine.Random.Range(280,1320),1020),new Vector2(UnityEngine.Random.Range(-110,110),-UnityEngine.Random.Range(1460,1530)));
                spawnTimer=UnityEngine.Random.Range(.38f,.64f);return;
            }
            if(launchLanes.Count==0)PlanLaunches();
            int laneIndex=launchLanes.Dequeue();
            float x=290+laneIndex*510+UnityEngine.Random.Range(-115,115);
            bool power=mode==GameMode.Arcade && elapsed>5 && UnityEngine.Random.value<.095f;
            SpawnFruit(UnityEngine.Random.Range(0,7),new Vector2(x,965+UnityEngine.Random.Range(0,50)),
                new Vector2((800-x)*.1f+UnityEngine.Random.Range(-35,35),UnityEngine.Random.Range(-1590,-1510)),power);
            bool pair=launchLanes.Count>0 && launchLanes.Peek()==laneIndex;
            spawnTimer=(pair?UnityEngine.Random.Range(.18f,.28f):UnityEngine.Random.Range(.44f,.78f))*
                (frenzy>0?.62f:Mathf.Lerp(1,.85f,Mathf.Clamp01(elapsed/90)));
        }
        Fruit SpawnFruit(int kind,Vector2 p,Vector2 velocity,bool power=false)
        {
            float size=kind==4?158:kind==3?145:kind==7?135:145;
            var r=Rect(fruitLayer,kind==7?"Bomb":"Fruit "+kind,p.x-size/2,p.y-size/2,size,size);
            var graphic=r.gameObject.AddComponent<FruitSliceGraphic>();graphic.atlas=atlas;graphic.uv=FruitUV(kind);graphic.raycastTarget=false;
            if(power) { var glow=Circle(r,"Power fruit glow",size*.5f,size*.5f,size*.94f,new Color(.5f,1,.9f,.22f));glow.transform.SetAsFirstSibling(); }
            var f=new Fruit {rect=r,graphic=graphic,p=p,v=velocity,radius=size*.35f,kind=kind,spin=UnityEngine.Random.Range(-100,100),angle=UnityEngine.Random.Range(-30,30),power=power,hitShape=FruitSliceGraphic.RoundHitShape()};
            fruits.Add(f);return f;
        }
        void TickFruit(float dt)
        {
            for(int i=fruits.Count-1;i>=0;i--)
            {
                Fruit f=fruits[i];f.v.y+=(f.half==0?1650:850)*dt;f.p+=f.v*dt;f.angle+=f.spin*dt;f.life-=dt;f.cutCooldown=Mathf.Max(0,f.cutCooldown-dt);
                f.rect.anchoredPosition=new Vector2(f.p.x,-f.p.y);f.rect.localEulerAngles=new Vector3(0,0,f.angle);
                if(f.half==0 && (f.power || f.kind==7) && f.p.y<930)
                {
                    f.trail-=dt;
                    if(f.trail<=0)
                    {
                        f.trail=.075f;
                        Vector2 at=f.power?f.p+UnityEngine.Random.insideUnitCircle*38:f.p+new Vector2(37,-42);
                        Color tint=f.power?Hex("A6FFE3"):Hex("FFC457");
                        SpawnFx(effectLayer,EffectGraphic.Form.Spark,at,UnityEngine.Random.insideUnitCircle*60+new Vector2(0,-65),new Vector2(11,18),tint,.38f,180,false,.1f);
                        if(f.power)SpawnFx(effectLayer,EffectGraphic.Form.Glow,at,Vector2.zero,Vector2.one*40,new Color(.35f,1,.8f,.3f),.36f,0,false,1.6f);
                    }
                }
                if(f.half!=0 && f.life<.55f) { Color c=f.graphic.color;c.a=Mathf.Clamp01(f.life/.55f);f.graphic.color=c; }
                if(f.p.y>1110 || f.p.x<-250 || f.p.x>1850 || f.life<=0)
                {
                    bool missed=f.half==0 && f.kind!=7;
                    Destroy(f.rect.gameObject);fruits.RemoveAt(i);
                    if(missed && mode==GameMode.Classic)
                    {
                        misses++;Float("MISSED!",new Vector2(Mathf.Clamp(f.p.x,240,1360),735),Hex("FFBE78"),28);
                        if(misses>=3) { EndRound("Three missed fruit. Another harvest awaits!");return; }
                    }
                }
            }
        }
        public void SliceSegment(Vector2 a,Vector2 b)
        {
            // Segment tests prevent fast swipes tunnelling through fruit between frames.
            for(int i=fruits.Count-1;i>=0;i--)
            {
                Fruit f=fruits[i];if(f.cutCooldown>0 || DistanceToSegment(f.p,a,b)>f.radius) continue;
                if(f.kind==7)
                {
                    Vector2 p=f.p;Destroy(f.rect.gameObject);fruits.RemoveAt(i);HitBomb(p);
                    if(state!=ScreenState.Playing) break;
                }
                else CutFruit(f,a,b);
            }
        }
        bool CutFruit(Fruit f,Vector2 a,Vector2 b)
        {
            if(fruits.Count>=FragmentBudget || (b-a).sqrMagnitude<1)return false;
            Vector2 localA=FruitLocalPoint(f,a),localB=FruitLocalPoint(f,b);
            if(!FruitSliceGraphic.SegmentHits(f.hitShape,localA,localB))return false;
            Vector2 localDirection=(localB-localA).normalized;
            Vector2 normal=new Vector2(-localDirection.y,localDirection.x);
            float offset=Vector2.Dot(localA,normal);
            var hitA=FruitSliceGraphic.Clip(f.hitShape,normal,offset);
            var hitB=FruitSliceGraphic.Clip(f.hitShape,-normal,-offset);
            if(FruitSliceGraphic.Area(hitA)<.006f || FruitSliceGraphic.Area(hitB)<.006f)return false;
            var shape=f.graphic.polygon ?? FruitSliceGraphic.WholeShape();
            var shapeA=FruitSliceGraphic.Clip(shape,normal,offset);
            var shapeB=FruitSliceGraphic.Clip(shape,-normal,-offset);
            bool firstCut=f.half==0,wasPower=f.power;
            Vector2 direction=(b-a).normalized,n=new Vector2(direction.y,-direction.x);
            Vector2 center=Vector2.zero;foreach(var point in f.hitShape)center+=point;center/=f.hitShape.Count;
            Vector2 at=FruitWorldPoint(f,center-normal*(Vector2.Dot(center,normal)-offset));
            float life=firstCut?2.5f:Mathf.Min(2.5f,f.life+.25f),force=firstCut?120:85;
            Vector2 motion=f.v*(firstCut?.3f:.55f)+new Vector2(0,firstCut?-150:-70);
            var other=Rect(fruitLayer,"Fruit piece",f.p.x-f.rect.sizeDelta.x/2,f.p.y-f.rect.sizeDelta.y/2,f.rect.sizeDelta.x,f.rect.sizeDelta.y);
            var graphic=other.gameObject.AddComponent<FruitSliceGraphic>();graphic.atlas=atlas;graphic.uv=f.graphic.uv;graphic.polygon=shapeB;graphic.raycastTarget=false;
            fruits.Add(new Fruit {rect=other,graphic=graphic,p=f.p-n*6,v=motion-n*force,kind=f.kind,half=1,angle=f.angle,spin=f.spin+95,life=life,radius=f.radius,cutCooldown=.12f,hitShape=hitB});
            other.localEulerAngles=new Vector3(0,0,f.angle);
            f.half=1;f.power=false;f.graphic.polygon=shapeA;f.graphic.SetVerticesDirty();f.hitShape=hitA;
            f.v=motion+n*force;f.p+=n*6;f.spin-=95;f.life=life;f.cutCooldown=.12f;
            if(firstCut)
            {
                roundFruit++;totalFruit++;strokeCount++;comboWindow=.24f;
                var glow=f.rect.Find("Power fruit glow");if(glow!=null)Destroy(glow.gameObject);
            }
            score++;
            SliceBurst(at,juice[f.kind],direction,f.kind);
            bool critical=firstCut && !verification && UnityEngine.Random.value<.035f;
            if(critical) { score+=9;StarBurst(f.p,Hex("FFE287"),18);Float("CRITICAL! +10",f.p+new Vector2(0,-68),Hex("FFE287"),36); }
            if(wasPower && firstCut)
            {
                if(UnityEngine.Random.value<.5f) { freeze=5;Float("FREEZE!",f.p+new Vector2(0,-95),Hex("A0FFE6"),43); }
                else { frenzy=6;Float("FRUIT FRENZY!",f.p+new Vector2(0,-95),Hex("FFE68C"),40); }
                Play(comboClip,.5f);
                StarBurst(f.p,freeze>0?Hex("9DFFE9"):Hex("FFCD57"),26);
            }
            Play(sliceClip,UnityEngine.Random.Range(.35f,.5f),UnityEngine.Random.Range(.85f,1.3f));
            UpdateHud();
            return true;
        }
        static Vector2 RotatePoint(Vector2 point,float degrees)
        {
            float angle=degrees*Mathf.Deg2Rad,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
            return new Vector2(point.x*c-point.y*s,point.x*s+point.y*c);
        }
        static Vector2 FruitLocalPoint(Fruit fruit,Vector2 world) =>
            RotatePoint(new Vector2(world.x-fruit.p.x,fruit.p.y-world.y),-fruit.angle)/fruit.rect.sizeDelta.x;
        static Vector2 FruitWorldPoint(Fruit fruit,Vector2 local)
        {
            Vector2 point=RotatePoint(local*fruit.rect.sizeDelta.x,fruit.angle);
            return fruit.p+new Vector2(point.x,-point.y);
        }
        void FinishStroke()
        {
            if(strokeCount>=3)
            {
                int bonus=strokeCount*strokeCount;score+=bonus;roundCombo=Mathf.Max(roundCombo,strokeCount);
                ComboBurst(new Vector2(800,350));
                Float(strokeCount+" FRUIT COMBO!",new Vector2(800,330),Hex("FFE586"),48);
                Float("+"+bonus+" BONUS",new Vector2(800,385),Cream,28);Play(comboClip,.45f,1+strokeCount*.045f);
            }
            strokeCount=0;comboWindow=0;
        }
        void HitBomb(Vector2 p)
        {
            FinishStroke();BombBurst(p);
            flash.color=new Color(1,.54f,.24f,.55f);shake=.55f;Play(bombClip,.8f);
            if(mode==GameMode.Classic) EndRound("A sneaky bomb ended your harvest!");
            else { score=Mathf.Max(0,score-10);Float("BOMB! -10",p,Hex("FFAF77"),44); }
        }

        public void Pause()
        {
            if(state!=ScreenState.Playing) return;
            state=ScreenState.Paused;blade.Clear();Play(clickClip,.4f);
            var panel=OpenModal("TAKE A BREATHER",660,462);
            Label(panel,"Message","Your fruit will wait for you.",45,116,570,44,24,Ink,TextAnchor.MiddleCenter,false);
            ActionButton(panel,"KEEP SLICING",78,184,504,91,Green,Resume);
            ActionButton(panel,"START FRESH",78,300,242,87,Paper,()=>StartRound(mode));
            ActionButton(panel,"MAIN MENU",340,300,242,87,Paper,ShowMenu);
        }
        public void Resume()
        {
            if(state!=ScreenState.Paused) return;
            CloseModal();state=ScreenState.Playing;hadPointer=false;Play(clickClip,.35f);
        }
        void EndRound(string reason)
        {
            if(state!=ScreenState.Playing) return;
            FinishStroke();state=ScreenState.Results;blade.Clear();
            int previous=PlayerPrefs.GetInt("Dojo.Best."+mode,0);bool record=score>previous;
            if(!verification)
            {
                PlayerPrefs.SetInt("Dojo.Best."+mode,Mathf.Max(previous,score));
                PlayerPrefs.SetInt("Dojo.TotalFruit",totalFruit);
                PlayerPrefs.SetInt("Dojo.Combo",Mathf.Max(PlayerPrefs.GetInt("Dojo.Combo",0),roundCombo));PlayerPrefs.Save();
            }
            var panel=OpenModal(record?"A FRESH NEW RECORD!":"NICE SLICING!",680,655);
            Label(panel,"Mode",mode.ToString().ToUpper()+" HARVEST",80,98,520,36,21,Hex("76562E"));
            Label(panel,"Final score",score.ToString(),70,136,540,136,118,Ink);
            Label(panel,"Points","JUICY POINTS",80,266,520,35,20,Hex("76562E"));
            var row=Board(panel,"Stats strip",55,318,570,70,Hex("C3995D"),0,false);
            Label(row,"Fruit",roundFruit+" FRUIT SLICED",15,7,260,50,22,Ink);
            Label(row,"Combo",roundCombo+" BEST COMBO",285,7,270,50,22,Ink);
            Label(panel,"Reason",reason,42,402,596,49,20,Ink,TextAnchor.MiddleCenter,false);
            ActionButton(panel,"SLICE AGAIN  >",95,467,490,84,Green,()=>StartRound(mode));
            TextButton(panel,"BACK TO THE GARDEN",120,566,440,72,ShowMenu);
            Play(comboClip,.45f);
        }

        void ShowHelp()
        {
            var p=OpenModal("A LITTLE DOJO KNOW-HOW",760,590);
            HelpRow(p,1,127,"SWIPE. SLICE. SPLASH.","Swipe through airborne fruit to slice it.");
            HelpRow(p,2,229,"MORE FRUIT. BIGGER COMBOS.","Slice 3 or more in one stroke for a juicy bonus.");
            HelpRow(p,3,331,"THOSE BOMBS ARE TROUBLE.","Classic: game over. Arcade: lose 10 points. Zen: relax!");
            Label(p,"Tip","TIP: Glowing Arcade fruit brings freeze or a fruit frenzy.",45,438,670,40,19,Hex("70502B"),TextAnchor.MiddleCenter,false);
            ActionButton(p,"GOT IT!",220,485,320,86,Green,CloseModal);
        }
        void HelpRow(RectTransform p,int n,float y,string title,string text)
        {
            Circle(p,"Step",75,y+22,49,Hex("7D9E39"));Label(p,"Number",n.ToString(),51,y-3,48,48,30,Cream);
            Label(p,"Heading",title,122,y-5,590,37,26,Ink,TextAnchor.MiddleLeft);
            Label(p,"Description",text,122,y+33,590,34,19,Ink,TextAnchor.MiddleLeft,false);
        }
        void ShowStats()
        {
            var p=OpenModal("YOUR HARVEST",720,580);
            string[] names={"CLASSIC","ARCADE","ZEN"};
            for(int i=0;i<3;i++)
            {
                var row=Board(p,"High score "+names[i],55,120+i*85,610,69,Hex("CBA065"),i%2==0?.5f:-.5f,false);
                Label(row,"Mode",names[i],23,10,330,45,29,Ink,TextAnchor.MiddleLeft);
                Label(row,"Best",PlayerPrefs.GetInt("Dojo.Best."+(GameMode)i,0)+" pts",350,11,230,43,29,Ink,TextAnchor.MiddleRight);
            }
            Label(p,"Lifetime",totalFruit+" FRUIT SLICED   ·   "+PlayerPrefs.GetInt("Dojo.Combo",0)+" BEST COMBO",50,398,620,41,23,Ink);
            ActionButton(p,"BACK TO THE GARDEN",132,469,456,86,Green,CloseModal);
        }
        void ShowBlades()
        {
            var p=OpenModal("PICK YOUR BLADE",750,490);
            string[] names={"MINT EDGE","GOLDEN HOUR","VIOLET SWISH"};
            for(int i=0;i<3;i++)
            {
                int pick=i;var card=Board(p,"Blade "+i,55+i*224,132,196,214,i==bladeIndex?Hex("8FA94F"):Hex("CFA96D"),i-1,true);
                var edge=Fill(card,"Blade",91,27,12,91,blades[i]);edge.rectTransform.localEulerAngles=new Vector3(0,0,-25);
                Label(card,"Blade name",names[i],10,129,176,35,20,Ink);
                Label(card,"Select",i==bladeIndex?"EQUIPPED":"SELECT",10,169,176,29,17,Ink);
                var b=card.gameObject.AddComponent<PaperButton>();b.action=()=>{ bladeIndex=pick;blade.bladeColor=blades[pick];SaveSettings();ShowBlades();Play(sliceClip,.3f); };
            }
            ActionButton(p,"LOOKING SHARP!",170,381,410,86,Green,CloseModal);
        }
        void ShowSettings()
        {
            var p=OpenModal("MAKE YOURSELF AT HOME",690,500);
            Label(p,"Sound label","SPLASHES & SWISHES",60,140,390,44,25,Ink,TextAnchor.MiddleLeft);
            ActionButton(p,soundOn?"ON":"OFF",475,117,156,88,soundOn?Green:Paper,()=>{ soundOn=!soundOn;SaveSettings();ShowSettings(); });
            Label(p,"Music label","GARDEN MELODY",60,244,390,44,25,Ink,TextAnchor.MiddleLeft);
            ActionButton(p,musicOn?"ON":"OFF",475,221,156,88,musicOn?Green:Paper,()=>{ musicOn=!musicOn;SaveSettings();ShowSettings(); });
            ActionButton(p,"ALL SET!",160,366,370,92,Green,CloseModal);
        }
        RectTransform OpenModal(string title,float w,float h)
        {
            CloseModal();modal=Rect(root,"Overlay",0,0,1600,900);
            var dim=Fill(modal,"Dim garden",-StageInset,0,StageWidth,900,new Color(.025f,.065f,.04f,.8f));dim.raycastTarget=true;
            var p=Board(modal,"Cardboard panel",800-w*.5f,450-h*.5f,w,h,Paper,-.7f,true);
            p.localScale=Vector3.one*1.18f;
            Tape(p,w*.5f-63,-12,126,33,-3);
            var titleBoard=Board(p,"Painted heading",23,29,w-46,65,Hex("6D4929"),.3f,false);
            Label(titleBoard,"Title",title,12,4,w-70,55,title.Length>22?31:37,Cream);
            return p;
        }
        void CloseModal() { if(modal!=null) Destroy(modal.gameObject);modal=null; }
        void SaveSettings()
        {
            PlayerPrefs.SetInt("Dojo.Sound",soundOn?1:0);PlayerPrefs.SetInt("Dojo.Music",musicOn?1:0);PlayerPrefs.SetInt("Dojo.Blade",bladeIndex);PlayerPrefs.Save();
            music.volume=musicOn?.13f:0;
        }

        void TickEffects(float dt)
        {
            for(int i=particles.Count-1;i>=0;i--)
            {
                var p=particles[i];p.life-=dt;
                if(p.life<=0) { if(p.rect!=null) Destroy(p.rect.gameObject);particles.RemoveAt(i);continue; }
                if(p.gravity) p.v.y+=950*dt;
                if(p.drag>0)p.v*=Mathf.Exp(-p.drag*dt);
                p.p+=p.v*dt;
                if(p.rect==null) { particles.RemoveAt(i);continue; }
                float age=1-p.life/p.maxLife;
                p.rect.anchoredPosition=new Vector2(p.p.x+Mathf.Sin(age*9+p.phase)*p.wobble,-p.p.y);
                p.rect.Rotate(0,0,p.spin*dt);p.rect.sizeDelta=p.size*Mathf.Lerp(1,p.endScale,age);
                Color c=p.tint;c.a*=Mathf.Clamp01(p.life/Mathf.Min(.55f,p.maxLife*.5f));
                if(p.fadeIn>0)c.a*=Mathf.Clamp01((p.maxLife-p.life)/p.fadeIn);
                p.graphic.color=c;
            }
            for(int i=floats.Count-1;i>=0;i--)
            {
                var f=floats[i];f.age+=dt;
                if(f.age>1.15f) { Destroy(f.rect.gameObject);floats.RemoveAt(i);continue; }
                f.rect.anchoredPosition=new Vector2(f.p.x,-f.p.y+f.age*65);
                f.rect.localScale=Vector3.one*(1+Mathf.Sin(Mathf.Min(1,f.age*5)*Mathf.PI)*.14f);
                Color c=f.text.color;c.a=Mathf.Clamp01((1.15f-f.age)*3);f.text.color=c;
            }
        }
        void SpawnFx(Transform layer,EffectGraphic.Form form,Vector2 at,Vector2 velocity,Vector2 size,Color tint,
            float life,float spin=0,bool gravity=false,float endScale=.25f,float drag=0,float wobble=0,float fadeIn=0)
        {
            if(particles.Count>=ParticleBudget)return;
            var r=Rect(layer,form.ToString(),at.x-size.x*.5f,at.y-size.y*.5f,size.x,size.y);
            var g=r.gameObject.AddComponent<EffectGraphic>();g.form=form;g.color=tint;g.raycastTarget=false;
            r.localEulerAngles=new Vector3(0,0,UnityEngine.Random.Range(0,360));
            particles.Add(new Particle {rect=r,graphic=g,p=at,v=velocity,size=size,tint=tint,life=life,maxLife=life,
                spin=spin,gravity=gravity,endScale=endScale,drag=drag,wobble=wobble,phase=UnityEngine.Random.value*6,fadeIn=fadeIn});
        }
        void SliceBurst(Vector2 at,Color tint,Vector2 direction,int kind)
        {
            SpawnFx(effectLayer,EffectGraphic.Form.Glow,at,Vector2.zero,Vector2.one*130,new Color(tint.r,tint.g,tint.b,.6f),.3f,0,false,1.5f);
            SpawnFx(effectLayer,EffectGraphic.Form.Ring,at,Vector2.zero,Vector2.one*35,new Color(tint.r,tint.g,tint.b,.75f),.34f,0,false,4.5f);
            for(int i=0;i<26;i++)
            {
                Vector2 velocity=UnityEngine.Random.insideUnitCircle*UnityEngine.Random.Range(170,530)+direction*140+new Vector2(0,-130);
                float w=UnityEngine.Random.Range(8,19);
                SpawnFx(effectLayer,EffectGraphic.Form.Drop,at,velocity,new Vector2(w,w*1.8f),tint,UnityEngine.Random.Range(.5f,.95f),UnityEngine.Random.Range(-250,250),true,.1f,.5f);
            }
            for(int i=0;i<8;i++)
            {
                Vector2 p=at+UnityEngine.Random.insideUnitCircle*73;Color stain=tint;stain.a=.38f;
                SpawnFx(effectLayer,EffectGraphic.Form.Glow,p,Vector2.zero,Vector2.one*UnityEngine.Random.Range(24,70),stain,1.5f,0,false,1.3f);
                SpawnFx(effectLayer,EffectGraphic.Form.Seed,at,UnityEngine.Random.insideUnitCircle*300+new Vector2(0,-120),new Vector2(4,8),kind==0||kind==6?Ink:Cream,.8f,UnityEngine.Random.Range(-350,350),true,.35f);
            }
            for(int i=0;i<4;i++)SpawnFx(effectLayer,EffectGraphic.Form.Spark,at+UnityEngine.Random.insideUnitCircle*42,UnityEngine.Random.insideUnitCircle*130,new Vector2(13,22),Cream,.3f,180,false,.1f);
        }
        void BladeSpark(Vector2 at,Vector2 direction)
        {
            for(int i=0;i<2;i++)SpawnFx(effectLayer,EffectGraphic.Form.Spark,at,UnityEngine.Random.insideUnitCircle*55-direction*75,
                new Vector2(6,14),blades[bladeIndex],.22f,140,false,.05f);
        }
        void StarBurst(Vector2 at,Color tint,int count)
        {
            SpawnFx(effectLayer,EffectGraphic.Form.Ring,at,Vector2.zero,Vector2.one*40,tint,.6f,0,false,7);
            for(int i=0;i<count;i++)
            {
                float a=i*Mathf.PI*2/count;Vector2 dir=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                SpawnFx(effectLayer,EffectGraphic.Form.Spark,at+dir*20,dir*UnityEngine.Random.Range(140,390),new Vector2(12,23),tint,UnityEngine.Random.Range(.5f,.9f),180,false,.08f,1.3f);
            }
        }
        void ComboBurst(Vector2 at)
        {
            StarBurst(at,Hex("FFE28A"),22);
            for(int i=0;i<26;i++)
            {
                Color c=i%3==0?Green:i%3==1?Hex("FFC15C"):Cream;
                SpawnFx(effectLayer,EffectGraphic.Form.Shard,at,new Vector2(UnityEngine.Random.Range(-400,400),UnityEngine.Random.Range(-360,-120)),
                    new Vector2(8,15),c,UnityEngine.Random.Range(.8f,1.3f),UnityEngine.Random.Range(-440,440),true,.7f,.25f);
            }
        }
        void BombBurst(Vector2 at)
        {
            SpawnFx(effectLayer,EffectGraphic.Form.Glow,at,Vector2.zero,Vector2.one*250,new Color(1,.59f,.12f,.85f),.45f,0,false,2);
            for(int i=0;i<3;i++)SpawnFx(effectLayer,EffectGraphic.Form.Ring,at,Vector2.zero,Vector2.one*(70+i*45),
                i==1?Hex("FFF0C2"):Hex("FFB54B"),.45f+i*.12f,0,false,5+i);
            for(int i=0;i<42;i++)
            {
                Vector2 v=UnityEngine.Random.insideUnitCircle.normalized*UnityEngine.Random.Range(180,680);
                SpawnFx(effectLayer,EffectGraphic.Form.Spark,at,v,new Vector2(9,27),i%3==0?Cream:Hex("FFB447"),UnityEngine.Random.Range(.45f,.9f),260,true,.05f,.6f);
            }
            for(int i=0;i<12;i++)
            {
                SpawnFx(effectLayer,EffectGraphic.Form.Glow,at+UnityEngine.Random.insideUnitCircle*50,UnityEngine.Random.insideUnitCircle*90+new Vector2(0,-100),
                    Vector2.one*UnityEngine.Random.Range(60,110),new Color(.15f,.16f,.13f,.65f),1.1f,10,false,2.7f,.7f);
                SpawnFx(effectLayer,EffectGraphic.Form.Shard,at,UnityEngine.Random.insideUnitCircle*410,new Vector2(12,17),Hex("5B5343"),1,220,true,.3f);
            }
        }
        void SpawnLeaf()
        {
            SpawnFx(ambientLayer,EffectGraphic.Form.Leaf,new Vector2(UnityEngine.Random.Range(100,1500),-20),
                new Vector2(UnityEngine.Random.Range(-24,24),UnityEngine.Random.Range(55,85)),new Vector2(11,22),Hex("ADB95D"),13,65,false,.8f,0,14,.6f);
        }
        void SpawnFirefly()
        {
            Vector2 p=new Vector2(UnityEngine.Random.Range(180,1420),UnityEngine.Random.Range(250,780));
            Vector2 v=new Vector2(UnityEngine.Random.Range(-16,16),UnityEngine.Random.Range(-26,-8));
            SpawnFx(ambientLayer,EffectGraphic.Form.Glow,p,v,Vector2.one*30,new Color(1,.86f,.35f,.5f),4,0,false,.8f,0,6,.6f);
            SpawnFx(ambientLayer,EffectGraphic.Form.Spark,p,v,new Vector2(3,5),new Color(1,.95f,.64f,.85f),4,0,false,.8f,0,6,.6f);
        }
        void Float(string text,Vector2 p,Color color,int size)
        {
            Text t=Label(effectLayer,"Floating score",text,p.x-400,p.y-35,800,70,size,color,TextAnchor.MiddleCenter,true,2.5f);
            floats.Add(new FloatText {rect=t.rectTransform,text=t,p=new Vector2(p.x,p.y)});
        }
        void ClearRound()
        {
            launchLanes.Clear();openingLaunches=true;pendingBomb=false;previousPattern=-1;
            foreach(var f in fruits) if(f.rect!=null) Destroy(f.rect.gameObject);fruits.Clear();
            foreach(var p in particles) if(p.rect!=null) Destroy(p.rect.gameObject);particles.Clear();
            foreach(var f in floats) if(f.rect!=null) Destroy(f.rect.gameObject);floats.Clear();
            blade?.Clear();
        }

        void SetupAudio()
        {
            sfx=gameObject.AddComponent<AudioSource>();sfx.playOnAwake=false;sfx.spatialBlend=0;
            music=gameObject.AddComponent<AudioSource>();music.loop=true;music.spatialBlend=0;
            sliceClip=Tone("Juicy slice",.16f,(t)=> Mathf.Sin(t*1700-t*t*5800)*Mathf.Exp(-t*32)*.45f+Noise(t)*Mathf.Exp(-t*45)*.55f);
            whooshClip=Tone("Blade swish",.16f,(t)=> Noise(t)*Mathf.Sin(t/.16f*Mathf.PI)*.19f);
            bombClip=Tone("Bomb pop",.7f,(t)=> (Noise(t)*.5f+Mathf.Sin(t*210-t*t*140)*.5f)*Mathf.Exp(-t*8));
            clickClip=Tone("Paper tap",.065f,(t)=> (Mathf.Sin(t*2200)*.6f+Noise(t)*.2f)*Mathf.Exp(-t*70));
            comboClip=Tone("Harvest chime",.5f,(t)=> (Mathf.Sin(t*3300)+Mathf.Sin(t*4158)*.5f+Mathf.Sin(t*4950)*.4f)*Mathf.Exp(-t*9)*.2f);
            float[] notes={261.63f,329.63f,392,523.25f,440,392,329.63f,293.66f,261.63f,329.63f,392,659.25f,587.33f,523.25f,392,329.63f};
            music.clip=Tone("Garden music",12.8f,(t)=> { int n=(int)(t/.8f)%notes.Length;float a=t%.8f;return (Mathf.Sin(a*notes[n]*Mathf.PI*2)+.25f*Mathf.Sin(a*notes[n]*Mathf.PI*4))*Mathf.Exp(-a*7)*.27f; });
            music.volume=musicOn?.13f:0;music.Play();
        }
        static float Noise(float t) => Mathf.Sin(t*124789f)*Mathf.Sin(t*77431f);
        static AudioClip Tone(string name,float duration,Func<float,float> wave)
        {
            const int rate=22050;var data=new float[Mathf.CeilToInt(duration*rate)];
            for(int i=0;i<data.Length;i++) data[i]=wave(i/(float)rate)*Mathf.Min(1,i/50f)*Mathf.Min(1,(data.Length-i)/100f);
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void Play(AudioClip clip,float volume=1,float pitch=1)
        { if(!soundOn || sfx==null || clip==null) return;sfx.pitch=pitch;sfx.PlayOneShot(clip,volume); }

        // Interface coordinates stay centered inside a clipped 1953 × 900 play area.
        static RectTransform Rect(Transform parent,string name,float x,float y,float w,float h)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(.5f,.5f);r.sizeDelta=new Vector2(w,h);r.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);return r;
        }
        Image Fill(Transform parent,string name,float x,float y,float w,float h,Color color)
        { var r=Rect(parent,name,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im; }
        RawImage Raw(Transform parent,string name,float x,float y,float w,float h,Texture tex)
        { var r=Rect(parent,name,x,y,w,h);var im=r.gameObject.AddComponent<RawImage>();im.texture=tex;im.raycastTarget=false;return im; }
        PaperGraphic Shape(Transform parent,string name,float x,float y,float w,float h,Color c,bool circle=false,bool raycast=false)
        {
            var r=Rect(parent,name,x,y,w,h);var g=r.gameObject.AddComponent<PaperGraphic>();g.texture=paperNoise;g.color=c;g.round=circle;g.raycastTarget=raycast;return g;
        }
        PaperGraphic Circle(Transform p,string name,float cx,float cy,float diameter,Color c)
        { return Shape(p,name,cx-diameter/2,cy-diameter/2,diameter,diameter,c,true); }
        RectTransform Board(Transform parent,string name,float x,float y,float w,float h,Color color,float angle,bool corrugated)
        {
            var r=Rect(parent,name,x,y,w,h);r.localEulerAngles=new Vector3(0,0,angle);
            Shape(r,"Cast shadow",0,7,w+4,h+6,new Color(.08f,.045f,.018f,.45f));
            Shape(r,"Dark cut edge",-3,0,w+6,h+3,Hex("3E2A19"));
            if(corrugated)
            {
                Shape(r,"Corrugated edge",1,5,w-2,h-1,Hex("A57A42"));
                for(int i=16;i<w-10;i+=9) Fill(r,"Flute",i,h-5,2,7,new Color(.24f,.12f,.04f,.35f));
            }
            Shape(r,"Paper face",1,0,w-2,h-6,color,false,true);
            return r;
        }
        void Tape(RectTransform p,float x,float y,float w,float h,float angle)
        {
            var t=Shape(p,"Masking tape",x,y,w,h,new Color(.96f,.85f,.6f,.83f));t.rectTransform.localEulerAngles=new Vector3(0,0,angle);
            Fill(t.transform,"Tape fiber",8,5,w-16,1,new Color(1,.95f,.8f,.18f));
        }
        void Nail(RectTransform p,float x,float y)
        {
            Circle(p,"Nail shadow",x+1,y+2,12,Hex("352519"));Circle(p,"Nail",x,y,10,Hex("B9AC84"));
            var line=Fill(p,"Nail slot",x-3,y-1,6,2,Hex("5D4D36"));line.rectTransform.localEulerAngles=new Vector3(0,0,35);
        }
        Text Label(Transform parent,string name,string text,float x,float y,float w,float h,int size,Color color,
            TextAnchor alignment=TextAnchor.MiddleCenter,bool display=true,float outline=0)
        {
            var r=Rect(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=display?heading:body;t.text=text;
            t.fontSize=size;t.color=color;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;
            if(outline>0) { var o=r.gameObject.AddComponent<Outline>();o.effectColor=Ink;o.effectDistance=new Vector2(outline,-outline);o.useGraphicAlpha=true; }
            return t;
        }
        void FruitIcon(Transform parent,int kind,float cx,float cy,float size,float angle,bool bob)
        {
            var r=Rect(parent,"Fruit illustration",cx-size/2,cy-size/2,size,size);
            var g=r.gameObject.AddComponent<FruitSliceGraphic>();g.atlas=atlas;g.uv=FruitUV(kind);g.raycastTarget=false;
            r.localEulerAngles=new Vector3(0,0,angle);
            if(bob) bobs.Add(new Bob {rect=r,start=r.anchoredPosition,phase=UnityEngine.Random.value*6});
        }
        void SmallButton(Transform p,string text,float x,float y,float w,float h,Action action)
        { ActionButton(p,text,x,y,w,h,Hex("D9B77B"),action); }
        void TextButton(Transform p,string text,float x,float y,float w,float h,Action action)
        {
            var r=Rect(p,text,x,y,w,h);var bg=Shape(r,"Hit target",0,0,w,h,new Color(1,1,1,0),false,true);
            bg.raycastPadding=new Vector4(0,-9,0,-9);
            Label(r,"Caption",text,0,0,w,h,24,Ink);var b=r.gameObject.AddComponent<PaperButton>();b.action=()=>{ Play(clickClip,.4f);action(); };
        }
        void ActionButton(Transform p,string text,float x,float y,float w,float h,Color color,Action action)
        {
            var r=Board(p,text,x,y,w,h,color,-.35f,false);
            r.Find("Paper face").GetComponent<PaperGraphic>().raycastPadding=new Vector4(-4,-10,-4,-10);
            Label(r,"Caption",text,7,2,w-14,h-8,text.Length<=2?36:28,color==Green?Cream:Ink);
            var b=r.gameObject.AddComponent<PaperButton>();b.action=()=>{ Play(clickClip,.4f);action(); };b.hoverSound=()=>Play(clickClip,.08f);
        }
        static Rect FruitUV(int kind) { return new Rect((kind%4)*.25f,kind<4?.5f:0,.25f,.5f); }
        static void ClearChildren(Transform p) { for(int i=p.childCount-1;i>=0;i--) Destroy(p.GetChild(i).gameObject); }
        static Texture2D MakePaper()
        {
            var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);var data=new Color[128*128];var random=new System.Random(42);
            for(int y=0;y<128;y++) for(int x=0;x<128;x++) { float n=.87f+(float)random.NextDouble()*.13f;if(y%7==0)n-=.025f;data[y*128+x]=new Color(n,n,n,1); }
            tex.SetPixels(data);tex.Apply();tex.wrapMode=TextureWrapMode.Repeat;return tex;
        }
        public static float DistanceToSegment(Vector2 point,Vector2 a,Vector2 b)
        { Vector2 d=b-a;float t=d.sqrMagnitude<.001f?0:Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude);return Vector2.Distance(point,a+d*t); }
        static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#"+hex,out var c);return c; }

        #if UNITY_EDITOR
        public void RunMobileSmokeTest(Action<string> done)
        {
            verification=true;int originalTotal=totalFruit;var randomState=UnityEngine.Random.state;
            UnityEngine.Random.InitState(217);
            StartRound(GameMode.Zen);countdown=0;
            var seen=new HashSet<Fruit>();var times=new List<float>();var lanes=new List<int>();
            int[] laneCounts=new int[3];float peak=900;bool staggered=true;
            // Exercise the real scheduler and physics for twenty seconds at 60 Hz.
            for(int i=0;i<1200;i++)
            {
                TickRound(1f/60f);int launched=0;
                foreach(var fruit in fruits)
                {
                    peak=Mathf.Min(peak,fruit.p.y);
                    if(!seen.Add(fruit))continue;
                    int lane=fruit.p.x<545?0:fruit.p.x>1055?2:1;
                    times.Add(elapsed);lanes.Add(lane);laneCounts[lane]++;launched++;
                }
                if(launched>1)staggered=false;
            }
            float minDelay=10,maxDelay=0;
            for(int i=1;i<times.Count;i++){float delay=times[i]-times[i-1];minDelay=Mathf.Min(minDelay,delay);maxDelay=Mathf.Max(maxDelay,delay);}
            bool spread=laneCounts[0]>3 && laneCounts[1]>3 && laneCounts[2]>3;
            bool opening=lanes.Count>4 && lanes[0]==1 && lanes[1]==1 && lanes[2]!=1 && lanes[3]==2-lanes[2];
            bool delayed=staggered && minDelay>=.14f && maxDelay<=.82f && maxDelay-minDelay>.2f;
            bool height=peak<350 && peak>145;
            Pause();float timerBefore=spawnTimer;int countBefore=fruits.Count;TickRound(2);
            bool pausedLaunches=spawnTimer==timerBefore && fruits.Count==countBefore;Resume();
            StartRound(GameMode.Zen);countdown=0;TickRound(.11f);
            bool reset=fruits.Count==1 && launchLanes.Count==3;
            ShowMenu();int cardFruit=menu.Find("ARCADE mode card").GetComponentsInChildren<FruitSliceGraphic>().Length;
            bool cleanMenu=cardFruit==1 && menu.Find("Swipe hint")==null && menu.Find("Juice Dojo logo/Painted logo")!=null;
            StartRound(GameMode.Zen);bool cleanHud=hud.Find("Play hint")==null && hud.Find("Pause hint")==null;
            bool aspect=Mathf.Abs(frame.rect.width/frame.rect.height-2.17f)<.0001f;
            Vector3 local=new Vector3(800,450,0);Vector3 screen=root.TransformPoint(local-new Vector3(800,450,0));
            bool pointerAligned=Vector2.Distance(new Vector2(screen.x,screen.y),stageCenter)<1;
            UnityEngine.Random.state=randomState;totalFruit=originalTotal;verification=false;ShowMenu();
            done("LaneCoverage="+spread+" HigherFruit="+height+" OpeningMiddlePair="+opening+" RandomDelays="+delayed+" OneLaunchPerBeat="+staggered+" PausedLaunches="+pausedLaunches+" PatternReset="+reset+
                " MenuClean="+cleanMenu+" HudClean="+cleanHud+" AspectRatio="+aspect+" PointerAligned="+pointerAligned+
                " Launches="+times.Count+" PeakY="+peak.ToString("0.0")+" MinDelay="+minDelay.ToString("0.000")+" MaxDelay="+maxDelay.ToString("0.000"));
        }
        public void RunRecutSmokeTest(Action<string> done)
        {
            verification=true;int originalTotal=totalFruit;
            StartRound(GameMode.Zen);countdown=0;spawnTimer=100;
            var fruit=SpawnFruit(0,new Vector2(800,450),Vector2.zero);fruit.angle=0;
            SliceSegment(new Vector2(650,450),new Vector2(950,450));
            bool first=fruits.Count==2 && roundFruit==1;
            SliceSegment(new Vector2(650,450),new Vector2(950,450));bool noCascade=fruits.Count==2;
            TickFruit(.14f);
            var target=fruits[0];Vector2 center=Vector2.zero;
            foreach(var point in target.hitShape)center+=point;center/=target.hitShape.Count;
            SliceSegment(FruitWorldPoint(target,center+Vector2.left*.6f),FruitWorldPoint(target,center+Vector2.right*.6f));
            bool second=fruits.Count==3;
            TickFruit(.14f);target=fruits[0];center=Vector2.zero;
            foreach(var point in target.hitShape)center+=point;center/=target.hitShape.Count;
            bool thirdCut=CutFruit(target,FruitWorldPoint(target,center+Vector2.down*.6f),FruitWorldPoint(target,center+Vector2.up*.6f));
            bool third=thirdCut && fruits.Count==4;
            float area=0;foreach(var piece in fruits)area+=FruitSliceGraphic.Area(piece.graphic.polygon);
            bool retainedShapes=Mathf.Abs(area-1)<.0001f && fruits.TrueForAll(piece=>piece.graphic.polygon!=null);
            bool harvestOnce=roundFruit==1 && totalFruit==originalTotal+1 && strokeCount==1 && score==3;
            // A swipe through a missing corner of a clipped piece must not hit it.
            bool emptyCorner=!FruitSliceGraphic.SegmentHits(FruitSliceGraphic.Clip(FruitSliceGraphic.WholeShape(),Vector2.up,0),new Vector2(-.4f,-.2f),new Vector2(.4f,-.2f));
            StartRound(GameMode.Classic);countdown=0;
            var missedPiece=SpawnFruit(1,new Vector2(800,450),Vector2.zero);missedPiece.angle=0;
            SliceSegment(new Vector2(650,450),new Vector2(950,450));
            foreach(var piece in fruits)piece.p.y=1120;TickFruit(.01f);bool noPieceMisses=misses==0;
            totalFruit=originalTotal;verification=false;ShowMenu();
            done("FirstCut="+first+" SecondCut="+second+" ThirdCut="+third+" RetainedPieceShapes="+retainedShapes+
                " HarvestCountedOnce="+harvestOnce+" NoSameSwipeCascade="+noCascade+" EmptyAreaMiss="+emptyCorner+" NoFragmentMissPenalty="+noPieceMisses);
        }
        public void RunPolishSmokeTest(Action<string> done) { StartCoroutine(PolishSmoke(done)); }
        System.Collections.IEnumerator PolishSmoke(Action<string> done)
        {
            bool art=logo!=null && orchard!=null && atlas!=null;
            int originalTotal=totalFruit;verification=true;ClearRound();CloseModal();
            state=ScreenState.Loading;menu.gameObject.SetActive(false);gameLayer.gameObject.SetActive(false);
            ShowLoadingScreen();SetLoadingProgress(.62f,"GATHERING THE JUICIEST FRUIT...");
            bool loading=loadingGroup.blocksRaycasts && Mathf.Abs(loadingFill.sizeDelta.x-287.68f)<.1f;
            yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Artifacts/LoadingScreen-v2.png"));
            yield return new WaitForSecondsRealtime(.2f);SetLoadingProgress(1,"FRESH FRUIT. SHARP BLADES. LET'S GO!");
            DismissLoading();ShowMenu();yield return null;yield return null;
            bool menuLogo=menu.Find("Juice Dojo logo/Painted logo")!=null;
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Artifacts/MainMenu-v2.png"));
            yield return new WaitForSecondsRealtime(.2f);
            StartRound(GameMode.Zen);countdown=0;spawnTimer=100;
            for(int i=0;i<3;i++)SpawnFruit(i,new Vector2(630+i*170,510),Vector2.zero);
            SliceSegment(new Vector2(500,510),new Vector2(1150,510));FinishStroke();
            bool sliceParticles=particles.Count>120;
            TickEffects(.08f);yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Artifacts/SliceEffects-v2.png"));
            yield return new WaitForSecondsRealtime(.2f);
            ClearRound();BombBurst(new Vector2(800,510));bool bomb=particles.Count>=60;
            TickEffects(.08f);yield return null;yield return null;
            ScreenCapture.CaptureScreenshot(System.IO.Path.GetFullPath("Artifacts/BombEffects-v2.png"));
            yield return new WaitForSecondsRealtime(.2f);
            for(int i=0;i<500;i++)SpawnFx(effectLayer,EffectGraphic.Form.Spark,new Vector2(800,450),Vector2.zero,Vector2.one*9,Cream,.3f);
            bool budget=particles.Count<=ParticleBudget;TickEffects(20);bool cleanup=particles.Count==0;
            totalFruit=originalTotal;verification=false;ShowMenu();
            done("LogoAsset="+art+" MenuLogo="+menuLogo+" LoadingProgress="+loading+" SliceBurst="+sliceParticles+" BombBurst="+bomb+" ParticleBudget="+budget+" ParticleCleanup="+cleanup);
        }
        // Editor verification calls the same spawning, collision and UI event paths as normal play.
        public void RunSmokeTest(Action<string> done)
        {
            verification=true;int originalTotal=totalFruit;
            StartRound(GameMode.Zen);countdown=0;
            for(int i=0;i<3;i++) SpawnFruit(i,new Vector2(660+i*140,450),Vector2.zero);
            SliceSegment(new Vector2(550,450),new Vector2(1100,450));FinishStroke();
            bool slice=roundFruit==3 && score==12 && roundCombo==3;
            Pause();bool pause=CurrentState=="Paused";Resume();bool resume=CurrentState=="Playing";
            StartRound(GameMode.Arcade);countdown=0;score=25;SpawnFruit(7,new Vector2(800,450),Vector2.zero);
            SliceSegment(new Vector2(650,450),new Vector2(950,450));bool bomb=score==15 && CurrentState=="Playing";
            StartRound(GameMode.Classic);countdown=0;SpawnFruit(7,new Vector2(800,450),Vector2.zero);
            SliceSegment(new Vector2(650,450),new Vector2(950,450));bool classic=CurrentState=="Results";
            StartRound(GameMode.Classic);countdown=0;
            for(int i=0;i<3;i++) { SpawnFruit(i,new Vector2(700,1120),Vector2.zero);TickFruit(.001f); }
            bool missed=CurrentState=="Results" && misses==3;
            StartRound(GameMode.Zen);countdown=0;
            for(int i=0;i<4;i++) { SpawnFruit(i,new Vector2(700,1120),Vector2.zero);TickFruit(.001f); }
            bool zen=misses==0 && CurrentState=="Playing";
            clock=.001f;TickRound(.02f);bool timer=CurrentState=="Results";
            StartRound(GameMode.Arcade);countdown=0;clock=20;freeze=3;TickRound(1);
            bool frozen=Mathf.Abs(clock-19.5f)<.001f;
            float pausedClock=clock;Pause();TickRound(1);bool pausedTimer=clock==pausedClock;
            bool segment=DistanceToSegment(new Vector2(800,450),new Vector2(10,450),new Vector2(1590,450))<.001f;
            verification=false;totalFruit=originalTotal;
            ShowMenu();done("Slicing="+slice+" Combo="+slice+" Pause="+pause+" Resume="+resume+" ArcadeBomb="+bomb+" ClassicBomb="+classic+" ThreeMisses="+missed+" ZenMisses="+zen+" Timer="+timer+" Freeze="+frozen+" PausedTimer="+pausedTimer+" FastSwipe="+segment);
        }
        public void RunUiSmokeTest(Action<string> done) { StartCoroutine(UiSmoke(done)); }
        System.Collections.IEnumerator UiSmoke(Action<string> done)
        {
            ShowMenu();yield return null;yield return null;Canvas.ForceUpdateCanvases();
            string cardHit=ClickDesign(new Vector2(1190,570));bool modeClick=state==ScreenState.Playing && mode==GameMode.Zen;
            yield return null;Canvas.ForceUpdateCanvases();
            string pauseHit=ClickDesign(new Vector2(1550,124));bool pauseClick=state==ScreenState.Paused;
            yield return null;Canvas.ForceUpdateCanvases();
            ClickDesign(new Vector2(800,466));bool resumeClick=state==ScreenState.Playing;
            SetLandscapeBlocked(true);float heldCountdown=countdown;yield return null;yield return null;
            bool portraitSuspended=state==ScreenState.Playing && countdown==heldCountdown;
            SetLandscapeBlocked(false);yield return null;
            bool landscapeResumed=state==ScreenState.Playing && countdown<heldCountdown;
            ShowMenu();yield return null;Canvas.ForceUpdateCanvases();
            ClickDesign(new Vector2(963,845));bool settingsClick=modal!=null;
            yield return null;Canvas.ForceUpdateCanvases();
            bool before=soundOn;ClickDesign(new Vector2(1012,374));bool soundClick=soundOn!=before;
            yield return null;Canvas.ForceUpdateCanvases();ClickDesign(new Vector2(1012,374));
            ShowMenu();
            done("ModeCard="+modeClick+" PauseButton="+pauseClick+" ResumeButton="+resumeClick+" PortraitSuspended="+portraitSuspended+" LandscapeResumed="+landscapeResumed+
                " SettingsButton="+settingsClick+" SoundToggle="+soundClick+" SettingsRestored="+(before==soundOn)+"\nCard hit: "+cardHit+"\nPause hit: "+pauseHit);
        }
        string ClickDesign(Vector2 p)
        {
            var pointer=new PointerEventData(EventSystem.current) { position=new Vector2((p.x-800)*uiScale+stageCenter.x,(450-p.y)*uiScale+stageCenter.y),button=PointerEventData.InputButton.Left };
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            if(hits.Count>0)
            {
                var go=hits[0].gameObject;string path=go.name;
                for(var t=go.transform.parent;t!=null;t=t.parent) path=t.name+"/"+path;
                ExecuteEvents.ExecuteHierarchy(go,pointer,ExecuteEvents.pointerClickHandler);return path+" => "+state+"/"+mode;
            }
            return "no hit";
        }
        #endif
    }
}
