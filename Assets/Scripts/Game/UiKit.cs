using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NeonDash
{
    /// <summary>
    /// Builds the game UI with shared cut-corner plates, controls, and typography.
    /// </summary>
    public static class UiKit
    {
        static Font uiFont;
        static Font displayFont;
        static Sprite chamferedSprite;
        static Sprite glowSprite;

        public static Font UiFont
        {
            get
            {
                if (uiFont != null) return uiFont;
                uiFont = Resources.Load<Font>("Fonts/Rajdhani-SemiBold");
                if (uiFont != null) return uiFont;
                try { uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
                catch { uiFont = null; }
                if (uiFont == null)
                {
                    try { uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                    catch { uiFont = null; }
                }
                if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return uiFont;
            }
        }

        public static Sprite Chamfered
        {
            get
            {
                if (chamferedSprite == null) chamferedSprite = MakeChamferedSprite(64, 12);
                return chamferedSprite;
            }
        }

        public static Sprite Glow
        {
            get
            {
                if (glowSprite == null) glowSprite = MakeGlowSprite(96);
                return glowSprite;
            }
        }

        static Sprite MakeChamferedSprite(int size, int cut)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = Mathf.Abs(x + 0.5f - size * 0.5f);
                    float py = Mathf.Abs(y + 0.5f - size * 0.5f);
                    float distance = (px + py - (size - cut)) * 0.7071f;
                    float alpha = Mathf.Clamp01(0.5f - distance);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            var border = new Vector4(cut + 1, cut + 1, cut + 1, cut + 1);
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                                 SpriteMeshType.FullRect, border);
        }

        static Sprite MakeGlowSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - center) / center;
                    float dy = (y + 0.5f - center) / center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static RectTransform CreateCanvas(string name, int sortingOrder)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            // Expand (rather than MatchWidthOrHeight) so a short, wide browser window shrinks the
            // whole layout instead of cropping the bottom of the result card.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            go.AddComponent<GraphicRaycaster>();

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("Event System");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
            RectTransform content = Container(canvas.transform, "Safe Area");
            go.AddComponent<ResponsiveCanvas>().Initialize(content, scaler);
            return content;
        }

        public static RectTransform Container(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            return rt;
        }

        public static Image Sprite9(Transform parent, string name, Color color, Sprite sprite, bool sliced = true)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite == null ? Chamfered : sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            if (!sliced) image.preserveAspect = false;
            Stretch(rt);
            return image;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        public static Text Label(Transform parent, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            bool display = style == FontStyle.Bold && size >= 38;
            if (display && displayFont == null) displayFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            label.font = display && displayFont != null ? displayFont : UiFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = anchor;
            label.fontStyle = display && displayFont != null ? FontStyle.Normal : style;
            label.raycastTarget = false;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(rt);
            return label;
        }

        public static Button Button(Transform parent, string caption, Color fill, out Text label, int fontSize = 30, bool playClick = true)
        {
            var panel = Sprite9(parent, "Button " + caption, fill, Chamfered);
            var button = AttachButton(panel, playClick);
            var edge = Sprite9(panel.transform, "Button highlight", Palette.Alpha(
                fill == Palette.Action ? Palette.Cyan : Palette.Violet, 0.72f), Chamfered);
            edge.raycastTarget = false;
            edge.rectTransform.anchorMin = new Vector2(0f, 1f);
            edge.rectTransform.anchorMax = new Vector2(1f, 1f);
            edge.rectTransform.offsetMin = new Vector2(13f, -3f);
            edge.rectTransform.offsetMax = new Vector2(-13f, -1f);

            label = Label(panel.transform, caption, fontSize, Palette.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (displayFont == null) displayFont = Resources.Load<Font>("Fonts/LilitaOne-Regular");
            if (displayFont != null)
            {
                label.font = displayFont;
                label.fontStyle = FontStyle.Normal;
            }
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -2f);
            return button;
        }

        public static Button AttachButton(Image panel, bool playClick = true)
        {
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None;
            panel.gameObject.AddComponent<UiButtonFx>();
            if (playClick) button.onClick.AddListener(() => NeonDashApp.Instance.Audio.PlayClick());
            return button;
        }

        public static RectTransform Frame(Transform parent, string name, Color accent)
        {
            var edge = Sprite9(parent, name, Palette.PanelLine, Chamfered);
            var face = Sprite9(edge.transform, "Panel face", Palette.Panel, Chamfered);
            face.raycastTarget = false;
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);

            var rail = Sprite9(edge.transform, "Status rail", accent, Chamfered);
            rail.raycastTarget = false;
            Place(rail.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -1f), new Vector2(104f, 3f));
            var tail = Sprite9(edge.transform, "Tail rail", Palette.Violet, Chamfered);
            tail.raycastTarget = false;
            Place(tail.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 1f), new Vector2(72f, 3f));
            return edge.rectTransform;
        }

        public static InputField Field(Transform parent, string hint, string value, out Text textComponent)
        {
            var panel = Sprite9(parent, "Field", Palette.PanelSoft, Chamfered);
            panel.gameObject.AddComponent<UiButtonFx>().HoverScale = 1.01f;

            var field = panel.gameObject.AddComponent<InputField>();
            field.targetGraphic = panel;
            field.characterLimit = 12;
            field.lineType = InputField.LineType.SingleLine;
            field.caretWidth = 2;

            textComponent = Label(panel.transform, value, 26, Palette.Text, TextAnchor.MiddleLeft);
            var textRect = textComponent.rectTransform;
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(18f, 0f);
            textRect.offsetMax = new Vector2(-14f, 0f);

            var placeholder = Label(panel.transform, hint, 26, Palette.Alpha(Palette.TextDim, 0.55f), TextAnchor.MiddleLeft);
            var placeholderRect = placeholder.rectTransform;
            placeholderRect.anchorMin = new Vector2(0f, 0f);
            placeholderRect.anchorMax = new Vector2(1f, 1f);
            placeholderRect.offsetMin = new Vector2(18f, 0f);
            placeholderRect.offsetMax = new Vector2(-14f, 0f);

            field.textComponent = textComponent;
            field.placeholder = placeholder;
            field.text = value;
            return field;
        }

        public static void AddButtonIcon(Text label, IconKind kind)
        {
            float size = label.fontSize * 0.9f;
            float labelWidth = label.preferredWidth;
            label.rectTransform.offsetMin = new Vector2(size + 8f, 0f);
            UiIcon.Add(label.transform.parent, kind, label.color, size, -(labelWidth + 8f) * 0.5f);
        }

        public static void FadeIn(CanvasGroup group, float duration, float delay = 0f)
        {
            if (group == null) return;
            group.alpha = 0f;
            TweenRunner.Run(duration, k => { if (group != null) group.alpha = k; }, delay, Ease.OutCubic);
        }

        public static void SlideIn(RectTransform rt, Vector2 from, Vector2 to, float duration, float delay, bool overshoot = true)
        {
            if (rt == null) return;
            rt.anchoredPosition = from;
            TweenRunner.Run(duration, k => { if (rt != null) rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k); }, delay,
                overshoot ? (System.Func<float, float>)Ease.OutBack : Ease.OutCubic);
        }

        public static void ScaleIn(Transform target, float duration, float delay = 0f)
        {
            if (target == null) return;
            target.localScale = Vector3.one * 0.86f;
            TweenRunner.Run(duration, k => { if (target != null) target.localScale = Vector3.one * k; }, delay, Ease.OutBack);
        }
    }
}
