using System;
using UnityEngine;

namespace ChickenRush
{
    public enum RoundState { Menu, Preparing, Safe, Hopping, Lost, CashedOut }

    // Currency and state authority live here, never in animation callbacks.
    public sealed class RoundRules
    {
        public RoundState State { get; private set; } = RoundState.Menu;
        public int Balance { get; private set; }
        public int Stake { get; private set; } = 1;
        public int Row { get; private set; }
        public int Reward { get; private set; }
        public float Multiplier => Row == 0 ? 1f : progression[Row - 1];
        public int PotentialReward => (int)decimal.Floor(Stake * (decimal)Multiplier);
        public int NextPotentialReward => (int)decimal.Floor(Stake*(decimal)(Row<progression.Length?progression[Row]:Multiplier));
        public bool Finished => State == RoundState.Lost || State == RoundState.CashedOut;
        public bool CanStart => State == RoundState.Menu || Finished;
        readonly float[] progression;
        readonly float[] successChances;
        int roundSeed;
        public bool PlannedSuccess { get; private set; }
        public bool UsesRisk => successChances!=null;
        public float NextChance => successChances==null || Row>=successChances.Length?1:successChances[Row];

        public RoundRules(int balance, float[] multipliers,float[] probabilities=null)
        {
            if (multipliers == null || multipliers.Length == 0) throw new ArgumentException("A progression is required.");
            if(probabilities!=null)
            {
                if(probabilities.Length!=multipliers.Length)throw new ArgumentException("One probability per lane is required.");
                foreach(float p in probabilities)if(float.IsNaN(p) || p<=0 || p>1)throw new ArgumentException("Invalid success chance.");
                successChances=(float[])probabilities.Clone();
            }
            Balance = Math.Max(0, balance);
            progression = (float[])multipliers.Clone();
        }
        public void SetStake(int value) { if (CanStart) Stake = Math.Max(1, Math.Min(Math.Max(1, Balance), value)); }
        public bool Start(int seed=0)
        {
            if (!CanStart || Balance < Stake) return false;
            Balance -= Stake;
            Row = Reward = 0;
            roundSeed=seed;
            State = RoundState.Preparing;
            return true;
        }
        public void Ready() { if (State == RoundState.Preparing) State = RoundState.Safe; }
        public bool Hop()
        {
            if (State != RoundState.Safe || Row >= progression.Length) return false;
            PlannedSuccess=successChances==null || SeededRisk.Survives(roundSeed,Row+1,NextChance);
            State = RoundState.Hopping;
            return true;
        }
        public bool Land()
        {
            if (State != RoundState.Hopping || (UsesRisk && !PlannedSuccess)) return false;
            Row++;
            State = RoundState.Safe;
            return true;
        }
        public bool Hit()
        {
            if (State != RoundState.Hopping || (UsesRisk && PlannedSuccess)) return false;
            State = RoundState.Lost;
            return true;
        }
        public bool CashOut()
        {
            if (State != RoundState.Safe || Row == 0) return false;
            Reward = PotentialReward;
            Balance += Reward;
            State = RoundState.CashedOut;
            return true;
        }
        public bool Refill()
        {
            if (!CanStart || Balance != 0) return false;
            Balance = 10;
            Stake = 1;
            return true;
        }
    }
}
