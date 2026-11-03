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
        enum ScreenState { Menu, Playing, Paused, Results }
        sealed class Fruit
        {
            public RectTransform rect;
            public FruitSliceGraphic graphic;
            public Vector2 p,v;
            public float radius,spin,angle,life=8;
            public int kind,half;
            public bool power;
        }
        sealed class Particle
        {
            public RectTransform rect;
            public Graphic graphic;
            public Vector2 p,v;
            public float life,maxLife,spin;
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

        static readonly Color Ink=Hex("3C2B1B"),Cream=Hex("FFF0BE"),Paper=Hex("D6AF70"),Green=Hex("83AE35");
        readonly Color[] juice={Hex("E94F5E"),Hex("FFB52E"),Hex("F34F38"),Hex("FFE56C"),Hex("F7CF43"),Hex("AD76E6"),Hex("FF5D63"),Hex("EEAC3E")};
        readonly Color[] blades={Hex("9BFFE0"),Hex("FFC267"),Hex("CEABFF")};
        readonly List<Fruit> fruits=new List<Fruit>();
        readonly List<Particle> particles=new List<Particle>();
        readonly List<FloatText> floats=new List<FloatText>();
        readonly List<Bob> bobs=new List<Bob>();
        RectTransform root,menu,gameLayer,fruitLayer,effectLayer,hud,modal,bladeLayer;
        BladeGraphic blade;
        Font heading,body;
        Texture2D orchard,atlas,paperNoise;
        Text scoreText,bestText,timeText,modeText,comboText,missText,noticeText;
        Image flash;
        AudioSource sfx,music;
        AudioClip sliceClip,bombClip,clickClip,whooshClip,comboClip;
        ScreenState state;
        GameMode mode;
        int score,misses,totalFruit,roundFruit,roundCombo,strokeCount,bladeIndex;
        float clock,elapsed,spawnTimer,countdown,comboWindow,strokeLastTime,shake,freeze,frenzy,ambientTimer;
        float uiScale=1;
        bool soundOn,musicOn,hadPointer,verification;
        Vector2 lastPointer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if(FindFirstObjectByType<FruitNinjaGame>()==null) new GameObject("Fruit Ninja · Backyard Dojo").AddComponent<FruitNinjaGame>();
        }

        void Awake()
        {
            Instance=this;
            Application.targetFrameRate=120;
            QualitySettings.vSyncCount=1;
            heading=Resources.Load<Font>("Fonts/LilitaOne");body=Resources.Load<Font>("Fonts/Lato");
            orchard=Resources.Load<Texture2D>("Art/Orchard");atlas=Resources.Load<Texture2D>("Art/FruitAtlas");
            soundOn=PlayerPrefs.GetInt("Dojo.Sound",1)==1;musicOn=PlayerPrefs.GetInt("Dojo.Music",1)==1;
            bladeIndex=PlayerPrefs.GetInt("Dojo.Blade",0);totalFruit=PlayerPrefs.GetInt("Dojo.TotalFruit",0);
            paperNoise=MakePaper();
            SetupCanvas();SetupAudio();ShowMenu();
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
            root=Rect(canvasObject.transform,"1600 × 900 stage",0,0,1600,900);
            root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
            var bg=Raw(root,"Orchard",0,0,1600,900,orchard);bg.raycastTarget=false;
            Fill(root,"Garden shade",0,0,1600,900,new Color(.02f,.08f,.045f,.13f));
            gameLayer=Rect(root,"Game",0,0,1600,900);
            fruitLayer=Rect(gameLayer,"Flying fruit",0,0,1600,900);
            effectLayer=Rect(gameLayer,"Juice & sparks",0,0,1600,900);
            hud=Rect(gameLayer,"HUD",0,0,1600,900);
            menu=Rect(root,"Menu",0,0,1600,900);
            bladeLayer=Rect(root,"Blade layer",0,0,1600,900);
            blade=bladeLayer.gameObject.AddComponent<BladeGraphic>();blade.raycastTarget=false;blade.bladeColor=blades[bladeIndex];
            flash=Fill(root,"Flash",0,0,1600,900,Color.clear);flash.raycastTarget=false;
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var es=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Resize();
        }

        void Update()
        {
            float dt=Mathf.Min(Time.unscaledDeltaTime,.04f);Resize();
            blade.Tick(dt);
            if(flash.color.a>0) { Color c=flash.color;c.a=Mathf.Max(0,c.a-dt*1.7f);flash.color=c; }
            if(shake>0) { shake-=dt;root.anchoredPosition=UnityEngine.Random.insideUnitCircle*shake*15; }
            else root.anchoredPosition=Vector2.zero;
            foreach(var b in bobs) if(b.rect!=null) b.rect.anchoredPosition=b.start+new Vector2(0,Mathf.Sin(Time.unscaledTime*1.6f+b.phase)*6);
            HandleKeyboard();HandlePointer();
            TickRound(dt);
            if(state!=ScreenState.Paused) TickEffects(dt);
            ambientTimer-=dt;
            if(ambientTimer<=0 && state==ScreenState.Menu) { SpawnLeaf();ambientTimer=UnityEngine.Random.Range(.6f,1.6f); }
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
                        if(spawnTimer<=0) { SpawnWave();spawnTimer=frenzy>0?.32f:Mathf.Max(.65f,1.65f-elapsed*.009f); }
                        TickFruit(dt*speed);
                        if(comboWindow>0) { comboWindow-=dt;if(comboWindow<=0) FinishStroke(); }
                        UpdateHud();
                    }
                }
            }
        }

        void Resize()
        {
            uiScale=Mathf.Min(Screen.width/1600f,Screen.height/900f);
            if(root!=null) root.localScale=Vector3.one*uiScale;
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
            Vector2 p=new Vector2((screen.x-Screen.width*.5f)/uiScale+800,450-(screen.y-Screen.height*.5f)/uiScale);
            if(held && p.x>=0 && p.x<=1600 && p.y>=0 && p.y<=900)
            {
                if(!hadPointer) { lastPointer=p;blade.Clear();strokeCount=0; }
                Vector2 delta=p-lastPointer;
                if(delta.sqrMagnitude>5)
                {
                    blade.Add(new Vector2(p.x-800,450-p.y));
                    if(state==ScreenState.Playing && countdown<=0 && p.y>138 && lastPointer.y>138)
                    {
                        if(delta.sqrMagnitude>4500 && Time.unscaledTime-strokeLastTime>.13f) { Play(whooshClip,.15f);strokeLastTime=Time.unscaledTime; }
                        SliceSegment(lastPointer,p);
                    }
                    else if(state==ScreenState.Menu && modal==null && hadPointer && delta.magnitude>12)
                    {
                        for(int i=0;i<3;i++) if(DistanceToSegment(new Vector2(410+i*390,462),lastPointer,p)<65)
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
            Label(brand,"station","THE BACKYARD DOJO",38,0,275,34,19,Cream,TextAnchor.MiddleLeft,false);
            SmallButton(menu,"?",1396,35,55,52,ShowHelp);
            SmallButton(menu,soundOn?"SFX":"OFF",1464,35,74,52,()=>{ soundOn=!soundOn;SaveSettings();ShowMenu(); });
            // A two-line painted wooden logo.
            var sign=Board(menu,"Fruit Ninja sign",460,78,680,218,Hex("664222"),-2,false);
            Fill(sign,"Top grain",25,27,630,2,new Color(.95f,.75f,.38f,.12f));
            Fill(sign,"Bottom grain",36,189,610,2,new Color(.1f,.04f,0,.25f));
            Nail(sign,26,26);Nail(sign,640,26);Nail(sign,30,178);Nail(sign,642,178);
            Label(sign,"FRUIT","FRUIT",64,-4,550,112,112,Hex("FFE395"),TextAnchor.MiddleCenter,true,5);
            Label(sign,"NINJA","NINJA",60,88,560,115,116,Hex("A6CF4C"),TextAnchor.MiddleCenter,true,5);
            // Little slash and leaves break the silhouette of the sign.
            var slash=Fill(sign,"Blade stroke",532,37,9,146,Hex("FFF6CB"));slash.rectTransform.localEulerAngles=new Vector3(0,0,-26);
            FruitIcon(menu,2,390,182,133,-18,true);FruitIcon(menu,1,1125,218,128,14,true);
            var strip=Board(menu,"Choose a mode",590,316,420,51,Hex("EFCD8D"),1.2f,true);
            Label(strip,"Choose","PICK YOUR CHALLENGE",15,4,390,41,25,Ink);
            CreateModeCard(0,245,390,-2.3f,"CLASSIC","THE ORIGINAL",0,"3 misses. One sharp blade.","Watch out for bombs!",Hex("759D37"));
            CreateModeCard(1,635,390,1.2f,"ARCADE","FAST & FRUITY",1,"60 seconds. Make it juicy.","Combos, frenzy & freeze.",Hex("C8782B"));
            CreateModeCard(2,1025,390,-1.5f,"ZEN","FIND YOUR FLOW",5,"90 seconds. Just fruit.","No bombs. No worries.",Hex("6C8D77"));
            var hint=Label(menu,"Swipe hint","HOLD & SWIPE TO SLICE  ·  CLICK A CARD TO PLAY",400,746,800,34,18,Cream);
            hint.color=new Color(1,.94f,.73f,.82f);
            var drawer=Board(menu,"Workbench drawer",510,819,580,55,Hex("B68B50"),-.8f,false);
            TextButton(drawer,"YOUR HARVEST",16,5,176,43,ShowStats);
            TextButton(drawer,"BLADES",205,5,130,43,ShowBlades);
            TextButton(drawer,"SETTINGS",350,5,205,43,ShowSettings);
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
            if(index==1) FruitIcon(card,7,222,126,70,13,true);
            Label(card,"Mode name",name,20,161,290,55,48,Ink);
            Label(card,"Description",line1,18,217,294,29,18,Ink,TextAnchor.MiddleCenter,false);
            Label(card,"Rule",line2,18,244,294,25,16,Hex("705532"),TextAnchor.MiddleCenter,false);
            var play=Board(card,"Start strip",30,283,270,35,accent,0,false);
            Label(play,"Play","SLICE TO START  >",4,0,262,32,20,Cream);
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
            SmallButton(hud,"II",1451,37,91,70,Pause);
            comboText=Label(hud,"Combo status","",500,166,600,42,27,Hex("FFE08A"));
            noticeText=Label(hud,"Countdown","",350,319,900,160,99,Cream,TextAnchor.MiddleCenter,true,5);
            Label(hud,"Play hint","HOLD + SWIPE TO SLICE",565,837,470,31,18,Hex("FFE9B5"));
            Label(hud,"Pause hint","ESC  PAUSE",1384,837,151,30,15,Hex("FFE9B5"),TextAnchor.MiddleRight,false);
        }
        void UpdateHud()
        {
            scoreText.text=score.ToString();bestText.text="BEST  "+PlayerPrefs.GetInt("Dojo.Best."+mode,0);
            if(mode==GameMode.Classic) { missText.text=(misses>0?"X":"_")+"  "+(misses>1?"X":"_")+"  "+(misses>2?"X":"_");missText.color=misses>0?Hex("B53F30"):Ink; }
            else { missText.text=Mathf.CeilToInt(clock).ToString()+"s";missText.color=clock<10?Hex("B53F30"):Ink; }
            comboText.text=freeze>0?"FROZEN TIME  "+freeze.ToString("0.0")+"s":frenzy>0?"FRUIT FRENZY!  "+frenzy.ToString("0.0")+"s":"";
        }

        void SpawnWave()
        {
            int count=mode==GameMode.Zen?UnityEngine.Random.Range(3,6):UnityEngine.Random.Range(2,5);
            if(frenzy>0) count=4;
            float baseX=UnityEngine.Random.Range(420,1180);
            for(int i=0;i<count;i++)
            {
                int kind=UnityEngine.Random.Range(0,7);
                bool power=mode==GameMode.Arcade && elapsed>5 && UnityEngine.Random.value<.095f;
                SpawnFruit(kind,new Vector2(Mathf.Clamp(baseX+(i-(count-1)*.5f)*118,170,1430),995+i*24),
                    new Vector2(UnityEngine.Random.Range(-115,115)+(800-baseX)*.12f,UnityEngine.Random.Range(-1370,-1190)),power);
            }
            if(mode!=GameMode.Zen && elapsed>3 && UnityEngine.Random.value<(mode==GameMode.Classic?.45f:.36f))
                SpawnFruit(7,new Vector2(UnityEngine.Random.Range(280,1320),1020),new Vector2(UnityEngine.Random.Range(-160,160),-UnityEngine.Random.Range(1190,1320)),false);
        }
        Fruit SpawnFruit(int kind,Vector2 p,Vector2 velocity,bool power=false)
        {
            float size=kind==4?158:kind==3?145:kind==7?135:145;
            var r=Rect(fruitLayer,kind==7?"Bomb":"Fruit "+kind,p.x-size/2,p.y-size/2,size,size);
            var graphic=r.gameObject.AddComponent<FruitSliceGraphic>();graphic.atlas=atlas;graphic.uv=FruitUV(kind);graphic.raycastTarget=false;
            if(power) { var glow=Circle(r,"Power fruit glow",size*.5f,size*.5f,size*.94f,new Color(.5f,1,.9f,.22f));glow.transform.SetAsFirstSibling(); }
            var f=new Fruit {rect=r,graphic=graphic,p=p,v=velocity,radius=size*.32f,kind=kind,spin=UnityEngine.Random.Range(-100,100),angle=UnityEngine.Random.Range(-30,30),power=power};
            fruits.Add(f);return f;
        }
        void TickFruit(float dt)
        {
            for(int i=fruits.Count-1;i>=0;i--)
            {
                Fruit f=fruits[i];f.v.y+=1650*dt;f.p+=f.v*dt;f.angle+=f.spin*dt;f.life-=dt;
                f.rect.anchoredPosition=new Vector2(f.p.x,-f.p.y);f.rect.localEulerAngles=new Vector3(0,0,f.angle);
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
                Fruit f=fruits[i];if(f.half!=0 || DistanceToSegment(f.p,a,b)>f.radius) continue;
                if(f.kind==7)
                {
                    Vector2 p=f.p;Destroy(f.rect.gameObject);fruits.RemoveAt(i);HitBomb(p);
                    if(state!=ScreenState.Playing) break;
                }
                else CutFruit(f,(b-a).normalized);
            }
        }
        void CutFruit(Fruit f,Vector2 direction)
        {
            if(f.half!=0) return;
            float angle=Mathf.Atan2(-direction.y,direction.x)+Mathf.PI*.5f-f.angle*Mathf.Deg2Rad;
            f.half=-1;f.graphic.half=-1;f.graphic.cutAngle=angle;f.graphic.SetVerticesDirty();
            var other=Rect(fruitLayer,"Sliced half",f.p.x-f.rect.sizeDelta.x/2,f.p.y-f.rect.sizeDelta.y/2,f.rect.sizeDelta.x,f.rect.sizeDelta.y);
            var graphic=other.gameObject.AddComponent<FruitSliceGraphic>();graphic.atlas=atlas;graphic.uv=f.graphic.uv;graphic.half=1;graphic.cutAngle=angle;graphic.raycastTarget=false;
            Vector2 n=new Vector2(-direction.y,direction.x);
            fruits.Add(new Fruit {rect=other,graphic=graphic,p=f.p-n*8,v=f.v*.35f-n*180+new Vector2(0,-90),kind=f.kind,half=1,angle=f.angle,spin=f.spin+180,life=1.6f});
            f.v=f.v*.35f+n*180+new Vector2(0,-90);f.p+=n*8;f.spin-=180;f.life=1.6f;
            score++;roundFruit++;totalFruit++;strokeCount++;comboWindow=.24f;
            Splash(f.p,juice[f.kind],18);
            bool critical=!verification && UnityEngine.Random.value<.035f;
            if(critical) { score+=9;Float("CRITICAL! +10",f.p+new Vector2(0,-68),Hex("FFE287"),36); }
            if(f.power)
            {
                if(UnityEngine.Random.value<.5f) { freeze=5;Float("FREEZE!",f.p+new Vector2(0,-95),Hex("A0FFE6"),43); }
                else { frenzy=6;Float("FRUIT FRENZY!",f.p+new Vector2(0,-95),Hex("FFE68C"),40); }
                Play(comboClip,.5f);
            }
            Play(sliceClip,UnityEngine.Random.Range(.35f,.5f),UnityEngine.Random.Range(.85f,1.3f));
            UpdateHud();
        }
        void FinishStroke()
        {
            if(strokeCount>=3)
            {
                int bonus=strokeCount*strokeCount;score+=bonus;roundCombo=Mathf.Max(roundCombo,strokeCount);
                Float(strokeCount+" FRUIT COMBO!",new Vector2(800,330),Hex("FFE586"),48);
                Float("+"+bonus+" BONUS",new Vector2(800,385),Cream,28);Play(comboClip,.45f,1+strokeCount*.045f);
            }
            strokeCount=0;comboWindow=0;
        }
        void HitBomb(Vector2 p)
        {
            FinishStroke();Splash(p,Hex("FFE29A"),35);Splash(p,Hex("5B554B"),22);
            flash.color=new Color(1,.54f,.24f,.55f);shake=.55f;Play(bombClip,.8f);
            if(mode==GameMode.Classic) EndRound("A sneaky bomb ended your harvest!");
            else { score=Mathf.Max(0,score-10);Float("BOMB! -10",p,Hex("FFAF77"),44); }
        }

        public void Pause()
        {
            if(state!=ScreenState.Playing) return;
            state=ScreenState.Paused;blade.Clear();Play(clickClip,.4f);
            var panel=OpenModal("TAKE A BREATHER",620,435);
            Label(panel,"Message","Your fruit will wait for you.",45,116,530,44,24,Ink,TextAnchor.MiddleCenter,false);
            ActionButton(panel,"KEEP SLICING",100,199,420,69,Green,Resume);
            ActionButton(panel,"START FRESH",100,285,201,58,Paper,()=>StartRound(mode));
            ActionButton(panel,"MAIN MENU",319,285,201,58,Paper,ShowMenu);
            Label(panel,"Shortcut","ESC / SPACE TO RESUME",80,367,460,29,16,Hex("7E613D"));
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
            var panel=OpenModal(record?"A FRESH NEW RECORD!":"NICE SLICING!",680,620);
            Label(panel,"Mode",mode.ToString().ToUpper()+" HARVEST",80,98,520,36,21,Hex("76562E"));
            Label(panel,"Final score",score.ToString(),70,136,540,136,118,Ink);
            Label(panel,"Points","JUICY POINTS",80,266,520,35,20,Hex("76562E"));
            var row=Board(panel,"Stats strip",55,318,570,70,Hex("C3995D"),0,false);
            Label(row,"Fruit",roundFruit+" FRUIT SLICED",15,7,260,50,22,Ink);
            Label(row,"Combo",roundCombo+" BEST COMBO",285,7,270,50,22,Ink);
            Label(panel,"Reason",reason,42,402,596,49,20,Ink,TextAnchor.MiddleCenter,false);
            ActionButton(panel,"SLICE AGAIN  >",95,474,490,65,Green,()=>StartRound(mode));
            TextButton(panel,"BACK TO THE GARDEN",120,551,440,42,ShowMenu);
            Play(comboClip,.45f);
        }

        void ShowHelp()
        {
            var p=OpenModal("A LITTLE NINJA KNOW-HOW",760,590);
            HelpRow(p,1,127,"HOLD. SWIPE. SPLASH.","Drag your mouse or finger through airborne fruit.");
            HelpRow(p,2,229,"MORE FRUIT. BIGGER COMBOS.","Slice 3 or more in one stroke for a juicy bonus.");
            HelpRow(p,3,331,"THOSE BOMBS ARE TROUBLE.","Classic: game over. Arcade: lose 10 points. Zen: relax!");
            Label(p,"Tip","TIP: Glowing Arcade fruit brings freeze or a fruit frenzy.",45,438,670,40,19,Hex("70502B"),TextAnchor.MiddleCenter,false);
            ActionButton(p,"GOT IT!",240,500,280,58,Green,CloseModal);
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
            ActionButton(p,"BACK TO THE GARDEN",155,480,410,61,Green,CloseModal);
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
            ActionButton(p,"LOOKING SHARP!",200,392,350,61,Green,CloseModal);
        }
        void ShowSettings()
        {
            var p=OpenModal("MAKE YOURSELF AT HOME",690,460);
            Label(p,"Sound label","SPLASHES & SWISHES",60,132,390,44,25,Ink,TextAnchor.MiddleLeft);
            ActionButton(p,soundOn?"ON":"OFF",490,130,135,49,soundOn?Green:Paper,()=>{ soundOn=!soundOn;SaveSettings();ShowSettings(); });
            Label(p,"Music label","GARDEN MELODY",60,217,390,44,25,Ink,TextAnchor.MiddleLeft);
            ActionButton(p,musicOn?"ON":"OFF",490,215,135,49,musicOn?Green:Paper,()=>{ musicOn=!musicOn;SaveSettings();ShowSettings(); });
            Label(p,"Control hint","Mouse or touch to slice  ·  Esc / P to pause",40,304,610,36,19,Hex("795D36"),TextAnchor.MiddleCenter,false);
            ActionButton(p,"ALL SET!",195,367,300,59,Green,CloseModal);
        }
        RectTransform OpenModal(string title,float w,float h)
        {
            CloseModal();modal=Rect(root,"Overlay",0,0,1600,900);
            var dim=Fill(modal,"Dim garden",0,0,1600,900,new Color(.025f,.065f,.04f,.8f));dim.raycastTarget=true;
            var p=Board(modal,"Cardboard panel",800-w*.5f,450-h*.5f,w,h,Paper,-.7f,true);
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
                if(p.gravity) p.v.y+=950*dt;p.p+=p.v*dt;
                if(p.rect==null) { particles.RemoveAt(i);continue; }
                p.rect.anchoredPosition=new Vector2(p.p.x,-p.p.y);p.rect.Rotate(0,0,p.spin*dt);
                Color c=p.graphic.color;c.a=Mathf.Min(1,p.life/(p.maxLife*.5f));p.graphic.color=c;
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
        void Splash(Vector2 at,Color c,int count)
        {
            // Juice spatters linger while the larger drops follow their own ballistic arcs.
            for(int i=0;i<count;i++)
            {
                float radius=UnityEngine.Random.Range(5,19);float life=UnityEngine.Random.Range(.4f,.9f);
                var g=Circle(effectLayer,"Juice",at.x,at.y,radius,c);
                particles.Add(new Particle {rect=g.rectTransform,graphic=g,p=at,v=UnityEngine.Random.insideUnitCircle*UnityEngine.Random.Range(130,470)+new Vector2(0,-150),life=life,maxLife=life,gravity=true,spin=UnityEngine.Random.Range(-180,180)});
            }
            for(int i=0;i<7;i++)
            {
                Vector2 p=at+UnityEngine.Random.insideUnitCircle*70;Color stain=c;stain.a=.5f;
                var g=Circle(effectLayer,"Juice stain",p.x,p.y,UnityEngine.Random.Range(11,38),stain);
                particles.Add(new Particle {rect=g.rectTransform,graphic=g,p=p,life=2,maxLife=2});
            }
        }
        void SpawnLeaf()
        {
            var g=Circle(menu,"Falling petal",UnityEngine.Random.Range(90,1510),-20,10,Hex("C8CF6B"));
            g.rectTransform.sizeDelta=new Vector2(16,7);
            particles.Add(new Particle {rect=g.rectTransform,graphic=g,p=new Vector2(g.rectTransform.anchoredPosition.x,20),v=new Vector2(UnityEngine.Random.Range(-25,25),UnityEngine.Random.Range(40,70)),spin=80,life=16,maxLife=16});
        }
        void Float(string text,Vector2 p,Color color,int size)
        {
            Text t=Label(effectLayer,"Floating score",text,p.x-400,p.y-35,800,70,size,color,TextAnchor.MiddleCenter,true,2.5f);
            floats.Add(new FloatText {rect=t.rectTransform,text=t,p=new Vector2(p.x,p.y)});
        }
        void ClearRound()
        {
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

        // UI construction helpers. All layout uses a fixed 1600 × 900 design surface.
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
            Label(r,"Caption",text,0,0,w,h,18,Ink);var b=r.gameObject.AddComponent<PaperButton>();b.action=()=>{ Play(clickClip,.4f);action(); };
        }
        void ActionButton(Transform p,string text,float x,float y,float w,float h,Color color,Action action)
        {
            var r=Board(p,text,x,y,w,h,color,-.35f,false);Label(r,"Caption",text,7,2,w-14,h-8,text.Length>23?19:w<100?21:25,color==Green?Cream:Ink);
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
            string pauseHit=ClickDesign(new Vector2(1490,70));bool pauseClick=state==ScreenState.Paused;
            yield return null;Canvas.ForceUpdateCanvases();
            ClickDesign(new Vector2(800,466));bool resumeClick=state==ScreenState.Playing;
            ShowMenu();yield return null;Canvas.ForceUpdateCanvases();
            ClickDesign(new Vector2(963,845));bool settingsClick=modal!=null;
            yield return null;Canvas.ForceUpdateCanvases();
            bool before=soundOn;ClickDesign(new Vector2(1012,374));bool soundClick=soundOn!=before;
            yield return null;Canvas.ForceUpdateCanvases();ClickDesign(new Vector2(1012,374));
            ShowMenu();
            done("ModeCard="+modeClick+" PauseButton="+pauseClick+" ResumeButton="+resumeClick+" SettingsButton="+settingsClick+" SoundToggle="+soundClick+" SettingsRestored="+(before==soundOn)+"\nCard hit: "+cardHit+"\nPause hit: "+pauseHit);
        }
        string ClickDesign(Vector2 p)
        {
            var pointer=new PointerEventData(EventSystem.current) { position=new Vector2((p.x-800)*uiScale+Screen.width*.5f,(450-p.y)*uiScale+Screen.height*.5f),button=PointerEventData.InputButton.Left };
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
