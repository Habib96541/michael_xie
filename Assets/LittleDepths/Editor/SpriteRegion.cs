using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LittleDepths.Editor
{
    // Sprite import analysis only: source textures are left intact.
    // A render mesh excludes disconnected pieces that belong to neighboring cells.
    public sealed class SpriteRegion
    {
        readonly Color32[] pixels;
        readonly int textureWidth;
        public readonly RectInt Rect;
        public readonly bool[] Main;
        public SpriteRegion(Color32[] pixels,int textureWidth,RectInt rect)
        {
            this.pixels=pixels;this.textureWidth=textureWidth;Rect=rect;
            var alpha=new bool[rect.width*rect.height];
            for(int y=0;y<rect.height;y++)for(int x=0;x<rect.width;x++)alpha[y*rect.width+x]=Pixel(x,y).a>=32;
            Main=Largest(alpha);
        }
        public bool Contains(int x,int y)=>x>=0&&y>=0&&x<Rect.width&&y<Rect.height&&Main[y*Rect.width+x];
        Color32 Pixel(int x,int y)=>pixels[(Rect.y+y)*textureWidth+Rect.x+x];
        bool[] Largest(bool[] candidates)
        {
            var seen=new bool[candidates.Length];var queue=new Queue<int>();var largest=new List<int>();
            for(int start=0;start<candidates.Length;start++)
            {
                if(seen[start]||!candidates[start])continue;
                var component=new List<int>();queue.Enqueue(start);seen[start]=true;
                while(queue.Count>0)
                {
                    int n=queue.Dequeue(),x=n%Rect.width,y=n/Rect.width;component.Add(n);
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
                    {
                        int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=Rect.width||ny>=Rect.height)continue;int k=ny*Rect.width+nx;
                        if(candidates[k]&&!seen[k]){seen[k]=true;queue.Enqueue(k);}
                    }
                }
                if(component.Count>largest.Count)largest=component;
            }
            var result=new bool[candidates.Length];foreach(int i in largest)result[i]=true;return result;
        }
        public Vector2[] Outline()
        {
            var points=new List<Vector2>();
            for(int y=0;y<Rect.height;y++)for(int x=0;x<Rect.width;x++)
            {
                if(!Contains(x,y)||(Contains(x-1,y)&&Contains(x+1,y)&&Contains(x,y-1)&&Contains(x,y+1)))continue;
                points.Add(new Vector2(x-1,y-1));points.Add(new Vector2(x+2,y-1));points.Add(new Vector2(x+2,y+2));points.Add(new Vector2(x-1,y+2));
            }
            points=points.Distinct().OrderBy(p=>p.x).ThenBy(p=>p.y).ToList();
            var hull=new List<Vector2>();
            foreach(var p in points){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            int lower=hull.Count;
            for(int i=points.Count-2;i>=0;i--){var p=points[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}
            if(hull.Count>1)hull.RemoveAt(hull.Count-1);
            hull.Reverse();var center=new Vector2(Rect.width*.5f,Rect.height*.5f);
            return hull.Select(p=>new Vector2(Mathf.Clamp(p.x,0,Rect.width),Mathf.Clamp(p.y,0,Rect.height))-center).ToArray();
        }
        static float Cross(Vector2 a,Vector2 b,Vector2 p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
        public Vector2 RodTip()
        {
            for(int y=Rect.height-1;y>=Rect.height*.50f;y--)
            {
                float sum=0;int count=0;
                for(int x=0;x<Rect.width*.50f;x++)if(Contains(x,y)){sum+=x;count++;}
                if(count>0)return new Vector2((sum/count+.5f)/Rect.width,(y+.5f)/Rect.height);
            }
            throw new Exception("No rod in seated sprite "+Rect);
        }
    }
}
