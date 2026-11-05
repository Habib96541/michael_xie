using UnityEngine;

namespace CyberWhack.UI
{
    /// <summary>
    /// Enforces mobile safe-area compliance with minimum margins:
    /// 44px top clearance (camera notches, hole punches, dynamic islands)
    /// 34px bottom clearance (OS home gesture bars).
    /// Dynamically adapts across 16:9, 19.5:9, 20:9, and 21:9 screens.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        [Header("Safe Area Minimum Margins (Physical Pixels)")]
        [SerializeField] private float minTopMarginPx = 44f;
        [SerializeField] private float minBottomMarginPx = 34f;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            // Only update on screen resize/orientation event (zero allocation)
            if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

            Rect safeArea = Screen.safeArea;
            float screenW = Screen.width;
            float screenH = Screen.height;

            if (screenW <= 0 || screenH <= 0) return;

            // Enforce minimum margins
            float topInset = Mathf.Max(screenH - safeArea.yMax, minTopMarginPx);
            float bottomInset = Mathf.Max(safeArea.yMin, minBottomMarginPx);

            float anchorMinY = bottomInset / screenH;
            float anchorMaxY = (screenH - topInset) / screenH;

            float anchorMinX = safeArea.xMin / screenW;
            float anchorMaxX = safeArea.xMax / screenW;

            _rectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
            _rectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
