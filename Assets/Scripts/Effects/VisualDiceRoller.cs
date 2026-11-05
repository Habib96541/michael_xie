using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.Effects
{
    public sealed class VisualDiceRoller : MonoBehaviour
    {
        [Header("Dice Transforms")]
        [SerializeField] private RectTransform dieLeft;
        [SerializeField] private RectTransform dieRight;
        [SerializeField] private Image dieLeftBg;
        [SerializeField] private Image dieRightBg;
        [SerializeField] private TextMeshProUGUI dieLeftText;
        [SerializeField] private TextMeshProUGUI dieRightText;
        [SerializeField] private RectTransform shadowLeft;
        [SerializeField] private RectTransform shadowRight;

        [Header("Result Number Display")]
        [SerializeField] private TextMeshProUGUI rollResultNumberText;
        [SerializeField] private RectTransform rollResultContainer;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.12f, 0.16f, 0.24f, 1.0f);
        [SerializeField] private Color winColor = new Color(0.0f, 0.9f, 0.55f, 1.0f);
        [SerializeField] private Color lossColor = new Color(0.95f, 0.2f, 0.35f, 1.0f);
        [SerializeField] private Color critColor = new Color(1.0f, 0.84f, 0.0f, 1.0f);

        private Vector2 _leftOrigin;
        private Vector2 _rightOrigin;
        private Vector2 _shadowLeftOrigin;
        private Vector2 _shadowRightOrigin;

        private bool _isRolling;
        private float _rollTimer;
        private float _rollDuration = 0.42f;
        private float _targetValue;
        private bool _isWin;
        private bool _isCrit;
        private Action _onRollComplete;

        // Cached digit characters to avoid string allocations
        private static readonly string[] DigitStrings = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private void Awake()
        {
            UpdateOrigins();

            SetDigits(5, 0);
            if (rollResultNumberText != null)
            {
                rollResultNumberText.text = "50.00";
            }
        }

        public void UpdateOrigins()
        {
            if (dieLeft != null) _leftOrigin = dieLeft.anchoredPosition;
            if (dieRight != null) _rightOrigin = dieRight.anchoredPosition;
            if (shadowLeft != null) _shadowLeftOrigin = shadowLeft.anchoredPosition;
            if (shadowRight != null) _shadowRightOrigin = shadowRight.anchoredPosition;
        }

        public void AnimateRoll(float targetRoll, bool isWin, bool isCrit, Action onComplete)
        {
            _targetValue = targetRoll;
            _isWin = isWin;
            _isCrit = isCrit;
            _onRollComplete = onComplete;

            _isRolling = true;
            _rollTimer = 0.0f;

            if (dieLeftBg != null) dieLeftBg.color = normalColor;
            if (dieRightBg != null) dieRightBg.color = normalColor;
        }

        private void Update()
        {
            // Smoothly decay result punch back to 1.0
            if (rollResultContainer != null && rollResultContainer.localScale.x > 1.0f)
            {
                rollResultContainer.localScale = Vector3.Lerp(rollResultContainer.localScale, Vector3.one, Time.deltaTime * 6.0f);
                if (rollResultContainer.localScale.x < 1.002f)
                {
                    rollResultContainer.localScale = Vector3.one;
                }
            }

            if (!_isRolling) return;

            _rollTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_rollTimer / _rollDuration);

            // Cycle numbers during tumble
            if (t < 0.85f)
            {
                int r1 = UnityEngine.Random.Range(0, 10);
                int r2 = UnityEngine.Random.Range(0, 10);
                SetDigits(r1, r2);

                // Vertical tumble arc
                float height = Mathf.Sin(t * Mathf.PI) * 45.0f;
                float tilt = Mathf.Sin(t * 18.0f) * 16.0f;

                if (dieLeft != null)
                {
                    dieLeft.anchoredPosition = _leftOrigin + new Vector2(0.0f, height);
                    dieLeft.localRotation = Quaternion.Euler(0.0f, 0.0f, tilt);
                    dieLeft.localScale = Vector3.one * (1.0f + height * 0.003f);
                }

                if (dieRight != null)
                {
                    dieRight.anchoredPosition = _rightOrigin + new Vector2(0.0f, height * 0.9f);
                    dieRight.localRotation = Quaternion.Euler(0.0f, 0.0f, -tilt * 1.1f);
                    dieRight.localScale = Vector3.one * (1.0f + height * 0.003f);
                }

                // Shadows stay on ground and shrink when dice go high
                float shadowScale = Mathf.Clamp01(1.0f - height * 0.015f);
                if (shadowLeft != null) shadowLeft.localScale = new Vector3(shadowScale, shadowScale * 0.6f, 1.0f);
                if (shadowRight != null) shadowRight.localScale = new Vector3(shadowScale, shadowScale * 0.6f, 1.0f);
            }
            else
            {
                // Landing bounce / squash & stretch
                float landT = (t - 0.85f) / 0.15f;
                float squashX = 1.0f + Mathf.Sin(landT * Mathf.PI) * 0.18f;
                float squashY = 1.0f - Mathf.Sin(landT * Mathf.PI) * 0.18f;

                if (dieLeft != null)
                {
                    dieLeft.anchoredPosition = _leftOrigin;
                    dieLeft.localRotation = Quaternion.identity;
                    dieLeft.localScale = new Vector3(squashX, squashY, 1.0f);
                }

                if (dieRight != null)
                {
                    dieRight.anchoredPosition = _rightOrigin;
                    dieRight.localRotation = Quaternion.identity;
                    dieRight.localScale = new Vector3(squashX, squashY, 1.0f);
                }

                if (shadowLeft != null) shadowLeft.localScale = Vector3.one;
                if (shadowRight != null) shadowRight.localScale = Vector3.one;
            }

            if (t >= 1.0f)
            {
                _isRolling = false;
                FinishRoll();
            }
        }

        private void FinishRoll()
        {
            // Set final digits
            int wholeVal = Mathf.Clamp((int)_targetValue, 0, 99);
            int tens = wholeVal / 10;
            int units = wholeVal % 10;
            SetDigits(tens, units);

            // Set final result text
            if (rollResultNumberText != null)
            {
                rollResultNumberText.text = _targetValue.ToString("F2");
                Color highlight = _isCrit ? critColor : (_isWin ? winColor : lossColor);
                rollResultNumberText.color = highlight;
            }

            // Tint dice backgrounds
            Color tint = _isCrit ? critColor : (_isWin ? winColor : lossColor);
            if (dieLeftBg != null) dieLeftBg.color = Color.Lerp(normalColor, tint, 0.45f);
            if (dieRightBg != null) dieRightBg.color = Color.Lerp(normalColor, tint, 0.45f);

            // Pop punch on result container
            if (rollResultContainer != null)
            {
                rollResultContainer.localScale = Vector3.one * 1.25f;
            }

            _onRollComplete?.Invoke();
        }

        private void SetDigits(int d1, int d2)
        {
            if (dieLeftText != null && d1 >= 0 && d1 < 10)
            {
                dieLeftText.text = DigitStrings[d1];
            }
            if (dieRightText != null && d2 >= 0 && d2 < 10)
            {
                dieRightText.text = DigitStrings[d2];
            }
        }
    }
}
