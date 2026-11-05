using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

namespace CoinFlip
{
    public sealed class UIManager : MonoBehaviour
    {
        UIFactory f;
        GameSession session;
        AudioManager sound;
        Canvas canvas;
        UnityEngine.UI.CanvasScaler scaler;
        RectTransform safe, content, header, stage, controls, history, footer, settingsPanel;
        RectTransform mainMenuPanel, mainMenuCard, pausePanel, pauseCard;
        RectTransform coinHome, coin, shadow, orbit, aura, stageTitle, eyebrow, resultBlock, resultIcon, stageHistory;
        RectTransform selectionRow, wagerBox, flipRect, controlsTitle, controlsSub, payout, balanceBox, logo, soundRect, divider, hint;
        RectTransform[] historySlots = new RectTransform[8];
        Image[] historyFaces = new Image[8];
        TextMeshProUGUI balance, resultTitle, resultSub, wager, flipLabel, returnLabel, streak, mutedLabel, muteButtonLabel;
        TextMeshProUGUI sectionTitle, sectionSub;
        Image coinImage, shadowImage, resultIconImage, selectedHeads, selectedTails;
        Button heads, tails, flip, minus, plus, pauseButton, reset;
        Button playButton, menuSettingsButton, resumeButton, pauseSettingsButton, mainMenuButton;
        Image headsFace, tailsFace;
        GameObject headsCheck, tailsCheck;
        float lastW, lastH,lastPixelRatio;
        Rect lastSafe;
        Coroutine balanceTween;
        int displayedBalance;
        bool pauseSettingsReturn;
        public RectTransform Coin => coin;
        public RectTransform CoinHome => coinHome;
        public RectTransform Shadow => shadow;
        public Image CoinImage => coinImage;
        public Image ShadowImage => shadowImage;
        public RectTransform Stage => stage;
        public float CoinSize { get; private set; }
        public bool IsMainMenu => mainMenuPanel!=null&&mainMenuPanel.gameObject.activeSelf;
        public bool IsPaused => pausePanel!=null&&pausePanel.gameObject.activeSelf;
        public event Action FlipRequested;
        public event Action ResetRequested;
        public event Action StartRequested;
        public event Action<bool> PauseChanged;
        public event Action MainMenuRequested;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern float CoinPixelRatio();
#endif

