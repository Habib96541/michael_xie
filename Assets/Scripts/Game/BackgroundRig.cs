using System.Collections.Generic;
using NeonDash.Core;
using UnityEngine;

namespace NeonDash
{
    /// <summary>Repeating inset panels scroll behind the course and tint with each mode.</summary>
    public sealed class BackgroundRig : MonoBehaviour
    {
        const float RepeatWidth = 28f;
        Camera cam;
        Transform sky;
        Transform tiles;
        MeshRenderer skyRenderer;
        MeshRenderer tileRenderer;
        MaterialPropertyBlock tintBlock;
        Color tint = Color.white;
        Color targetTint = Color.white;

        public void Build(Camera camera)
        {
            cam = camera;
            tintBlock = new MaterialPropertyBlock();

            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();
            MeshTools.AddQuadGradient(verts, colors, tris, -80f, -9f, 80f, 9f,
                new Color(0.035f, 0.34f, 0.83f), new Color(0.017f, 0.075f, 0.25f));
            sky = BuildMesh("Blue sky", 40, verts, colors, tris, out skyRenderer);

            verts.Clear();
            colors.Clear();
            tris.Clear();
            for (int repeat = -2; repeat <= 2; repeat++)
            {
                for (int row = -3; row < 5; row++)
                {
                    float x = RepeatWidth * repeat + (row % 2 == 0 ? 0f : 2.2f);
                    float y = row * 4.5f;
                    for (int col = 0; col < 6; col++)
                    {
                        float width = col % 3 == 0 ? 6.1f : col % 3 == 1 ? 4.1f : 3.8f;
                        float margin = 0.13f;
                        MeshTools.AddQuad(verts, colors, tris, x + margin, y + margin, x + width - margin, y + 4.35f,
                            new Color(0.006f, 0.025f, 0.14f, 0.39f));
                        MeshTools.AddQuad(verts, colors, tris, x + 0.24f, y + 0.25f, x + width - 0.21f, y + 4.30f,
                            new Color(0.022f, 0.14f, 0.47f, 0.29f));
                        x += width;
                    }
                }
            }
            tiles = BuildMesh("Parallax panels", 32, verts, colors, tris, out tileRenderer);
        }

        Transform BuildMesh(string name, int depth, List<Vector3> verts, List<Color> colors,
            List<int> tris, out MeshRenderer renderer)
        {
            GameObject layer = MeshTools.CreateRenderer(name, transform, depth);
            layer.GetComponent<MeshFilter>().sharedMesh = MeshTools.Build(verts, colors, tris);
            renderer = layer.GetComponent<MeshRenderer>();
            return layer.transform;
        }

        public void SetMode(PlayerMode mode, bool inverted, bool immediate = false)
        {
            targetTint = inverted ? new Color(1f, 0.61f, 0.78f) :
                mode == PlayerMode.Ship ? new Color(0.52f, 0.92f, 1f) : Color.white;
            if (immediate) tint = targetTint;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 p = cam.transform.position;
            sky.position = new Vector3(p.x, p.y + 1.3f, 40f);
            sky.localScale = new Vector3(1f, Mathf.Max(1f, cam.orthographicSize / 6f), 1f);
            tiles.position = new Vector3(p.x - Mathf.Repeat(p.x * 0.43f, RepeatWidth), p.y * 0.24f, 32f);
            tint = Color.Lerp(tint, targetTint, 1f - Mathf.Exp(-5f * Time.deltaTime));
            tintBlock.SetColor("_Color", tint);
            skyRenderer.SetPropertyBlock(tintBlock);
            tileRenderer.SetPropertyBlock(tintBlock);
        }
    }
}
