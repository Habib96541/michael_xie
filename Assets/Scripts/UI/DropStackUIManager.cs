using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DropStack.Game;
using DropStack.Data;
using DropStack.Rendering;
using DropStack.Audio;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DropStack.UI
{
    public class DropStackUIManager : MonoBehaviour
    {
        [SerializeField] private DropStackGameManager gameManager;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private SpeedCurveManager speedManager;
        [SerializeField] private Text scoreText,bestText,linesText,levelText,comboText,timeText,statusText,modeText,goalText,dangerText,gearText;
        [SerializeField] private Image timerFill,goalFill,speedFill;
        [SerializeField] private List<Image> stackSegments=new(),boardEdges=new();
        [SerializeField] private ArcadeSurface timerSurface;
        [SerializeField] private List<PiecePreviewRenderer> nextPreviews=new();
        [SerializeField] private PiecePreviewRenderer holdPreview;
        [SerializeField] private Text holdHint,menuBestText,difficultyDescription,runStatsText,hapticsCaption;
        [SerializeField] private Button holdButton,restartButton,pauseButton,helpButton,menuHelpButton,pauseRestartButton,pauseMenuButton,resultMenuButton;
        [SerializeField] private List<Button> modeButtons=new();
        [SerializeField] private GameObject startModal,settingsModal,restartModal,gameOverModal,helpModal;
        [SerializeField] private Button playButton,resumeButton,restartConfirmButton,restartContinueButton,playAgainButton,helpCloseButton;
        [SerializeField] private UIToggleSwitch soundToggle,musicToggle,motionToggle,hapticsToggle;
        [SerializeField] private Text restartTitleText,restartBodyText,gameOverTitleText,finalScoreText,finalHighScoreText,finalLinesText,finalMaxStreakText,finalBreakdownText,finalModeText,finalRankText;
        [SerializeField] private GameObject newRecordBadge;
        [SerializeField] private CanvasGroup toastGroup;
        [SerializeField] private Text toastTitle,toastDetail;
        [SerializeField] private ArcadeSurface toastSurface;
        private readonly Queue<Notice> notices=new();
        private struct Notice { public string title,detail; public Color color; }
        private float animatedScore,targetScore,scorePunch,comboPunch,toastAge=-1,goalAmount,finalScoreAge;
        private int displayedTime=-1,stackHeight;
        private bool subscribed,helpResumes,helpFromPause,restartWasPaused,returningToMenu;
        private GameState previousState=GameState.StartMenu;
        private Text boardLabel;
        private static readonly Color Cyan=new Color(.08f,.88f,1),Muted=new Color(.58f,.72f,.81f),Pink=new Color(1,.22f,.4f),Amber=new Color(1,.75f,.16f);

        public void Bind(DropStackGameManager gm,ScoreManager score,SpeedCurveManager speed) { gameManager=gm; scoreManager=score; speedManager=speed; }
        private void Start()
        {
            ArcadeFeedback.Initialize(); boardLabel=transform.Find("PlayfieldFrame/BoardLabel").GetComponent<Text>(); Subscribe();
            Click(playButton,StartRun); Click(playAgainButton,StartRun); Click(restartButton,OpenRestartModal); Click(pauseButton,OpenSettingsModal);
            Click(resumeButton,CloseSettingsModal); Click(restartConfirmButton,ConfirmRestart); Click(restartContinueButton,CloseRestartModal);
            Click(helpButton,OpenHelp); Click(menuHelpButton,OpenHelp); Click(helpCloseButton,CloseHelp);
            Click(pauseRestartButton,OpenRestartModal); Click(pauseMenuButton,RequestMenu); Click(resultMenuButton,()=>gameManager.ReturnToMenu());
            Click(holdButton,()=>gameManager.GetComponent<ActivePieceController>().RequestHold());
            for(int i=0;i<modeButtons.Count;i++) { var mode=(GameDifficulty)i; Click(modeButtons[i],()=>gameManager.SetDifficulty(mode)); }
            soundToggle.OnValueChanged+=SetSound; musicToggle.OnValueChanged+=SetMusic; motionToggle.OnValueChanged+=SetMotion; hapticsToggle.OnValueChanged+=SetHaptics;
            HandleScoreChanged(scoreManager.CurrentScore,0,""); HandleLinesChanged(0,scoreManager.TotalLinesCleared); HandleComboChanged(scoreManager.CurrentStreak,scoreManager.CurrentMultiplier);
            HandleSpeedLevelChanged(speedManager.CurrentLevel,speedManager.CurrentDropInterval,speedManager.IsPeakVelocity);
            HandleTimerUpdated(gameManager.TimeRemaining); HandleHoldChanged(gameManager.HoldPiece); HandleNextQueueChanged(gameManager.NextQueue);
            HandleDifficulty(gameManager.Difficulty); HandleGameStateChanged(gameManager.CurrentState); SyncToggles(); UpdateStack();
        }
        private static void Click(Button button,UnityEngine.Events.UnityAction callback)
        {
            button.onClick.AddListener(()=>{ DropStackAudioManager.Instance?.PlayClick(); ArcadeFeedback.Pulse(12); EventSystem.current?.SetSelectedGameObject(null); callback(); });
        }
        private void Subscribe()
        {
            if(subscribed) return; subscribed=true;
            gameManager.OnGameStateChanged+=HandleGameStateChanged; gameManager.OnTimerUpdated+=HandleTimerUpdated;
            gameManager.OnHoldChanged+=HandleHoldChanged; gameManager.OnNextQueueChanged+=HandleNextQueueChanged; gameManager.OnGameOver+=HandleGameOver;
            gameManager.OnDifficultyChanged+=HandleDifficulty; gameManager.OnSpeedBannerTriggered+=HandleSpeedBanner;
            gameManager.GetComponent<DropStackBoard>().OnBoardChanged+=UpdateStack;
            scoreManager.OnScoreChanged+=HandleScoreChanged; scoreManager.OnComboChanged+=HandleComboChanged; scoreManager.OnLinesChanged+=HandleLinesChanged;
            speedManager.OnSpeedLevelChanged+=HandleSpeedLevelChanged;
        }
        private void OnDestroy()
        {
            if(!subscribed) return;
            if(gameManager!=null) {
                gameManager.OnGameStateChanged-=HandleGameStateChanged; gameManager.OnTimerUpdated-=HandleTimerUpdated; gameManager.OnHoldChanged-=HandleHoldChanged;
                gameManager.OnNextQueueChanged-=HandleNextQueueChanged; gameManager.OnGameOver-=HandleGameOver; gameManager.OnDifficultyChanged-=HandleDifficulty;
                gameManager.OnSpeedBannerTriggered-=HandleSpeedBanner; gameManager.GetComponent<DropStackBoard>().OnBoardChanged-=UpdateStack;
            }
            if(scoreManager!=null) { scoreManager.OnScoreChanged-=HandleScoreChanged; scoreManager.OnComboChanged-=HandleComboChanged; scoreManager.OnLinesChanged-=HandleLinesChanged; }
            if(speedManager!=null) speedManager.OnSpeedLevelChanged-=HandleSpeedLevelChanged;
        }
        private void Update()
        {
            float dt=Time.unscaledDeltaTime;
            animatedScore=Mathf.Lerp(animatedScore,targetScore,1-Mathf.Exp(-13*dt)); if(Mathf.Abs(animatedScore-targetScore)<.5f) animatedScore=targetScore;
            scoreText.text=Mathf.RoundToInt(animatedScore).ToString("N0");
            scorePunch=Mathf.MoveTowards(scorePunch,0,dt*3); comboPunch=Mathf.MoveTowards(comboPunch,0,dt*2.6f);
            scoreText.rectTransform.localScale=Vector3.one*(1+(ArcadeFeedback.MotionEnabled?scorePunch*.12f:0));
            comboText.rectTransform.localScale=Vector3.one*(1+(ArcadeFeedback.MotionEnabled?comboPunch*.18f:0));
            bool playing=gameManager.CurrentState==GameState.Playing;
            holdButton.interactable=playing&&gameManager.CanHold;
            holdHint.text=gameManager.HoldPiece==PolyominoType.None?"TAP / C":gameManager.CanHold?"SWAP / C":"LOCKED";
            float phase=Time.unscaledTime%1f;
            float beat=Mathf.Exp(-Mathf.Pow(phase-.1f,2)*180)+.6f*Mathf.Exp(-Mathf.Pow(phase-.3f,2)*180);
            bool urgent=playing&&gameManager.TimeRemaining<=10;
            float pulse=ArcadeFeedback.MotionEnabled&&urgent?beat:0;
            timeText.rectTransform.localScale=Vector3.one*(1+pulse*.075f);
            timerSurface.SetEdge(urgent?Color.Lerp(Pink,Color.white,pulse*.4f):Cyan*.65f);
            foreach(var edge in boardEdges) edge.color=playing&&(stackHeight>=16||urgent)?Color.Lerp(Pink,Cyan,.16f+(ArcadeFeedback.MotionEnabled?1-beat:1)*.3f):Cyan;
            goalAmount=Mathf.Lerp(goalAmount,Mathf.Clamp01(scoreManager.TotalLinesCleared/(float)gameManager.TargetLines),1-Mathf.Exp(-10*dt)); goalFill.fillAmount=goalAmount;
            speedFill.fillAmount=speedManager.CurrentLevel>=6?1:(gameManager.ElapsedTime%15)/15;
            UpdateToast(dt,playing);
            if(gameOverModal.activeSelf) {
                finalScoreAge+=dt; float t=ArcadeFeedback.MotionEnabled?1-Mathf.Pow(1-Mathf.Clamp01(finalScoreAge/.7f),3):1;
                finalScoreText.text=Mathf.RoundToInt(scoreManager.CurrentScore*t).ToString("N0");
            }
            bool escape=false,enter=false,restartKey=false; int modeKey=-1;
#if ENABLE_INPUT_SYSTEM
            var kb=Keyboard.current;
            if(kb!=null) { escape=kb.escapeKey.wasPressedThisFrame||kb.pKey.wasPressedThisFrame; enter=kb.enterKey.wasPressedThisFrame||kb.numpadEnterKey.wasPressedThisFrame; restartKey=kb.rKey.wasPressedThisFrame;
                if(kb.digit1Key.wasPressedThisFrame) modeKey=0; if(kb.digit2Key.wasPressedThisFrame) modeKey=1; if(kb.digit3Key.wasPressedThisFrame) modeKey=2; }
#elif ENABLE_LEGACY_INPUT_MANAGER
            escape=Input.GetKeyDown(KeyCode.Escape)||Input.GetKeyDown(KeyCode.P); enter=Input.GetKeyDown(KeyCode.Return); restartKey=Input.GetKeyDown(KeyCode.R);
            if(Input.GetKeyDown(KeyCode.Alpha1)) modeKey=0; if(Input.GetKeyDown(KeyCode.Alpha2)) modeKey=1; if(Input.GetKeyDown(KeyCode.Alpha3)) modeKey=2;
#endif
            if(modeKey>=0&&gameManager.CurrentState==GameState.StartMenu&&!helpModal.activeSelf) gameManager.SetDifficulty((GameDifficulty)modeKey);
            if(restartKey&&playing) OpenRestartModal();
            if(enter) { if(helpModal.activeSelf) CloseHelp(); else if(gameManager.CurrentState==GameState.StartMenu||gameManager.CurrentState==GameState.GameOver) StartRun(); }
            if(!escape) return;
            if(helpModal.activeSelf) CloseHelp(); else if(restartModal.activeSelf) CloseRestartModal();
            else if(gameManager.CurrentState==GameState.Paused) CloseSettingsModal(); else if(playing) OpenSettingsModal();
        }
        private void StartRun() { HideModals(); gameManager.StartGame(); DropStackAudioManager.Instance?.PlayBGM(); }
        private void HideModals() { startModal.SetActive(false); settingsModal.SetActive(false); restartModal.SetActive(false); gameOverModal.SetActive(false); helpModal.SetActive(false); }
        private void SyncToggles()
        {
            var audio=DropStackAudioManager.Instance;
            soundToggle.SetState(audio==null||audio.IsSoundEnabled,false); musicToggle.SetState(audio==null||audio.IsMusicEnabled,false);
            motionToggle.SetState(ArcadeFeedback.MotionEnabled,false); hapticsToggle.SetState(ArcadeFeedback.HapticsEnabled,false);
            hapticsToggle.SetInteractable(ArcadeFeedback.HapticsSupported); hapticsCaption.text=ArcadeFeedback.HapticsSupported?"VIBRATION FEEDBACK":"AVAILABLE ON SUPPORTED PHONES";
        }
        private void SetSound(bool value)=>DropStackAudioManager.Instance?.SetSoundEnabled(value);
        private void SetMusic(bool value)=>DropStackAudioManager.Instance?.SetMusicEnabled(value);
        private void SetMotion(bool value) { PlayerPrefs.SetInt("DropStack_Motion",value?1:0); PlayerPrefs.Save(); }
        private void SetHaptics(bool value) { PlayerPrefs.SetInt("DropStack_Haptics",value?1:0); PlayerPrefs.Save(); }
        public void OpenSettingsModal()
        {
            if(gameManager.CurrentState!=GameState.Playing&&gameManager.CurrentState!=GameState.Paused) return;
            if(gameManager.CurrentState==GameState.Playing) gameManager.TogglePause();
            SyncToggles(); settingsModal.SetActive(true);
        }
        public void CloseSettingsModal() { settingsModal.SetActive(false); gameManager.ResumeGame(); }
        public void OpenRestartModal() => OpenConfirmation(false);
        private void RequestMenu() => OpenConfirmation(true);
        private void OpenConfirmation(bool menu)
        {
            if(gameManager.CurrentState!=GameState.Playing&&gameManager.CurrentState!=GameState.Paused) return;
            restartWasPaused=gameManager.CurrentState==GameState.Paused; returningToMenu=menu;
            if(!restartWasPaused) gameManager.TogglePause();
            settingsModal.SetActive(false); restartModal.SetActive(true);
            restartTitleText.text=menu?"LEAVE THIS RUN?":"FRESH START?";
            restartBodyText.text=menu?"Your best score is saved.\nThis run will end when you return to the menu.":"Your current points will reset.\nYour personal best stays with you.";
        }
        public void CloseRestartModal() { restartModal.SetActive(false); if(restartWasPaused) settingsModal.SetActive(true); else gameManager.ResumeGame(); }
        public void ConfirmRestart() { if(returningToMenu) { HideModals(); gameManager.ReturnToMenu(); } else StartRun(); }
        private void OpenHelp()
        {
            helpResumes=gameManager.CurrentState==GameState.Playing; helpFromPause=gameManager.CurrentState==GameState.Paused;
            if(helpResumes) gameManager.TogglePause(); settingsModal.SetActive(false); helpModal.SetActive(true);
        }
        private void CloseHelp() { helpModal.SetActive(false); if(helpResumes) gameManager.ResumeGame(); else if(helpFromPause) settingsModal.SetActive(true); }
        private void HandleGameStateChanged(GameState state)
        {
            startModal.SetActive(state==GameState.StartMenu);
            if(state==GameState.Paused&&!restartModal.activeSelf&&!helpModal.activeSelf) { SyncToggles(); settingsModal.SetActive(true); }
            if(state==GameState.Playing) { settingsModal.SetActive(false); gameOverModal.SetActive(false); if(previousState!=GameState.Paused) { notices.Clear(); toastAge=-1; Notify("LET'S STACK",gameManager.Difficulty.ToString().ToUpper()+" / 90 SECOND SPRINT",Cyan); } }
            else { notices.Clear(); toastGroup.alpha=0; toastAge=-1; }
            if(state==GameState.GameOver) { settingsModal.SetActive(false); restartModal.SetActive(false); helpModal.SetActive(false); gameOverModal.SetActive(true); }
            if(state==GameState.StartMenu) { settingsModal.SetActive(false); gameOverModal.SetActive(false); restartModal.SetActive(false); helpModal.SetActive(false); HandleDifficulty(gameManager.Difficulty); }
            statusText.text=state==GameState.Playing?"90 SECOND SPRINT":state==GameState.Paused?"RUN ON HOLD":state==GameState.GameOver?"RUN COMPLETE":"PICK YOUR PACE";
            restartButton.interactable=pauseButton.interactable=state==GameState.Playing||state==GameState.Paused;
            runStatsText.text=$"{gameManager.Difficulty.ToString().ToUpper()}   /   {scoreManager.CurrentScore:N0} PTS   /   {Mathf.CeilToInt(gameManager.TimeRemaining):00}s LEFT";
            DropStackAudioManager.Instance?.SetPaused(state!=GameState.Playing); previousState=state;
        }
        private void HandleTimerUpdated(float seconds)
        {
            int total=Mathf.CeilToInt(seconds); if(total!=displayedTime) { displayedTime=total; timeText.text=$"{total/60:00}:{total%60:00}"; }
            timeText.color=seconds<=10?Pink:Color.white; timerFill.fillAmount=seconds/90; timerFill.color=seconds<=10?Pink:Cyan;
        }
        private void HandleScoreChanged(int value,int added,string reason)
        {
            targetScore=value; if(added==0||value<animatedScore) animatedScore=value;
            if(added>0) scorePunch=Mathf.Max(scorePunch,added>=8?1:.18f);
            bestText.text=scoreManager.HighScore.ToString("N0");
            if(added>0&&reason!="Hard Drop"&&reason!="Soft Drop") Notify(reason.Contains("PERFECT")?"PERFECT CLEAR!":reason,$"+{added:N0} POINTS / x{scoreManager.CurrentMultiplier:0.0}",reason.Contains("TARGET")?Amber:Cyan);
        }
        private void HandleComboChanged(int streak,float multiplier) { comboText.text=$"x{multiplier:0.0}"; comboText.color=streak>=2?Amber:Muted; if(streak>=2) comboPunch=1; }
        private void HandleLinesChanged(int added,int total) { linesText.text=total.ToString("00"); goalText.text=$"{total:00} / {gameManager.TargetLines:00}"; }
        private void HandleSpeedLevelChanged(int level,float interval,bool peak) { levelText.text=level.ToString("00"); levelText.color=peak?Pink:Color.white; gearText.text=level>=6?"OVERDRIVE":"NEXT GEAR"; }
        private void HandleSpeedBanner(string message) { Notify(speedManager.IsPeakVelocity?"OVERDRIVE!":"GEAR UP",$"LEVEL {speedManager.CurrentLevel:00} / KEEP IT CLEAN",speedManager.IsPeakVelocity?Pink:Amber); ArcadeFeedback.Pulse(30); }
        private void HandleHoldChanged(PolyominoType type) { if(type==PolyominoType.None) holdPreview.Clear(); else holdPreview.SetShape(PolyominoDatabase.GetShape(type)); }
        private void HandleNextQueueChanged(IReadOnlyList<PolyominoType> queue) { for(int i=0;i<nextPreviews.Count;i++) { if(i<queue.Count) nextPreviews[i].SetShape(PolyominoDatabase.GetShape(queue[i])); else nextPreviews[i].Clear(); } }
        private void HandleDifficulty(GameDifficulty mode)
        {
            var tint=DifficultyRules.Color(mode); modeText.text=mode.ToString().ToUpper(); modeText.color=tint;
            menuBestText.text=$"{mode.ToString().ToUpper()} BEST   {ScoreManager.BestFor(mode):N0}";
            difficultyDescription.text=mode==GameDifficulty.Noob?"7 CLASSIC SHAPES / ROOM TO LEARN":mode==GameDifficulty.Pro?"16 SHAPES / FAST FROM THE FIRST DROP":"16 SHAPES / A STEADY CLIMB";
            for(int i=0;i<modeButtons.Count;i++) { bool selected=i==(int)mode; var panel=modeButtons[i].GetComponent<ArcadeSurface>(); panel.edge=selected?DifficultyRules.Color((GameDifficulty)i):new Color(.18f,.3f,.4f); panel.top=selected?Color.Lerp(new Color(.06f,.13f,.2f),panel.edge,.22f):new Color(.045f,.095f,.15f); panel.bright=selected; panel.SetAllDirty(); modeButtons[i].transform.Find("Selected").GetComponent<Text>().text=selected?"SELECTED":""; }
            goalText.text=$"{scoreManager.TotalLinesCleared:00} / {gameManager.TargetLines:00}";
        }
        private void UpdateStack()
        {
            var board=gameManager.GetComponent<DropStackBoard>(); stackHeight=0;
            for(int y=0;y<DropStackBoard.VisibleRows;y++) for(int x=0;x<10;x++) if(board.GetCell(x,y)>0) stackHeight=Mathf.Max(stackHeight,y+1);
            dangerText.text=stackHeight>=16?"DANGER":stackHeight>=12?"RISING":"CLEAR";
            dangerText.color=stackHeight>=16?Pink:stackHeight>=12?Amber:Muted;
            for(int i=0;i<stackSegments.Count;i++) stackSegments[i].color=i<Mathf.CeilToInt(stackHeight*.5f)?(i>=8?Pink:i>=6?Amber:Cyan):new Color(.1f,.18f,.24f);
        }
        private void HandleGameOver(string reason,bool record)
        {
            gameOverTitleText.text=reason=="TIME'S UP!"?"TIME'S UP!":"STACKED OUT!"; finalScoreAge=0;
            finalHighScoreText.text=$"PERSONAL BEST   {scoreManager.HighScore:N0}"; finalLinesText.text=$"{scoreManager.TotalLinesCleared:00}  LINES";
            finalMaxStreakText.text=$"x{scoreManager.MaxMultiplier:0.0}  MAX COMBO";
            finalModeText.text=gameManager.Difficulty.ToString().ToUpper()+" / 90 SECOND SPRINT";
            int score=scoreManager.CurrentScore; finalRankText.text=score>=5000?"S / STACK MASTER":score>=2000?"A / IN THE ZONE":score>=700?"B / NICE FLOW":"C / KEEP STACKING";
            finalBreakdownText.text=$"{scoreManager.SinglesCount} SINGLE     {scoreManager.DoublesCount} DOUBLE     {scoreManager.TriplesCount} TRIPLE\n{scoreManager.DropStacksCount} DROP STACK     {scoreManager.PentrisCount} PENTRIS";
            newRecordBadge.SetActive(record); ArcadeFeedback.Pulse(record?65:35);
        }
        private void Notify(string title,string detail,Color tint)
        { if(notices.Count>=3) notices.Dequeue(); notices.Enqueue(new Notice{title=title,detail=detail,color=tint}); }
        private void UpdateToast(float dt,bool playing)
        {
            if(!playing) { boardLabel.enabled=true; statusText.enabled=true; return; }
            if(toastAge<0&&notices.Count>0) { var n=notices.Dequeue(); toastTitle.text=n.title; toastDetail.text=n.detail; toastTitle.color=n.color; toastSurface.SetEdge(n.color); toastAge=0; }
            boardLabel.enabled=statusText.enabled=toastAge<0;
            if(toastAge<0) return; toastAge+=dt;
            float fade=Mathf.Min(Mathf.Clamp01(toastAge/.15f),Mathf.Clamp01((1.6f-toastAge)/.3f)); toastGroup.alpha=fade;
            toastGroup.transform.localScale=Vector3.one*(ArcadeFeedback.MotionEnabled?Mathf.Lerp(.88f,1,1-Mathf.Pow(1-Mathf.Clamp01(toastAge/.24f),3)):1);
            if(toastAge>=1.6f) { toastAge=-1; toastGroup.alpha=0; }
        }
        public void ApplyMenuLayout(bool portrait)
        {
            if(startModal==null) return; var card=startModal.transform.Find("StartCard").GetComponent<RectTransform>();
            card.sizeDelta=new Vector2(portrait?600:720,portrait?1040:790);
            Move(card,"Eyebrow",new Vector2(0,portrait?470:345)); Move(card,"Logo",new Vector2(0,portrait?322:208));
            Move(card,"Tagline",new Vector2(0,portrait?170:67)); Move(card,"ModeLabel",new Vector2(0,portrait?113:14));
            for(int i=0;i<modeButtons.Count;i++) {
                var rt=modeButtons[i].GetComponent<RectTransform>(); rt.anchoredPosition=portrait?new Vector2(0,35-i*108):new Vector2((i-1)*212,-83); rt.sizeDelta=portrait?new Vector2(520,94):new Vector2(196,142);
                Move(rt,"ModeTitle",new Vector2(portrait?-145:0,portrait?8:28)); Move(rt,"ModeDetail",new Vector2(portrait?82:0,portrait?5:-14));
                Move(rt,"Selected",new Vector2(portrait?-145:0,portrait?-21:-48));
            }
            Move(card,"DifficultyDescription",new Vector2(0,portrait?-240:-188)); Move(card,"MenuBest",new Vector2(0,portrait?-286:-224));
            Move(card,"Play",new Vector2(0,portrait?-367:-289)); Move(card,"Help",new Vector2(0,portrait?-455:-354));
        }
        private static void Move(Transform root,string child,Vector2 position) { var target=root.Find(child); if(target!=null) target.GetComponent<RectTransform>().anchoredPosition=position; }
    }
}
