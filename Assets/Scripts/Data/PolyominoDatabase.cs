using System;
using System.Collections.Generic;
using UnityEngine;

namespace DropStack.Data
{
    public static class PolyominoDatabase
    {
        private static readonly Dictionary<PolyominoType, PolyominoShape> Shapes = new();
        private static readonly List<PolyominoType> AllTypes = new();

        static PolyominoDatabase()
        {
            InitializeShapes();
        }

        private static void InitializeShapes()
        {
            Shapes.Clear();
            AllTypes.Clear();

            // 1. I3 (3 blocks line) - Mint Teal
            Register(new PolyominoShape
            {
                type = PolyominoType.I3,
                displayName = "I-Tromino",
                blockCount = 3,
                primaryColor = new Color(0.1f, 0.95f, 0.85f),
                glowColor = new Color(0.3f, 1f, 0.9f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }
                }
            });

            // 2. L3 (3 blocks corner) - Neon Fuchsia
            Register(new PolyominoShape
            {
                type = PolyominoType.L3,
                displayName = "Corner Tromino",
                blockCount = 3,
                primaryColor = new Color(1.0f, 0.15f, 0.65f),
                glowColor = new Color(1.0f, 0.35f, 0.75f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, -1) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(-1, 0) },
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1) }
                }
            });

            // 3. I4 (Tetromino I) - Electric Cyan
            Register(new PolyominoShape
            {
                type = PolyominoType.I4,
                displayName = "I-Tetromino",
                blockCount = 4,
                primaryColor = new Color(0.0f, 0.88f, 1.0f),
                glowColor = new Color(0.2f, 0.95f, 1.0f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }
                }
            });

            // 4. O4 (Tetromino O) - Golden Sun Yellow
            Register(new PolyominoShape
            {
                type = PolyominoType.O4,
                displayName = "O-Tetromino",
                blockCount = 4,
                primaryColor = new Color(1.0f, 0.85f, 0.05f),
                glowColor = new Color(1.0f, 0.92f, 0.3f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) }
                }
            });

            // 5. T4 (Tetromino T) - Cosmic Purple
            Register(new PolyominoShape
            {
                type = PolyominoType.T4,
                displayName = "T-Tetromino",
                blockCount = 4,
                primaryColor = new Color(0.68f, 0.28f, 1.0f),
                glowColor = new Color(0.78f, 0.45f, 1.0f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) }
                }
            });

            // 6. S4 (Tetromino S) - Bright Emerald Green
            Register(new PolyominoShape
            {
                type = PolyominoType.S4,
                displayName = "S-Tetromino",
                blockCount = 4,
                primaryColor = new Color(0.12f, 0.88f, 0.35f),
                glowColor = new Color(0.35f, 0.95f, 0.5f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, -1) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, -1) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 1) }
                }
            });

            // 7. Z4 (Tetromino Z) - Crimson Blaze
            Register(new PolyominoShape
            {
                type = PolyominoType.Z4,
                displayName = "Z-Tetromino",
                blockCount = 4,
                primaryColor = new Color(0.96f, 0.22f, 0.25f),
                glowColor = new Color(1.0f, 0.4f, 0.4f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1) }
                }
            });

            // 8. J4 (Tetromino J) - Royal Sapphire
            Register(new PolyominoShape
            {
                type = PolyominoType.J4,
                displayName = "J-Tetromino",
                blockCount = 4,
                primaryColor = new Color(0.2f, 0.45f, 1.0f),
                glowColor = new Color(0.4f, 0.65f, 1.0f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1) },
                    new[] { new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0) },
                    new[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1) }
                }
            });

            // 9. L4 (Tetromino L) - Vibrant Orange
            Register(new PolyominoShape
            {
                type = PolyominoType.L4,
                displayName = "L-Tetromino",
                blockCount = 4,
                primaryColor = new Color(1.0f, 0.5f, 0.05f),
                glowColor = new Color(1.0f, 0.68f, 0.25f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1) }
                }
            });

            // 10. P5 (Pentamino P) - Lime Neon
            Register(new PolyominoShape
            {
                type = PolyominoType.P5,
                displayName = "P-Pentamino",
                blockCount = 5,
                primaryColor = new Color(0.65f, 0.95f, 0.1f),
                glowColor = new Color(0.78f, 1.0f, 0.35f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(-1, -1) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1) }
                }
            });

            // 11. U5 (Pentamino U / Cup) - Indigo Violet
            Register(new PolyominoShape
            {
                type = PolyominoType.U5,
                displayName = "U-Pentamino",
                blockCount = 5,
                primaryColor = new Color(0.55f, 0.35f, 1.0f),
                glowColor = new Color(0.7f, 0.55f, 1.0f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) },
                    new[] { new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
                    new[] { new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1) },
                    new[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1) }
                }
            });

            // 12. T5 (Pentamino T / Extended T) - Magenta Flare
            Register(new PolyominoShape
            {
                type = PolyominoType.T5,
                displayName = "Tall-T Pentamino",
                blockCount = 5,
                primaryColor = new Color(0.95f, 0.25f, 0.75f),
                glowColor = new Color(1.0f, 0.45f, 0.85f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) },
                    new[] { new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                    new[] { new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(0, -2) },
                    new[] { new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(-2, 0) }
                }
            });

            // 13. V5 (Pentamino V / Corner V) - Sky Cyan
            Register(new PolyominoShape
            {
                type = PolyominoType.V5,
                displayName = "V-Pentamino",
                blockCount = 5,
                primaryColor = new Color(0.05f, 0.72f, 0.95f),
                glowColor = new Color(0.35f, 0.85f, 1.0f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(0, 2), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                    new[] { new Vector2Int(2, 0), new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2) },
                    new[] { new Vector2Int(0, -2), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0) },
                    new[] { new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }
                }
            });

            // 14. W5 (Pentamino W / Stairs) - Warm Amber
            Register(new PolyominoShape
            {
                type = PolyominoType.W5,
                displayName = "W-Pentamino",
                blockCount = 5,
                primaryColor = new Color(1.0f, 0.65f, 0.15f),
                glowColor = new Color(1.0f, 0.78f, 0.35f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-1, 1), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(1, -1) },
                    new[] { new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-1, -1) },
                    new[] { new Vector2Int(1, -1), new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1) },
                    new[] { new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1) }
                }
            });

            // 15. X5 (Pentamino Plus / Cross) - Electric Rose
            Register(new PolyominoShape
            {
                type = PolyominoType.X5,
                displayName = "Cross Pentamino",
                blockCount = 5,
                primaryColor = new Color(1.0f, 0.3f, 0.55f),
                glowColor = new Color(1.0f, 0.5f, 0.7f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) },
                    new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(-1, 0), new Vector2Int(1, 0) }
                }
            });

            // 16. I5 (Pentamino I / 5-Line Clearer!) - Emerald Aqua
            Register(new PolyominoShape
            {
                type = PolyominoType.I5,
                displayName = "I5-Pentamino",
                blockCount = 5,
                primaryColor = new Color(0.05f, 0.95f, 0.65f),
                glowColor = new Color(0.3f, 1.0f, 0.8f, 0.5f),
                rotations = new[]
                {
                    new[] { new Vector2Int(-2, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                    new[] { new Vector2Int(0, 2), new Vector2Int(0, 1), new Vector2Int(0, 0), new Vector2Int(0, -1), new Vector2Int(0, -2) },
                    new[] { new Vector2Int(2, 0), new Vector2Int(1, 0), new Vector2Int(0, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0) },
                    new[] { new Vector2Int(0, -2), new Vector2Int(0, -1), new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2) }
                }
            });
        }

        private static void Register(PolyominoShape shape)
        {
            Shapes[shape.type] = shape;
            AllTypes.Add(shape.type);
        }

        public static PolyominoShape GetShape(PolyominoType type)
        {
            if (Shapes.Count == 0) InitializeShapes();
            return Shapes.TryGetValue(type, out var shape) ? shape : null;
        }

        public static IReadOnlyList<PolyominoType> GetAllTypes()
        {
            if (AllTypes.Count == 0) InitializeShapes();
            return AllTypes;
        }
    }

    /// <summary>
    /// Generates randomized bags of polyominoes ensuring high variety while maintaining balanced stackability.
    /// </summary>
    public class BagRandomizer
    {
        private readonly List<PolyominoType> currentBag = new();
        private readonly System.Random rng = new();
        private IReadOnlyList<PolyominoType> allowedTypes;

        public BagRandomizer()
        {
            RefillBag();
        }

        public void Reset(IReadOnlyList<PolyominoType> types)
        {
            if (types == null || types.Count == 0) throw new System.ArgumentException("A bag needs at least one shape.");
            allowedTypes = types;
            RefillBag();
        }

        private void RefillBag()
        {
            currentBag.Clear();
            var all = allowedTypes ?? PolyominoDatabase.GetAllTypes();
            currentBag.AddRange(all);

            // Fisher-Yates shuffle
            for (int i = currentBag.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (currentBag[i], currentBag[j]) = (currentBag[j], currentBag[i]);
            }
        }

        public PolyominoType Next()
        {
            if (currentBag.Count == 0)
            {
                RefillBag();
            }

            PolyominoType next = currentBag[0];
            currentBag.RemoveAt(0);
            return next;
        }
    }
}
