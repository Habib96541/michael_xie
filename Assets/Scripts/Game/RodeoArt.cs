using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace BullRide
{
    internal sealed class RodeoArt
    {
        // Exported from the embedded Bull game assets. Meshes use position, normal and vertex color.
        readonly byte[] bullData;
        readonly Part[] arena, rider;
        readonly (Vector3 center, Vector3 size)[] railBoxes, gateBoxes;
        readonly Material[] materials = new Material[10];
        readonly Material shadow;
        static RodeoArt instance;
        public static RodeoArt Shared => instance ??= new RodeoArt();

        sealed class Part
        {
            public string Name;
            public int Parent;
            public Vector3 Position, Euler;
            public (Mesh mesh, int material)[] Meshes;
        }

        RodeoArt()
        {
            TextAsset art = Resources.Load<TextAsset>("BullRideArt");
            if (art == null) throw new InvalidOperationException("BullRideArt is missing.");
            using var reader = new BinaryReader(new MemoryStream(art.bytes));
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RDO1")
                throw new InvalidDataException("Unrecognized rodeo art data.");
            bullData = reader.ReadBytes(reader.ReadInt32());
            arena = ReadParts(reader);
            rider = ReadParts(reader);
            railBoxes = ReadBoxes(reader);
            gateBoxes = ReadBoxes(reader);
            shadow = new Material(Resources.Load<Shader>("RodeoShadow")) { name = "Rodeo planar shadow" };
            Shader shader = Resources.Load<Shader>("RodeoSurface");
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = new Material(shader) { name = "Rodeo surface " + i };
                materials[i].SetFloat("_Style", i);
            }
        }

        public Dictionary<string, Transform> BuildArena(Transform parent)
        {
            var nodes = BuildParts(arena, parent);
            AddBoxes(parent, railBoxes);
            AddBoxes(nodes["Gate"], gateBoxes);
            return nodes;
        }

        static void AddBoxes(Transform parent, (Vector3 center, Vector3 size)[] boxes)
        {
            foreach (var (center, size) in boxes)
            {
                var collider = parent.gameObject.AddComponent<BoxCollider>();
                collider.center = center;
                collider.size = size;
            }
        }

        static (Vector3, Vector3)[] ReadBoxes(BinaryReader reader)
        {
            if (reader.BaseStream.Position == reader.BaseStream.Length)
                return Array.Empty<(Vector3, Vector3)>();
            var boxes = new (Vector3, Vector3)[reader.ReadInt32()];
            for (int i = 0; i < boxes.Length; i++) boxes[i] = (ReadVector(reader), ReadVector(reader));
            return boxes;
        }
        public Dictionary<string, Transform> BuildRider(Transform parent) => BuildParts(rider, parent);
        public Material RopeMaterial => materials[0];

        Dictionary<string, Transform> BuildParts(Part[] parts, Transform parent)
        {
            var names = new Dictionary<string, Transform>();
            var nodes = new Transform[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                Part part = parts[i];
                Transform node = new GameObject(part.Name).transform;
                nodes[i] = node;
                names[part.Name] = node;
                node.SetParent(part.Parent < 0 ? parent : nodes[part.Parent], false);
                node.localPosition = part.Position;
                node.localRotation = Rotation(part.Euler);
                foreach (var (mesh, material) in part.Meshes)
                {
                    var visual = new GameObject(mesh.name);
                    visual.transform.SetParent(node, false);
                    visual.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var renderer = visual.AddComponent<MeshRenderer>();
                    SetMaterials(renderer, material, material == 0 || material == 2 || material == 4 || material == 5);
                }
            }
            return names;
        }

        void SetMaterials(Renderer renderer, int surface, bool castPlanarShadow)
        {
            renderer.sharedMaterials = castPlanarShadow ? new[] { materials[surface], shadow } : new[] { materials[surface] };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        public (Transform[] bones, Quaternion[] rest) BuildBull(Transform owner)
        {
            using var reader = new BinaryReader(new MemoryStream(bullData));
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "BULL")
                throw new InvalidDataException("Unrecognized bull mesh data.");
            int vertexCount = reader.ReadUInt16(), indexCount = reader.ReadUInt16(), jointCount = reader.ReadUInt16();
            reader.ReadUInt16();
            var mesh = new Mesh { name = "Original skinned bull" };
            mesh.vertices = ReadVectors(reader, vertexCount);
            mesh.normals = ReadVectors(reader, vertexCount);
            mesh.colors = ReadColors(reader, vertexCount);
            byte[] joints = reader.ReadBytes(vertexCount * 4);
            var weights = new BoneWeight[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                int j = i * 4;
                weights[i] = new BoneWeight
                {
                    boneIndex0 = joints[j], boneIndex1 = joints[j + 1], boneIndex2 = joints[j + 2], boneIndex3 = joints[j + 3],
                    weight0 = reader.ReadSingle(), weight1 = reader.ReadSingle(), weight2 = reader.ReadSingle(), weight3 = reader.ReadSingle()
                };
            }
            mesh.triangles = ReadIndices(reader, indexCount);
            var bones = new Transform[jointCount];
            var rest = new Quaternion[jointCount];
            var bind = new Matrix4x4[jointCount];
            for (int i = 0; i < jointCount; i++)
            {
                int parent = reader.ReadInt16();
                reader.ReadInt16();
                var bone = new GameObject("Joint " + i).transform;
                bone.SetParent(parent < 0 ? owner : bones[parent], false);
                bone.localPosition = ReadVector(reader);
                rest[i] = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()).normalized;
                bone.localRotation = rest[i];
                Matrix4x4 inverse = new Matrix4x4();
                for (int n = 0; n < 16; n++) inverse[n] = reader.ReadSingle();
                bind[i] = inverse;
                bone.localScale = Vector3.one * reader.ReadSingle();
                bones[i] = bone;
            }

            // The source hoof controls are separate roots. Keep their bind transforms when
            // attaching them to the matching ankle so the skinned hooves follow the leg.
            bones[34].SetParent(bones[18], true);
            bones[36].SetParent(bones[11], true);
            bones[38].SetParent(bones[22], true);
            bones[40].SetParent(bones[14], true);
            mesh.bindposes = bind;
            mesh.boneWeights = weights;
            var renderer = owner.gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = bones;
            renderer.rootBone = owner;
            renderer.localBounds = new Bounds(Vector3.up, new Vector3(4, 4, 5));
            SetMaterials(renderer, 7, true);
            return (bones, rest);
        }

        static Part[] ReadParts(BinaryReader reader)
        {
            var parts = new Part[reader.ReadInt32()];
            for (int i = 0; i < parts.Length; i++)
            {
                var part = new Part
                {
                    Name = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32())),
                    Parent = reader.ReadInt32(),
                    Position = ReadVector(reader),
                    Euler = ReadVector(reader),
                    Meshes = new (Mesh, int)[reader.ReadInt32()]
                };
                for (int j = 0; j < part.Meshes.Length; j++)
                {
                    int material = reader.ReadInt32(), count = reader.ReadInt32(), indices = reader.ReadInt32();
                    var mesh = new Mesh { name = part.Name + " mesh " + j };
                    mesh.vertices = ReadVectors(reader, count);
                    mesh.normals = ReadVectors(reader, count);
                    mesh.colors = ReadColors(reader, count);
                    mesh.triangles = ReadIndices(reader, indices);
                    part.Meshes[j] = (mesh, material);
                }
                parts[i] = part;
            }
            return parts;
        }

        public static Quaternion Rotation(Vector3 e) => Quaternion.Euler(0, e.y, 0) *
            Quaternion.Euler(e.x, 0, 0) * Quaternion.Euler(0, 0, e.z);

        static Vector3 ReadVector(BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        static Vector3[] ReadVectors(BinaryReader reader, int count)
        {
            var values = new Vector3[count];
            for (int i = 0; i < count; i++) values[i] = ReadVector(reader);
            return values;
        }

        static Color[] ReadColors(BinaryReader reader, int count)
        {
            var values = new Color[count];
            for (int i = 0; i < count; i++)
                values[i] = new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            return values;
        }

        static int[] ReadIndices(BinaryReader reader, int count)
        {
            var values = new int[count];
            for (int i = 0; i < count; i++) values[i] = reader.ReadUInt16();
            return values;
        }
    }
}

