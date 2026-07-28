using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public class ColumnAnchoring : HeuristicElementBase, IHeuristicElement
    {
        public string Name { get { return "ColumnAnchoring"; } }
        public bool IsAdditive { get { return true; } }
        private bool _ignoreCorners;

        public ColumnAnchoring(Span<int> targetPositions, int gridSize, bool ignoreCorners) : base(targetPositions, gridSize)
        {
            _ignoreCorners = ignoreCorners;
        }

        public int Calculate(Span<byte> board, int gridSize)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            int penalty = 0;
            int start = _ignoreCorners ? 1 : 0;
            int end = _ignoreCorners ? gridSize - 1 : gridSize;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = start; col < end; col++)
                {
                    int position = row * gridSize + col;
                    byte currentValue = board[position];
                    if (currentValue == 0) continue;

                    if (currentValue == TargetPositions[currentValue])
                    {
                        // Tile is correctly placed; we do not like this if the one directly above is NOT correctly placed
                        if (row > 0)
                        {
                            int rowAbovePosition = position - gridSize;
                            byte aboveRowValue = board[rowAbovePosition];
                            if (TargetPositions[aboveRowValue] != rowAbovePosition)
                                penalty += 10;
                        }
                    }
                    else
                    {
                        // Tile is not correctly placed; we do not want the correct tile to be directly underneath this one
                        // (this may be a bit of a silly penalty if the current row is completely unsolved)
                        if (row < gridSize - 1)
                        {
                            int rowBelowPosition = position + gridSize;
                            byte belowRowValue = board[rowBelowPosition];
                            if (TargetPositions[belowRowValue] == position)
                                penalty += 10;
                        }
                    }
                }
            }
            Statistics.NumberOfCalls++;
            Statistics.TotalTimeSpentMs += Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            return penalty;
        }

        public void UpdateTargetPositionsFromBoard(Span<byte> board)
        {
            TargetPositionsUpdateFromBoard(board);
        }
    }
}
