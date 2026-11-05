using UnityEngine;

namespace Crash
{
    /// All game maths lives here, free of Unity objects, so it can be self-tested.
    public static class Rules
    {
        // multiplier m(t) = 1 + A * (exp(B*t) - 1)
        public const float GrowthA = 0.85f;
        public const float GrowthB = 0.34f;

        public const int PointsPerX = 1000;
        public const int MaxShields = 3;
        public const int StreakForShield = 4;
        public const float MeanCrashSeconds = 4.05f;
        public const float MaxMultiplier = 600f;

        /// One table width is this many seconds of flight. The rocket ends the round
        /// when it reaches the right edge of the table, so this also caps a round.
        public const float TableSeconds = 6.5f;

        public static float TableMaxMultiplier { get { return Multiplier(TableSeconds); } }

        /// A round can never run past the end of the table.
        public static float ClampToTable(float m)
        {
            return Mathf.Min(m, TableMaxMultiplier);
        }

        public static float Multiplier(float t)
        {
            if (t <= 0f) return 1f;
            return 1f + GrowthA * (Mathf.Exp(GrowthB * t) - 1f);
        }

        public static float TimeFor(float m)
        {
            if (m <= 1f) return 0f;
            return Mathf.Log(1f + (m - 1f) / GrowthA) / GrowthB;
        }

        /// Points you bank if you cash out at multiplier m.
        public static long RoundPoints(float m)
        {
            if (m <= 1.0001f) return 0;
            return (long)Mathf.Floor((m - 1f) * PointsPerX);
        }

        /// Crash multiplier from an exponential flight-time distribution:
        /// most rounds die early, a few run very long. Never below ~1.3x.
        public static float SampleCrashMultiplier(System.Random rng)
        {
            double u = rng.NextDouble();
            if (u > 0.9995) u = 0.9995;
            double t = -System.Math.Log(1.0 - u) * MeanCrashSeconds;
            if (t < 1.05) t = 1.05;
            if (t > 120.0) t = 120.0;
            return Mathf.Min(Multiplier((float)t), MaxMultiplier);
        }
    }
}
