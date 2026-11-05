using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.UI
{
    public sealed class DiceStatsHeaderView : MonoBehaviour
    {
        [Header("Bankroll Display")]
        [SerializeField] private TextMeshProUGUI bankrollText;
        [SerializeField] private RectTransform bankrollContainer;

        [Header("Streak & Fever")]
        [SerializeField] private TextMeshProUGUI streakText;
        [SerializeField] private RectTransform streakContainer;
        [SerializeField] private Image feverMeterFill;
        [SerializeField] private GameObject feverActiveBanner;

        [Header("Mode & Controls")]
        [SerializeField] private TextMeshProUGUI modeBadgeText;
        [SerializeField] private Button buttonModeSelect;

        [Header("Colors")]
        [SerializeField] private Color normalBankrollColor = Color.white;
        [SerializeField] private Color winPulseColor = new Color(0.0f, 1.0f, 0.6f, 1.0f);
        [SerializeField] private Color lossPulseColor = new Color(1.0f, 0.3f, 0.4f, 1.0f);

        public event Action OnModeSelectClicked;

        private long _targetBankroll;
        private double _displayedBankroll;
        private float _pulseTimer;
        private Color _currentBankrollColor;

        private void Awake()
        {
            if (buttonModeSelect != null)
            {
                buttonModeSelect.onClick.AddListener(() => OnModeSelectClicked?.Invoke());
            }

            _currentBankrollColor = normalBankrollColor;
            if (feverActiveBanner != null) feverActiveBanner.SetActive(false);
        }

        private void OnDestroy()
        {
            if (buttonModeSelect != null)
            {
                buttonModeSelect.onClick.RemoveAllListeners();
            }
        }

        public void Initialize(long startingBankroll, string modeName)
        {
            _targetBankroll = startingBankroll;
            _displayedBankroll = startingBankroll;
            UpdateBankrollText((long)_displayedBankroll);

            if (modeBadgeText != null)
            {
                modeBadgeText.text = modeName;
            }

            SetStreak(0);
            SetFeverProgress(0.0f, false);
        }

        public void SetTargetBankroll(long newBankroll, bool animatePulse = true, bool isGain = true)
        {
            _targetBankroll = newBankroll;

            if (animatePulse && bankrollContainer != null)
            {
                bankrollContainer.localScale = Vector3.one * 1.2f;
                _currentBankrollColor = isGain ? winPulseColor : lossPulseColor;
                _pulseTimer = 0.35f;
            }
        }

        public void SetStreak(int streak)
        {
            if (streakText != null)
            {
                if (streak > 0)
                {
                    streakText.text = "STREAK x" + streak.ToString();
                    streakText.gameObject.SetActive(true);
                }
                else
                {
                    streakText.gameObject.SetActive(false);
                }
            }

            if (streak > 0 && streakContainer != null)
            {
                streakContainer.localScale = Vector3.one * 1.25f;
            }
        }

        public void SetFeverProgress(float progress01, bool isFeverActive)
        {
            if (feverMeterFill != null)
            {
                feverMeterFill.fillAmount = Mathf.Clamp01(progress01);
            }

            if (feverActiveBanner != null && feverActiveBanner.activeSelf != isFeverActive)
            {
                feverActiveBanner.SetActive(isFeverActive);
            }
        }

        public void SetModeName(string modeName)
        {
            if (modeBadgeText != null)
            {
                modeBadgeText.text = modeName;
            }
        }

        private void Update()
        {
            // Smooth bankroll lerp without GC allocations
            if (Math.Abs(_displayedBankroll - _targetBankroll) > 0.5)
            {
                _displayedBankroll = Mathf.Lerp((float)_displayedBankroll, (float)_targetBankroll, Time.deltaTime * 12.0f);
                UpdateBankrollText((long)Math.Round(_displayedBankroll));
            }

            // Scale return
            if (bankrollContainer != null && bankrollContainer.localScale.x > 1.0f)
            {
                bankrollContainer.localScale = Vector3.Lerp(bankrollContainer.localScale, Vector3.one, Time.deltaTime * 8.0f);
            }

            if (streakContainer != null && streakContainer.localScale.x > 1.0f)
            {
                streakContainer.localScale = Vector3.Lerp(streakContainer.localScale, Vector3.one, Time.deltaTime * 8.0f);
            }

            // Color return
            if (_pulseTimer > 0.0f)
            {
                _pulseTimer -= Time.deltaTime;
                if (_pulseTimer <= 0.0f)
                {
                    _currentBankrollColor = normalBankrollColor;
                    if (bankrollText != null) bankrollText.color = normalBankrollColor;
                }
            }
        }

        private void UpdateBankrollText(long value)
        {
            if (bankrollText != null)
            {
                bankrollText.text = "$" + value.ToString("N0");
                bankrollText.color = _currentBankrollColor;
            }
        }
    }
}
