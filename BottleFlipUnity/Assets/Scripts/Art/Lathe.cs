using System.Collections.Generic;
using UnityEngine;

namespace BottleFlip
{
    /// <summary>
    /// Builds a surface of revolution from a 2D profile (x = radius, y = height).
    /// Winding is resolved automatically so faces always point away from the axis.
    /// </summary>
    public static class Lathe
    {
        public static Mesh Build(IList<Vector2> profile, int segments, string name, bool capBottom = true, bool capTop = true)
        {
            segments = Mathf.Max(3, segments);
            int rings = profile.Count;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            // Ring normals derived from the profile tangent (smooth shading across rings).
            var ringNormals = new Vector3[rings];
            for (int i = 0; i < rings; i++)
            {
                Vector2 tangent;
                if (i == 0) tangent = profile[1] - profile[0];
                else if (i == rings - 1) tangent = profile[i] - profile[i - 1];
                else tangent = profile[i + 1] - profile[i - 1];

                if (tangent.sqrMagnitude < 1e-8f) tangent = Vector2.up;

                // Outward normal of a profile point: rotate tangent by -90 degrees.
                Vector2 n2 = new Vector2(tangent.y, -tangent.x).normalized;
                ringNormals[i] = new Vector3(n2.x, n2.y, 0f);
            }

            // Arc length for V coordinates.
            var arc = new float[rings];
            for (int i = 1; i < rings; i++) arc[i] = arc[i - 1] + Vector2.Distance(profile[i], profile[i - 1]);
            float totalArc = Mathf.Max(0.0001f, arc[rings - 1]);

            for (int i = 0; i < rings; i++)
            {
                float v = arc[i] / totalArc;
                for (int j = 0; j <= segments; j++)
                {
                    float a = (j / (float)segments) * Mathf.PI * 2f;
                    float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                    float r = profile[i].x;
                    verts.Add(new Vector3(r * cos, profile[i].y, r * sin));
                    norms.Add(new Vector3(ringNormals[i].x * cos, ringNormals[i].y, ringNormals[i].x * sin).normalized);
                    uvs.Add(new Vector2(j / (float)segments, v));
                }
            }

            for (int i = 0; i < rings - 1; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int a = i * (segments + 1) + j;
                    int b = a + 1;
                    int c = (i + 1) * (segments + 1) + j;
                    int d = c + 1;
                    AddQuad(verts, tris, a, b, c, d, Outward(verts, a, c));
                }
            }

            if (capBottom && profile[0].x > 0.0005f)
                AddCap(verts, norms, uvs, tris, profile[0].x, profile[0].y, Vector3.down, segments);
            if (capTop && profile[rings - 1].x > 0.0005f)
                AddCap(verts, norms, uvs, tris, profile[rings - 1].x, profile[rings - 1].y, Vector3.up, segments);

            var mesh = new Mesh { name = name };
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static Vector3 Outward(List<Vector3> verts, int a, int d)
        {
            Vector3 mid = (verts[a] + verts[d]) * 0.5f;
            mid.y = 0f;
            return mid.sqrMagnitude < 1e-6f ? Vector3.up : mid.normalized;
        }

        static void AddQuad(List<Vector3> verts, List<int> tris, int a, int b, int c, int d, Vector3 outward)
        {
            AddTri(verts, tris, a, b, d, outward);
            AddTri(verts, tris, a, d, c, outward);
        }

        static void AddTri(List<Vector3> verts, List<int> tris, int a, int b, int c, Vector3 outward)
        {
            Vector3 n = Vector3.Cross(verts[b] - verts[a], verts[c] - verts[a]);
            if (n.sqrMagnitude < 1e-10f) return;
            if (Vector3.Dot(n, outward) < 0f)
            {
                int tmp = b; b = c; c = tmp;
            }
            tris.Add(a); tris.Add(b); tris.Add(c);
        }

        static void AddCap(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs, List<int> tris,
                           float radius, float y, Vector3 normal, int segments)
        {
            int center = verts.Count;
            verts.Add(new Vector3(0f, y, 0f));
            norms.Add(normal);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int start = verts.Count;
            for (int j = 0; j <= segments; j++)
            {
                float a = (j / (float)segments) * Mathf.PI * 2f;
                verts.Add(new Vector3(radius * Mathf.Cos(a), y, radius * Mathf.Sin(a)));
                norms.Add(normal);
                uvs.Add(new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a)));
            }

            for (int j = 0; j < segments; j++)
                AddTri(verts, tris, center, start + j, start + j + 1, normal);
        }
    }

    /// <summary>Round trunk / soft blob helpers built on top of Lathe.</summary>
    public static class Shapes
    {
        public static Mesh Cylinder(float radius, float height, float bevel, int segments, string name)
        {
            var p = new List<Vector2>();
            if (bevel <= 0.001f)
            {
                p.Add(new Vector2(radius, 0f));
                p.Add(new Vector2(radius, height));
            }
            else
            {
                p.Add(new Vector2(radius - bevel, 0f));
                p.Add(new Vector2(radius, bevel * 0.5f));
                p.Add(new Vector2(radius, height - bevel * 0.5f));
                p.Add(new Vector2(radius - bevel, height));
            }
            return Lathe.Build(p, segments, name);
        }

        public static Mesh Ring(float radius, float thickness, int segments, string name)
        {
            float r0 = radius - thickness * 0.5f;
            float r1 = radius + thickness * 0.5f;
            float h = thickness * 0.5f;
            var p = new List<Vector2>
            {
                new Vector2(r0, 0f),
                new Vector2(r1, 0f),
                new Vector2(r1, h),
                new Vector2(r0, h),
                new Vector2(r0, 0f)
            };
            // No caps: this is a hollow band, not a disc.
            return Lathe.Build(p, segments, name, false, false);
        }
    }
}
