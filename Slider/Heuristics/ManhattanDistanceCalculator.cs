using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public class ManhattanDistanceCalculator : IHeuristicElement
    {
        public string Name { get { return "ManhattanDistance"; } }
        public bool IsAdditive { get { return true; } }
        public IHeuristicsStatistics Statistics { get; }

        public ManhattanDistanceCalculator()
        {
            Statistics = new HeuristicsStatistics();
        }
        public int Calculate(Span<byte> board, int gridSize)
        {
            long startTimestamp = Stopwatch.GetTimestamp();
            int distance = 0;
            for (int i = 0; i < board.Length; i++)
            {
                int targetRow;
                int targetCol;
                int row = i / gridSize;
                int col = i % gridSize;

                if (board[i] == 0)
                {
                    continue;
                }

                targetRow = (board[i] - 1) / gridSize;
                targetCol = (board[i] - 1) % gridSize;
                distance += Math.Abs(row - targetRow) + Math.Abs(col - targetCol);
            }
            Statistics.NumberOfCalls++;
            Statistics.TotalTimeSpentMs += Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            return distance;
        }
    }
}
