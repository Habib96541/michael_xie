using TMPro;
using UnityEngine;

namespace DiceGame.Effects
{
    public sealed class FloatingTextItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI textMesh;
        [SerializeField] private RectTransform rectTransform;

        private Vector2 _startPos;
        private Vector2 _velocity;
        private float _elapsed;
        private float _duration;
        private Color _baseColor;
        private bool _active;

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            if (textMesh == null)
            {
                textMesh = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        public void Spawn(string message, Vector2 screenPos, Color color, float size = 38.0f, float duration = 1.0f)
        {
            if (rectTransform == null || textMesh == null) return;

            _startPos = screenPos;
            rectTransform.anchoredPosition = screenPos;
            rectTransform.localScale = Vector3.one * 0.4f;

            textMesh.text = message;
            textMesh.fontSize = size;
            _baseColor = color;
            textMesh.color = color;

            _velocity = new Vector2(Random.Range(-30.0f, 30.0f), Random.Range(70.0f, 120.0f));
            _duration = duration;
            _elapsed = 0.0f;
            _active = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_active) return;

            _elapsed += Time.deltaTime;
            float t = _elapsed / _duration;

            if (t >= 1.0f)
            {
                _active = false;
                gameObject.SetActive(false);
                return;
            }

            float scale;
            if (t < 0.2f)
            {
                scale = Mathf.Lerp(0.4f, 1.25f, t / 0.2f);
            }
            else if (t < 0.4f)
            {
                scale = Mathf.Lerp(1.25f, 1.0f, (t - 0.2f) / 0.2f);
            }
            else
            {
                scale = 1.0f;
            }
            rectTransform.localScale = new Vector3(scale, scale, 1.0f);

            rectTransform.anchoredPosition = _startPos + _velocity * _elapsed;

            float alpha = (t > 0.6f) ? Mathf.Lerp(1.0f, 0.0f, (t - 0.6f) / 0.4f) : 1.0f;
            Color c = _baseColor;
            c.a = alpha;
            textMesh.color = c;
        }

        public bool IsAvailable => !_active;
    }
}

