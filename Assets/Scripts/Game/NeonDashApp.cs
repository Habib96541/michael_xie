using NeonDash.Core;
using UnityEngine;
using UnityEngine.EventSystems;

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
        string seed = "2026";
        float cameraX;
        float cameraY = 5.2f;
        float idleTime;
        int runToken;
        bool resultPresented;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            SetupCamera();

            store = new RecordStore();
            store.Load();
            Store = store;
            seed = store.LastSeed;

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
            background.Build(cam, fx);

            Session = new RunSession();
            BuildLevel(seed);

            Canvas canvas = UiKit.CreateCanvas("Geometry Dash UI", 10);
            lobby = UiScreen.Create<UiLobby>(this, canvas.transform, "Lobby Screen");
            hud = UiScreen.Create<UiHud>(this, canvas.transform, "HUD Screen");
            result = UiScreen.Create<UiResult>(this, canvas.transform, "Result Screen");
            records = UiScreen.Create<UiRecords>(this, canvas.transform, "Records Screen");
            pause = UiScreen.Create<UiPause>(this, canvas.transform, "Pause Screen");
        }

        void Start()
        {
            EnterLobby();
        }

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
            playerView.ResetForRun(CurrentLevel);
            levelView.SetCeilingVisible(false);
            Audio.StopMusic();
            idleTime = 0f;
        }

        public void StartRun()
        {
            runToken++;
            resultPresented = false;
            Session = new RunSession();
            Session.Begin(CurrentLevel);

            playerView.ResetForRun(CurrentLevel);
            levelView.SetCeilingVisible(true);
            levelView.ResetPickups();
            lobby.Hide();
            result.Hide();
            records.Hide();
            pause.Hide();
            hud.Show();
            hud.ResetForRun();

            Phase = AppPhase.Playing;
            cameraX = Session.Player.X + CameraLead();
            cameraY = 4.7f;
            Audio.StartMusic();
            ApplyCamera(0f);

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
            pause.Show();
        }

        public void ResumeRun()
        {
            if (Phase != AppPhase.Paused) return;
            Phase = AppPhase.Playing;
            pause.Hide();
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

        public void SetSeed(string value)
        {
            string cleaned = RecordStore.SanitiseSeed(value);
            if (cleaned == seed)
            {
                store.SetSeed(cleaned);
                return;
            }
            seed = cleaned;
            store.SetSeed(cleaned);
            store.Save();
            BuildLevel(seed);
            lobby.Refresh();
            playerView.ResetForRun(CurrentLevel);
        }

        public void RandomSeed()
        {
            string next = Random.Range(1000, 99999).ToString();
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
            levelView.SetCeilingVisible(Phase == AppPhase.Playing || Phase == AppPhase.Paused || Phase == AppPhase.Rolling);
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
                case AppPhase.Results:
                    if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return)) RetryRun();
                    break;
            }
        }

        void UpdatePlaying()
        {
            float dt = Time.deltaTime;
            bool held = JumpHeld();
            bool pressed = JumpPressed();

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
                for (int i = 0; i < Session.FrameGems.Count; i++)
                {
                    int index = Session.FrameGems[i];
                    bool high = IsHighGem(index);
                    Color gemColor = high ? Palette.Cyan : Palette.Gold;
                    levelView.CollectGem(index);
                    fx.Ring(new Vector2(LevelGemX(index), LevelGemY(index)), gemColor, 10, 4.5f);
                    hud.PulseProgress();
                }
                Audio.PlayOrb();
            }
            if (events.Landed) playerView.OnLanded(player, fx);
            if (!player.Alive)
            {
                Audio.PlayDeath();
                fx.Burst(center, Palette.Lime, 34);
                fx.Burst(center, Palette.Magenta, 18);
                fx.AddShake(0.75f);
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
            idleTime += Time.deltaTime;
            float targetX = 10f + Mathf.Sin(idleTime * 0.16f) * 6f;
            cameraX = Mathf.Lerp(cameraX, targetX, Time.deltaTime * 1.2f);
            cameraY = Mathf.Lerp(cameraY, 5.4f, Time.deltaTime * 1.5f);
            ApplyCamera(Time.deltaTime);
        }

        /// <summary>
        /// How far ahead of the player the camera sits. Scaled to the visible width so the cube
        /// keeps the same on-screen position whether the browser window is wide or narrow
        /// (a fixed offset pushes the cube off the left edge in portrait windows).
        /// </summary>
        float CameraLead()
        {
            float halfWidth = cam.orthographicSize * Mathf.Max(0.1f, cam.aspect);
            return Mathf.Clamp(halfWidth * 0.6f, 3.2f, 8.2f);
        }

        void UpdateCamera(float dt, PlayerSim player)
        {
            float targetX = player.X + CameraLead();
            cameraX = Mathf.Lerp(cameraX, targetX, 1f - Mathf.Exp(-9f * dt));

            // Like Geometry Dash, the camera holds its height in cube mode so the floor stays put
            // instead of sliding out of frame on every jump. It only climbs for the ceiling run
            // (flipped gravity) or to follow a ship high in the corridor.
            float targetY;
            if (player.GravitySign < 0) targetY = 8.5f;
            else if (player.Mode == PlayerMode.Ship) targetY = Mathf.Clamp(player.CenterY + 3.4f, 5.2f, 7.2f);
            else targetY = 4.7f;

            cameraY = Mathf.Lerp(cameraY, targetY, 1f - Mathf.Exp(-3.4f * dt));
            ApplyCamera(dt);
        }

        void ApplyCamera(float dt)
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
        bool JumpHeld()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                    if (!PointerOverUi(Input.GetTouch(i).fingerId)) return true;
            }
            if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) return true;
            return Input.GetMouseButton(0) && !PointerOverUi(-1);
        }

        bool JumpPressed()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) return true;
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase == TouchPhase.Began && !PointerOverUi(touch.fingerId)) return true;
                }
            }
            return Input.GetMouseButtonDown(0) && !PointerOverUi(-1);
        }

        static bool PointerOverUi(int fingerId)
        {
            if (EventSystem.current == null) return false;
            return fingerId >= 0
                ? EventSystem.current.IsPointerOverGameObject(fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }
    }
}
