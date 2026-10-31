using UnityEngine;

namespace BrickRush
{
    public static class Art
    {
        public static readonly Color Cyan=Hex("50F5E3"), Violet=Hex("9A7BFF"), Pink=Hex("FF69B4"), Orange=Hex("FF9A51"), Gold=Hex("E9FF83"), Ink=Hex("070D1C"), Muted=Hex("8193B2"), White=Hex("EDF5FF");
        public static readonly Color[] Palette={Violet,Pink,Orange,Gold,Cyan,Hex("5AABFF"),Violet};
        public static Sprite Brick, Paddle, Ball, Glow, Ring, Spark, Panel;
        public static readonly Sprite[] Marks=new Sprite[5];
        public static Material TrailMaterial, SpriteMaterial;
        public static Color Hex(string value) { ColorUtility.TryParseHtmlString("#"+value,out var c);return c; }
        public static void Load()
        {
            Brick=Resources.Load<Sprite>("Art/brick"); Paddle=Resources.Load<Sprite>("Art/paddle"); Ball=Resources.Load<Sprite>("Art/ball"); Glow=Resources.Load<Sprite>("Art/glow"); Ring=Resources.Load<Sprite>("Art/ring"); Spark=Resources.Load<Sprite>("Art/spark"); Panel=Resources.Load<Sprite>("Art/panel");
            Marks[1]=Resources.Load<Sprite>("Art/armored"); Marks[2]=Resources.Load<Sprite>("Art/explosive"); Marks[3]=Resources.Load<Sprite>("Art/bonus"); Marks[4]=Resources.Load<Sprite>("Art/obstacle");
            TrailMaterial=Resources.Load<Material>("TrailMaterial");
            SpriteMaterial=Resources.Load<Material>("SpriteMaterial");
        }
        public static SpriteRenderer Sprite(string name,Sprite sprite,Vector2 position,Vector2 size,Color color,int order=0,Transform parent=null)
        {
            var go=new GameObject(name);if(parent!=null)go.transform.SetParent(parent,false);
            go.transform.localPosition=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=SpriteMaterial;sr.color=color;sr.sortingOrder=order;
            Size(sr,size); return sr;
        }
        public static void Size(SpriteRenderer sr,Vector2 size) => sr.transform.localScale=new Vector3(size.x/sr.sprite.bounds.size.x,size.y/sr.sprite.bounds.size.y,1);
    }
}
