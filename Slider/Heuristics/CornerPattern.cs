using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class CornerPattern : HeuristicElementBase, IHeuristicElement
    {
        public string Name { get { return "CornerPattern"; } }
        public bool IsAdditive { get { return true; } }
        private readonly HashSet<int> _cornerTileValues;
        private readonly byte[] _cornerIndexes;
        public CornerPattern(Span<int> targetPositions, int gridSize) : base(targetPositions, gridSize)
        {
            // Identify corner tile values
            // For 3×3: corners should have 1, 3, 7, 8
            // For 4×4: corners should have 1, 4, 13, 15
            // Formula: value at position (r, c) = r * gridSize + c + 1 (or 0 if last)
            //_cornerTileValues =
            //     [
            //         1,                                      // Top-left (0, 0)
            //         gridSize,                         // Top-right (0, gridSize-1)
            //         (gridSize - 1) * gridSize + 1, // Bottom-left (gridSize-1, 0)
            //         gridSize * gridSize - 1         // Bottom-right (gridSize-1, gridSize-1)
            //     ];
            _cornerIndexes =
               [
                     (byte)0,
                     (byte)(gridSize - 1),
                     (byte)(gridSize*gridSize - gridSize),
                     (byte)(gridSize*gridSize - 1)
                 ];

            _cornerTileValues =
                 [
                     TargetValues[_cornerIndexes[0]],                                      // Top-left (0, 0)
                     TargetValues[_cornerIndexes[1]],                         // Top-right (0, gridSize-1)
                     TargetValues[_cornerIndexes[2]],// (gridSize - 1) * gridSize + 1, // Bottom-left (gridSize-1, 0)
//                     GoalValues[_cornerIndexes[3]] == 0 ? GoalValues[_cornerIndexes[3]-1] : GoalValues[_cornerIndexes[3]]
                     TargetValues[_cornerIndexes[3]]
                     ]; // gridSize * gridSize - 1         // Bottom-right (gridSize-1, gridSize-1)

        }

        private bool IsLock(Span<byte> board, int position, int gridSize)
        {
            // If we get here, then we know the 'position' is in a corner, and it has a wrong tile
            // Check along the two edges, and see if they are correctly placed. If they are, it's a major penalty
            (int row, int column) = GetRowAndColumn(position);
            
            // Find adjacent row
            int adjacentLocation1, adjacentLocation2;
            if (row == 0)
            {
                adjacentLocation1 = position + gridSize;
            }
            else
            {
                adjacentLocation1 = position - gridSize;
            }
            // Find adjacent column
            if (column == 0)
            {
                adjacentLocation2 = position + 1;
            }
            else
            {
                adjacentLocation2 = position - 1;
            }
            // one of these adjacent positions must be in place, and the other must be the corner tile
            if ((board[adjacentLocation1] == 0) || (board[adjacentLocation2] == 0))
            {
                return false;
            }
            bool locked = ((board[adjacentLocation1] == TargetValues[adjacentLocation1] && board[adjacentLocation2] == TargetValues[position]) ||
                (board[adjacentLocation2] == TargetValues[adjacentLocation2] && board[adjacentLocation1] == TargetValues[position]));
            return locked;
        }

        public int Calculate(Span<byte> board, int gridSize)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            int penalty = 0;

            // Iterate through each corner
            for (int i = 0; i < _cornerIndexes.Length; i++)
            {
                byte position = _cornerIndexes[i];
                byte tileValue = board[position];
                byte expectedTile = TargetValues[position];

                // Case 1: Non-corner tile in corner = LOCK situation (highest penalty)
                if (tileValue != expectedTile)
                {
                    if (IsLock(board, position, gridSize))
                    {
                        penalty += 7;
                        continue;
                    }
                    if (!_cornerTileValues.Contains(tileValue))
                    {
                        // A center/edge tile is blocking a corner - major lock
                        penalty += 2;
                    }
                    // Case 2: Corner tile in wrong corner (lower penalty)
                    else
                    {
                        // Wrong corner tile here - less restrictive but still wrong
                        penalty += 1;
                    }
                }
                // Case 3: Correct tile in correct corner (no penalty)
            }
            Statistics.NumberOfCalls++;
            Statistics.TotalTimeSpentMs += Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            return penalty;
        }

    }
}
