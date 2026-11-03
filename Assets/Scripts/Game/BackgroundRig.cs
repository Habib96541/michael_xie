using System.Collections.Generic;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// The living backdrop: a vertical gradient, two parallax neon grids and slow dust motes.
    /// All of it follows the camera, which is what sells the sense of speed.
    /// </summary>
    public sealed class BackgroundRig : MonoBehaviour
    {
        Camera cam;
        Fx fx;
        Transform gradient;
        Transform gridFar;
        Transform gridNear;
        float ambientTimer;
        float time;

        public void Build(Camera camera, Fx particleSystem)
        {
            cam = camera;
            fx = particleSystem;
            BuildGradient();
            gridFar = BuildGrid("Grid Far", 4f, Palette.Alpha(Palette.Cyan, 0.055f), 26f);
            gridNear = BuildGrid("Grid Near", 2f, Palette.Alpha(Palette.Magenta, 0.075f), 22f);
        }

        void BuildGradient()
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            MeshTools.AddQuadGradient(verts, colors, tris, -60f, -30f, 60f, 30f,
                Palette.Background, Palette.BackgroundGlow);

            gradient = MeshTools.CreateRenderer("Sky Gradient", transform, 40).transform;
            gradient.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);
        }

        Transform BuildGrid(string name, float spacing, Color color, float z)
        {
            var verts = new List<Vector3>();
            var colors = new List<Color>();
            var tris = new List<int>();

            const float width = 90f;
            const float height = 34f;

            for (float x = -width * 0.5f; x <= width * 0.5f; x += spacing)
                MeshTools.AddQuad(verts, colors, tris, x, -height * 0.5f, x + 0.035f, height * 0.5f, color);
            for (float y = -height * 0.5f; y <= height * 0.5f; y += spacing)
                MeshTools.AddQuad(verts, colors, tris, -width * 0.5f, y, width * 0.5f, y + 0.035f,
                    Palette.Alpha(color, color.a * 0.8f));

            GameObject go = MeshTools.CreateRenderer(name, transform, (int)(50f - z));
            go.transform.position = new Vector3(0f, 0f, z);
            go.GetComponent<MeshFilter>().mesh = MeshTools.Build(verts, colors, tris);
            return go.transform;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            time += Time.deltaTime;

            Vector3 cameraPosition = cam.transform.position;

            if (gradient != null)
                gradient.position = new Vector3(cameraPosition.x, cameraPosition.y, 40f);

            if (gridFar != null)
                gridFar.position = new Vector3(cameraPosition.x * 0.55f, cameraPosition.y * 0.2f, 26f);

            if (gridNear != null)
                gridNear.position = new Vector3(cameraPosition.x * 0.78f + Mathf.Sin(time * 0.2f) * 0.5f,
                                                cameraPosition.y * 0.35f, 22f);

            if (fx == null) return;
            ambientTimer -= Time.deltaTime;
            if (ambientTimer <= 0f)
            {
                ambientTimer = 0.07f;
                Vector2 spawn = new Vector2(cameraPosition.x + 12f, Random.Range(0.5f, 10.5f));
                fx.Ambient(spawn, Palette.Alpha(Palette.Cyan, 0.55f));
            }
        }
    }
}
