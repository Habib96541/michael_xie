using System;
using System.Collections.Generic;

namespace HiLo.Core
{
    public readonly struct Card
    {
        public readonly int Rank; // 2..14; Ace is always high.
        public readonly int Suit;
        public Card(int rank, int suit) { Rank = rank; Suit = suit; }
        public string AssetName => "card_" + Suit + "_" + Rank;
        public string Label => (Rank == 14 ? "A" : Rank == 13 ? "K" : Rank == 12 ? "Q" : Rank == 11 ? "J" : Rank.ToString()) + new[] { "♠", "♥", "♣", "♦" }[Suit];
    }
    public readonly struct Odds
    {
        public readonly int Higher, Lower, Equal;
        public int Total => Higher + Lower + Equal;
        public Odds(int higher, int lower, int equal) { Higher = higher; Lower = lower; Equal = equal; }
        public int Wins(bool higher) => higher ? Higher : Lower;
        public double Probability(bool higher) => Total == 0 ? 0 : (double)Wins(higher) / Total;
        public double Factor(bool higher, double edge, double minimumGrowth = .01) => Wins(higher) == 0 ? 0 : Math.Max(1 + minimumGrowth, 1 + ((double)(Higher + Lower) / Wins(higher) - 1) * (1 - edge));
    }
    public sealed class CardDeckManager
    {
        readonly Random random;
        readonly List<Card> cards = new List<Card>(52);
        int cursor;
        public int Remaining => cards.Count - cursor;
        public int Shuffles { get; private set; }
        public CardDeckManager(int? seed = null) { random = seed.HasValue ? new Random(seed.Value) : new Random(); Shuffle(); }
        public void Shuffle()
        {
            cards.Clear(); cursor = 0;
            for (int suit = 0; suit < 4; suit++) for (int rank = 2; rank <= 14; rank++) cards.Add(new Card(rank, suit));
            for (int i = cards.Count - 1; i > 0; i--) { int j = random.Next(i + 1); Card c = cards[i]; cards[i] = cards[j]; cards[j] = c; }
            Shuffles++;
        }
        public void EnsureAvailable() { if (Remaining == 0) Shuffle(); }
        public Card Draw() { EnsureAvailable(); return cards[cursor++]; }
        public Odds GetOdds(Card current)
        {
            EnsureAvailable(); int hi = 0, lo = 0, eq = 0;
            for (int i = cursor; i < cards.Count; i++) { if (cards[i].Rank > current.Rank) hi++; else if (cards[i].Rank < current.Rank) lo++; else eq++; }
            return new Odds(hi, lo, eq);
        }
        // Test fixture only; no live outcome manipulation.
        public void SetTestSequence(params Card[] sequence) { cards.Clear(); cards.AddRange(sequence); cursor = 0; }
    }
}
