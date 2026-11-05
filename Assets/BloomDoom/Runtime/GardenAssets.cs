using System.Collections.Generic;
using UnityEngine;

namespace BloomDoom
{
    public static class GardenAssets
    {
        static readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        static Sprite round,circle;
        public static Sprite Get(string name,int frame=0)
        {
            if(!sprites.TryGetValue(name,out var frames)){
                var texture=Resources.Load<Texture2D>("BloomDoom/Art/"+name);
                if(texture==null){Debug.LogError("Missing garden artwork: "+name);return null;}
                int count=texture.width==4096&&texture.height==256?16:1;
                frames=new Sprite[count];for(int i=0;i<count;i++)frames[i]=Sprite.Create(texture,new Rect(i*texture.width/count,0,texture.width/count,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                sprites[name]=frames;
            }
            return frames[Mathf.Abs(frame)%frames.Length];
        }
        public static Color Color(string hex){ColorUtility.TryParseHtmlString(hex,out var color);return color;}
        public static Sprite Rounded
        {get{if(round==null)round=Shape(false);return round;}}
        public static Sprite Circle
        {get{if(circle==null)circle=Shape(true);return circle;}}
        static Sprite Shape(bool disk)
        {
            const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float distance=disk?Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32))-30:Vector2.Distance(new Vector2(Mathf.Max(0,12-x-.5f,x+.5f-52),Mathf.Max(0,12-y-.5f,y+.5f-52)),Vector2.zero)-12;
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(.5f-distance));
            }
            texture.SetPixels(pixels);texture.Apply();texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,disk?Vector4.zero:new Vector4(15,15,15,15));
        }
    }
}
