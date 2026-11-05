using UnityEngine;

namespace Watermelon
{
    public sealed class WatermelonGame : MonoBehaviour
    {
        [SerializeField] private WatermelonVisual visual;
        [SerializeField] private WatermelonUI userInterface;
        [SerializeField] private WatermelonAudio audioFeedback;
        [SerializeField, Min(2)] private int minimumBurstCount = 8;
        [SerializeField, Min(2)] private int maximumBurstCount = 22;
        [SerializeField, Min(0.01f)] private float multiplierPerBand = 0.15f;
        [SerializeField, Min(0.05f)] private float bandInterval = 0.22f;

        private int burstCount;
        private float nextBandTime;

        public int BandCount { get; private set; }
        public float Multiplier => 1f + BandCount * multiplierPerBand;
        public bool HasBurst { get; private set; }
        public bool IsPlaying { get; private set; }
        public bool CanAddBand => IsPlaying && !HasBurst && Time.time >= nextBandTime;

        private void Start()
        {
            ReturnToMenu();
        }

        public void AddBand()
        {
            if (!CanAddBand)
                return;

            nextBandTime = Time.time + bandInterval;
            BandCount++;
            visual.AddBand(BandCount);
            audioFeedback.PlayBand();
            HasBurst = BandCount >= burstCount;

            if (HasBurst)
            {
                visual.Burst();
                audioFeedback.PlayBurst();
            }

            userInterface.Refresh(BandCount, Multiplier, HasBurst);
        }

        public void RestartRound()
        {
            ResetRound();
            IsPlaying = true;
            userInterface.ShowMenu(false);
        }

        public void ReturnToMenu()
        {
            ResetRound();
            IsPlaying = false;
            userInterface.ShowMenu(true);
        }

        public void QuitGame()
        {
            audioFeedback.StopSounds();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResetRound()
        {
            int minimum = Mathf.Max(2, minimumBurstCount);
            int maximum = Mathf.Max(minimum, maximumBurstCount);
            burstCount = Random.Range(minimum, maximum + 1);
            BandCount = 0;
            HasBurst = false;
            nextBandTime = Time.time;
            audioFeedback.StopSounds();
            visual.ResetRound();
            userInterface.Refresh(BandCount, Multiplier, HasBurst);
        }

        private void OnValidate()
        {
            minimumBurstCount = Mathf.Clamp(minimumBurstCount, 2, 100);
            maximumBurstCount = Mathf.Clamp(maximumBurstCount, minimumBurstCount, 100);
        }
    }
}
