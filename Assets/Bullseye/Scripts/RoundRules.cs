using System;

namespace BullseyeBlitz
{
    public enum RoundState { Home, Countdown, Playing, Success, BombHit, Results }

    public sealed class RoundRules
    {
        public RoundState State { get; private set; } = RoundState.Home;
        public double Time { get; private set; }
        public int Pending { get; private set; }
        public int Lost { get; private set; }
        public string Id { get; private set; }
        public bool Paused { get; set; }
        public void BeginCountdown()
        {
            Id = Guid.NewGuid().ToString("N");
            Time = 0;
            Pending = 0;
            Lost = 0;
            Paused = false;
            State = RoundState.Countdown;
        }
        public void Start() { if (State == RoundState.Countdown) { State = RoundState.Playing; } }
        public void Advance(double delta)
        {
            if (State != RoundState.Playing || Paused) { return; }
            Time = Math.Min(20, Time + Math.Max(0, delta));
            if (Time >= 20) { State = RoundState.Success; }
        }
        public bool Hit(bool bomb, int value)
        {
            if (State != RoundState.Playing || Paused || Time >= 20) { return false; }
            if (bomb)
            {
                Lost = Pending;
                Pending = 0;
                State = RoundState.BombHit;
            }
            else { Pending = checked(Pending + value); }
            return true;
        }
        public void ShowResults() { State = RoundState.Results; }
        public void Abandon() { Pending = 0; Paused = false; State = RoundState.Home; }
    }
}
