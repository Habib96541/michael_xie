using System;
using UnityEngine;

namespace CyberScratch.Data
{
    public class TicketModel
    {
        public const int Rows = 5;
        public const int Columns = 3;
        public const int TotalCells = Rows * Columns;

        private readonly SymbolType[,] _grid = new SymbolType[Rows, Columns];
        private readonly bool[,] _revealed = new bool[Rows, Columns];
        private readonly float[] _rowPrizes = new float[Rows];
        private readonly bool[] _rowWins = new bool[Rows];
        private readonly bool[] _rowNearMisses = new bool[Rows];

        private float _ticketCost;
        private int _totalRevealedCount;

        public float TicketCost => _ticketCost;
        public int TotalRevealedCount => _totalRevealedCount;
        public bool IsFullyCompleted => _totalRevealedCount >= TotalCells;

        public SymbolType GetSymbol(int row, int col) => _grid[row, col];
        public bool IsCellRevealed(int row, int col) => _revealed[row, col];
        public float GetRowPrize(int row) => _rowPrizes[row];
        public bool IsRowWin(int row) => _rowWins[row];
        public bool IsRowNearMiss(int row) => _rowNearMisses[row];

        public void Generate(float ticketCost, SymbolDefinition[] symbolDefs, float rowWinChance = 0.32f, float nearMissRate = 0.45f)
        {
            _ticketCost = ticketCost;
            _totalRevealedCount = 0;
            float rowStake = Mathf.Max(0.2f, ticketCost / Rows);

            // Calculate total weight for RNG
            float totalWeight = 0f;
            for (int i = 0; i < symbolDefs.Length; i++)
            {
                totalWeight += symbolDefs[i].Weight;
            }

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    _revealed[r, c] = false;
                }

                bool willWin = UnityEngine.Random.value < rowWinChance;
                if (willWin)
                {
                    SymbolType winSymbol = RollSymbol(symbolDefs, totalWeight);
                    _grid[r, 0] = winSymbol;
                    _grid[r, 1] = winSymbol;
                    _grid[r, 2] = winSymbol;

                    _rowWins[r] = true;
                    _rowNearMisses[r] = false;

                    float multiplier = GetMultiplier(winSymbol, symbolDefs);
                    _rowPrizes[r] = Mathf.Round(rowStake * multiplier * 100f) / 100f;
                }
                else
                {
                    _rowWins[r] = false;
                    bool isNearMiss = UnityEngine.Random.value < nearMissRate;
                    _rowNearMisses[r] = isNearMiss;

                    if (isNearMiss)
                    {
                        SymbolType matchSymbol = RollSymbol(symbolDefs, totalWeight);
                        _grid[r, 0] = matchSymbol;
                        _grid[r, 1] = matchSymbol;

                        // Ensure 3rd symbol is different
                        SymbolType thirdSymbol;
                        do
                        {
                            thirdSymbol = RollSymbol(symbolDefs, totalWeight);
                        } while (thirdSymbol == matchSymbol);
                        _grid[r, 2] = thirdSymbol;

                        float potentialMult = GetMultiplier(matchSymbol, symbolDefs);
                        _rowPrizes[r] = Mathf.Round(rowStake * potentialMult * 100f) / 100f;
                    }
                    else
                    {
                        // 3 symbols without full match
                        SymbolType s0 = RollSymbol(symbolDefs, totalWeight);
                        SymbolType s1 = RollSymbol(symbolDefs, totalWeight);
                        SymbolType s2 = RollSymbol(symbolDefs, totalWeight);

                        if (s0 == s1 && s1 == s2)
                        {
                            // Avoid accidental win on losing row
                            s2 = (SymbolType)(((int)s2 + 1) % symbolDefs.Length);
                        }

                        _grid[r, 0] = s0;
                        _grid[r, 1] = s1;
                        _grid[r, 2] = s2;

                        float mult = GetMultiplier(s0, symbolDefs);
                        _rowPrizes[r] = Mathf.Round(rowStake * mult * 100f) / 100f;
                    }
                }
            }
        }

        private SymbolType RollSymbol(SymbolDefinition[] defs, float totalWeight)
        {
            float roll = UnityEngine.Random.value * totalWeight;
            float accum = 0f;
            for (int i = 0; i < defs.Length; i++)
            {
                accum += defs[i].Weight;
                if (roll <= accum)
                {
                    return defs[i].Type;
                }
            }
            return defs[defs.Length - 1].Type;
        }

        private float GetMultiplier(SymbolType type, SymbolDefinition[] defs)
        {
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i].Type == type)
                {
                    return defs[i].Multiplier;
                }
            }
            return 1f;
        }

        public bool MarkRevealed(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Columns)
            {
                return false;
            }

            if (_revealed[row, col])
            {
                return false;
            }

            _revealed[row, col] = true;
            _totalRevealedCount++;
            return true;
        }

        public bool IsRowFullyRevealed(int row)
        {
            return _revealed[row, 0] && _revealed[row, 1] && _revealed[row, 2];
        }

        public bool IsRowNearMissActive(int row)
        {
            // First 2 revealed and match, 3rd not yet revealed
            return _revealed[row, 0] && _revealed[row, 1] && !_revealed[row, 2] && (_grid[row, 0] == _grid[row, 1]);
        }

        public float CalculateTotalWinnings()
        {
            float total = 0f;
            for (int r = 0; r < Rows; r++)
            {
                if (_rowWins[r])
                {
                    total += _rowPrizes[r];
                }
            }
            return total;
        }

        public int GetWinningRowCount()
        {
            int count = 0;
            for (int r = 0; r < Rows; r++)
            {
                if (_rowWins[r]) count++;
            }
            return count;
        }
    }
}
