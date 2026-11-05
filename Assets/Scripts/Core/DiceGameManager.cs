using System;
using DiceGame.Audio;
using DiceGame.Config;
using DiceGame.Effects;
using DiceGame.UI;
using UnityEngine;

namespace DiceGame.Core
{
    public sealed class DiceGameManager : MonoBehaviour
    {
        public enum GameMode
        {
            Blitz20s,
            Endless
        }

        [Header("Configuration")]
        [SerializeField] private DiceGameConfig config;

        [Header("Systems & Audio")]
        [SerializeField] private DiceAudioSynthesizer audioSynth;
        [SerializeField] private ScreenShakeController screenShake;
        [SerializeField] private FloatingTextController floatingText;
        [SerializeField] private ParticleFxController particleFx;
        [SerializeField] private VisualDiceRoller diceRoller;

        [Header("UI Views")]
        [SerializeField] private DiceStatsHeaderView statsHeader;
        [SerializeField] private DiceRoundTimerView timerView;
        [SerializeField] private DiceSliderView sliderView;
        [SerializeField] private DiceBetControlsView betControls;
        [SerializeField] private DiceGameOverModalView gameOverModal;
        [SerializeField] private DiceHistoryView historyView;

        private GameMode _currentMode = GameMode.Blitz20s;
        private long _currentBankroll;
        private long _currentBet;
        private float _currentTarget = 50.0f;
        private bool _isRollUnder = true;

        private float _timeRemaining;
        private bool _isRoundRunning;
        private int _totalRolls;
        private int _totalWins;
        private int _currentStreak;
        private int _bestStreak;
        private int _totalCrits;
        private long _startingRoundBankroll;

        private bool _isFeverActive;
        private float _feverTimer;

        private float _critMin = 70.0f;
        private float _critMax = 75.0f;

        private bool _isRolling;
        private bool _isAutoRolling;
        private float _autoRollTimer;

        private const string PrefsKeyHighScore = "DiceOverdrive_HighScore";

        private void Awake()
        {
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<DiceGameConfig>();
            }
        }

        private void Start()
        {
            StartNewBlitzRun();
        }

        private void OnEnable()
        {
            if (sliderView != null)
            {
                sliderView.OnTargetChanged += HandleTargetChanged;
                sliderView.OnToggleModeRequested += HandleToggleMode;
                sliderView.OnPresetSelected += HandlePresetSelected;
            }

            if (betControls != null)
            {
                betControls.OnHalfClicked += HandleHalfBet;
                betControls.OnDoubleClicked += HandleDoubleBet;
                betControls.OnMinClicked += HandleMinBet;
                betControls.OnMaxClicked += HandleMaxBet;
                betControls.OnRollClicked += HandleRollClicked;
                betControls.OnAutoRollToggled += HandleAutoRollToggled;
            }

            if (statsHeader != null)
            {
                statsHeader.OnModeSelectClicked += HandleModeToggle;
            }

            if (gameOverModal != null)
            {
                gameOverModal.OnPlayAgainClicked += HandlePlayAgain;
                gameOverModal.OnEndlessClicked += HandleSwitchToEndless;
            }
        }

        private void OnDisable()
        {
            if (sliderView != null)
            {
                sliderView.OnTargetChanged -= HandleTargetChanged;
                sliderView.OnToggleModeRequested -= HandleToggleMode;
                sliderView.OnPresetSelected -= HandlePresetSelected;
            }

            if (betControls != null)
            {
                betControls.OnHalfClicked -= HandleHalfBet;
                betControls.OnDoubleClicked -= HandleDoubleBet;
                betControls.OnMinClicked -= HandleMinBet;
                betControls.OnMaxClicked -= HandleMaxBet;
                betControls.OnRollClicked -= HandleRollClicked;
                betControls.OnAutoRollToggled -= HandleAutoRollToggled;
            }

            if (statsHeader != null)
            {
                statsHeader.OnModeSelectClicked -= HandleModeToggle;
            }

            if (gameOverModal != null)
            {
                gameOverModal.OnPlayAgainClicked -= HandlePlayAgain;
                gameOverModal.OnEndlessClicked -= HandleSwitchToEndless;
            }
        }

