using UnityEngine;

namespace DiceGame.Effects
{
    public sealed class ScreenShakeController : MonoBehaviour
    {
        [SerializeField] private RectTransform targetTransform;
        [SerializeField] private float maxTranslationShake = 18.0f;
        [SerializeField] private float maxRotationShake = 2.5f;
        [SerializeField] private float traumaDecayRate = 2.4f;

        private float _trauma;
        private Vector2 _initialAnchoredPos;
        private Quaternion _initialRotation;
        private float _timeSeed;

        private void Awake()
        {
            if (targetTransform == null)
            {
                targetTransform = GetComponent<RectTransform>();
            }

            if (targetTransform != null)
            {
                _initialAnchoredPos = targetTransform.anchoredPosition;
                _initialRotation = targetTransform.localRotation;
            }

            _timeSeed = Random.value * 100.0f;
        }

        public void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + amount);
        }

        private void Update()
        {
            if (targetTransform == null) return;

            if (_trauma > 0.001f)
            {
                _trauma = Mathf.Max(0.0f, _trauma - Time.deltaTime * traumaDecayRate);
                float shake = _trauma * _trauma; // Quadratic falloff

                float time = (Time.time + _timeSeed) * 28.0f;
                float offsetX = (Mathf.PerlinNoise(time, 0.0f) * 2.0f - 1.0f) * maxTranslationShake * shake;
                float offsetY = (Mathf.PerlinNoise(0.0f, time) * 2.0f - 1.0f) * maxTranslationShake * shake;
                float rotZ = (Mathf.PerlinNoise(time * 0.7f, time * 0.7f) * 2.0f - 1.0f) * maxRotationShake * shake;

                targetTransform.anchoredPosition = _initialAnchoredPos + new Vector2(offsetX, offsetY);
                targetTransform.localRotation = _initialRotation * Quaternion.Euler(0.0f, 0.0f, rotZ);
            }
            else
            {
                targetTransform.anchoredPosition = _initialAnchoredPos;
                targetTransform.localRotation = _initialRotation;
            }
        }
    }
}
