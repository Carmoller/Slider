using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class EdgePattern : HeuristicElementBase, IHeuristicElement
    {
        private readonly int Weight = 1;
        public string Name { get { return "EdgePattern"; } }
        public bool IsAdditive { get { return true; } }
        private bool _ignoreCorners;
        public EdgePattern(Span<int> targetPositions, int gridSize, bool ignoreCorners) : base(targetPositions, gridSize)
        {
            _ignoreCorners = ignoreCorners;
        }

        private bool IsNonEdgeTile(int tile, int gridSize)
        {
            // Check if this tile's goal position is in the center (not on any edge)
            (int goalRow, int goalCol) = base.GetRowAndColumn(base.GetTargetPosition(tile));
            // Non-edge tiles have goal positions away from all boundaries
            return goalRow > 0 && goalRow < gridSize - 1 && goalCol > 0 && goalCol < gridSize - 1;
        }

        (int start, int end) GetTopRowStartAndEnd(int gridSize)
        {
            if (_ignoreCorners)
            {
                return (1, gridSize - 1);
            }
            else
            {
                return (0,  gridSize);
            }
        }
        (int start, int end) GetBottomRowStartAndEnd(int gridSize)
        {
            if (_ignoreCorners)
            {
                return (gridSize * gridSize - gridSize + 1, gridSize * gridSize - 1);
            }
            else
            {
                return (gridSize * gridSize - gridSize, gridSize * gridSize);
            }
        }
        (int start, int end) GetLeftRowStartAndEnd(int gridSize)
        {
            if (_ignoreCorners)
            {
                return (gridSize, gridSize * gridSize-gridSize);
            }
            else
            {
                return (0, gridSize * gridSize);
            }
        }
        (int start, int end) GetRightRowStartAndEnd(int gridSize)
        {
            if (_ignoreCorners)
            {
                return (2*gridSize-1, gridSize * gridSize - 1);
            }
            else
            {
                return (gridSize-1, gridSize * gridSize);
            }
        }

        public int Calculate(Span<byte> board, int gridSize)
        {
            long startTime = Stopwatch.GetTimestamp();
            int penalty = 0;

            // Penalty for non-edge tiles positioned on any edge (constrained movement)
            // Non-edge tiles have 4-directional freedom in the center, but only 3 on edges

            // Check top edge 
            (int start, int end) = GetTopRowStartAndEnd(gridSize);
            for (int col = start; col < end; col++)
            {
                byte tile = board[col];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            // Check bottom edge (row gridSize-1, columns 1 to gridSize-2)
            (start, end) = GetBottomRowStartAndEnd(gridSize);
            for (int col = start; col < end; col++)
            {
                byte tile = board[col];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            // Check left edge (column 0, rows 1 to gridSize-2)
            (start, end) = GetLeftRowStartAndEnd(gridSize);
            for (int row = start; row < end; row+=gridSize)
            {
                byte tile = board[row];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            (start, end) = GetRightRowStartAndEnd(gridSize);
            // Check right edge (column gridSize-1, rows 1 to gridSize-2)
            for (int row = start; row < end; row+=gridSize)
            {
                byte tile = board[row];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            Statistics.NumberOfCalls++;
            Statistics.TotalTimeSpentMs += Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            return penalty;
        }

    }
}
