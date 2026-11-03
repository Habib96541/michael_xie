using System;

namespace NeonDash.Core
{
    /// <summary>
    /// Scoring for a 30 second seeded run.
    ///
    /// The multiplier is not rolled: it is earned during the run by collecting orbs and pads (see
    /// <see cref="MultiplierLedger"/>), so it starts at 1.00x and climbs as far as the player can
    /// chain pickups, capped at 10x. Score = distance x 100 x earned multiplier.
    /// </summary>
    public static class ScoreRules
    {
        public static float Progress(float distance, float courseLength)
        {
            if (courseLength <= 0f) return 0f;
            float p = distance / courseLength;
            if (p < 0f) return 0f;
            if (p > 1f) return 1f;
            return p;
        }

        /// <summary>Score for a finished run: distance blocks x 100 x earned multiplier.</summary>
        public static int Payout(int distanceBlocks, float multiplier)
        {
            if (distanceBlocks <= 0 || multiplier <= 0f) return 0;
            double points = distanceBlocks * (double)GameConstants.PointsPerBlock * multiplier;
            return (int)Math.Round(points);
        }
    }

    /// <summary>Everything the result screen needs about a finished run.</summary>
    [Serializable]
    public struct RunResult
    {
        public string Seed;
        public string PlayerName;
        public int Distance;
        public float Progress;
        /// <summary>Earned multiplier at the end of the run.</summary>
        public float Multiplier;
        public int Orbs;
        public int Pads;
        public int Gems;
        public int BestChain;
        public int Payout;
        public bool Died;
        public bool Completed;
        public float Duration;
    }
}
