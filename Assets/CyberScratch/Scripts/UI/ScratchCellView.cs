using System.Collections;
using CyberScratch.Data;
using UnityEngine;
using UnityEngine.UI;

namespace CyberScratch.UI
{
    public class ScratchCellView : MonoBehaviour
    {
        [SerializeField] private Image _symbolIcon;
        [SerializeField] private Image _frameImage;
        [SerializeField] private Image _targetHalo;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Sprite _idleFrameSprite;
        [SerializeField] private Sprite _winFrameSprite;

        private bool _isNearMissHaloActive;
        private Coroutine _bounceRoutine;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_targetHalo != null)
            {
                _targetHalo.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Zero-allocation near-miss animated target halo pulse
            if (_isNearMissHaloActive && _targetHalo != null)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
                float scale = 1.0f + pulse * 0.22f;
                _targetHalo.transform.localScale = new Vector3(scale, scale, 1f);

                Color c = _targetHalo.color;
                c.a = 0.6f + pulse * 0.4f;
                _targetHalo.color = c;
            }
        }

        public void ConfigureFrames(Sprite idleFrame, Sprite winFrame)
        {
            _idleFrameSprite = idleFrame;
            _winFrameSprite = winFrame;
            ApplyFrame(false);
        }

        public void SetWinning(bool winning)
        {
            ApplyFrame(winning);
        }

        private void ApplyFrame(bool winning)
        {
            if (_frameImage == null) return;

            Sprite sprite = winning && _winFrameSprite != null ? _winFrameSprite : _idleFrameSprite;
            if (sprite != null)
            {
                _frameImage.sprite = sprite;
            }
            _frameImage.color = Color.white;
            _frameImage.raycastTarget = false;
        }

        public void SetSymbol(SymbolDefinition def)
        {
            if (_symbolIcon != null && def != null)
            {
                _symbolIcon.sprite = def.Icon;
                _symbolIcon.color = Color.white;
                _symbolIcon.enabled = def.Icon != null;
            }
        }

        public void SetNearMissHalo(bool active)
        {
            _isNearMissHaloActive = active;
            if (_targetHalo != null)
            {
                _targetHalo.gameObject.SetActive(active);
                if (!active)
                {
                    _targetHalo.transform.localScale = Vector3.one;
                }
            }
        }

        public void PlayWinBounce()
        {
            if (_bounceRoutine != null)
            {
                StopCoroutine(_bounceRoutine);
            }
            _bounceRoutine = StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            // Scale bounce animation: 1.0 -> 1.25 -> 1.0 as specified in GDD
            float duration = 0.32f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float scaleFactor = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.28f;
                if (_symbolIcon != null)
                {
                    _symbolIcon.transform.localScale = _baseScale * scaleFactor;
                }
                yield return null;
            }

            if (_symbolIcon != null)
            {
                _symbolIcon.transform.localScale = _baseScale;
            }
            _bounceRoutine = null;
        }

        public void SetDim(bool dim)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = dim ? 0.5f : 1.0f;
            }
        }

        public void ResetView()
        {
            if (_bounceRoutine != null)
            {
                StopCoroutine(_bounceRoutine);
                _bounceRoutine = null;
            }

            SetNearMissHalo(false);
            SetDim(false);
            ApplyFrame(false);

            if (_symbolIcon != null)
            {
                _symbolIcon.transform.localScale = _baseScale;
            }
        }
    }
}
