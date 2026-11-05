using UnityEngine;

namespace CyberWhack.Data
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CyberWhack/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Tournament Timing")]
        [SerializeField] private float roundDuration = 60.0f;
        [SerializeField] private float countdownDuration = 3.0f;

        [Header("Standard Cyber-Mole (Blue Visor)")]
        [SerializeField] private float standardActiveDuration = 0.85f;
        [SerializeField] private int standardHitsRequired = 1;
        [SerializeField] private int standardBaseScore = 100;

        [Header("Overclocked Gold Mole (Gold Chassis & Arcs)")]
        [SerializeField] private float goldActiveDuration = 0.50f;
        [SerializeField] private int goldHitsRequired = 1;
        [SerializeField] private int goldBaseScore = 300;

        [Header("Armored Tank Mole (Reinforced Blast Shield)")]
        [SerializeField] private float armoredActiveDuration = 1.10f;
        [SerializeField] private int armoredHitsRequired = 2;
        [SerializeField] private int armoredBaseScore = 250;

        [Header("EMP Decoy Mine (Pulsing Hazard Drone)")]
        [SerializeField] private float empActiveDuration = 1.00f;
        [SerializeField] private int empScorePenalty = 200;

        [Header("Combo Streak Thresholds")]
        [SerializeField] private int tier2Streak = 5;
        [SerializeField] private int tier3Streak = 10;
        [SerializeField] private int tier4Streak = 15;

        [Header("Spawn Cadence (Frantic Difficulty Scaling)")]
        [SerializeField] private float initialSpawnInterval = 0.80f;
        [SerializeField] private float minSpawnInterval = 0.42f;
        [SerializeField] private int maxSimultaneousTargets = 3;

        public float RoundDuration => roundDuration;
        public float CountdownDuration => countdownDuration;

        public float StandardActiveDuration => standardActiveDuration;
        public int StandardHitsRequired => standardHitsRequired;
        public int StandardBaseScore => standardBaseScore;

        public float GoldActiveDuration => goldActiveDuration;
        public int GoldHitsRequired => goldHitsRequired;
        public int GoldBaseScore => goldBaseScore;

        public float ArmoredActiveDuration => armoredActiveDuration;
        public int ArmoredHitsRequired => armoredHitsRequired;
        public int ArmoredBaseScore => armoredBaseScore;

        public float EmpActiveDuration => empActiveDuration;
        public int EmpScorePenalty => empScorePenalty;

        public int Tier2Streak => tier2Streak;
        public int Tier3Streak => tier3Streak;
        public int Tier4Streak => tier4Streak;

        public float InitialSpawnInterval => initialSpawnInterval;
        public float MinSpawnInterval => minSpawnInterval;
        public int MaxSimultaneousTargets => maxSimultaneousTargets;

        public float GetActiveDuration(TargetType type)
        {
            return type switch
            {
                TargetType.StandardCyberMole => standardActiveDuration,
                TargetType.OverclockedGoldMole => goldActiveDuration,
                TargetType.ArmoredTankMole => armoredActiveDuration,
                TargetType.EmpDecoyMine => empActiveDuration,
                _ => 1.0f
            };
        }

        public int GetHitsRequired(TargetType type)
        {
            return type switch
            {
                TargetType.ArmoredTankMole => armoredHitsRequired,
                _ => 1
            };
        }

        public int GetBaseScore(TargetType type)
        {
            return type switch
            {
                TargetType.StandardCyberMole => standardBaseScore,
                TargetType.OverclockedGoldMole => goldBaseScore,
                TargetType.ArmoredTankMole => armoredBaseScore,
                TargetType.EmpDecoyMine => -empScorePenalty,
                _ => 0
            };
        }

        public int CalculateMultiplier(int streak)
        {
            if (streak >= tier4Streak) return 4;
            if (streak >= tier3Streak) return 3;
            if (streak >= tier2Streak) return 2;
            return 1;
        }
    }
}
