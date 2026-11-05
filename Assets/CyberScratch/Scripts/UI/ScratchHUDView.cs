using System;
using System.Collections;
using CyberScratch.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.UI
{
    public class ScratchHUDView : MonoBehaviour
    {
        [Header("Top HUD Banner")]
        [SerializeField] private Button _menuButton;
        [SerializeField] private Text _balanceText;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _soundButton;
        [SerializeField] private Text _soundButtonText;
        [SerializeField] private Button _infoButton;
        [SerializeField] private Button _plusButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Text _timerText;
        [SerializeField] private CanvasGroup _settingsGroup;

        [Header("Wager Selector")]
        [SerializeField] private Button[] _wagerTierButtons;
        [SerializeField] private Text[] _wagerTierTexts;
        [SerializeField] private Color _wagerSelectedColor = new Color(0.12f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private Color _wagerUnselectedColor = new Color(0.18f, 0.22f, 0.30f, 0.8f);

        [Header("Bottom Action Controls")]
        [SerializeField] private Button _revealAllButton;
        [SerializeField] private Button _buyTicketButton;
        [SerializeField] private Text _buyTicketButtonText;
        [SerializeField] private Text _revealAllButtonText;
        [SerializeField] private Text _winCounterText;
        [SerializeField] private Image _revealAllImage;
        [SerializeField] private Image _buyTicketImage;
        [SerializeField] private Sprite _revealEnabledSprite;
        [SerializeField] private Sprite _buyEnabledSprite;
        [SerializeField] private Sprite _disabledButtonSprite;

        private float _currentDisplayedBalance;
        private Coroutine _balanceRollupRoutine;
        private Coroutine _balancePunchRoutine;
        private Action _onBuyTicketCallback;
        private Action _onRevealAllCallback;
        private Action<int> _onWagerSelectedCallback;
        private Action _onMenuCallback;
        private Action _onInfoCallback;
        private Action _onAddFundsCallback;

        public bool IsSettingsOpen => _settingsGroup != null && _settingsGroup.blocksRaycasts && _settingsGroup.alpha > 0.5f;

        public void Setup(
            float initialBalance,
            float[] wagerTiers,
            int defaultWagerIndex,
            Action onBuyTicket,
            Action onRevealAll,
            Action<int> onWagerSelected,
            Action onMenu = null,
            Action onInfo = null,
            Action onAddFunds = null)
        {
            _currentDisplayedBalance = initialBalance;
            UpdateBalanceInstant(initialBalance);
            SetTimer(90, false);

            _onBuyTicketCallback = onBuyTicket;
            _onRevealAllCallback = onRevealAll;
            _onWagerSelectedCallback = onWagerSelected;
            _onMenuCallback = onMenu;
            _onInfoCallback = onInfo;
            _onAddFundsCallback = onAddFunds;

            if (_buyTicketButton != null)
            {
                _buyTicketButton.onClick.RemoveAllListeners();
                _buyTicketButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    _onBuyTicketCallback?.Invoke();
                });
            }

            if (_revealAllButton != null)
            {
                _revealAllButton.onClick.RemoveAllListeners();
                _revealAllButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    _onRevealAllCallback?.Invoke();
                });
            }

            if (_menuButton != null)
            {
                _menuButton.onClick.RemoveAllListeners();
                _menuButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    SetSettingsOpen(false);
                    _onMenuCallback?.Invoke();
                });
            }

            if (_infoButton != null)
            {
                _infoButton.onClick.RemoveAllListeners();
                _infoButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    SetSettingsOpen(false);
                    _onInfoCallback?.Invoke();
                });
            }

            if (_soundButton != null)
            {
                _soundButton.onClick.RemoveAllListeners();
                _soundButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.ToggleMute();
                    UpdateSoundButtonState();
                });
                UpdateSoundButtonState();
            }

            if (_plusButton != null)
            {
                _plusButton.onClick.RemoveAllListeners();
                _plusButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    _onAddFundsCallback?.Invoke();
                });
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    SetSettingsOpen(!IsSettingsOpen);
                });
            }

            SetSettingsOpen(false);
            SetupWagerButtons(wagerTiers, defaultWagerIndex);
            UpdateWinDisplay(0f);
            SetStatus(string.Empty);
        }

        public void SetSettingsOpen(bool open)
        {
            if (_settingsGroup == null) return;
            _settingsGroup.alpha = open ? 1f : 0f;
            _settingsGroup.interactable = open;
            _settingsGroup.blocksRaycasts = open;
        }

        public void SetTimer(int seconds, bool urgent)
        {
            if (_timerText == null) return;
            _timerText.text = Mathf.Max(0, seconds) + "s";
            _timerText.color = urgent ? new Color(1f, 0.42f, 0.32f, 1f) : Color.white;
        }

        private void SetupWagerButtons(float[] tiers, int defaultIndex)
        {
            if (_wagerTierButtons == null || tiers == null) return;

            for (int i = 0; i < _wagerTierButtons.Length; i++)
            {
                if (i >= tiers.Length) break;

                int index = i;
                float tierValue = tiers[i];

                if (_wagerTierTexts != null && i < _wagerTierTexts.Length && _wagerTierTexts[i] != null)
                {
                    _wagerTierTexts[i].text = $"${tierValue:0}";
                }

                if (_wagerTierButtons[i] != null)
                {
                    _wagerTierButtons[i].onClick.RemoveAllListeners();
                    _wagerTierButtons[i].onClick.AddListener(() =>
                    {
                        CyberAudioService.Instance?.PlayButtonClick();
                        HighlightSelectedWager(index);
                        _onWagerSelectedCallback?.Invoke(index);
                    });
                }
            }

            HighlightSelectedWager(defaultIndex);
        }

        public void HighlightSelectedWager(int selectedIndex)
        {
            if (_wagerTierButtons == null) return;

            for (int i = 0; i < _wagerTierButtons.Length; i++)
            {
                if (_wagerTierButtons[i] == null) continue;

                Image img = _wagerTierButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = (i == selectedIndex) ? _wagerSelectedColor : _wagerUnselectedColor;
                }
            }
        }

        public void UpdateBuyTicketCost(float cost)
        {
            if (_buyTicketButtonText != null)
            {
                _buyTicketButtonText.text = cost >= 10f ? $"NEW CARD (${cost:0})" : $"NEW CARD (${cost:0.##})";
            }
        }

        public void UpdateBalanceInstant(float balance)
        {
            _currentDisplayedBalance = balance;
            if (_balanceText != null)
            {
                _balanceText.text = $"${balance:N2}";
            }
        }

        public void AnimateBalanceRollup(float targetBalance, float duration = 1.0f)
        {
            if (_balanceRollupRoutine != null)
            {
                StopCoroutine(_balanceRollupRoutine);
            }
            _balanceRollupRoutine = StartCoroutine(BalanceRollupRoutine(targetBalance, duration));
        }

        private IEnumerator BalanceRollupRoutine(float targetBalance, float duration)
        {
            float start = _currentDisplayedBalance;
            float elapsed = 0f;
            float tickTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                tickTimer += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                _currentDisplayedBalance = Mathf.Lerp(start, targetBalance, t);

                if (_balanceText != null)
                {
                    _balanceText.text = $"${_currentDisplayedBalance:N2}";
                }

                if (tickTimer >= 0.06f)
                {
                    tickTimer = 0f;
                    CyberAudioService.Instance?.PlayTallyTick();
                }

                yield return null;
            }

            UpdateBalanceInstant(targetBalance);
            _balanceRollupRoutine = null;
            PlayBalancePunch();
        }

        private void PlayBalancePunch()
        {
            if (_balanceText == null) return;
            if (_balancePunchRoutine != null) StopCoroutine(_balancePunchRoutine);
            _balancePunchRoutine = StartCoroutine(BalancePunchRoutine());
        }

        private IEnumerator BalancePunchRoutine()
        {
            Transform target = _balanceText.transform;
            float elapsed = 0f;
            const float duration = 0.28f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
            _balancePunchRoutine = null;
        }

        public void UpdateWinDisplay(float totalWin)
        {
            if (_winCounterText != null)
            {
                _winCounterText.text = totalWin > 0f ? $"TOTAL WIN: ${totalWin:N2}" : "TOTAL WIN: $0.00";
                _winCounterText.color = totalWin > 0f ? new Color(0.1f, 0.95f, 0.45f) : new Color(0.6f, 0.7f, 0.85f);
            }
        }

        public void SetStatus(string message)
        {
            if (_statusText != null)
            {
                _statusText.text = message;
            }
        }

        public void SetGameplayControlsInteractable(bool canRevealAll, bool canBuyTicket)
        {
            ApplyActionButton(_revealAllButton, _revealAllImage, _revealEnabledSprite, canRevealAll, _revealAllButtonText, new Color(0.22f, 0.09f, 0.02f, 1f));
            ApplyActionButton(_buyTicketButton, _buyTicketImage, _buyEnabledSprite, canBuyTicket, _buyTicketButtonText, Color.white);
        }

        private void ApplyActionButton(Button button, Image image, Sprite enabledSprite, bool enabled, Text label, Color enabledColor)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }

            if (image != null)
            {
                if (!enabled && _disabledButtonSprite != null)
                {
                    image.sprite = _disabledButtonSprite;
                }
                else if (enabledSprite != null)
                {
                    image.sprite = enabledSprite;
                }
                image.color = Color.white;
            }

            if (label != null)
            {
                label.color = enabled ? enabledColor : new Color(0.75f, 0.8f, 0.84f, 1f);
            }
        }

        public void SetWagerTiersInteractable(bool interactable)
        {
            if (_wagerTierButtons == null) return;
            for (int i = 0; i < _wagerTierButtons.Length; i++)
            {
                if (_wagerTierButtons[i] != null)
                {
                    _wagerTierButtons[i].interactable = interactable;
                }
            }
        }

        public void UpdateSoundButtonState()
        {
            bool muted = CyberAudioService.Instance != null && CyberAudioService.Instance.IsMuted;
            if (_soundButtonText != null)
            {
                _soundButtonText.text = muted ? "MUTE [OFF]" : "SOUND [ON]";
                _soundButtonText.color = muted ? new Color(0.85f, 0.85f, 0.85f, 0.75f) : Color.white;
            }
        }
    }
}
