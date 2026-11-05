using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DropStack.UI
{
    public class ArcadeLogo : MaskableGraphic
    {
        public bool stacked = true;
        private static readonly Dictionary<char,string[]> Glyphs = new Dictionary<char,string[]> {
            {'D',new[]{"11110","11011","11001","11001","11001","11011","11110"}},
            {'R',new[]{"11110","11011","11011","11110","11100","11010","11011"}},
            {'O',new[]{"01110","11011","11011","11011","11011","11011","01110"}},
            {'P',new[]{"11110","11011","11011","11110","11000","11000","11000"}},
            {'S',new[]{"01111","11000","11000","01110","00011","00011","11110"}},
            {'T',new[]{"11111","01110","00100","00100","00100","00100","00100"}},
            {'A',new[]{"01110","11011","11011","11111","11011","11011","11011"}},
            {'C',new[]{"01111","11000","11000","11000","11000","11000","01111"}},
            {'K',new[]{"11011","11010","11100","11100","11110","11011","11011"}}
        };
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Rect r=GetPixelAdjustedRect(); float cell=Mathf.Min(r.width/(stacked?29f:55f),r.height/(stacked?16f:8f));
            if(stacked) { Word(vh,"DROP",new Vector2(r.center.x-23*cell/2,r.center.y+cell),cell,new Color(.9f,.97f,1)); Word(vh,"STACK",new Vector2(r.center.x-29*cell/2,r.center.y-8*cell),cell,new Color(.025f,.82f,1)); }
            else Word(vh,"DROPSTACK",new Vector2(r.center.x-53*cell/2,r.center.y-3.5f*cell),cell,new Color(.08f,.87f,1));
        }
        private void Word(VertexHelper vh,string text,Vector2 origin,float cell,Color tint)
        {
            for(int k=0;k<text.Length;k++) for(int y=0;y<7;y++) for(int x=0;x<5;x++) if(Glyphs[text[k]][y][x]=='1') {
                var r=new Rect(origin.x+(k*6+x)*cell,origin.y+(6-y)*cell,cell*.92f,cell*.92f);
                Rect shadow=r; shadow.y-=cell*.22f; Quad(vh,shadow,new Color(.005f,.025f,.045f));
                Quad(vh,r,tint*.6f); r.xMin+=cell*.12f; r.xMax-=cell*.12f; r.yMin+=cell*.13f; r.yMax-=cell*.1f; Quad(vh,r,tint);
                Quad(vh,new Rect(r.xMin,r.yMax-cell*.1f,r.width,cell*.1f),Color.Lerp(tint,Color.white,.65f));
            }
        }
        private void Quad(VertexHelper vh,Rect r,Color tint)
        {
            int i=vh.currentVertCount; tint.a=1; tint*=color;
            vh.AddVert(new Vector2(r.xMin,r.yMin),tint,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),tint,Vector2.zero);
            vh.AddVert(new Vector2(r.xMax,r.yMax),tint,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),tint,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
    }
}
