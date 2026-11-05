using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>Fits each screen in canvas coordinates, independently of browser pixel density.</summary>
    [DefaultExecutionOrder(-100)]
    public sealed class ResponsiveCanvas : MonoBehaviour
    {
        struct ScreenLayout
        {
            public RectTransform Root;
            public Vector2 Portrait;
        }

        readonly List<ScreenLayout> screens = new List<ScreenLayout>();
        RectTransform content;

        public static bool TouchLayout => Application.isMobilePlatform || Input.touchSupported
            || Mathf.Min(Screen.width, Screen.height) <= 600;

        public void Initialize(RectTransform safeContent, CanvasScaler canvasScaler)
        {
            content = safeContent;
            // CanvasScaler's display dimensions can differ from the WebGL drawing buffer.
            // Fit menus using their actual parent rect instead of mixing coordinate spaces.
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            canvasScaler.scaleFactor = 1f;
            Apply();
        }

        public void Register(RectTransform root, Vector2 portraitSize)
        {
            screens.Add(new ScreenLayout { Root = root, Portrait = portraitSize });
            Apply();
        }

        void Update() { Apply(); }

        public void Apply()
        {
            if (content == null || Screen.width < 1 || Screen.height < 1) return;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            Rect safe = Screen.safeArea;
#if UNITY_WEBGL && !UNITY_EDITOR
            // CSS already reserves the notch and home indicator outside the canvas.
            safe = new Rect(Vector2.zero, screen);
#endif
            float left = Mathf.Clamp(safe.xMin, 0f, screen.x);
            float bottom = Mathf.Clamp(safe.yMin, 0f, screen.y);
            float right = Mathf.Clamp(safe.xMax, left, screen.x);
            float top = Mathf.Clamp(safe.yMax, bottom, screen.y);
            if (right - left < 1f || top - bottom < 1f)
            {
                left = bottom = 0f;
                right = screen.x;
                top = screen.y;
            }
            content.anchorMin = new Vector2(left / screen.x, bottom / screen.y);
            content.anchorMax = new Vector2(right / screen.x, top / screen.y);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.ForceUpdateRectTransforms();
            Vector2 available = content.rect.size;
            if (available.x < 1f || available.y < 1f) return;
            bool portrait = available.y > available.x * 1.2f;
            foreach (ScreenLayout layout in screens)
            {
                if (layout.Root == null) continue;
                Vector2 design = portrait ? layout.Portrait
                    : TouchLayout ? new Vector2(1240f, 780f) : new Vector2(1600f, 900f);
                float scale = Mathf.Min(available.x / design.x, available.y / design.y);
                layout.Root.anchorMin = layout.Root.anchorMax = layout.Root.pivot = new Vector2(0.5f, 0.5f);
                layout.Root.anchoredPosition = Vector2.zero;
                layout.Root.sizeDelta = available / scale;
                layout.Root.localScale = new Vector3(scale, scale, 1f);
            }
        }
    }
}
