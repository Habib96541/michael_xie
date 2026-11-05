using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Keeps UI inside the player safe area and fits the existing screens at narrow sizes.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ResponsiveCanvas : MonoBehaviour
    {
        RectTransform content;
        CanvasScaler scaler;
        Rect previousArea;
        Vector2Int previousSize;

        public static bool TouchLayout => Application.isMobilePlatform || Mathf.Min(Screen.width, Screen.height) <= 600;

        public void Initialize(RectTransform safeContent, CanvasScaler canvasScaler)
        {
            content = safeContent;
            scaler = canvasScaler;
            Apply();
        }

        void Update() { Apply(); }

        void Apply()
        {
            if (content == null || Screen.width < 1 || Screen.height < 1) return;
            var size = new Vector2Int(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
            if (safe.width <= 0 || safe.height <= 0) safe = new Rect(0, 0, size.x, size.y);
            if (size == previousSize && safe == previousArea) return;
            previousSize = size;
            previousArea = safe;

            var screen = (Vector2)size;
            content.anchorMin = safe.min / screen;
            content.anchorMax = safe.max / screen;
            content.offsetMin = content.offsetMax = Vector2.zero;

            Vector2 design = safe.width < safe.height ? new Vector2(1110f, 900f)
                : TouchLayout ? new Vector2(1240f, 780f) : new Vector2(1600f, 900f);
            // CanvasScaler sees the full screen, so give it the equivalent reference size.
            scaler.referenceResolution = design * (screen / safe.size);
        }
    }
}
