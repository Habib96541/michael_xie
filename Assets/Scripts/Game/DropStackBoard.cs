using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DropStack.Data;

namespace DropStack.Game
{
    public class DropStackBoard : MonoBehaviour
    {
        public const int Columns = 10;
        public const int VisibleRows = 20;
        public const int BufferRows = 4;
        public const int TotalRows = VisibleRows + BufferRows; // 24

        // 2D discrete matrix: 0 = empty, 1..16 = shape type (used for color)
        private int[,] matrix = new int[Columns, TotalRows];

        // Events
        public event Action<List<int>> OnLinesClearing;
        public event Action<int> OnLinesCleared; // count
        public event Action OnBoardChanged;

        public bool IsClearingLines { get; private set; }

        private void Awake()
        {
            ClearMatrix();
        }

        public void ClearMatrix()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < TotalRows; y++)
                {
                    matrix[x, y] = 0;
                }
            }
            IsClearingLines = false;
            OnBoardChanged?.Invoke();
        }

        public int GetCell(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= TotalRows)
                return -1;
            return matrix[x, y];
        }

        public bool IsCellOccupied(int x, int y)
        {
            if (x < 0 || x >= Columns || y < 0 || y >= TotalRows)
                return true;
            return matrix[x, y] != 0;
        }

        public bool IsValidPosition(PolyominoShape shape, Vector2Int boardPos, int rotationIndex)
        {
            if (shape == null) return false;
            Vector2Int[] blocks = shape.GetBlocks(rotationIndex);

            foreach (var b in blocks)
            {
                int x = boardPos.x + b.x;
                int y = boardPos.y + b.y;

                // Out of horizontal bounds
                if (x < 0 || x >= Columns) return false;

                // Below floor
                if (y < 0) return false;

                // Ceiling check: Can be in buffer zone (y >= VisibleRows), but not beyond TotalRows
                if (y >= TotalRows) return false;

                // Occupied by locked piece
                if (matrix[x, y] != 0) return false;
            }

            return true;
        }

        public void LockPiece(PolyominoShape shape, Vector2Int boardPos, int rotationIndex)
        {
            if (shape == null) return;
            Vector2Int[] blocks = shape.GetBlocks(rotationIndex);
            int shapeId = (int)shape.type;

            foreach (var b in blocks)
            {
                int x = boardPos.x + b.x;
                int y = boardPos.y + b.y;

                if (x >= 0 && x < Columns && y >= 0 && y < TotalRows)
                {
                    matrix[x, y] = shapeId;
                }
            }

            OnBoardChanged?.Invoke();
        }

        public List<int> FindFullLines()
        {
            List<int> fullLines = new List<int>();

            for (int y = 0; y < VisibleRows; y++)
            {
                bool full = true;
                for (int x = 0; x < Columns; x++)
                {
                    if (matrix[x, y] == 0)
                    {
                        full = false;
                        break;
                    }
                }

                if (full)
                {
                    fullLines.Add(y);
                }
            }

            return fullLines;
        }

        public IEnumerator ClearLinesCoroutine(List<int> linesToClear, Action onComplete)
        {
            if (linesToClear == null || linesToClear.Count == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            IsClearingLines = true;
            OnLinesClearing?.Invoke(linesToClear);

            // Flash & explosion delay
            yield return new WaitForSeconds(0.20f);

            // Shift rows down
            // Sort lines ascending
            linesToClear.Sort();

            // Shift down for each cleared line
            // Process from bottom to top
            int dropDistance = 0;
            HashSet<int> clearedSet = new HashSet<int>(linesToClear);

            for (int y = 0; y < TotalRows; y++)
            {
                if (clearedSet.Contains(y))
                {
                    dropDistance++;
                }
                else if (dropDistance > 0)
                {
                    for (int x = 0; x < Columns; x++)
                    {
                        matrix[x, y - dropDistance] = matrix[x, y];
                    }
                }
            }

            // Fill top emptied rows with 0
            for (int y = TotalRows - dropDistance; y < TotalRows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    matrix[x, y] = 0;
                }
            }

            IsClearingLines = false;
            OnLinesCleared?.Invoke(linesToClear.Count);
            OnBoardChanged?.Invoke();

            onComplete?.Invoke();
        }

        public bool IsCompletelyEmpty()
        {
            for (int y = 0; y < TotalRows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (matrix[x, y] != 0) return false;
                }
            }
            return true;
        }

        public bool HasBlocksAboveCeiling()
        {
            for (int y = VisibleRows; y < TotalRows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    if (matrix[x, y] != 0) return true;
                }
            }
            return false;
        }
    }
}
