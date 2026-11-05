using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ChickenRush
{
    public sealed class RushHUD
    {
        sealed class Widget
        {
            public RectTransform rect;public Image image;public TextMeshProUGUI text;public Button button;public Rect bounds;
        }
        [Serializable] public sealed class ControlRect { public string name;public float x,y,width,height; }
        [Serializable] public sealed class UISnapshot
        {
            public string layout,clippedTextNames;public float width,height;public Rect viewport;public int overflowingTexts;public ControlRect[] controls;
        }
        readonly Sprite panel,gear,arrow,coin;
        readonly TMP_FontAsset font;
        readonly Canvas canvas;
        readonly CanvasScaler scaler;
        readonly RectTransform root;
        readonly List<TextMeshProUGUI> texts=new List<TextMeshProUGUI>();
        readonly Widget header,footer,edge,roadTap,brand,subtitle,wallet,best,difficulty,settings,multiplier,potential,risk,riskTitle,stake,minus,plus,hop,cash,status,next,roundLabel;
        readonly Widget scrim,modal,title,help,sound,fullscreen,resume,keys;
        readonly CanvasGroup modalFade;
        readonly Image hopArrow;
        readonly UISnapshot report=new UISnapshot();
        readonly Vector3[] corners=new Vector3[4];
        readonly Color cream=RoadWorld.Hex("FFF3DC"),muted=RoadWorld.Hex("A2B1BC"),navy=RoadWorld.Hex("132234");
        readonly Dictionary<string,Widget> controls=new Dictionary<string,Widget>();
        RushLayout layout;
        Rect lastSafe;
        int pixelWidth,pixelHeight;
        float cssWidth,cssHeight,displayCoins,targetCoins,multiplierPop,modalAlpha;
        bool paused,choosing=true,hasWallet;
        string modeName="NORMAL",bestCaption="1x";
        float lastMultiplier=1;
        RoundRules latestRules;
        RushDifficulty latestConfig;
        public RushLayout Layout => layout;
        public Rect CameraRect {get;private set;}

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern float RushCssWidth();
        [DllImport("__Internal")] static extern float RushCssHeight();
#endif
        public RushHUD(Transform parent,Sprite panelSprite,Sprite gearSprite,Sprite arrowSprite,Sprite coinSprite,TMP_FontAsset fontAsset,
            Action primary,Action bank,Action<int> changeStake,Action cycle,Action pause,Action toggleSound,Action requestFullscreen)
        {
            panel=panelSprite;gear=gearSprite;arrow=arrowSprite;coin=coinSprite;font=fontAsset;
            var go=new GameObject("Responsive HUD",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(parent,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.pixelPerfect=true;
            scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;scaler.referencePixelsPerUnit=100;
            root=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(go.transform,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            roadTap=Box("Road input",root,Color.clear,false);roadTap.image.raycastTarget=true;roadTap.button=roadTap.rect.gameObject.AddComponent<Button>();roadTap.button.onClick.AddListener(()=>primary());roadTap.button.navigation=new Navigation{mode=Navigation.Mode.None};
            header=Box("Information bar",root,navy,false);footer=Box("Control panel",root,RoadWorld.Hex("1E3448"));edge=Box("Dock edge",root,RoadWorld.Hex("35485A"),false);
            brand=Text("Brand",root,"CHICKEN RUSH",26,cream,TextAlignmentOptions.Left,true);
            subtitle=Text("Brand caption",root,"C A S H  O U T !",11,RoadWorld.Hex("EAC16F"),TextAlignmentOptions.Left,true);
            wallet=Card("Wallet",root,18);best=Card("Best",root,18);
            difficulty=Button("difficulty","NORMAL",root,RoadWorld.Hex("26394C"),14,cycle);
            settings=Button("settings","",root,RoadWorld.Hex("26394C"),18,pause);Icon(settings,gear,24,Vector2.zero,cream);
            multiplier=Text("Current multiplier",root,"1.00x",52,cream,TextAlignmentOptions.Left,true);
            potential=Text("Potential reward",root,"Choose your risk",14,muted,TextAlignmentOptions.Left);
            next=Text("Next multiplier and chance",root,"NEXT HOP",18,RoadWorld.Hex("9AE7CA"),TextAlignmentOptions.Left,true);
            roundLabel=Text("Round label",root,"YOUR CROSSING",13,muted,TextAlignmentOptions.Left,true);
            risk=Group("Risk selector",root);riskTitle=Text("Risk caption",risk.rect,"RISK",10,muted,TextAlignmentOptions.Center,true);
            stake=Text("Risk or lane",risk.rect,"1",20,cream,TextAlignmentOptions.Center,true);
            minus=Button("minus","-",risk.rect,RoadWorld.Hex("293D50"),23,()=>changeStake(-1));
            plus=Button("plus","+",risk.rect,RoadWorld.Hex("293D50"),23,()=>changeStake(1));
            hop=Button("hop","PLAY",root,RoadWorld.Hex("73DBB4"),21,primary);hop.text.color=navy;
            hopArrow=Icon(hop,arrow,16,new Vector2(-24,0),navy);hopArrow.rectTransform.anchorMin=hopArrow.rectTransform.anchorMax=new Vector2(1,.5f);hopArrow.rectTransform.localEulerAngles=new Vector3(0,0,-90);
            hop.text.rectTransform.offsetMax=new Vector2(-34,-2);hop.text.rectTransform.offsetMin=new Vector2(14,2);
            cash=Button("cash","CASH OUT",root,RoadWorld.Hex("FFD06B"),20,bank);cash.text.color=RoadWorld.Hex("453326");
            var cashCoin=Icon(cash,coin,14,new Vector2(23,0),RoadWorld.Hex("6B4D28"));cashCoin.rectTransform.anchorMin=cashCoin.rectTransform.anchorMax=new Vector2(0,.5f);
            cash.text.rectTransform.offsetMin=new Vector2(34,2);
            status=Text("Round feedback",root,"Watch both directions. Hop right.",14,muted,TextAlignmentOptions.Left);
            scrim=Box("Settings overlay",root,new Color(.025f,.05f,.09f,.86f),false);scrim.image.raycastTarget=true;
            modalFade=scrim.rect.gameObject.AddComponent<CanvasGroup>();modalFade.alpha=0;modalFade.blocksRaycasts=false;modalFade.interactable=false;
            modal=Box("Settings card",scrim.rect,RoadWorld.Hex("203449"),true);
            title=Text("Settings title",modal.rect,"TAKE A BREATHER",23,cream,TextAlignmentOptions.Center,true);
            help=Text("Controls help",modal.rect,"Each hop rolls a new risk.\nWaiting does not change the odds.\nCash out between hops. Virtual coins only.",14,muted);
            sound=Button("sound","SOUND ON",modal.rect,RoadWorld.Hex("304A60"),14,toggleSound);
            fullscreen=Button("fullscreen","FULLSCREEN",modal.rect,RoadWorld.Hex("304A60"),14,requestFullscreen);
            resume=Button("resume","KEEP GOING",modal.rect,RoadWorld.Hex("73DBB4"),19,pause);resume.text.color=navy;
            keys=Text("Keyboard help",modal.rect,"SPACE / RIGHT  Hop     C  Cash out     P  Pause",11,muted);
            scrim.rect.gameObject.SetActive(false);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(parent,false);events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            report.controls=new ControlRect[controls.Count];int index=0;foreach(var pair in controls)report.controls[index++]=new ControlRect{name=pair.Key};
            Resize(true);
        }
        Widget Group(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);return new Widget{rect=rect};
        }
        Widget Box(string name,Transform parent,Color color,bool rounded=true)
        {
            var w=Group(name,parent);w.image=w.rect.gameObject.AddComponent<Image>();w.image.color=color;w.image.raycastTarget=false;
            if(rounded){w.image.sprite=panel;w.image.type=Image.Type.Sliced;w.image.pixelsPerUnitMultiplier=2;}
            return w;
        }
        Widget Text(string name,Transform parent,string value,float size,Color color,TextAlignmentOptions alignment=TextAlignmentOptions.Center,bool bold=false)
        {
            var w=Group(name,parent);w.text=w.rect.gameObject.AddComponent<TextMeshProUGUI>();ConfigureText(w.text,value,size,color,alignment,bold);return w;
        }
        void ConfigureText(TextMeshProUGUI text,string value,float size,Color color,TextAlignmentOptions alignment,bool bold)
        {
            text.font=font;text.text=value;text.fontSize=size;text.color=color;text.alignment=alignment;text.fontStyle=bold?FontStyles.Bold:FontStyles.Normal;
            text.raycastTarget=false;text.enableAutoSizing=true;text.fontSizeMin=size*.78f;text.fontSizeMax=size;
            text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Ellipsis;texts.Add(text);
        }
        Widget Card(string name,Transform parent,float size)
        {
            var w=Box(name,parent,RoadWorld.Hex("203348"));w.text=new GameObject(name+" label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();w.text.transform.SetParent(w.rect,false);
            Stretch(w.text.rectTransform,10,2);ConfigureText(w.text,"",size,cream,TextAlignmentOptions.Center,false);return w;
        }
        Widget Button(string id,string label,Transform parent,Color color,float size,Action action)
        {
            var w=Box(id,parent,color);w.image.raycastTarget=true;w.button=w.rect.gameObject.AddComponent<Button>();w.button.targetGraphic=w.image;
            var colors=w.button.colors;colors.highlightedColor=new Color(1.04f,1.04f,1.04f);colors.pressedColor=new Color(.86f,.86f,.86f);colors.disabledColor=new Color(.56f,.61f,.65f,.82f);colors.fadeDuration=.1f;w.button.colors=colors;
            w.button.navigation=new Navigation{mode=Navigation.Mode.None};w.button.onClick.AddListener(()=>action());w.rect.gameObject.AddComponent<ButtonFeel>();
            w.text=new GameObject(id+" label",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();w.text.transform.SetParent(w.rect,false);Stretch(w.text.rectTransform,12,2);
            ConfigureText(w.text,label,size,cream,TextAlignmentOptions.Center,true);controls.Add(id,w);return w;
        }
        static void Stretch(RectTransform rect,float horizontal,float vertical)
        {
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(horizontal,vertical);rect.offsetMax=new Vector2(-horizontal,-vertical);
        }
        Image Icon(Widget parent,Sprite sprite,float size,Vector2 position,Color color)
        {
            var image=new GameObject("Icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();image.transform.SetParent(parent.rect,false);
            image.rectTransform.anchorMin=image.rectTransform.anchorMax=new Vector2(.5f,.5f);image.rectTransform.sizeDelta=Vector2.one*size;image.rectTransform.anchoredPosition=position;
            image.sprite=sprite;image.color=color;image.preserveAspect=true;image.raycastTarget=false;return image;
        }
        static void Place(Widget widget,Rect bounds)
        {
            widget.bounds=bounds;widget.rect.anchorMin=widget.rect.anchorMax=new Vector2(0,1);widget.rect.pivot=new Vector2(.5f,.5f);
            widget.rect.anchoredPosition=new Vector2(Mathf.Round(bounds.x+bounds.width*.5f),-Mathf.Round(bounds.y+bounds.height*.5f));widget.rect.sizeDelta=new Vector2(Mathf.Round(bounds.width),Mathf.Round(bounds.height));
        }
        public bool Resize(bool force=false)
        {
            float w=Screen.width,h=Screen.height;
#if UNITY_WEBGL && !UNITY_EDITOR
            w=Mathf.Max(1,RushCssWidth());h=Mathf.Max(1,RushCssHeight());
#endif
            if(!force && pixelWidth==Screen.width && pixelHeight==Screen.height && Mathf.Abs(cssWidth-w)<.5f && Mathf.Abs(cssHeight-h)<.5f && lastSafe==Screen.safeArea)return false;
            pixelWidth=Screen.width;pixelHeight=Screen.height;cssWidth=w;cssHeight=h;lastSafe=Screen.safeArea;
            scaler.referenceResolution=new Vector2(w,h);
            root.anchorMin=new Vector2(lastSafe.xMin/pixelWidth,lastSafe.yMin/pixelHeight);root.anchorMax=new Vector2(lastSafe.xMax/pixelWidth,lastSafe.yMax/pixelHeight);
            float safeWidth=w*lastSafe.width/pixelWidth,safeHeight=h*lastSafe.height/pixelHeight;
            layout=RushLayout.Calculate(safeWidth,safeHeight);
            Place(header,new Rect(0,0,safeWidth,layout.top));Place(footer,layout.controlPanel);edge.rect.gameObject.SetActive(false);
            Place(roadTap,layout.viewport);Place(brand,layout.brand);Place(subtitle,layout.subtitle);subtitle.rect.gameObject.SetActive(layout.subtitle.height>0);
            brand.text.fontSizeMax=layout.Compact?20:layout.mode==RushDisplay.Desktop?30:22;
            brand.text.text=layout.Compact && safeWidth<750?"RUSH":"CHICKEN RUSH";
            Place(wallet,layout.wallet);Place(best,layout.best);Place(difficulty,layout.difficulty);Place(settings,layout.settings);
            Place(multiplier,layout.multiplier);Place(potential,layout.potential);Place(risk,layout.risk);Place(hop,layout.hop);Place(cash,layout.cash);Place(status,layout.status);
            Place(next,layout.next);Place(roundLabel,layout.roundLabel);roundLabel.rect.gameObject.SetActive(layout.roundLabel.height>0);
            status.rect.gameObject.SetActive(layout.status.height>0);multiplier.text.fontSizeMax=layout.mode==RushDisplay.Desktop?52:34;multiplier.text.fontSizeMin=28;
            potential.text.fontSizeMax=layout.Compact?13:16;potential.text.fontSizeMin=12;
            next.text.fontSizeMax=layout.mode==RushDisplay.Desktop?19:14;next.text.fontSizeMin=12;
            status.text.textWrappingMode=layout.mode==RushDisplay.Desktop?TextWrappingModes.Normal:TextWrappingModes.NoWrap;
            PlaceRisk();
            Place(scrim,new Rect(0,0,safeWidth,safeHeight));float modalWidth=Mathf.Min(420,safeWidth-32),modalHeight=Mathf.Min(330,safeHeight-24);
            Place(modal,new Rect((safeWidth-modalWidth)*.5f,(safeHeight-modalHeight)*.5f,modalWidth,modalHeight));
            Place(title,new Rect(16,18,modalWidth-32,34));Place(help,new Rect(20,61,modalWidth-40,74));
            float buttonWidth=(modalWidth-52)*.5f;
            Place(sound,new Rect(20,151,buttonWidth,46));Place(fullscreen,new Rect(32+buttonWidth,151,buttonWidth,46));Place(resume,new Rect(20,215,modalWidth-40,54));Place(keys,new Rect(14,281,modalWidth-28,24));
            keys.rect.gameObject.SetActive(layout.mode==RushDisplay.Desktop);
            float sx=lastSafe.xMin/pixelWidth,sy=lastSafe.yMin/pixelHeight,sw=lastSafe.width/pixelWidth,sh=lastSafe.height/pixelHeight;
            CameraRect=new Rect(sx+layout.viewport.x/safeWidth*sw,sy+(safeHeight-layout.viewport.yMax)/safeHeight*sh,layout.viewport.width/safeWidth*sw,layout.viewport.height/safeHeight*sh);
            UpdateHeader();if(latestRules!=null)UpdateNext(latestRules,latestConfig);Canvas.ForceUpdateCanvases();return true;
        }
        void PlaceRisk()
        {
            float width=layout.risk.width;
            if(choosing)
            {
                bool compact=layout.Compact;float y=compact?2:14;
                Place(minus,new Rect(0,y,44,44));Place(plus,new Rect(width-44,y,44,44));
                Place(riskTitle,new Rect(48,compact?0:0,width-96,compact?14:13));
                Place(stake,new Rect(48,compact?14:14,width-96,compact?30:44));
            }
            else{Place(riskTitle,new Rect(0,0,width,14));Place(stake,new Rect(0,15,width,35));}
        }
        public void Tick(float dt)
        {
            if(multiplierPop>0){multiplierPop=Mathf.Max(0,multiplierPop-dt);float pop=Mathf.Sin(multiplierPop/.28f*Mathf.PI)*.10f;multiplier.rect.localScale=Vector3.one*(1+Mathf.Max(0,pop));}
            if(Mathf.Abs(displayCoins-targetCoins)>.05f){displayCoins=Mathf.Lerp(displayCoins,targetCoins,1-Mathf.Exp(-dt*12));UpdateWallet();}
            else if(displayCoins!=targetCoins){displayCoins=targetCoins;UpdateWallet();}
            modalAlpha=Mathf.MoveTowards(modalAlpha,paused?1:0,dt*8);modalFade.alpha=modalAlpha;
            if(!paused && modalAlpha<=0 && scrim.rect.gameObject.activeSelf)scrim.rect.gameObject.SetActive(false);
        }
        static string Coins(int value)=>value>=1000000?(value/1000000f).ToString("0.##")+"M":value>=10000?(value/1000f).ToString("0.#")+"K":value.ToString("N0");
        bool NarrowStats=>layout!=null && layout.mode==RushDisplay.Portrait;
        void UpdateWallet(){wallet.text.text=(NarrowStats?"<size=9>COINS</size>\n<b>":"<size=10>COINS</size>  <b>")+Coins(Mathf.RoundToInt(displayCoins))+"</b>";}
        void UpdateHeader()
        {
            if(hasWallet)UpdateWallet();
            best.text.text=(NarrowStats?"<size=9>BEST</size>\n<b>":"<size=10>BEST</size>  <b>")+bestCaption+"</b>";
            difficulty.text.text=(NarrowStats?"<size=9>MODE</size>\n":"<size=9>MODE</size>  ")+modeName;
        }
        public void Pause(bool value)
        {
            paused=value;modalFade.blocksRaycasts=value;modalFade.interactable=value;if(value)scrim.rect.gameObject.SetActive(true);
        }
        public void Sound(bool enabled){sound.text.text=enabled?"SOUND ON":"SOUND OFF";}
        void UpdateNext(RoundRules rules,RushDifficulty config)
        {
            bool twoLines=layout.mode==RushDisplay.Desktop || layout.Compact;
            next.text.color=RoadWorld.Hex(rules.State==RoundState.CashedOut?"FFD06B":rules.State==RoundState.Lost?"B0BECB":"9AE7CA");
            next.text.text=rules.State==RoundState.CashedOut?"SECURED "+Coins(rules.Reward)+" COINS":rules.State==RoundState.Lost?"A FRESH CROSSING AWAITS":
                rules.Row<config.multipliers.Length?"NEXT "+config.multipliers[rules.Row].ToString("0.##")+"x · "+Coins(rules.NextPotentialReward)+(rules.NextPotentialReward==1?" COIN":" COINS")+(twoLines?"\n":"  ·  ")+(rules.NextChance*100).ToString("0.#")+"% chance":"ROAD CLEARED";
        }
        public void Refresh(RoundRules rules,RushDifficulty config,float bestValue,string notice,bool canRestart)
        {
            latestRules=rules;latestConfig=config;
            targetCoins=rules.Balance;if(!hasWallet){displayCoins=targetCoins;hasWallet=true;}
            bestCaption=bestValue.ToString("0.##")+"x";modeName=config.displayName;UpdateHeader();
            if(lastMultiplier!=rules.Multiplier){lastMultiplier=rules.Multiplier;multiplierPop=.28f;}
            multiplier.text.text=rules.Multiplier.ToString("0.00")+"x";
            multiplier.text.color=RoadWorld.Hex(rules.State==RoundState.CashedOut?"FFD06B":rules.State==RoundState.Lost?"FF9D86":"FFF3DC");
            choosing=rules.CanStart && canRestart;minus.rect.gameObject.SetActive(choosing);plus.rect.gameObject.SetActive(choosing);
            riskTitle.text.text=choosing?"RISK COINS":"AT RISK";stake.text.text=rules.Stake.ToString();PlaceRisk();
            UpdateNext(rules,config);
            roundLabel.text.text=rules.State==RoundState.CashedOut?"COINS SECURED":rules.State==RoundState.Lost?"ROUND OVER":"YOUR CROSSING";
            difficulty.button.interactable=choosing;
            hop.button.interactable=canRestart && (rules.CanStart || rules.State==RoundState.Safe);
            cash.button.interactable=rules.State==RoundState.Safe && rules.Row>0;
            hop.text.text=rules.CanStart?(rules.Balance==0?"FREE COINS":rules.Finished?"PLAY AGAIN":"PLAY"):rules.State==RoundState.Hopping?"HOPPING":rules.State==RoundState.Preparing?"GET READY":"HOP";
            bool showArrow=rules.State==RoundState.Safe;
            hopArrow.gameObject.SetActive(showArrow);hop.text.rectTransform.offsetMin=new Vector2(showArrow?14:12,2);hop.text.rectTransform.offsetMax=new Vector2(showArrow?-34:-12,-2);
            cash.text.text=rules.State==RoundState.CashedOut?"BANKED +"+Coins(rules.Reward):rules.Row>0 && rules.State==RoundState.Safe?"CASH OUT "+Coins(rules.PotentialReward):"CASH OUT";
            potential.text.text=rules.State==RoundState.CashedOut?"Banked "+Coins(rules.Reward)+(rules.Reward==1?" coin":" coins"):
                rules.Row>0 && rules.State!=RoundState.Lost?"Bank "+Coins(rules.PotentialReward)+(rules.PotentialReward==1?" coin":" coins"):
                rules.State==RoundState.Lost?"Lost "+rules.Stake+(rules.Stake==1?" coin":" coins"):"Virtual coins only";
            status.text.text=notice;status.text.color=RoadWorld.Hex(rules.State==RoundState.Lost?"FF9D86":rules.State==RoundState.CashedOut?"FFD06B":"AEBFC6");
        }
        public UISnapshot Snapshot()
        {
            Canvas.ForceUpdateCanvases();report.layout=layout.mode.ToString();report.width=layout.width;report.height=layout.height;report.viewport=layout.viewport;report.overflowingTexts=0;report.clippedTextNames="";
            foreach(var text in texts)if(text.gameObject.activeInHierarchy && (text.isTextOverflowing || text.isTextTruncated))
            {report.overflowingTexts++;report.clippedTextNames+=text.gameObject.name+";";}
            int index=0;
            foreach(var pair in controls)
            {
                var widget=pair.Value;widget.rect.GetWorldCorners(corners);
                var rect=report.controls[index++];rect.x=corners[0].x/Screen.width*cssWidth;rect.y=(Screen.height-corners[1].y)/Screen.height*cssHeight;
                rect.width=(corners[2].x-corners[0].x)/Screen.width*cssWidth;rect.height=(corners[1].y-corners[0].y)/Screen.height*cssHeight;
            }
            return report;
        }
    }
}
