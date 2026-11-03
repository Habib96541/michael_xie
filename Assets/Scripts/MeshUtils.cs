using UnityEngine;

/// <summary>Small procedural mesh helpers used by the arena and chests.</summary>
public static class MeshUtils
{
    /// <summary>A flat ring (annulus) in the XZ plane, facing up.</summary>
    public static Mesh Ring(float innerRadius, float outerRadius, int segments = 96)
    {
        segments = Mathf.Max(8, segments);
        var mesh = new Mesh { name = "Ring" };
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            vertices[i * 2] = direction * innerRadius;
            vertices[i * 2 + 1] = direction * outerRadius;
            uvs[i * 2] = new Vector2(i / (float)segments, 0f);
            uvs[i * 2 + 1] = new Vector2(i / (float)segments, 1f);
        }

        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            int b = i * 2 + 1;
            int c = (i + 1) * 2;
            int d = (i + 1) * 2 + 1;
            int t = i * 6;
            triangles[t] = a;
            triangles[t + 1] = c;
            triangles[t + 2] = b;
            triangles[t + 3] = b;
            triangles[t + 4] = c;
            triangles[t + 5] = d;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
