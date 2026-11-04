using UnityEngine;

namespace BullseyeBlitz
{
    [CreateAssetMenu(menuName = "Bullseye Blitz/Configuration")]
    public sealed class BlitzConfig : ScriptableObject
    {
        public float RoundSeconds = 20f;
        public Vector2 SpawnInterval = new Vector2(0.45f, 0.75f);
        public Vector2 Exposure = new Vector2(1.4f, 2.1f);
        public float TransitionSeconds = 0.15f;
        public int MaximumTargets = 5;
        public int MaximumBombs = 2;
        public float BombChance = 0.18f;
        public int[] Rewards = { 10, 20, 50, 100 };
        public int[] Weights = { 45, 30, 18, 7 };
        public Sprite Coin;
        public Sprite Bomb;
        public Sprite Button;
        public Font DisplayFont;
        public Font BodyFont;
        public AudioClip Music;
        public AudioClip Pop;
        public AudioClip Reward;

        public bool IsValid()
        {
            if (!Finite(RoundSeconds) || !Finite(TransitionSeconds) || !Finite(BombChance) || !Finite(SpawnInterval.x) || !Finite(SpawnInterval.y) || !Finite(Exposure.x) || !Finite(Exposure.y)) { return false; }
            if (RoundSeconds != 20f || TransitionSeconds <= 0 || MaximumTargets < 2 || MaximumTargets > 9 || MaximumBombs < 0 || MaximumBombs > MaximumTargets || BombChance < 0 || BombChance > 1 || SpawnInterval.x <= 0 || SpawnInterval.y < SpawnInterval.x || Exposure.x <= 0 || Exposure.y < Exposure.x || Rewards == null || Weights == null || Rewards.Length == 0 || Rewards.Length != Weights.Length)
            {
                return false;
            }
            int sum = 0;
            for (int i = 0; i < Rewards.Length; i++)
            {
                if (Rewards[i] <= 0 || Weights[i] < 0) { return false; }
                sum += Weights[i];
            }
            return sum > 0;
        }
        private bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
