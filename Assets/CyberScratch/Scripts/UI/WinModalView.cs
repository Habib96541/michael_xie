using System;
using System.Collections;
using CyberScratch.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.UI
{
    public class WinModalView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _dialogPanel;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _rowsWonText;
        [SerializeField] private Text _totalWinAmountText;
        [SerializeField] private Button _collectButton;
        [SerializeField] private ParticleSystem _winParticles;

        private Action _onCollectCallback;
        private Coroutine _showRoutine;
        private Coroutine _rollupRoutine;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            HideInstant();

            if (_collectButton != null)
            {
                _collectButton.onClick.RemoveAllListeners();
                _collectButton.onClick.AddListener(OnCollectClicked);
            }
        }

        public void Show(float totalWin, int winningRowsCount, Action onCollect)
        {
            _onCollectCallback = onCollect;

            if (_showRoutine != null) StopCoroutine(_showRoutine);
            _showRoutine = StartCoroutine(ShowRoutine(totalWin, winningRowsCount));
        }

        private IEnumerator ShowRoutine(float totalWin, int winningRowsCount)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            if (_dialogPanel != null)
            {
                _dialogPanel.localScale = Vector3.one * 0.75f;
            }

            if (_titleText != null)
            {
                _titleText.text = "CYBER VICTORY!";
            }

            if (_rowsWonText != null)
            {
                _rowsWonText.text = winningRowsCount == 1 ? "1 WINNING ROW!" : $"{winningRowsCount} WINNING ROWS!";
            }

            if (_winParticles != null)
            {
                _winParticles.Play();
            }

            // Animate dialog panel pop-in
            float elapsed = 0f;
            float duration = 0.35f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
                if (_dialogPanel != null)
                {
                    float scale = 0.75f + Mathf.Sin(t * Mathf.PI * 0.5f) * 0.25f + Mathf.Sin(t * Mathf.PI) * 0.08f;
                    _dialogPanel.localScale = Vector3.one * scale;
                }
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            if (_dialogPanel != null) _dialogPanel.localScale = Vector3.one;

            // Start win amount rollup animation
            if (_rollupRoutine != null) StopCoroutine(_rollupRoutine);
            _rollupRoutine = StartCoroutine(RollupRoutine(totalWin));
        }

        private IEnumerator RollupRoutine(float targetWin)
        {
            float elapsed = 0f;
            float duration = 1.0f;
            float tickTimer = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                tickTimer += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float current = Mathf.Lerp(0f, targetWin, t);

                if (_totalWinAmountText != null)
                {
                    _totalWinAmountText.text = $"${current:N2}";
                }

                if (tickTimer >= 0.05f)
                {
                    tickTimer = 0f;
                    CyberAudioService.Instance?.PlayTallyTick();
                }

                yield return null;
            }

            if (_totalWinAmountText != null)
            {
                _totalWinAmountText.text = $"${targetWin:N2}";
            }
            CyberAudioService.Instance?.PlayRowWinStinger();
            _rollupRoutine = null;
        }

        private void OnCollectClicked()
        {
            CyberAudioService.Instance?.PlayButtonClick();
            StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            _canvasGroup.interactable = false;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - (elapsed / duration);
                _canvasGroup.alpha = t;
                if (_dialogPanel != null)
                {
                    _dialogPanel.localScale = Vector3.one * (0.8f + t * 0.2f);
                }
                yield return null;
            }

            HideInstant();
            _onCollectCallback?.Invoke();
        }

        public void HideInstant()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }
    }
}
