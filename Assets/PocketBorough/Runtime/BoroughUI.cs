using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace PocketBorough
{
    public class BoroughUI:MonoBehaviour
    {
        BoroughGame game;BoroughCatalog catalog;
        RectTransform safe;
        GameObject menu,hud,pause,results,settings,help,portrait;
        TMP_Text score,timer,rank,level,countdown,toast,best,skin,music,sfx,resultTitle,resultStats,youMarker;
        readonly TMP_Text[] leaderboard=new TMP_Text[8],resultRows=new TMP_Text[8];
        UnityEngine.UI.Image growth;
        RectTransform stick,stickKnob;
        readonly TMP_Text[] pops=new TMP_Text[12];
        readonly float[] popLife=new float[12];readonly Vector3[] popWorld=new Vector3[12];
        int nextPop;float toastLife,transition=1;RectTransform transitionPanel;
        readonly Color navy=MeshWorkshop.C("233D4B"),muted=MeshWorkshop.C("6F8387"),cream=MeshWorkshop.C("FFF8E9"),coral=MeshWorkshop.C("F58D73"),teal=MeshWorkshop.C("D9EBDF");
        int width,height;Action backFromModal;
        public void Initialize(BoroughGame owner,BoroughCatalog data)
        {
            game=owner;catalog=data;
            var canvasObj=new GameObject("Borough UI • responsive safe area",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasObj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=canvasObj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new(1280,720);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            safe=Rect("Safe area",canvasObj.transform,Vector2.zero,Vector2.zero,Vector2.zero);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.pivot=new(.5f,.5f);safe.offsetMin=safe.offsetMax=Vector2.zero;
            if(EventSystem.current==null)new GameObject("Input events",typeof(EventSystem),typeof(InputSystemUIInputModule));
            menu=CreateScreen("Main menu");hud=CreateScreen("Match HUD");pause=CreateScreen("Pause");results=CreateScreen("Results");settings=CreateScreen("Settings");help=CreateScreen("How to play");
            BuildMenu();BuildHUD();BuildPause();BuildResults();BuildSettings();BuildHelp();
            portrait=CreateScreen("Rotate device");var shade=Panel(portrait.transform,"Portrait curtain",new(.5f,.5f),Vector2.zero,new(1800,2100),navy);Text(shade,"PLEASE ROTATE",38,new(.5f,.5f),new(0,50),new(600,70),cream,TextAlignmentOptions.Center);Text(shade,"Pocket Borough plays best in landscape.",24,new(.5f,.5f),new(0,-30),new(600,70),cream,TextAlignmentOptions.Center);
            Resize();
        }
        GameObject CreateScreen(string name){var r=Rect(name,safe,new(.5f,.5f),Vector2.zero,Vector2.zero);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;return r.gameObject;}
        RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;}
        RectTransform Panel(Transform parent,string name,Vector2 anchor,Vector2 pos,Vector2 size,Color color,bool raycast=false)
        {var r=Rect(name,parent,anchor,pos,size);var image=r.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=catalog.panelSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.color=color;image.raycastTarget=raycast;return r;}
        TMP_Text Text(Transform parent,string value,float fontSize,Vector2 anchor,Vector2 pos,Vector2 size,Color color,TextAlignmentOptions align=TextAlignmentOptions.Left)
        {var r=Rect(value.Length>30?value.Substring(0,30):value,parent,anchor,pos,size);var t=r.gameObject.AddComponent<TextMeshProUGUI>();t.font=catalog.font;t.text=value;t.fontSize=fontSize;t.color=color;t.alignment=align;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;return t;}
        void Button(Transform parent,string title,Vector2 anchor,Vector2 pos,Vector2 size,Color color,Action callback,bool light=false)
        {
            var r=Panel(parent,title,anchor,pos,size,color,true);var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new(.94f,.97f,.94f);colors.pressedColor=new(.83f,.87f,.84f);colors.selectedColor=Color.white;colors.fadeDuration=.12f;b.colors=colors;
            b.onClick.AddListener(()=>{game.Audio.Play(4);callback();});Text(r,title,size.x<220?20:24,new(.5f,.5f),Vector2.zero,size-new Vector2(16,6),light?cream:navy,TextAlignmentOptions.Center);
            var nav=b.navigation;nav.mode=Navigation.Mode.None;b.navigation=nav;
        }
        void BuildMenu()
        {
            var p=Panel(menu.transform,"Welcome card",new(0,.5f),new(34,0),new(432,648),cream);
            Text(p,"SMALL CITY / BIG APPETITE",16,new(0,1),new(30,-30),new(374,28),muted);
            Text(p,"POCKET\nBOROUGH",62,new(0,1),new(28,-75),new(380,157),navy).fontStyle=FontStyles.Bold;
            Text(p,"Make room for a little chaos.",22,new(0,1),new(30,-244),new(374,40),muted);
            var rule=Panel(p,"Match details",new(0,1),new(30,-302),new(372,63),teal);Text(rule,"2 MINUTES     7 RIVALS     1 CITY",17,new(.5f,.5f),Vector2.zero,new(350,45),navy,TextAlignmentOptions.Center);
            Button(p,"LET'S EAT   >",new(0,1),new(30,-391),new(372,76),coral,game.StartMatch);
            Button(p,"HOW TO PLAY",new(0,1),new(30,-481),new(178,60),teal,()=>OpenModal(help));
            Button(p,"SETTINGS",new(0,1),new(223,-481),new(179,60),teal,()=>OpenModal(settings));
            skin=Text(p,"",18,new(0,1),new(30,-559),new(238,40),navy);
            Button(p,"CHANGE",new(0,1),new(270,-551),new(132,53),MeshWorkshop.C("EEE4CF"),()=>{game.SelectSkin(game.Skin+1);UpdatePreferences();});
            best=Text(p,"BEST  0",17,new(0,1),new(30,-615),new(370,24),muted);
            var badge=Panel(menu.transform,"City badge",new(1,1),new(-28,-26),new(275,52),cream);Text(badge,"BOROUGH No. 01  /  EST. 2026",15,new(.5f,.5f),Vector2.zero,new(250,40),navy,TextAlignmentOptions.Center);
            Text(menu.transform,"A pocket-sized city. An outsized appetite.",19,new(1,0),new(-30,27),new(520,42),cream,TextAlignmentOptions.Right);
        }
        void BuildHUD()
        {
            var p=Panel(hud.transform,"Score card",new(0,1),new(24,-22),new(237,103),cream);
            Text(p,"YOUR HAUL",14,new(0,1),new(20,-13),new(195,24),muted);score=Text(p,"0",38,new(0,1),new(20,-38),new(150,51),navy);rank=Text(p,"#8",24,new(1,.5f),new(-18,-9),new(64,43),coral,TextAlignmentOptions.Right);
            var clock=Panel(hud.transform,"Timer",new(.5f,1),new(0,-22),new(142,70),navy);timer=Text(clock,"2:00",34,new(.5f,.5f),Vector2.zero,new(130,60),cream,TextAlignmentOptions.Center);
            Button(hud.transform,"II",new(1,1),new(-24,-22),new(72,70),cream,game.TogglePause);
            var board=Panel(hud.transform,"Leaderboard",new(1,1),new(-24,-109),new(219,277),cream);Text(board,"THE NEIGHBORHOOD",14,new(0,1),new(17,-14),new(190,26),muted);
            for(int i=0;i<8;i++)leaderboard[i]=Text(board,"",18,new(0,1),new(17,-48-i*27),new(188,26),navy);
            var size=Panel(hud.transform,"Growth",new(0,0),new(24,24),new(270,82),cream);level=Text(size,"LEVEL 1",18,new(0,1),new(16,-12),new(240,28),navy);
            Panel(size,"Growth track",new(0,0),new(16,15),new(238,10),teal);var fill=Panel(size,"Growth fill",new(0,0),new(16,15),new(0,10),coral);growth=fill.GetComponent<UnityEngine.UI.Image>();
            var hint=Panel(hud.transform,"Steering hint",new(.5f,0),new(50,24),new(470,34),new Color(1,.973f,.914f,.9f));
            Text(hint,UnityEngine.InputSystem.Touchscreen.current!=null?"DRAG ANYWHERE TO STEER":"WASD / ARROWS  ·  OR DRAG TO STEER",15,new(.5f,.5f),Vector2.zero,new(460,32),navy,TextAlignmentOptions.Center);
            countdown=Text(hud.transform,"3",112,new(.5f,.5f),Vector2.zero,new(550,180),cream,TextAlignmentOptions.Center);countdown.fontStyle=FontStyles.Bold;
            youMarker=Text(hud.transform,"YOU",16,new(.5f,.5f),Vector2.zero,new(90,28),cream,TextAlignmentOptions.Center);youMarker.fontStyle=FontStyles.Bold;youMarker.outlineColor=navy;youMarker.outlineWidth=.2f;
            toast=Text(hud.transform,"",23,new(.5f,.8f),Vector2.zero,new(680,52),navy,TextAlignmentOptions.Center);toast.fontStyle=FontStyles.Bold;
            var stickR=Panel(hud.transform,"Touch origin",Vector2.zero,Vector2.zero,new(110,110),new Color(.14f,.24f,.29f,.24f));stickR.pivot=new(.5f,.5f);stick=stickR;
            stickKnob=Panel(stick,"Touch knob",new(.5f,.5f),Vector2.zero,new(40,40),cream);stick.gameObject.SetActive(false);
            for(int i=0;i<pops.Length;i++){pops[i]=Text(hud.transform,"",23,new(.5f,.5f),Vector2.zero,new(160,45),coral,TextAlignmentOptions.Center);pops[i].fontStyle=FontStyles.Bold;pops[i].gameObject.SetActive(false);}
        }
        void BuildPause()
        {
            Panel(pause.transform,"Shade",new(.5f,.5f),Vector2.zero,new(2500,1600),new Color(.12f,.22f,.26f,.55f),true);
            var p=Panel(pause.transform,"Pause card",new(.5f,.5f),Vector2.zero,new(460,497),cream);
            Text(p,"TAKE A BREATHER",15,new(.5f,1),new(0,-32),new(400,32),muted,TextAlignmentOptions.Center);Text(p,"City on hold.",40,new(.5f,1),new(0,-87),new(400,64),navy,TextAlignmentOptions.Center);
            Button(p,"KEEP EATING",new(.5f,1),new(0,-187),new(380,70),coral,game.Resume);
            Button(p,"SETTINGS",new(.5f,1),new(0,-272),new(380,62),teal,()=>OpenModal(settings));
            Button(p,"MAIN MENU",new(.5f,1),new(0,-350),new(380,62),teal,game.MainMenu);
            Text(p,"The match timer is paused.",17,new(.5f,0),new(0,25),new(390,32),muted,TextAlignmentOptions.Center);
        }
        void BuildResults()
        {
            Panel(results.transform,"Results shade",new(.5f,.5f),Vector2.zero,new(2500,1600),new Color(.12f,.22f,.26f,.55f),true);
            var p=Panel(results.transform,"Results card",new(.5f,.5f),Vector2.zero,new(850,584),cream);
            Text(p,"THE LAST BITE",16,new(0,1),new(34,-28),new(400,32),muted);resultTitle=Text(p,"",42,new(0,1),new(34,-76),new(750,65),navy);
            resultStats=Text(p,"",24,new(0,1),new(34,-176),new(340,244),navy);
            var board=Panel(p,"Final standings",new(1,1),new(-32,-164),new(363,279),teal);
            for(int i=0;i<8;i++)resultRows[i]=Text(board,"",21,new(0,1),new(20,-17-i*31),new(323,29),navy);
            Button(p,"ONE MORE BITE",new(0,0),new(34,38),new(370,76),coral,game.StartMatch);
            Button(p,"MAIN MENU",new(1,0),new(-32,38),new(363,76),teal,game.MainMenu);
        }
        void BuildSettings()
        {
            var p=Panel(settings.transform,"Settings card",new(.5f,.5f),Vector2.zero,new(610,530),cream);
            Text(p,"TUNE YOUR BOROUGH",16,new(0,1),new(35,-30),new(530,30),muted);Text(p,"Good vibes, your way.",36,new(0,1),new(35,-82),new(540,66),navy);
            music=Text(p,"",25,new(0,1),new(35,-190),new(320,50),navy);sfx=Text(p,"",25,new(0,1),new(35,-282),new(320,50),navy);
            Button(p,"−",new(1,1),new(-126,-181),new(66,64),teal,()=>{game.Audio.SetMusic(Mathf.Clamp01(game.Audio.MusicVolume-.2f));UpdatePreferences();});Button(p,"+",new(1,1),new(-35,-181),new(66,64),teal,()=>{game.Audio.SetMusic(Mathf.Clamp01(game.Audio.MusicVolume+.2f));UpdatePreferences();});
            Button(p,"−",new(1,1),new(-126,-273),new(66,64),teal,()=>{game.Audio.SetSfx(Mathf.Clamp01(game.Audio.SfxVolume-.2f));UpdatePreferences();});Button(p,"+",new(1,1),new(-35,-273),new(66,64),teal,()=>{game.Audio.SetSfx(Mathf.Clamp01(game.Audio.SfxVolume+.2f));UpdatePreferences();});
            Text(p,"Adaptive quality keeps the city moving smoothly.",17,new(0,1),new(35,-367),new(540,44),muted);
            Button(p,"ALL SET",new(.5f,0),new(0,32),new(540,72),coral,CloseModal);
        }
        void BuildHelp()
        {
            var p=Panel(help.transform,"How to play card",new(.5f,.5f),Vector2.zero,new(700,590),cream);
            Text(p,"WELCOME TO THE NEIGHBORHOOD",16,new(0,1),new(36,-28),new(628,30),muted);Text(p,"Start small. Eat the city.",38,new(0,1),new(36,-80),new(628,65),navy);
            Text(p,"01   FIND YOUR FEET\nSteer with WASD, arrows, or drag anywhere.\n\n02   TAKE LITTLE BITES\nCones, bins and shrubs fit your first opening.\nSwallow more to grow into cars and buildings.\n\n03   MIND THE NEIGHBORS\nEat smaller rivals. Keep clear of bigger ones.\nThe highest score after 2 minutes wins.\nFresh street drops keep the city full.",22,new(0,1),new(36,-173),new(628,309),navy);
            Button(p,"GOT IT",new(.5f,0),new(0,32),new(628,70),coral,CloseModal);
        }
        void HideAll(){menu.SetActive(false);hud.SetActive(false);pause.SetActive(false);results.SetActive(false);settings.SetActive(false);help.SetActive(false);}
        void Show(GameObject screen){HideAll();screen.SetActive(true);transitionPanel=screen.transform.GetChild(0) as RectTransform;transition=0;}
        public void ShowMenu(){Show(menu);UpdatePreferences();}
        public void ShowHUD(){Show(hud);toastLife=0;toast.text="";for(int i=0;i<pops.Length;i++){popLife[i]=0;pops[i].gameObject.SetActive(false);}}
        public void ShowPause()=>Show(pause);
        public void ShowResults()
        {
            Show(results);resultTitle.text=game.PlayerRank==1?"You ate the neighborhood!":game.Rankings[0].Name+" takes the borough.";
            resultStats.text="YOUR RANK     #"+game.PlayerRank+" / 8\n\n"+game.Player.Score.ToString("N0")+"  POINTS\n"+game.Player.Consumed+"  OBJECTS EATEN\n"+(game.Player.MaxRadius*2).ToString("0.0")+" m  BIGGEST BITE\n\nBEST  "+game.Best.ToString("N0");
            for(int i=0;i<8;i++){var h=game.Rankings[i];resultRows[i].text=(i+1)+"   "+h.Name+"<pos=75%>"+h.Score;resultRows[i].color=h.IsPlayer?MeshWorkshop.C("C76650"):navy;}
        }
        void OpenModal(GameObject screen){backFromModal=game.Stage==MatchStage.Menu?ShowMenu:ShowPause;Show(screen);UpdatePreferences();}
        void CloseModal(){PlayerPrefs.Save();backFromModal?.Invoke();}
        void UpdatePreferences(){string[] skins={"PAPAYA","LAGOON","LAVENDER","HONEY"};skin.text="YOUR RIM  /  "+skins[game.Skin];best.text="PERSONAL BEST   "+game.Best.ToString("N0");music.text="MUSIC   "+Mathf.RoundToInt(game.Audio.MusicVolume*100)+"%";sfx.text="SOUND FX   "+Mathf.RoundToInt(game.Audio.SfxVolume*100)+"%";}
        public void Refresh()
        {
            score.text=game.Player.Score.ToString("N0");rank.text="#"+game.PlayerRank;
            int seconds=Mathf.CeilToInt(game.TimeLeft);timer.text=(seconds/60)+":"+(seconds%60).ToString("00");timer.color=seconds<=15?coral:cream;
            level.text="LEVEL "+game.Player.Level+"    /    "+(game.Player.Radius*2).ToString("0.0")+" m";float progress=Mathf.Repeat((game.Player.Radius-BoroughRules.InitialRadius)/.8f,1);growth.rectTransform.sizeDelta=new(238*progress,10);
            for(int i=0;i<8;i++){var h=game.Rankings[i];leaderboard[i].text=(i+1)+"  "+h.Name+"<pos=77%>"+h.Score;leaderboard[i].color=h.IsPlayer?MeshWorkshop.C("C76650"):navy;}
        }
        public void Animate(float dt)
        {
            if(Screen.width!=width||Screen.height!=height)Resize();
            if(transitionPanel&&transition<1){transition=Mathf.Min(1,transition+dt*7);transitionPanel.localScale=Vector3.one*Mathf.Lerp(.96f,1,1-Mathf.Pow(1-transition,3));}
            countdown.gameObject.SetActive(game.Stage==MatchStage.Countdown||(!game.Player.Alive&&game.Stage==MatchStage.Playing));
            countdown.text=!game.Player.Alive?"GULP!":Mathf.Max(1,Mathf.CeilToInt(game.Countdown)).ToString();countdown.transform.localScale=Vector3.one*(1+.06f*Mathf.Sin(Time.unscaledTime*9));
            youMarker.gameObject.SetActive(game.Player.Alive&&game.Stage==MatchStage.Playing);var marker=game.Camera.WorldToScreenPoint(game.Player.Position+new Vector3(0,.1f,-game.Player.VisualRadius-1));RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,marker,null,out var markerLocal);youMarker.rectTransform.anchoredPosition=markerLocal;
            toastLife=Mathf.Max(0,toastLife-dt);toast.gameObject.SetActive(toastLife>0);
            for(int i=0;i<pops.Length;i++)if(popLife[i]>0){popLife[i]-=dt;pops[i].gameObject.SetActive(popLife[i]>0);var screen=game.Camera.WorldToScreenPoint(popWorld[i]);RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,screen,null,out var local);pops[i].rectTransform.anchoredPosition=local+Vector2.up*(1-popLife[i])*55;var c=pops[i].color;c.a=Mathf.Min(1,popLife[i]*2);pops[i].color=c;}
        }
        void Resize()
        {width=Screen.width;height=Screen.height;var area=Screen.safeArea;safe.anchorMin=area.position/new Vector2(width,height);safe.anchorMax=(area.position+area.size)/new Vector2(width,height);safe.offsetMin=safe.offsetMax=Vector2.zero;portrait.SetActive(height>width);}
        public void Toast(string text){toast.text=text;toastLife=2.2f;}
        public void Popup(string text,Vector3 position,Color color){int i=nextPop++%pops.Length;pops[i].text=text;pops[i].color=color;popLife[i]=1;popWorld[i]=position;pops[i].gameObject.SetActive(true);}
        public void SetJoystick(Vector2 origin,Vector2 point,bool visible)
        {stick.gameObject.SetActive(visible);if(!visible)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(safe,origin,null,out var local);stick.position=origin;stickKnob.anchoredPosition=Vector2.ClampMagnitude(point-origin,45);}
    }
}

