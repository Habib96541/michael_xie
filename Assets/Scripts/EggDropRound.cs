using System;

namespace EggDrop
{
    public enum RoundState { Ready, Falling, Collected, Cracked }

    public sealed class EggDropRound
    {
        public const int MaximumFloor = 12;
        public const int StartingPoints = 100;

        public RoundState State { get; private set; } = RoundState.Ready;
        public int SuccessfulDrops { get; private set; }
        public int BankedPoints { get; private set; }
        public int Floor => Math.Min(SuccessfulDrops + 1, MaximumFloor);
        public float CrackChance => 0.08f + (Floor - 1) * 0.055f;
        public float Multiplier => SuccessfulDrops == 0 ? 0f : MultiplierFor(SuccessfulDrops);
        public float NextMultiplier => MultiplierFor(Floor);
        public int RoundPoints => State == RoundState.Cracked ? 0 : (int)Math.Round(StartingPoints * Multiplier);
        public bool CanDrop => State == RoundState.Ready && SuccessfulDrops < MaximumFloor;
        public bool CanCollect => State == RoundState.Ready && SuccessfulDrops > 0;

        public bool TryDrop()
        {
            if (!CanDrop) return false;
            State = RoundState.Falling;
            return true;
        }

        public bool ResolveLanding(float roll)
        {
            if (State != RoundState.Falling)
                throw new InvalidOperationException("A landing requires a drop in progress.");
            if (float.IsNaN(roll) || roll < 0f || roll >= 1f)
                throw new ArgumentOutOfRangeException(nameof(roll));

            if (roll < CrackChance)
            {
                State = RoundState.Cracked;
                return false;
            }

            SuccessfulDrops++;
            State = RoundState.Ready;
            return true;
        }

        public bool TryCollect()
        {
            if (!CanCollect) return false;
            BankedPoints += RoundPoints;
            State = RoundState.Collected;
            return true;
        }

        public void Restart()
        {
            if (State != RoundState.Collected && State != RoundState.Cracked) return;
            SuccessfulDrops = 0;
            State = RoundState.Ready;
        }

        private static float MultiplierFor(int floor) => 1f + floor * 0.25f + floor * floor * 0.07f;
    }
}
