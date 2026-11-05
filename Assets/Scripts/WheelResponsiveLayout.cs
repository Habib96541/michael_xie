using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Screen = UnityEngine.Device.Screen;

namespace SpinTheWheel
{
    [ExecuteAlways]
    public sealed class WheelResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private CanvasScaler canvasScaler;
        [SerializeField] private ScrollRect scrollView;
        [SerializeField] private RectTransform safeArea;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform wheelGroup;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text eyebrow;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private RectTransform headerDivider;
        [SerializeField] private RectTransform footerDivider;
        [SerializeField] private TMP_Text footer;
        [SerializeField] private RectTransform panelBackground;
        [SerializeField] private RectTransform panelShadow;
        [SerializeField] private RectTransform panelAccent;
        [SerializeField] private RectTransform panelDivider;
        [SerializeField] private RectTransform spinButton;
        [SerializeField] private TMP_Text resultHeading;
        [SerializeField] private TMP_Text result;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private TMP_Text odds;
        [SerializeField] private TMP_Text range;
        [SerializeField] private TMP_Text[] segmentLabels;
        [SerializeField] private RectTransform menuButton;

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
            if (canvas == null || content == null || wheelGroup == null || Screen.width < 1 || Screen.height < 1)
                return;

            ApplyLayout(Screen.width, Screen.height, Screen.safeArea);
        }

        private void ApplyLayout(int screenWidth, int screenHeight, Rect screenSafeArea)
        {
            previousSize = new Vector2Int(screenWidth, screenHeight);
            previousSafeArea = screenSafeArea;
            if (screenSafeArea.width <= 0f || screenSafeArea.height <= 0f)
                screenSafeArea = new Rect(0f, 0f, screenWidth, screenHeight);

            bool portrait = screenSafeArea.height > screenSafeArea.width;
            float portraitReferenceWidth = (float)screenWidth / screenHeight > 0.65f ? 720f : 480f;
            float scale = Mathf.Max(0.8f, portrait ? screenWidth / portraitReferenceWidth
                : Mathf.Min(screenWidth / 1280f, screenHeight / 900f));
            canvasScaler.scaleFactor = scale;
            canvas.scaleFactor = scale;
            safeArea.anchorMin = new Vector2(screenSafeArea.xMin / screenWidth, screenSafeArea.yMin / screenHeight);
            safeArea.anchorMax = new Vector2(screenSafeArea.xMax / screenWidth, screenSafeArea.yMax / screenHeight);
            safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;

            float width = screenSafeArea.width / scale;
            float height = screenSafeArea.height / scale;
            float padding = Mathf.Clamp(width * 0.04f, 16f, 32f);
            float innerWidth = Mathf.Max(1f, Mathf.Min(1200f, width - padding * 2f));
            bool wide = width >= 700f && width > height * 1.2f;
            bool compact = !wide || height < 700f;
            float headerHeight = compact ? (wide ? 104f : 132f) : 160f;
            float navigationHeight = menuButton == null ? 0f : 64f;
            headerHeight += navigationHeight;
            const float footerHeight = 52f;
            float panelWidth = wide ? Mathf.Clamp(innerWidth * 0.34f, 280f, 400f) : Mathf.Min(innerWidth, 520f);
            float panelHeight = compact ? 292f : 424f;
            float wheelColumnWidth = wide ? innerWidth - panelWidth - 32f : innerWidth;
            float availableWheelHeight = height - padding * 2f - headerHeight - footerHeight - 16f;
            if (!wide)
                availableWheelHeight -= panelHeight + 20f;

            float maximumDiameter = Mathf.Min(wheelColumnWidth, 640f);
            float diameter = Mathf.Clamp(availableWheelHeight * 600f / 640f,
                Mathf.Min(340f, maximumDiameter), maximumDiameter);
            float wheelHeight = diameter * 640f / 600f;
            float bodyHeight = wide ? Mathf.Max(wheelHeight, panelHeight) : wheelHeight + 20f + panelHeight;
            float naturalHeight = padding * 2f + headerHeight + bodyHeight + 16f + footerHeight;
            float contentHeight = Mathf.Max(height, naturalHeight);
            float bodyTop = padding + headerHeight + (contentHeight - naturalHeight) * 0.5f;

            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(width, contentHeight);
            content.localScale = Vector3.one;
            scrollView.StopMovement();
            content.anchoredPosition = Vector2.zero;
            scrollView.vertical = contentHeight > height + 0.5f;

            if (menuButton != null)
                Place(menuButton, (innerWidth - 112f) * 0.5f, padding, 112f, 56f);
            LayoutHeader(innerWidth, padding + navigationHeight, compact, wide);
            float wheelCenter = wide ? -innerWidth * 0.5f + wheelColumnWidth * 0.5f : 0f;
            float wheelTop = bodyTop + (wide ? (bodyHeight - wheelHeight) * 0.5f : 0f);
            float wheelScale = diameter / 600f;
            Place(wheelGroup, wheelCenter, wheelTop, 600f, 640f);
            wheelGroup.localScale = new Vector3(wheelScale, wheelScale, 1f);
            wheelGroup.anchoredPosition = new Vector2(wheelCenter, -wheelTop - wheelHeight * 0.5f);

            float panelCenter = wide ? innerWidth * 0.5f - panelWidth * 0.5f : 0f;
            float panelTop = bodyTop + (wide ? (bodyHeight - panelHeight) * 0.5f : wheelHeight + 20f);
            Place(panel, panelCenter, panelTop, panelWidth, panelHeight);
            LayoutPanel(panelWidth, panelHeight, compact);

            float footerTop = contentHeight - padding - footerHeight;
            Place(footerDivider, 0f, footerTop, innerWidth, 1f);
            Text(footer, 0f, footerTop + 12f, innerWidth, 40f, 14f);
            if (segmentLabels == null)
                return;
            foreach (TMP_Text label in segmentLabels)
            {
                if (label == null)
                    continue;
                label.fontSize = Mathf.Clamp(12f / (wheelScale * scale), 21f, 32f);
                label.rectTransform.sizeDelta = new Vector2(90f, Mathf.Max(28f, label.fontSize * 1.3f));
            }
        }

        private void LayoutHeader(float width, float top, bool compact, bool wide)
        {
            TextAlignmentOptions alignment = wide ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
            if (eyebrow != null)
                eyebrow.alignment = alignment;
            if (title != null)
                title.alignment = alignment;
            if (subtitle != null)
                subtitle.alignment = alignment;
            if (compact && wide)
            {
                Text(eyebrow, 0f, top, width, 20f, 13f);
                Text(title, 0f, top + 20f, width, 46f, 34f);
                Text(subtitle, 0f, top + 68f, width, 26f, 14f);
                Place(headerDivider, 0f, top + 100f, width, 1f);
                return;
            }
            Text(eyebrow, 0f, top, width, 22f, compact ? 14f : 16f);
            Text(title, 0f, top + (compact ? 24f : 30f), width, compact ? 52f : 64f,
                compact ? Mathf.Clamp(width * 0.095f, 28f, 40f) : 48f);
            Text(subtitle, 0f, top + (compact ? 78f : 100f), width, 40f, compact ? 16f : 19f);
            Place(headerDivider, 0f, top + (compact ? 124f : 148f), width, 1f);
        }

        private void LayoutPanel(float width, float height, bool compact)
        {
            Place(panelBackground, 0f, 0f, width, height);
            Place(panelShadow, 0f, 7f, width, height);
            Place(panelAccent, 0f, 1f, width - 12f, 2f);
            float textWidth = width - 32f;
            Text(resultHeading, 0f, compact ? 12f : 32f, textWidth, 24f, compact ? 13f : 15f);
            Text(result, 0f, compact ? 36f : 76f, textWidth, compact ? 68f : 104f, compact ? 56f : 78f);
            Text(status, 0f, compact ? 108f : 194f, textWidth, compact ? 40f : 56f, compact ? 16f : 19f);
            Place(panelDivider, 0f, compact ? 156f : 264f, textWidth, 1f);
            Place(spinButton, 0f, compact ? 170f : 282f, textWidth, compact ? 56f : 64f);
            Text(buttonLabel, 0f, 0f, textWidth, compact ? 56f : 64f, compact ? 19f : 21f);
            Text(odds, 0f, compact ? 238f : 358f, textWidth, 24f, compact ? 13f : 15f);
            Text(range, 0f, compact ? 262f : 386f, textWidth, 24f, compact ? 13f : 14f);
        }

        private static void Text(TMP_Text label, float center, float top, float width, float height, float fontSize)
        {
            if (label == null)
                return;
            Place(label.rectTransform, center, top, width, height);
            label.fontSize = fontSize;
        }

        private static void Place(RectTransform rect, float center, float top, float width, float height)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(center, -top - height * 0.5f);
        }
    }
}
