using TMPro;
using UnityEngine;

namespace CyberWhack.VFX
{
    public class FloatingTextCallout : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private float duration = 0.55f;
        [SerializeField] private float floatDistance = 65.0f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector2 _startPos;
        private float _elapsed;
        private float _drift;
        private bool _isActive;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        public void Spawn(Vector2 canvasPos, string message, Color color)
        {
            _startPos = canvasPos;
            _drift = ((int)canvasPos.x % 19) - 9f;
            _rectTransform.anchoredPosition = canvasPos;
            if (textMesh != null)
            {
                textMesh.text = message;
                textMesh.color = color;
            }

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

            // Upward float with ease-out
            float ease = 1.0f - Mathf.Pow(1.0f - t, 2.0f);
            _rectTransform.anchoredPosition = new Vector2(_startPos.x + _drift * ease, _startPos.y + ease * floatDistance);
            _canvasGroup.alpha = 1.0f - t;

            // Comic juice pop scale
            float scaleEase = (t < 0.22f) ? Mathf.Lerp(0.5f, 1.35f, t / 0.22f) : Mathf.Lerp(1.35f, 1.0f, (t - 0.22f) / 0.78f);
            _rectTransform.localScale = new Vector3(scaleEase, scaleEase, 1.0f);
        }
    }
}
