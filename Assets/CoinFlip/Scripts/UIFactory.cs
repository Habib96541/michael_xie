using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Image = UnityEngine.UI.Image;
using Button = UnityEngine.UI.Button;

namespace CoinFlip
{
    public static class Palette
    {
        public static readonly Color Background = Hex("101827"), Surface = Hex("182438"), Raised = Hex("202D42"),
            Line = Hex("304057"), Gold = Hex("F5B942"), Cyan = Hex("77DBE5"), Text = Hex("F4F2EA"),
            Muted = Hex("94A3B8"), Green = Hex("78D9AE"), Red = Hex("F18D87");
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color c); return c; }
    }

    public sealed class UIFactory
    {
        readonly TMP_FontAsset font;
        public UIFactory(TMP_FontAsset font) { this.font = font; }
        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); return rect;
        }
        public static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x + w * .5f, -y - h * .5f); rect.sizeDelta = new Vector2(w, h);
        }
        public static void Fill(RectTransform rect, float inset = 0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = Vector2.one * .5f;
            rect.offsetMin = Vector2.one * inset; rect.offsetMax = -Vector2.one * inset;
        }
        public Image Image(string name, Transform parent, string sprite, Color color)
        {
            RectTransform rt = Rect(name, parent); var image = rt.gameObject.AddComponent<Image>();
            if (!string.IsNullOrEmpty(sprite)) image.sprite = Resources.Load<Sprite>("Art/" + sprite);
            image.color = color; image.raycastTarget = false;
            if (sprite == "Rounded") { image.type = UnityEngine.UI.Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1.4f; }
            return image;
        }
        public TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            RectTransform rt = Rect(name, parent); var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = align;
            text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow; text.extraPadding = true;
            return text;
        }
        public Button Button(string name, Transform parent, Color color, UnityAction action, AudioManager sound)
        {
            Image image = Image(name, parent, "Rounded", color); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.08f,1.08f,1.08f);
            colors.pressedColor = new Color(.84f,.84f,.84f); colors.disabledColor = new Color(.52f,.56f,.63f,.7f);
            colors.fadeDuration = .09f; button.colors = colors;
            var nav = button.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = nav;
            button.onClick.AddListener(() => { sound.Unlock(); sound.Play(AudioManager.Cue.Press); action(); });
            image.gameObject.AddComponent<ButtonFeedback>().Initialize(button, sound);
            return button;
        }
    }

    public sealed class ButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        Button button; AudioManager sound; Coroutine tween;
        public void Initialize(Button value, AudioManager sound) { button = value; this.sound = sound; }
        public void OnPointerEnter(PointerEventData e) { if (button.interactable) sound.Play(AudioManager.Cue.Hover); }
        public void OnPointerDown(PointerEventData e) { if (button.interactable) { sound.Unlock(); Scale(.965f); } }
        public void OnPointerUp(PointerEventData e) { Scale(1); }
        public void OnPointerExit(PointerEventData e) { Scale(1); }
        void Scale(float end) { if (tween != null) StopCoroutine(tween); tween = StartCoroutine(Animate(end)); }
        IEnumerator Animate(float end)
        {
            float start = transform.localScale.x;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / .1f)
            { transform.localScale = Vector3.one * Mathf.Lerp(start, end, 1 - Mathf.Pow(1-t, 3)); yield return null; }
            transform.localScale = Vector3.one * end;
        }
        void OnDisable() { if (tween != null) StopCoroutine(tween); transform.localScale = Vector3.one; }
    }
}
