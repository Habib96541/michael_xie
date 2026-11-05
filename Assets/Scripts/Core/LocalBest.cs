using UnityEngine;

namespace BullRide.Core
{
    public sealed class LocalBest
    {
        const string KeyBest = "BullRide.BestScore";
        const string KeyLongest = "BullRide.LongestRide";
        const string KeyMuted = "BullRide.Muted";

        public int BestScore { get; private set; }
        public float LongestRide { get; private set; }
        public bool Muted { get; private set; }

        public void Load()
        {
            BestScore = PlayerPrefs.GetInt(KeyBest, 0);
            LongestRide = PlayerPrefs.GetFloat(KeyLongest, 0f);
            Muted = PlayerPrefs.GetInt(KeyMuted, 0) == 1;
        }

        public bool Submit(int score, float rideSeconds)
        {
            bool improved = false;
            if (score > BestScore)
            {
                BestScore = score;
                improved = true;
            }
            if (rideSeconds > LongestRide)
            {
                LongestRide = rideSeconds;
                improved = true;
            }
            if (improved) Save();
            return improved;
        }

        public void SetMuted(bool muted)
        {
            Muted = muted;
            PlayerPrefs.SetInt(KeyMuted, muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        void Save()
        {
            PlayerPrefs.SetInt(KeyBest, BestScore);
            PlayerPrefs.SetFloat(KeyLongest, LongestRide);
            PlayerPrefs.Save();
        }
    }
}
