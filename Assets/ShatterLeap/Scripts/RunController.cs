using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ShatterLeap
{
    public sealed class RunController : MonoBehaviour
    {
        [SerializeField] private GameConfig _config;
        [SerializeField] private BridgeView _bridge;
        [SerializeField] private BridgeCamera _camera;
        [SerializeField] private GameUi _ui;
        [SerializeField] private AudioEffects _effects;
        private ContestantMotor _player;
        private GlassPanel _target;
        private float _phaseTime;
        private float _reportTime;
        private bool _terminalHandled;
        private Vector3 _fallAnchor;
        public RunRules Run { get; private set; }
        public SaveStore Saves { get; private set; }
        public bool Paused { get; private set; }
        public bool DialogOpen { get; set; }
        public bool CanChoose => Run != null && Run.State == RunState.Playing && !Paused && !DialogOpen;
        public bool Active => Run != null && (!Run.IsTerminal || Run.State == RunState.Falling);
        public float PreviewRemaining => _phaseTime;
        public event Action Changed;

        private void Start()
        {
            _config.Validate();
            Saves = new SaveStore(PlayerPrefs.GetString(SaveStore.Key, ""), json =>
            {
                PlayerPrefs.SetString(SaveStore.Key, json);
                PlayerPrefs.Save();
            });
            _player = Instantiate(_config.ContestantPrefab).GetComponent<ContestantMotor>();
            _effects.Initialize(Saves.Data);
            _ui.Initialize(this, _config);
            ShowHome();
#if UNITY_WEBGL && !UNITY_EDITOR
            ShatterBindVisibility(gameObject.name);
#endif
        }
        public void Gesture()
        {
            _effects.Unlock();
            _effects.Play(Cue.Ui);
        }
        public void SelectDifficulty(int index)
        {
            Saves.Data.Difficulty = Mathf.Clamp(index, 0, 2);
            Saves.Save();
            Changed?.Invoke();
        }
        public void SettingsChanged()
        {
            Saves.Save();
            _effects.ApplySettings();
        }
        public void BeginRun()
        {
            Time.timeScale = 1;
            Paused = false;
            DialogOpen = false;
            _terminalHandled = false;
            _effects.ResetPool();
            Run = new RunRules(_config.Difficulties[Saves.Data.Difficulty], _config.StepValue, Guid.NewGuid().GetHashCode());
            _bridge.Generate(Run);
            _bridge.Reveal(Run, true);
            _player.ResetAt(_bridge.StartPoint);
            _camera.Overview(_bridge.Length);
            _camera.Snap();
            _phaseTime = Run.Difficulty.PreviewSeconds;
            _effects.Play(Cue.Preview);
            Changed?.Invoke();
        }
        public void ShowHome()
        {
            Time.timeScale = 1;
            Paused = false;
            DialogOpen = false;
            Run = null;
            _effects.ResetPool();
            RunRules display = new RunRules(_config.Difficulties[Saves.Data.Difficulty], _config.StepValue, 42);
            _bridge.Generate(display);
            _player.ResetAt(_bridge.StartPoint);
            _camera.Overview(_bridge.Length);
            _camera.Snap();
            Changed?.Invoke();
        }
        public void Choose(int side)
        {
            if (!CanChoose || !Run.Choose(side)) { return; }
            _target = _bridge.Panel(Run.Steps, side);
            _player.Jump(_target.LandingPoint, _config.JumpDuration, _config.JumpHeight);
            _effects.Play(Cue.Jump);
            Changed?.Invoke();
        }
        public void Pause()
        {
            if (!Active || Paused) { return; }
            Paused = true;
            Time.timeScale = 0;
            Changed?.Invoke();
        }
        public void Resume()
        {
            if (!Paused) { return; }
            Paused = false;
            DialogOpen = false;
            Time.timeScale = 1;
            Changed?.Invoke();
        }
        public void BrowserVisibility(string hidden)
        {
            if (hidden == "1") { Pause(); }
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus) { Pause(); }
        }
        private void OnApplicationPause(bool pause)
        {
            if (pause) { Pause(); }
        }
        private void OnDestroy()
        {
            Time.timeScale = 1;
        }
        private void Update()
        {
            if (_player == null) { return; }
            float delta = Time.deltaTime;
            if (!Paused)
            {
                _effects.Advance(delta);
                if (Run != null)
                {
                    if (Run.Tick(delta)) { HandleTerminal(); }
                    if (Run.State == RunState.Preview)
                    {
                        _phaseTime -= delta;
                        _camera.Overview(_bridge.Length);
                        if (_phaseTime <= 0)
                        {
                            _bridge.Reveal(Run, false);
                            Run.BeginTransition();
                            _phaseTime = _config.CameraTransitionSeconds;
                            _effects.Play(Cue.Preview);
                            Changed?.Invoke();
                        }
                    }
                    else if (Run.State == RunState.CameraTransition)
                    {
                        _phaseTime -= delta;
                        if (_phaseTime <= 0)
                        {
                            Run.BeginPlaying();
                            Changed?.Invoke();
                        }
                    }
                    _player.Advance(delta);
                    if (Run.State == RunState.Jumping && _player.Arrived)
                    {
                        bool finish = Run.AtFinish;
                        Collider support = finish ? _bridge.FinishSupport : _target.Support;
                        bool safe = Run.Land(_player.ValidateLanding(support));
                        if (Run.IsTerminal)
                        {
                            if (Run.Outcome == RunState.Falling && _target != null)
                            {
                                _target.Break();
                                _effects.BreakAt(_target.transform.position);
                            }
                            HandleTerminal();
                        }
                        else if (safe)
                        {
                            _effects.Play(Cue.Safe);
                            if (Run.AtFinish && Run.BeginFinish())
                            {
                                _target = null;
                                _player.Jump(_bridge.FinishPoint, _config.JumpDuration, _config.JumpHeight);
                            }
                            Changed?.Invoke();
                        }
                    }
                    if (Run.IsTerminal && Run.State != RunState.Results)
                    {
                        _phaseTime -= delta;
                        if (_phaseTime <= 0)
                        {
                            Run.ShowResults();
                            Changed?.Invoke();
                        }
                    }
                    if (Run.State == RunState.Falling)
                    {
                        _camera.Falling(_fallAnchor, Saves.Data.ReducedEffects);
                    }
                    else if (Run.State != RunState.Preview && Run.State != RunState.Results)
                    {
                        // Stop following the vertical fall so the bridge remains legible.
                        Vector3 focus = _player.Position;
                        focus.y = 0.12f;
                        _camera.Follow(focus, Saves.Data.ReducedEffects);
                    }
                }
                else
                {
                    _camera.Overview(_bridge.Length);
                    _player.Advance(delta);
                }
                _camera.Advance(delta);
            }
            _ui.Tick();
            _reportTime -= Time.unscaledDeltaTime;
            if (_reportTime <= 0)
            {
                _reportTime = 0.25f;
#if UNITY_WEBGL && !UNITY_EDITOR
                ShatterReport(JsonUtility.ToJson(new PublicStatus
                {
                    State = Paused ? "Paused" : Run == null ? "Home" : Run.State.ToString(),
                    Steps = Run?.Steps ?? 0, Wallet = Saves.Data.Coins,
                    RoundCoins = Run?.Earnings ?? 0, Seconds = Run?.Remaining ?? 0,
                    FallSeconds = _player.FallElapsed, PlayerY = _player.Position.y
                }));
#endif
            }
        }
        private void HandleTerminal()
        {
            if (_terminalHandled) { return; }
            _terminalHandled = true;
            _phaseTime = _config.ResultDelay;
            if (Run.Outcome == RunState.Won)
            {
                Saves.Bank(Run);
                _effects.Play(Cue.Win);
            }
            else
            {
                if (Run.Outcome == RunState.Falling)
                {
                    _fallAnchor = _player.Position;
                    _phaseTime = _config.FallResultDelay;
                    _player.Fall();
                }
                _effects.Play(Run.Outcome == RunState.Falling ? Cue.Break : Cue.Lose);
            }
            Changed?.Invoke();
        }
        [Serializable]
        private sealed class PublicStatus
        {
            public string State;
            public int Steps;
            public int Wallet;
            public int RoundCoins;
            public float Seconds;
            public float FallSeconds;
            public float PlayerY;
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void ShatterBindVisibility(string objectName);
        [DllImport("__Internal")] private static extern void ShatterReport(string json);
#endif
    }
}
