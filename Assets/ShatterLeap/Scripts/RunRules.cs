using System;

namespace ShatterLeap
{
    public enum RunState { Home, Preview, CameraTransition, Playing, Jumping, Won, Falling, TimedOut, Results }

    // Pure rules: no animation, physics, UI, or wallet side effects.
    public sealed class RunRules
    {
        private readonly int[] _safeSides;
        private readonly int _stepValue;
        public string Id { get; } = Guid.NewGuid().ToString("N");
        public RunState State { get; private set; } = RunState.Preview;
        public RunState Outcome { get; private set; }
        public Difficulty Difficulty { get; }
        public int Steps { get; private set; }
        public int Earnings { get; private set; }
        public float Remaining { get; private set; }
        public int ChosenSide { get; private set; }
        public bool AtFinish => Steps == _safeSides.Length;
        public bool IsTerminal => State == RunState.Won || State == RunState.Falling || State == RunState.TimedOut || State == RunState.Results;

        public RunRules(Difficulty difficulty, int stepValue, int seed)
        {
            Difficulty = difficulty;
            _stepValue = stepValue;
            Remaining = difficulty.PlaySeconds;
            _safeSides = new int[difficulty.Rows];
            Random random = new Random(seed);
            for (int i = 0; i < _safeSides.Length; i++)
            {
                _safeSides[i] = random.Next(2);
            }
        }

        public int SafeSide(int row) => _safeSides[row];
        public bool BeginTransition()
        {
            if (State != RunState.Preview) { return false; }
            State = RunState.CameraTransition;
            return true;
        }
        public bool BeginPlaying()
        {
            if (State != RunState.CameraTransition) { return false; }
            State = RunState.Playing;
            return true;
        }
        public bool Choose(int side)
        {
            if (State != RunState.Playing || AtFinish || side < 0 || side > 1) { return false; }
            ChosenSide = side;
            State = RunState.Jumping;
            return true;
        }
        public bool BeginFinish()
        {
            if (State != RunState.Playing || !AtFinish) { return false; }
            State = RunState.Jumping;
            return true;
        }
        public bool Tick(float seconds)
        {
            if (State != RunState.Playing && State != RunState.Jumping) { return false; }
            Remaining = Math.Max(0, Remaining - Math.Max(0, seconds));
            return Remaining <= 0 && End(RunState.TimedOut);
        }
        public bool Land(bool validSupport)
        {
            if (State != RunState.Jumping || Remaining <= 0) { return false; }
            if (!validSupport || (!AtFinish && ChosenSide != _safeSides[Steps]))
            {
                End(RunState.Falling);
                return false;
            }
            if (AtFinish)
            {
                return End(RunState.Won);
            }
            Steps++;
            Earnings = checked(Earnings + Steps * _stepValue);
            State = RunState.Playing;
            return true;
        }
        public bool End(RunState outcome)
        {
            if (IsTerminal || (outcome != RunState.Won && outcome != RunState.Falling && outcome != RunState.TimedOut)) { return false; }
            if (outcome == RunState.Won && (!AtFinish || State != RunState.Jumping || Remaining <= 0)) { return false; }
            Outcome = outcome;
            State = outcome;
            return true;
        }
        public void ShowResults()
        {
            if (IsTerminal) { State = RunState.Results; }
        }
        public int Payout => Outcome == RunState.Won ? checked(Earnings + Difficulty.Bonus) : 0;
    }
}
