using System;
using UnityEngine;

namespace DropStack.Game
{
    public class ScoreManager : MonoBehaviour
    {
        private const string LegacyHighScoreKey = "DropStack_HighScore_v1";
        private string HighScoreKey => $"DropStack_Best_{Difficulty}_v2";
        public GameDifficulty Difficulty { get; private set; } = GameDifficulty.Neutral;
        private int ReadBest() => PlayerPrefs.GetInt(HighScoreKey, Difficulty == GameDifficulty.Neutral ? PlayerPrefs.GetInt(LegacyHighScoreKey, 0) : 0);
        public static int BestFor(GameDifficulty mode) => PlayerPrefs.GetInt($"DropStack_Best_{mode}_v2", mode == GameDifficulty.Neutral ? PlayerPrefs.GetInt(LegacyHighScoreKey, 0) : 0);
        public void SetDifficulty(GameDifficulty mode) { SaveHighScore(); Difficulty = mode; HighScore = ReadBest(); }
        public void RegisterTargetBonus(int points) => AddScore(points, "TARGET CLEARED!", true);

        public int CurrentScore { get; private set; }
        public int HighScore { get; private set; }
        public int TotalLinesCleared { get; private set; }
        public int CurrentStreak { get; private set; }
        public int MaxStreak { get; private set; }
        public float CurrentMultiplier { get; private set; } = 1.0f;
        public float MaxMultiplier { get; private set; } = 1.0f;

        // Statistics
        public int SinglesCount { get; private set; }
        public int DoublesCount { get; private set; }
        public int TriplesCount { get; private set; }
        public int DropStacksCount { get; private set; } // 4-lines
        public int PentrisCount { get; private set; }    // 5-lines
        public bool IsNewHighScore { get; private set; }

        public event Action<int, int, string> OnScoreChanged; // currentScore, addedPoints, reason
        public event Action<int, float> OnComboChanged;       // streakCount, multiplier
        public event Action<int, int> OnLinesChanged;         // addedLines, totalLines
        public event Action<int> OnNewHighScoreAchieved;

        private void Awake()
        {
            HighScore = ReadBest();
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            TotalLinesCleared = 0;
            CurrentStreak = 0;
            MaxStreak = 0;
            CurrentMultiplier = 1.0f;
            MaxMultiplier = 1.0f;
            SinglesCount = 0;
            DoublesCount = 0;
            TriplesCount = 0;
            DropStacksCount = 0;
            PentrisCount = 0;
            IsNewHighScore = false;

            HighScore = ReadBest();

            OnScoreChanged?.Invoke(CurrentScore, 0, "Reset");
            OnComboChanged?.Invoke(CurrentStreak, CurrentMultiplier);
            OnLinesChanged?.Invoke(0, TotalLinesCleared);
        }

        public void RegisterDropScore(int cells, bool isHardDrop)
        {
            if (cells <= 0) return;
            int pts = isHardDrop ? cells * 2 : cells * 1;
            AddScore(pts, isHardDrop ? "Hard Drop" : "Soft Drop", false);
        }

        public void RegisterPieceLockedNoClear()
        {
            if (CurrentStreak > 0)
            {
                CurrentStreak = 0;
                CurrentMultiplier = 1.0f;
                OnComboChanged?.Invoke(CurrentStreak, CurrentMultiplier);
            }
        }

        public int RegisterLineClear(int lineCount, bool isPerfectClear)
        {
            if (lineCount <= 0) return 0;

            TotalLinesCleared += lineCount;
            CurrentStreak++;
            if (CurrentStreak > MaxStreak) MaxStreak = CurrentStreak;

            // Calculate streak multiplier
            // Streak 1 = 1.0x, Streak 2 = 1.2x, Streak 3 = 1.5x, Streak 4 = 2.0x, Streak 5 = 2.5x, etc.
            if (CurrentStreak <= 1)
            {
                CurrentMultiplier = 1.0f;
            }
            else if (CurrentStreak == 2)
            {
                CurrentMultiplier = 1.2f;
            }
            else if (CurrentStreak == 3)
            {
                CurrentMultiplier = 1.5f;
            }
            else if (CurrentStreak == 4)
            {
                CurrentMultiplier = 2.0f;
            }
            else
            {
                CurrentMultiplier = 2.0f + (CurrentStreak - 4) * 0.5f;
            }

            // Exponential point bonuses per spec:
            // Single: 100, Multi: 300, 700, 1500 (and 2500 for 5 lines)
            int basePoints = 100;
            string clearName = "Single";

            switch (lineCount)
            {
                case 1:
                    basePoints = 100;
                    clearName = "SINGLE!";
                    SinglesCount++;
                    break;
                case 2:
                    basePoints = 300;
                    clearName = "DOUBLE!";
                    DoublesCount++;
                    break;
                case 3:
                    basePoints = 700;
                    clearName = "TRIPLE!";
                    TriplesCount++;
                    break;
                case 4:
                    basePoints = 1500;
                    clearName = "DROP STACK!";
                    DropStacksCount++;
                    break;
                default:
                    basePoints = 2500;
                    clearName = "MEGA PENTRIS!";
                    PentrisCount++;
                    break;
            }

            int awardedPoints = Mathf.RoundToInt(basePoints * CurrentMultiplier);
            MaxMultiplier = Mathf.Max(MaxMultiplier, CurrentMultiplier);

            if (isPerfectClear)
            {
                awardedPoints += 3000;
                clearName += " + PERFECT CLEAR!";
            }

            AddScore(awardedPoints, clearName, true);
            OnComboChanged?.Invoke(CurrentStreak, CurrentMultiplier);
            OnLinesChanged?.Invoke(lineCount, TotalLinesCleared);

            return awardedPoints;
        }

        private void AddScore(int points, string reason, bool canBeHighScore)
        {
            CurrentScore += points;

            if (CurrentScore > HighScore)
            {
                HighScore = CurrentScore;
                if (!IsNewHighScore)
                {
                    IsNewHighScore = true;
                    OnNewHighScoreAchieved?.Invoke(HighScore);
                }
            }

            OnScoreChanged?.Invoke(CurrentScore, points, reason);
        }

        public void SaveHighScore()
        {
            if (HighScore <= PlayerPrefs.GetInt(HighScoreKey, 0)) return;
            PlayerPrefs.SetInt(HighScoreKey, HighScore);
            PlayerPrefs.Save();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) SaveHighScore(); }
        private void OnApplicationPause(bool paused) { if (paused) SaveHighScore(); }
        private void OnApplicationQuit() => SaveHighScore();
    }
}
