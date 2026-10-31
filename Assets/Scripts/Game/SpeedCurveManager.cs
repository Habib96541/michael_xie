using System;
using UnityEngine;

namespace DropStack.Game
{
    public class SpeedCurveManager : MonoBehaviour
    {
        // 90 second duration
        public const float TotalGameDuration = 90.0f;
        public const float AccelerationStepDuration = 15.0f;

        // Speed intervals in seconds per drop for levels 1 to 6
        private readonly float[] levelIntervals = new float[]
        {
            1.00f, // Level 1 (0-15s elapsed, 1.0 drop/sec)
            0.75f, // Level 2 (15-30s elapsed)
            0.55f, // Level 3 (30-45s elapsed)
            0.38f, // Level 4 (45-60s elapsed)
            0.24f, // Level 5 (60-75s elapsed - final 30s escalation!)
            0.14f  // Level 6 (75-90s elapsed - peak velocity!)
        };

        public int CurrentLevel { get; private set; } = 1;
        public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Neutral;
        public float CurrentDropInterval => DifficultyRules.DropInterval(Difficulty, CurrentLevel);
        public void SetDifficulty(GameDifficulty mode) { Difficulty = mode; ResetSpeed(); }
        public bool IsPeakVelocity => CurrentLevel >= 5;

        public event Action<int, float, bool> OnSpeedLevelChanged; // level, interval, isPeak

        public void ResetSpeed()
        {
            CurrentLevel = 1;
            OnSpeedLevelChanged?.Invoke(CurrentLevel, CurrentDropInterval, IsPeakVelocity);
        }

        public void UpdateTime(float elapsedTime)
        {
            int calculatedLevel = Mathf.FloorToInt(elapsedTime / AccelerationStepDuration) + 1;
            calculatedLevel = Mathf.Clamp(calculatedLevel, 1, levelIntervals.Length);

            if (calculatedLevel != CurrentLevel)
            {
                CurrentLevel = calculatedLevel;
                OnSpeedLevelChanged?.Invoke(CurrentLevel, CurrentDropInterval, IsPeakVelocity);
            }
        }
    }
}
