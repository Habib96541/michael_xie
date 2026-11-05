using UnityEngine;

namespace DiceGame.Config
{
    [CreateAssetMenu(fileName = "DiceGameConfig", menuName = "DiceGame/Game Config")]
    public sealed class DiceGameConfig : ScriptableObject
    {
        [Header("Bankroll & Betting")]
        [SerializeField] private long startingBankroll = 1000;
        [SerializeField] private long minBet = 10;
        [SerializeField] private long maxBet = 10000;
        [SerializeField] private long defaultBet = 50;

        [Header("Target & House Edge")]
        [SerializeField] private float minTarget = 2.0f;
        [SerializeField] private float maxTarget = 98.0f;
        [SerializeField] private float defaultTarget = 50.0f;
        [SerializeField] private float houseEdgePercent = 1.0f; // 99% RTP

        [Header("20-Second Blitz Mode")]
        [SerializeField] private float blitzDurationSeconds = 20.0f;
        [SerializeField] private float critTimeBonus = 2.0f;
        [SerializeField] private float streakTimeBonus = 2.0f;

        [Header("Fever Mode")]
        [SerializeField] private int feverStreakThreshold = 3;
        [SerializeField] private float feverDurationSeconds = 5.0f;
        [SerializeField] private float feverMultiplierBonus = 1.5f;

        [Header("Golden Jackpot Crit Zone")]
        [SerializeField] private float critZoneWidth = 5.0f;
        [SerializeField] private float critBonusMultiplier = 3.0f;

        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 0.85f;
        [SerializeField] private float sfxVolume = 1.0f;

        // Public getters for encapsulation
        public long StartingBankroll => startingBankroll;
        public long MinBet => minBet;
        public long MaxBet => maxBet;
        public long DefaultBet => defaultBet;

        public float MinTarget => minTarget;
        public float MaxTarget => maxTarget;
        public float DefaultTarget => defaultTarget;
        public float HouseEdgePercent => houseEdgePercent;
        public float PayoutFactor => 100.0f - houseEdgePercent; // 99.0f

        public float BlitzDurationSeconds => blitzDurationSeconds;
        public float CritTimeBonus => critTimeBonus;
        public float StreakTimeBonus => streakTimeBonus;

        public int FeverStreakThreshold => feverStreakThreshold;
        public float FeverDurationSeconds => feverDurationSeconds;
        public float FeverMultiplierBonus => feverMultiplierBonus;

        public float CritZoneWidth => critZoneWidth;
        public float CritBonusMultiplier => critBonusMultiplier;

        public float MasterVolume => masterVolume;
        public float SfxVolume => sfxVolume;
    }
}