        public void Initialize(GameSession model, AudioManager sound, TMP_FontAsset font)
        {
            session = model; this.sound = sound; f = new UIFactory(font);
            canvas = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster)).GetComponent<Canvas>();
            canvas.transform.SetParent(transform, false); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            Image bg = f.Image("Navy background", canvas.transform, null, Palette.Background); UIFactory.Fill(bg.rectTransform);
            safe = UIFactory.Rect("Safe Area", canvas.transform); UIFactory.Fill(safe);
            content = UIFactory.Rect("Responsive Game", safe);
            header = UIFactory.Rect("Header", content); stage = UIFactory.Rect("Coin Stage", content);
            controls = UIFactory.Rect("Prediction Panel", content); footer = UIFactory.Rect("Footer", content);
            BuildHeader(); BuildStage(); BuildControls(); BuildFooter(); BuildSettings(); BuildMainMenu(); BuildPauseMenu();
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }
            displayedBalance = model.Data.balance; balance.text = displayedBalance.ToString();
            content.gameObject.SetActive(false);
            mainMenuPanel.gameObject.SetActive(true);
            pausePanel.gameObject.SetActive(false);
            mainMenuPanel.SetAsLastSibling();
            model.Changed += Refresh;
            Layout(); Refresh();
        }

        void BuildHeader()
        {
            logo = UIFactory.Rect("Coinflip wordmark", header);
            Image mark = f.Image("Logo mark", logo, "Sparkle", Palette.Gold); UIFactory.Place(mark.rectTransform,0,5,28,28);
            var brand = f.Text("Coinflip",logo,"coinflip",28,Palette.Text); brand.fontStyle = FontStyles.Bold; UIFactory.Place(brand.rectTransform,36,0,140,38);
            var edition = f.Text("Arcade edition",logo,"THE LUCKY LITTLE ARCADE",8.5f,Palette.Muted); edition.characterSpacing=2; UIFactory.Place(edition.rectTransform,37,37,190,13);
            Image bb=f.Image("Wallet",header,"Rounded",Palette.Raised); balanceBox=bb.rectTransform;
            Image icon=f.Image("Coin currency",balanceBox,"Heads",Color.white);UIFactory.Place(icon.rectTransform,12,12,28,28);
            balance=f.Text("Balance",balanceBox,"10",24,Palette.Text);UIFactory.Place(balance.rectTransform,50,4,78,32);
            var lbl=f.Text("Virtual coins",balanceBox,"VIRTUAL COINS",8.5f,Palette.Muted);lbl.characterSpacing=1.5f;UIFactory.Place(lbl.rectTransform,50,33,98,14);
            pauseButton=f.Button("Pause menu",header,Palette.Raised,TogglePause,sound);soundRect=(RectTransform)pauseButton.transform;
            Image pauseLeft=f.Image("Pause bar left",soundRect,"Rounded",Palette.Text);UIFactory.Place(pauseLeft.rectTransform,18,15,5,20);
            Image pauseRight=f.Image("Pause bar right",soundRect,"Rounded",Palette.Text);UIFactory.Place(pauseRight.rectTransform,28,15,5,20);
            Image line=f.Image("Header divider",header,null,Palette.Line);divider=line.rectTransform;
        }
        void BuildStage()
        {
            eyebrow=UIFactory.Rect("Stage eyebrow",stage);
            var label=f.Text("Stage label",eyebrow,"A  L I T T L E  L U C K .  A  L O V E L Y  F L I P .",10,Palette.Muted,TextAlignmentOptions.Center);UIFactory.Fill(label.rectTransform);
            stageTitle=UIFactory.Rect("Stage headline",stage);
            var title=f.Text("Headline",stageTitle,"Make your own luck.",32,Palette.Text,TextAlignmentOptions.Center);title.fontStyle=FontStyles.Bold;UIFactory.Fill(title.rectTransform);
            Image a=f.Image("Ambient halo",stage,"Aura",Color.white);aura=a.rectTransform;
            Image ring=f.Image("Orbital ring",stage,"Ring",new Color(.36f,.50f,.64f,.18f));orbit=ring.rectTransform;
            for(int i=0;i<7;i++)
            {
                float ang=(i*51+18)*Mathf.Deg2Rad;
                Image dot=f.Image("Orbit accent "+i,orbit,i%3==0?"Sparkle":"Circle",i%3==0?new Color(.96f,.73f,.26f,.55f):new Color(.5f,.7f,.8f,.35f));
                dot.rectTransform.anchorMin=dot.rectTransform.anchorMax=Vector2.one*.5f;
                dot.rectTransform.sizeDelta=Vector2.one*(i%3==0?11:3);dot.rectTransform.anchoredPosition=new Vector2(Mathf.Cos(ang),Mathf.Sin(ang))*145;
            }
            shadowImage=f.Image("Soft coin shadow",stage,"Shadow",Color.white);shadow=shadowImage.rectTransform;
            coinHome=UIFactory.Rect("Coin resting point",stage);coinImage=f.Image("Animated 2D coin",coinHome,"Heads",Color.white);coin=coinImage.rectTransform;UIFactory.Fill(coin);
            resultBlock=UIFactory.Rect("Result",stage);resultIconImage=f.Image("Outcome icon",resultBlock,"Sparkle",Palette.Gold);resultIcon=resultIconImage.rectTransform;
            resultTitle=f.Text("Result headline",resultBlock,"A good flip starts with a feeling.",16,Palette.Text,TextAlignmentOptions.Center);
            resultSub=f.Text("Result detail",resultBlock,"Heads or tails? You call it.",11,Palette.Muted,TextAlignmentOptions.Center);
            stageHistory=UIFactory.Rect("Recent results",stage);
            var recent=f.Text("Recent label",stageHistory,"RECENT FLIPS",9,Palette.Muted,TextAlignmentOptions.Center);recent.characterSpacing=2;UIFactory.Place(recent.rectTransform,0,0,300,18);
            history=UIFactory.Rect("History row",stageHistory);UIFactory.Place(history,0,25,300,32);
            var layout=history.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();layout.spacing=8;layout.childAlignment=TextAnchor.MiddleCenter;layout.childControlWidth=false;layout.childControlHeight=false;layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;
            for(int i=0;i<8;i++)
            {
                Image slot=f.Image("History "+i,history,"Circle",Palette.Raised);historySlots[i]=slot.rectTransform;slot.rectTransform.sizeDelta=Vector2.one*28;
                Image face=f.Image("Face",slot.transform,"Heads",Color.white);historyFaces[i]=face;UIFactory.Fill(face.rectTransform,2);
            }
        }
        void BuildControls()
        {
            Image border=f.Image("Panel edge",controls,"Rounded",Palette.Line);UIFactory.Fill(border.rectTransform);
            Image fill=f.Image("Panel surface",controls,"Rounded",Palette.Surface);UIFactory.Fill(fill.rectTransform,1);
            sectionTitle=f.Text("Call it",controls,"Call it.",28,Palette.Text);sectionTitle.fontStyle=FontStyles.Bold;controlsTitle=sectionTitle.rectTransform;
            sectionSub=f.Text("Choose side",controls,"Two sides. One good feeling.",12,Palette.Muted);controlsSub=sectionSub.rectTransform;
            selectionRow=UIFactory.Rect("Side choices",controls);
            heads=SideButton("Heads",CoinSide.Heads,Palette.Gold,out selectedHeads,out headsFace,out headsCheck);
            tails=SideButton("Tails",CoinSide.Tails,Palette.Cyan,out selectedTails,out tailsFace,out tailsCheck);
            wagerBox=UIFactory.Rect("Wager controls",controls);
            var wl=f.Text("Wager label",wagerBox,"WAGER",10,Palette.Muted);wl.characterSpacing=1.5f;UIFactory.Place(wl.rectTransform,0,0,100,16);
            minus=f.Button("Decrease wager",wagerBox,Palette.Raised,()=>session.SetWager(session.Data.wager-1),sound);
            plus=f.Button("Increase wager",wagerBox,Palette.Raised,()=>session.SetWager(session.Data.wager+1),sound);
            UIFactory.Fill(f.Text("Minus",minus.transform,"−",25,Palette.Text,TextAlignmentOptions.Center).rectTransform);
            UIFactory.Fill(f.Text("Plus",plus.transform,"+",24,Palette.Text,TextAlignmentOptions.Center).rectTransform);
            wager=f.Text("Wager amount",wagerBox,"1",25,Palette.Text,TextAlignmentOptions.Center);
            var coinIcon=f.Image("Wager coin",wagerBox,"Heads",Color.white);coinIcon.rectTransform.anchorMin=coinIcon.rectTransform.anchorMax=new Vector2(.5f,1);coinIcon.rectTransform.sizeDelta=Vector2.one*19;coinIcon.rectTransform.anchoredPosition=new Vector2(-28,-48);
            payout=UIFactory.Rect("Return",controls);var pl=f.Text("Return label",payout,"ON A WIN",10,Palette.Muted);UIFactory.Place(pl.rectTransform,0,0,90,20);
            returnLabel=f.Text("Return value",payout,"2 coins  ·  2×",12,Palette.Gold,TextAlignmentOptions.MidlineRight);
            flip=f.Button("Flip coin",controls,Palette.Gold,()=>FlipRequested?.Invoke(),sound);flipRect=(RectTransform)flip.transform;
            flipLabel=f.Text("Flip label",flipRect,"Flip coin   ↗",20,Palette.Background,TextAlignmentOptions.Center);flipLabel.fontStyle=FontStyles.Bold;UIFactory.Fill(flipLabel.rectTransform);
            hint=UIFactory.Rect("Hint",controls);var ht=f.Text("Hint label",hint,"Pick your side to get started",10,Palette.Muted,TextAlignmentOptions.Center);UIFactory.Fill(ht.rectTransform);
        }
        Button SideButton(string name,CoinSide side,Color accent,out Image border,out Image face,out GameObject check)
        {
            Button b=f.Button(name,selectionRow,Palette.Line,()=>{session.Select(side);sound.Play(AudioManager.Cue.Select);},sound);
            border=b.GetComponent<Image>();Image inner=f.Image("Side surface",b.transform,"Rounded",Palette.Raised);UIFactory.Fill(inner.rectTransform,1.5f);
            face=f.Image("Side illustration",b.transform,name,Color.white);UIFactory.Place(face.rectTransform,14,14,36,36);
            var text=f.Text(name+" label",b.transform,name,16,Palette.Text);UIFactory.Place(text.rectTransform,12,57,100,24);
            var odds=f.Text("Even chance",b.transform,"50% chance",10,Palette.Muted);UIFactory.Place(odds.rectTransform,12,83,100,15);
            Image tick=f.Image("Selected",b.transform,"Circle",accent);UIFactory.Place(tick.rectTransform,94,15,17,17);
            Image tickIcon=f.Image("Check",tick.transform,"Check",Palette.Background);UIFactory.Fill(tickIcon.rectTransform,3);check=tick.gameObject;check.SetActive(false);
            return b;
        }
        void BuildFooter()
        {
            var ft=f.Text("Footer note",footer,"JUST FOR FUN. ALWAYS VIRTUAL.",9,Palette.Muted);ft.characterSpacing=1.4f;UIFactory.Place(ft.rectTransform,0,0,300,22);
            streak=f.Text("Best streak",footer,"BEST STREAK  0",9,Palette.Muted,TextAlignmentOptions.MidlineRight);streak.characterSpacing=1;UIFactory.Place(streak.rectTransform,300,0,200,22);
        }
        void BuildSettings()
        {
            Image blocker=f.Image("Settings backdrop",canvas.transform,null,new Color(.02f,.04f,.08f,.86f));settingsPanel=blocker.rectTransform;UIFactory.Fill(settingsPanel);blocker.raycastTarget=true;
            Image panel=f.Image("Settings card",settingsPanel,"Rounded",Palette.Raised);RectTransform card=panel.rectTransform;card.anchorMin=card.anchorMax=Vector2.one*.5f;card.sizeDelta=new Vector2(326,365);
            var title=f.Text("Settings title",card,"Your kind of quiet.",24,Palette.Text);UIFactory.Place(title.rectTransform,24,24,280,38);
            var close=f.Button("Close settings",card,Palette.Surface,ToggleSettings,sound);UIFactory.Place((RectTransform)close.transform,266,24,40,40);Image x=f.Image("Close icon",close.transform,"Close",Palette.Text);UIFactory.Fill(x.rectTransform,10);
            mutedLabel=f.Text("Mute text",card,"Sound on",14,Palette.Text);UIFactory.Place(mutedLabel.rectTransform,24,78,180,30);
            var mute=f.Button("Toggle mute",card,Palette.Surface,()=>{session.Data.muted=!session.Data.muted;SaveManager.Save(session.Data);Refresh();},sound);UIFactory.Place((RectTransform)mute.transform,240,76,62,34);muteButtonLabel=f.Text("Mute label",mute.transform,"Mute",12,Palette.Text,TextAlignmentOptions.Center);UIFactory.Fill(muteButtonLabel.rectTransform);
            AddSlider(card,"Sound effects",120,session.Data.sfx,v=>{session.Data.sfx=v;SaveManager.Save(session.Data);});
            AddSlider(card,"Music",200,session.Data.music,v=>{session.Data.music=v;SaveManager.Save(session.Data);});
            reset=f.Button("Fresh start",card,Palette.Surface,()=>{ResetRequested?.Invoke();ToggleSettings();},sound);UIFactory.Place((RectTransform)reset.transform,24,284,278,48);UIFactory.Fill(f.Text("Reset label",reset.transform,"Fresh start  ·  10 coins",14,Palette.Text,TextAlignmentOptions.Center).rectTransform);
            settingsPanel.gameObject.SetActive(false);
        }
        void BuildMainMenu()
        {
            Image backdrop=f.Image("Main menu backdrop",safe,null,Palette.Background);
            mainMenuPanel=backdrop.rectTransform;UIFactory.Fill(mainMenuPanel);backdrop.raycastTarget=true;
            Image surface=f.Image("Main menu card",mainMenuPanel,"Rounded",Palette.Surface);
            mainMenuCard=surface.rectTransform;
            var eyebrowText=f.Text("Menu eyebrow",mainMenuCard,"A LITTLE LUCKY ARCADE",9,Palette.Gold,TextAlignmentOptions.Center);
            eyebrowText.characterSpacing=2;
            Image head=f.Image("Main menu heads coin",mainMenuCard,"Heads",Color.white);
            Image tail=f.Image("Main menu tails coin",mainMenuCard,"Tails",Color.white);
            var title=f.Text("Main menu title",mainMenuCard,"Coinflip",38,Palette.Text,TextAlignmentOptions.Center);title.fontStyle=FontStyles.Bold;
            var subtitle=f.Text("Main menu subtitle",mainMenuCard,"Call your side. Enjoy the moment.",14,Palette.Muted,TextAlignmentOptions.Center);
            playButton=f.Button("Play game",mainMenuCard,Palette.Gold,()=>StartRequested?.Invoke(),sound);
            var play=f.Text("Play label",playButton.transform,"Play",20,Palette.Background,TextAlignmentOptions.Center);play.fontStyle=FontStyles.Bold;UIFactory.Fill(play.rectTransform);
            menuSettingsButton=f.Button("Main menu settings",mainMenuCard,Palette.Raised,ToggleSettings,sound);
            UIFactory.Fill(f.Text("Menu settings label",menuSettingsButton.transform,"Sound settings",14,Palette.Text,TextAlignmentOptions.Center).rectTransform);
            var note=f.Text("Menu note",mainMenuCard,"Just for fun. Always virtual.",10,Palette.Muted,TextAlignmentOptions.Center);
            UIFactory.Place(eyebrowText.rectTransform,24,18,380,18);
            UIFactory.Place(head.rectTransform,162,53,82,82);
            UIFactory.Place(tail.rectTransform,256,53,82,82);
            UIFactory.Place(title.rectTransform,24,143,380,52);
            UIFactory.Place(subtitle.rectTransform,24,197,380,24);
            UIFactory.Place((RectTransform)playButton.transform,24,254,380,58);
            UIFactory.Place((RectTransform)menuSettingsButton.transform,24,324,380,46);
            UIFactory.Place(note.rectTransform,24,394,380,18);
        }
        void BuildPauseMenu()
        {
            Image backdrop=f.Image("Pause menu backdrop",safe,null,new Color(.025f,.045f,.075f,.88f));
            pausePanel=backdrop.rectTransform;UIFactory.Fill(pausePanel);backdrop.raycastTarget=true;
            Image surface=f.Image("Pause menu card",pausePanel,"Rounded",Palette.Surface);pauseCard=surface.rectTransform;
            var title=f.Text("Pause title",pauseCard,"Take a breath.",28,Palette.Text,TextAlignmentOptions.Center);title.fontStyle=FontStyles.Bold;
            var subtitle=f.Text("Pause subtitle",pauseCard,"Your flip will be right here.",13,Palette.Muted,TextAlignmentOptions.Center);
            resumeButton=f.Button("Resume game",pauseCard,Palette.Gold,ResumeGame,sound);
            var resume=f.Text("Resume label",resumeButton.transform,"Resume",18,Palette.Background,TextAlignmentOptions.Center);resume.fontStyle=FontStyles.Bold;UIFactory.Fill(resume.rectTransform);
            pauseSettingsButton=f.Button("Pause settings",pauseCard,Palette.Raised,OpenPauseSettings,sound);
            UIFactory.Fill(f.Text("Pause settings label",pauseSettingsButton.transform,"Sound settings",14,Palette.Text,TextAlignmentOptions.Center).rectTransform);
            mainMenuButton=f.Button("Return to main menu",pauseCard,Palette.Raised,RequestMainMenu,sound);
            UIFactory.Fill(f.Text("Return to menu label",mainMenuButton.transform,"Main menu",14,Palette.Text,TextAlignmentOptions.Center).rectTransform);
            UIFactory.Place(title.rectTransform,20,34,320,42);UIFactory.Place(subtitle.rectTransform,20,77,320,24);
            UIFactory.Place((RectTransform)resumeButton.transform,24,122,312,56);
            UIFactory.Place((RectTransform)pauseSettingsButton.transform,24,192,312,48);
            UIFactory.Place((RectTransform)mainMenuButton.transform,24,254,312,48);
        }
        void AddSlider(Transform parent,string name,float y,float value,UnityEngine.Events.UnityAction<float> changed)
        {
            var label=f.Text(name+" label",parent,name,13,Palette.Muted);UIFactory.Place(label.rectTransform,24,y,250,22);
            var rt=UIFactory.Rect(name+" slider",parent);UIFactory.Place(rt,24,y+28,278,44);
            var hit=rt.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var slider=rt.gameObject.AddComponent<Slider>();var track=f.Image("Track",rt,"Rounded",Palette.Line);UIFactory.Place(track.rectTransform,0,19,278,6);
            var fillArea=UIFactory.Rect("Fill area",rt);UIFactory.Fill(fillArea,0);var fill=f.Image("Fill",fillArea,"Rounded",Palette.Gold);UIFactory.Fill(fill.rectTransform);fill.rectTransform.offsetMin=new Vector2(0,19);fill.rectTransform.offsetMax=new Vector2(0,-19);slider.fillRect=fill.rectTransform;
            var handleArea=UIFactory.Rect("Handle area",rt);UIFactory.Fill(handleArea,9);var handle=f.Image("Handle",handleArea,"Circle",Palette.Text);handle.raycastTarget=true;handle.rectTransform.sizeDelta=Vector2.one*22;slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;
            slider.minValue=0;slider.maxValue=1;slider.value=value;slider.onValueChanged.AddListener(changed);
        }

        public void ToggleSettings()
        {
            if(session.Busy&&!pauseSettingsReturn)return;
            bool open=!settingsPanel.gameObject.activeSelf;
            settingsPanel.gameObject.SetActive(open);
            if(open)settingsPanel.SetAsLastSibling();
            else if(pauseSettingsReturn){pauseSettingsReturn=false;pausePanel.gameObject.SetActive(true);pausePanel.SetAsLastSibling();}
            sound.Play(AudioManager.Cue.Transition);
        }
        void TogglePause()
        {
            if(!content.gameObject.activeSelf||settingsPanel.gameObject.activeSelf)return;
            if(pausePanel.gameObject.activeSelf){ResumeGame();return;}
            pausePanel.gameObject.SetActive(true);pausePanel.SetAsLastSibling();PauseChanged?.Invoke(true);sound.Play(AudioManager.Cue.Transition);
        }
        void ResumeGame()
        {
            if(!pausePanel.gameObject.activeSelf)return;
            pausePanel.gameObject.SetActive(false);PauseChanged?.Invoke(false);sound.Play(AudioManager.Cue.Transition);
        }
        void OpenPauseSettings()
        {
            if(session.Busy&&!pausePanel.gameObject.activeSelf)return;
            pauseSettingsReturn=true;pausePanel.gameObject.SetActive(false);ToggleSettings();
        }
        void RequestMainMenu(){MainMenuRequested?.Invoke();}
        public void ShowMainMenu()
        {
            settingsPanel.gameObject.SetActive(false);pausePanel.gameObject.SetActive(false);
            content.gameObject.SetActive(false);mainMenuPanel.gameObject.SetActive(true);mainMenuPanel.SetAsLastSibling();
            PauseChanged?.Invoke(false);sound.Play(AudioManager.Cue.Transition);
        }
        public void ShowGame()
        {
            mainMenuPanel.gameObject.SetActive(false);pausePanel.gameObject.SetActive(false);
            content.gameObject.SetActive(true);pauseSettingsReturn=false;PauseChanged?.Invoke(false);ShowReady();
        }
        void Update()
        {
            if(lastW!=Screen.width||lastH!=Screen.height||lastSafe!=Screen.safeArea||Mathf.Abs(lastPixelRatio-CurrentPixelRatio())>.001f) Layout();
        }
        static float CurrentPixelRatio()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return Mathf.Max(1,CoinPixelRatio());
