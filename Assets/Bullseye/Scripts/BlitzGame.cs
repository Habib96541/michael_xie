using UnityEngine;
using UnityEngine.EventSystems;

namespace BullseyeBlitz
{
    public sealed class BlitzGame : MonoBehaviour
    {
        [SerializeField] private BlitzConfig _config;
        [SerializeField] private BoothSpawner _spawner;
        [SerializeField] private Camera _camera;
        private readonly RoundRules _rules = new RoundRules();
        private WalletStore _wallet;
        private BlitzUI _ui;
        private BlitzAudio _audio;
        private BlitzFeedback _feedback;
        private int _suppressThroughFrame;
        private int _warningSecond = -1;
        private int _countdownSecond = -1;
        private float _telemetryAt;
        private double _lastReal;
        private double _countdown;
        private double _resultsAt;
        private bool _success;
        private bool _settled;
        public RoundRules Rules => _rules;
        public Camera GameCamera => _camera;
        public bool CanShoot => _rules.State == RoundState.Playing && !_rules.Paused && !_ui.ModalOpen && UnityEngine.Time.frameCount > _suppressThroughFrame;

        public void Configure(BlitzConfig config, BoothSpawner spawner, Camera camera)
        {
            _config = config; _spawner = spawner; _camera = camera;
        }
        private void Start()
        {
            if (_config == null || !_config.IsValid()) { Debug.LogError("Invalid Bullseye configuration."); enabled = false; return; }
            _wallet = new WalletStore();
            _ui = new GameObject("Bullseye UI").AddComponent<BlitzUI>();
            _ui.Configure(_config); _ui.PauseRequested = Pause;
            _audio = gameObject.AddComponent<BlitzAudio>(); _audio.Configure(_config, _wallet);
            _ui.ActionInvoked = () => { _audio.Activate(); _audio.Play(); };
            _feedback = gameObject.AddComponent<BlitzFeedback>(); _feedback.Configure(_camera, _config.BodyFont);
            gameObject.AddComponent<BlitzPointer>().Configure(this);
            _lastReal = UnityEngine.Time.realtimeSinceStartupAsDouble;
            Home();
        }
        private void Update()
        {
            SyncClock();
            if (_rules.State == RoundState.Countdown)
            {
                double left = _countdown - UnityEngine.Time.realtimeSinceStartupAsDouble;
                int count = Mathf.CeilToInt((float)left);
                if (count != _countdownSecond && count > 0) { _countdownSecond = count; _audio.Play(false, 1f + (3 - count) * .15f); }
                _ui.Hud(20, 0, Mathf.CeilToInt((float)left).ToString());
                if (left <= 0)
                {
                    _rules.Start(); _spawner.Begin(_config);
                    _lastReal = UnityEngine.Time.realtimeSinceStartupAsDouble;
                }
            }
            else
            {
                _ui.Hud(20 - _rules.Time, _rules.Pending, "");
            }
            _feedback.SetPlaying(CanShoot);
            int second = Mathf.CeilToInt((float)(20 - _rules.Time));
            if (CanShoot && second <= 5 && second != _warningSecond) { _warningSecond = second; _audio.Play(false, 1.5f); }
            if (_rules.State == RoundState.Playing && !_rules.Paused) { _spawner.Tick(_rules.Time); }
            if ((_rules.State == RoundState.Success || _rules.State == RoundState.BombHit) && UnityEngine.Time.realtimeSinceStartupAsDouble >= _resultsAt) { Results(); }
            Layout();
            if (UnityEngine.Time.unscaledTime >= _telemetryAt)
            {
                _telemetryAt = UnityEngine.Time.unscaledTime + .1f;
                Publish();
            }
        }
        public void SyncClock()
        {
            double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
            RoundState previous = _rules.State;
            _rules.Advance(now - _lastReal);
            _lastReal = now;
            if (previous == RoundState.Playing && _rules.State == RoundState.Success) { Terminal(true); }
        }
        public void Shoot(Vector2 world)
        {
            SyncClock();
            if (!CanShoot) { return; }
            Physics2D.SyncTransforms();
            Collider2D[] overlaps = Physics2D.OverlapPointAll(world, (1 << 8) | (1 << 9));
            BoothTarget chosen = null;
            foreach (Collider2D collider in overlaps)
            {
                if (collider.gameObject.layer == 9) { _audio.Play(false, .7f); return; }
                BoothTarget target = collider.GetComponent<BoothTarget>();
                if (target != null && target.State == TargetState.Exposed && (chosen == null || target.Order > chosen.Order)) { chosen = target; }
            }
            if (chosen == null) { _audio.Play(false, .7f); _feedback.Show(world, "MISS", new Color(.7f, .75f, 1), _wallet.Data.ReducedEffects); return; }
            bool bomb = chosen.Bomb; int reward = chosen.Reward;
            if (!chosen.Consume(chosen.Activation, _rules.Time)) { return; }
            _rules.Hit(bomb, reward);
            if (_wallet.Data.Vibration) { BlitzBrowser.Vibrate(); }
            _audio.Play(!bomb, bomb ? .45f : 1.2f);
            if (bomb) { _feedback.Explosion(world, _wallet.Data.ReducedEffects); }
            else { _feedback.Show(world, "+" + reward, new Color(1, .85f, .2f), _wallet.Data.ReducedEffects); }
            if (bomb) { Terminal(false); }
        }
        private void Terminal(bool success)
        {
            _success = success;
            _spawner.Clear();
            _settled = !success || _wallet.Settle(_rules.Id, _rules.Pending);
            if (success) { _audio.Play(true, 1f); }
            _resultsAt = UnityEngine.Time.realtimeSinceStartupAsDouble + (success ? .25 : .6);
        }
        private void Results()
        {
            _rules.ShowResults();
            string message = _success
                ? (_settled ? "BANKED  +" + _rules.Pending + " coins" : "Saving failed. Your payout is pending. Retry saving before leaving.")
                : "Bomb hit! Lost " + _rules.Lost + " round coins.\nYour saved coins are safe.";
            message += "\nWALLET  " + _wallet.Data.Wallet;
            RectTransform panel = _ui.Panel(_success ? "BOOTH CLEARED!" : "BOOM!", message);
            if (!_settled)
            {
                _ui.Action(panel, "Retry saving", 0, () => { _settled = _wallet.Settle(_rules.Id, _rules.Pending); Results(); });
                return;
            }
            _ui.Action(panel, "Retry — Free", 0, Begin);
            _ui.Action(panel, "Home", 1, Home);
        }
        private void Begin()
        {
            _audio.Activate(); _audio.Play(); _ui.Close(); _spawner.Clear();
            _rules.BeginCountdown(); _countdown = UnityEngine.Time.realtimeSinceStartupAsDouble + 3;
            _warningSecond = -1;
            _countdownSecond = -1;
            _suppressThroughFrame = UnityEngine.Time.frameCount + 1;
            _lastReal = UnityEngine.Time.realtimeSinceStartupAsDouble;
        }
        private void Home()
        {
            _rules.Abandon(); _spawner.Clear();
            RectTransform panel = _ui.Panel("CARNIVAL COIN CHALLENGE", "Shoot coin targets. Avoid bombs. Survive 20 seconds to keep your coins.\n\nWALLET  " + _wallet.Data.Wallet + "     BEST ROUND  " + _wallet.Data.Best);
            _ui.Action(panel, "Play — Free", 0, Begin);
            _ui.Action(panel, "Settings", 1, Settings);
            _ui.Action(panel, "How to Play", 2, Help);
        }
        public void Pause()
        {
            SyncClock();
            if (_rules.State != RoundState.Playing) { return; }
            _rules.Paused = true;
            RectTransform panel = _ui.Panel("PAUSED", "Round coins: " + _rules.Pending + "\nThe clock and targets are frozen.");
            _ui.Action(panel, "Resume", 0, () => { _ui.Close(); _lastReal = UnityEngine.Time.realtimeSinceStartupAsDouble; _rules.Paused = false; _suppressThroughFrame = UnityEngine.Time.frameCount + 1; });
            _ui.Action(panel, "Restart", 1, () => Confirm(true));
            _ui.Action(panel, "Home", 2, () => Confirm(false));
        }
        private void Confirm(bool restart)
        {
            RectTransform panel = _ui.Panel("LEAVE THIS ROUND?", "You will lose " + _rules.Pending + " pending round coins.\nPreviously banked coins remain safe.");
            _ui.Action(panel, restart ? "Discard & Restart" : "Discard & Home", 0, () => { if (restart) { Begin(); } else { Home(); } });
            _ui.Action(panel, "Cancel", 1, Pause);
        }
        private void Settings()
        {
            _audio.Activate();
            RectTransform panel = _ui.Panel("SETTINGS", "Audio starts after interaction.\nSettings are saved on this device.");
            _ui.Volume(panel, "Music", .42f, _wallet.Data.Music, value => { _wallet.Data.Music = value; _audio.Apply(); _wallet.Save(); });
            _ui.Volume(panel, "Sound", .33f, _wallet.Data.Sound, value => { _wallet.Data.Sound = value; _audio.Apply(); _wallet.Save(); });
            _ui.Action(panel, "Reduced effects: " + (_wallet.Data.ReducedEffects ? "On" : "Off"), 2, () => { _wallet.Data.ReducedEffects = !_wallet.Data.ReducedEffects; _wallet.Save(); Settings(); });
            if (BlitzBrowser.SupportsVibration)
            {
                _ui.Action(panel, "Vibration: " + (_wallet.Data.Vibration ? "On" : "Off"), 3, () => { _wallet.Data.Vibration = !_wallet.Data.Vibration; _wallet.Save(); Settings(); });
            }
            _ui.Action(panel, "Home", BlitzBrowser.SupportsVibration ? 4 : 3, Home);
        }
        private void Help()
        {
            RectTransform panel = _ui.Panel("HOW TO PLAY", "Click or tap exposed coins to earn their displayed value.\nAvoid bombs! They end the round and erase round coins.\nSurvive 20 seconds to bank your earnings.\nUnlimited shots. Free retries. No early cash-out.");
            _ui.Action(panel, "Home", 0, Home);
        }
        private void Layout()
        {
            float aspect = Mathf.Max(.3f, _camera.aspect);
            _camera.orthographicSize = Mathf.Max(4.1f, 4f / aspect);
            float width = Mathf.Min(7.2f, _camera.orthographicSize * aspect * 1.65f);
            for (int i = 0; i < _spawner.Slots.Length; i++)
            {
                _spawner.Slots[i].Place(new Vector3((i % 3 - 1) * width / 3, 2.25f - (i / 3) * 2.05f, 0));
            }
        }
        private void OnApplicationFocus(bool focus) { if (!focus && _ui != null) { Pause(); } }
        private void OnApplicationPause(bool paused) { if (paused && _ui != null) { Pause(); } }
        public void BrowserHidden() { if (_ui != null) { Pause(); } }
        private void Publish()
        {
            BlitzBrowser.Slot[] slots = new BlitzBrowser.Slot[_spawner.Slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                BoothTarget target = _spawner.Slots[i];
                Vector3 screen = _camera.WorldToScreenPoint(target.transform.position);
                slots[i] = new BlitzBrowser.Slot
                {
                    State = target.State.ToString(), Bomb = target.Bomb, Reward = target.Reward,
                    Activation = target.Activation, X = screen.x / Screen.width, Y = 1 - screen.y / Screen.height,
                    Collider = target.GetComponent<CircleCollider2D>().enabled,
                    Diameter = 1.118f / (_camera.orthographicSize * 2) * Screen.height
                };
            }
            BlitzBrowser.Publish(new BlitzBrowser.Snapshot
            {
                State = _rules.State.ToString(), Paused = _rules.Paused, Time = _rules.Time,
                Pending = _rules.Pending, Wallet = _wallet.Data.Wallet, Completed = _wallet.Data.Completed,
                Best = _wallet.Data.Best, Slots = slots, Frames = UnityEngine.Time.frameCount,
                Fps = 1f / Mathf.Max(.001f, UnityEngine.Time.unscaledDeltaTime)
            });
        }
    }
}
