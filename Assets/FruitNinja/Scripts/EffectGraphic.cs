using UnityEngine;
using UnityEngine.UI;

namespace BackyardDojo
{
    /// <summary>Small vector meshes keep particle edges clean without extra texture downloads.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EffectGraphic : MaskableGraphic
    {
        public enum Form { Drop, Spark, Ring, Leaf, Seed, Glow, Shard, Arc }
        public Form form;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 size=rectTransform.rect.size;
            if(form==Form.Ring || form==Form.Arc)
            {
                int count=48;float arc=form==Form.Arc?.77f:1;
                for(int i=0;i<=count;i++)
                {
                    float a=i/(float)count*Mathf.PI*2*arc;
                    Vector2 d=new Vector2(Mathf.Cos(a)*size.x,Mathf.Sin(a)*size.y);
                    Color c=color;if(form==Form.Arc)c.a*=.25f+.75f*i/count;
                    vh.AddVert(d*.5f,c,Vector2.zero);vh.AddVert(d*(form==Form.Arc?.36f:.465f),c,Vector2.zero);
                    if(i>0) { int n=i*2;vh.AddTriangle(n-2,n-1,n);vh.AddTriangle(n-1,n+1,n); }
                }
                return;
            }
            Vector2[] polygon;
            if(form==Form.Spark) polygon=new[] {new Vector2(0,.5f),new Vector2(.1f,.1f),new Vector2(.42f,0),new Vector2(.1f,-.1f),new Vector2(0,-.5f),new Vector2(-.1f,-.1f),new Vector2(-.42f,0),new Vector2(-.1f,.1f)};
            else if(form==Form.Drop) polygon=new[] {new Vector2(0,.5f),new Vector2(.16f,.14f),new Vector2(.26f,-.14f),new Vector2(.18f,-.37f),new Vector2(0,-.46f),new Vector2(-.18f,-.37f),new Vector2(-.26f,-.14f),new Vector2(-.16f,.14f)};
            else if(form==Form.Leaf) polygon=new[] {new Vector2(0,.5f),new Vector2(.28f,.27f),new Vector2(.27f,-.1f),new Vector2(0,-.5f),new Vector2(-.27f,-.1f),new Vector2(-.28f,.27f)};
            else if(form==Form.Shard) polygon=new[] {new Vector2(-.4f,.5f),new Vector2(.35f,.36f),new Vector2(.5f,-.42f),new Vector2(-.5f,-.5f)};
            else
            {
                polygon=new Vector2[24];
                for(int i=0;i<polygon.Length;i++) {float a=i*Mathf.PI*2/polygon.Length;polygon[i]=new Vector2(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f);}
            }
            vh.AddVert(Vector3.zero,color,Vector2.zero);
            foreach(var v in polygon)
            {
                Color c=color;if(form==Form.Glow)c.a=0;
                vh.AddVert(new Vector3(v.x*size.x,v.y*size.y),c,Vector2.zero);
            }
            for(int i=0;i<polygon.Length;i++)vh.AddTriangle(0,i+1,(i+1)%polygon.Length+1);
        }
    }
}
