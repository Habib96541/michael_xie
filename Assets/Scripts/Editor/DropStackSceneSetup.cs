using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor.SceneManagement;
using DropStack.Game;
using DropStack.Rendering;
using DropStack.Audio;
using DropStack.UI;

namespace DropStack.Editor
{
    public static class DropStackSceneSetup
    {
        private static Font display,body;
        private static readonly Color White=new Color(.93f,.97f,1),Muted=new Color(.58f,.72f,.81f),Cyan=new Color(.04f,.86f,1),Amber=new Color(1,.7f,.12f),Purple=new Color(.63f,.42f,1),Green=new Color(.53f,.94f,.25f);
        [MenuItem("Drop Stack/Setup Scene")]
        public static void SetupScene()
        {
            DropStackArtGenerator.GenerateBackdrop();
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            foreach(string name in new[]{"DropStackGameManager","DropStackBoard","BoardRenderer","ActivePieceRenderer","DropStackAudioManager","JuiceEffects","Canvas","EventSystem","BoardBorder","DangerLine","ArcadeBackdrop"}) { var old=GameObject.Find(name); if(old!=null) Object.DestroyImmediate(old); }
            display=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/StackDisplay.ttf"); body=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/StackUI.ttf");
            if(display==null||body==null) throw new System.Exception("The required StackDisplay.ttf and StackUI.ttf fonts are missing from Assets/Fonts.");
            var cam=Camera.main; if(cam==null) { var go=new GameObject("Main Camera"); go.tag="MainCamera"; cam=go.AddComponent<Camera>(); }
            cam.orthographic=true; cam.orthographicSize=11.25f; cam.transform.position=new Vector3(0,0,-10); cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.018f,.035f,.065f);
            if(cam.GetComponent<AudioListener>()==null) cam.gameObject.AddComponent<AudioListener>();
            var bg=new GameObject("ArcadeBackdrop").AddComponent<SpriteRenderer>(); bg.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Theme/arcade_backdrop.png"); bg.sortingOrder=-50;
            new GameObject("DropStackAudioManager").AddComponent<DropStackAudioManager>();
            var systems=new GameObject("DropStackGameManager"); var board=systems.AddComponent<DropStackBoard>(); var score=systems.AddComponent<ScoreManager>(); var speed=systems.AddComponent<SpeedCurveManager>();
            var piece=systems.AddComponent<ActivePieceController>(); piece.Initialize(board,score,speed); var game=systems.AddComponent<DropStackGameManager>();
            var view=new GameObject("BoardRenderer").AddComponent<BoardRenderer>(); var active=new GameObject("ActivePieceRenderer").AddComponent<ActivePieceRenderer>(); active.Bind(view,piece);
            Set(game,"board",board); Set(game,"pieceController",piece); Set(game,"scoreManager",score); Set(game,"speedManager",speed); Set(game,"boardRenderer",view); Set(game,"activePieceRenderer",active);
            var juice=new GameObject("JuiceEffects").AddComponent<JuiceEffects>();
            const string matPath="Assets/Sprites/Theme/arcade_particles.mat"; var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath); if(mat==null) { mat=new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(mat,matPath); } Set(juice,"particleMaterial",mat);
            var root=Obj("Canvas",null); var canvas=root.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize; root.AddComponent<GraphicRaycaster>();
            var es=new GameObject("EventSystem").AddComponent<EventSystem>(); es.sendNavigationEvents=false; es.gameObject.AddComponent<InputSystemUIInputModule>();
            var ui=root.AddComponent<DropStackUIManager>(); ui.Bind(game,score,speed);

            var header=Obj("Header",root.transform).GetComponent<RectTransform>();
            var brand=Obj("Brand",header).GetComponent<RectTransform>(); Rect(brand,new Vector2(-385,6),new Vector2(270,50)); var logo=brand.gameObject.AddComponent<ArcadeLogo>(); logo.stacked=false; logo.raycastTarget=false;
            T("Edition",header,"BLOCKS. BEATS. HIGH SCORES.",12,new Vector2(-385,-32),new Vector2(300,24),Muted);
            var mode=T("Mode",header,"NEUTRAL",20,new Vector2(0,17),new Vector2(260,34),Cyan);
            var status=T("Status",header,"PICK YOUR PACE",12,new Vector2(0,-15),new Vector2(300,28),Muted);
            var restart=Btn("Restart",header,"R",new Vector2(410,0),new Vector2(50,50),Cyan,20);
            var pause=Btn("Pause",header,"II",new Vector2(470,0),new Vector2(50,50),Cyan,20);
            var help=Btn("Help",header,"?",new Vector2(530,0),new Vector2(50,50),Cyan,22);
            Set(ui,"restartButton",restart); Set(ui,"pauseButton",pause); Set(ui,"helpButton",help); Set(ui,"statusText",status); Set(ui,"modeText",mode);

            var frame=Obj("PlayfieldFrame",root.transform).GetComponent<RectTransform>();
            Border(frame,new Color(.008f,.018f,.03f),8,0); Border(frame,new Color(.08f,.24f,.3f),4,4); var edges=Border(frame,Cyan,2,6); SetList(ui,"boardEdges",edges);
            var boardLabel=T("BoardLabel",frame,"LOCK / CLEAR / REPEAT",11,Vector2.zero,new Vector2(320,28),Muted).rectTransform;
            boardLabel.anchorMin=boardLabel.anchorMax=new Vector2(.5f,1); boardLabel.anchoredPosition=new Vector2(0,18);

            var hold=Panel("HoldPanel",root.transform,new Vector2(208,174),Vector2.zero,Cyan*.55f);
            T("HoldLabel",hold,"HOLD",14,new Vector2(0,62),new Vector2(140,30),Muted);
            var holdSlot=Obj("HoldPreview",hold).GetComponent<RectTransform>(); Rect(holdSlot,new Vector2(0,0),new Vector2(132,82)); var holdPreview=holdSlot.gameObject.AddComponent<PiecePreviewRenderer>(); Float(holdPreview,"blockSize",24);
            var holdHint=T("HoldHint",hold,"TAP / C",12,new Vector2(0,-64),new Vector2(140,26),Muted);
            var holdButton=hold.gameObject.AddComponent<Button>(); holdButton.targetGraphic=hold.GetComponent<ArcadeSurface>(); hold.GetComponent<ArcadeSurface>().raycastTarget=true; holdButton.navigation=new Navigation{mode=Navigation.Mode.None};
            Set(ui,"holdPreview",holdPreview); Set(ui,"holdHint",holdHint); Set(ui,"holdButton",holdButton);

            var stats=Panel("StatsPanel",root.transform,new Vector2(208,340),Vector2.zero,Cyan*.4f);
            T("ScoreLabel",stats,"SCORE",14,new Vector2(0,139),new Vector2(160,28),Muted);
            var scoreText=T("Score",stats,"0",44,new Vector2(0,96),new Vector2(185,58),White);
            T("BestLabel",stats,"BEST",12,new Vector2(0,49),new Vector2(150,26),Muted);
            var best=T("Best",stats,"0",25,new Vector2(0,19),new Vector2(180,40),Amber); Rule(stats,-12);
            T("LinesLabel",stats,"LINES",12,new Vector2(-45,-43),new Vector2(80,25),Muted); T("LevelLabel",stats,"LEVEL",12,new Vector2(45,-43),new Vector2(80,25),Muted);
            var lines=T("Lines",stats,"00",28,new Vector2(-45,-74),new Vector2(80,42),White); var level=T("Level",stats,"01",28,new Vector2(45,-74),new Vector2(80,42),White); Rule(stats,-104);
            T("ComboLabel",stats,"COMBO",12,new Vector2(-44,-134),new Vector2(90,28),Muted); var combo=T("Combo",stats,"x1.0",24,new Vector2(44,-134),new Vector2(90,40),Muted);
            Set(ui,"scoreText",scoreText); Set(ui,"bestText",best); Set(ui,"linesText",lines); Set(ui,"levelText",level); Set(ui,"comboText",combo);

            var next=Panel("NextPanel",root.transform,new Vector2(208,334),Vector2.zero,Purple*.6f); T("NextLabel",next,"UP NEXT",14,new Vector2(0,140),new Vector2(170,28),Muted);
            var previews=new List<PiecePreviewRenderer>();
            for(int i=0;i<3;i++) { var slot=Obj("NextPiece_"+i,next).GetComponent<RectTransform>(); Rect(slot,new Vector2(0,79-i*96),new Vector2(140,80)); var pr=slot.gameObject.AddComponent<PiecePreviewRenderer>(); Float(pr,"blockSize",i==0?25:22); previews.Add(pr); if(i<2) Rule(next,32-i*96); }
            SetList(ui,"nextPreviews",previews);
            var timer=Panel("TimerPanel",root.transform,new Vector2(208,184),Vector2.zero,Cyan*.6f);
            T("TimeLabel",timer,"TIME LEFT",14,new Vector2(0,62),new Vector2(170,28),Muted); var time=T("Time",timer,"01:30",40,new Vector2(0,20),new Vector2(190,56),White);
            I("TimerTrack",timer,new Vector2(144,6),new Vector2(0,-23),new Color(.1f,.2f,.27f)); var timerFill=Fill("TimerFill",timer,new Vector2(144,6),new Vector2(0,-23),Cyan);
            var gearText=T("GearLabel",timer,"NEXT GEAR",10,new Vector2(0,-49),new Vector2(160,23),Muted); I("SpeedTrack",timer,new Vector2(144,3),new Vector2(0,-68),new Color(.1f,.18f,.25f)); var speedFill=Fill("SpeedFill",timer,new Vector2(144,3),new Vector2(0,-68),Amber);
            Set(ui,"gearText",gearText);
            Set(ui,"timeText",time); Set(ui,"timerFill",timerFill); Set(ui,"speedFill",speedFill); Set(ui,"timerSurface",timer.GetComponent<ArcadeSurface>());

            var goal=Panel("GoalPanel",root.transform,new Vector2(208,108),Vector2.zero,Green*.6f);
            T("TargetLabel",goal,"LINE TARGET",12,new Vector2(0,32),new Vector2(170,28),Muted); var goalText=T("Goal",goal,"00 / 08",26,new Vector2(0,0),new Vector2(160,40),Green);
            I("GoalTrack",goal,new Vector2(144,5),new Vector2(0,-32),new Color(.1f,.19f,.2f)); var goalFill=Fill("GoalFill",goal,new Vector2(144,5),new Vector2(0,-32),Green);
            Set(ui,"goalText",goalText); Set(ui,"goalFill",goalFill);
            var danger=Panel("DangerPanel",root.transform,new Vector2(208,108),Vector2.zero,Cyan*.4f); T("StackLabel",danger,"STACK HEIGHT",12,new Vector2(0,32),new Vector2(180,28),Muted);
            var segmentRoot=Obj("Segments",danger).GetComponent<RectTransform>(); Rect(segmentRoot,new Vector2(0,0),new Vector2(144,18)); var segments=new List<Image>();
            for(int i=0;i<10;i++) segments.Add(I("Segment_"+i,segmentRoot,new Vector2(10,14),new Vector2(-58.5f+i*13,0),new Color(.1f,.18f,.24f)));
            var dangerText=T("Danger",danger,"CLEAR",12,new Vector2(0,-31),new Vector2(160,26),Muted); SetList(ui,"stackSegments",segments); Set(ui,"dangerText",dangerText);

            var controls=Obj("Controls",root.transform).GetComponent<RectTransform>(); var touch=controls.gameObject.AddComponent<TouchControlsUI>();
            var left=Btn("Left",controls,"<",new Vector2(-333,0),new Vector2(64,60),Cyan,26); var right=Btn("Right",controls,">",new Vector2(-259,0),new Vector2(64,60),Cyan,26);
            var soft=Btn("SoftDrop",controls,"V",new Vector2(-185,0),new Vector2(64,60),Cyan,22); var reserve=Btn("Hold",controls,"HOLD",new Vector2(-89,0),new Vector2(108,60),Green,18);
            var ccw=Btn("RotateLeft",controls,"Z / CCW",new Vector2(31,0),new Vector2(112,60),Purple,16); var cw=Btn("RotateRight",controls,"X / CW",new Vector2(153,0),new Vector2(112,60),Purple,16);
            var hard=Btn("HardDrop",controls,"DROP",new Vector2(295,0),new Vector2(150,60),Amber,22,true);
            Set(touch,"controller",piece); Set(touch,"btnLeft",left); Set(touch,"btnRight",right); Set(touch,"btnSoftDrop",soft); Set(touch,"btnHold",reserve); Set(touch,"btnHardDrop",hard); Set(touch,"btnRotateCW",cw); Set(touch,"btnRotateCCW",ccw);
            var footer=T("KeyboardGuide",root.transform,"ARROWS / WASD  MOVE     Z / X  ROTATE     SPACE  DROP     C  HOLD     ESC  PAUSE",11,Vector2.zero,new Vector2(1050,28),Muted).rectTransform;
            var toast=Panel("ActionToast",root.transform,new Vector2(400,68),new Vector2(0,356),Cyan); var toastGroup=toast.gameObject.AddComponent<CanvasGroup>(); toastGroup.alpha=0; toastGroup.blocksRaycasts=false;
            var toastTitle=T("Title",toast,"LET'S STACK",24,new Vector2(0,11),new Vector2(380,36),Cyan); var toastDetail=T("Detail",toast,"90 SECONDS",13,new Vector2(0,-18),new Vector2(380,26),White);
            Set(ui,"toastGroup",toastGroup); Set(ui,"toastTitle",toastTitle); Set(ui,"toastDetail",toastDetail); Set(ui,"toastSurface",toast.GetComponent<ArcadeSurface>());

            var start=Overlay("StartMenu",root.transform,.94f); var startCard=Card(start,"StartCard",new Vector2(720,790),Cyan);
            T("Eyebrow",startCard,"DROP IN. ZONE OUT. STACK HIGH.",13,new Vector2(0,345),new Vector2(620,30),Muted);
            var startLogo=Obj("Logo",startCard).GetComponent<RectTransform>(); Rect(startLogo,new Vector2(0,208),new Vector2(440,234)); startLogo.gameObject.AddComponent<ArcadeLogo>().raycastTarget=false;
            T("Tagline",startCard,"One clean drop. One more high score.",21,new Vector2(0,67),new Vector2(620,38),White,true);
            T("ModeLabel",startCard,"CHOOSE YOUR DIFFICULTY",13,new Vector2(0,14),new Vector2(620,30),Muted);
            var modeButtons=new List<Button>(); string[] names={"NOOB","NEUTRAL","PRO"}; string[] details={"CLASSIC PIECES\nRELAXED PACE","ALL 16 SHAPES\nSTEADY PACE","ALL 16 SHAPES\nFAST + PRECISE"};
            for(int i=0;i<3;i++) {
                var b=Btn("Mode_"+names[i],startCard,"",new Vector2((i-1)*212,-83),new Vector2(196,142),DifficultyRules.Color((GameDifficulty)i),20); modeButtons.Add(b);
                T("ModeTitle",b.transform,names[i],25,new Vector2(0,28),new Vector2(170,36),DifficultyRules.Color((GameDifficulty)i));
                T("ModeDetail",b.transform,details[i],13,new Vector2(0,-14),new Vector2(174,50),White,true);
                T("Selected",b.transform,i==1?"SELECTED":"",10,new Vector2(0,-48),new Vector2(170,24),White);
            }
            SetList(ui,"modeButtons",modeButtons);
            var difficultyDescription=T("DifficultyDescription",startCard,"16 SHAPES / A STEADY CLIMB",13,new Vector2(0,-188),new Vector2(620,30),Muted);
            var menuBest=T("MenuBest",startCard,"NEUTRAL BEST  0",17,new Vector2(0,-224),new Vector2(620,34),Amber);
            var play=Btn("Play",startCard,"LET'S PLAY   >",new Vector2(0,-289),new Vector2(400,68),Cyan,25,true);
            var menuHelp=Btn("Help",startCard,"HOW TO PLAY / ENTER TO START",new Vector2(0,-354),new Vector2(400,42),Cyan,13);
            Set(ui,"startModal",start); Set(ui,"playButton",play); Set(ui,"menuHelpButton",menuHelp); Set(ui,"menuBestText",menuBest); Set(ui,"difficultyDescription",difficultyDescription);

            var settings=Overlay("PauseMenu",root.transform); var pauseCard=Card(settings,"PauseCard",new Vector2(580,650),Cyan);
            T("Eyebrow",pauseCard,"TAKE A BREATHER",12,new Vector2(0,278),new Vector2(500,30),Muted); T("Heading",pauseCard,"PAUSED",42,new Vector2(0,232),new Vector2(500,62),White);
            var runStats=T("RunStats",pauseCard,"NEUTRAL / 0 PTS / 90s LEFT",14,new Vector2(0,176),new Vector2(520,34),Cyan);
            var sound=ToggleRow(pauseCard,"Sound effects","CRISP HITS + CLEAR CHIMES",101,out _); var music=ToggleRow(pauseCard,"Music","ORIGINAL ARCADE LOOP",22,out _);
            var motion=ToggleRow(pauseCard,"Screen effects","SHAKE + HEARTBEAT ANIMATIONS",-57,out _); var haptics=ToggleRow(pauseCard,"Haptics","VIBRATION FEEDBACK",-136,out var hapticCaption);
            var resume=Btn("Resume",pauseCard,"BACK TO THE STACK   >",new Vector2(0,-230),new Vector2(480,64),Cyan,21,true);
            var pauseMenu=Btn("MainMenu",pauseCard,"MAIN MENU",new Vector2(-125,-291),new Vector2(230,42),Cyan,14); var pauseRestart=Btn("Restart",pauseCard,"RESTART RUN",new Vector2(125,-291),new Vector2(230,42),Amber,14);
            Set(ui,"settingsModal",settings); Set(ui,"resumeButton",resume); Set(ui,"pauseMenuButton",pauseMenu); Set(ui,"pauseRestartButton",pauseRestart); Set(ui,"runStatsText",runStats);
            Set(ui,"soundToggle",sound); Set(ui,"musicToggle",music); Set(ui,"motionToggle",motion); Set(ui,"hapticsToggle",haptics); Set(ui,"hapticsCaption",hapticCaption);

            var restartModal=Overlay("RestartModal",root.transform); var restartCard=Card(restartModal,"RestartCard",new Vector2(580,384),Amber);
            T("Eyebrow",restartCard,"HOLD UP",12,new Vector2(0,147),new Vector2(500,28),Amber);
            var restartTitle=T("Heading",restartCard,"FRESH START?",35,new Vector2(0,96),new Vector2(520,58),White);
            var restartBody=T("Warning",restartCard,"Your current points will reset.\nYour personal best stays with you.",20,new Vector2(0,14),new Vector2(500,94),Muted,true);
            var confirm=Btn("ConfirmRestart",restartCard,"CONFIRM",new Vector2(-126,-116),new Vector2(236,62),Amber,21,true); var cancel=Btn("ContinueRun",restartCard,"CANCEL",new Vector2(126,-116),new Vector2(236,62),Cyan,21);
            Set(ui,"restartModal",restartModal); Set(ui,"restartConfirmButton",confirm); Set(ui,"restartContinueButton",cancel); Set(ui,"restartTitleText",restartTitle); Set(ui,"restartBodyText",restartBody);

            var results=Overlay("ResultsMenu",root.transform); var resultCard=Card(results,"ResultsCard",new Vector2(620,700),Cyan);
            var resultTitle=T("Heading",resultCard,"TIME'S UP!",42,new Vector2(0,292),new Vector2(560,66),White);
            var finalMode=T("Mode",resultCard,"NEUTRAL / 90 SECOND SPRINT",13,new Vector2(0,242),new Vector2(560,30),Muted);
            var rank=T("Rank",resultCard,"C / KEEP STACKING",16,new Vector2(0,196),new Vector2(560,36),Cyan);
            var finalScore=T("FinalScore",resultCard,"0",82,new Vector2(0,122),new Vector2(560,108),White);
            var record=Panel("NewBest",resultCard,new Vector2(380,34),new Vector2(0,48),Amber); T("Record",record,"NEW PERSONAL BEST",14,Vector2.zero,new Vector2(360,30),Amber);
            var finalBest=T("FinalBest",resultCard,"PERSONAL BEST  0",16,new Vector2(0,0),new Vector2(560,36),Muted);
            var resultLines=T("FinalLines",resultCard,"00  LINES",20,new Vector2(-132,-55),new Vector2(250,40),Cyan); var resultCombo=T("FinalCombo",resultCard,"x1.0  MAX COMBO",20,new Vector2(132,-55),new Vector2(250,40),Amber);
            var breakdown=T("Breakdown",resultCard,"",14,new Vector2(0,-125),new Vector2(560,76),Muted,true);
            var again=Btn("PlayAgain",resultCard,"ONE MORE RUN   >",new Vector2(0,-227),new Vector2(500,66),Cyan,24,true); var resultMenu=Btn("ChangeMode",resultCard,"CHANGE DIFFICULTY",new Vector2(0,-295),new Vector2(500,44),Cyan,15);
            Set(ui,"gameOverModal",results); Set(ui,"gameOverTitleText",resultTitle); Set(ui,"finalScoreText",finalScore); Set(ui,"finalHighScoreText",finalBest); Set(ui,"finalLinesText",resultLines); Set(ui,"finalMaxStreakText",resultCombo); Set(ui,"finalBreakdownText",breakdown); Set(ui,"finalModeText",finalMode); Set(ui,"finalRankText",rank); Set(ui,"newRecordBadge",record.gameObject); Set(ui,"playAgainButton",again); Set(ui,"resultMenuButton",resultMenu);

            var helpModal=Overlay("HowToPlay",root.transform); var helpCard=Card(helpModal,"HelpCard",new Vector2(620,700),Cyan);
            T("Heading",helpCard,"MAKE ROOM.",39,new Vector2(0,283),new Vector2(560,66),White);
            T("Intro",helpCard,"Fill rows. Keep the stack low.\nChain clears to multiply your points.",21,new Vector2(0,193),new Vector2(540,92),Muted,true);
            string[] keys={"ARROWS / WASD","UP / X    /    Z / Q","SPACE","C / SHIFT","ESC / P"}; string[] actions={"MOVE + SOFT DROP","ROTATE RIGHT / LEFT","INSTANT DROP","HOLD + SWAP","PAUSE"};
            for(int i=0;i<5;i++) { var y=85-i*50; T("Key_"+i,helpCard,keys[i],15,new Vector2(-128,y),new Vector2(260,38),Cyan); T("Action_"+i,helpCard,actions[i],14,new Vector2(140,y),new Vector2(270,38),White,true); }
            T("Tip",helpCard,"The outline is your landing guide.\nReach your mode's line target for a bonus.\nThe pace climbs every 15 seconds.",16,new Vector2(0,-186),new Vector2(540,90),Muted,true);
            var gotIt=Btn("GotIt",helpCard,"GOT IT   >",new Vector2(0,-291),new Vector2(480,58),Cyan,22,true); Set(ui,"helpModal",helpModal); Set(ui,"helpCloseButton",gotIt);

            start.SetActive(true); settings.SetActive(false); restartModal.SetActive(false); results.SetActive(false); helpModal.SetActive(false);
            var layout=root.AddComponent<ResponsiveGameLayout>(); Set(layout,"scaler",scaler); Set(layout,"gameCamera",cam); Set(layout,"backdrop",bg); Set(layout,"boardFrame",frame); Set(layout,"header",header);
            Set(layout,"holdPanel",hold); Set(layout,"statsPanel",stats); Set(layout,"nextPanel",next); Set(layout,"timerPanel",timer); Set(layout,"goalPanel",goal); Set(layout,"dangerPanel",danger); Set(layout,"controls",controls); Set(layout,"footer",footer);
            layout.ApplyLayout(); EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(scene.path,true)};
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions"); DropStackWebBuild.Configure();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Debug.Log("[DropStack] Arcade UI, difficulty selector and responsive layouts saved.");
        }
        private static GameObject Obj(string name,Transform parent) { var go=new GameObject(name,typeof(RectTransform)); if(parent!=null) go.transform.SetParent(parent,false); return go; }
        private static void Rect(RectTransform rt,Vector2 pos,Vector2 size) { rt.anchoredPosition=pos; rt.sizeDelta=size; }
        private static RectTransform Panel(string name,Transform parent,Vector2 size,Vector2 pos,Color edge)
        { var rt=Obj(name,parent).GetComponent<RectTransform>(); Rect(rt,pos,size); var surface=rt.gameObject.AddComponent<ArcadeSurface>(); surface.edge=edge; surface.raycastTarget=false; return rt; }
        private static Text T(string name,Transform parent,string text,int size,Vector2 pos,Vector2 bounds,Color tint,bool ui=false)
        { var rt=Obj(name,parent).GetComponent<RectTransform>(); Rect(rt,pos,bounds); var label=rt.gameObject.AddComponent<Text>(); label.font=ui?body:display; label.fontStyle=FontStyle.Normal; label.fontSize=size; label.color=tint; label.text=text; label.alignment=TextAnchor.MiddleCenter; label.raycastTarget=false; return label; }
        private static Image I(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
        { var rt=Obj(name,parent).GetComponent<RectTransform>(); Rect(rt,pos,size); var image=rt.gameObject.AddComponent<Image>(); image.color=color; image.raycastTarget=false; return image; }
        private static Image Fill(string name,Transform parent,Vector2 size,Vector2 pos,Color color)
        { var image=I(name,parent,size,pos,color); image.sprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"); image.type=Image.Type.Filled; image.fillMethod=Image.FillMethod.Horizontal; image.fillAmount=0; return image; }
        private static void Rule(Transform parent,float y) => I("Divider",parent,new Vector2(150,1),new Vector2(0,y),new Color(.17f,.29f,.37f));
        private static Button Btn(string name,Transform parent,string title,Vector2 pos,Vector2 size,Color accent,int fontSize,bool filled=false)
        {
            var rt=Panel(name,parent,size,pos,accent*.7f); var surface=rt.GetComponent<ArcadeSurface>(); surface.top=Color.Lerp(new Color(.04f,.09f,.15f),accent,filled?.7f:.18f); surface.bottom=Color.Lerp(new Color(.015f,.04f,.075f),accent,filled?.37f:.08f); surface.bright=filled; surface.corner=7; surface.raycastTarget=true;
            var button=rt.gameObject.AddComponent<Button>(); button.targetGraphic=surface; button.navigation=new Navigation{mode=Navigation.Mode.None}; var colors=button.colors; colors.highlightedColor=new Color(1.15f,1.15f,1.15f); colors.pressedColor=new Color(.8f,.88f,.95f); colors.disabledColor=new Color(.55f,.6f,.66f,.6f); button.colors=colors;
            T("Label",rt,title,fontSize,new Vector2(0,2),size-new Vector2(12,4),White,true); rt.gameObject.AddComponent<ArcadeButtonFeedback>(); return button;
        }
        private static GameObject Overlay(string name,Transform parent,float alpha=.88f)
        { var image=I(name,parent,Vector2.zero,Vector2.zero,new Color(.006f,.015f,.03f,alpha)); var rt=image.rectTransform; rt.anchorMin=Vector2.zero; rt.anchorMax=Vector2.one; rt.offsetMin=rt.offsetMax=Vector2.zero; image.raycastTarget=true; var group=image.gameObject.AddComponent<CanvasGroup>(); group.blocksRaycasts=true; image.gameObject.AddComponent<ArcadeModalMotion>(); return image.gameObject; }
        private static RectTransform Card(GameObject overlay,string name,Vector2 size,Color color) { var card=Panel(name,overlay.transform,size,Vector2.zero,color*.7f); card.GetComponent<ArcadeSurface>().corner=14; overlay.GetComponent<ArcadeModalMotion>().card=card; return card; }
        private static UIToggleSwitch ToggleRow(Transform parent,string label,string caption,float y,out Text sublabel)
        {
            var row=Panel(label+"Row",parent,new Vector2(500,70),new Vector2(0,y),new Color(.11f,.22f,.3f)); row.GetComponent<ArcadeSurface>().corner=4;
            T("Name",row,label,21,new Vector2(-62,13),new Vector2(326,34),White,true).alignment=TextAnchor.MiddleLeft;
            sublabel=T("Caption",row,caption,10,new Vector2(-62,-16),new Vector2(326,24),Muted); sublabel.alignment=TextAnchor.MiddleLeft;
            var track=Panel("Toggle",row,new Vector2(106,44),new Vector2(181,0),Green*.6f); track.GetComponent<ArcadeSurface>().corner=3; track.GetComponent<ArcadeSurface>().raycastTarget=true;
            var button=track.gameObject.AddComponent<Button>(); button.targetGraphic=track.GetComponent<ArcadeSurface>(); button.navigation=new Navigation{mode=Navigation.Mode.None};
            var knob=Panel("Knob",track,new Vector2(38,32),new Vector2(27,0),Green); knob.GetComponent<ArcadeSurface>().corner=2;
            var state=T("State",track,"ON",12,new Vector2(-18,0),new Vector2(54,28),Green);
            var toggle=track.gameObject.AddComponent<UIToggleSwitch>(); Set(toggle,"button",button); Set(toggle,"knob",knob); Set(toggle,"stateText",state); Set(toggle,"track",track.GetComponent<ArcadeSurface>()); return toggle;
        }
        private static List<Image> Border(RectTransform parent,Color color,float thickness,float inset)
        {
            var result=new List<Image>();
            foreach(bool vertical in new[]{true,false}) foreach(int sign in new[]{-1,1}) {
                var image=I("FrameEdge",parent,Vector2.zero,Vector2.zero,color); var bar=image.rectTransform;
                if(vertical) { bar.anchorMin=new Vector2(sign<0?0:1,0); bar.anchorMax=new Vector2(sign<0?0:1,1); bar.sizeDelta=new Vector2(thickness,-inset*2); bar.anchoredPosition=new Vector2(-sign*(inset+thickness*.5f),0); }
                else { bar.anchorMin=new Vector2(0,sign<0?0:1); bar.anchorMax=new Vector2(1,sign<0?0:1); bar.sizeDelta=new Vector2(-inset*2,thickness); bar.anchoredPosition=new Vector2(0,-sign*(inset+thickness*.5f)); }
                result.Add(image);
            }
            return result;
        }
        private static void Set(Object target,string field,Object value) { var so=new SerializedObject(target); var p=so.FindProperty(field); if(p==null) throw new System.Exception("Missing binding: "+field); p.objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void Float(Object target,string field,float value) { var so=new SerializedObject(target); so.FindProperty(field).floatValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetList<T>(Object target,string field,List<T> values) where T:Object { var so=new SerializedObject(target); var p=so.FindProperty(field); p.arraySize=values.Count; for(int i=0;i<values.Count;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
