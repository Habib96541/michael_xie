using System.Collections.Generic;
using NeonDash.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeonDash
{
    public enum AppPhase { Lobby, Playing, Paused, Rolling, Results, Records }

    /// <summary>
    /// Top-level game controller: owns the level, the run session, the presentation systems and
    /// every screen transition. This is the only MonoBehaviour the scene needs.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class NeonDashApp : MonoBehaviour
    {
        public static NeonDashApp Instance { get; private set; }

        public AppPhase Phase { get; private set; }
        public Level CurrentLevel { get; private set; }
        public RunSession Session { get; private set; }
        public RecordStore Store { get; private set; }
        public AudioDirector Audio { get; private set; }
        public string Seed { get { return seed; } }
        public string PlayerName { get { return store == null ? "PLAYER" : store.PlayerName; } }

        Camera cam;
        LevelView levelView;
        PlayerView playerView;
        Fx fx;
        BackgroundRig background;
        UiLobby lobby;
        UiHud hud;
        UiResult result;
        UiRecords records;
        UiPause pause;

        RecordStore store;
        string seed;
        float cameraX;
        float cameraY = 5.2f;
        float idleTime;
        int runToken;
        bool resultPresented;
        readonly HashSet<int> gameplayTouches = new HashSet<int>();
        readonly List<RaycastResult> touchHits = new List<RaycastResult>();
        PointerEventData touchPointer;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

#if !UNITY_WEBGL
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }
#endif

            SetupCamera();

            store = new RecordStore();
            store.Load();
            Store = store;
            seed = store.LastSeed;
            store.Save();

            levelView = new GameObject("Level View").AddComponent<LevelView>();
            playerView = new GameObject("Player View").AddComponent<PlayerView>();
            fx = new GameObject("Fx").AddComponent<Fx>();
            fx.transform.SetParent(transform, false);
            playerView.transform.SetParent(transform, false);
            levelView.transform.SetParent(transform, false);

            Audio = gameObject.AddComponent<AudioDirector>();
            Audio.SetMuted(store.Muted);

            background = new GameObject("Background").AddComponent<BackgroundRig>();
            background.transform.SetParent(transform, false);
            background.Build(cam);

            Session = new RunSession();
            BuildLevel(seed);

            RectTransform canvas = UiKit.CreateCanvas("NeonDash UI", 10);
            lobby = UiScreen.Create<UiLobby>(this, canvas, "Lobby Screen");
            hud = UiScreen.Create<UiHud>(this, canvas, "HUD Screen");
            result = UiScreen.Create<UiResult>(this, canvas, "Result Screen");
            records = UiScreen.Create<UiRecords>(this, canvas, "Records Screen");
            pause = UiScreen.Create<UiPause>(this, canvas, "Pause Screen");
        }

        void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            NeonDash_InstallSeedPaste(gameObject.name);
#endif
            EnterLobby();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        static extern void NeonDash_InstallSeedPaste(string target);

        [UnityEngine.Scripting.Preserve]
        public void PasteSeed(string value)
        {
            if (Phase == AppPhase.Lobby) lobby.PasteSeed(value);
        }