        public void StartNewBlitzRun()
        {
            _currentMode = GameMode.Blitz20s;
            _currentBankroll = config.StartingBankroll;
            _startingRoundBankroll = _currentBankroll;
            _currentBet = (long)Mathf.Clamp(config.DefaultBet, config.MinBet, _currentBankroll);
            _currentTarget = config.DefaultTarget;
            _isRollUnder = true;

            _timeRemaining = config.BlitzDurationSeconds;
            _isRoundRunning = true;
            _totalRolls = 0;
            _totalWins = 0;
            _currentStreak = 0;
            _bestStreak = 0;
            _totalCrits = 0;

            _isFeverActive = false;
            _feverTimer = 0.0f;
            _isRolling = false;
            _isAutoRolling = false;

            SpawnRandomCritZone();

            if (gameOverModal != null) gameOverModal.Hide();
            if (historyView != null) historyView.ClearHistory();

            if (statsHeader != null) statsHeader.Initialize(_currentBankroll, "20s BLITZ");
            if (timerView != null)
            {
                timerView.gameObject.SetActive(true);
                timerView.Initialize(config.BlitzDurationSeconds);
            }
            if (sliderView != null)
            {
                sliderView.Initialize(config.MinTarget, config.MaxTarget, _currentTarget, _isRollUnder);
                sliderView.SetCritZone(_critMin, _critMax);
            }
            if (betControls != null)
            {
                betControls.SetBetAmount(_currentBet);
                betControls.SetRollInteractable(true);
                betControls.SetAutoRollState(false);
            }

            UpdateOddsDisplay();
        }

        public void StartEndlessMode()
        {
            _currentMode = GameMode.Endless;
            _currentBankroll = config.StartingBankroll;
            _currentBet = (long)Mathf.Clamp(config.DefaultBet, config.MinBet, _currentBankroll);
            _isRoundRunning = true;
            _isRolling = false;
            _isAutoRolling = false;
            _currentStreak = 0;

            if (gameOverModal != null) gameOverModal.Hide();
            if (historyView != null) historyView.ClearHistory();

            if (statsHeader != null) statsHeader.Initialize(_currentBankroll, "ENDLESS");
            if (timerView != null) timerView.gameObject.SetActive(false);
            if (betControls != null)
            {
                betControls.SetBetAmount(_currentBet);
                betControls.SetRollInteractable(true);
                betControls.SetAutoRollState(false);
            }

            UpdateOddsDisplay();
        }

        private void Update()
        {
            HandleKeyboardHotkeys();

            if (_currentMode == GameMode.Blitz20s && _isRoundRunning)
            {
                int prevSec = (int)Mathf.Ceil(_timeRemaining);
                _timeRemaining -= Time.deltaTime;
                int newSec = (int)Mathf.Ceil(_timeRemaining);

                if (newSec < prevSec && newSec <= 5 && newSec >= 1)
                {
                    if (audioSynth != null) audioSynth.PlayTimerWarning();
                }

                if (timerView != null)
                {
                    timerView.SetRemainingTime(_timeRemaining);
                }

                if (_timeRemaining <= 0.0f)
                {
                    _timeRemaining = 0.0f;
                    _isRoundRunning = false;
                    _isAutoRolling = false;
                    if (betControls != null) betControls.SetAutoRollState(false);
                    EndBlitzRun();
                    return;
                }
            }

            if (_isFeverActive)
            {
                _feverTimer -= Time.deltaTime;
                float progress = Mathf.Clamp01(_feverTimer / config.FeverDurationSeconds);
                if (statsHeader != null) statsHeader.SetFeverProgress(progress, true);

                if (_feverTimer <= 0.0f)
                {
                    _isFeverActive = false;
                    if (statsHeader != null) statsHeader.SetFeverProgress(0.0f, false);
                    if (particleFx != null) particleFx.SetFeverAuraActive(false);
                }
            }

            if (_isAutoRolling && _isRoundRunning && !_isRolling)
            {
                _autoRollTimer += Time.deltaTime;
                if (_autoRollTimer >= 0.45f)
                {
                    _autoRollTimer = 0.0f;
                    ExecuteRoll();
                }
            }
        }

