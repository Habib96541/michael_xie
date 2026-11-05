using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BottleFlip
{
    public enum GamePhase { Title, Playing, Over }

    /// <summary>
    /// Owns the run. Every tap flips the bottle; it lands on its base almost every time,
    /// but there is a 10% chance it topples over and that ends the run.
    /// Input is polled directly (not through uGUI raycasts) so one tap can never be
    /// delivered twice by the browser's pointer/mouse event duplication.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>Chance that a flip topples the bottle (which ends the run), per flip.</summary>
        public const float FallChance = 0.10f;

        public UIRoot Ui;
        public BottleRig Rig;
        public Fx Fx;
        public Sfx Sfx;
        public GameCamera Cam;

        public GamePhase Phase { get; private set; } = GamePhase.Title;
        public int Score { get; private set; }
        public int Best { get; private set; }

        float _titleTime;
        bool _bufferedTap;
        int _forceOutcome;   // 0 = random, 1 = force stand, 2 = force fall (test hook)
        bool _starting;
        int _attempts;

        // Input edge detection. Unity's WebGL input can report a press on several
        // consecutive frames, so taps are derived from the press *edge* here.
        bool _pointerDown;
        int _upFrames;
        float _lastTapTime = -1f;
        const float TapCooldown = 0.06f;
        const int MinReleasedFrames = 3;

        public void Init()
        {
            Best = Prefs.GetInt("bf_best", 0);

            Ui.PlayPressed += RequestPlay;
            Ui.MenuPressed += ToMenu;
            Ui.SoundPressed += ToggleSound;

            Rig.Landed += OnLanded;
            Rig.Fell += OnFell;
            Rig.ReadyAgain += OnReady;

            UIButton.GlobalClickSound = () => Sfx.Click();

            Ui.SetBest(Best);
            Ui.SetSoundState(Sfx.Muted);
            Ui.ShowTitle();

            Rig.IdleMotion = true;
            Report("boot");
        }

        void Update()
        {
            _titleTime += Time.unscaledDeltaTime;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (!IsPointerOverUi())
                    OnTap(new Vector2(Screen.width * 0.5f, Screen.height * 0.6f));
            }

            PollPointer();
            InputDiagnostics();
        }

        // Temporary playtest instrumentation: how often does the raw pointer report down/up?
        float _diagT;
        int _diagDown, _diagUp, _diagEdges;
        bool _diagPrev;

        void InputDiagnostics()
        {
            bool down = Input.GetMouseButton(0) || Input.touchCount > 0;
            if (down) _diagDown++; else _diagUp++;
            if (down && !_diagPrev) _diagEdges++;
            _diagPrev = down;

            _diagT += Time.unscaledDeltaTime;
            if (_diagT < 2f) return;
            WebBridge.Log($"diag down={_diagDown} up={_diagUp} edges={_diagEdges} pos={Input.mousePosition.x:0},{Input.mousePosition.y:0}");
            _diagT = 0f;
            _diagDown = _diagUp = _diagEdges = 0;
        }

        void PollPointer()
        {
            int touches = Input.touchCount;
            Vector2 pos = Input.mousePosition;
            bool down = Input.GetMouseButton(0);

            if (touches > 0)
            {
                Touch t = Input.GetTouch(0);
                pos = t.position;
                down |= t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
            }

            bool edge = down && !_pointerDown;
            _pointerDown = down;

            if (!down)
            {
                _upFrames = Mathf.Min(_upFrames + 1, 120);
                return;
            }
            if (!edge) return;

            // A real tap always comes after a genuine release; this filters out the
            // duplicate press events some browsers deliver on the same frame.
            if (_upFrames < MinReleasedFrames)
            {
                _upFrames = 0;
                return;
            }
            _upFrames = 0;

            float now = Time.unscaledTime;
            if (_lastTapTime > 0f && now - _lastTapTime < TapCooldown) return;
            _lastTapTime = now;

            if (IsPointerOverUi()) return;
            OnTap(pos);
        }

        static bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        // ------------------------------------------------------------------ flow

        void OnTap(Vector2 screenPos)
        {
            if (_starting) return;

            if (Phase == GamePhase.Title)
            {
                if (_titleTime < 0.30f) return;
                Ui.TapFeedback(screenPos);
                StartRun();
                return;
            }

            if (Phase != GamePhase.Playing) return;

            Ui.TapFeedback(screenPos);
            TryFlip();
        }

        /// <summary>Single entry point for every "play" button in the UI.</summary>
        public void RequestPlay()
        {
            if (Phase == GamePhase.Over) PlayAgain();
            else if (Phase == GamePhase.Title) StartRun();
        }

        void StartRun()
        {
            if (Phase == GamePhase.Playing || _starting) return;

            _starting = true;
            Phase = GamePhase.Playing;
            Score = 0;
            _attempts = 0;
            _bufferedTap = false;
            _titleTime = 0f;

            Rig.IdleMotion = false;
            Rig.ResetPose();
            Cam.ResetZoom();

            Ui.HideGameOver();
            Ui.SetBest(Best);
            Ui.SetScore(0, false);
            Ui.SetHintVisible(true, false);
            Ui.HideTitle();
            Ui.ShowHud(true);

            Sfx.Pop();
            Cam.FocusBottle(false);
            Cam.Punch(0.5f);

            Tween.Delay(0.02f, () => _starting = false);
            Report("start");
        }

        public void PlayAgain()
        {
            if (Phase != GamePhase.Over) return;
            Phase = GamePhase.Title;
            _bufferedTap = false;
            Ui.HideGameOver();
            Cam.FocusBottle(false);
            Tween.Delay(0.24f, StartRun);
        }

        public void ToMenu()
        {
            Ui.HideGameOver();
            Cam.ResetZoom();
            Cam.FocusBottle(false);
            Phase = GamePhase.Title;
            _titleTime = 0f;
            Rig.ResetPose();
            Rig.IdleMotion = true;

            Tween.Delay(0.34f, () =>
            {
                Ui.HideHud();
                Ui.SetBest(Best);
                Ui.ShowTitle();
                Report("menu");
            });
        }

        void TryFlip()
        {
            if (Rig.Busy)
            {
                // Late taps get buffered so fast tappers never lose an input.
                _bufferedTap = true;
                return;
            }

            bool falls = ConsumeOutcome();
            _attempts++;

            Sfx.Whoosh();
            Cam.Punch(1f);
            Fx.Sparkle(Rig.transform.position + Vector3.up * 0.14f, 9, Pal.AmberSoft);

            Rig.Flip(falls);
            Cam.FollowJump(Rig.PlannedApex);
            Report(falls ? "flip:fall" : "flip:toss");
        }

        bool ConsumeOutcome()
        {
            if (_forceOutcome != 0)
            {
                bool forced = _forceOutcome == 2;
                _forceOutcome = 0;
                return forced;
            }
            return Random.value < FallChance;
        }

        void OnReady()
        {
            if (Phase != GamePhase.Playing) return;
            if (_bufferedTap)
            {
                _bufferedTap = false;
                TryFlip();
            }
        }

        /// <summary>Bottle planted on its base: score one, then keep flipping.</summary>
        void OnLanded()
        {
            Score++;
            Ui.SetScore(Score, true);
            Ui.PlusOne(Rig.transform.position + Vector3.up * 1.98f);
            if (_attempts >= 3) Ui.SetHintVisible(false);
            CheckMilestone(Score);

            Sfx.Land();
            Fx.Dust(Rig.transform.position, 8, 0.65f);
            Fx.Sparkle(Rig.transform.position + Vector3.up * 0.25f, 6, Pal.AmberSoft);
            Cam.Shake(0.09f, 0.18f);
            Cam.ZoomTo(1f, 0.5f);

            Report("landed");
        }

        /// <summary>The bottle toppled over: run over.</summary>
        void OnFell()
        {
            Phase = GamePhase.Over;
            _bufferedTap = false;

            Sfx.Fall();
            Cam.Shake(0.42f, 0.5f);
            Cam.ZoomTo(1f, 0.45f);
            Cam.FocusBottle(true);

            Vector3 p = Rig.transform.position;
            Fx.Dust(p + Vector3.up * 0.15f, 30, 1.8f);
            Ui.Flash(0.30f, Pal.Coral);

            Report("fell");
            StartCoroutine(FallSequence());
        }

        IEnumerator FallSequence()
        {
            Time.timeScale = 0.2f;
            yield return new WaitForSecondsRealtime(0.45f);

            float t = 0f;
            const float recover = 0.3f;
            while (t < recover)
            {
                t += Time.unscaledDeltaTime;
                Time.timeScale = Mathf.Lerp(0.2f, 1f, t / recover);
                yield return null;
            }
            Time.timeScale = 1f;

            yield return new WaitForSecondsRealtime(0.2f);

            bool newBest = Score > Best;
            if (newBest)
            {
                Best = Score;
                Prefs.SetInt("bf_best", Best);
            }

            Ui.HideHud();
            Ui.SetBest(Best);
            Ui.ShowGameOver(Score, Best, newBest);
            if (newBest) Sfx.Best();

            Report(newBest ? "new-best" : "panel");
        }

        void ToggleSound()
        {
            Sfx.ToggleMute();
            Ui.SetSoundState(Sfx.Muted);
        }

        void CheckMilestone(int score)
        {
            switch (score)
            {
                case 10:
                    Ui.Toast("TEN LANDED - STEADY HANDS", Pal.Amber);
                    Sfx.Pop();
                    break;
                case 25:
                    Ui.Toast("TWENTY FIVE LANDED", Pal.Teal);
                    Sfx.Pop();
                    break;
                case 50:
                    Ui.Toast("FIFTY LANDED?!", Pal.Coral);
                    Sfx.Pop();
                    break;
                case 100:
                    Ui.Toast("ONE HUNDRED LANDED. UNREAL.", Pal.Amber);
                    Sfx.Pop();
                    break;
            }
        }

        void Report(string lastEvent)
        {
            string phaseName = Phase == GamePhase.Title ? "title" : (Phase == GamePhase.Playing ? "playing" : "over");
            WebBridge.Report(phaseName, Score, Best, lastEvent);
        }

        // ------------------------------------------------------- test hooks (JS)

        /// <summary>Forces the next flip outcome. kind: "fall", "stand" or "random".</summary>
        public void DebugForceOutcome(string kind)
        {
            _forceOutcome = kind == "fall" ? 2 : (kind == "stand" ? 1 : 0);
            WebBridge.Log("force:" + kind);
        }

        public void DebugTap() => OnTap(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

        public void DebugPlayAgain() => PlayAgain();

        /// <summary>Captures the current frame and posts it to the local playtest server.</summary>
        public void DebugScreenshot(string name) => StartCoroutine(CaptureShot(name));

        IEnumerator CaptureShot(string name)
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex == null)
            {
                WebBridge.Log("shot:null");
                yield break;
            }
            byte[] png = tex.EncodeToPNG();
            Destroy(tex);
            WebBridge.UploadPng(name, png);
            WebBridge.Log("shot:" + name + " bytes=" + png.Length);
        }
    }
}
