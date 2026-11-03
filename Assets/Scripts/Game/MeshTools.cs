using System.Collections.Generic;
using UnityEngine;

namespace NeonDash
{
    /// <summary>
    /// Procedural geometry helpers. The game ships with no imported art: every block, spike,
    /// portal and particle is a mesh built from these primitives at runtime.
    /// </summary>
    public static class MeshTools
    {
        static Material unlitMaterial;

        /// <summary>Unlit vertex-coloured material, shared by every generated mesh.</summary>
        public static Material NeonMaterial
        {
            get
            {
                if (unlitMaterial == null)
                {
                    // Prefer the material asset created by the editor setup: a material asset
                    // guarantees the shader is not stripped from a player build (WebGL included).
                    Material stored = Resources.Load<Material>("NeonUnlit");
                    if (stored != null)
                    {
                        unlitMaterial = stored;
                    }
                    else
                    {
                        Shader shader = Shader.Find("Sprites/Default");
                        if (shader == null) shader = Shader.Find("Unlit/Color");
                        unlitMaterial = new Material(shader);
                        unlitMaterial.name = "Neon Unlit";
                    }
                }
                return unlitMaterial;
            }
        }

        public static GameObject CreateRenderer(string name, Transform parent, int order)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = NeonMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            filter.sharedMesh = new Mesh();
            filter.sharedMesh.name = name;
            go.transform.position = new Vector3(0f, 0f, order);
            return go;
        }

        public static void AddQuad(List<Vector3> verts, List<Color> colors, List<int> tris,
                                   float x0, float y0, float x1, float y1, Color color)
        {
            int start = verts.Count;
            verts.Add(new Vector3(x0, y0, 0f));
            verts.Add(new Vector3(x1, y0, 0f));
            verts.Add(new Vector3(x1, y1, 0f));
            verts.Add(new Vector3(x0, y1, 0f));
            for (int i = 0; i < 4; i++) colors.Add(color);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
        }

        public static void AddQuadGradient(List<Vector3> verts, List<Color> colors, List<int> tris,
                                           float x0, float y0, float x1, float y1, Color bottom, Color top)
        {
            int start = verts.Count;
            verts.Add(new Vector3(x0, y0, 0f));
            verts.Add(new Vector3(x1, y0, 0f));
            verts.Add(new Vector3(x1, y1, 0f));
            verts.Add(new Vector3(x0, y1, 0f));
            colors.Add(bottom); colors.Add(bottom); colors.Add(top); colors.Add(top);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
        }

        public static void AddTriangle(List<Vector3> verts, List<Color> colors, List<int> tris,
                                       float ax, float ay, float bx, float by, float cx, float cy, Color color)
        {
            int start = verts.Count;
            verts.Add(new Vector3(ax, ay, 0f));
            verts.Add(new Vector3(bx, by, 0f));
            verts.Add(new Vector3(cx, cy, 0f));
            colors.Add(color); colors.Add(color); colors.Add(color);
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
        }

        /// <summary>Adds a filled circle made of a fan of triangles.</summary>
        public static void AddCircle(List<Vector3> verts, List<Color> colors, List<int> tris,
                                     float cx, float cy, float radius, int segments, Color color)
        {
            if (segments < 3) segments = 3;
            int center = verts.Count;
            verts.Add(new Vector3(cx, cy, 0f));
            colors.Add(color);
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                verts.Add(new Vector3(cx + Mathf.Cos(angle) * radius, cy + Mathf.Sin(angle) * radius, 0f));
                colors.Add(color);
            }
            for (int i = 0; i < segments; i++)
            {
                tris.Add(center); tris.Add(center + 1 + i); tris.Add(center + 2 + i);
            }
        }

        /// <summary>Adds an annulus (ring) used for portals and orb halos.</summary>
        public static void AddRing(List<Vector3> verts, List<Color> colors, List<int> tris,
                                   float cx, float cy, float inner, float outer, int segments, Color color)
        {
            if (segments < 3) segments = 3;
            int start = verts.Count;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                verts.Add(new Vector3(cx + cos * inner, cy + sin * inner, 0f));
                verts.Add(new Vector3(cx + cos * outer, cy + sin * outer, 0f));
                colors.Add(color);
                colors.Add(color);
            }
            for (int i = 0; i < segments; i++)
            {
                int a = start + i * 2;
                tris.Add(a); tris.Add(a + 1); tris.Add(a + 3);
                tris.Add(a); tris.Add(a + 3); tris.Add(a + 2);
            }
        }

        /// <summary>Adds a thick line segment as a quad.</summary>
        public static void AddLine(List<Vector3> verts, List<Color> colors, List<int> tris,
                                   float x0, float y0, float x1, float y1, float thickness, Color color)
        {
            float dx = x1 - x0;
            float dy = y1 - y0;
            float length = Mathf.Sqrt(dx * dx + dy * dy);
            if (length < 0.0001f) return;
            float nx = -dy / length * thickness * 0.5f;
            float ny = dx / length * thickness * 0.5f;
            int start = verts.Count;
            verts.Add(new Vector3(x0 + nx, y0 + ny, 0f));
            verts.Add(new Vector3(x1 + nx, y1 + ny, 0f));
            verts.Add(new Vector3(x1 - nx, y1 - ny, 0f));
            verts.Add(new Vector3(x0 - nx, y0 - ny, 0f));
            for (int i = 0; i < 4; i++) colors.Add(color);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 1);
            tris.Add(start); tris.Add(start + 3); tris.Add(start + 2);
        }

        public static Mesh Build(List<Vector3> verts, List<Color> colors, List<int> tris)
        {
            var mesh = new Mesh();
            mesh.indexFormat = verts.Count > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(verts);
            mesh.SetColors(colors);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Creates a mesh object parented under <paramref name="parent"/>.</summary>
        public static MeshRenderer CreateMeshObject(string name, Transform parent, int order, out MeshFilter filter)
        {
            GameObject go = CreateRenderer(name, parent, order);
            filter = go.GetComponent<MeshFilter>();
            return go.GetComponent<MeshRenderer>();
        }

        public static void Clear(Mesh mesh)
        {
            if (mesh != null) mesh.Clear();
        }
    }
}