        private void HandleKeyboardHotkeys()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                if (_isRoundRunning && !_isRolling)
                {
                    ExecuteRoll();
                }
                else if (gameOverModal != null && gameOverModal.gameObject.activeSelf)
                {
                    HandlePlayAgain();
                }
            }

            if (kb.aKey.wasPressedThisFrame)
            {
                HandleAutoRollToggled();
            }

            if (kb.hKey.wasPressedThisFrame)
            {
                HandleHalfBet();
            }

            if (kb.dKey.wasPressedThisFrame)
            {
                HandleDoubleBet();
            }

            if (kb.mKey.wasPressedThisFrame)
            {
                HandleMaxBet();
            }

            if (kb.tKey.wasPressedThisFrame)
            {
                HandleToggleMode();
            }

            if (sliderView != null)
            {
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame)
                {
                    sliderView.ApplyPresetChance(25.0f);
                }
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame)
                {
                    sliderView.ApplyPresetChance(50.0f);
                }
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame)
                {
                    sliderView.ApplyPresetChance(75.0f);
                }
                else if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame)
                {
                    sliderView.ApplyPresetChance(90.0f);
                }
                else if (kb.leftArrowKey.wasPressedThisFrame)
                {
                    sliderView.SetTarget(_currentTarget - 5.0f);
                }
                else if (kb.rightArrowKey.wasPressedThisFrame)
                {
                    sliderView.SetTarget(_currentTarget + 5.0f);
                }
            }
        }

        private void HandlePresetSelected(float chance)
        {
            if (audioSynth != null) audioSynth.PlayTick();
        }

        public void ExecuteRoll()
        {
            if (_isRolling || !_isRoundRunning) return;

            if (_currentBankroll < _currentBet)
            {
                if (_currentBankroll <= 0)
                {
                    _isAutoRolling = false;
                    if (betControls != null) betControls.SetAutoRollState(false);
                    if (_currentMode == GameMode.Blitz20s)
                    {
                        EndBlitzRun();
                    }
                    return;
                }
                _currentBet = _currentBankroll;
                if (betControls != null) betControls.SetBetAmount(_currentBet);
            }

            _isRolling = true;
            if (betControls != null) betControls.SetRollInteractable(false);

            _currentBankroll -= _currentBet;
            if (statsHeader != null) statsHeader.SetTargetBankroll(_currentBankroll, animatePulse: false, isGain: false);

            float roll = UnityEngine.Random.Range(0.01f, 99.99f);

            bool isWin = _isRollUnder ? (roll < _currentTarget) : (roll > _currentTarget);
            bool isCrit = (roll >= _critMin && roll <= _critMax);

            float winChance = _isRollUnder ? _currentTarget : (100.0f - _currentTarget);
            float baseMultiplier = config.PayoutFactor / Mathf.Max(0.5f, winChance);
            float finalMultiplier = baseMultiplier;

            if (_isFeverActive) finalMultiplier *= config.FeverMultiplierBonus;
            if (isCrit) finalMultiplier *= config.CritBonusMultiplier;

            long payout = isWin ? (long)Math.Round(_currentBet * finalMultiplier) : 0;

            if (audioSynth != null) audioSynth.PlayRattle();

            if (diceRoller != null)
            {
                diceRoller.AnimateRoll(roll, isWin, isCrit, () =>
                {
                    OnRollAnimationComplete(roll, isWin, isCrit, payout, finalMultiplier);
                });
            }
            else
            {
                OnRollAnimationComplete(roll, isWin, isCrit, payout, finalMultiplier);
            }

            if (sliderView != null)
            {
                sliderView.AnimateResultPin(roll, isWin, isCrit);
            }
        }

        private void OnRollAnimationComplete(float roll, bool isWin, bool isCrit, long payout, float multiplier)
        {
            _isRolling = false;
            _totalRolls++;

            if (audioSynth != null) audioSynth.PlayDiceLand();

            if (isWin)
            {
                _totalWins++;
                _currentStreak++;
                if (_currentStreak > _bestStreak) _bestStreak = _currentStreak;

                _currentBankroll += payout;
                if (statsHeader != null)
                {
                    statsHeader.SetTargetBankroll(_currentBankroll, animatePulse: true, isGain: true);
                    statsHeader.SetStreak(_currentStreak);
                }

                if (!_isFeverActive && _currentStreak >= config.FeverStreakThreshold)
                {
                    TriggerFeverMode();
                }

                if (isCrit)
                {
                    _totalCrits++;
                    if (audioSynth != null) audioSynth.PlayJackpot();
                    if (particleFx != null) particleFx.PlayJackpotCrit(transform.position);
                    if (screenShake != null) screenShake.AddTrauma(0.85f);
                    if (floatingText != null)
                    {
                        floatingText.Show("JACKPOT CRIT! +" + payout.ToString("N0"), Vector2.up * 80.0f, Color.yellow, 44.0f, 1.4f);
                    }

                    if (_currentMode == GameMode.Blitz20s)
                    {
                        _timeRemaining += config.CritTimeBonus;
                        if (timerView != null) timerView.PulseBonusTime();
                        if (floatingText != null)
                        {
                            floatingText.Show("+" + config.CritTimeBonus.ToString("F0") + "s TIME SURGE!", Vector2.up * 130.0f, Color.cyan, 36.0f, 1.2f);
                        }
                    }
                }
                else
                {
                    if (audioSynth != null) audioSynth.PlayWin(_currentStreak);
                    if (particleFx != null) particleFx.PlayWinSparkles(transform.position);
                    if (screenShake != null) screenShake.AddTrauma(0.25f);
                    if (floatingText != null)
                    {
                        floatingText.Show("+$" + payout.ToString("N0"), Vector2.up * 70.0f, Color.green, 38.0f, 1.0f);
                    }
                }
            }
            else
            {
                _currentStreak = 0;
                if (statsHeader != null)
                {
                    statsHeader.SetStreak(0);
                    statsHeader.SetTargetBankroll(_currentBankroll, animatePulse: true, isGain: false);
                }

                if (audioSynth != null) audioSynth.PlayLoss();
                if (screenShake != null) screenShake.AddTrauma(0.15f);
                if (floatingText != null)
                {
                    floatingText.Show("MISS", Vector2.up * 70.0f, new Color(1.0f, 0.3f, 0.4f), 34.0f, 0.8f);
                }
            }

            if (historyView != null)
            {
                historyView.AddRecord(roll, isWin, isCrit);
            }

            if (_totalRolls % 4 == 0)
            {
                SpawnRandomCritZone();
            }

            if (betControls != null)
            {
                betControls.SetRollInteractable(true);
            }

            if (_currentBankroll <= 0)
            {
                _isAutoRolling = false;
                if (betControls != null) betControls.SetAutoRollState(false);
                if (_currentMode == GameMode.Blitz20s)
                {
                    EndBlitzRun();
                }
            }
        }

        private void TriggerFeverMode()
        {
            _isFeverActive = true;
            _feverTimer = config.FeverDurationSeconds;

            if (audioSynth != null) audioSynth.PlayFever();
            if (particleFx != null) particleFx.SetFeverAuraActive(true);
            if (screenShake != null) screenShake.AddTrauma(0.5f);
            if (statsHeader != null) statsHeader.SetFeverProgress(1.0f, true);

            if (_currentMode == GameMode.Blitz20s)
            {
                _timeRemaining += config.StreakTimeBonus;
                if (timerView != null) timerView.PulseBonusTime();
            }

            if (floatingText != null)
            {
                floatingText.Show("OVERDRIVE FEVER!", Vector2.up * 110.0f, new Color(1.0f, 0.4f, 0.9f), 42.0f, 1.3f);
            }
        }

        private void SpawnRandomCritZone()
        {
            float width = config.CritZoneWidth;
            _critMin = UnityEngine.Random.Range(5.0f, 95.0f - width);
            _critMax = _critMin + width;

            if (sliderView != null)
            {
                sliderView.SetCritZone(_critMin, _critMax);
            }
        }

        private void EndBlitzRun()
        {
            long netProfit = _currentBankroll - _startingRoundBankroll;
            long savedHighScore = PlayerPrefs.GetInt(PrefsKeyHighScore, 0);
            bool isNewRecord = false;

            if (_currentBankroll > savedHighScore)
            {
                isNewRecord = true;
                PlayerPrefs.SetInt(PrefsKeyHighScore, (int)_currentBankroll);
                PlayerPrefs.Save();
            }

            if (gameOverModal != null)
            {
                gameOverModal.Show(
                    _currentBankroll,
                    netProfit,
                    _totalRolls,
                    _totalWins,
                    _bestStreak,
                    _totalCrits,
                    isNewRecord);
            }

            if (isNewRecord && particleFx != null)
            {
                particleFx.PlayConfettiBurst(transform.position);
            }
        }

        private void UpdateOddsDisplay()
        {
            float winChance = _isRollUnder ? _currentTarget : (100.0f - _currentTarget);
            float multiplier = config.PayoutFactor / Mathf.Max(0.5f, winChance);

            if (sliderView != null)
            {
                sliderView.SetMultiplierAndChance(multiplier, winChance);
            }
        }

        private void HandleTargetChanged(float target)
        {
            _currentTarget = target;
            UpdateOddsDisplay();
            if (audioSynth != null) audioSynth.PlayTick();
        }

        private void HandleToggleMode()
        {
            _isRollUnder = !_isRollUnder;
            if (sliderView != null) sliderView.SetMode(_isRollUnder);
            UpdateOddsDisplay();
            if (audioSynth != null) audioSynth.PlayClick();
        }

        private void HandleHalfBet()
        {
            _currentBet = (long)Mathf.Max(config.MinBet, _currentBet / 2);
            if (betControls != null) betControls.SetBetAmount(_currentBet);
            if (audioSynth != null) audioSynth.PlayBetAdjust();
        }

        private void HandleDoubleBet()
        {
            _currentBet = (long)Mathf.Clamp(_currentBet * 2, config.MinBet, Math.Min(config.MaxBet, _currentBankroll));
            if (betControls != null) betControls.SetBetAmount(_currentBet);
            if (audioSynth != null) audioSynth.PlayBetAdjust();
        }

        private void HandleMinBet()
        {
            _currentBet = config.MinBet;
            if (betControls != null) betControls.SetBetAmount(_currentBet);
            if (audioSynth != null) audioSynth.PlayBetAdjust();
        }

        private void HandleMaxBet()
        {
            _currentBet = (long)Mathf.Clamp(_currentBankroll, config.MinBet, config.MaxBet);
            if (betControls != null) betControls.SetBetAmount(_currentBet);
            if (audioSynth != null) audioSynth.PlayBetAdjust();
        }

        private void HandleRollClicked()
        {
            ExecuteRoll();
        }

        private void HandleAutoRollToggled()
        {
            _isAutoRolling = !_isAutoRolling;
            _autoRollTimer = 0.0f;
            if (betControls != null) betControls.SetAutoRollState(_isAutoRolling);
            if (audioSynth != null) audioSynth.PlayClick();
        }

        private void HandleModeToggle()
        {
            if (audioSynth != null) audioSynth.PlayClick();
            if (_currentMode == GameMode.Blitz20s)
            {
                StartEndlessMode();
            }
            else
            {
                StartNewBlitzRun();
            }
        }

        private void HandlePlayAgain()
        {
            StartNewBlitzRun();
            if (audioSynth != null) audioSynth.PlayClick();
        }

        private void HandleSwitchToEndless()
        {
            StartEndlessMode();
            if (audioSynth != null) audioSynth.PlayClick();
        }
    }
}

