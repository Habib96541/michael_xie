using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Screen = UnityEngine.Device.Screen;

namespace SpinTheWheel
{
    [ExecuteAlways]
    public sealed class WheelMenuLayout : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform wheelGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text eyebrow;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text footer;
        [SerializeField] private TMP_Text panelHeading;
        [SerializeField] private TMP_Text panelDescription;
        [SerializeField] private RectTransform panelBackground;
        [SerializeField] private RectTransform panelShadow;
        [SerializeField] private RectTransform playButton;
        [SerializeField] private RectTransform quitButton;

        private Vector2Int previousSize;
        private Rect previousSafeArea;

        private void OnEnable()
        {
            RefreshLayout();
        }

        private void LateUpdate()
        {
            if (previousSize.x != Screen.width || previousSize.y != Screen.height ||
                previousSafeArea != Screen.safeArea)
                RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (canvas == null || content == null || Screen.width < 1 || Screen.height < 1)
                return;
            ApplyLayout(Screen.width, Screen.height, Screen.safeArea);
        }

        private void ApplyLayout(int width, int height, Rect area)
        {
            previousSize = new Vector2Int(width, height);
            previousSafeArea = area;
            if (area.width <= 0f || area.height <= 0f)
                area = new Rect(0f, 0f, width, height);

            bool portrait = area.height > area.width;
            float referenceWidth = (float)width / height > 0.65f ? 720f : 480f;
            float scale = Mathf.Max(0.8f, portrait ? width / referenceWidth
                : Mathf.Min(width / 1280f, height / 900f));
            canvasScaler.scaleFactor = canvas.scaleFactor = scale;
            safeArea.anchorMin = new Vector2(area.xMin / width, area.yMin / height);
            safeArea.anchorMax = new Vector2(area.xMax / width, area.yMax / height);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            float availableWidth = area.width / scale;
            float availableHeight = area.height / scale;
            float innerWidth = Mathf.Min(1120f, availableWidth - 32f);
            bool wide = availableWidth >= 700f && availableWidth > availableHeight * 1.2f;
            float panelWidth = wide ? Mathf.Clamp(innerWidth * 0.38f, 280f, 400f) : Mathf.Min(innerWidth, 440f);
            float panelHeight = wide ? 340f : 264f;
            float wheelWidth = wide ? innerWidth - panelWidth - 40f : innerWidth;
            float diameter = Mathf.Min(wheelWidth, wide ? 540f : 360f);
            float wheelHeight = diameter * 640f / 600f;
            float bodyHeight = wide ? Mathf.Max(wheelHeight, panelHeight) : wheelHeight + 20f + panelHeight;
            const float bodyTop = 156f;
            float naturalHeight = bodyTop + bodyHeight + 64f;
            float contentHeight = Mathf.Max(availableHeight, naturalHeight);
            float extra = (contentHeight - naturalHeight) * 0.5f;

            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(availableWidth, contentHeight);
            content.anchoredPosition = Vector2.zero;
            scrollView.StopMovement();
            scrollView.vertical = contentHeight > availableHeight + 0.5f;

            Text(eyebrow, 16f, innerWidth, 24f, 14f);
            Text(title, 42f, innerWidth, 62f, Mathf.Clamp(innerWidth * 0.095f, 28f, 48f));
            Text(subtitle, 108f, innerWidth, 36f, 17f);
            float wheelCenter = wide ? -innerWidth * 0.5f + wheelWidth * 0.5f : 0f;
            float wheelTop = bodyTop + extra + (wide ? (bodyHeight - wheelHeight) * 0.5f : 0f);
            Place(wheelGroup, wheelCenter, wheelTop, 600f, 640f);
            wheelGroup.localScale = Vector3.one * (diameter / 600f);
            wheelGroup.anchoredPosition = new Vector2(wheelCenter, -wheelTop - wheelHeight * 0.5f);
            float panelCenter = wide ? innerWidth * 0.5f - panelWidth * 0.5f : 0f;
            float panelTop = bodyTop + extra + (wide ? (bodyHeight - panelHeight) * 0.5f : wheelHeight + 20f);
            Place(panel, panelCenter, panelTop, panelWidth, panelHeight);
            Place(panelBackground, 0f, 0f, panelWidth, panelHeight);
            Place(panelShadow, 0f, 7f, panelWidth, panelHeight);
            Text(panelHeading, wide ? 32f : 16f, panelWidth - 32f, 40f, wide ? 30f : 26f);
            Text(panelDescription, wide ? 84f : 60f, panelWidth - 40f, 48f, 17f);
            Place(playButton, 0f, wide ? 160f : 122f, panelWidth - 40f, 64f);
            Place(quitButton, 0f, wide ? 240f : 194f, panelWidth - 40f, 56f);
            Text(footer, contentHeight - 48f, innerWidth, 32f, 14f);
        }

        private static void Text(TMP_Text label, float top, float width, float height, float fontSize)
        {
            Place(label.rectTransform, 0f, top, width, height);
            label.fontSize = fontSize;
        }

        private static void Place(RectTransform rect, float center, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(center, -top - height * 0.5f);
        }
    }
}