#else
            return 1;
#endif
        }
        void Layout()
        {
            lastW=Screen.width;lastH=Screen.height;lastSafe=Screen.safeArea;
            // Browser backing-buffer density can stabilize after the first Unity frame.
            float pixelRatio=CurrentPixelRatio();lastPixelRatio=pixelRatio;
            float viewportW=Screen.width/pixelRatio,viewportH=Screen.height/pixelRatio;
            bool physicalWide=viewportW>viewportH*1.15f;
            float scale=physicalWide?Mathf.Min(viewportW/1100f,viewportH/740f):Mathf.Min(viewportW/390f,viewportH/780f);
            if(physicalWide && viewportH<600)scale=Mathf.Min(viewportW/760f,viewportH/430f);
            scale=Mathf.Min(scale,1.35f)*pixelRatio;
            scaler.referenceResolution=new Vector2(Screen.width/scale,Screen.height/scale);Canvas.ForceUpdateCanvases();
            safe.anchorMin=new Vector2(lastSafe.xMin/Screen.width,lastSafe.yMin/Screen.height);safe.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();float sw=safe.rect.width,sh=safe.rect.height;
            float w=Mathf.Min(sw-40,1080),h=Mathf.Min(sh-28,740);bool wide=w>=710;bool compact=wide&&h<580;
            LayoutMenus(sw,sh,wide);
            content.anchorMin=content.anchorMax=Vector2.one*.5f;content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(w,h);
            float headerH=compact?61:wide?78:67;UIFactory.Place(header,0,0,w,headerH);UIFactory.Place(logo,0,4,190,50);
            if(!wide)logo.localScale=Vector3.one*.83f;else logo.localScale=Vector3.one;
            UIFactory.Place(balanceBox,w-(wide?225:184),4,wide?157:126,52);UIFactory.Place(soundRect,w-50,5,50,50);UIFactory.Place(divider,0,headerH-1,w,1);
            float panelW=wide?326:w;float panelH=compact?305:wide?416:330;
            float stageH=wide?h-headerH-34:h-headerH-panelH-36;
            if(!wide)stageH=Mathf.Max(290,stageH);
            float stageW=wide?w-panelW-44:w;
            UIFactory.Place(stage,0,headerH+(compact?3:10),stageW,stageH);
            UIFactory.Place(controls,wide?w-panelW:0,wide?headerH+(stageH-panelH)*.5f:headerH+stageH+10,panelW,panelH);
            UIFactory.Place(footer,0,h-24,w,24);UIFactory.Place(streak.rectTransform,w-140,0,140,22);
            if(!wide)footer.GetChild(0).GetComponent<TextMeshProUGUI>().fontSize=7.5f;
            float pad=22,inner=panelW-pad*2;
            bool shortPanel=compact||!wide;
            UIFactory.Place(controlsTitle,pad,compact?5:shortPanel?10:20,inner,35);sectionTitle.fontSize=shortPanel?24:28;
            UIFactory.Place(controlsSub,pad,compact?35:shortPanel?40:58,inner,20);
            float sideY=compact?58:shortPanel?67:91,sideH=compact?80:shortPanel?88:112;
            UIFactory.Place(selectionRow,pad,sideY,inner,sideH);
            UIFactory.Place((RectTransform)heads.transform,0,0,(inner-12)/2,sideH);UIFactory.Place((RectTransform)tails.transform,(inner+12)/2,0,(inner-12)/2,sideH);
            foreach(Button b in new[]{heads,tails})
            {
                float bw=(inner-12)/2;UIFactory.Place((RectTransform)b.transform.GetChild(0),1.5f,1.5f,bw-3,sideH-3);
                UIFactory.Place((RectTransform)b.transform.GetChild(1),12,compact?7:shortPanel?9:12,compact?25:shortPanel?29:36,compact?25:shortPanel?29:36);
                UIFactory.Place((RectTransform)b.transform.GetChild(2),12,compact?34:shortPanel?40:55,bw-20,24);
                UIFactory.Place((RectTransform)b.transform.GetChild(3),12,compact?60:shortPanel?65:83,bw-20,15);
                UIFactory.Place((RectTransform)b.transform.GetChild(4),bw-28,14,16,16);
            }
            float wagerY=sideY+sideH+(compact?5:shortPanel?9:22);UIFactory.Place(wagerBox,pad,wagerY,inner,76);
            UIFactory.Place((RectTransform)minus.transform,0,20,54,54);UIFactory.Place((RectTransform)plus.transform,inner-54,20,54,54);UIFactory.Place(wager.rectTransform,inner*.5f-14,26,48,37);
            float payoutY=wagerY+76;UIFactory.Place(payout,pad,payoutY,inner,20);UIFactory.Place(returnLabel.rectTransform,70,0,inner-70,20);
            float flipY=compact?247:shortPanel?panelH-67:panelH-92;UIFactory.Place(flipRect,pad,flipY,inner,shortPanel?54:58);
            UIFactory.Place(hint,pad,panelH-26,inner,16);hint.gameObject.SetActive(!shortPanel);
            float centerY=compact?stageH*.42f:stageH*.43f;
            if(!wide)centerY=stageH*.41f;
            CoinSize=compact?Mathf.Min(164,stageH*.53f):Mathf.Min(wide?244:148,stageH*.5f);
            UIFactory.Place(eyebrow,0,4,stageW,18);eyebrow.gameObject.SetActive(wide&&!compact&&stageH>300&&!session.Busy);
            UIFactory.Place(stageTitle,0,wide&&!compact&&stageH>300?30:0,stageW,40);stageTitle.GetComponentInChildren<TextMeshProUGUI>().fontSize=compact?24:wide?34:27;
            UIFactory.Place(coinHome,(stageW-CoinSize)/2,centerY-CoinSize/2,CoinSize,CoinSize);
            float orbitSize=CoinSize*1.42f;UIFactory.Place(orbit,(stageW-orbitSize)/2,centerY-orbitSize/2,orbitSize,orbitSize);
            for(int i=0;i<orbit.childCount;i++) {float angle=(i*51+18)*Mathf.Deg2Rad;((RectTransform)orbit.GetChild(i)).anchoredPosition=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*orbitSize*.5f;}
            UIFactory.Place(aura,(stageW-CoinSize*2)/2,centerY-CoinSize,CoinSize*2,CoinSize*2);
            UIFactory.Place(shadow,(stageW-CoinSize*.85f)/2,centerY+CoinSize*.47f,CoinSize*.85f,40);
            float resultY=compact?stageH-98:wide?centerY+CoinSize*.62f+9:stageH-104;UIFactory.Place(resultBlock,0,resultY,stageW,58);
            resultIcon.gameObject.SetActive(!compact);UIFactory.Place(resultIcon,stageW*.5f-8,0,16,16);UIFactory.Place(resultTitle.rectTransform,0,compact?9:20,stageW,24);UIFactory.Place(resultSub.rectTransform,0,compact?34:45,stageW,18);
            UIFactory.Place(stageHistory,(stageW-300)/2,stageH-(wide&&!compact?62:39),300,wide&&!compact?60:38);
            stageHistory.gameObject.SetActive(stageH>285);
            stageHistory.GetChild(0).gameObject.SetActive(wide&&!compact);
            UIFactory.Place(history,0,wide&&!compact?25:3,300,32);
            // Resizing the home rect leaves the animated child transform and result intact.
        }
        void LayoutMenus(float sw,float sh,bool wide)
        {
            float menuW=Mathf.Min(sw-32,wide?600:420);
            float menuH=wide?Mathf.Min(sh-28,354):Mathf.Min(sh-32,486);
            mainMenuCard.anchorMin=mainMenuCard.anchorMax=Vector2.one*.5f;mainMenuCard.pivot=Vector2.one*.5f;
            mainMenuCard.anchoredPosition=Vector2.zero;mainMenuCard.sizeDelta=new Vector2(menuW,menuH);
            float inner=menuW-40,coin=wide?70:82;
            float coinY=wide?31:49,titleY=wide?108:143,subtitleY=wide?150:197;
            float playY=wide?190:254,settingsY=wide?254:324,noteY=wide?315:394;
            UIFactory.Place((RectTransform)mainMenuCard.GetChild(1),menuW*.5f-coin-5,coinY,coin,coin);
            UIFactory.Place((RectTransform)mainMenuCard.GetChild(2),menuW*.5f+5,coinY,coin,coin);
            UIFactory.Place((RectTransform)mainMenuCard.GetChild(3),20,titleY,inner,wide?42:52);
            UIFactory.Place((RectTransform)mainMenuCard.GetChild(4),20,subtitleY,inner,24);
            UIFactory.Place((RectTransform)playButton.transform,20,playY,inner,wide?52:58);
            UIFactory.Place((RectTransform)menuSettingsButton.transform,20,settingsY,inner,wide?44:46);
            UIFactory.Place((RectTransform)mainMenuCard.GetChild(7),20,noteY,inner,18);

            float pauseW=Mathf.Min(sw-32,380),pauseH=Mathf.Min(sh-32,344);
            pauseCard.anchorMin=pauseCard.anchorMax=Vector2.one*.5f;pauseCard.pivot=Vector2.one*.5f;
            pauseCard.anchoredPosition=Vector2.zero;pauseCard.sizeDelta=new Vector2(pauseW,pauseH);
            float pauseInner=pauseW-48;
            UIFactory.Place((RectTransform)pauseCard.GetChild(0),24,26,pauseInner,42);
            UIFactory.Place((RectTransform)pauseCard.GetChild(1),24,68,pauseInner,24);
            UIFactory.Place((RectTransform)resumeButton.transform,24,112,pauseInner,56);
            UIFactory.Place((RectTransform)pauseSettingsButton.transform,24,180,pauseInner,48);
            UIFactory.Place((RectTransform)mainMenuButton.transform,24,240,pauseInner,48);
        }

        public void Refresh()
        {
            SaveData d=session.Data;
            if(displayedBalance!=d.balance)
            {
                if(balanceTween!=null)StopCoroutine(balanceTween);
                balanceTween=StartCoroutine(CountBalance(d.balance));
            }
            wager.text=d.wager.ToString();returnLabel.text=(d.wager*2)+" coins  ·  2×";
            heads.interactable=tails.interactable=!session.Busy;minus.interactable=!session.Busy&&d.wager>1;
            plus.interactable=!session.Busy&&d.wager<Mathf.Min(99,d.balance);pauseButton.interactable=true;reset.interactable=!session.Busy;
            headsCheck.SetActive(d.selected==0);tailsCheck.SetActive(d.selected==1);
            selectedHeads.color=d.selected==0?Palette.Gold:Palette.Line;selectedTails.color=d.selected==1?Palette.Cyan:Palette.Line;
            flip.interactable=d.balance==0&&!session.Busy||session.CanFlip;
            flipLabel.text=session.Busy?"Flipping…":d.balance==0?"Play again  ·  10 coins":d.selected<0?"Choose your side":"Flip coin   ↗";
            hint.GetComponentInChildren<TextMeshProUGUI>().text=d.balance==0?"A fresh start is one tap away":d.selected<0?"Pick your side to get started":d.wager>d.balance?"Wager exceeds your balance":"50 / 50 chance. Every single flip.";
            streak.text="BEST STREAK  "+d.bestStreak;mutedLabel.text=d.muted?"Sound muted":"Sound on";muteButtonLabel.text=d.muted?"Unmute":"Mute";
            for(int i=0;i<8;i++){historyFaces[i].gameObject.SetActive(i<d.history.Count);if(i<d.history.Count)historyFaces[i].sprite=Resources.Load<Sprite>("Art/"+(d.history[i]==0?"Heads":"Tails"));}
        }
        IEnumerator CountBalance(int target)
        {
            int start=displayedBalance;
            for(float t=0;t<1;t+=Time.unscaledDeltaTime/.4f){displayedBalance=Mathf.RoundToInt(Mathf.Lerp(start,target,1-Mathf.Pow(1-t,3)));balance.text=displayedBalance.ToString();yield return null;}
            displayedBalance=target;balance.text=target.ToString();sound.Play(AudioManager.Cue.Balance);
        }
        public void ShowFlipping()
        {stageTitle.gameObject.SetActive(false);eyebrow.gameObject.SetActive(false);resultTitle.text="A little suspense…";resultTitle.color=Palette.Text;resultSub.text="Your coin is in the air";resultIconImage.sprite=Resources.Load<Sprite>("Art/Sparkle");resultIconImage.color=Palette.Gold;}
        public void ShowResult(Round round)
        {
            stageTitle.gameObject.SetActive(true);Layout();
            string side=round.Outcome==CoinSide.Heads?"Heads":"Tails";
            resultTitle.text=round.Won?side+"! You called it.":side+". Next flip, new luck.";resultTitle.color=round.Won?Palette.Green:Palette.Red;
            resultSub.text=round.Won?"+"+round.Wager+" coin"+(round.Wager==1?"":"s")+"  ·  "+(session.Data.streak>1?session.Data.streak+" win streak":"Nicely done!"):"−"+round.Wager+" coin"+(round.Wager==1?"":"s")+"  ·  "+(session.Data.balance==0?"Ready for a fresh start?":"Give your luck another spin.");
            resultIconImage.sprite=Resources.Load<Sprite>("Art/"+(round.Won?"Check":"Close"));resultIconImage.color=round.Won?Palette.Green:Palette.Red;
        }
        public void ShowReady(){stageTitle.gameObject.SetActive(true);Layout();resultTitle.text="A good flip starts with a feeling.";resultTitle.color=Palette.Text;resultSub.text="Heads or tails? You call it.";resultIconImage.sprite=Resources.Load<Sprite>("Art/Sparkle");resultIconImage.color=Palette.Gold;}
        // Read-only inspection for browser QA. There is no way to inject an outcome or currency.
        public string Snapshot(string phase="Idle",float progress=0,bool paused=false,float duration=0)
        {
            Button[] buttons=canvas.GetComponentsInChildren<Button>(false);var bounds=new ButtonBounds[buttons.Length];var corners=new Vector3[4];
            for(int i=0;i<buttons.Length;i++)
            {
                ((RectTransform)buttons[i].transform).GetWorldCorners(corners);
                bounds[i]=new ButtonBounds{name=buttons[i].name,x=corners[0].x/Screen.width,y=1-corners[1].y/Screen.height,w=(corners[2].x-corners[1].x)/Screen.width,h=(corners[1].y-corners[0].y)/Screen.height,enabled=buttons[i].interactable};
            }
            Slider[] sliders=canvas.GetComponentsInChildren<Slider>(false);var sliderBounds=new ButtonBounds[sliders.Length];
            for(int i=0;i<sliders.Length;i++)
            {((RectTransform)sliders[i].transform).GetWorldCorners(corners);sliderBounds[i]=new ButtonBounds{name=sliders[i].name,x=corners[0].x/Screen.width,y=1-corners[1].y/Screen.height,w=(corners[2].x-corners[1].x)/Screen.width,h=(corners[1].y-corners[0].y)/Screen.height,enabled=sliders[i].interactable};}
            return JsonUtility.ToJson(new RuntimeSnapshot{balance=session.Data.balance,wager=session.Data.wager,selected=session.Data.selected,state=session.State.ToString(),face=coinImage.sprite.name,history=session.Data.history.ToArray(),round=session.Data.roundId,coinWidth=coin.localScale.x,viewport=new Vector2(Screen.width,Screen.height),coinPosition=coin.anchoredPosition,buttons=bounds,sliders=sliderBounds,frame=Time.frameCount,muted=session.Data.muted,sfx=session.Data.sfx,music=session.Data.music,phase=phase,progress=progress,paused=paused,duration=duration,delta=Time.unscaledDeltaTime,coinSize=CoinSize,mainMenu=mainMenuPanel.gameObject.activeSelf,pauseMenu=pausePanel.gameObject.activeSelf,settingsMenu=settingsPanel.gameObject.activeSelf});
        }
        [Serializable] class ButtonBounds{public string name;public float x,y,w,h;public bool enabled;}
        [Serializable] class RuntimeSnapshot{public int balance,wager,selected,round,frame;public string state,face,phase;public int[] history;public float coinWidth,sfx,music,progress,duration,delta,coinSize;public bool muted,paused,mainMenu,pauseMenu,settingsMenu;public Vector2 viewport,coinPosition;public ButtonBounds[] buttons,sliders;}
        void OnDestroy(){if(session!=null)session.Changed-=Refresh;}
    }
}
