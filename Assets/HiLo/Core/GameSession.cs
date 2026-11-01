using System;
using System.Numerics;

namespace HiLo.Core
{
    public enum RoundState { Ready, Decision, Revealing, Ended }
    public enum Outcome { Correct, Wrong, Push, CashOut, Timeout }
    public sealed class GameSession
    {
        public readonly CardDeckManager Deck;
        public ScientificNumber Balance { get; private set; }
        public ScientificNumber Multiplier { get; private set; } = ScientificNumber.One;
        public ScientificNumber Payout => Multiplier.Multiply(Stake);
        public BigInteger Streak { get; private set; }
        public Card Current { get; private set; }
        public RoundState State { get; private set; } = RoundState.Ready;
        public Outcome LastOutcome { get; private set; }
        public Odds Odds => Deck.GetOdds(Current);
        public readonly double Stake, HouseEdge, MinimumGrowth;
        public bool CanAfford => Balance.CompareTo(new ScientificNumber(Stake)) >= 0;
        public GameSession(ScientificNumber balance, double stake = 10, double houseEdge = 0.04, CardDeckManager deck = null, double minimumGrowth = .01)
        { Balance = balance; Stake = stake; HouseEdge = houseEdge; MinimumGrowth = minimumGrowth; Deck = deck ?? new CardDeckManager(); }
        public bool Begin()
        {
            if (State == RoundState.Decision || State == RoundState.Revealing || !CanAfford) return false;
            Balance = Balance.Subtract(new ScientificNumber(Stake)); Multiplier = ScientificNumber.One; Streak = 0;
            Deck.Shuffle(); Current = Deck.Draw(); State = RoundState.Decision; return true;
        }
        public bool Predict(bool higher)
        {
            if (State != RoundState.Decision) return false;
            Odds odds = Odds;
            if (odds.Wins(higher) == 0 && odds.Higher + odds.Lower > 0) return false;
            double factor = odds.Factor(higher, HouseEdge, MinimumGrowth);
            Card next = Deck.Draw();
            if (next.Rank == Current.Rank) LastOutcome = Outcome.Push;
            else if ((next.Rank > Current.Rank) == higher) { LastOutcome = Outcome.Correct; Streak++; Multiplier = Multiplier.Multiply(factor); }
            else LastOutcome = Outcome.Wrong;
            Current = next; State = RoundState.Revealing; return true;
        }
        public void CompleteReveal() { if (State == RoundState.Revealing) { State = LastOutcome == Outcome.Wrong ? RoundState.Ended : RoundState.Decision; if (State == RoundState.Decision) Deck.EnsureAvailable(); } }
        public bool CashOut() { if (State != RoundState.Decision) return false; Balance = Balance.Add(Payout); LastOutcome = Outcome.CashOut; State = RoundState.Ended; return true; }
        public bool Timeout() { if (State != RoundState.Decision) return false; LastOutcome = Outcome.Timeout; State = RoundState.Ended; return true; }
        public bool Refill(double amount) { if (CanAfford || State == RoundState.Decision || State == RoundState.Revealing) return false; Balance = Balance.Add(new ScientificNumber(amount)); return true; }
    }
}
