using System;
using System.Numerics;
using UnityEngine;

namespace HiLo
{
    [CreateAssetMenu(menuName = "HiLo/Game configuration")]
    public sealed class GameConfig : ScriptableObject
    {
        public double stake = 10;
        public double initialBalance = 200;
        public double freeRefill = 100;
        [Range(0, .25f)] public float houseEdge = .04f;
        [Range(.001f,.05f)] public float minimumMultiplierGrowth = .01f;
        public float minimumDecisionSeconds = 4.2f;
        public float endlessSecondsPerTenWins = .3f;
        public DifficultyStage[] stages = {
            new DifficultyStage("Beginner", 0, 0, .42f),
            new DifficultyStage("Intermediate", 5, 16, .36f),
            new DifficultyStage("Advanced", 10, 11, .3f),
            new DifficultyStage("Expert", 20, 8, .25f),
            new DifficultyStage("Endless", 40, 7, .22f)
        };
        public int StageIndex(BigInteger streak) { for (int i = stages.Length - 1; i >= 0; i--) if (streak >= stages[i].startsAt) return i; return 0; }
        public float DecisionSeconds(BigInteger streak)
        {
            int i = StageIndex(streak); float seconds = stages[i].decisionSeconds;
            if (i == stages.Length - 1) seconds -= (float)System.Math.Min(1000, (double)(streak - stages[i].startsAt) / 10) * endlessSecondsPerTenWins;
            return seconds == 0 ? 0 : Mathf.Max(minimumDecisionSeconds, seconds);
        }
    }
    [Serializable] public struct DifficultyStage
    {
        public string name;
        public int startsAt;
        public float decisionSeconds, revealSeconds;
        public DifficultyStage(string name, int startsAt, float decisionSeconds, float revealSeconds) { this.name = name; this.startsAt = startsAt; this.decisionSeconds = decisionSeconds; this.revealSeconds = revealSeconds; }
    }
}
