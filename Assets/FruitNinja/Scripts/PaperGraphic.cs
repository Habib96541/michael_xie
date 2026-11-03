using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BackyardDojo
{
    // Slightly crooked, textured paper silhouettes keep every sign tactile at any resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PaperGraphic : MaskableGraphic
    {
        public Texture texture;
        public bool round;
        public override Texture mainTexture => texture != null ? texture : Texture2D.whiteTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            Vector2[] p = round ? Circle(48) : new[] {
                new Vector2(.018f,.032f),new Vector2(.27f,.012f),new Vector2(.72f,.022f),
                new Vector2(.98f,0),new Vector2(1,.16f),new Vector2(.99f,.76f),
                new Vector2(.974f,.975f),new Vector2(.67f,.99f),new Vector2(.32f,.977f),
                new Vector2(.012f,1),new Vector2(0,.81f),new Vector2(.009f,.34f) };
            vh.AddVert(r.center, color, new Vector2(.5f,.5f));
            foreach (var v in p) vh.AddVert(new Vector3(r.x + v.x*r.width,r.y + v.y*r.height),color,v);
            for (int i=0;i<p.Length;i++) vh.AddTriangle(0,i+1,(i+1)%p.Length+1);
        }
        static Vector2[] Circle(int count)
        {
            var v = new Vector2[count];
            for(int i=0;i<count;i++) { float a=i*Mathf.PI*2/count; v[i]=new Vector2(.5f+Mathf.Cos(a)*.5f,.5f+Mathf.Sin(a)*.5f); }
            return v;
        }
    }

    public sealed class PaperButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public Action action;
        public Action hoverSound;
        public bool selected;
        bool over, down;
        Vector3 initial;
        void Awake() { initial=transform.localScale; }
        void Update()
        {
            float s=down ? .96f : over || selected ? 1.035f : 1;
            transform.localScale=Vector3.Lerp(transform.localScale,initial*s,Time.unscaledDeltaTime*15);
        }
        public void OnPointerEnter(PointerEventData e) { over=true; hoverSound?.Invoke(); }
        public void OnPointerExit(PointerEventData e) { over=false; down=false; }
        public void OnPointerDown(PointerEventData e) { down=true; }
        public void OnPointerUp(PointerEventData e) { down=false; }
        public void OnPointerClick(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left) action?.Invoke(); }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class FruitSliceGraphic : MaskableGraphic
    {
        public Texture atlas;
        public Rect uv;
        public int half; // 0 whole, -1 and 1 two actual clipped pieces
        public float cutAngle;
        public override Texture mainTexture => atlas != null ? atlas : Texture2D.whiteTexture;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var poly = new System.Collections.Generic.List<Vector2> {
                new Vector2(-.5f,-.5f),new Vector2(.5f,-.5f),new Vector2(.5f,.5f),new Vector2(-.5f,.5f) };
            if(half!=0)
            {
                Vector2 n=new Vector2(Mathf.Cos(cutAngle),Mathf.Sin(cutAngle))*half;
                var clipped=new System.Collections.Generic.List<Vector2>();
                for(int i=0;i<poly.Count;i++)
                {
                    Vector2 a=poly[i],b=poly[(i+1)%poly.Count];
                    float da=Vector2.Dot(a,n),db=Vector2.Dot(b,n);
                    if(da>=0) clipped.Add(a);
                    if((da>=0)!=(db>=0)) clipped.Add(Vector2.Lerp(a,b,da/(da-db)));
                }
                poly=clipped;
            }
            Rect r=rectTransform.rect;
            foreach(var p in poly) vh.AddVert(new Vector3(p.x*r.width,p.y*r.height),color,
                new Vector2(uv.x+(p.x+.5f)*uv.width,uv.y+(p.y+.5f)*uv.height));
            for(int i=1;i<poly.Count-1;i++) vh.AddTriangle(0,i,i+1);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BladeGraphic : MaskableGraphic
    {
        public readonly System.Collections.Generic.List<Vector2> points=new System.Collections.Generic.List<Vector2>();
        public readonly System.Collections.Generic.List<float> ages=new System.Collections.Generic.List<float>();
        public Color bladeColor=new Color(.6f,1,.9f);
        public void Tick(float dt)
        {
            for(int i=ages.Count-1;i>=0;i--) { ages[i]+=dt; if(ages[i]>.19f) { ages.RemoveAt(i);points.RemoveAt(i); } }
            SetVerticesDirty();
        }
        public void Add(Vector2 p) { points.Add(p);ages.Add(0);SetVerticesDirty(); }
        public void Clear() { points.Clear();ages.Clear();SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for(int layer=0;layer<2;layer++)
            for(int i=1;i<points.Count;i++)
            {
                Vector2 a=points[i-1],b=points[i];
                Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized;
                float f=1-ages[i]/.19f;
                float w=(layer==0?11:3.2f)*f;
                Color c=layer==0?bladeColor:Color.white;c.a=f*(layer==0?.3f:1);
                int n=vh.currentVertCount;
                vh.AddVert(a+normal*w,c,Vector2.zero);vh.AddVert(a-normal*w,c,Vector2.zero);
                vh.AddVert(b-normal*w,c,Vector2.zero);vh.AddVert(b+normal*w,c,Vector2.zero);
                vh.AddTriangle(n,n+1,n+2);vh.AddTriangle(n,n+2,n+3);
            }
        }
    }
}
