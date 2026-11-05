using System;
using System.Collections;
using CyberScratch.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.UI
{
    public class MainMenuView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Text _balanceText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _howToPlayButton;
        [SerializeField] private Button _soundButton;
        [SerializeField] private Text _soundButtonText;
        [SerializeField] private Button _resetBalanceButton;

        private Action _onPlayCallback;
        private Action _onHowToPlayCallback;
        private Action _onResetBalanceCallback;
        private bool _isVisible = true;

        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Setup(
            float initialBalance,
            Action onPlay,
            Action onHowToPlay,
            Action onResetBalance)
        {
            _onPlayCallback = onPlay;
            _onHowToPlayCallback = onHowToPlay;
            _onResetBalanceCallback = onResetBalance;

            UpdateBalance(initialBalance);
            UpdateSoundState();

            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    _onPlayCallback?.Invoke();
                });
            }

            if (_howToPlayButton != null)
            {
                _howToPlayButton.onClick.RemoveAllListeners();
                _howToPlayButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    _onHowToPlayCallback?.Invoke();
                });
            }

            if (_soundButton != null)
            {
                _soundButton.onClick.RemoveAllListeners();
                _soundButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.ToggleMute();
                    UpdateSoundState();
                });
            }

            if (_resetBalanceButton != null)
            {
                _resetBalanceButton.onClick.RemoveAllListeners();
                _resetBalanceButton.onClick.AddListener(() =>
                {
                    CyberAudioService.Instance?.PlayButtonClick();
                    _onResetBalanceCallback?.Invoke();
                });
            }
        }

        public void UpdateBalance(float balance)
        {
            if (_balanceText != null)
            {
                _balanceText.text = $"BALANCE: ${balance:N2}";
            }
        }

        public void UpdateSoundState()
        {
            bool isMuted = CyberAudioService.Instance != null && CyberAudioService.Instance.IsMuted;
            if (_soundButtonText != null)
            {
                _soundButtonText.text = isMuted ? "SOUND: [OFF]" : "SOUND: [ON]";
                _soundButtonText.color = isMuted ? new Color(0.85f, 0.85f, 0.85f, 0.75f) : Color.white;
            }
        }

        public void Show()
        {
            _isVisible = true;
            if (!Application.isPlaying)
            {
                ShowInstant();
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(1f, true));
        }

        public void Hide()
        {
            _isVisible = false;
            if (!Application.isPlaying)
            {
                HideInstant();
                return;
            }
            StopAllCoroutines();
            StartCoroutine(FadeRoutine(0f, false));
        }

        public void ShowInstant()
        {
            _isVisible = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        public void HideInstant()
        {
            _isVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool interactable)
        {
            if (interactable)
            {
                _canvasGroup.blocksRaycasts = true;
            }

            float start = _canvasGroup.alpha;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
            _isVisible = interactable;
        }
    }
}
