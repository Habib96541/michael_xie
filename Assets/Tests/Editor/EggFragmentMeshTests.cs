using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EggDrop.Tests
{
    public sealed class EggFragmentMeshTests
    {
        [TestCaseSource(nameof(FragmentNumbers))]
        public void ShellHasThicknessAndClosedBrokenEdges(int fragment)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Fracture/Shell " + fragment.ToString("00") + ".asset");
            Assert.That(mesh, Is.Not.Null);
            Assert.That(mesh.subMeshCount, Is.EqualTo(3));
            Assert.That(mesh.GetTriangles(2).Length, Is.GreaterThan(0));
            int outerVertexCount = mesh.GetTriangles(0).Max() + 1;
            Vector3[] vertices = mesh.vertices;
            for (int index = 0; index < outerVertexCount; index++)
                Assert.That(Vector3.Distance(vertices[index], vertices[index + outerVertexCount]), Is.EqualTo(0.024f).Within(0.00001f));

            var welded = new Dictionary<Vector3, int>();
            int[] vertexNumbers = new int[vertices.Length];
            for (int index = 0; index < vertices.Length; index++)
            {
                if (!welded.TryGetValue(vertices[index], out int number))
                {
                    number = welded.Count;
                    welded.Add(vertices[index], number);
                }
                vertexNumbers[index] = number;
            }
            var edges = new Dictionary<(int, int), int>();
            int[] triangles = mesh.triangles;
            for (int index = 0; index < triangles.Length; index += 3)
                for (int corner = 0; corner < 3; corner++)
                {
                    int first = vertexNumbers[triangles[index + corner]];
                    int second = vertexNumbers[triangles[index + (corner + 1) % 3]];
                    var edge = (Mathf.Min(first, second), Mathf.Max(first, second));
                    edges[edge] = edges.TryGetValue(edge, out int count) ? count + 1 : 1;
                }
            Assert.That(edges.Values.All(count => count == 2), Is.True, "The shell contains an open or non-manifold edge.");
        }

        private static IEnumerable<int> FragmentNumbers() => Enumerable.Range(1, 10);
    }
}
