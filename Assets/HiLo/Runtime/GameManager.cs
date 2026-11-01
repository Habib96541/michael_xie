using System.Collections;
using System.Numerics;
using System.Runtime.InteropServices;
using HiLo.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using static HiLo.UIFactory;
using Vector2=UnityEngine.Vector2;
using Vector3=UnityEngine.Vector3;

namespace HiLo
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameConfig config;
        public SaveManager Save { get; private set; }
        public GameSession Session { get; private set; }
        public GameView View { get; private set; }
        public bool ReducedMotion => Save.Data.reducedMotion;
        AudioManager audioManager;
        CardAnimationController animationController;
        float decisionTime,totalTime,uiTick;
        bool modalOpen,focused=true,roundModal;
        int modalKind;
        [DllImport("__Internal")] static extern void HiLoReady();
        [DllImport("__Internal")] static extern void HiLoState(string json);
        [DllImport("__Internal")] static extern void HiLoFullscreen();
        void Awake()
        {
            Instance=this;Application.targetFrameRate=60;Application.runInBackground=true;
            config=config!=null?config:Resources.Load<GameConfig>("GameConfig");
            if(config==null)config=ScriptableObject.CreateInstance<GameConfig>();
            Save=new SaveManager(config.initialBalance);
            Session=new GameSession(ScientificNumber.Decode(Save.Data.balance,config.initialBalance),config.stake,config.houseEdge,minimumGrowth:config.minimumMultiplierGrowth);
            UIFactory.Font=Resources.Load<TMP_FontAsset>("Fonts/Outfit SDF");
            var go=new GameObject("Hi Lo Canvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var root=Rect("Safe area",go.transform);Fill(root);View=new GameView(root,this);
            var responsive=go.AddComponent<ResponsiveUIManager>();responsive.Initialize(scaler,root,View);
            var eventSystem=new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));
            audioManager=gameObject.AddComponent<AudioManager>();audioManager.Initialize();animationController=gameObject.AddComponent<CardAnimationController>();
            UpdateView(); ShowWelcome();
            #if UNITY_WEBGL && !UNITY_EDITOR
            HiLoReady();
            #endif
        }
        void Update()
        {
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame && modalOpen && !roundModal)CloseModal();
                else if(!modalOpen){if(keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)Predict(true);else if(keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)Predict(false);else if(keyboard.spaceKey.wasPressedThisFrame)CashOut();}
                else if(roundModal && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))ModalPrimary();
            }
            if(Session.State==RoundState.Decision && !modalOpen && focused && totalTime>0)
            {
                decisionTime-=Time.unscaledDeltaTime;
                View.TimerFill.fillAmount=Mathf.Clamp01(decisionTime/totalTime);
                View.TimerFill.color=decisionTime<3?Coral:Teal;
                uiTick+=Time.unscaledDeltaTime;
                if(uiTick>.1f){uiTick=0;View.Timer.text=Mathf.CeilToInt(Mathf.Max(0,decisionTime)).ToString();}
                if(decisionTime<=0 && Session.Timeout()){Save.Record(Session);audioManager.Play(Sound.Wrong);ShowResult();}
            }
        }
        void ShowWelcome()
        {
            modalKind=0;roundModal=true;modalOpen=true;View.ModalMode(false,false);
            View.ModalTitle.text="One more card.";
            View.ModalBody.text="Higher ↑ or lower ↓?\n\nBuild your streak. Collect anytime.\nEqual cards are a push. Ace is high.\n\n<color=#EBCB86>"+new ScientificNumber(config.initialBalance).Format()+" free virtual coins</color>\n"+config.stake.ToString("0.##")+" coins per round";
            View.ModalActionLabel.text="Let’s play  ·  "+config.stake.ToString("0.##");
            if(Save.Data.learned){View.ModalTitle.text="Your next move.";View.ModalBody.text="<size=42>"+Session.Balance.Format()+"</size>\nvirtual coins\n\nBest streak  "+Save.Data.bestStreak+"\nBest multiplier  "+ScientificNumber.Decode(Save.Data.bestMultiplier,1).Format(true);View.ModalActionLabel.text=Session.CanAfford?"Play  ·  "+config.stake.ToString("0.##"):RefillLabel();}
        }
        public void ModalPrimary()
        {
            audioManager.Unlock();audioManager.Play(Sound.Click);
            if(modalKind==1){CloseModal();return;}
            if(!Session.CanAfford){Session.Refill(config.freeRefill);Save.Record(Session);View.ModalBody.text="<color=#EBCB86>+"+config.freeRefill.ToString("0")+" free virtual coins</color>\n\nYour balance is refilled.\nThere’s always another round.";View.ModalActionLabel.text="Play  ·  "+config.stake.ToString("0.##");UpdateView();return;}
            if(!Session.Begin())return;
            Save.Data.learned=true;Save.Data.rounds=(Save.Number(Save.Data.rounds)+1).ToString();Save.Record(Session);
            modalOpen=false;roundModal=false;View.Overlay.gameObject.SetActive(false);View.ResetHistory();View.SetCard(Session.Current);View.NextImage.sprite=Resources.Load<Sprite>("Art/card_back");View.Feedback.text="Your next move.";View.Feedback.color=Muted;
            audioManager.Play(Sound.Deal);StartCoroutine(animationController.Pulse(View.CurrentCard,ReducedMotion));ResetTimer();UpdateView();
        }
        public void Predict(bool higher)
        {
            if(modalOpen || !focused || Session.State!=RoundState.Decision)return;
            audioManager.Unlock();Card previous=Session.Current;
            if(!Session.Predict(higher))return;
            Save.Data.predictions=(Save.Number(Save.Data.predictions)+1).ToString();Save.Record(Session);
            View.AddHistory(previous);SetButtons(false);audioManager.Play(Sound.Flip);PublishState();StartCoroutine(Reveal());
        }
        IEnumerator Reveal()
        {
            int previousStage=config.StageIndex(Session.Streak>0?Session.Streak-1:0);
            float seconds=config.stages[config.StageIndex(Session.Streak)].revealSeconds;
            yield return animationController.Reveal(View,Session.Current,Session.LastOutcome,seconds,ReducedMotion);
            Outcome result=Session.LastOutcome;
            audioManager.Play(result==Outcome.Correct?Sound.Correct:result==Outcome.Push?Sound.Click:Sound.Wrong);
            if(result==Outcome.Wrong){View.Feedback.text="Round over.";View.Feedback.color=Coral;}
            else if(result==Outcome.Push){View.Feedback.text="Equal. Nothing lost.";View.Feedback.color=Gold;}
            else
            {
                View.Feedback.text=Session.Streak%5==0?Session.Streak+" in a row!":"Good instinct.";View.Feedback.color=Teal;
                if(Session.Streak%5==0)audioManager.Play(Sound.Milestone);else audioManager.Play(Sound.Grow);
                StartCoroutine(animationController.Pulse(View.MultiplierRect,ReducedMotion));
                if(config.StageIndex(Session.Streak)>previousStage)View.Feedback.text=config.stages[config.StageIndex(Session.Streak)].name+" · timer tightens";
            }
            if(result!=Outcome.Wrong)yield return animationController.Transfer(View,Session.Current,ReducedMotion);
            Session.CompleteReveal();UpdateView();
            if(Session.State==RoundState.Ended){ShowResult();yield break;}
            ResetTimer();UpdateView();
        }
        public void CashOut()
        {
            if(modalOpen || !focused || !Session.CashOut())return;
            audioManager.Unlock();audioManager.Play(Sound.CashOut);Save.Data.cashouts=(Save.Number(Save.Data.cashouts)+1).ToString();Save.Record(Session);UpdateView();ShowResult();StartCoroutine(animationController.Pulse(View.Modal,ReducedMotion));
        }
        void ShowResult()
        {
            modalKind=0;modalOpen=true;roundModal=true;View.ModalMode(false,false);
            bool collected=Session.LastOutcome==Outcome.CashOut;
            View.ModalTitle.text=collected?"Well played.":Session.LastOutcome==Outcome.Timeout?"Time’s up.":"Next time.";
            View.ModalBody.text=(collected?"<color=#EBCB86><size=40>+"+Session.Payout.Format()+"</size></color>\ncoins collected":Session.LastOutcome==Outcome.Timeout?"The decision timer expired.\nYour round’s potential was lost.":"The next card was "+Session.Current.Label+".\nYour round’s potential was lost.")+"\n\n"+Session.Streak+" streak   ·   "+Session.Multiplier.Format(true)+"\nBest  "+Save.Data.bestStreak+"   ·   Balance  "+Session.Balance.Format()+(Session.CanAfford?"":"\n\nOut of coins? Refill free, anytime.");
            View.ModalActionLabel.text=Session.CanAfford?"Play again  ·  "+config.stake.ToString("0.##"):RefillLabel();SetButtons(false);PublishState();
        }
        string RefillLabel()=>"Free refill  +"+new ScientificNumber(config.freeRefill).Format();
        public void ShowSettings()
        {
            if(Session.State==RoundState.Revealing)return;
            audioManager.Unlock();audioManager.Play(Sound.Click);modalKind=2;modalOpen=true;roundModal=false;View.ModalMode(true,true);View.ModalTitle.text="Make it yours.";UpdateSettingLabels();PublishState();
        }
        public void ShowHelp()
        {
            if(Session.State==RoundState.Revealing)return;
            modalKind=1;modalOpen=true;roundModal=false;View.ModalMode(false,true);View.ModalTitle.text="Follow your instinct.";
            View.ModalBody.text="↑ Higher   ·   ↓ Lower\nSPACE to collect   ·   Ace is high\n\nEqual cards push: your multiplier stays.\nOdds use the cards left in a fair deck.\nA fresh deck follows the last card.\n\nThe timer starts at a 5-card streak.\nTime out and the round ends.\nChange the timer in settings.\n\nVirtual coins only. No real money.";View.ModalBody.fontSize=14;View.ModalActionLabel.text="Got it";PublishState();
        }
        public void CloseModal()
        {
            if(roundModal)return;
            audioManager.Play(Sound.Click);modalOpen=false;View.Overlay.gameObject.SetActive(false);View.ModalBody.fontSize=16;
            if(Session.State==RoundState.Ready || Session.State==RoundState.Ended)ShowWelcome();else UpdateView();
        }
        public void ToggleSetting(int i)
        {
            if(i==0)Save.Data.sound=!Save.Data.sound;if(i==1)Save.Data.music=!Save.Data.music;if(i==2)Save.Data.reducedMotion=!Save.Data.reducedMotion;if(i==3){Save.Data.pressure=!Save.Data.pressure;ResetTimer();}
            Save.Flush();audioManager.Refresh();audioManager.Play(Sound.Click);UpdateSettingLabels();PublishState();
        }
        void UpdateSettingLabels()
        {
            string[] names={"Sound effects","Ambient music","Reduced motion","Streak timer"};bool[] values={Save.Data.sound,Save.Data.music,Save.Data.reducedMotion,Save.Data.pressure};
            for(int i=0;i<4;i++)View.ToggleLabels[i].text=names[i]+"     <color="+(values[i]?"#55E6BE>ON":"#8BABA9>OFF")+"</color>";
        }
        void ResetTimer()
        {
            totalTime=Save.Data.pressure?config.DecisionSeconds(Session.Streak):0;decisionTime=totalTime;View.TimerFill.fillAmount=1;View.TimerFill.color=Teal;View.Timer.text=totalTime==0?"∞":Mathf.CeilToInt(totalTime).ToString();
        }
        void SetButtons(bool active){Odds odds=Session.Odds;bool pushOnly=odds.Higher+odds.Lower==0;View.Higher.interactable=active && (odds.Higher>0||pushOnly);View.Lower.interactable=active && (odds.Lower>0||pushOnly);View.Collect.interactable=active;}
        void UpdateView()
        {
            View.Balance.text=Session.Balance.Format();View.Streak.text=CompactCount(Session.Streak);View.Best.text=CompactCount(Save.Number(Save.Data.bestStreak));View.Multiplier.text=Session.Multiplier.Format(true);View.Payout.text=Session.Payout.Format();View.LayoutReward();
            int index=config.StageIndex(Session.Streak);View.Stage.text=(index+1).ToString("00")+"  /  "+config.stages[index].name.ToUpperInvariant();
            Odds odds=Session.Odds;View.OddsHigher.text=OddsLabel(odds,true);View.OddsLower.text=OddsLabel(odds,false);View.DeckLabel.text=Session.Deck.Remaining+" / 52  ·  DECK "+Session.Deck.Shuffles;
            SetButtons(Session.State==RoundState.Decision);View.Glow.color=new Color(1,1,1,.65f+index*.09f);PublishState();
        }
        string OddsLabel(Odds odds,bool higher) => odds.Higher+odds.Lower==0?"Push only":odds.Wins(higher)==0?"No cards":""+odds.Probability(higher).ToString("P0")+"  ·  +"+(odds.Factor(higher,config.houseEdge,config.minimumMultiplierGrowth)-1).ToString("P0");
        string CompactCount(BigInteger number)
        {
            if(number<1000000)return number.ToString();
            string digits=number.ToString();double leading=double.Parse(digits.Substring(0,System.Math.Min(6,digits.Length)),System.Globalization.CultureInfo.InvariantCulture);
            return new ScientificNumber(leading/System.Math.Pow(10,System.Math.Min(6,digits.Length)-1),digits.Length-1).Format();
        }
        public void ApplyReducedMotion(){Save.Data.reducedMotion=true;Save.Flush();}
        public void Fullscreen()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            HiLoFullscreen();
            #else
            Screen.fullScreen=!Screen.fullScreen;
            #endif
        }
        public void SetBrowserFocus(string value) { focused=value=="1"; if(!focused)Save.Record(Session); }
        void OnApplicationFocus(bool focus){focused=focus;}
        void OnApplicationPause(bool paused){focused=!paused;if(paused && Save!=null)Save.Record(Session);}
        void OnApplicationQuit(){if(Save!=null)Save.Record(Session);}
        void PublishState()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            HiLoState(JsonUtility.ToJson(new BrowserState{state=Session.State.ToString(),currentRank=Session.Current.Rank,currentSuit=Session.Current.Suit,streak=Session.Streak.ToString(),balance=Session.Balance.Encode(),multiplier=Session.Multiplier.Encode(),higher=Session.Odds.Higher,lower=Session.Odds.Lower,equal=Session.Odds.Equal,modal=modalOpen,stage=config.StageIndex(Session.Streak),outcome=Session.LastOutcome.ToString(),predictions=Save.Data.predictions,pressure=Save.Data.pressure,reducedMotion=Save.Data.reducedMotion,sound=Save.Data.sound,music=Save.Data.music}));
            #endif
        }
        [System.Serializable] sealed class BrowserState {public string state,streak,balance,multiplier,outcome,predictions;public int currentRank,currentSuit,higher,lower,equal,stage;public bool modal,pressure,reducedMotion,sound,music;}
    }
}
