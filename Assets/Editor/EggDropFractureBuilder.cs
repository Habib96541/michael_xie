using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace EggDrop.Editor
{
    public static class EggDropFractureBuilder
    {
        private const int FragmentCount = 10;
        private const float Thickness = 0.024f;
        private const string Folder = "Assets/Art/Fracture";

        [MenuItem("Egg Drop/Upgrade Crack Effect")]
        public static void UpgradeScene()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before upgrading the egg.");
            Scene current = SceneManager.GetActiveScene();
            if (current.path == EggDropSceneBuilder.ScenePath && current.isDirty)
                throw new InvalidOperationException("Save the game scene before upgrading the egg.");
            bool alreadyOpen = current.path == EggDropSceneBuilder.ScenePath;
            Scene scene = alreadyOpen ? current : EditorSceneManager.OpenScene(EggDropSceneBuilder.ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                EggBody egg = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EggBody>(true)).Single();
                EggFracture existing = egg.GetComponentInChildren<EggFracture>(true);
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
                EggFracture fracture = CreateEffect(egg.transform);
                var serialized = new SerializedObject(egg);
                serialized.FindProperty("fracture").objectReferenceValue = fracture;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                foreach (string name in new[] { "Upper shell", "Lower shell", "Yolk" })
                {
                    Transform previous = egg.transform.Find(name);
                    if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
                }
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("Egg Drop upgraded with ten shell fragments and an egg-white and yolk spill.");
            }
            finally
            {
                if (!alreadyOpen)
                {
                    SceneManager.SetActiveScene(current);
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        internal static EggFracture CreateEffect(Transform egg)
        {
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            Material outer = Material("Shell Exterior", new Color(0.93f, 0.90f, 0.80f), 0.28f);
            Material inner = Material("Shell Interior", new Color(0.94f, 0.94f, 0.90f), 0.15f);
            Material edge = Material("Broken Edge", new Color(0.78f, 0.76f, 0.68f), 0.1f);
            Material white = Material("Egg White", new Color(0.92f, 0.95f, 0.88f, 0.55f), 0.88f);
            Material yolkMaterial = Material("Spilled Yolk", new Color(1f, 0.76f, 0.18f), 0.85f);
            white.SetFloat("_Surface", 1f);
            white.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            white.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            white.SetFloat("_ZWrite", 0f);
            white.SetOverrideTag("RenderType", "Transparent");
            white.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            white.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(white);
            PhysicsMaterial friction = LoadFriction();
            GameObject root = new GameObject("Fracture Effect");
            root.transform.SetParent(egg, false);
            EggFracture effect = root.AddComponent<EggFracture>();
            Mesh[] shells = CreateFragments(32, 48, "Shell", out Vector3[] centers);
            Rigidbody[] bodies = new Rigidbody[FragmentCount];
            for (int index = 0; index < FragmentCount; index++)
            {
                GameObject part = MeshObject("Shell Fragment " + (index + 1).ToString("00"), shells[index], new[] { outer, inner, edge }, root.transform);
                part.transform.localPosition = centers[index];
                GameObject collisionObject = new GameObject("Collision Shape");
                collisionObject.transform.SetParent(part.transform, false);
                MeshCollider offsetCollider = collisionObject.AddComponent<MeshCollider>();
                offsetCollider.sharedMesh = CollisionMesh(shells[index], "Collision " + (index + 1).ToString("00"));
                offsetCollider.convex = true;
                offsetCollider.sharedMaterial = friction;
                Rigidbody body = part.AddComponent<Rigidbody>();
                body.mass = 0.008f;
                body.isKinematic = true;
                body.linearDamping = 1.2f;
                body.angularDamping = 2.8f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.maxDepenetrationVelocity = 1.5f;
                body.sleepThreshold = 0.002f;
                bodies[index] = body;
                part.SetActive(false);
            }
            GameObject eggWhite = MeshObject("Egg White", PuddleMesh("Egg White Puddle", 1.12f, 0.82f, 0.006f), new[] { white }, root.transform);
            eggWhite.transform.localPosition = new Vector3(0f, -0.678f, 0f);
            GameObject yolk = MeshObject("Yolk", PuddleMesh("Yolk Puddle", 0.55f, 0.42f, 0.08f), new[] { yolkMaterial }, root.transform);
            yolk.transform.localPosition = new Vector3(0.13f, -0.669f, -0.08f);
            for (int index = 0; index < 5; index++)
            {
                float angle = index * 2.399963f;
                GameObject droplet = MeshObject("White Droplet " + index, PuddleMesh("White Droplet", 0.11f, 0.07f, 0.006f), new[] { white }, eggWhite.transform);
                droplet.transform.localPosition = new Vector3(Mathf.Cos(angle) * (1.1f + index * 0.035f), 0.001f, Mathf.Sin(angle) * 0.78f);
                droplet.transform.localRotation = Quaternion.Euler(0, index * 43, 0);
            }
            eggWhite.SetActive(false);
            yolk.SetActive(false);
            var serializedEffect = new SerializedObject(effect);
            SerializedProperty fragments = serializedEffect.FindProperty("fragments");
            fragments.arraySize = bodies.Length;
            for (int index = 0; index < bodies.Length; index++) fragments.GetArrayElementAtIndex(index).objectReferenceValue = bodies[index];
            serializedEffect.FindProperty("eggWhite").objectReferenceValue = eggWhite.transform;
            serializedEffect.FindProperty("yolk").objectReferenceValue = yolk.transform;
            serializedEffect.ApplyModifiedPropertiesWithoutUndo();
            return effect;
        }

        private static Mesh[] CreateFragments(int rings, int segments, string prefix, out Vector3[] centers)
        {
            var vertices = new List<Vector3> { new Vector3(0, 0.65f, 0) };
            for (int ring = 1; ring < rings; ring++)
            {
                float theta = ring * Mathf.PI / rings;
                float radius = Mathf.Sin(theta) * 0.46f * (1f - 0.21f * Mathf.Cos(theta));
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Cos(theta) * 0.65f, Mathf.Sin(angle) * radius));
                }
            }
            int bottom = vertices.Count;
            vertices.Add(new Vector3(0, -0.65f, 0));
            var faces = new List<int>();
            for (int segment = 0; segment < segments; segment++)
            {
                int next = (segment + 1) % segments;
                faces.AddRange(new[] { 0, 1 + next, 1 + segment });
                for (int ring = 0; ring < rings - 2; ring++)
                {
                    int first = 1 + ring * segments + segment;
                    int adjacent = 1 + ring * segments + next;
                    faces.AddRange(new[] { first, adjacent, first + segments, adjacent, adjacent + segments, first + segments });
                }
                faces.AddRange(new[] { 1 + (rings - 2) * segments + segment, 1 + (rings - 2) * segments + next, bottom });
            }
            var source = new Mesh { vertices = vertices.ToArray(), triangles = faces.ToArray() };
            source.RecalculateNormals();
            Vector3[] normals = source.normals;
            int[] labels = new int[faces.Count / 3];
            for (int triangle = 0; triangle < faces.Count; triangle += 3)
            {
                Vector3 direction = (vertices[faces[triangle]] + vertices[faces[triangle + 1]] + vertices[faces[triangle + 2]]).normalized;
                int closest = 0;
                float best = float.NegativeInfinity;
                for (int seed = 0; seed < FragmentCount; seed++)
                {
                    float height = 1f - 2f * (seed + 0.5f) / FragmentCount;
                    float radius = Mathf.Sqrt(1f - height * height);
                    float angle = seed * 2.399963f + 0.16f * Mathf.Sin(seed * 3.1f);
                    Vector3 seedDirection = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
                    float score = Vector3.Dot(direction, seedDirection);
                    if (score <= best) continue;
                    best = score;
                    closest = seed;
                }
                labels[triangle / 3] = closest;
            }
            SmoothPartition(faces, labels);
            var groups = Enumerable.Range(0, FragmentCount).Select(index => new List<int>()).ToArray();
            for (int triangle = 0; triangle < labels.Length; triangle++)
                groups[labels[triangle]].AddRange(new[] { faces[triangle * 3], faces[triangle * 3 + 1], faces[triangle * 3 + 2] });
            centers = new Vector3[FragmentCount];
            Mesh[] meshes = new Mesh[FragmentCount];
            for (int index = 0; index < FragmentCount; index++)
                meshes[index] = ClosedShell(prefix + " " + (index + 1).ToString("00"), vertices, normals, groups[index], out centers[index]);
            UnityEngine.Object.DestroyImmediate(source);
            return meshes;
        }

        private static void SmoothPartition(List<int> faces, int[] labels)
        {
            var edges = new Dictionary<(int, int), int>();
            var neighbors = Enumerable.Range(0, labels.Length).Select(index => new List<int>()).ToArray();
            for (int triangle = 0; triangle < labels.Length; triangle++)
                for (int corner = 0; corner < 3; corner++)
                {
                    int first = faces[triangle * 3 + corner];
                    int second = faces[triangle * 3 + (corner + 1) % 3];
                    var key = (Mathf.Min(first, second), Mathf.Max(first, second));
                    if (!edges.TryGetValue(key, out int other)) { edges.Add(key, triangle); continue; }
                    neighbors[triangle].Add(other);
                    neighbors[other].Add(triangle);
                }
            for (int pass = 0; pass < 3; pass++)
                for (int triangle = 0; triangle < labels.Length; triangle++)
                {
                    int first = labels[neighbors[triangle][0]];
                    int second = labels[neighbors[triangle][1]];
                    int third = labels[neighbors[triangle][2]];
                    if (first == second || first == third) labels[triangle] = first;
                    else if (second == third) labels[triangle] = second;
                }
        }

        private static Mesh ClosedShell(string name, List<Vector3> source, Vector3[] normals, List<int> faces, out Vector3 center)
        {
            int[] used = faces.Distinct().ToArray();
            Vector3 average = Vector3.zero;
            foreach (int vertex in used) average += source[vertex];
            center = average / used.Length;
            var vertices = new List<Vector3>();
            var mapping = new Dictionary<int, int>();
            for (int index = 0; index < used.Length; index++)
            {
                mapping.Add(used[index], index);
                vertices.Add(source[used[index]] - center);
            }
            foreach (int vertex in used) vertices.Add(source[vertex] - normals[vertex] * Thickness - center);
            var outer = new List<int>();
            var inner = new List<int>();
            var rim = new List<int>();
            var boundary = new Dictionary<(int, int), (int first, int second, int count)>();
            for (int triangle = 0; triangle < faces.Count; triangle += 3)
            {
                int first = mapping[faces[triangle]];
                int second = mapping[faces[triangle + 1]];
                int third = mapping[faces[triangle + 2]];
                outer.AddRange(new[] { first, second, third });
                inner.AddRange(new[] { first + used.Length, third + used.Length, second + used.Length });
                AddEdge(first, second);
                AddEdge(second, third);
                AddEdge(third, first);
            }
            foreach (var edge in boundary.Values)
            {
                if (edge.count != 1) continue;
                int start = vertices.Count;
                vertices.AddRange(new[] { vertices[edge.first], vertices[edge.first + used.Length], vertices[edge.second + used.Length], vertices[edge.second] });
                rim.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }
            Mesh mesh = new Mesh { name = name, vertices = vertices.ToArray(), subMeshCount = 3 };
            mesh.SetTriangles(outer, 0);
            mesh.SetTriangles(inner, 1);
            mesh.SetTriangles(rim, 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);

            void AddEdge(int first, int second)
            {
                var key = (Mathf.Min(first, second), Mathf.Max(first, second));
                boundary[key] = boundary.TryGetValue(key, out var existing)
                    ? (existing.first, existing.second, existing.count + 1) : (first, second, 1);
            }
        }

        private static Mesh CollisionMesh(Mesh shell, string name)
        {
            Vector3[] surface = shell.vertices;
            var points = new List<Vector3>();
            var directions = new List<Vector3> { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int index = 0; index < 96; index++)
            {
                float height = 1f - 2f * (index + 0.5f) / 96f;
                float radius = Mathf.Sqrt(1f - height * height);
                float angle = index * 2.399963f;
                directions.Add(new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius));
            }
            foreach (Vector3 direction in directions)
            {
                Vector3 extreme = surface[0];
                foreach (Vector3 point in surface)
                    if (Vector3.Dot(point, direction) > Vector3.Dot(extreme, direction)) extreme = point;
                points.Add(extreme + direction * 0.025f);
            }
            var faces = new List<int>();
            for (int index = 1; index < points.Count - 1; index++) faces.AddRange(new[] { 0, index, index + 1 });
            Mesh mesh = new Mesh { name = name, vertices = points.ToArray(), triangles = faces.ToArray() };
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        private static Mesh PuddleMesh(string name, float width, float depth, float height)
        {
            const int segments = 48;
            var vertices = new List<Vector3> { new Vector3(0, height, 0) };
            var triangles = new List<int>();
            for (int ring = 1; ring <= 2; ring++)
                for (int index = 0; index < segments; index++)
                {
                    float angle = index * Mathf.PI * 2 / segments;
                    float radius = ring * 0.5f * (1f + 0.08f * Mathf.Sin(angle * 5f + 0.7f) + 0.045f * Mathf.Cos(angle * 9f));
                    vertices.Add(new Vector3(Mathf.Cos(angle) * radius * width, ring == 1 ? height * 0.75f : 0f, Mathf.Sin(angle) * radius * depth));
                }
            for (int index = 0; index < segments; index++)
            {
                int first = index + 1;
                int next = (index + 1) % segments + 1;
                triangles.AddRange(new[] { 0, next, first, first, next, first + segments, next, next + segments, first + segments });
            }
            Mesh mesh = new Mesh { name = name, vertices = vertices.ToArray(), triangles = triangles.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return SaveMesh(mesh, name);
        }

        private static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = Folder + "/" + name + ".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) { AssetDatabase.CreateAsset(mesh, path); return mesh; }
            EditorUtility.CopySerialized(mesh, existing);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            return existing;
        }

        private static Material Material(string name, Color color, float smoothness)
        {
            string path = Folder + "/" + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color.linear;
                existing.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color.linear };
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PhysicsMaterial LoadFriction()
        {
            string path = Folder + "/Shell Friction.physicMaterial";
            PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material != null) return material;
            material = new PhysicsMaterial("Shell Friction") { dynamicFriction = 0.8f, staticFriction = 0.9f,
                bounciness = 0.02f, frictionCombine = PhysicsMaterialCombine.Maximum };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject MeshObject(string name, Mesh mesh, Material[] materials, Transform parent)
        {
            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<MeshRenderer>().sharedMaterials = materials;
            return instance;
        }
    }
}
