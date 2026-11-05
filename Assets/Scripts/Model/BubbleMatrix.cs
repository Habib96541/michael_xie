using UnityEngine;
using System.Collections;

namespace com.javierquevedo
{
    public class BubbleMatrix
    {
        /* Public properties */
        public bool isBaselineAlignedLeft; // Determines if the top row of the matrix is aligned to the LEFT

        /* Private iVars */
        private int _rows;
        private int _columns;
        private Bubble[,] _bubbleMatrix;

        public BubbleMatrix(int rows, int columns)
        {
            isBaselineAlignedLeft = true;
            this._rows = rows;
            this._columns = columns;
            this._bubbleMatrix = new Bubble[rows, columns];
        }

        public void clear()
        {
            isBaselineAlignedLeft = true;
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _columns; c++)
                {
                    _bubbleMatrix[r, c] = null;
                }
            }
        }

        public void insert(Bubble bubble, int x, int y)
        {
            if (x < 0 || x > this._rows - 1 || y < 0 || y > this._columns - 1)
                throw new System.ArgumentException("Adding Bubble to illegal coordinates");

            this._bubbleMatrix[x, y] = bubble;
        }

        public void remove(int x, int y)
        {
            if (x < 0 || x > this._rows - 1 || y < 0 || y > this._columns - 1)
                throw new System.ArgumentException("Removing Bubble from illegal coordinates");

            this._bubbleMatrix[x, y] = null;
        }

        public void remove(Bubble bubble)
        {
            Vector2 location = this.location(bubble);
            if ((int)location.x > -1 && (int)location.y > -1)
                this.remove((int)location.x, (int)location.y);
        }

        public ArrayList bubbles
        {
            get
            {
                ArrayList list = new ArrayList();
                for (int i = 0; i < this._rows; i++)
                {
                    for (int j = 0; j < this._columns; j++)
                    {
                        if (this._bubbleMatrix[i, j] != null)
                            list.Add(this._bubbleMatrix[i, j]);
                    }
                }
                return list;
            }
        }

        public bool hasBubble(int row, int column)
        {
            if (row < 0 || row >= _rows || column < 0 || column >= _columns)
                return false;
            return this.bubble(row, column) != null;
        }

        public Bubble bubble(int row, int column)
        {
            if (row < 0 || row >= _rows || column < 0 || column >= _columns)
                return null;
            return this._bubbleMatrix[row, column];
        }

        public ArrayList neighbours(Vector2 location)
        {
            int row = (int)location.x;
            int column = (int)location.y;

            ArrayList _neighbours = new ArrayList();
            if (row < 0 || row > this._rows - 1 || column < 0 || column > this._columns - 1)
                return _neighbours;

            if (this._bubbleMatrix[row, column] != null)
            {
                _neighbours.AddNotNull(this._bubbleMatrix[row, column]);

                // Left and right neighbours
                if (column > 0) _neighbours.AddNotNull(this._bubbleMatrix[row, column - 1]);
                if (column < this._columns - 1) _neighbours.AddNotNull(this._bubbleMatrix[row, column + 1]);

                // Higher and lower neighbours
                bool isRowEven = row % 2 == 0;
                if ((isBaselineAlignedLeft && isRowEven) || (!isBaselineAlignedLeft && !isRowEven))
                {
                    if (row > 0)
                    {
                        if (column > 0) _neighbours.AddNotNull(this._bubbleMatrix[row - 1, column - 1]);
                        _neighbours.AddNotNull(this._bubbleMatrix[row - 1, column]);
                    }
                    if (row < this._rows - 1)
                    {
                        if (column > 0) _neighbours.AddNotNull(this._bubbleMatrix[row + 1, column - 1]);
                        _neighbours.AddNotNull(this._bubbleMatrix[row + 1, column]);
                    }
                }
                else
                {
                    if (row > 0)
                    {
                        _neighbours.AddNotNull(this._bubbleMatrix[row - 1, column]);
                        if (column < this._columns - 1) _neighbours.AddNotNull(this._bubbleMatrix[row - 1, column + 1]);
                    }
                    if (row < this._rows - 1)
                    {
                        _neighbours.AddNotNull(this._bubbleMatrix[row + 1, column]);
                        if (column < this._columns - 1) _neighbours.AddNotNull(this._bubbleMatrix[row + 1, column + 1]);
                    }
                }
                return _neighbours;
            }
            return _neighbours;
        }

        public Vector2 location(Bubble bubble)
        {
            for (int i = 0; i < this._rows; i++)
            {
                for (int j = 0; j < this._columns; j++)
                {
                    Bubble someBubble = this._bubbleMatrix[i, j];
                    if (bubble == someBubble)
                    {
                        return new Vector2(i, j);
                    }
                }
            }
            return new Vector2(-1, -1);
        }

        public ArrayList neighbours(Bubble bubble)
        {
            return neighbours(location(bubble));
        }

        public ArrayList colorCluster(Bubble bubble)
        {
            if (bubble == null) return new ArrayList();
            return colorClusterRecursive(bubble, new ArrayList()).Distinct();
        }

        public ArrayList looseBubbles()
        {
            ArrayList anchored = this.anchoredBubbles();
            ArrayList connected = new ArrayList();

            foreach (Bubble anchoredBubble in anchored)
            {
                ArrayList c = this.connectedBubbles(anchoredBubble);
                connected.AddRange(c);
                connected = connected.Distinct();
            }
            ArrayList theBubbles = this.bubbles;
            theBubbles.Exclusive(connected);
            return theBubbles;
        }

        private ArrayList anchoredBubbles()
        {
            ArrayList anchored = new ArrayList();
            for (int j = 0; j < this._columns; j++)
            {
                if (this._bubbleMatrix[0, j] != null)
                {
                    anchored.Add(this._bubbleMatrix[0, j]);
                }
            }
            return anchored;
        }

        private ArrayList connectedBubbles(Bubble bubble)
        {
            return connectedBubblesRecursive(bubble, new ArrayList(), isBaselineAlignedLeft);
        }

        private ArrayList connectedBubblesRecursive(Bubble bubble, ArrayList visited, bool isBasedAlignedLeft)
        {
            ArrayList neighboursNotVisited = this.neighbours(bubble);
            neighboursNotVisited.Exclusive(visited);
            visited.Add(bubble);
            ArrayList returnArray = new ArrayList();
            returnArray.Add(bubble);
            foreach (Bubble someBubble in neighboursNotVisited)
            {
                if (bubble != someBubble)
                    returnArray.AddRange(connectedBubblesRecursive(someBubble, visited, isBasedAlignedLeft));
            }
            return returnArray;
        }

        private ArrayList colorClusterRecursive(Bubble bubble, ArrayList visited)
        {
            ArrayList similarColorNeighbours = JQUtils.FilterByColor(this.neighbours(bubble), bubble.color);
            similarColorNeighbours.Exclusive(visited);
            visited.Add(bubble);
            ArrayList returnArray = new ArrayList();
            returnArray.Add(bubble);
            foreach (Bubble aBubble in similarColorNeighbours)
            {
                if (bubble != aBubble)
                    returnArray.AddRange(colorClusterRecursive(aBubble, visited));
            }
            return returnArray;
        }

        public bool shiftOneRow()
        {
            bool overflows = false;
            for (int i = this._rows - 1; i >= 0; i--)
            {
                for (int j = 0; j < this._columns; j++)
                {
                    if (_bubbleMatrix[i, j] != null)
                    {
                        if (i >= this._rows - 1)
                        {
                            overflows = true;
                        }
                        else
                        {
                            _bubbleMatrix[i + 1, j] = _bubbleMatrix[i, j];
                            _bubbleMatrix[i, j] = null;
                        }
                    }
                }
            }
            isBaselineAlignedLeft = !isBaselineAlignedLeft;
            return overflows;
        }
    }
}
