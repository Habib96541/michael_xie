using UnityEngine;

namespace MineGrid
{
    public enum RoundState
    {
        Setup,
        Playing,
        Won,
        Lost,
        Finished
    }

    [CreateAssetMenu(menuName = "MineGrid/Game Config")]
    public sealed class GameConfig : ScriptableObject
    {
        [Min(1)]
        public int pointsPerGem = 10;
        public int[] wheelCoins =
        {
            50,
            75,
            100,
            150,
            200,
            300
        };
        public int[] milestonePoints =
        {
            500,
            1500,
            3000,
            5000,
            10000
        };
        public int[] milestoneCoins =
        {
            100,
            200,
            350,
            500,
            1000
        };
        public int Score(int gems, int mines) => gems * Mathf.Max(1, pointsPerGem) * mines;
    }
}
