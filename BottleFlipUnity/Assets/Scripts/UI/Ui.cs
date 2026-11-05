using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BottleFlip
{
    /// <summary>Small helpers for building the UI in code.</summary>
    public static class Ui
    {
        static TMP_FontAsset _font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (_font != null) return _font;
                _font = TMP_Settings.defaultFontAsset;
                if (_font == null) _font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                return _font;
            }
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
            return rt;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, Image.Type type = Image.Type.Simple)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f));
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = type;
            img.color = color;
            img.raycastTarget = false;
            img.pixelsPerUnitMultiplier = 1f;
            return img;
        }

        public static void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
                                           TextAlignmentOptions align = TextAlignmentOptions.Center,
                                           FontStyles style = FontStyles.Bold)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, size * 1.6f));
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = Font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = false;
            tmp.margin = Vector4.zero;
            return tmp;
        }

        public static TextMeshProUGUI Shadowed(TextMeshProUGUI source, Color shadowColor, Vector2 offset)
        {
            var go = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
            go.name = source.name + "Shadow";
            go.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
            var shadow = go.GetComponent<TextMeshProUGUI>();
            shadow.color = shadowColor;
            shadow.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = ((RectTransform)source.transform).anchoredPosition + offset;
            return shadow;
        }

        public static CanvasGroup Group(GameObject go, bool blocksByDefault = true)
        {
            var g = go.GetComponent<CanvasGroup>();
            if (g == null) g = go.AddComponent<CanvasGroup>();
            g.alpha = 1f;
            g.interactable = blocksByDefault;
            g.blocksRaycasts = blocksByDefault;
            return g;
        }

        public static void SetVisible(CanvasGroup g, bool visible)
        {
            g.alpha = visible ? 1f : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
            g.gameObject.SetActive(visible);
        }
    }

    /// <summary>Canvas-friendly button with press/hover scale and optional idle breathing.</summary>
    public class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public Action Clicked;
        public static Action GlobalClickSound;

        public float PulseAmplitude;
        public float PulseSpeed = 2.4f;
        public Transform Visual;   // what gets scaled (defaults to this transform)

        RectTransform _rt;
        float _scale = 1f;
        float _target = 1f;
        float _pulseT;
        bool _hover;
        bool _down;

        public void Init()
        {
            _rt = Visual != null ? (RectTransform)Visual : (RectTransform)transform;
            _scale = 1f;
        }

        public void SetPulse(float amplitude, float speed = 2.4f)
        {
            PulseAmplitude = amplitude;
            PulseSpeed = speed;
            _pulseT = 0f;
        }

        void Update()
        {
            if (_rt == null) return;
            float want = _target;
            if (PulseAmplitude > 0f)
            {
                _pulseT += Time.unscaledDeltaTime * PulseSpeed;
                want += PulseAmplitude * (0.5f + 0.5f * Mathf.Sin(_pulseT * Mathf.PI * 2f));
            }
            float k = 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime);
            _scale = Mathf.Lerp(_scale, want, k);
            _rt.localScale = new Vector3(_scale, _scale, 1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _down = true;
            _target = 0.92f;
            GlobalClickSound?.Invoke();
            Clicked?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _down = false;
            _target = _hover ? 1.04f : 1f;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hover = true;
            if (!_down) _target = 1.04f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            if (!_down) _target = 1f;
        }
    }

    /// <summary>Full-screen invisible catcher that turns any tap into a game input.</summary>
    public class TapCatcher : MonoBehaviour, IPointerDownHandler, IPointerClickHandler
    {
        public Action<Vector2> Tapped;
        bool _consumed;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!enabled) return;
            _consumed = true;
            Tapped?.Invoke(eventData.position);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // PointerDown already handled it; this keeps the click from bubbling elsewhere.
            if (_consumed) _consumed = false;
        }
    }
}
