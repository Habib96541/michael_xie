using UnityEngine;

namespace LittleDepths
{
    public static class AppearanceFit
    {
        public const float BoatWidth=3.95f;
        public static Vector2 Size(Sprite sprite)=>sprite.rect.size/sprite.pixelsPerUnit;
        public static Vector3 Point(Sprite sprite,Vector2 uv,bool flip=false)
        {
            var p=new Vector3((uv.x*sprite.rect.width-sprite.pivot.x)/sprite.pixelsPerUnit,(uv.y*sprite.rect.height-sprite.pivot.y)/sprite.pixelsPerUnit,0);
            if(flip)p.x=-p.x;return p;
        }
        public static float CompleteScale(FishingConfig config,int index)
        {
            var sprite=config.completeAnglers[index];
            float aboveWater=(sprite.rect.height-sprite.pivot.y)/sprite.pixelsPerUnit;
            return Mathf.Min(BoatWidth/config.completeHullWidths[index],2.05f/aboveWater);
        }
    }
}
