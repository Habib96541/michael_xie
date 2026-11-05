using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiceGame.UI
{
    public sealed class DiceRoundTimerView : MonoBehaviour
    {
        [Header("Timer UI")]
        [SerializeField] private Image timerFillBar;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private RectTransform timerContainer;
        [SerializeField] private GameObject urgencyOverlay;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.0f, 0.85f, 1.0f, 1.0f);
        [SerializeField] private Color warningColor = new Color(1.0f, 0.75f, 0.0f, 1.0f);
        [SerializeField] private Color criticalColor = new Color(1.0f, 0.2f, 0.3f, 1.0f);

        private float _maxDuration = 20.0f;
        private float _currentRemaining;
        private int _lastTenths = -1;
        private bool _isUrgent;

        private readonly char[] _timeBuffer = new char[8];

        private void Awake()
        {
            if (urgencyOverlay != null) urgencyOverlay.SetActive(false);
        }

        public void Initialize(float maxDuration)
        {
            _maxDuration = maxDuration;
            _currentRemaining = maxDuration;
            _lastTenths = -1;
            UpdateDisplay(true);
        }

        public void SetRemainingTime(float remaining)
        {
            _currentRemaining = Mathf.Max(0.0f, remaining);
            UpdateDisplay(false);
        }

        public void PulseBonusTime()
        {
            if (timerContainer != null)
            {
                timerContainer.localScale = Vector3.one * 1.35f;
            }
        }

        private void Update()
        {
            if (timerContainer != null && timerContainer.localScale.x > 1.0f)
            {
                timerContainer.localScale = Vector3.Lerp(timerContainer.localScale, Vector3.one, Time.deltaTime * 6.0f);
            }

            if (_isUrgent && urgencyOverlay != null)
            {
                float flash = (Mathf.Sin(Time.time * 12.0f) + 1.0f) * 0.5f;
                urgencyOverlay.SetActive(flash > 0.45f);
            }
        }

        private void UpdateDisplay(bool force)
        {
            float fill = Mathf.Clamp01(_currentRemaining / _maxDuration);
            if (timerFillBar != null)
            {
                timerFillBar.fillAmount = fill;
            }

            int tenths = (int)(_currentRemaining * 10.0f);
            if (tenths != _lastTenths || force)
            {
                _lastTenths = tenths;
                FormatTimeBuffer(_currentRemaining);

                if (timerText != null)
                {
                    timerText.SetText(_timeBuffer, 0, 5);

                    if (_currentRemaining <= 5.0f)
                    {
                        timerText.color = criticalColor;
                        if (timerFillBar != null) timerFillBar.color = criticalColor;
                        _isUrgent = true;
                    }
                    else if (_currentRemaining <= 10.0f)
                    {
                        timerText.color = warningColor;
                        if (timerFillBar != null) timerFillBar.color = warningColor;
                        _isUrgent = false;
                        if (urgencyOverlay != null) urgencyOverlay.SetActive(false);
                    }
                    else
                    {
                        timerText.color = normalColor;
                        if (timerFillBar != null) timerFillBar.color = normalColor;
                        _isUrgent = false;
                        if (urgencyOverlay != null) urgencyOverlay.SetActive(false);
                    }
                }
            }
        }

        private void FormatTimeBuffer(float seconds)
        {
            int s = (int)seconds;
            if (s > 99) s = 99;
            int frac = (int)((seconds - s) * 10.0f) % 10;

            _timeBuffer[0] = (char)('0' + (s / 10));
            _timeBuffer[1] = (char)('0' + (s % 10));
            _timeBuffer[2] = '.';
            _timeBuffer[3] = (char)('0' + frac);
            _timeBuffer[4] = 's';
        }
    }
}

