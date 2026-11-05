using System;

namespace HydraulicPress
{
    public enum RoundState { Ready, Crushing, Failed, Collected, Perfect }

    // The presentation never supplies outcomes. All twenty slots are committed at construction.
    public sealed class RoundManager
    {
        public const int SlotCount = 20;
        public const int DangerCount = 3;
        public const int SafeCount = SlotCount - DangerCount;
        private readonly bool[] dangers = new bool[SlotCount];
        private readonly int[] objects = new int[SlotCount];
        private readonly int basePoints;
        private readonly double margin;
        public RoundState State { get; private set; } = RoundState.Ready;
        public int Crushed { get; private set; }
        public int CurrentObject => objects[Math.Min(Crushed, SlotCount - 1)];
        public double Multiplier => MultiplierAt(Crushed, margin);
        public long Reward => RewardFor(basePoints, Multiplier);
        public bool CanCrush => State == RoundState.Ready;
        public bool CanCollect => State == RoundState.Ready && Crushed > 0;
        public event Action Changed;

        public RoundManager(int seed, int basePoints = 100, double margin = .04)
        {
            if (basePoints < 1 || basePoints > 1000000) throw new ArgumentOutOfRangeException(nameof(basePoints));
            if (double.IsNaN(margin) || margin < 0 || margin > .2) throw new ArgumentOutOfRangeException(nameof(margin));
            this.basePoints = basePoints;
            this.margin = margin;
            var random = new Random(seed);
            for (int i = 0; i < SlotCount; i++) { objects[i] = i; dangers[i] = i < DangerCount; }
            Shuffle(objects, random);
            Shuffle(dangers, random);
        }

        private static void Shuffle<T>(T[] values, Random random)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T value = values[i]; values[i] = values[j]; values[j] = value;
            }
        }

        public bool TryBeginCrush(out bool dangerous)
        {
            dangerous = false;
            if (!CanCrush) return false;
            dangerous = dangers[Crushed];
            State = RoundState.Crushing;
            Changed?.Invoke();
            return true;
        }

        public bool ResolveCrush()
        {
            if (State != RoundState.Crushing) return false;
            if (dangers[Crushed]) State = RoundState.Failed;
            else { Crushed++; State = Crushed == SafeCount ? RoundState.Perfect : RoundState.Ready; }
            Changed?.Invoke();
            return true;
        }

        public bool TryCollect(out long points)
        {
            points = 0;
            if (!CanCollect) return false;
            points = Reward;
            State = RoundState.Collected;
            Changed?.Invoke();
            return true;
        }

        public static double SurvivalAt(int successes)
        {
            if (successes < 0 || successes > SafeCount) throw new ArgumentOutOfRangeException(nameof(successes));
            // C(20-k, 3) / C(20, 3), computed without factorials.
            int remaining = SlotCount - successes;
            return (double)(remaining * (remaining - 1) * (remaining - 2)) / (20 * 19 * 18);
        }

        public static double MultiplierAt(int successes, double margin = .04) => successes == 0 ? 1 : (1 - margin) / SurvivalAt(successes);
        private static long RewardFor(int stake, double multiplier) => (long)Math.Floor(stake * multiplier + 1e-8);

        // Copies for automated fairness audits; no risk data is sent to the HUD.
        public bool[] CopyDangerPositions() => (bool[])dangers.Clone();
        public int[] CopyObjectOrder() => (int[])objects.Clone();
    }
}
