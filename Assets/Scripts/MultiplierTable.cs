using System.Globalization;
using UnityEngine;

/// <summary>
/// Multiplier rules for Chest Shuffle 3D. Every round has one weighted loss,
/// one weighted profit, and one random wildcard between 0x and 10x.
/// </summary>
public static class MultiplierTable
{
    public const int ChestCount = 3;
    public const float MinMultiplier = 0f;
    public const float MaxMultiplier = 10f;

    public static readonly float[] Values = { 0.1f, 0.2f, 0.5f, 1f, 1.5f, 2f, 3f, 5f, 10f };
    public static readonly int[] Weights = { 22, 16, 16, 16, 10, 8, 6, 4, 2 };

    /// <summary>Draws a distinct loss, profit and wildcard, then shuffles their chest positions.</summary>
    public static float[] RollDistinct(System.Random rng)
    {
        bool[] used = new bool[Values.Length];
        int lossIndex = WeightedIndex(rng, used, 0);
        used[lossIndex] = true;
        int profitIndex = WeightedIndex(rng, used, 1);
        used[profitIndex] = true;

        bool wildcardInLossRange = rng.Next(2) == 0;
        float wildcard;
        do
        {
            wildcard = wildcardInLossRange
                ? (float)rng.NextDouble()
                : 1f + (float)rng.NextDouble() * (MaxMultiplier - 1f);
        }
        while (Mathf.Approximately(wildcard, Values[lossIndex]) || Mathf.Approximately(wildcard, Values[profitIndex]));

        float[] result = { Values[lossIndex], Values[profitIndex], wildcard };
        for (int i = result.Length - 1; i > 0; i--)
        {
            int other = rng.Next(i + 1);
            float value = result[i];
            result[i] = result[other];
            result[other] = value;
        }
        return result;
    }

    // category: 0 selects below 1x, 1 selects above 1x.
    static int WeightedIndex(System.Random rng, bool[] used, int category)
    {
        int total = 0;
        for (int i = 0; i < Values.Length; i++)
        {
            if (!used[i] && (category == 0 ? Values[i] < 1f : Values[i] > 1f)) total += Weights[i];
        }

        int roll = rng.Next(Mathf.Max(1, total));
        for (int i = 0; i < Values.Length; i++)
        {
            if (used[i] || (category == 0 ? Values[i] >= 1f : Values[i] <= 1f)) continue;
            if (roll < Weights[i]) return i;
            roll -= Weights[i];
        }

        for (int i = Values.Length - 1; i >= 0; i--)
            if (!used[i] && (category == 0 ? Values[i] < 1f : Values[i] > 1f)) return i;
        return 0;
    }

    /// <summary>Reward for a chest: stake multiplied by the multiplier, rounded to whole coins.</summary>
    public static int Payout(int stake, float multiplier)
    {
        return Mathf.Max(0, Mathf.RoundToInt(stake * multiplier));
    }

    public static string Format(float multiplier)
    {
        return multiplier.ToString("0.##", CultureInfo.InvariantCulture) + "\u00D7";
    }

    public static string Coins(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture);
    }

    public static Color ColorFor(float multiplier)
    {
        if (multiplier >= 10f) return new Color(1f, 0.42f, 0.72f);
        if (multiplier >= 5f) return new Color(1f, 0.80f, 0.35f);
        if (multiplier >= 3f) return new Color(1f, 0.70f, 0.32f);
        if (multiplier >= 2f) return new Color(0.45f, 0.92f, 0.78f);
        if (multiplier > 1f) return new Color(0.80f, 0.92f, 0.78f);
        if (multiplier >= 1f) return new Color(0.86f, 0.83f, 0.90f);
        return new Color(1f, 0.44f, 0.52f);
    }

    public static string Verdict(float multiplier)
    {
        if (multiplier >= 10f) return "JACKPOT";
        if (multiplier >= 5f) return "MASSIVE WIN";
        if (multiplier >= 3f) return "HUGE WIN";
        if (multiplier > 1f) return "YOU WIN";
        if (multiplier >= 1f) return "BREAK EVEN";
        return "TOUGH LUCK";
    }
}
