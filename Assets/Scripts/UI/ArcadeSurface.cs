using UnityEngine;
using UnityEngine.UI;

namespace DropStack.UI
{
    // Resolution-independent cut corners, a lit bevel and a quiet recessed face.
    [RequireComponent(typeof(CanvasRenderer))]
    public class ArcadeSurface : MaskableGraphic
    {
        public Color top = new Color(.07f,.14f,.22f), bottom = new Color(.025f,.065f,.11f), edge = new Color(.18f,.36f,.45f);
        public float corner = 9, rim = 2;
        public bool bright;
        private float glow;
        public void SetGlow(float value) { if (Mathf.Abs(glow-value)<.015f) return; glow=value; SetVerticesDirty(); }
        public void SetEdge(Color value) { if (edge==value) return; edge=value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r=GetPixelAdjustedRect();
            Polygon(vh,r,new Color(.006f,.013f,.025f),new Color(.006f,.013f,.025f),corner);
            r.yMin+=3;
            Color border=Color.Lerp(edge,Color.white,glow*.32f);
            Polygon(vh,r,border,border*.62f,corner);
            r.xMin+=rim; r.xMax-=rim; r.yMin+=rim; r.yMax-=rim;
            Polygon(vh,r,Color.Lerp(top,edge,glow*.22f),bottom,Mathf.Max(0,corner-rim));
            Rect light=new Rect(r.xMin+corner,r.yMax-2,Mathf.Max(0,r.width-corner*2),2);
            Quad(vh,light,Color.Lerp(edge,Color.white,bright?.55f:.16f));
        }
        private void Polygon(VertexHelper vh,Rect r,Color upper,Color lower,float cut)
        {
            float c=Mathf.Min(cut,Mathf.Min(r.width,r.height)*.3f); int start=vh.currentVertCount;
            Vector2[] p={new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin,r.yMin+c)};
            vh.AddVert(r.center,Color.Lerp(lower,upper,.5f)*color,Vector2.zero);
            for(int i=0;i<8;i++) vh.AddVert(p[i],Color.Lerp(lower,upper,Mathf.InverseLerp(r.yMin,r.yMax,p[i].y))*color,Vector2.zero);
            for(int i=0;i<8;i++) vh.AddTriangle(start,start+1+i,start+1+(i+1)%8);
        }
        private void Quad(VertexHelper vh,Rect r,Color tint)
        {
            int i=vh.currentVertCount; tint*=color;
            vh.AddVert(new Vector2(r.xMin,r.yMin),tint,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),tint,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),tint,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),tint,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
    }
}
