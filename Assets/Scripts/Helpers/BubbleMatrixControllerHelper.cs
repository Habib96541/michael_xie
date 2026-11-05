using UnityEngine;
using System.Collections;

namespace com.javierquevedo
{
    public class BubbleMatrixControllerHelper
    {
        /* Returns the position in the matrix given a position and a matrix geometry
         * @param {Vector3} position. Position of the object
         * @param {BubbleMatrixGeometry} geometry. Geometry of the matrix
         * @param {bool} isBaselineAlignedLeft. True if the first(top) row of the matrix is aligned to the left border
         * @return Vector2 position of the object in the matrix.
         */
        public static Vector2 CellForPosition(Vector3 position, BubbleMatrixGeometry geometry, bool isBaselineAlignedLeft)
        {
            int row = geometry.rows - Mathf.FloorToInt(position.y) - 1;
            if (row < 0) row = 0;
            int column;
            bool rowIsEven = row % 2 == 0;
            if ((rowIsEven && isBaselineAlignedLeft) || (!rowIsEven && !isBaselineAlignedLeft))
            {
                column = Mathf.FloorToInt(position.x);
            }
            else
            {
                column = Mathf.FloorToInt(position.x - geometry.bubbleRadius);
            }
            Vector2 result = new Vector2(row, JQMath.TruncateToInterval(column, 0, geometry.columns - 1));
            return result;
        }

        public static Vector3 PositionForCell(Vector2 cell, BubbleMatrixGeometry geometry, bool isBaselineAlignedLeft)
        {
            // cell.x = row in the matrix
            // cell.y = column in the matrix
            int rowInt = Mathf.RoundToInt(cell.x);
            bool rowIsEven = (rowInt % 2) == 0;
            float y = geometry.rows - cell.x - geometry.bubbleRadius;
            float x;
            if (isBaselineAlignedLeft)
            {
                if (rowIsEven)
                    x = cell.y + geometry.bubbleRadius;
                else
                    x = cell.y + 2f * geometry.bubbleRadius;
            }
            else
            {
                if (rowIsEven)
                    x = cell.y + 2f * geometry.bubbleRadius;
                else
                    x = cell.y + geometry.bubbleRadius;
            }
            return new Vector3(x, y, geometry.depth);
        }

        /// <summary>
        /// Finds the closest valid, unoccupied hexagonal matrix cell for a colliding orb
        /// so that it never overwrites an existing orb or floats disconnected.
        /// Returns row == geometry.rows if the orb snapped below the playfield (overflow/loss).
        /// </summary>
        public static Vector2 FindBestSnapCell(Vector3 worldPos, BubbleMatrix matrix, BubbleMatrixGeometry geometry)
        {
            Vector2 directCell = CellForPosition(worldPos, geometry, matrix.isBaselineAlignedLeft);
            int directRow = Mathf.RoundToInt(directCell.x);
            int directCol = Mathf.RoundToInt(directCell.y);

            // Check if the ball fell below the allowed rows
            if (directRow >= geometry.rows)
                return new Vector2(geometry.rows, Mathf.Clamp(directCol, 0, geometry.columns - 1));

            // If direct cell is valid, empty, and either at top row or adjacent to an occupied neighbor, use it if closest
            float bestDistSq = float.MaxValue;
            Vector2 bestCell = new Vector2(-1, -1);

            for (int r = 0; r < geometry.rows; r++)
            {
                for (int c = 0; c < geometry.columns; c++)
                {
                    if (matrix.hasBubble(r, c))
                        continue;

                    // Must either be in top row (r == 0) or adjacent to at least one existing bubble
                    if (r > 0 && !HasOccupiedNeighbor(matrix, r, c, geometry.rows, geometry.columns))
                        continue;

                    Vector3 cellWorld = PositionForCell(new Vector2(r, c), geometry, matrix.isBaselineAlignedLeft);
                    float dx = worldPos.x - cellWorld.x;
                    float dy = worldPos.y - cellWorld.y;
                    float distSq = dx * dx + dy * dy;

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestCell = new Vector2(r, c);
                    }
                }
            }

            if (bestCell.x >= 0)
                return bestCell;

            // Fallback: check if an adjacent cell in row == geometry.rows was closest (overflow)
            if (directRow >= geometry.rows - 1 && matrix.hasBubble(Mathf.Clamp(directRow, 0, geometry.rows - 1), directCol))
                return new Vector2(geometry.rows, directCol);

            return new Vector2(
                Mathf.Clamp(directRow, 0, geometry.rows - 1),
                Mathf.Clamp(directCol, 0, geometry.columns - 1)
            );
        }

        public static bool HasOccupiedNeighbor(BubbleMatrix matrix, int row, int col, int totalRows, int totalCols)
        {
            if (col > 0 && matrix.hasBubble(row, col - 1)) return true;
            if (col < totalCols - 1 && matrix.hasBubble(row, col + 1)) return true;

            bool isRowEven = (row % 2) == 0;
            if ((matrix.isBaselineAlignedLeft && isRowEven) || (!matrix.isBaselineAlignedLeft && !isRowEven))
            {
                if (row > 0)
                {
                    if (col > 0 && matrix.hasBubble(row - 1, col - 1)) return true;
                    if (matrix.hasBubble(row - 1, col)) return true;
                }
                if (row < totalRows - 1)
                {
                    if (col > 0 && matrix.hasBubble(row + 1, col - 1)) return true;
                    if (matrix.hasBubble(row + 1, col)) return true;
                }
            }
            else
            {
                if (row > 0)
                {
                    if (matrix.hasBubble(row - 1, col)) return true;
                    if (col < totalCols - 1 && matrix.hasBubble(row - 1, col + 1)) return true;
                }
                if (row < totalRows - 1)
                {
                    if (matrix.hasBubble(row + 1, col)) return true;
                    if (col < totalCols - 1 && matrix.hasBubble(row + 1, col + 1)) return true;
                }
            }
            return false;
        }
    }
}
