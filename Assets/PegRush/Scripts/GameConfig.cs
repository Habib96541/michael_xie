using System;
using System.Collections.Generic;
using UnityEngine;

namespace PegRush
{
    [Serializable]
    public sealed class RewardTable
    {
        public int Rows;
        public int Profile;
        public int[] Hundredths;
    }

    [CreateAssetMenu(menuName = "PegRush/Configuration")]
    public sealed class GameConfig : ScriptableObject
    {
        public int Capacity = 50;
        public float Gravity = 1.6f;
        public float Timeout = 35f;
        public float PegDiameter = 0.21f;
        public float BallDiameter = 0.28f;
        public float RowSpacing = 1f;
        public float Restitution = 0.58f;
        public float Friction = 0.05f;
        public float Damping = 0f;
        public float SpawnSpread = 0.16f;
        public float SpawnAbovePeg = 1.1f;
        public int TopPegCount = 1;
        public float BoundaryPadding = 0.35f;

        public float TopY(int rows) => 2f + (rows - 1) * RowSpacing;
        public float SpawnY(int rows) => TopY(rows) + SpawnAbovePeg;
        public List<RewardTable> Tables = new List<RewardTable>();

        public int[] Table(int rows, int profile)
        {
            return Tables.Find(t => t.Rows == rows && t.Profile == profile).Hundredths;
        }

        public void Validate()
        {
            for (int rows = 8; rows <= 16; rows++)
            {
                for (int profile = 0; profile < 3; profile++)
                {
                    int[] values = Table(rows, profile);
                    if (values.Length != rows + 1) throw new InvalidOperationException("Invalid pocket table");
                    for (int i = 0; i <= rows; i++)
                    {
                        if (values[i] < 0 || values[i] != values[rows - i]) throw new InvalidOperationException("Invalid symmetry");
                    }
                }
            }
        }
    }
}
