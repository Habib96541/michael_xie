using System.Collections.Generic;
using DropStack.Data;
using UnityEngine;

namespace DropStack.Game
{
    public enum GameDifficulty { Noob, Neutral, Pro }

    public static class DifficultyRules
    {
        private static readonly PolyominoType[] Classic = { PolyominoType.I4, PolyominoType.O4, PolyominoType.T4, PolyominoType.S4, PolyominoType.Z4, PolyominoType.J4, PolyominoType.L4 };
        private static readonly float[][] Intervals = {
            new[] { 1.3f, 1.1f, .9f, .74f, .6f, .48f },
            new[] { 1f, .8f, .62f, .46f, .34f, .24f },
            new[] { .62f, .48f, .36f, .26f, .18f, .12f }
        };
        public static float DropInterval(GameDifficulty mode, int level) => Intervals[(int)mode][Mathf.Clamp(level - 1, 0, 5)];
        public static float LockDelay(GameDifficulty mode) => mode == GameDifficulty.Noob ? .7f : mode == GameDifficulty.Pro ? .34f : .5f;
        public static int Target(GameDifficulty mode) => mode == GameDifficulty.Noob ? 4 : mode == GameDifficulty.Pro ? 12 : 8;
        public static int TargetBonus(GameDifficulty mode) => mode == GameDifficulty.Noob ? 500 : mode == GameDifficulty.Pro ? 2000 : 1200;
        public static IReadOnlyList<PolyominoType> Pieces(GameDifficulty mode) => mode == GameDifficulty.Noob ? Classic : PolyominoDatabase.GetAllTypes();
        public static Color Color(GameDifficulty mode) => mode == GameDifficulty.Noob ? new Color(.55f, .95f, .27f) : mode == GameDifficulty.Pro ? new Color(1f, .23f, .46f) : new Color(.05f, .86f, 1f);
    }
}
