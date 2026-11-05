namespace DiceGame.Core
{
    public readonly struct DiceRollResult
    {
        public float RollValue { get; }
        public bool IsWin { get; }
        public bool IsCrit { get; }
        public long BetAmount { get; }
        public long PayoutAmount { get; }
        public float Multiplier { get; }
        public int ComboStreak { get; }
        public bool FeverActive { get; }
        public float TargetValue { get; }
        public bool IsRollUnder { get; }

        public DiceRollResult(
            float rollValue,
            bool isWin,
            bool isCrit,
            long betAmount,
            long payoutAmount,
            float multiplier,
            int comboStreak,
            bool feverActive,
            float targetValue,
            bool isRollUnder)
        {
            RollValue = rollValue;
            IsWin = isWin;
            IsCrit = isCrit;
            BetAmount = betAmount;
            PayoutAmount = payoutAmount;
            Multiplier = multiplier;
            ComboStreak = comboStreak;
            FeverActive = feverActive;
            TargetValue = targetValue;
            IsRollUnder = isRollUnder;
        }
    }
}
