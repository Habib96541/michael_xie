using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths
{
    public static class UiKit
    {
        public static readonly Color Ink = Hex("14234c"), Muted = Hex("b9ddff"), Cream = Hex("ffffff"), Coral = Hex("ffdc43"), Teal = Hex("68c923");
        public static Color Hex(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        public static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h, Vector2? anchor = null)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = anchor ?? new Vector2(.5f,.5f);
            r.pivot = new Vector2(.5f,.5f); r.anchoredPosition = new Vector2(x,y); r.sizeDelta = new Vector2(w,h); return r;
        }
        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        }
        public static Image Image(string name, Transform parent, Sprite sprite, Color color, float x,float y,float w,float h, Vector2? anchor=null)
        {
            var r = Rect(name,parent,x,y,w,h,anchor); var i = r.gameObject.AddComponent<Image>();
            i.sprite=sprite; i.color=color; i.raycastTarget=false;
            if (sprite && sprite.border.sqrMagnitude > 0) i.type=UnityEngine.UI.Image.Type.Sliced;
            return i;
        }
        public static Text Text(string name, Transform parent, Font font, string value, int size, Color color, float x,float y,float w,float h, TextAnchor align=TextAnchor.MiddleCenter, Vector2? anchor=null)
        {
            var r=Rect(name,parent,x,y,w,h,anchor); var t=r.gameObject.AddComponent<Text>();
            t.font=font; t.text=value; t.fontSize=size; t.color=color; t.alignment=align; t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap; t.verticalOverflow=VerticalWrapMode.Overflow;
            t.supportRichText=true; return t;
        }
        public static Button Button(string name, Transform parent, Sprite sprite, Color color, float x,float y,float w,float h, Vector2? anchor=null)
        {
            var im=Image(name,parent,sprite,color,x,y,w,h,anchor); im.raycastTarget=true;
            var b=im.gameObject.AddComponent<Button>(); b.targetGraphic=im;
            var colors=b.colors; colors.highlightedColor=new Color(1,1,1,.92f); colors.pressedColor=new Color(.84f,.86f,.8f); colors.disabledColor=new Color(.7f,.75f,.72f,.68f); b.colors=colors;
            b.navigation=new Navigation { mode=Navigation.Mode.None };b.gameObject.AddComponent<UiMotion>(); return b;
        }
    }
}
