using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HiLo
{
    public static class UIFactory
    {
        public static TMP_FontAsset Font;
        public static Color Ink = Hex("EAF0EA"), Muted = Hex("8BABA9"), Teal = Hex("55E6BE"), Coral = Hex("F18A7E"), Gold = Hex("EBCB86"), Panel = Hex("142E36");
        public static Color Hex(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = (RectTransform)go.transform; r.SetParent(parent, false); return r;
        }
        public static void Place(RectTransform r, float x, float y, float w, float h)
        { r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f); r.anchoredPosition = new Vector2(x,y); r.sizeDelta = new Vector2(w,h); }
        public static void Fill(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        public static UnityEngine.UI.Image Image(string name, Transform parent, string sprite, Color color)
        {
            var r = Rect(name,parent); var image = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = Resources.Load<Sprite>("Art/" + sprite); image.color = color; image.raycastTarget = false;
            if (sprite == "rounded") image.type = UnityEngine.UI.Image.Type.Sliced;
            return image;
        }
        public static TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var r=Rect(name,parent);var text=r.gameObject.AddComponent<TextMeshProUGUI>(); text.font=Font; text.text=value; text.fontSize=size; text.color=color;
            text.alignment=alignment; text.raycastTarget=false; text.textWrappingMode=TextWrappingModes.NoWrap; text.overflowMode=TextOverflowModes.Ellipsis; text.margin=Vector4.zero;
            return text;
        }
        public static UnityEngine.UI.Button Button(string name, Transform parent, Color color, UnityEngine.Events.UnityAction action)
        {
            var image=Image(name,parent,"rounded",color); image.raycastTarget=true; var button=image.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(.78f,.86f,.85f);colors.disabledColor=new Color(.45f,.5f,.5f,.5f);button.colors=colors;
            button.onClick.AddListener(action); button.gameObject.AddComponent<ButtonFeel>(); return button;
        }
    }
    public sealed class ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public void OnPointerEnter(PointerEventData e) { if (!GameManager.Instance.ReducedMotion) transform.localScale = Vector3.one * 1.025f; }
        public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;
        public void OnPointerDown(PointerEventData e) { if (!GameManager.Instance.ReducedMotion) transform.localScale = Vector3.one * .97f; }
        public void OnPointerUp(PointerEventData e) => transform.localScale = Vector3.one;
        void OnDisable() => transform.localScale = Vector3.one;
    }
}
