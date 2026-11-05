using System;
using System.Collections;
using CyberScratch.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.UI
{
    public class HowToPlayModalView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _dialogPanel;
        [SerializeField] private Button _closeButton;

        private bool _isVisible;
        public bool IsVisible => _isVisible;

        private void Awake()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            HideInstant();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        public void Show()
        {
            _isVisible = true;
            if (!Application.isPlaying)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 1f;
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                }
                if (_dialogPanel != null)
                {
                    _dialogPanel.localScale = Vector3.one;
                }
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

        public void HideInstant()
        {
            _isVisible = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_dialogPanel != null)
            {
                _dialogPanel.localScale = Vector3.one * 0.85f;
            }
        }

        private void OnCloseClicked()
        {
            CyberAudioService.Instance?.PlayButtonClick();
            Hide();
        }

        private IEnumerator FadeRoutine(float targetAlpha, bool interactable)
        {
            if (interactable)
            {
                _canvasGroup.blocksRaycasts = true;
            }

            float start = _canvasGroup.alpha;
            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
                if (_dialogPanel != null)
                {
                    float scale = interactable ? Mathf.Lerp(0.85f, 1f, t) : Mathf.Lerp(1f, 0.85f, t);
                    _dialogPanel.localScale = Vector3.one * scale;
                }
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
            _isVisible = interactable;
        }
    }
}
