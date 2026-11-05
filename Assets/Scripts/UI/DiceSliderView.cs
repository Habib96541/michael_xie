using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiceGame.UI
{
    public sealed class DiceSliderView : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("Slider Track Components")]
        [SerializeField] private RectTransform trackRect;
        [SerializeField] private RectTransform winZoneRect;
        [SerializeField] private RectTransform lossZoneRect;
        [SerializeField] private RectTransform critZoneRect;
        [SerializeField] private RectTransform handleRect;
        [SerializeField] private RectTransform resultPinRect;

        [Header("Visual Elements")]
        [SerializeField] private Image winZoneImage;
        [SerializeField] private Image lossZoneImage;
        [SerializeField] private Image critZoneImage;
        [SerializeField] private Image resultPinImage;

        [Header("Labels")]
        [SerializeField] private TextMeshProUGUI targetValueText;
        [SerializeField] private TextMeshProUGUI winChanceText;
        [SerializeField] private TextMeshProUGUI multiplierText;
        [SerializeField] private TextMeshProUGUI modeToggleButtonText;

        [Header("Colors")]
        [SerializeField] private Color winColor = new Color(0.0f, 0.95f, 0.6f, 1.0f);
        [SerializeField] private Color lossColor = new Color(0.95f, 0.22f, 0.38f, 1.0f);
        [SerializeField] private Color critColor = new Color(1.0f, 0.84f, 0.0f, 1.0f);

        [Header("Quick Preset Buttons")]
        [SerializeField] private Button buttonPreset25;
        [SerializeField] private Button buttonPreset50;
        [SerializeField] private Button buttonPreset75;
        [SerializeField] private Button buttonPreset90;
        [SerializeField] private Image bgPreset25;
        [SerializeField] private Image bgPreset50;
        [SerializeField] private Image bgPreset75;
        [SerializeField] private Image bgPreset90;

        public event Action<float> OnTargetChanged;
        public event Action OnToggleModeRequested;
        public event Action<float> OnPresetSelected;

        private static readonly Color PresetNormalBg = new Color(0.13f, 0.17f, 0.24f, 0.85f);
        private static readonly Color PresetActiveBg = new Color(0.0f, 0.75f, 0.95f, 0.35f);

        private float _minTarget = 2.0f;
        private float _maxTarget = 98.0f;
        private float _currentTarget = 50.0f;
        private bool _isRollUnder = true;
        private float _critMin = 70.0f;
        private float _critMax = 75.0f;

        private bool _isPinAnimating;
        private float _pinAnimTimer;
        private float _pinAnimDuration = 0.4f;
        private float _pinStartPos;
        private float _pinTargetPos;

        private void Awake()
        {
            if (winZoneImage != null) winZoneImage.color = winColor;
            if (lossZoneImage != null) lossZoneImage.color = lossColor;
            if (critZoneImage != null) critZoneImage.color = critColor;

            if (buttonPreset25 != null) buttonPreset25.onClick.AddListener(OnPreset25Clicked);
            if (buttonPreset50 != null) buttonPreset50.onClick.AddListener(OnPreset50Clicked);
            if (buttonPreset75 != null) buttonPreset75.onClick.AddListener(OnPreset75Clicked);
            if (buttonPreset90 != null) buttonPreset90.onClick.AddListener(OnPreset90Clicked);
        }

        private void OnDestroy()
        {
            if (buttonPreset25 != null) buttonPreset25.onClick.RemoveListener(OnPreset25Clicked);
            if (buttonPreset50 != null) buttonPreset50.onClick.RemoveListener(OnPreset50Clicked);
            if (buttonPreset75 != null) buttonPreset75.onClick.RemoveListener(OnPreset75Clicked);
            if (buttonPreset90 != null) buttonPreset90.onClick.RemoveListener(OnPreset90Clicked);
        }

        public void ApplyPresetChance(float chance)
        {
            float target = _isRollUnder ? chance : (100.0f - chance);
            SetTarget(target, notify: true);
            OnPresetSelected?.Invoke(chance);
        }

        private void OnPreset25Clicked() => ApplyPresetChance(25.0f);
        private void OnPreset50Clicked() => ApplyPresetChance(50.0f);
        private void OnPreset75Clicked() => ApplyPresetChance(75.0f);
        private void OnPreset90Clicked() => ApplyPresetChance(90.0f);

        public void Initialize(float minTarget, float maxTarget, float initialTarget, bool initialRollUnder)
        {
            _minTarget = minTarget;
            _maxTarget = maxTarget;
            _currentTarget = initialTarget;
            _isRollUnder = initialRollUnder;
            RefreshLayout();
        }

        public void SetTarget(float target, bool notify = true)
        {
            _currentTarget = Mathf.Clamp(target, _minTarget, _maxTarget);
            RefreshLayout();
            if (notify)
            {
                OnTargetChanged?.Invoke(_currentTarget);
            }
        }

        public void SetMode(bool isRollUnder)
        {
            _isRollUnder = isRollUnder;
            RefreshLayout();
        }

        public void SetCritZone(float min, float max)
        {
            _critMin = min;
            _critMax = max;
            UpdateCritZoneLayout();
        }

        public void SetMultiplierAndChance(float multiplier, float winChance)
        {
            if (multiplierText != null)
            {
                multiplierText.text = multiplier.ToString("F2") + "x";
            }
            if (winChanceText != null)
            {
                winChanceText.text = winChance.ToString("F1") + "%";
            }
        }

        public void AnimateResultPin(float rollValue, bool isWin, bool isCrit)
        {
            if (trackRect == null || resultPinRect == null) return;

            float trackWidth = trackRect.rect.width;
            float normalized = Mathf.Clamp01(rollValue / 100.0f);

            _pinStartPos = resultPinRect.anchoredPosition.x;
            _pinTargetPos = (normalized - 0.5f) * trackWidth;
            _isPinAnimating = true;
            _pinAnimTimer = 0.0f;

            if (resultPinImage != null)
            {
                resultPinImage.color = isCrit ? critColor : (isWin ? winColor : lossColor);
            }
        }

        private void Update()
        {
            if (!_isPinAnimating || resultPinRect == null) return;

            _pinAnimTimer += Time.deltaTime;
            float t = Mathf.Clamp01(_pinAnimTimer / _pinAnimDuration);

            float curved = 1.0f - Mathf.Pow(1.0f - t, 3.0f);
            float currentX = Mathf.Lerp(_pinStartPos, _pinTargetPos, curved);
            resultPinRect.anchoredPosition = new Vector2(currentX, resultPinRect.anchoredPosition.y);

            float pop = 1.0f + Mathf.Sin(t * Mathf.PI) * 0.35f;
            resultPinRect.localScale = new Vector3(pop, pop, 1.0f);

            if (t >= 1.0f)
            {
                _isPinAnimating = false;
                resultPinRect.localScale = Vector3.one;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateTargetFromPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateTargetFromPointer(eventData);
        }

        private void UpdateTargetFromPointer(PointerEventData eventData)
        {
            if (trackRect == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(trackRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            {
                float trackWidth = trackRect.rect.width;
                float normalized = Mathf.Clamp01((localPoint.x + trackWidth * 0.5f) / trackWidth);
                float newTarget = Mathf.Round(normalized * 100.0f);
                SetTarget(newTarget, notify: true);
            }
        }

        public void ToggleModeClicked()
        {
            OnToggleModeRequested?.Invoke();
        }

        private void OnRectTransformDimensionsChange()
        {
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (trackRect == null) return;

            float trackWidth = trackRect.rect.width;
            float normalizedTarget = _currentTarget / 100.0f;
            float targetPixelX = (normalizedTarget - 0.5f) * trackWidth;

            if (handleRect != null)
            {
                handleRect.anchoredPosition = new Vector2(targetPixelX, handleRect.anchoredPosition.y);
            }

            if (_isRollUnder)
            {
                if (winZoneRect != null)
                {
                    winZoneRect.anchorMin = new Vector2(0.0f, 0.0f);
                    winZoneRect.anchorMax = new Vector2(normalizedTarget, 1.0f);
                    winZoneRect.offsetMin = Vector2.zero;
                    winZoneRect.offsetMax = Vector2.zero;
                }
                if (lossZoneRect != null)
                {
                    lossZoneRect.anchorMin = new Vector2(normalizedTarget, 0.0f);
                    lossZoneRect.anchorMax = new Vector2(1.0f, 1.0f);
                    lossZoneRect.offsetMin = Vector2.zero;
                    lossZoneRect.offsetMax = Vector2.zero;
                }
                if (modeToggleButtonText != null)
                {
                    modeToggleButtonText.text = "ROLL UNDER";
                }
            }
            else
            {
                if (lossZoneRect != null)
                {
                    lossZoneRect.anchorMin = new Vector2(0.0f, 0.0f);
                    lossZoneRect.anchorMax = new Vector2(normalizedTarget, 1.0f);
                    lossZoneRect.offsetMin = Vector2.zero;
                    lossZoneRect.offsetMax = Vector2.zero;
                }
                if (winZoneRect != null)
                {
                    winZoneRect.anchorMin = new Vector2(normalizedTarget, 0.0f);
                    winZoneRect.anchorMax = new Vector2(1.0f, 1.0f);
                    winZoneRect.offsetMin = Vector2.zero;
                    winZoneRect.offsetMax = Vector2.zero;
                }
                if (modeToggleButtonText != null)
                {
                    modeToggleButtonText.text = "ROLL OVER";
                }
            }

            if (targetValueText != null)
            {
                targetValueText.text = _currentTarget.ToString("F0");
            }

            UpdateCritZoneLayout();
            UpdatePresetHighlights();
        }

        private void UpdatePresetHighlights()
        {
            float currentChance = _isRollUnder ? _currentTarget : (100.0f - _currentTarget);

            if (bgPreset25 != null) bgPreset25.color = (Mathf.Abs(currentChance - 25.0f) < 0.5f) ? PresetActiveBg : PresetNormalBg;
            if (bgPreset50 != null) bgPreset50.color = (Mathf.Abs(currentChance - 50.0f) < 0.5f) ? PresetActiveBg : PresetNormalBg;
            if (bgPreset75 != null) bgPreset75.color = (Mathf.Abs(currentChance - 75.0f) < 0.5f) ? PresetActiveBg : PresetNormalBg;
            if (bgPreset90 != null) bgPreset90.color = (Mathf.Abs(currentChance - 90.0f) < 0.5f) ? PresetActiveBg : PresetNormalBg;
        }

        private void UpdateCritZoneLayout()
        {
            if (critZoneRect == null) return;

            float minNorm = Mathf.Clamp01(_critMin / 100.0f);
            float maxNorm = Mathf.Clamp01(_critMax / 100.0f);

            critZoneRect.anchorMin = new Vector2(minNorm, 0.0f);
            critZoneRect.anchorMax = new Vector2(maxNorm, 1.0f);
            critZoneRect.offsetMin = Vector2.zero;
            critZoneRect.offsetMax = Vector2.zero;
        }
    }
}