#endif

        void SetupCamera()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cam = cameraObject.AddComponent<Camera>();
            }
            cam.orthographic = true;
            if (FindAnyObjectByType<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.transform.position = new Vector3(0f, cameraY, -10f);
        }

        // ------------------------------------------------------------------
        // Flow
        // ------------------------------------------------------------------
        void EnterLobby()
        {
            Phase = AppPhase.Lobby;
            Time.timeScale = 1f;
            hud.Hide();
            result.Hide();
            pause.Hide();
            records.Hide();
            lobby.Show();
            lobby.Refresh();
            playerView.ResetForRun();
            levelView.SetCeilingVisible(false);
            background.SetMode(PlayerMode.Cube, false, true);
            Audio.StopMusic();
            idleTime = 0f;
        }

        public void StartRun()
        {
            runToken++;
            resultPresented = false;
            Session = new RunSession();
            Session.Begin(CurrentLevel);

            playerView.ResetForRun();
            fx.ResetForRun();
            levelView.SetCeilingVisible(false);
            background.SetMode(PlayerMode.Cube, false, true);
            levelView.ResetPickups();
            lobby.Hide();
            result.Hide();
            records.Hide();
            pause.Hide();
            hud.Show();
            hud.ResetForRun();

            Phase = AppPhase.Playing;
            gameplayTouches.Clear();
            FitCamera();
            cameraX = Session.Player.X + CameraLead();
            cameraY = cam.orthographicSize * (2.9f / 6f);
            Audio.RestartMusic();
            ApplyCamera();

            hud.ShowTutorial("SEED " + seed, "TAP TO JUMP  ·  SURVIVE 30 SECONDS", Palette.Cyan);
        }

        public void RetryRun()
        {
            StartRun();
        }

        public void ReturnToLobby()
        {
            EnterLobby();
        }

        public void PauseRun()
        {
            if (Phase != AppPhase.Playing) return;
            Phase = AppPhase.Paused;
            gameplayTouches.Clear();
            Audio.PauseMusic();
            pause.Show();
        }

        public void ResumeRun()
        {
            if (Phase != AppPhase.Paused) return;
            Phase = AppPhase.Playing;
            Audio.ResumeMusic();
            pause.Hide();
        }

        void OnApplicationPause(bool suspended)
        {
            if (suspended && Phase == AppPhase.Playing) PauseRun();
        }

        public void OpenRecords()
        {
            if (Phase == AppPhase.Playing) PauseRun();
            records.Present(store.Table);
            lobby.Hide();
            result.Hide();
            pause.Hide();
            Phase = AppPhase.Records;
        }

        public void CloseRecords()
        {
            records.Hide();
            EnterLobby();
        }

        public bool SetSeed(string value)
        {
            if (!SeedCode.TryNormalize(value, out string cleaned)) return false;
            if (cleaned == seed) return true;
            seed = cleaned;
            store.SetSeed(cleaned);
            store.Save();
            BuildLevel(seed);
            playerView.ResetForRun();
            return true;
        }

        public void RandomSeed()
        {
            string next;
            do { next = SeedCode.Generate(); }
            while (next == seed || store.Table.Find(next) != null);
            SetSeed(next);
            idleTime = 0f;
        }

        public void SetPlayerName(string value)
        {
            store.SetPlayerName(value);
        }

        /// <summary>Called when the name field loses focus, so we are not writing prefs per keystroke.</summary>
        public void SaveProfile()
        {
            store.Save();
            lobby.Refresh();
        }

        public void ToggleMute()
        {
            store.SetMuted(!store.Muted);
            store.Save();
            Audio.SetMuted(store.Muted);
        }

        void BuildLevel(string levelSeed)
        {
            CurrentLevel = LevelBuilder.Build(levelSeed);
            levelView.Build(CurrentLevel);
            levelView.SetCeilingVisible(false);
        }

        // ------------------------------------------------------------------
        // Frame
        // ------------------------------------------------------------------
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Phase == AppPhase.Playing) PauseRun();
                else if (Phase == AppPhase.Paused) ResumeRun();
                else if (Phase == AppPhase.Results || Phase == AppPhase.Records) ReturnToLobby();
                return;
            }

            if (Input.GetKeyDown(KeyCode.M)) ToggleMute();

            switch (Phase)
            {
                case AppPhase.Lobby:
                case AppPhase.Records:
                    UpdateIdleCamera();
                    break;
                case AppPhase.Playing:
                    UpdatePlaying();
                    break;
                case AppPhase.Rolling:
                    ApplyCamera();
                    break;
                case AppPhase.Results:
                    if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)) RetryRun();
                    break;
            }
        }

        void UpdatePlaying()
        {
            float dt = Time.deltaTime;
            ReadJumpInput(out bool held, out bool pressed);

            Session.Advance(dt, held, pressed);
            HandleEvents();

            playerView.Tick(Session.Player, dt, fx, Session.Player.Alive);
            UpdateCamera(dt, Session.Player);

            if (!Session.Running) CompleteRun();
        }

        void HandleEvents()
        {
            PlayerSim player = Session.Player;
            // FrameEvents merges every fixed step of this frame, so nothing that happened in an
            // earlier sub-step (a gem, a jump) can be overwritten and lost.
            SimEvents events = Session.FrameEvents;
            Vector2 center = new Vector2(player.X, player.CenterY);

            if (events.Jumped)
            {
                Audio.PlayJump();
                fx.Emit(new Vector2(player.X, player.Y), Palette.Alpha(Palette.Cyan, 0.6f), 6, 3.2f, 0.13f, 0.32f);
            }
            if (events.Orb)
            {
                // Orbs are single use, so the one that fired pops and vanishes like a gem.
                levelView.ConsumeOrb(events.OrbIndex);
                Audio.PlayOrb();
                fx.Ring(center, Palette.Gold, 16, 6f);
                hud.PulseProgress();
            }
            if (events.Pad)
            {
                levelView.ConsumePad(events.PadIndex);
                Audio.PlayPad();
                fx.Ring(new Vector2(player.X, player.Y + 0.2f), Palette.Pink, 14, 7f);
            }
            if (events.Portal)
            {
                Audio.PlayPortal();
                fx.Ring(center, Palette.Violet, 22, 8f);
                fx.AddShake(0.12f);
            }
            if (events.ModeChanged || events.GravityChanged)
            {
                levelView.SetCeilingVisible(player.Mode == PlayerMode.Ship || player.GravitySign < 0);
                background.SetMode(player.Mode, player.GravitySign < 0);
            }
            if (events.ModeChanged)
            {
                hud.FlashMode();
                if (player.Mode == PlayerMode.Ship)
                    hud.ShowTutorial("SHIP MODE", "HOLD TO FLY  ·  RELEASE TO FALL", Palette.Magenta);
                else
                    hud.ShowTutorial("CUBE MODE", "TAP TO JUMP  ·  HOLD TO CHAIN JUMPS", Palette.Lime);
            }
            if (events.GravityChanged && player.Mode == PlayerMode.Cube)
            {
                hud.FlashMode();
                hud.ShowTutorial(player.GravitySign < 0 ? "GRAVITY FLIPPED" : "GRAVITY NORMAL",
                    player.GravitySign < 0 ? "YOU FALL UP  ·  TAP TO JUMP DOWN" : "YOU FALL DOWN  ·  TAP TO JUMP UP",
                    Palette.Violet);
            }
            if (events.Gem)
            {
                // One burst per gem: several can be touched inside a single step.
                bool highPickup = false;
                for (int i = 0; i < Session.FrameGems.Count; i++)
                {
                    int index = Session.FrameGems[i];
                    bool high = IsHighGem(index);
                    highPickup |= high;
                    Color gemColor = high ? Palette.Cyan : Palette.Gold;
                    levelView.CollectGem(index);
                    fx.Ring(new Vector2(LevelGemX(index), LevelGemY(index)), gemColor, 10, 4.5f);
                    hud.PulseProgress();
                }
                Audio.PlayCoin(highPickup);
            }
            if (events.Landed && player.Alive) playerView.OnLanded(player, fx);
            if (events.Died)
            {
                Audio.PlayDeath();
                fx.Shatter(PlayerView.VisualCenter(player), player.Rotation, player.GravitySign,
                    player.Mode == PlayerMode.Ship ? Palette.Cyan : Palette.Pink);
            }
        }

        bool IsHighGem(int index)
        {
            return CurrentLevel != null && index >= 0 && index < CurrentLevel.Gems.Count
                && CurrentLevel.Gems[index].Kind == GemKind.High;
        }

        float LevelGemX(int index)
        {
            return CurrentLevel != null && index >= 0 && index < CurrentLevel.Gems.Count
                ? CurrentLevel.Gems[index].X : 0f;
        }

        float LevelGemY(int index)
        {
            return CurrentLevel != null && index >= 0 && index < CurrentLevel.Gems.Count
                ? CurrentLevel.Gems[index].Y : 0f;
        }

        void UpdateIdleCamera()
        {
            FitCamera();
            idleTime += Time.deltaTime;
            float targetX = 10f + Mathf.Sin(idleTime * 0.16f) * 6f;
            cameraX = Mathf.Lerp(cameraX, targetX, Time.deltaTime * 1.2f);
            cameraY = Mathf.Lerp(cameraY, 4.3f, Time.deltaTime * 1.5f);
            ApplyCamera();
        }

        /// <summary>
        /// How far ahead of the player the camera sits. Scaled to the visible width so the cube
        /// keeps the same on-screen position whether the browser window is wide or narrow
        /// (a fixed offset pushes the cube off the left edge in portrait windows).
        /// </summary>
        float CameraLead()
        {
            float halfWidth = cam.orthographicSize * Mathf.Max(0.1f, cam.aspect);
            return Mathf.Min(halfWidth * 0.42f, 7f);
        }

        void UpdateCamera(float dt, PlayerSim player)
        {
            FitCamera();
            cameraX = player.X + CameraLead();

            // Like Geometry Dash, the camera holds its height in cube mode so the floor stays put
            // instead of sliding out of frame on every jump. It only climbs for the ceiling run
            // (flipped gravity) or to follow a ship high in the corridor.
            float targetY;
            if (player.GravitySign < 0) targetY = 6.1f;
            else if (player.Mode == PlayerMode.Ship) targetY = CurrentLevel.Ceiling * 0.5f;
            else targetY = cam.orthographicSize * (2.9f / 6f) + Mathf.Max(0f, player.CenterY - 3.5f) * 0.55f;

            cameraY = Mathf.Lerp(cameraY, targetY, 1f - Mathf.Exp(-3.4f * dt));
            ApplyCamera();
        }

        void FitCamera()
        {
            // Keep at least 7.6 world units visible ahead of the cube in portrait.
            cam.orthographicSize = Mathf.Clamp(5.4f / Mathf.Max(0.1f, cam.aspect), 6f, 18f);
        }

        void ApplyCamera()
        {
            Vector3 offset = Vector3.zero;
            if (fx != null && fx.ShakeAmount > 0.001f)
            {
                float amount = fx.ShakeAmount * 0.4f;
                offset = new Vector3(Random.Range(-amount, amount), Random.Range(-amount, amount), 0f);
            }
            cam.transform.position = new Vector3(cameraX + offset.x, cameraY + offset.y, -10f);
        }

        void CompleteRun()
        {
            if (resultPresented) return;
            resultPresented = true;

            Phase = AppPhase.Rolling;
            int token = runToken;

            bool newBest = store.Table.Submit(seed, store.PlayerName,
                Session.Result.Distance, Session.Result.Multiplier, Session.Result.Payout);
            store.Save();

            Audio.StopMusic();
            lobby.Refresh();

            if (Session.Result.Died)
            {
                TweenRunner.Delay(0.45f, delegate
                {
                    if (token != runToken || this == null) return;
                    Present();
                });
            }
            else
            {
                if (Session.Result.Completed) Audio.PlayClear();
                Present();
            }

            void Present()
            {
                hud.Hide();
                result.Present(Session.Result, newBest, delegate
                {
                    if (token != runToken) return;
                    Phase = AppPhase.Results;
                });
            }
        }

        // ------------------------------------------------------------------
        // Input
        // ------------------------------------------------------------------
        void ReadJumpInput(out bool held, out bool pressed)
        {
            held = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            pressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);

            int touchCount = Input.touchCount;
            if (touchCount == 0)
            {
                gameplayTouches.Clear();
                if (Input.GetMouseButton(0) && !OverControl(Input.mousePosition, -1))
                {
                    held = true;
                    pressed |= Input.GetMouseButtonDown(0);
                }
                return;
            }

            for (int i = 0; i < touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    gameplayTouches.Remove(touch.fingerId);
                    continue;
                }
                if (touch.phase == TouchPhase.Began)
                {
                    gameplayTouches.Remove(touch.fingerId);
                    if (!OverControl(touch.position, touch.fingerId))
                    {
                        gameplayTouches.Add(touch.fingerId);
                        pressed = true;
                    }
                }
                held |= gameplayTouches.Contains(touch.fingerId);
            }
        }

        bool OverControl(Vector2 position, int pointerId)
        {
            EventSystem events = EventSystem.current;
            if (events == null) return false;
            if (touchPointer == null) touchPointer = new PointerEventData(events);
            touchPointer.Reset();
            touchPointer.position = position;
            touchPointer.pointerId = pointerId;
            events.RaycastAll(touchPointer, touchHits);
            foreach (RaycastResult hit in touchHits)
            {
                Selectable control = hit.gameObject.GetComponentInParent<Selectable>();
                if (control != null && control.IsInteractable()) return true;
            }
            return false;
        }
    }
}
