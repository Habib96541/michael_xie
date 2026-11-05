using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Crash
{
    /// Canvas / widget factory + an animated button.
    public static class UIKit
    {
        public static Canvas MakeCanvas(string name, Vector2 reference)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = reference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("[EventSystem]", typeof(EventSystem), typeof(StandaloneInputModule));
                Object.DontDestroyOnLoad(es);
            }
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
        {
            var rt = Rect(parent, name);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name, Color color, int radius = 28, bool raycast = false)
        {
            var img = Img(parent, name, Tex.Round(96, Mathf.Clamp(radius, 2, 46)), color, raycast);
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, bool bold = false)
        {
            var rt = Rect(parent, name);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Pal.UiFont;
            t.fontSize = size;
            t.text = text;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static UIButton Button(Transform parent, string name, string text, Vector2 size,
            Color bgColor, Color textColor, int fontSize = 44, int radius = -1)
        {
            var rt = Rect(parent, name);
            rt.sizeDelta = size;
            int rad = radius > 0 ? radius : Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(size.x, size.y) * 0.34f), 10, 46);

            var glow = Img(rt, "Glow", Tex.Glow(128), bgColor.WithAlpha(0.0f));
            glow.rectTransform.Stretch(-30, -30, -30, -30);
            glow.transform.SetAsFirstSibling();

            var bg = Panel(rt, "BG", bgColor, rad, true);
            bg.rectTransform.Stretch();

            var sheen = Img(bg.rectTransform, "Sheen", Tex.Fade(128), new Color(1f, 1f, 1f, 0.10f));
            sheen.rectTransform.Stretch(3, 3, 3, 3);
            sheen.raycastTarget = false;

            var label = Label(rt, "Label", text, fontSize, textColor, TextAnchor.MiddleCenter, true);
            label.rectTransform.Stretch(12, 4, 12, 4);

            var btn = rt.gameObject.AddComponent<UIButton>();
            btn.body = rt;
            btn.bg = bg;
            btn.label = label;
            btn.glow = glow;
            btn.baseColor = bgColor;
            return btn;
        }
    }

    /// Button with hover/press/pulse feel, driven by the tween runner.
    public class UIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public RectTransform body;
        public Image bg;
        public Text label;
        public Image glow;
        public Color baseColor = Color.white;
        public bool interactable = true;
        public System.Action onClick;

        float _hover;
        float _press;
        float _pulse;
        bool _busy;

        void Update()
        {
            if (body == null) return;
            float target = interactable ? (1f + 0.005f) : 0.985f;
            float s = target + _hover * 0.045f - _press * 0.055f + _pulse;
            body.localScale = Vector3.Lerp(body.localScale, Vector3.one * s, Time.unscaledDeltaTime * 16f);
            if (glow != null && glow.color.a > 0.001f)
            {
                var c = glow.color;
                c.a = Mathf.Lerp(c.a, 0f, Time.unscaledDeltaTime * 3.4f);
                glow.color = c;
            }
            if (bg != null)
            {
                float dim = interactable ? 1f : 0.45f;
                var c = baseColor;
                c.a = baseColor.a * dim;
                bg.color = Color.Lerp(bg.color, Color.Lerp(c, Color.white, _hover * 0.12f), Time.unscaledDeltaTime * 14f);
            }
        }

        public void Pulse(float strength = 0.05f)
        {
            _pulse = strength;
            TweenRunner.To(0.28f, k => _pulse = Mathf.Lerp(strength, 0f, k), Ease.OutCubic);
            if (glow != null)
            {
                var c = glow.color;
                c.a = 0.55f;
                glow.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!interactable) return;
            _hover = 1f;
            if (Sfx.I != null) Sfx.I.Blip(0.10f, 0.9f);
        }

        public void OnPointerExit(PointerEventData e) { _hover = 0f; _press = 0f; }
        public void OnPointerDown(PointerEventData e) { if (interactable) _press = 1f; }
        public void OnPointerUp(PointerEventData e) { _press = 0f; }

        public void OnPointerClick(PointerEventData e)
        {
            if (!interactable || onClick == null) return;
            if (Sfx.I != null) Sfx.I.Blip(0.16f, 0.7f);
            onClick();
        }
    }
}
