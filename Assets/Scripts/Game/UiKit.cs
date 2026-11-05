using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BullRide
{
    public static class UiKit
    {
        static Font uiFont, displayFont;
        public static Font Display => displayFont != null ? displayFont : displayFont = Resources.Load<Font>("Rye-Regular") ?? Font;
        static Sprite rounded;
        static Sprite circle;
        static Sprite guideRing;
        static Sprite recoveryArrow;
        public static readonly Vector2 Center = new Vector2(.5f, .5f);
        public static readonly Color Signal = new Color(.89f, .22f, .18f);

        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x + .5f - size / 2, y + .5f - size / 2);
                    byte alpha = (byte)(Mathf.Clamp01(size / 2 - point.magnitude) * 255);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                circle = Sprite.Create(texture, new Rect(0, 0, size, size), Center);
                return circle;
            }
        }

        public static Sprite GuideRing
        {
            get
            {
                if (guideRing != null) return guideRing;
                const int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + .5f - size / 2, y + .5f - size / 2);
                    float alpha = Mathf.Clamp01(1.4f - Mathf.Abs(p.magnitude - 60));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                guideRing = Sprite.Create(texture, new Rect(0, 0, size, size), Center);
                return guideRing;
            }
        }

        public static void Stripe(RectTransform parent, Color color)
        {
            Image stripe = Panel(parent, "Top accent", color);
            stripe.raycastTarget = false;
            RectTransform rect = stripe.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, -3);
            rect.offsetMax = Vector2.zero;
        }

        public static Sprite RecoveryArrow
        {
            get
            {
                if (recoveryArrow != null) return recoveryArrow;
                const int size = 32;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 3; y < 29; y++)
                for (int x = 0; x < size; x++)
                {
                    float halfWidth = y < 15 ? 2.5f : (29 - y) * .8f;
                    if (Mathf.Abs(x + .5f - size / 2) <= halfWidth)
                        pixels[y * size + x] = new Color32(255, 255, 255, 255);
                }
                texture.SetPixels32(pixels);
                texture.Apply();
                recoveryArrow = Sprite.Create(texture, new Rect(0, 0, size, size), Center);
                return recoveryArrow;
            }
        }

        public static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size,
            Vector2? pivot = null)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot ?? Center;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        public static Font Font
        {
            get
            {
                if (uiFont != null) return uiFont;
                try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                if (uiFont == null)
                {
                    try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                }
                if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 28);
                return uiFont;
            }
        }

        public static Sprite Rounded
        {
            get
            {
                if (rounded != null) return rounded;
                const int size = 64;
                const int radius = 5;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                float half = size * 0.5f - radius;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - size * 0.5f;
                    float py = y + 0.5f - size * 0.5f;
                    float qx = Mathf.Abs(px) - half;
                    float qy = Mathf.Abs(py) - half;
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float d = outside + inside - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(pixels);
                tex.Apply();
                rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
                return rounded;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            go.AddComponent<GraphicRaycaster>();
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("Event System");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return canvas;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = Font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            rt.sizeDelta = new Vector2(800f, 80f);
            return label;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Button Button(Transform parent, string name, string caption, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            return Button(parent, name, caption, size, 34, new Color(0.75f, 0.22f, 0.16f, 0.95f), onClick);
        }

        public static Button Button(Transform parent, string name, string caption, Vector2 size, int fontSize, Color color,
            UnityEngine.Events.UnityAction onClick)
        {
            var img = Panel(parent, name, color);
            var rt = img.rectTransform;
            rt.sizeDelta = size;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.selectedColor = Color.white;
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            var label = Label(rt, "Label", caption, fontSize, Palette.Text, TextAnchor.MiddleCenter);
            label.font = Display;
            Stretch(label.rectTransform);
            return btn;
        }

        public static Text LabelOf(Button button)
        {
            return button.GetComponentInChildren<Text>();
        }

        /// <summary>Cash formatting: 1234 → $1,234.</summary>
        public static string Money(int amount)
        {
            string digits = Mathf.Abs(amount).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            return (amount < 0 ? "-$" : "$") + digits;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void AnchorTop(RectTransform rt, float height, float top = 24f)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(900f, height);
            rt.anchoredPosition = new Vector2(0f, -top);
        }
    }
}
