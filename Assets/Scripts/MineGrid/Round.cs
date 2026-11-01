using System;

namespace MineGrid
{
    public sealed class Round
    {
        public RoundState State { get; private set; } = RoundState.Setup;
        public Board Board { get; private set; }
        public int Score { get; private set; }
        public int Banked { get; private set; }
        public int TriggeredMine { get; private set; } = -1;
        public string Id { get; private set; }

        private readonly GameConfig _config;
        public Round(GameConfig config)
        {
            this._config = config;
        }

        public void Start(int mines, System.Random random)
        {
            if (State == RoundState.Playing)
            {
                return;
            }

            Board = new Board(mines, random);
            Id = Guid.NewGuid().ToString("N");
            TriggeredMine = -1;
            Score = Banked = 0;
            State = RoundState.Playing;
        }

        public bool Reveal(int index)
        {
            if (State != RoundState.Playing || !Board.Reveal(index))
            {
                return false;
            }

            if (Board.IsMine(index))
            {
                TriggeredMine = index;
                Score = 0;
                State = RoundState.Lost;
            }
            else
            {
                Score = _config.Score(Board.SafeRevealed, Board.MineCount);
                if (Board.SafeRevealed == 25 - Board.MineCount)
                {
                    Banked = Score;
                    State = RoundState.Won;
                }
            }

            return true;
        }

        public bool Finish()
        {
            if (State != RoundState.Playing)
            {
                return false;
            }

            Banked = Score;
            State = RoundState.Finished;
            return true;
        }

        public bool Forfeit()
        {
            if (State != RoundState.Playing)
            {
                return false;
            }

            Score = Banked = 0;
            State = RoundState.Lost;
            return true;
        }
    }
}
