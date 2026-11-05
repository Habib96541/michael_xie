using System;
using CyberWhack.Audio;
using CyberWhack.VFX;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CyberWhack.UI
{
    public class CyberHUD : MonoBehaviour
    {
        [Header("Main Menu Terminal UI")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private RectTransform heroTitleTransform;
        [SerializeField] private TextMeshProUGUI menuHighScoreText;
        [SerializeField] private TMP_InputField menuSeedInput;
        [SerializeField] private Button menuStartButton;
        [SerializeField] private Button menuIntelButton;
        [SerializeField] private Button menuRandomSeedButton;
        [SerializeField] private Button audioToggleButton;
        [SerializeField] private Image audioToggleIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("Target Intel Briefing Modal")]
        [SerializeField] private GameObject targetIntelModal;
        [SerializeField] private Button closeIntelButton;

        [Header("In-Game Top Header")]
        [SerializeField] private GameObject inGameHeader;
        [SerializeField] private Button abortButton;
        [SerializeField] private Button inGameAudioToggleButton;
        [SerializeField] private Image inGameAudioToggleIcon;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private Image comboStreakFill;
        [SerializeField] private TextMeshProUGUI comboStreakCountText;
        [SerializeField] private RectTransform multiplierBadgeTransform;
        [SerializeField] private Image comboGlow;
        [SerializeField] private Image multiplierGlow;

        [Header("Tournament 60s Countdown Gauge")]
        [SerializeField] private Image timerCircleGauge;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI inGameSeedText;

        [Header("Ready / Countdown Overlay")]
        [SerializeField] private GameObject readyOverlay;
        [SerializeField] private TextMeshProUGUI readyCountdownText;
        [SerializeField] private RectTransform countdownBurstTransform;

        [Header("Combat Evaluation / Game Over Modal")]
        [SerializeField] private GameObject gameOverModal;
        [SerializeField] private TextMeshProUGUI rankBadgeText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI newRecordBadge;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private TextMeshProUGUI maxStreakText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI destroyedStatsText;
        [SerializeField] private TextMeshProUGUI finalSeedText;
        [SerializeField] private Button restartSameSeedButton;
        [SerializeField] private Button restartNewSeedButton;
        [SerializeField] private Button returnToMenuButton;

        [Header("Pause")]
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private GameObject modalScrim;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button pauseMenuButton;
        [SerializeField] private Button shakeToggleButton;
        [SerializeField] private TextMeshProUGUI shakeToggleLabel;

        // Public Events
        public event Action OnStartGameClicked;
        public event Action OnPauseClicked;
        public event Action OnResumeClicked;
        public event Action OnRestartSameSeedClicked;
        public event Action OnRestartNewSeedClicked;
        public event Action OnReturnToMenuClicked;
        public event Action OnSeedRandomizeClicked;
        public event Action<int> OnSeedChanged;

        private static readonly string[] CountdownStrings = { "GO!", "1", "2", "3", "4", "5" };
        private static readonly string[] SecondsFormatted = GenerateSecondsCache();

        private static string[] GenerateSecondsCache()
        {
            var arr = new string[100];
            for (int i = 0; i < 100; i++)
            {
                arr[i] = $"{i / 60}:{i % 60:00}";
            }
            return arr;
        }

        // Cached runtime state
        private int _lastDisplayedScore = -1;
        private int _lastDisplayedMultiplier = -1;
        private int _lastDisplayedSeconds = -1;
        private int _lastDisplayedCountdown = -1;

        // Juice Animation Parameters (Zero-allocation Update Lerping)
        private float _targetComboFill;
        private float _scorePunchScale = 1.0f;
        private float _multiplierPunchScale = 1.0f;
        private float _countdownPunchScale = 1.0f;
        private float _resultsScale = 1f;

        // Colors tuned for the cream plaques, not the old night panels.
        private static readonly Color32 NormalTimerColor = new Color32(58, 30, 12, 255);
        private static readonly Color32 WarningTimerColor = new Color32(184, 84, 12, 255);
        private static readonly Color32 DangerTimerColor = new Color32(176, 32, 28, 255);
        private static readonly Color32 CountdownInk = new Color32(58, 30, 12, 255);
        private static readonly Color32 CountdownGo = new Color32(176, 36, 24, 255);

        private void Awake()
        {
            RefreshBindings();
            UpdateAudioIcon(CyberAudioManager.Instance != null && CyberAudioManager.Instance.IsMuted);

            if (CyberAudioManager.Instance != null)
            {
                CyberAudioManager.Instance.OnMuteStateChanged += UpdateAudioIcon;
            }
        }

        public void RefreshBindings()
        {
            if (menuStartButton != null)
            {
                menuStartButton.onClick.RemoveAllListeners();
                menuStartButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnStartGameClicked?.Invoke();
                });
            }

            if (menuIntelButton != null)
            {
                menuIntelButton.onClick.RemoveAllListeners();
                menuIntelButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    ShowTargetIntel();
                });
            }

            if (closeIntelButton != null)
            {
                closeIntelButton.onClick.RemoveAllListeners();
                closeIntelButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    HideTargetIntel();
                });
            }

            if (menuRandomSeedButton != null)
            {
                menuRandomSeedButton.onClick.RemoveAllListeners();
                menuRandomSeedButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnSeedRandomizeClicked?.Invoke();
                });
            }

            if (audioToggleButton != null)
            {
                audioToggleButton.onClick.RemoveAllListeners();
                audioToggleButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.ToggleMute();
                });
            }

            if (inGameAudioToggleButton != null)
            {
                inGameAudioToggleButton.onClick.RemoveAllListeners();
                inGameAudioToggleButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.ToggleMute();
                });
            }

            if (abortButton != null)
            {
                abortButton.onClick.RemoveAllListeners();
                abortButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnPauseClicked?.Invoke();
                });
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
                resumeButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnResumeClicked?.Invoke();
                });
            }

            if (pauseMenuButton != null)
            {
                pauseMenuButton.onClick.RemoveAllListeners();
                pauseMenuButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnReturnToMenuClicked?.Invoke();
                });
            }

            if (shakeToggleButton != null)
            {
                shakeToggleButton.onClick.RemoveAllListeners();
                shakeToggleButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    bool enabled = ArcadeFeel.ToggleShake();
                    if (shakeToggleLabel != null) shakeToggleLabel.text = enabled ? "Shakes on" : "Shakes off";
                });
            }

            if (restartSameSeedButton != null)
            {
                restartSameSeedButton.onClick.RemoveAllListeners();
                restartSameSeedButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnRestartSameSeedClicked?.Invoke();
                });
            }

            if (restartNewSeedButton != null)
            {
                restartNewSeedButton.onClick.RemoveAllListeners();
                restartNewSeedButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnRestartNewSeedClicked?.Invoke();
                });
            }

            if (returnToMenuButton != null)
            {
                returnToMenuButton.onClick.RemoveAllListeners();
                returnToMenuButton.onClick.AddListener(() =>
                {
                    CyberAudioManager.Instance?.PlayUiClick();
                    OnReturnToMenuClicked?.Invoke();
                });
            }

            if (menuSeedInput != null)
            {
                menuSeedInput.onEndEdit.RemoveListener(OnCustomSeedInputEntered);
                menuSeedInput.onEndEdit.AddListener(OnCustomSeedInputEntered);
            }
        }

        private void OnDestroy()
        {
            if (menuStartButton != null) menuStartButton.onClick.RemoveAllListeners();
            if (menuIntelButton != null) menuIntelButton.onClick.RemoveAllListeners();
            if (closeIntelButton != null) closeIntelButton.onClick.RemoveAllListeners();
            if (menuRandomSeedButton != null) menuRandomSeedButton.onClick.RemoveAllListeners();
            if (audioToggleButton != null) audioToggleButton.onClick.RemoveAllListeners();
            if (inGameAudioToggleButton != null) inGameAudioToggleButton.onClick.RemoveAllListeners();
            if (abortButton != null) abortButton.onClick.RemoveAllListeners();
            if (restartSameSeedButton != null) restartSameSeedButton.onClick.RemoveAllListeners();
            if (restartNewSeedButton != null) restartNewSeedButton.onClick.RemoveAllListeners();
            if (returnToMenuButton != null) returnToMenuButton.onClick.RemoveAllListeners();
            if (menuSeedInput != null) menuSeedInput.onEndEdit.RemoveAllListeners();

            if (CyberAudioManager.Instance != null)
            {
                CyberAudioManager.Instance.OnMuteStateChanged -= UpdateAudioIcon;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 1. Smoothly Lerp Combo Streak Fill Bar (Zero Alloc)
            if (comboStreakFill != null)
            {
                comboStreakFill.fillAmount = Mathf.MoveTowards(comboStreakFill.fillAmount, _targetComboFill, dt * 6f);
            }

            // Score, combo badge, and countdown settle with an ease instead of a linear slide.
            _scorePunchScale = Damp(_scorePunchScale, 1f, dt, 14f);
            if (scoreText != null)
            {
                scoreText.transform.localScale = new Vector3(_scorePunchScale, _scorePunchScale, 1f);
            }

            _multiplierPunchScale = Damp(_multiplierPunchScale, 1f, dt, 12f);
            if (multiplierBadgeTransform != null)
            {
                multiplierBadgeTransform.localScale = new Vector3(_multiplierPunchScale, _multiplierPunchScale, 1f);
            }

            _countdownPunchScale = Damp(_countdownPunchScale, 1f, dt, 10f);
            if (readyCountdownText != null)
            {
                readyCountdownText.transform.localScale = new Vector3(_countdownPunchScale, _countdownPunchScale, 1f);
            }

            _resultsScale = Damp(_resultsScale, 1f, dt, 8f);
            if (finalScoreText != null && gameOverModal != null && gameOverModal.activeSelf)
            {
                finalScoreText.transform.localScale = new Vector3(_resultsScale, _resultsScale, 1f);
            }

            if (readyOverlay != null && readyOverlay.activeSelf && countdownBurstTransform != null)
            {
                countdownBurstTransform.Rotate(0f, 0f, -40f * dt);
            }

            if (timerText != null)
            {
                bool urgent = inGameHeader != null && inGameHeader.activeSelf && _lastDisplayedSeconds > 0 && _lastDisplayedSeconds <= 10;
                if (urgent)
                {
                    float rate = Mathf.Lerp(4.5f, 9f, Mathf.InverseLerp(10f, 1f, _lastDisplayedSeconds));
                    float beat = 1f + Mathf.Pow(Mathf.Abs(Mathf.Sin(Time.unscaledTime * rate * 1.6f)), 8f) * 0.16f;
                    timerText.transform.localScale = new Vector3(beat, beat, 1f);
                }
                else if (_lastDisplayedSeconds > 10 || inGameHeader == null || !inGameHeader.activeSelf)
                {
                    timerText.transform.localScale = Vector3.one;
                }
            }

            if (comboGlow != null)
            {
                float wave = (Mathf.Sin(Time.unscaledTime * 3.4f) + 1f) * 0.5f;
                float targetAlpha = _lastDisplayedMultiplier >= 2 ? 0.10f + wave * 0.18f : 0.03f + wave * 0.04f;
                Color glowColor = comboGlow.color;
                glowColor.a = Damp(glowColor.a, targetAlpha, dt, 8f);
                comboGlow.color = glowColor;
            }

            if (multiplierGlow != null)
            {
                multiplierGlow.transform.Rotate(0f, 0f, -26f * dt);
                float wave = (Mathf.Sin(Time.unscaledTime * 4.2f) + 1f) * 0.5f;
                Color haloColor = multiplierGlow.color;
                haloColor.a = _lastDisplayedMultiplier >= 2 ? 0.10f + wave * 0.10f : 0.04f;
                multiplierGlow.color = haloColor;
            }

            if (mainMenuPanel != null && mainMenuPanel.activeSelf && heroTitleTransform != null)
            {
                float titleScale = 1f + Mathf.Sin(Time.unscaledTime * 2.1f) * 0.018f;
                heroTitleTransform.localScale = new Vector3(titleScale, titleScale, 1f);
                heroTitleTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.35f) * 0.8f);
            }
        }

        private static float Damp(float current, float target, float dt, float speed)
        {
            return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * dt));
        }

        private void OnCustomSeedInputEntered(string text)
        {
            if (int.TryParse(text, out int parsedSeed))
            {
                OnSeedChanged?.Invoke(parsedSeed);
            }
        }

        public void ConfigureAudioSprites(Sprite onSprite, Sprite offSprite)
        {
            soundOnSprite = onSprite;
            soundOffSprite = offSprite;
            UpdateAudioIcon(CyberAudioManager.Instance != null && CyberAudioManager.Instance.IsMuted);
        }

        private void UpdateAudioIcon(bool isMuted)
        {
            Sprite targetSprite = isMuted ? soundOffSprite : soundOnSprite;
            if (audioToggleIcon != null) audioToggleIcon.sprite = targetSprite;
            if (inGameAudioToggleIcon != null) inGameAudioToggleIcon.sprite = targetSprite;
        }

        public void ShowMainMenu(int highScore, int currentSeed)
        {
            _lastDisplayedScore = -1;
            _lastDisplayedMultiplier = -1;
            _lastDisplayedSeconds = -1;
            _lastDisplayedCountdown = -1;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
            if (targetIntelModal != null) targetIntelModal.SetActive(false);
            if (inGameHeader != null) inGameHeader.SetActive(false);
            if (readyOverlay != null) readyOverlay.SetActive(false);
            if (gameOverModal != null) gameOverModal.SetActive(false);

            if (menuHighScoreText != null)
            {
                menuHighScoreText.enableAutoSizing = false;
                menuHighScoreText.fontSize = 52f;
                menuHighScoreText.fontStyle = FontStyles.Bold;
                menuHighScoreText.alignment = TextAlignmentOptions.Center;
                menuHighScoreText.verticalAlignment = VerticalAlignmentOptions.Middle;
                menuHighScoreText.textWrappingMode = TextWrappingModes.Normal;
                menuHighScoreText.overflowMode = TextOverflowModes.Truncate;
                menuHighScoreText.margin = new Vector4(4f, 2f, 4f, 2f);
                menuHighScoreText.text = $"<size=72%><color=#7a4018>PERSONAL BEST</color></size>\n<size=112%><b><color=#3a1e0c>{highScore:N0}</color></b></size>";
            }

            HidePause();
            SetScrim(false);
            Focus(menuStartButton);
            SetMatchSeed(currentSeed);
        }

        public void ShowTargetIntel()
        {
            if (targetIntelModal != null) targetIntelModal.SetActive(true);
            PlaceScrimBehind(targetIntelModal);
            Focus(closeIntelButton);
        }

        public void HideTargetIntel()
        {
            if (targetIntelModal != null) targetIntelModal.SetActive(false);
            SetScrim(false);
            Focus(menuStartButton);
        }

        public void TransitionToGame()
        {
            _lastDisplayedScore = -1;
            _lastDisplayedMultiplier = -1;
            _lastDisplayedSeconds = -1;
            _lastDisplayedCountdown = -1;
            _targetComboFill = 0f;
            if (comboStreakFill != null) comboStreakFill.fillAmount = 0f;

            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (targetIntelModal != null) targetIntelModal.SetActive(false);
            if (inGameHeader != null) inGameHeader.SetActive(true);
            if (gameOverModal != null) gameOverModal.SetActive(false);
            HidePause();
            SetScrim(false);

            if (timerText != null)
            {
                timerText.transform.localScale = Vector3.one;
                timerText.color = NormalTimerColor;
            }
        }

        public void SetMatchSeed(int seed)
        {
            if (menuSeedInput != null)
            {
                menuSeedInput.text = seed.ToString();
            }
            if (inGameSeedText != null)
            {
                inGameSeedText.text = $"Code  {seed:D6}";
                inGameSeedText.color = new Color32(58, 30, 12, 255);
            }
            if (finalSeedText != null)
            {
                finalSeedText.text = $"Code  {seed:D6}";
            }
        }

        public void UpdateScore(int score)
        {
            if (_lastDisplayedScore == score) return;

            if (_lastDisplayedScore != -1 && score > _lastDisplayedScore)
            {
                _scorePunchScale = 1.08f;
            }
            _lastDisplayedScore = score;

            if (scoreText != null)
            {
                scoreText.text = score.ToString("N0");
            }
        }

        public void UpdateMultiplier(int multiplier, int streak, int nextTierTarget)
        {
            if (_lastDisplayedMultiplier != multiplier)
            {
                if (_lastDisplayedMultiplier != -1 && multiplier > _lastDisplayedMultiplier)
                {
                    _multiplierPunchScale = 1.12f;
                }
                _lastDisplayedMultiplier = multiplier;

                if (multiplierText != null)
                {
                    multiplierText.text = multiplier > 1 ? $"{multiplier}x" : "1x";
                    multiplierText.color = multiplier switch
                    {
                        4 => new Color32(120, 48, 4, 255),
                        3 => new Color32(140, 52, 8, 255),
                        2 => new Color32(22, 96, 40, 255),
                        _ => new Color32(58, 30, 12, 255)
                    };
                }
            }

            if (nextTierTarget > 0)
            {
                _targetComboFill = Mathf.Clamp01((float)streak / nextTierTarget);
            }
            else
            {
                _targetComboFill = 0f;
            }

            if (comboStreakCountText != null)
            {
                if (streak > 0)
                {
                    comboStreakCountText.text = $"Streak  {streak}";
                }
                else
                {
                    comboStreakCountText.text = "Combo";
                }
            }
        }

        public void UpdateTimer(float remainingSeconds, float totalDuration)
        {
            float clamped = Mathf.Max(0f, remainingSeconds);
            if (timerCircleGauge != null && totalDuration > 0f)
            {
                timerCircleGauge.fillAmount = Mathf.Clamp01(clamped / totalDuration);
            }

            int sec = Mathf.CeilToInt(clamped);
            if (sec != _lastDisplayedSeconds && timerText != null)
            {
                _lastDisplayedSeconds = sec;
                if (sec >= 0 && sec < SecondsFormatted.Length)
                {
                    timerText.text = SecondsFormatted[sec];
                }
                else
                {
                    timerText.text = "00:00";
                }

                if (sec <= 5)
                {
                    timerText.color = DangerTimerColor;
                }
                else if (sec <= 15)
                {
                    timerText.color = WarningTimerColor;
                }
                else
                {
                    timerText.color = NormalTimerColor;
                    timerText.transform.localScale = Vector3.one;
                }
            }
        }

        public void ShowReadyCountdown(int secondsRemaining)
        {
            if (readyOverlay != null && !readyOverlay.activeSelf) readyOverlay.SetActive(true);
            if (secondsRemaining == _lastDisplayedCountdown) return;
            _lastDisplayedCountdown = secondsRemaining;

            _countdownPunchScale = (secondsRemaining == 0) ? 1.55f : 1.28f;

            if (readyCountdownText != null)
            {
                readyCountdownText.color = secondsRemaining == 0 ? CountdownGo : CountdownInk;
                if (secondsRemaining >= 0 && secondsRemaining < CountdownStrings.Length)
                {
                    readyCountdownText.text = CountdownStrings[secondsRemaining];
                }
                else
                {
                    readyCountdownText.text = secondsRemaining > 0 ? secondsRemaining.ToString() : "GO!";
                }
            }
        }

        public void HideReadyCountdown()
        {
            _lastDisplayedCountdown = -1;
            if (readyOverlay != null) readyOverlay.SetActive(false);

            // Golden "GO!" pop right where the countdown burst was.
            if (EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.PlayImpact(new Vector2(0f, 40f), ImpactFlavor.Gold);
            }
        }

        public void ShowGameOver(int finalScore, int highScore, bool isNewRecord, int maxStreak, float accuracyPercent, int destroyed, int empHits, int seed)
        {
            if (gameOverModal != null) gameOverModal.SetActive(true);
            if (inGameHeader != null) inGameHeader.SetActive(false);
            HidePause();
            PlaceScrimBehind(gameOverModal);
            _resultsScale = 1.08f;

            if (isNewRecord && EffectPoolManager.Instance != null)
            {
                EffectPoolManager.Instance.PlayCelebration();
            }

            if (finalScoreText != null) finalScoreText.text = finalScore.ToString("N0");
            if (highScoreText != null) highScoreText.text = $"Best  {highScore:N0}";

            if (newRecordBadge != null)
            {
                newRecordBadge.gameObject.SetActive(isNewRecord);
                newRecordBadge.color = new Color32(176, 104, 8, 255);
            }

            // Calculate Operational Rank Badge
            if (rankBadgeText != null)
            {
                if (finalScore >= 10000 && accuracyPercent >= 85f)
                {
                    rankBadgeText.text = "Mole legend";
                    rankBadgeText.color = new Color32(176, 104, 8, 255);
                }
                else if (finalScore >= 6000)
                {
                    rankBadgeText.text = "Burrow champ";
                    rankBadgeText.color = new Color32(184, 84, 12, 255);
                }
                else if (finalScore >= 3000)
                {
                    rankBadgeText.text = "Quick paws";
                    rankBadgeText.color = new Color32(28, 122, 52, 255);
                }
                else
                {
                    rankBadgeText.text = "Dirt rookie";
                    rankBadgeText.color = new Color32(90, 52, 24, 255);
                }
            }

            if (maxStreakText != null) maxStreakText.text = $"Streak  {maxStreak}";
            if (accuracyText != null) accuracyText.text = $"Accuracy  {accuracyPercent:F0}%";
            if (destroyedStatsText != null) destroyedStatsText.text = $"Whacks  {destroyed}    Bombs  {empHits}";
            if (finalSeedText != null) finalSeedText.text = $"Code  {seed:D6}";
            Focus(restartSameSeedButton);
        }

        public void ShowPause()
        {
            if (pauseOverlay != null) pauseOverlay.SetActive(true);
            if (shakeToggleLabel != null) shakeToggleLabel.text = ArcadeFeel.ShakeEnabled ? "Shakes on" : "Shakes off";
            PlaceScrimBehind(pauseOverlay);
            Focus(resumeButton);
        }

        public void HidePause()
        {
            if (pauseOverlay != null) pauseOverlay.SetActive(false);
            if (gameOverModal == null || !gameOverModal.activeSelf)
            {
                SetScrim(false);
            }
        }

        private void PlaceScrimBehind(GameObject front)
        {
            if (modalScrim == null || front == null) return;
            modalScrim.SetActive(true);
            int frontIndex = front.transform.GetSiblingIndex();
            modalScrim.transform.SetSiblingIndex(Mathf.Max(0, frontIndex));
            front.transform.SetAsLastSibling();
        }

        private void SetScrim(bool visible)
        {
            if (modalScrim != null) modalScrim.SetActive(visible);
        }

        private static void Focus(Button button)
        {
            if (button == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        public void HideGameOver()
        {
            if (gameOverModal != null) gameOverModal.SetActive(false);
        }
    }
}
