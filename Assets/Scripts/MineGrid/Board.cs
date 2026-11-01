using System;

namespace MineGrid
{
    public sealed class Board
    {
        private readonly bool[] _mines = new bool[25];
        private readonly bool[] _revealed = new bool[25];
        public int SafeRevealed { get; private set; }
        public int MineCount { get; }

        public Board(int count, System.Random random)
        {
            if (count < 1 || count > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            MineCount = count;
            var cells = new int[25];
            for (int i = 0; i < 25; i++)
            {
                cells[i] = i;
            }

            for (int i = 0; i < count; i++)
            {
                int j = random.Next(i, 25);
                (cells[i], cells[j]) = (cells[j], cells[i]);
                _mines[cells[i]] = true;
            }
        }

        public bool IsMine(int index) => _mines[index];
        public bool IsRevealed(int index) => _revealed[index];
        public bool Reveal(int index)
        {
            if (index < 0 || index >= 25 || _revealed[index])
            {
                return false;
            }

            _revealed[index] = true;
            if (!_mines[index])
            {
                SafeRevealed++;
            }

            return true;
        }
    }
}
