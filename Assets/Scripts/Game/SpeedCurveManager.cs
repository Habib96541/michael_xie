using System;
using UnityEngine;

namespace DropStack.Game
{
    public class SpeedCurveManager : MonoBehaviour
    {
        // 90 second duration
        public const float TotalGameDuration = 90.0f;
        public const float AccelerationStepDuration = 15.0f;

        private const int LevelCount = 6;

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
            calculatedLevel = Mathf.Clamp(calculatedLevel, 1, LevelCount);

            if (calculatedLevel != CurrentLevel)
            {
                CurrentLevel = calculatedLevel;
                OnSpeedLevelChanged?.Invoke(CurrentLevel, CurrentDropInterval, IsPeakVelocity);
            }
        }
    }
}
