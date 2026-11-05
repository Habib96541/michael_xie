using System.Collections.Generic;
using UnityEngine;

namespace HydraulicPress
{
    // Original low-poly production meshes. Faces have actual bevels, not scaled primitive cubes.
    public static class ModelGeometry
    {
        public static Mesh RoundedBox(Vector3 size, float radius = .06f)
        {
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * .45f);
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var triangles = new List<int>();
            Vector3 inner = size * .5f - Vector3.one * radius;
            Vector3[] ns = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (Vector3 n in ns)
            {
                Vector3 u = Mathf.Abs(n.y) > .5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(n, u);
                float a = Vector3.Dot(Abs(u), size) * .5f, b = Vector3.Dot(Abs(v), size) * .5f;
                float depth = Vector3.Dot(Abs(n), size) * .5f;
                float[] xs = { -a, -a + radius, a - radius, a };
                float[] ys = { -b, -b + radius, b - radius, b };
                int start = vertices.Count;
                for (int j = 0; j < 4; j++) for (int i = 0; i < 4; i++)
                {
                    Vector3 p = n * depth + u * xs[i] + v * ys[j];
                    Vector3 q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 normal = (p - q).normalized;
                    vertices.Add(q + normal * radius); normals.Add(normal);
                }
                for (int j = 0; j < 3; j++) for (int i = 0; i < 3; i++)
                {
                    int x = start + j * 4 + i;
                    triangles.AddRange(new[] { x, x + 1, x + 5, x, x + 5, x + 4 });
                }
            }
            var mesh = new Mesh { name = "Beveled steelwork" };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Abs(Vector3 p) => new Vector3(Mathf.Abs(p.x), Mathf.Abs(p.y), Mathf.Abs(p.z));

        public static Mesh Lathe(Vector2[] profile, int sides = 32)
        {
            var vs = new List<Vector3>(); var uv = new List<Vector2>(); var ts = new List<int>();
            for (int j = 0; j < profile.Length; j++) for (int i = 0; i <= sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                vs.Add(new Vector3(Mathf.Cos(a) * profile[j].x, profile[j].y, Mathf.Sin(a) * profile[j].x));
                uv.Add(new Vector2((float)i / sides, (float)j / (profile.Length - 1)));
                if (j > 0 && i < sides)
                {
                    int k = j * (sides + 1) + i;
                    ts.AddRange(new[] { k, k + 1, k - sides - 1, k + 1, k - sides, k - sides - 1 });
                }
            }
            var mesh = new Mesh { name = "Turned profile" };
            mesh.SetVertices(vs); mesh.SetUVs(0, uv); mesh.SetTriangles(ts, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        public static Mesh Ellipsoid(Vector3 size)
        {
            var profile = new Vector2[17];
            for (int j = 0; j <= 16; j++) { float t = j * Mathf.PI / 16; profile[j] = new Vector2(Mathf.Sin(t) * .5f, -Mathf.Cos(t) * .5f); }
            var mesh = Lathe(profile, 28);
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = Vector3.Scale(vertices[i], size);
            mesh.vertices = vertices; mesh.RecalculateNormals(); mesh.RecalculateBounds(); mesh.name = "Sculpted oval"; return mesh;
        }

        public static Mesh Tube(Vector3[] path, float radius, int sides = 8)
        {
            var vs = new List<Vector3>(); var ts = new List<int>();
            for (int j = 0; j < path.Length; j++)
            {
                Vector3 tangent = (path[Mathf.Min(j + 1, path.Length - 1)] - path[Mathf.Max(j - 1, 0)]).normalized;
                Vector3 right = Vector3.Cross(tangent, Mathf.Abs(tangent.y) > .95f ? Vector3.forward : Vector3.up).normalized;
                Vector3 up = Vector3.Cross(right, tangent).normalized;
                for (int i = 0; i < sides; i++)
                {
                    float a = i * Mathf.PI * 2 / sides;
                    vs.Add(path[j] + radius * (right * Mathf.Cos(a) + up * Mathf.Sin(a)));
                    if (j > 0)
                    {
                        int k = j * sides + i, next = j * sides + (i + 1) % sides;
                        ts.AddRange(new[] { k, next, k - sides, next, next - sides, k - sides });
                    }
                }
            }
            var mesh = new Mesh { name = "Bent tubing" }; mesh.SetVertices(vs); mesh.SetTriangles(ts, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }

        public static Mesh Ring(float radius, float thickness, int segments = 32)
        {
            var path = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++) { float a = i * 2 * Mathf.PI / segments; path[i] = new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius); }
            return Tube(path, thickness);
        }

        public static Mesh Shard(int seed)
        {
            var random = new System.Random(seed);
            Vector3[] vs = { new Vector3(-.5f, -.2f, -.4f), new Vector3(.5f, -.25f, -.3f), new Vector3(.2f, -.1f, .5f), new Vector3(-.3f, -.15f, .3f), new Vector3(0, .5f, 0) };
            for (int i = 0; i < vs.Length; i++) vs[i] *= (float)(.7 + random.NextDouble() * .6);
            int[] ts = { 0, 1, 4, 1, 2, 4, 2, 3, 4, 3, 0, 4, 0, 3, 2, 0, 2, 1 };
            var mesh = new Mesh { name = "Fracture " + seed, vertices = vs, triangles = ts }; mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
    }
}
