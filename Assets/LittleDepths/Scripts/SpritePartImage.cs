using UnityEngine;
using UnityEngine.UI;

namespace LittleDepths
{
    // Draw exactly the same isolated sprite mesh as SpriteRenderer, in its original
    // pixel canvas. This avoids both neighboring atlas fragments and UI-only refitting.
    public class SpritePartImage : Image
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!sprite)return;
            var vertices=sprite.vertices;var uv=sprite.uv;var triangles=sprite.triangles;var r=GetPixelAdjustedRect();float ppu=sprite.pixelsPerUnit;
            for(int i=0;i<vertices.Length;i++)
            {
                Vector2 pixel=vertices[i]*ppu+sprite.pivot;
                vh.AddVert(new Vector3(r.xMin+pixel.x/sprite.rect.width*r.width,r.yMin+pixel.y/sprite.rect.height*r.height,0),color,uv[i]);
            }
            for(int i=0;i<triangles.Length;i+=3)vh.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
    }
}
