using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace BrickRush
{
    public sealed class UIManager : MonoBehaviour
    {
        GameManager game;Canvas canvas;RectTransform root,board,hud,footer,left,right,overlay,overlayDesign;
        GameObject menuPage,pausePage,resultsPage,helpPage,settingsPage;GameObject returnPage;
        TMP_FontAsset bodyFont,displayFont;TextMeshProUGUI scoreText,timerText,livesText,comboText,toastText,countText,bestText,menuBest,resultScore,resultDetails,resultValues,resultBest,shakeLabel,reducedLabel,controlHint;
        readonly TextMeshProUGUI[] powerTexts=new TextMeshProUGUI[7];
        UnityEngine.UI.Image timerFill;float toastLife,displayScore;int width,height;Rect safe;bool desktop;
        readonly Color line=Art.Hex("23304A"),panelColor=Art.Hex("101A2E");
        public void Initialize(GameManager owner)
        {
            game=owner;bodyFont=Resources.Load<TMP_FontAsset>("BodyFont");displayFont=Resources.Load<TMP_FontAsset>("DisplayFont");
            var go=new GameObject("Interface",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1440,960);scaler.screenMatchMode=UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=1;
            root=Rect("Safe area",go.transform,Vector2.zero,Vector2.zero);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;
            // Four opaque regions leave the camera's arena rectangle visible.
            for(int i=0;i<4;i++){var back=Image("Backdrop "+i,root,Vector2.zero,Vector2.one,Art.Ink);back.sprite=null;}
            board=Rect("Arena viewport",root,Vector2.zero,new Vector2(500,737));
            hud=Rect("Scoreboard",root,Vector2.zero,new Vector2(500,83));
            Label(hud,"SCORE",new Vector2(-186,28),new Vector2(115,22),11,Art.Muted);
            scoreText=Label(hud,"00000",new Vector2(-178,-1),new Vector2(130,46),38,Art.White,true);
            Label(hud,"TIME LEFT",new Vector2(0,28),new Vector2(110,22),11,Art.Muted);
            timerText=Label(hud,"1:30",new Vector2(0,-1),new Vector2(130,46),38,Art.Gold,true);
            Label(hud,"LIVES",new Vector2(194,28),new Vector2(100,22),11,Art.Muted);
            livesText=Label(hud,"I I I",new Vector2(194,-1),new Vector2(100,46),31,Art.Pink,true);
            var timerTrack=Image("Session track",hud,new Vector2(0,-40),new Vector2(490,2),line);
            timerFill=Image("Time",timerTrack.transform,Vector2.zero,new Vector2(490,2),Art.Cyan);timerFill.rectTransform.pivot=new Vector2(0,.5f);timerFill.rectTransform.anchoredPosition=new Vector2(-245,0);
            footer=Rect("Controls",root,Vector2.zero,new Vector2(500,100));
            controlHint=Label(footer,"MOVE. BOUNCE. BREAK.",new Vector2(-64,-4),new Vector2(360,24),15,Art.Muted,true);
            Button(footer,"II",new Vector2(212,-5),new Vector2(64,56),()=>game.TogglePause(),panelColor,Art.White,22);
            comboText=Label(board,"COMBO  x1",new Vector2(0,0),new Vector2(200,30),16,Art.Cyan,true);comboText.rectTransform.anchorMin=comboText.rectTransform.anchorMax=new Vector2(.5f,0);comboText.rectTransform.anchoredPosition=new Vector2(0,39);
            for(int i=0;i<7;i++)powerTexts[i]=Label(footer,"",new Vector2((i-3)*70,-47),new Vector2(70,28),10,PowerUpManager.Colors[i],true);
            toastText=Label(board,"",Vector2.zero,new Vector2(450,60),26,Art.Gold,true);toastText.raycastTarget=false;
            countText=Label(board,"3",new Vector2(0,-100),new Vector2(350,130),100,Art.White,true);countText.gameObject.SetActive(false);
            left=Rect("Editorial",root,Vector2.zero,new Vector2(280,700));right=Rect("Field notes",root,Vector2.zero,new Vector2(270,700));BuildSides();
            overlay=Rect("Overlay",board,Vector2.zero,Vector2.zero);Stretch(overlay);
            var scrimImage=Image("Scrim",overlay,Vector2.zero,new Vector2(500,737),new Color(.025f,.045f,.10f,.88f));scrimImage.sprite=null;
            overlayDesign=Rect("Pages",overlay,Vector2.zero,new Vector2(500,737));
            menuPage=Page("Main menu");BuildMenu(menuPage.transform);
            pausePage=Page("Pause");BuildPause(pausePage.transform);
            resultsPage=Page("Results");BuildResults(resultsPage.transform);
            helpPage=Page("How to play");BuildHelp(helpPage.transform);
            settingsPage=Page("Settings");BuildSettings(settingsPage.transform);
            if(EventSystem.current==null){var events=new GameObject("EventSystem");events.AddComponent<EventSystem>();events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            BuildHeader();Layout();
        }
        RectTransform header;TextMeshProUGUI headerTag;
        void BuildHeader()
        {
            header=Rect("Masthead",root,Vector2.zero,new Vector2(1440,58));
            var icon=Image("Logo mark",header,new Vector2(-665,0),new Vector2(28,28),Art.Gold);icon.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            icon.sprite=null;Image("Logo cut",icon.transform,Vector2.zero,new Vector2(13,13),Art.Ink).sprite=null;
            Label(header,"BRICK RUSH",new Vector2(-552,0),new Vector2(175,35),25,Art.White,true);
            headerTag=Label(header,"ARCADE / 001     -     THE 90-SECOND CHALLENGE",new Vector2(435,0),new Vector2(450,35),12,Art.Muted);
        }
        void BuildSides()
        {
            Label(left,"A LITTLE CHAOS. A LOT OF COLOR.",new Vector2(0,284),new Vector2(280,28),11,Art.Cyan,true,TextAlignmentOptions.Left);
            Label(left,"BREAK.\nBUILD.\nRUSH.",new Vector2(-3,130),new Vector2(300,290),87,Art.White,true,TextAlignmentOptions.Left);
            Image("Underline",left,new Vector2(-108,-28),new Vector2(64,4),Art.Gold);
            Label(left,"Find your rhythm. Chase the chain.\nMake every second count.",new Vector2(0,-86),new Vector2(280,75),18,Art.Muted,false,TextAlignmentOptions.Left);
            Label(left,"90",new Vector2(-91,-191),new Vector2(100,80),64,Art.Gold,true);
            Label(left,"SECONDS\nONE MORE RUN.",new Vector2(45,-191),new Vector2(170,60),15,Art.White,true,TextAlignmentOptions.Left);
            Image("Rule",left,new Vector2(0,-250),new Vector2(280,1),line);
            Label(left,"MOUSE / TOUCH / A D / ARROWS\nSPACE TO LAUNCH   -   ESC TO PAUSE",new Vector2(0,-293),new Vector2(280,65),12,Art.Muted,false,TextAlignmentOptions.Left);
            var card=Image("Personal best card",right,new Vector2(0,227),new Vector2(270,144),panelColor);
            Label(card.transform,"YOUR PERSONAL BEST",new Vector2(0,39),new Vector2(225,25),12,Art.Muted,true,TextAlignmentOptions.Left);
            bestText=Label(card.transform,"00000",new Vector2(0,-8),new Vector2(225,65),49,Art.Gold,true,TextAlignmentOptions.Left);
            Label(right,"THE FIELD GUIDE",new Vector2(0,112),new Vector2(270,28),13,Art.White,true,TextAlignmentOptions.Left);
            GuideRow(right,68,Art.Cyan,"STANDARD","10 PTS  /  ONE CLEAN HIT");GuideRow(right,15,Art.Violet,"ARMORED","25 PTS  /  TWO HITS");GuideRow(right,-38,Art.Orange,"EXPLOSIVE","20 PTS  /  CHAIN REACTION");GuideRow(right,-91,Art.Gold,"BONUS","50 PTS  /  EXTRA GOOD STUFF");
            Image("Rule",right,new Vector2(0,-137),new Vector2(270,1),line);
            Label(right,"KEEP THE CHAIN ALIVE",new Vector2(0,-169),new Vector2(270,26),13,Art.White,true,TextAlignmentOptions.Left);
            Label(right,"3 / x2     6 / x3     10 / x4     15 / x5",new Vector2(0,-205),new Vector2(270,24),16,Art.Cyan,true,TextAlignmentOptions.Left);
            Label(right,"Every paddle return resets the combo.\nEvery saved life adds 100 points.",new Vector2(0,-251),new Vector2(270,58),14,Art.Muted,false,TextAlignmentOptions.Left);
            Button(right,"HOW TO PLAY  >",new Vector2(0,-318),new Vector2(270,46),()=>OpenPage(helpPage),panelColor,Art.White,16);
        }
        void GuideRow(Transform parent,float y,Color color,string title,string subtitle)
        {
            Image("Brick swatch",parent,new Vector2(-114,y),new Vector2(32,17),color,Art.Brick);
            Label(parent,title,new Vector2(29,y+8),new Vector2(201,23),15,Art.White,true,TextAlignmentOptions.Left);
            Label(parent,subtitle,new Vector2(29,y-12),new Vector2(201,20),10,Art.Muted,false,TextAlignmentOptions.Left);
        }
        void BuildMenu(Transform page)
        {
            Label(page,"01 / 90-SECOND RUSH",new Vector2(0,248),new Vector2(380,30),14,Art.Cyan,true);
            Label(page,"BRICK\nRUSH",new Vector2(0,129),new Vector2(420,218),106,Art.White,true);
            Image("Title accent",page,new Vector2(0,12),new Vector2(46,4),Art.Gold);
            Label(page,"A burst of color. A perfect bounce.\nYour next high score starts here.",new Vector2(0,-43),new Vector2(400,65),18,Art.Muted);
            Button(page,"LET'S RUSH   >",new Vector2(0,-133),new Vector2(374,66),()=>game.StartRun(),Art.Gold,Art.Ink,28);
            Label(page,"90 SECONDS    /    3 LIVES    /    ALL YOU",new Vector2(0,-192),new Vector2(410,28),12,Art.Muted,true);
            menuBest=Label(page,"PERSONAL BEST / 00000",new Vector2(0,-221),new Vector2(410,24),13,Art.Gold,true);
            Button(page,"HOW TO PLAY",new Vector2(-97,-256),new Vector2(180,46),()=>OpenPage(helpPage),panelColor,Art.White,16);
            Button(page,"SETTINGS",new Vector2(97,-256),new Vector2(180,46),()=>OpenPage(settingsPage),panelColor,Art.White,16);
        }
        void BuildPause(Transform page)
        {
            Label(page,"PAUSED / CATCH YOUR BREATH",new Vector2(0,200),new Vector2(430,30),14,Art.Cyan,true);
            Label(page,"GOOD THINGS\nTAKE A BREAK.",new Vector2(0,92),new Vector2(430,170),58,Art.White,true);
            Button(page,"BACK TO THE RUSH   >",new Vector2(0,-62),new Vector2(374,66),()=>game.TogglePause(),Art.Gold,Art.Ink,24);
            Button(page,"SETTINGS",new Vector2(0,-144),new Vector2(374,49),()=>OpenPage(settingsPage),panelColor,Art.White,17);
            Button(page,"MAIN MENU",new Vector2(0,-213),new Vector2(374,49),()=>game.ShowMenu(),panelColor,Art.Muted,17);
        }
        void BuildResults(Transform page)
        {
            Label(page,"THAT WAS A RUSH.",new Vector2(0,271),new Vector2(430,40),35,Art.White,true);
            resultBest=Label(page,"RUN COMPLETE",new Vector2(0,226),new Vector2(420,26),13,Art.Cyan,true);
            resultScore=Label(page,"00000",new Vector2(0,161),new Vector2(440,105),87,Art.Gold,true);
            Label(page,"FINAL SCORE",new Vector2(0,94),new Vector2(400,24),12,Art.Muted,true);
            Image("Divider",page,new Vector2(0,61),new Vector2(374,1),line);
            resultDetails=Label(page,"BRICKS CLEARED\nHIGHEST COMBO\nWAVES / TIME\nGAMEPLAY POINTS\nLIVES / BONUS",new Vector2(-71,-28),new Vector2(225,162),18,Art.Muted,false,TextAlignmentOptions.Left);
            resultValues=Label(page,"",new Vector2(113,-28),new Vector2(143,162),18,Art.White,false,TextAlignmentOptions.Right);
            Button(page,"ONE MORE RUN   >",new Vector2(0,-171),new Vector2(374,64),()=>game.StartRun(),Art.Gold,Art.Ink,26);
            Button(page,"MAIN MENU",new Vector2(0,-252),new Vector2(374,46),()=>game.ShowMenu(),panelColor,Art.White,16);
        }
        void BuildHelp(Transform page)
        {
            Label(page,"A QUICK FIELD GUIDE",new Vector2(0,292),new Vector2(420,24),13,Art.Cyan,true);
            Label(page,"MAKE EVERY\nBOUNCE COUNT.",new Vector2(0,218),new Vector2(430,135),49,Art.White,true);
            Label(page,"01  MOVE & RETURN",new Vector2(0,119),new Vector2(374,28),18,Art.Gold,true,TextAlignmentOptions.Left);
            Label(page,"Move your mouse, drag a finger, or use A/D.\nAim with the paddle's edges. The ball launches\nafter the countdown. Space / tap to relaunch.",new Vector2(0,63),new Vector2(374,75),17,Art.White,false,TextAlignmentOptions.Left);
            Label(page,"02  CHASE THE CHAIN",new Vector2(0,-2),new Vector2(374,28),18,Art.Gold,true,TextAlignmentOptions.Left);
            Label(page,"3 hits x2  /  6 hits x3  /  10 hits x4  /  15 hits x5\nAny paddle return resets the shared combo.\nOnly losing the last ball costs a life.",new Vector2(0,-58),new Vector2(374,76),17,Art.White,false,TextAlignmentOptions.Left);
            Label(page,"03  CATCH THE GOOD STUFF",new Vector2(0,-124),new Vector2(374,28),18,Art.Gold,true,TextAlignmentOptions.Left);
            Label(page,"M  Multiball     W  Wide paddle     F  Fireball\nL  Auto laser     S  Slow ball     x2  Score boost\n+  One-use shield. Timed effects last 10s.\nSteel striped bricks cannot be destroyed.",new Vector2(0,-190),new Vector2(374,102),16,Art.White,false,TextAlignmentOptions.Left);
            Button(page,"GOT IT   >",new Vector2(0,-293),new Vector2(374,54),ClosePage,Art.Gold,Art.Ink,23);
        }
        void BuildSettings(Transform page)
        {
            Label(page,"MAKE IT YOURS",new Vector2(0,264),new Vector2(420,40),44,Art.White,true);
            Label(page,"SOUND & FEEL",new Vector2(0,216),new Vector2(420,26),13,Art.Cyan,true);
            Label(page,"MUSIC",new Vector2(0,153),new Vector2(374,28),16,Art.White,true,TextAlignmentOptions.Left);
            Slider(page,new Vector2(0,112),game.Audio.MusicVolume,v=>game.Audio.Volumes(v,game.Audio.SfxVolume));
            Label(page,"SOUND EFFECTS",new Vector2(0,47),new Vector2(374,28),16,Art.White,true,TextAlignmentOptions.Left);
            Slider(page,new Vector2(0,6),game.Audio.SfxVolume,v=>game.Audio.Volumes(game.Audio.MusicVolume,v));
            var shake=Button(page,"",new Vector2(0,-85),new Vector2(374,53),()=>{game.SetPreferences(game.ReducedEffects,!game.ScreenShake);PreferenceLabels();},panelColor,Art.White,17);
            shakeLabel=Label(shake.transform,"",Vector2.zero,new Vector2(330,45),17,Art.White,true);
            var reduced=Button(page,"",new Vector2(0,-158),new Vector2(374,53),()=>{game.SetPreferences(!game.ReducedEffects,game.ScreenShake);PreferenceLabels();},panelColor,Art.White,17);
            reducedLabel=Label(reduced.transform,"",Vector2.zero,new Vector2(330,45),17,Art.White,true);
            Button(page,"ALL SET   >",new Vector2(0,-266),new Vector2(374,59),ClosePage,Art.Gold,Art.Ink,25);PreferenceLabels();
        }
        void PreferenceLabels(){shakeLabel.text="SCREEN SHAKE                         "+(game.ScreenShake?"ON":"OFF");reducedLabel.text="REDUCED EFFECTS                     "+(game.ReducedEffects?"ON":"OFF");}
        void Slider(Transform parent,Vector2 position,float value,Action<float> changed)
        {
            var track=Image("Volume slider",parent,position,new Vector2(374,32),Color.clear);track.raycastTarget=true;
            Image("Rail",track.transform,Vector2.zero,new Vector2(374,5),panelColor);
            var fill=Image("Fill",track.transform,Vector2.zero,new Vector2(374,5),Art.Cyan);
            var handle=Image("Handle",track.transform,Vector2.zero,new Vector2(22,28),Art.White);handle.raycastTarget=true;
            // Slider drives both anchors. Keep sizeDelta relative to its 32px hit area.
            fill.rectTransform.sizeDelta=new Vector2(0,-27);handle.rectTransform.sizeDelta=new Vector2(22,-4);
            var slider=track.gameObject.AddComponent<UnityEngine.UI.Slider>();slider.fillRect=fill.rectTransform;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.value=value;slider.onValueChanged.AddListener(v=>changed(v));
        }
        void LateUpdate(){if(width!=Screen.width||height!=Screen.height||safe!=Screen.safeArea)Layout();}
        void Layout()
        {
            width=Screen.width;height=Screen.height;safe=Screen.safeArea;
            root.anchorMin=new Vector2(safe.xMin/width,safe.yMin/height);root.anchorMax=new Vector2(safe.xMax/width,safe.yMax/height);Canvas.ForceUpdateCanvases();
            float w=root.rect.width,h=root.rect.height;desktop=w/h>1.25f&&w>1250;
            controlHint.text=desktop?"MOUSE / A D / ARROWS  -  ESC TO PAUSE":"DRAG HERE TO MOVE  -  TAP TO LAUNCH";
            float bh=Mathf.Min(h-(desktop?200:245),desktop?730:800);float bw=Mathf.Min(desktop?520:w-28,bh*9.9f/14.6f);bh=bw*14.6f/9.9f;
            float y=desktop?-22:h/2-151-bh/2;board.sizeDelta=new Vector2(bw,bh);board.anchoredPosition=new Vector2(0,y);
            float scale=bw/500;hud.anchoredPosition=new Vector2(0,y+bh/2+50*scale);hud.localScale=Vector3.one*scale;
            footer.anchoredPosition=new Vector2(0,y-bh/2-35*scale);footer.localScale=Vector3.one*scale;
            overlayDesign.localScale=Vector3.one*scale;var scrim=overlay.GetChild(0).GetComponent<RectTransform>();scrim.sizeDelta=new Vector2(bw,bh);
            left.gameObject.SetActive(desktop);right.gameObject.SetActive(desktop);
            left.anchoredPosition=new Vector2(-bw/2-205,-10);right.anchoredPosition=new Vector2(bw/2+200,-10);
            header.anchoredPosition=new Vector2(0,h/2-35);header.localScale=Vector3.one*(desktop?Mathf.Min(1,(w-64)/1440):.75f);
            if(!desktop){header.GetChild(0).GetComponent<RectTransform>().anchoredPosition=new Vector2(-110,0);header.GetChild(1).GetComponent<RectTransform>().anchoredPosition=new Vector2(0,0);}
            else{header.GetChild(0).GetComponent<RectTransform>().anchoredPosition=new Vector2(-665,0);header.GetChild(1).GetComponent<RectTransform>().anchoredPosition=new Vector2(-552,0);}
            headerTag.gameObject.SetActive(desktop);
            // Canvas to screen coordinates preserve a circular ball at every aspect ratio.
            var corners=new Vector3[4];board.GetWorldCorners(corners);game.Camera.pixelRect=new Rect(corners[0].x,corners[0].y,corners[2].x-corners[0].x,corners[2].y-corners[0].y);
            for(int i=0;i<4;i++)
            {
                var back=root.GetChild(i).GetComponent<RectTransform>();
                if(i==0){back.sizeDelta=new Vector2(w,h/2-y-bh/2);back.anchoredPosition=new Vector2(0,(h/2+y+bh/2)/2);}
                if(i==1){back.sizeDelta=new Vector2(w,h/2+y-bh/2);back.anchoredPosition=new Vector2(0,(-h/2+y-bh/2)/2);}
                if(i==2){back.sizeDelta=new Vector2((w-bw)/2,bh);back.anchoredPosition=new Vector2(-(w+bw)/4,y);}
                if(i==3){back.sizeDelta=new Vector2((w-bw)/2,bh);back.anchoredPosition=new Vector2((w+bw)/4,y);}
            }
            toastText.rectTransform.sizeDelta=new Vector2(bw-20,60);toastText.fontSize=24*scale;toastText.rectTransform.anchoredPosition=new Vector2(0,-bh*.08f);
            countText.fontSize=95*scale;
        }
        public void Refresh()
        {
            if(scoreText==null)return;
            int seconds=Mathf.CeilToInt(game.Remaining);timerText.SetText("{0}:{1:00}",seconds/60,seconds%60);timerText.color=seconds<=15?Art.Orange:Art.Gold;
            livesText.text=game.Lives>=3?"I I I":game.Lives==2?"I I -":game.Lives==1?"I - -":"- - -";
            comboText.SetText("COMBO  x{0}",game.Score.Multiplier);comboText.color=game.Score.Multiplier>=3?Art.Gold:Art.Cyan;
            bestText.text=SaveManager.Best.ToString("00000");timerFill.rectTransform.sizeDelta=new Vector2(490*game.Remaining/game.Config.duration,2);
            menuBest.text="PERSONAL BEST / "+SaveManager.Best.ToString("00000");
            for(int i=0;i<7;i++){bool active=game.Powers.Has((PowerKind)i);powerTexts[i].text=active?(PowerUpManager.Symbols[i]+"\n"+(i==6?"READY":Mathf.CeilToInt(game.Powers.Durations[i])+"s")):"";}
        }
        public void Animate(float dt)
        {
            displayScore=Mathf.MoveTowards(displayScore,game.Score.Points,Mathf.Max(200,Mathf.Abs(displayScore-game.Score.Points)*10)*dt);scoreText.SetText("{0:00000}",Mathf.RoundToInt(displayScore));
            if(toastLife>0){toastLife-=dt;toastText.alpha=Mathf.Clamp01(toastLife*3);toastText.transform.localScale=Vector3.one*(1+Mathf.Max(0,toastLife-1.1f)*.25f);}
        }
        public void Toast(string text,Color color){toastText.text=text;toastText.color=color;toastText.alpha=1;toastLife=1.45f;}
        public void Countdown(string text){countText.text=text;countText.gameObject.SetActive(true);}
        public void HideCountdown()=>countText.gameObject.SetActive(false);
        void SetPage(GameObject page){foreach(Transform child in overlayDesign)child.gameObject.SetActive(child.gameObject==page);overlay.gameObject.SetActive(true);countText.gameObject.SetActive(false);toastLife=0;toastText.alpha=0;}
        public void ShowMenu(){displayScore=0;SetPage(menuPage);}
        public void ShowGame(){if(game.State==SessionState.Countdown&&game.Elapsed<.01f)displayScore=0;overlay.gameObject.SetActive(false);countText.gameObject.SetActive(game.State==SessionState.Countdown);}
        public void ShowPause()=>SetPage(pausePage);
        public void ShowResults()
        {
            resultScore.text=game.FinalScore.ToString("00000");resultBest.text=game.NewBest?"A NEW PERSONAL BEST!":"PERSONAL BEST / "+SaveManager.Best.ToString("00000");
            resultValues.text=game.Score.Destroyed+"\nx"+game.Score.HighestCombo+"\n"+game.Waves.Completed+" / "+Mathf.FloorToInt(game.Elapsed)+"s\n"+game.Score.Points+"\n"+game.Lives+" / +"+(game.Lives*game.Config.lifeBonus);
            SetPage(resultsPage);
        }
        void OpenPage(GameObject page)
        {
            if(game.State==SessionState.Playing||game.State==SessionState.Countdown)game.TogglePause();
            returnPage=game.State==SessionState.Paused?pausePage:game.State==SessionState.Results?resultsPage:menuPage;SetPage(page);
        }
        void ClosePage()=>SetPage(returnPage==null?menuPage:returnPage);
        GameObject Page(string name){var r=Rect(name,overlayDesign,Vector2.zero,new Vector2(500,737));return r.gameObject;}
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=pos;return rt;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        UnityEngine.UI.Image Image(string name,Transform parent,Vector2 pos,Vector2 size,Color color,Sprite sprite=null)
        {
            var rt=Rect(name,parent,pos,size);var image=rt.gameObject.AddComponent<UnityEngine.UI.Image>();image.sprite=sprite==null?Art.Panel:sprite;image.type=sprite==null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;if(size.x<=4||size.y<=4)image.sprite=null;image.color=color;image.raycastTarget=false;return image;
        }
        TextMeshProUGUI Label(Transform parent,string text,Vector2 pos,Vector2 size,float fontSize,Color color,bool display=false,TextAlignmentOptions align=TextAlignmentOptions.Center)
        {
            var rt=Rect(text.Length>0?text.Split('\n')[0]:"Label",parent,pos,size);var label=rt.gameObject.AddComponent<TextMeshProUGUI>();label.font=display?displayFont:bodyFont;label.text=text;label.fontSize=fontSize;label.color=color;label.alignment=align;label.raycastTarget=false;label.textWrappingMode=TextWrappingModes.Normal;label.overflowMode=TextOverflowModes.Overflow;label.lineSpacing=display&&fontSize>=45?-20:-3;return label;
        }
        UnityEngine.UI.Button Button(Transform parent,string text,Vector2 pos,Vector2 size,Action action,Color color,Color foreground,float fontSize)
        {
            var image=Image(text,parent,pos,size,color);image.raycastTarget=true;var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.highlightedColor=Art.Hex("D2E9DD");colors.pressedColor=Art.Hex("829EAA");colors.selectedColor=Color.white;colors.fadeDuration=.08f;button.colors=colors;
            Label(image.transform,text,Vector2.zero,size-new Vector2(12,0),fontSize,foreground,true);
            button.onClick.AddListener(()=>{game.Audio.Unlock();game.Audio.Play("click",.45f);action();});return button;
        }
    }
}
