using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace BloomDoom
{
    /// <summary>The single native Unity game. Editor Play and WebGL execute this same component.</summary>
    public sealed class BloomDoomGame : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int BloomDoomPlayBlocked();
#endif
        enum Overlay { None,Home,Pause,Days,Settings,Help,Guide,Result }
        sealed class Actor {public RectTransform Root;public Image Body,Shadow,Health,HealthBack;}
        sealed class Puff {public RectTransform Rect;public Graphic Graphic;public Vector2 Origin,Velocity;public float Age,Life;}
        public GardenSimulation Simulation {get;private set;}
        public bool HasStarted=>Simulation!=null&&Simulation.Phase!=GardenPhase.Ready;
        RectTransform stage,field,actors,particles,modal;Image scenery,hoverImage;Font display,body;
        Text sunText,waveText,waveDetail,chapterText,gardenText,hintText,announcement,speedText,soundText,locationText;Image progress;
        readonly Image[] cards=new Image[8],cooldowns=new Image[8];readonly Text[] recharge=new Text[8];
        readonly Dictionary<int,Actor> views=new Dictionary<int,Actor>();readonly List<Puff> puffs=new List<Puff>();
        readonly HashSet<int> shown=new HashSet<int>();readonly int[] stars=new int[6];
        readonly List<GardenPlant> demoPlants=new List<GardenPlant>();readonly List<GardenGuest> demoGuests=new List<GardenGuest>();
        GardenAudio sound;Overlay overlay=Overlay.Home,returnOverlay=Overlay.Home;int selected=-1,hoverRow=-1,hoverCol=-1,speed=1,unlocked;bool gentle;
        float accumulator,noticeTime,uiTimer,animTime;string sceneryName="";bool initialized;
        static readonly Color Ink=GardenAssets.Color("#454d36"),Paper=GardenAssets.Color("#f6f1df"),Cream=GardenAssets.Color("#fff9e9"),Green=GardenAssets.Color("#799a4e"),Muted=GardenAssets.Color("#737e5b");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureGame(){if(FindFirstObjectByType<BloomDoomGame>()==null)new GameObject("Bloom & Doom").AddComponent<BloomDoomGame>();}
        void Awake(){Initialize();}
        public void Initialize()
        {
            if(initialized)return;initialized=true;Application.targetFrameRate=60;
            new GameObject("Bloom & Doom - Loading Screen",typeof(RectTransform)).AddComponent<GardenLoadingScreen>();
            Simulation=new GardenSimulation();Simulation.Event+=OnGameEvent;Simulation.Effect+=SpawnEffect;
            sound=gameObject.AddComponent<GardenAudio>();display=Resources.Load<Font>("BloomDoom/Fonts/LilitaOne-Regular");body=Resources.Load<Font>("BloomDoom/Fonts/Nunito");
            if(display==null)display=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");if(body==null)body=display;
            gentle=PlayerPrefs.GetInt("BloomDoom.Gentle",0)==1;unlocked=Mathf.Clamp(PlayerPrefs.GetInt("BloomDoom.Unlocked",0),0,5);for(int i=0;i<6;i++)stars[i]=Mathf.Clamp(PlayerPrefs.GetInt("BloomDoom.Stars."+i,0),0,3);
            var camera=Camera.main;if(camera!=null){camera.backgroundColor=Paper;camera.clearFlags=CameraClearFlags.SolidColor;if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();}
            if(FindFirstObjectByType<EventSystem>()==null){var events=new GameObject("Garden Input");events.transform.SetParent(transform,false);events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>();}
            CreateUI();CreateDemo();ShowHome();
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;}
        Image Box(Transform parent,string name,float x,float y,float width,float height,Color color,bool rounded=true,bool raycast=false)
        {var r=Rect(parent,name,x,y,width,height);var image=r.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;if(rounded){image.sprite=GardenAssets.Rounded;image.type=Image.Type.Sliced;}return image;}
        Image Picture(Transform parent,string name,string sprite,float x,float y,float width,float height)
        {var image=Box(parent,name,x,y,width,height,Color.white,false);image.sprite=GardenAssets.Get(sprite);image.preserveAspect=true;return image;}
        Text Label(Transform parent,string name,string value,float x,float y,float width,float height,int size,Color color,bool title=false,TextAnchor align=TextAnchor.MiddleLeft)
        {var r=Rect(parent,name,x,y,width,height);var text=r.gameObject.AddComponent<Text>();text.text=value;text.font=title?display:body;text.fontSize=size;text.fontStyle=title?FontStyle.Normal:FontStyle.Bold;text.color=color;text.alignment=align;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;return text;}
        Button Button(Transform parent,string title,float x,float y,float width,float height,Action action,bool primary=false)
        {
            Box(parent,title+" shadow",x,y+4,width,height,primary?GardenAssets.Color("#526c3e"):GardenAssets.Color("#c5ceaa"));
            var image=Box(parent,title,x,y,width,height,primary?Green:GardenAssets.Color("#edf0d8"),true,true);
            var button=TouchButton(image);
            var label=Label(image.transform,"Label",title,5,2,width-10,height-4,primary?30:22,primary?Cream:Ink,true,TextAnchor.MiddleCenter).rectTransform;
            label.anchorMin=Vector2.zero;
            label.anchorMax=Vector2.one;
            label.offsetMin=new Vector2(5,2);
            label.offsetMax=new Vector2(-5,-2);
            button.onClick.AddListener(()=>{sound.Begin();sound.Play("ui");EventSystem.current?.SetSelectedGameObject(null);action();});return button;
        }
        static Button TouchButton(Image image)
        {
            var button=image.gameObject.AddComponent<Button>();
            button.targetGraphic=image;
            var colors=button.colors;
            colors.highlightedColor=new Color(1.08f,1.08f,.98f);
            colors.pressedColor=new Color(.76f,.83f,.62f);
            colors.disabledColor=new Color(.7f,.72f,.63f,.45f);
            colors.fadeDuration=.05f;
            button.colors=colors;
            return button;
        }
        void CreateUI()
        {
            var root=new GameObject("Bloom & Doom - Native Unity UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1080);scaler.matchWidthOrHeight=.5f;
            var background=Box(root.transform,"Paper backdrop",0,0,1600,1080,Paper,false);background.rectTransform.anchorMin=Vector2.zero;background.rectTransform.anchorMax=Vector2.one;background.rectTransform.offsetMin=background.rectTransform.offsetMax=Vector2.zero;
            stage=Rect(root.transform,"Garden stage",0,0,1480,1040);stage.anchorMin=stage.anchorMax=stage.pivot=new Vector2(.5f,.5f);stage.anchoredPosition=Vector2.zero;
            Picture(stage,"Brand flower","icon_sunbud",10,5,74,78);Label(stage,"Brand","BLOOM & DOOM",90,6,470,50,43,Ink,true);Label(stage,"Brand tagline","A   B A C K Y A R D   B A T T L E",94,59,430,24,12,Muted);
            Label(stage,"Afternoon","A little sunshine. A lot of trouble.",635,23,420,44,19,Muted,false,TextAnchor.MiddleCenter);
            var mute=Button(stage,"SOUND",1118,18,103,45,()=>{sound.SetMuted(!sound.Muted);soundText.text=sound.Muted?"MUTED":"SOUND";});soundText=mute.GetComponentInChildren<Text>();
            Button(stage,"?",1236,18,55,45,()=>{RememberReturn();ShowHelp();});Button(stage,"SETTINGS",1308,18,152,45,()=>{RememberReturn();ShowSettings();});
            Box(stage,"Frame shadow",0,106,1480,910,GardenAssets.Color("#d3d2b3"));Box(stage,"Frame outline",0,100,1480,910,Ink);Box(stage,"Game frame",2,102,1476,906,Cream);
            Box(stage,"Chapter strip",5,105,1470,42,GardenAssets.Color("#eeefda"));chapterText=Label(stage,"Chapter","ADVENTURE  /  DAY 01",28,111,390,30,14,Green,true);
            gardenText=Label(stage,"Garden title","The front yard",480,111,490,30,16,Muted);Label(stage,"Motto","HOMEGROWN HEROES.  UNINVITED GUESTS.",1000,111,440,30,11,Muted,false,TextAnchor.MiddleRight);
            Box(stage,"Sun border",24,161,104,91,GardenAssets.Color("#d7ba68"));Box(stage,"Sun bank",26,163,100,85,GardenAssets.Color("#f4e3a3"));Picture(stage,"Sun icon","sun",29,174,35,35);sunText=Label(stage,"Sunshine","200",61,169,62,48,30,Ink,true,TextAnchor.MiddleCenter);sunText.resizeTextForBestFit=true;sunText.resizeTextMinSize=14;sunText.resizeTextMaxSize=30;Label(stage,"Sunshine caption","SUNSHINE",35,219,84,19,10,GardenAssets.Color("#9b823f"),true,TextAnchor.MiddleCenter);
            for(int i=0;i<8;i++){
                int kind=i;float x=146+i*105;Box(stage,"Seed shadow",x,158,94,101,GardenAssets.Color("#ced5b4"));cards[i]=Box(stage,"Seed "+GardenData.Seeds[i].Name,x,155,94,101,GardenAssets.Color("#edf0d8"),true,true);var b=TouchButton(cards[i]);b.onClick.AddListener(()=>{sound.Begin();Select(kind);EventSystem.current?.SetSelectedGameObject(null);});
                Picture(cards[i].transform,"Plant portrait","icon_"+GardenData.Seeds[i].Id,17,0,62,63);Label(cards[i].transform,"Key",(i+1).ToString(),6,3,15,16,10,Muted);
                Label(cards[i].transform,"Plant name",GardenData.Seeds[i].Name,2,61,90,18,12,Ink,true,TextAnchor.MiddleCenter);Label(cards[i].transform,"Cost",GardenData.Seeds[i].Cost+" sun",2,80,90,18,14,Green,true,TextAnchor.MiddleCenter);
                cooldowns[i]=Box(cards[i].transform,"Recharge shade",0,0,94,101,new Color(.28f,.37f,.2f,.38f));cooldowns[i].type=Image.Type.Filled;cooldowns[i].fillMethod=Image.FillMethod.Vertical;cooldowns[i].fillOrigin=1;
                recharge[i]=Label(cards[i].transform,"Recharge time","",0,21,94,42,29,Cream,true,TextAnchor.MiddleCenter);
            }
            var shovel=Button(stage,"",1006,161,70,88,()=>Select(8));Picture(shovel.transform,"Shovel","shovel",12,3,48,56);Label(shovel.transform,"Shovel label","SHOVEL",0,61,70,18,10,Muted,true,TextAnchor.MiddleCenter);
            waveText=Label(stage,"Wave heading","MAKE YOURSELF AT HOME",1110,164,335,28,17,Ink,true);Box(stage,"Wave track",1110,204,330,10,GardenAssets.Color("#e0e5c9"));progress=Box(stage,"Wave progress",1110,204,0,10,Green);waveDetail=Label(stage,"Wave detail","Your garden adventure starts here",1110,224,335,24,12,Muted);
            field=Rect(stage,"The garden - five lanes, nine patches",20,270,1440,700);field.gameObject.AddComponent<RectMask2D>();scenery=Picture(field,"Illustrated garden","garden_day",0,0,1440,700);scenery.raycastTarget=true;
            var pointer=scenery.gameObject.AddComponent<GardenPointer>();pointer.Point=Point;pointer.Leave=()=>hoverRow=hoverCol=-1;
            hoverImage=Box(field,"Planting patch",0,0,106,96,new Color(.98f,.94f,.65f,.35f));hoverImage.gameObject.SetActive(false);
            actors=Rect(field,"Animated garden residents",0,0,1440,700);particles=Rect(field,"Combat particles",0,0,1440,700);
            locationText=Label(field,"Location badge","THE FRONT YARD",21,20,430,32,16,Cream,true);
            speedText=Button(field,"1x",1288,638,55,42,()=>{if(overlay!=Overlay.None)return;speed=speed==1?2:1;speedText.text=speed+"x";}).GetComponentInChildren<Text>();Button(field,"II",1358,638,55,42,()=>{if(overlay==Overlay.None)ShowPause();else if(overlay==Overlay.Pause)Close();});
            announcement=Label(field,"Announcements","",120,235,1200,120,48,Cream,true,TextAnchor.MiddleCenter);var outline=announcement.gameObject.AddComponent<Outline>();outline.effectColor=Ink;outline.effectDistance=new Vector2(2,-2);
            modal=Rect(field,"Menus",0,0,1440,700);
            hintText=Label(stage,"Garden tip","Good things come to those who plant.",29,970,1180,34,15,Muted);Label(stage,"Footer","GROW A LITTLE COURAGE.       1 - 8: SEEDS    /    S: SHOVEL    /    C: SUNS    /    ESC: PAUSE",22,1011,1436,28,12,Muted,false,TextAnchor.MiddleCenter);
            gameObject.AddComponent<GardenResponsiveLayout>().Initialize(stage,field,actors,particles,modal);
        }
        void CreateDemo()
        {
            int[,] defs={{0,0,0},{0,2,0},{0,4,0},{1,1,1},{1,3,1},{1,0,2},{3,4,3},{2,1,5},{2,3,6},{5,2,4}};
            for(int i=0;i<defs.GetLength(0);i++){var pos=GardenData.Center(defs[i,1],defs[i,2]);demoPlants.Add(new GardenPlant{Uid=-i-1,Kind=defs[i,0],Row=defs[i,1],Col=defs[i,2],X=pos.x,Y=pos.y,Age=10,Health=100,MaxHealth=100,Armed=true});}
            int[,] guests={{0,0,1187},{1,1,1286},{0,3,1171},{2,4,1328},{3,2,1375}};for(int i=0;i<guests.GetLength(0);i++)demoGuests.Add(new GardenGuest{Uid=-30-i,Kind=guests[i,0],Row=guests[i,1],X=guests[i,2],Y=GardenData.Center(guests[i,1],0).y,Health=100,MaxHealth=100});
        }
        void Select(int kind){if(overlay!=Overlay.None)return;selected=selected==kind?-1:kind;sound.Play("select");hintText.text=selected==8?"Tap a plant to dig it up. Right-click to put the shovel away.":selected>=0?GardenData.Seeds[selected].Description:Simulation.Configuration.Tip;}
        void Point(Vector2 point,bool click)
        {
            if(overlay!=Overlay.None)return;
            if(GardenData.Cell(point,out int row,out int col)){hoverRow=row;hoverCol=col;}else hoverRow=hoverCol=-1;
            if(!click)return;sound.Begin();var sun=Simulation.Suns.LastOrDefault(s=>!s.Collected&&Vector2.Distance(new Vector2(s.X,s.Y),point)<42);if(sun!=null){Simulation.CollectSun(sun.Uid);return;}PlantAtHover();
        }
        void PlantAtHover()
        {
            if(overlay!=Overlay.None||hoverRow<0)return;
            if(selected==8){if(Simulation.Dig(hoverRow,hoverCol))selected=-1;return;}
            if(selected<0){Notice("Choose a seed packet, then a patch.");return;}
            if(!Simulation.Place(selected,hoverRow,hoverCol,out string reason)){Notice(reason);sound.Play("error");}
        }
        void OnGameEvent(string type,string text)
        {
            sound.Play(type);if(!string.IsNullOrEmpty(text))Notice(text);
            if(type=="mower")Notice("Last line of lawn defense!");
            if(type=="win"){stars[Simulation.Day]=Mathf.Max(stars[Simulation.Day],Simulation.Stars);unlocked=Mathf.Max(unlocked,Mathf.Min(5,Simulation.Day+1));PlayerPrefs.SetInt("BloomDoom.Unlocked",unlocked);PlayerPrefs.SetInt("BloomDoom.Stars."+Simulation.Day,stars[Simulation.Day]);PlayerPrefs.SetInt("BloomDoom.Best",Mathf.Max(PlayerPrefs.GetInt("BloomDoom.Best",0),Simulation.Score));PlayerPrefs.Save();ShowResult(true);}
            if(type=="lose"){if(Simulation.Endless)PlayerPrefs.SetInt("BloomDoom.EndlessBest",Mathf.Max(PlayerPrefs.GetInt("BloomDoom.EndlessBest",0),Simulation.Wave));PlayerPrefs.Save();ShowResult(false);}
        }
        void Notice(string text){announcement.text=text;noticeTime=2.5f;}
        public void StartDay(int day,bool endless=false)
        {
            ClearResidents();Simulation.Reset(day,endless);selected=0;speed=1;speedText.text="1x";hoverRow=hoverCol=-1;accumulator=0;Close();Simulation.Start();sound.Begin();chapterText.text=endless?"SURVIVAL  /  ENDLESS GARDEN":"ADVENTURE  /  DAY "+(day+1).ToString("00");gardenText.text=Simulation.Configuration.Name;locationText.text=Simulation.Configuration.Name.ToUpperInvariant();hintText.text=Simulation.Configuration.Tip;Notice("Ready. Set. Grow!");
        }
        void Update()
        {
            if(!initialized)return;float dt=Mathf.Min(.15f,Time.unscaledDeltaTime);animTime+=dt;
#if UNITY_WEBGL && !UNITY_EDITOR
            if(overlay==Overlay.None && Simulation.Running && BloomDoomPlayBlocked()!=0)ShowPause();
#endif
            HandleKeyboard();if(overlay==Overlay.None){accumulator+=dt*speed;for(int i=0;accumulator>=1/60f&&i<20;i++){Simulation.Tick(1/60f);accumulator-=1/60f;}}else accumulator=0;
            if(noticeTime>0){noticeTime-=dt;if(noticeTime<=0)announcement.text="";}
            RenderResidents();UpdatePuffs(overlay==Overlay.None||overlay==Overlay.Home?dt:0);uiTimer-=dt;if(uiTimer<=0){UpdateHud();uiTimer=.08f;}
        }
        void HandleKeyboard()
        {
            var k=Keyboard.current;if(k==null)return;
            if(k.escapeKey.wasPressedThisFrame){if(overlay==Overlay.None)ShowPause();else if(overlay==Overlay.Pause)Close();else if(overlay!=Overlay.Home&&overlay!=Overlay.Result)Back();return;}
            if(overlay!=Overlay.None)return;
            Key[] keys={Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8};for(int i=0;i<8;i++)if(k[keys[i]].wasPressedThisFrame)Select(i);
            if(k.sKey.wasPressedThisFrame)Select(8);if(k.cKey.wasPressedThisFrame)Simulation.CollectAll();if(k.spaceKey.wasPressedThisFrame)Simulation.SendWave();if(Mouse.current!=null&&Mouse.current.rightButton.wasPressedThisFrame)selected=-1;
            if(k.leftArrowKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame||k.upArrowKey.wasPressedThisFrame||k.downArrowKey.wasPressedThisFrame){if(hoverRow<0){hoverRow=2;hoverCol=0;}if(k.leftArrowKey.wasPressedThisFrame)hoverCol=Mathf.Max(0,hoverCol-1);if(k.rightArrowKey.wasPressedThisFrame)hoverCol=Mathf.Min(8,hoverCol+1);if(k.upArrowKey.wasPressedThisFrame)hoverRow=Mathf.Max(0,hoverRow-1);if(k.downArrowKey.wasPressedThisFrame)hoverRow=Mathf.Min(4,hoverRow+1);}
            if(k.enterKey.wasPressedThisFrame)PlantAtHover();
        }
        void UpdateHud()
        {
            sunText.text=Simulation.Sunshine.ToString();soundText.text=sound.Muted?"MUTED":"SOUND";
            for(int i=0;i<8;i++){cards[i].color=selected==i&&overlay==Overlay.None?GardenAssets.Color("#d1e3a7"):Simulation.Sunshine<GardenData.Seeds[i].Cost?GardenAssets.Color("#dedfcf"):GardenAssets.Color("#edf0d8");cooldowns[i].fillAmount=Simulation.Cooldowns[i]/GardenData.Seeds[i].Cooldown;recharge[i].text=Simulation.Cooldowns[i]>.1f?Mathf.CeilToInt(Simulation.Cooldowns[i]).ToString():"";}
            if(Simulation.Phase==GardenPhase.Ready){waveText.text="MAKE YOURSELF AT HOME";waveDetail.text="Your garden adventure starts here";}else{
                waveText.text="WAVE "+Mathf.Max(1,Simulation.Wave)+"  /  "+(Simulation.Endless?"ENDLESS":Simulation.Configuration.Waves.ToString());
                waveDetail.text=Simulation.Phase==GardenPhase.Preparation||Simulation.Phase==GardenPhase.Intermission?"Next wave in "+Mathf.CeilToInt(Simulation.Countdown)+"s - Space to send":Simulation.Guests.Count+Simulation.Waiting+" uninvited guests remaining";
            }
            float fraction=Simulation.Wave==0?0:((Simulation.Wave-1)+Simulation.WaveSpawned/(float)Mathf.Max(1,Simulation.WaveTotal))/(Simulation.Endless?Simulation.Wave+1:Simulation.Configuration.Waves);progress.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,(stage.rect.width>2000?550:330)*Mathf.Clamp01(fraction));
        }
        void RememberReturn(){if(overlay==Overlay.Home||!Simulation.Running)returnOverlay=Overlay.Home;else if(overlay==Overlay.Pause)returnOverlay=Overlay.Pause;else if(overlay==Overlay.None)returnOverlay=Overlay.None;}
        void Close(){ClearModal();overlay=Overlay.None;sound.Pause(false);}
        void Back(){if(returnOverlay==Overlay.Home)ShowHome();else if(returnOverlay==Overlay.Pause)ShowPause();else Close();}
        void ClearModal(){for(int i=modal.childCount-1;i>=0;i--){var child=modal.GetChild(i).gameObject;child.SetActive(false);Destroy(child);}}
        RectTransform Menu(Overlay which,string kicker,string title,float width,float height,bool close=true)
        {
            ClearModal();overlay=which;sound.Pause(which!=Overlay.Home&&which!=Overlay.Result);Box(modal,"Menu veil",0,0,1440,700,new Color(.17f,.25f,.12f,.38f),false,true);
            float x=(1440-width)/2,y=(700-height)/2;Box(modal,"Card shadow",x,y+7,width,height,new Color(.16f,.23f,.12f,.25f));var border=Box(modal,"Menu card outline",x,y,width,height,Ink);var content=Box(border.transform,"Menu card",2,2,width-4,height-4,Cream).rectTransform;
            Label(content,"Kicker",kicker,22,20,width-44,25,12,Muted,true,TextAnchor.MiddleCenter);Label(content,"Title",title,20,48,width-40,94,40,Ink,true,TextAnchor.MiddleCenter);
            if(close)Button(content,"X",width-83,12,65,60,Back);return content;
        }
        void ShowHome()
        {
            var p=Menu(Overlay.Home,"WELCOME TO YOUR NEW OBSESSION","Your garden. Their bad day.",720,540,false);
            Picture(p,"Sprout","icon_sprout",191,132,108,112);Picture(p,"Sunbud","icon_sunbud",302,117,126,132);Picture(p,"Brave little acorn","icon_nut",426,132,108,112);
            Label(p,"Welcome","Grow a lovable little army. Keep the uninvited out.",34,250,648,40,22,Muted,false,TextAnchor.MiddleCenter);
            Button(p,"LET'S GET GROWING  >",34,304,648,88,()=>StartDay(unlocked),true);
            Button(p,"CHOOSE A DAY",34,405,314,58,()=>{returnOverlay=Overlay.Home;ShowDays();});Button(p,"ENDLESS GARDEN",368,405,314,58,()=>StartDay(2,true));
            Button(p,"FIELD GUIDE",34,472,314,58,()=>{returnOverlay=Overlay.Home;ShowGuide();});Button(p,"HOW TO PLAY",368,472,314,58,()=>{returnOverlay=Overlay.Home;ShowHelp();});
        }
        void ShowPause()
        {
            if(!Simulation.Running)return;var p=Menu(Overlay.Pause,"EVEN GARDENERS NEED A TEA BREAK","Smell the sunflowers.",660,415,false);
            Label(p,"Pause message","Your little friends will wait right here.\nThe zombies, too. Very considerate of them.",30,137,596,65,22,Muted,false,TextAnchor.MiddleCenter);
            Button(p,"BACK TO THE BACKYARD  >",30,216,596,88,Close,true);
            Button(p,"RESTART",30,320,184,72,()=>StartDay(Simulation.Day,Simulation.Endless));
            Button(p,"SETTINGS",236,320,184,72,()=>{returnOverlay=Overlay.Pause;ShowSettings();});
            Button(p,"MAIN MENU",442,320,184,72,ShowHome);
        }
        void ShowDays()
        {
            var p=Menu(Overlay.Days,"SIX DAYS. ONE DETERMINED GARDENER.","A backyard worth fighting for.",830,530);
            for(int i=0;i<6;i++){int day=i;float x=29+(i%3)*260,y=157+(i/3)*161;var b=Button(p,"",x,y,248,142,()=>StartDay(day));b.interactable=i<=unlocked;
                Label(b.transform,"Day number",(i+1).ToString("00"),17,9,60,35,29,Green,true);Label(b.transform,"Stars",i>unlocked?"LOCKED":new string('*',stars[i]),100,12,126,28,18,GardenAssets.Color("#bd9b41"),true,TextAnchor.MiddleRight);
                Label(b.transform,"Garden",GardenData.Days[i].Name,17,49,216,53,23,Ink,true);Label(b.transform,"Time of day",GardenData.Days[i].Theme.ToUpper()+"  /  "+GardenData.Days[i].Waves+" WAVES",17,109,216,20,12,Muted);
            }
            Label(p,"Best","PERSONAL BEST: "+PlayerPrefs.GetInt("BloomDoom.Best",0)+" GARDEN POINTS",20,492,786,24,12,Muted,true,TextAnchor.MiddleCenter);
        }
        void ShowHelp()
        {
            var p=Menu(Overlay.Help,"A GARDENER'S POCKET HANDBOOK","A little dirt. A little strategy.",850,530);string[] titles={"1. Catch some rays","2. Pick. Plant. Protect.","3. Hold your ground"};string[] descriptions={"Tap sunshine to collect it. Every sun is worth 25. Sunbuds grow more for you.","Pick a seed, then tap the lawn. Shooters defend their own lane. Cover all five!","Shellnuts buy you time. Each lane has one emergency mower. Keep the house safe."};string[] ids={"sunbud","sprout","nut"};
            for(int i=0;i<3;i++){float x=29+i*270;Picture(p,"Helper", "icon_"+ids[i],x+70,150,100,104);Label(p,"How to",titles[i],x,264,248,40,23,Ink,true,TextAnchor.MiddleCenter);Label(p,"Instruction",descriptions[i],x+8,313,232,95,18,Muted,false,TextAnchor.UpperCenter);}
            Button(p,"I'VE GOT A GREEN THUMB  >",193,415,460,72,Back,true);Label(p,"Controls","1-8: seeds  /  S: shovel  /  C: collect suns  /  Space: send wave  /  Esc: pause",22,495,802,24,12,Muted,false,TextAnchor.MiddleCenter);
        }
        void ShowGuide()
        {
            var p=Menu(Overlay.Guide,"GET TO KNOW THE LOCALS","Small sprouts. Big personalities.",1050,610);
            for(int i=0;i<8;i++){float x=25+(i%4)*252,y=149+(i/4)*216;var entry=Box(p,"Field guide entry",x,y,242,202,GardenAssets.Color("#edf0d8"));Picture(entry.transform,"Portrait","icon_"+GardenData.Seeds[i].Id,80,4,82,82);Label(entry.transform,"Name",GardenData.Seeds[i].Name,9,87,224,30,24,Ink,true,TextAnchor.MiddleCenter);Label(entry.transform,"Cost",GardenData.Seeds[i].Cost+" SUNSHINE",9,120,224,22,12,Green,true,TextAnchor.MiddleCenter);Label(entry.transform,"Description",GardenData.Seeds[i].Description,14,147,214,48,14,Muted,false,TextAnchor.UpperCenter);}
        }
        void ShowSettings()
        {
            var p=Menu(Overlay.Settings,"JUST THE WAY YOU GROW IT","Garden settings",620,542);
            Label(p,"Music","Garden soundtrack: "+Mathf.RoundToInt(sound.MusicVolume*100)+"%",28,145,385,64,22,Muted);
            Button(p,"-",423,145,72,64,()=>{sound.SetMusic(sound.MusicVolume-.1f);ShowSettings();});
            Button(p,"+",508,145,72,64,()=>{sound.SetMusic(sound.MusicVolume+.1f);ShowSettings();});
            Label(p,"Effects","Pops and crunches: "+Mathf.RoundToInt(sound.EffectsVolume*100)+"%",28,220,385,64,22,Muted);
            Button(p,"-",423,220,72,64,()=>{sound.SetEffects(sound.EffectsVolume-.1f);ShowSettings();});
            Button(p,"+",508,220,72,64,()=>{sound.SetEffects(sound.EffectsVolume+.1f);ShowSettings();});
            Label(p,"Sound","All sound",28,295,385,64,22,Muted);
            Button(p,sound.Muted?"OFF":"ON",423,295,157,64,()=>{sound.SetMuted(!sound.Muted);ShowSettings();});
            Label(p,"Motion","Gentle animations",28,370,385,64,22,Muted);
            Button(p,gentle?"ON":"OFF",423,370,157,64,()=>{gentle=!gentle;PlayerPrefs.SetInt("BloomDoom.Gentle",gentle?1:0);ShowSettings();});
            Button(p,"ALL SET  >",28,448,552,76,Back,true);
        }
        void ShowResult(bool won)
        {
            var p=Menu(Overlay.Result,won?"NOT TODAY, UNDEAD. NOT TODAY.":"A LITTLE COMPOST. A FRESH START.",won?"The lawn lives on!":"They got the garden.",540,465,false);
            Label(p,"Summary",won?new string('*',Simulation.Stars)+"  A beautiful bit of gardening.  "+new string('*',Simulation.Stars):"Grow more Sunbuds and cover every lane.\nEvery good gardener gets another chance.",30,145,476,66,19,Muted,false,TextAnchor.MiddleCenter);
            Label(p,"Results",Simulation.Kills+" GUESTS EVICTED     "+Simulation.Score+" GARDEN POINTS",20,237,496,38,19,Green,true,TextAnchor.MiddleCenter);
            Button(p,won?Simulation.Day==5?"TRY THE ENDLESS GARDEN":"ON TO THE NEXT DAY  >":"GIVE IT ANOTHER GROW  >",30,301,476,56,()=>{if(won){if(Simulation.Day==5)StartDay(2,true);else StartDay(Simulation.Day+1);}else StartDay(Simulation.Day,Simulation.Endless);},true);
            Button(p,"CHOOSE A DAY",30,388,230,41,()=>{returnOverlay=Overlay.Home;ShowDays();});Button(p,"MAIN MENU",276,388,230,41,ShowHome);
        }
        Actor View(int uid)
        {
            shown.Add(uid);if(views.TryGetValue(uid,out var actor))return actor;var root=Rect(actors,"Resident "+uid,0,0,256,256);
            var shadow=Box(root,"Ground shadow",97,160,62,16,new Color(.13f,.25f,.12f,.17f),false);shadow.sprite=GardenAssets.Circle;
            var image=Box(root,"Animated illustration",0,0,256,256,Color.white,false);image.preserveAspect=true;
            var healthBack=Box(root,"Health track",109,53,38,4,new Color(.2f,.32f,.17f,.3f));var health=Box(root,"Health",109,53,38,4,GardenAssets.Color("#e8d78b"));actor=new Actor{Root=root,Body=image,Shadow=shadow,HealthBack=healthBack,Health=health};views[uid]=actor;return actor;
        }
        void RenderResidents()
        {
            bool demo=overlay==Overlay.Home||Simulation.Phase==GardenPhase.Ready;string theme=demo?"day":Simulation.Configuration.Theme;if(sceneryName!=theme){sceneryName=theme;scenery.sprite=GardenAssets.Get("garden_"+theme);}
            shown.Clear();var ps=demo?demoPlants:Simulation.Plants;var zs=demo?demoGuests:Simulation.Guests;
            for(int row=0;row<5;row++){
                var mower=Simulation.Mowers[row];if(demo||!mower.Spent){var v=View(-100-row);v.Root.anchoredPosition=new Vector2((demo?170:mower.X)-64,-GardenData.Center(row,0).y-14+64);v.Root.sizeDelta=new Vector2(128,128);v.Body.rectTransform.sizeDelta=new Vector2(128,128);v.Body.sprite=GardenAssets.Get("mower");v.Shadow.enabled=v.Health.enabled=v.HealthBack.enabled=false;}
                foreach(var p in ps.Where(p=>p.Row==row)){
                    var v=View(p.Uid);v.Root.SetAsLastSibling();v.Root.anchoredPosition=new Vector2(p.X-128,-p.Y+128);v.Root.sizeDelta=new Vector2(256,256);v.Body.rectTransform.sizeDelta=new Vector2(256,256);string id=GardenData.Seeds[p.Kind].Id;
                    if(p.Kind==2&&p.Health/p.MaxHealth<.65f)id=p.Health/p.MaxHealth<.3f?"nut_critical":"nut_damaged";if(p.Kind==5&&!p.Armed)id="mine_unarmed";
                    int frame=gentle?0:Mathf.FloorToInt((demo?animTime:p.Age)*6.6f)%16;v.Body.sprite=GardenAssets.Get("plant_"+id,frame);v.Body.color=p.Flash>0?new Color(1,.68f,.55f):Color.white;
                    float pop=Mathf.Clamp01(p.Age*5);v.Body.rectTransform.localScale=new Vector3((.7f+.3f*pop)*(1-p.Recoil*.07f),.65f+.35f*pop,1);v.Shadow.enabled=true;v.Shadow.rectTransform.anchoredPosition=new Vector2(97,-160);SetHealth(v,p.Health/p.MaxHealth,53);
                }
                foreach(var z in zs.Where(z=>z.Row==row)){
                    var v=View(z.Uid);v.Root.SetAsLastSibling();v.Root.anchoredPosition=new Vector2(z.X-128,-z.Y+170);v.Root.sizeDelta=new Vector2(256,256);v.Body.rectTransform.sizeDelta=new Vector2(256,256);
                    int frame=gentle?0:Mathf.FloorToInt((demo?animTime:z.Age)*(z.Kind==2?20:11.5f))%16;string id=z.Enraged?"basic":GardenData.GuestIds[z.Kind];v.Body.sprite=GardenAssets.Get("zombie_"+id+"_"+(z.Eating?"eat":"walk"),frame);v.Body.color=z.Flash>0?new Color(1,.65f,.55f):z.Slow>0?new Color(.7f,.9f,1):Color.white;v.Body.rectTransform.localScale=Vector3.one;v.Shadow.enabled=true;v.Shadow.rectTransform.anchoredPosition=new Vector2(97,-202);SetHealth(v,z.Health/z.MaxHealth,z.Kind==5?21:62);
                }
            }
            foreach(var b in Simulation.Peas){var v=View(b.Uid);v.Root.anchoredPosition=new Vector2(b.X-8,-b.Y+8);v.Root.sizeDelta=new Vector2(16,16);v.Body.rectTransform.sizeDelta=new Vector2(16,16);v.Body.sprite=GardenAssets.Get("pea");v.Body.color=b.Frost?new Color(.65f,.95f,1):Color.white;v.Shadow.enabled=v.Health.enabled=v.HealthBack.enabled=false;}
            foreach(var s in Simulation.Suns){var v=View(s.Uid);v.Root.SetAsLastSibling();v.Root.anchoredPosition=new Vector2(s.X-48,-s.Y+48);v.Root.sizeDelta=new Vector2(96,96);v.Body.rectTransform.sizeDelta=new Vector2(96,96);v.Body.sprite=GardenAssets.Get("sun");v.Body.color=new Color(1,1,1,s.Collected?1-s.FlightAge*1.5f:s.Life<3?.5f+Mathf.Sin(animTime*10)*.3f:1);v.Body.rectTransform.localRotation=Quaternion.Euler(0,0,gentle?0:Mathf.Sin(animTime*2+s.Uid)*8);v.Shadow.enabled=v.Health.enabled=v.HealthBack.enabled=false;}
            foreach(int key in views.Keys.Where(key=>!shown.Contains(key)).ToArray()){Destroy(views[key].Root.gameObject);views.Remove(key);}
            hoverImage.gameObject.SetActive(overlay==Overlay.None&&selected>=0&&hoverRow>=0);if(hoverRow>=0){hoverImage.rectTransform.anchoredPosition=new Vector2(229+112*hoverCol,-108-102*hoverRow);hoverImage.color=Simulation.At(hoverRow,hoverCol)!=null&&selected!=8?new Color(.9f,.55f,.4f,.4f):new Color(.98f,.94f,.65f,.35f);}
        }
        void SetHealth(Actor view,float fraction,float y){bool visible=fraction<.98f;view.Health.enabled=view.HealthBack.enabled=visible;view.Health.rectTransform.anchoredPosition=view.HealthBack.rectTransform.anchoredPosition=new Vector2(109,-y);view.Health.rectTransform.sizeDelta=new Vector2(38*Mathf.Clamp01(fraction),4);}
        void ClearResidents(){foreach(var v in views.Values)Destroy(v.Root.gameObject);views.Clear();foreach(var p in puffs)Destroy(p.Rect.gameObject);puffs.Clear();}
        void SpawnEffect(string kind,float x,float y,int row)
        {
            bool blast=kind=="explosion"||kind=="fire";int count=gentle?3:blast?30:kind=="death"?14:8;
            for(int i=0;i<count&&puffs.Count<220;i++){
                float angle=UnityEngine.Random.Range(0,Mathf.PI*2),velocity=UnityEngine.Random.Range(blast?80:20,blast?230:90),size=UnityEngine.Random.Range(blast?10:3,blast?34:9);float px=kind=="fire"?226+i/(float)count*1008:x;
                Color color=kind=="dirt"?GardenAssets.Color("#ac925e"):kind=="ice"?GardenAssets.Color("#c1e9e4"):blast?(i%2==0?GardenAssets.Color("#f0c274"):GardenAssets.Color("#f5e297")):GardenAssets.Color("#b9cf8b");
                var image=Box(particles,"Garden particle",px,y,size,size,color,false);image.sprite=GardenAssets.Circle;puffs.Add(new Puff{Rect=image.rectTransform,Graphic=image,Origin=new Vector2(px,y),Velocity=new Vector2(Mathf.Cos(angle)*velocity,Mathf.Sin(angle)*velocity-40),Life=blast?1.1f:.7f});
            }
            if(blast){var label=Label(particles,"POP!",kind=="fire"?"WHOOSH!":"POP!",x-150,y-75,300,70,52,Cream,true,TextAnchor.MiddleCenter);puffs.Add(new Puff{Rect=label.rectTransform,Graphic=label,Origin=new Vector2(x-150,y-75),Velocity=new Vector2(0,-35),Life=.9f});}
        }
        void UpdatePuffs(float dt)
        {
            for(int i=puffs.Count-1;i>=0;i--){var p=puffs[i];p.Age+=dt;float t=p.Age/p.Life;if(t>=1){Destroy(p.Rect.gameObject);puffs.RemoveAt(i);continue;}var pos=p.Origin+p.Velocity*p.Age+new Vector2(0,65*p.Age*p.Age);p.Rect.anchoredPosition=new Vector2(pos.x,-pos.y);var color=p.Graphic.color;color.a=1-t;p.Graphic.color=color;p.Rect.localScale=Vector3.one*(1-t*.5f);}
        }
        void OnApplicationFocus(bool focused){if(!focused&&initialized&&overlay==Overlay.None&&Simulation.Running)ShowPause();}
        void OnDestroy(){if(Simulation!=null){Simulation.Event-=OnGameEvent;Simulation.Effect-=SpawnEffect;}}
    }
}
