using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public class ManhattanDistanceCalculator : HeuristicElementBase, IHeuristicElement
    {
        public string Name { get { return "ManhattanDistance"; } }
        public bool IsAdditive { get { return true; } }

        public ManhattanDistanceCalculator(Span<int> targetPositions, int gridSize) : base(targetPositions, gridSize)
        {
        }
        public int Calculate(Span<byte> board, int gridSize)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            int distance = 0;
            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == 0)
                {
                    continue;
                }
                (int row, int col) = base.GetRowAndColumn(i);
                (int targetRow, int targetCol) = base.GetRowAndColumn(TargetPositions[board[i]]);
                distance += Math.Abs(row - targetRow) + Math.Abs(col - targetCol);
            }
            Statistics.NumberOfCalls++;
            Statistics.TotalTimeSpentMs += Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            return distance;
        }
    }
}
