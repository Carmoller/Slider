using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class LinearConflict : IHeuristicElement
    {
        public IHeuristicsStatistics Statistics { get; private set; } = new HeuristicsStatistics();
        public string Name { get { return "LinearConflict"; } }
        public bool IsAdditive { get { return true; } }
        private readonly Stopwatch _stopwatch = new();

        public int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            _stopwatch.Restart();
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
            _stopwatch.Stop();
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += _stopwatch.ElapsedTicks;
            return conflictCount * 4; // Each conflict adds four moves
        }
        public int Calculate(byte[] board, int gridSize)
        {
            _stopwatch.Restart();
            int conflictCount = 0;

            // Check for row conflicts
            for (int row = 0; row < gridSize; row++)
            {
                for (int col1 = 0; col1 < gridSize; col1++)
                {
                    int idx1 = row * gridSize + col1;
                    byte value1 = board[idx1];
                    if (value1 == 0) continue;

                    // Calculate goal position for value1
                    byte goalRow1 = (byte)((value1 - 1) / gridSize);
                    byte goalCol1 = (byte)((value1 - 1) % gridSize);

                    // Only consider tiles that belong in this row
                    if (goalRow1 != row) continue;

                    for (int col2 = col1 + 1; col2 < gridSize; col2++)
                    {
                        int idx2 = row * gridSize + col2;
                        byte value2 = board[idx2];
                        if (value2 == 0) continue;

                        // Calculate goal position for value2
                        byte goalRow2 = (byte)((value2 - 1) / gridSize);
                        byte goalCol2 = (byte)((value2 - 1) % gridSize);

                        // Only consider tiles that belong in this row
                        if (goalRow2 != row) continue;

                        // Both tiles are in their goal row. Check for conflict.
                        if ((goalCol1 < goalCol2 && col1 > col2) || (goalCol1 > goalCol2 && col1 < col2))
                        {
                            conflictCount++;
                        }
                    }
                }
            }

            // Check for column conflicts
            for (int col = 0; col < gridSize; col++)
            {
                for (int row1 = 0; row1 < gridSize; row1++)
                {
                    int idx1 = row1 * gridSize + col;
                    byte value1 = board[idx1];
                    if (value1 == 0) continue;

                    // Calculate goal position for value1
                    byte goalRow1 = (byte)((value1 - 1) / gridSize);
                    byte goalCol1 = (byte)((value1 - 1) % gridSize);

                    // Only consider tiles that belong in this column
                    if (goalCol1 != col) continue;

                    for (int row2 = row1 + 1; row2 < gridSize; row2++)
                    {
                        int idx2 = row2 * gridSize + col;
                        byte value2 = board[idx2];
                        if (value2 == 0) continue;

                        // Calculate goal position for value2
                        byte goalRow2 = (byte)((value2 - 1) / gridSize);
                        byte goalCol2 = (byte)((value2 - 1) % gridSize);

                        // Only consider tiles that belong in this column
                        if (goalCol2 != col) continue;

                        // Both tiles are in their goal column. Check for conflict.
                        if ((goalRow1 < goalRow2 && row1 > row2) || (goalRow1 > goalRow2 && row1 < row2))
                        {
                            conflictCount++;
                        }
                    }
                }
            }

            _stopwatch.Stop();
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += _stopwatch.ElapsedTicks;
            return conflictCount * 4; // Each conflict adds four moves
        }
    }
}
