using UnityEditor;
using UnityEngine;

namespace Crash.EditorTools
{
    /// Runs without a scene: verifies the maths that decides points, crash timing and shields.
    public static class SelfTest
    {
        static int _fails;

        static void Check(bool ok, string name)
        {
            if (ok) Debug.Log("[SELFTEST] PASS " + name);
            else { Debug.LogError("[SELFTEST] FAIL " + name); _fails++; }
        }

        public static void Run()
        {
            _fails = 0;

            Check(Mathf.Abs(Rules.Multiplier(0f) - 1f) < 1e-4f, "multiplier starts at 1.00");
            Check(Rules.Multiplier(1f) > 1.3f && Rules.Multiplier(1f) < 1.7f, "multiplier after 1s is ~1.46");
            Check(Rules.Multiplier(3f) > Rules.Multiplier(2f), "multiplier rises with time");

            for (int i = 0; i < 24; i++)
            {
                float t = i * 0.75f;
                float m = Rules.Multiplier(t);
                float back = Rules.TimeFor(m);
                Check(Mathf.Abs(back - t) < 0.02f, "TimeFor inverts Multiplier at t=" + t);
            }

            Check(Rules.RoundPoints(1f) == 0, "no points at 1.00x");
            Check(Rules.RoundPoints(2.5f) == 1500, "2.50x pays 1500");
            Check(Rules.RoundPoints(10f) == 9000, "10.00x pays 9000");
            Check(Rules.RoundPoints(10f) > Rules.RoundPoints(5f), "points accelerate with multiplier");

            var rng = new System.Random(12345);
            int n = 20000, atLeast2 = 0, under1_4 = 0;
            float min = float.MaxValue, max = 0f, medianSum = 0f;
            var values = new float[n];
            for (int i = 0; i < n; i++)
            {
                float c = Rules.SampleCrashMultiplier(rng);
                values[i] = c;
                if (c < min) min = c;
                if (c > max) max = c;
                if (c >= 2f) atLeast2++;
                if (c < 1.4f) under1_4++;
            }
            System.Array.Sort(values);
            float median = values[n / 2];
            float p90 = values[(int)(n * 0.9f)];

            Check(min >= 1.3f, "crash never below 1.3x (min=" + min.ToString("0.00") + ")");
            Check(max <= Rules.MaxMultiplier + 0.01f, "crash respects cap");
            Check(median > 1.8f && median < 2.8f, "median crash is 1.8x-2.8x (" + median.ToString("0.00") + ")");
            Check(atLeast2 > n * 0.45f && atLeast2 < n * 0.66f, "roughly 57% of rounds reach 2x (" + (100f * atLeast2 / n).ToString("0.0") + "%)");
            Check(under1_4 > n * 0.05f && under1_4 < n * 0.35f, "early busts are uncommon but real (" + (100f * under1_4 / n).ToString("0.0") + "%)");
            Check(p90 > 8f && p90 < 26f, "tail is fat: p90 = " + p90.ToString("0.00") + "x");

            // shield economy: three crashes tolerated, the fourth ends the run
            int shields = Rules.MaxShields;
            int tolerated = 0;
            for (int i = 0; i < 10; i++)
            {
                if (shields > 0) { shields--; tolerated++; }
                else break;
            }
            Check(tolerated == 3, "three shields absorb three crashes");
            Check(shields == 0, "shields run out after three crashes");

            // streak refill
            int streak = 0, sh = 1;
            for (int i = 0; i < 4; i++)
            {
                streak++;
                if (streak % Rules.StreakForShield == 0 && sh < Rules.MaxShields) sh++;
            }
            Check(sh == 2, "four cash-outs in a row restore one shield");

            Debug.Log("[SELFTEST] " + (_fails == 0 ? "ALL PASS" : _fails + " FAILURES"));
            EditorApplication.Exit(_fails == 0 ? 0 : 1);
        }
    }
}
