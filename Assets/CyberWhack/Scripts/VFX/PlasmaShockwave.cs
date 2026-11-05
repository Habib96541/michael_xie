using UnityEngine;
using UnityEngine.UI;

namespace CyberWhack.VFX
{
    /// <summary>
    /// Renders an instantaneous 0.15-second expanding circular electric cyan
    /// plasma ring shockwave at the tap point. Zero-allocation per frame update.
    /// </summary>
    public class PlasmaShockwave : MonoBehaviour
    {
        [SerializeField] private Image shockwaveImage;
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float startScale = 0.3f;
        [SerializeField] private float endScale = 1.6f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Image _image;
        private float _elapsed;
        private bool _isActive;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _image = GetComponent<Image>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Play(Vector2 screenOrCanvasPosition, Color tint)
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            if (_image == null) _image = GetComponent<Image>();
            _rectTransform.anchoredPosition = screenOrCanvasPosition;
            _rectTransform.localScale = Vector3.one * startScale;
            _rectTransform.localRotation = Quaternion.Euler(0f, 0f, (screenOrCanvasPosition.x % 40f) - 20f);
            if (_image != null) _image.color = tint;
            _canvasGroup.alpha = 1.0f;
            _elapsed = 0.0f;
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsed += Time.deltaTime;
            float t = _elapsed / duration;

            if (t >= 1.0f)
            {
                _isActive = false;
                gameObject.SetActive(false);
                return;
            }

            // Fast expansion + smooth alpha fade out
            float scale = Mathf.Lerp(startScale, endScale, t);
            _rectTransform.localScale = new Vector3(scale, scale, 1.0f);
            _canvasGroup.alpha = 1.0f - (t * t); // Smooth quadratic fade
        }
    }
}
