using System;
using System.Collections.Generic;

namespace CoinFlip
{
    public enum CoinSide { Heads, Tails }
    public enum RoundState { Idle, SideSelected, Flipping, Result, Ready }

    [Serializable]
    public sealed class SaveData
    {
        public int version = 1;
        public int balance = 10;
        public int wager = 1;
        public int selected = -1;
        public int streak;
        public int bestStreak;
        public int roundId;
        public int pendingResult = -1;
        public int pendingChoice = -1;
        public int pendingWager;
        public float sfx = .7f;
        public float music = .15f;
        public bool muted;
        public List<int> history = new List<int>();

        public bool IsValid()
        {
            if (version != 1 || balance < 0 || balance > 1000000000 || wager < 1 || wager > 99 ||
                selected < -1 || selected > 1 || streak < 0 || bestStreak < streak || roundId < 0 ||
                float.IsNaN(sfx) || float.IsNaN(music) || sfx < 0 || sfx > 1 || music < 0 || music > 1 ||
                history == null || history.Count > 8 || pendingResult < -1 || pendingResult > 1) return false;
            foreach (int face in history) if (face < 0 || face > 1) return false;
            if (pendingResult >= 0 && (pendingChoice < 0 || pendingChoice > 1 || pendingWager < 1 || pendingWager > 99)) return false;
            return true;
        }
    }

    public readonly struct Round
    {
        public readonly int Id, Wager;
        public readonly CoinSide Choice, Outcome;
        public bool Won => Choice == Outcome;
        public Round(int id, int wager, CoinSide choice, CoinSide outcome)
        { Id = id; Wager = wager; Choice = choice; Outcome = outcome; }
    }

    // Sole authority for currency, outcomes, and settlement. Presentation never pays out.
    public sealed class GameSession
    {
        readonly Func<CoinSide> outcomeSource;
        public SaveData Data { get; }
        public RoundState State { get; private set; }
        public bool Busy => State == RoundState.Flipping;
        public bool CanFlip => !Busy && Data.selected >= 0 && Data.wager >= 1 && Data.wager <= Data.balance;
        public event Action Changed;

        public GameSession(SaveData data, Func<CoinSide> outcomeSource)
        {
            Data = data != null && data.IsValid() ? data : new SaveData();
            this.outcomeSource = outcomeSource ?? throw new ArgumentNullException(nameof(outcomeSource));
            // A saved in-flight round already had its stake deducted. Resolve the stored
            // result before showing controls, rather than refunding or drawing again.
            if (Data.pendingResult >= 0)
            {
                State = RoundState.Flipping;
                Complete(new Round(Data.roundId, Data.pendingWager, (CoinSide)Data.pendingChoice, (CoinSide)Data.pendingResult));
            }
            State = Data.selected >= 0 ? RoundState.Ready : RoundState.Idle;
        }

        public bool Select(CoinSide side)
        {
            if (Busy || (int)side < 0 || (int)side > 1) return false;
            Data.selected = (int)side; State = RoundState.SideSelected; Changed?.Invoke(); return true;
        }
        public bool SetWager(int wager)
        {
            if (Busy || wager < 1 || wager > 99) return false;
            Data.wager = wager; Changed?.Invoke(); return true;
        }
        public bool TryBegin(out Round round)
        {
            round = default;
            if (!CanFlip) return false;
            // Exactly one unbiased binary draw, only after all validation succeeds.
            CoinSide outcome = outcomeSource();
            if ((int)outcome < 0 || (int)outcome > 1) throw new InvalidOperationException("Invalid coin outcome");
            State = RoundState.Flipping;
            Data.roundId++;
            Data.pendingResult = (int)outcome; Data.pendingChoice = Data.selected; Data.pendingWager = Data.wager;
            Data.balance -= Data.wager;
            round = new Round(Data.roundId, Data.wager, (CoinSide)Data.selected, outcome);
            Changed?.Invoke(); return true;
        }
        public bool Complete(Round round)
        {
            if (!Busy || Data.pendingResult < 0 || round.Id != Data.roundId ||
                (int)round.Outcome != Data.pendingResult || (int)round.Choice != Data.pendingChoice || round.Wager != Data.pendingWager) return false;
            if (round.Won) { Data.balance += round.Wager * 2; Data.streak++; }
            else Data.streak = 0;
            Data.bestStreak = Math.Max(Data.bestStreak, Data.streak);
            Data.history.Insert(0, (int)round.Outcome);
            if (Data.history.Count > 8) Data.history.RemoveAt(8);
            Data.pendingResult = -1; Data.pendingChoice = -1; Data.pendingWager = 0;
            Data.wager = Math.Max(1, Math.Min(Data.wager, Data.balance));
            State = RoundState.Result; Changed?.Invoke(); return true;
        }
        public void Ready() { if (State == RoundState.Result) { State = RoundState.Ready; Changed?.Invoke(); } }
        public bool Reset()
        {
            if (Busy) return false;
            Data.balance = 10; Data.wager = 1; Data.streak = 0; Data.history.Clear();
            State = Data.selected >= 0 ? RoundState.Ready : RoundState.Idle;
            Changed?.Invoke(); return true;
        }
    }
}
