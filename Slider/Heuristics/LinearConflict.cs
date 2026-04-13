using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class LinearConflict : IHeuristicElement
    {
        public HeuristicStatistics Statistics { get; private set; } = new();
        public string Name { get { return "LinearConflict"; } }
        public int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int conflictCount = 0;
            for (byte row = 0; row < gridSize; row++)
            {
                for (byte col1 = 0; col1 < gridSize; col1++)
                {
                    int value1 = board[row, col1];
                    if (value1 == 0) continue;
                    var (goalRow1, goalCol1) = goalPositions[value1];
                    if (goalRow1 != row) continue;
                    for (byte col2 = (byte)(col1 + 1); col2 < gridSize; col2++)
                    {
                        int value2 = board[row, col2];
                        if (value2 == 0) continue;
                        var (goalRow2, goalCol2) = goalPositions[value2];
                        if (goalRow2 != row) continue;
                        // Both tiles are in their goal row. Check for conflict.
                        if ((goalCol1 < goalCol2 && col1 > col2) || (goalCol1 > goalCol2 && col1 < col2))
                        {
                            conflictCount++;
                        }
                    }
                }
            }
            for (byte col = 0; col < gridSize; col++)
            {
                for (byte row1 = 0; row1 < gridSize; row1++)
                {
                    int value1 = board[row1, col];
                    if (value1 == 0) continue;
                    var (goalRow1, goalCol1) = goalPositions[value1];
                    if (goalCol1 != col) continue;
                    for (byte row2 = (byte)(row1 + 1); row2 < gridSize; row2++)
                    {
                        int value2 = board[row2, col];
                        if (value2 == 0) continue;
                        var (goalRow2, goalCol2) = goalPositions[value2];
                        if (goalCol2 != col) continue;
                        // Both tiles are in their goal column. Check for conflict.
                        if ((goalRow1 < goalRow2 && row1 > row2) || (goalRow1 > goalRow2 && row1 < row2))
                        {
                            conflictCount++;
                        }
                    }
                }
            }
            sw.Stop();
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += sw.ElapsedTicks;
            return conflictCount * 4; // Each conflict adds four moves
        }
    }
}
