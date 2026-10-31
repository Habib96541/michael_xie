using System;
using UnityEngine;

namespace DropStack.Data
{
    public enum PolyominoType
    {
        None = 0,
        // 3-Block Trominoes
        I3 = 1,
        L3 = 2,
        // 4-Block Classic Tetrominoes
        I4 = 3,
        O4 = 4,
        T4 = 5,
        S4 = 6,
        Z4 = 7,
        J4 = 8,
        L4 = 9,
        // 5-Block Pentaminoes / Asymmetric
        P5 = 10,
        U5 = 11,
        T5 = 12,
        V5 = 13,
        W5 = 14,
        X5 = 15,
        I5 = 16
    }

    [Serializable]
    public class PolyominoShape
    {
        public PolyominoType type;
        public string displayName;
        public int blockCount;
        public Color primaryColor;
        public Color glowColor;
        // 4 rotation states [0..3], each state has blockCount coordinates
        public Vector2Int[][] rotations;

        public Vector2Int[] GetBlocks(int rotationIndex)
        {
            if (rotations == null || rotations.Length == 0) return Array.Empty<Vector2Int>();
            int idx = ((rotationIndex % rotations.Length) + rotations.Length) % rotations.Length;
            return rotations[idx];
        }

        public BoundsInt GetBoundingBox(int rotationIndex)
        {
            Vector2Int[] blocks = GetBlocks(rotationIndex);
            if (blocks.Length == 0) return new BoundsInt(0, 0, 0, 0, 0, 0);

            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var b in blocks)
            {
                if (b.x < minX) minX = b.x;
                if (b.x > maxX) maxX = b.x;
                if (b.y < minY) minY = b.y;
                if (b.y > maxY) maxY = b.y;
            }

            return new BoundsInt(minX, minY, 0, maxX - minX + 1, maxY - minY + 1, 1);
        }
    }
}
