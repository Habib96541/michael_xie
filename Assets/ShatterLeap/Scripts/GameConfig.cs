using System;
using UnityEngine;

namespace ShatterLeap
{
    [Serializable]
    public sealed class Difficulty
    {
        public string Name;
        public int Rows;
        public float PreviewSeconds;
        public float PlaySeconds;
        public int Bonus;
    }

    [CreateAssetMenu(menuName = "ShatterLeap/Game Configuration")]
    public sealed class GameConfig : ScriptableObject
    {
        public Difficulty[] Difficulties =
        {
            new Difficulty { Name = "Easy", Rows = 6, PreviewSeconds = 5, PlaySeconds = 30, Bonus = 50 },
            new Difficulty { Name = "Normal", Rows = 10, PreviewSeconds = 4, PlaySeconds = 45, Bonus = 100 },
            new Difficulty { Name = "Hard", Rows = 14, PreviewSeconds = 3, PlaySeconds = 60, Bonus = 200 }
        };
        [Min(1)] public int StepValue = 10;
        [Min(0.1f)] public float JumpDuration = 0.65f;
        [Min(0.1f)] public float JumpHeight = 1.5f;
        [Min(0.1f)] public float CameraTransitionSeconds = 0.8f;
        [Min(0.1f)] public float ResultDelay = 1.8f;
        [Min(1)] public int ShardCount = 20;
        public GameObject ContestantPrefab;
        public GameObject PanelPrefab;
        public Sprite Logo;
        public Sprite CoinIcon;
        public Sprite UiPanel;
        public AudioClip Music;
        public AudioClip Click;
        public AudioClip Success;
        public AudioClip Glass;
        public AudioClip Failure;
        public AudioClip Coin;

        public void Validate()
        {
            if (Difficulties == null || Difficulties.Length != 3 || StepValue <= 0 ||
                JumpDuration <= 0 || JumpHeight <= 0 || CameraTransitionSeconds <= 0 || ResultDelay <= 0 || ShardCount < 1)
            {
                throw new InvalidOperationException("Invalid game configuration.");
            }
            foreach (Difficulty difficulty in Difficulties)
            {
                if (difficulty == null || difficulty.Rows < 1 || difficulty.Rows > 30 ||
                    difficulty.PreviewSeconds <= 0 || difficulty.PlaySeconds <= 0 || difficulty.Bonus < 0)
                {
                    throw new InvalidOperationException("Invalid difficulty.");
                }
            }
            if (!ContestantPrefab || !PanelPrefab || !Logo || !CoinIcon)
            {
                throw new InvalidOperationException("Missing prefab or UI asset reference.");
            }
        }
    }
}
