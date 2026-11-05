using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;

namespace CoinFlip
{
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] GameSettings settings;
        [SerializeField] TMP_FontAsset font;
        GameSession session;UIManager ui;AudioManager sound;CoinAnimationController animator;Round activeRound;Coroutine presentation;
        bool presenting,userPaused,applicationSuspended;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void CoinReportState(string json);
#endif
        void Awake()
        {
            if(settings==null||font==null)throw new System.InvalidOperationException("Coinflip requires the serialized GameSettings and font. Rebuild the scene using Coin Flip > Prepare Project.");
            Application.targetFrameRate=60;QualitySettings.vSyncCount=0;
            session=new GameSession(SaveManager.Load(),()=> (CoinSide)Random.Range(0,2));
            // Persist any recovered settlement before accepting further interaction.
            SaveManager.Save(session.Data);session.Changed+=Save;
            sound=gameObject.AddComponent<AudioManager>();sound.Initialize(session.Data);
            ui=gameObject.AddComponent<UIManager>();ui.Initialize(session,sound,font);
            var effects=gameObject.AddComponent<VFXManager>();effects.Initialize(ui,settings.particlePoolSize);
            animator=gameObject.AddComponent<CoinAnimationController>();animator.Initialize(ui,sound,effects,settings);
            if(session.Data.history.Count>0)animator.Settle((CoinSide)session.Data.history[0]);
            ui.FlipRequested+=TryFlip;ui.ResetRequested+=RestartRun;ui.StartRequested+=StartGame;
            ui.PauseChanged+=SetPaused;ui.MainMenuRequested+=ReturnToMenu;
        }
        void Save(){SaveManager.Save(session.Data);}
        void TryFlip()
        {
            if(presenting)return;
            if(session.Data.balance==0){RestartRun();return;}
            if(!session.TryBegin(out activeRound))return;
            presenting=true;ui.ShowFlipping();presentation=StartCoroutine(Present());
        }
        IEnumerator Present()
        {
            yield return animator.Flip(activeRound,()=>
            {
                if(session.Complete(activeRound))ui.ShowResult(activeRound);
            });
            presenting=false;presentation=null;session.Ready();ui.Refresh();
        }
        void RestartRun(){if(presenting)return;if(session.Reset()){ui.ShowReady();sound.Play(AudioManager.Cue.Transition);}}
        void StartGame(){ui.ShowGame();}
        void SetPaused(bool value){userPaused=value;if(animator!=null)animator.Suspended=userPaused||applicationSuspended;}
        void ReturnToMenu()
        {
            if(presenting)
            {
                if(presentation!=null)StopCoroutine(presentation);
                if(session.Busy)session.Complete(activeRound);
                animator.Settle(activeRound.Outcome);session.Ready();presenting=false;presentation=null;
            }
            userPaused=false;if(animator!=null)animator.Suspended=applicationSuspended;ui.ShowMainMenu();
        }
        void OnApplicationFocus(bool focused){applicationSuspended=!focused;if(animator!=null)animator.Suspended=userPaused||applicationSuspended;if(!focused&&session!=null)Save();}
        void OnApplicationPause(bool paused){applicationSuspended=paused;if(animator!=null)animator.Suspended=userPaused||applicationSuspended;if(paused&&session!=null)Save();}
        void OnDisable()
        {
            if(session==null)return;
            // Component interruption cannot strand an in-memory wager or award it twice.
            if(session.Busy){session.Complete(activeRound);if(animator!=null)animator.Settle(activeRound.Outcome);}
            StopAllCoroutines();presenting=false;presentation=null;Save();
        }
        public void BrowserVisibility(string value){if(animator!=null)animator.Suspended=value=="hidden";}
        public void ReportState()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CoinReportState(ui.Snapshot(animator.Phase,animator.Progress,animator.Suspended,settings.flipDuration));
#else
            Debug.Log(ui.Snapshot());
#endif
        }
        void OnDestroy(){if(session!=null)session.Changed-=Save;if(ui!=null){ui.FlipRequested-=TryFlip;ui.ResetRequested-=RestartRun;ui.StartRequested-=StartGame;ui.PauseChanged-=SetPaused;ui.MainMenuRequested-=ReturnToMenu;}}
    }
}
