using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public static class ReverseManhattanCalculator
    {
        public static int Calculate(byte[] board, byte[] targetPositions, int gridSize)
        {
            int distance = 0;
            for (int i = 0; i < board.Length; i++)
            {
                int targetRow;
                int targetCol;
                int row = i / gridSize;
                int col = i % gridSize;

                byte boardValue = board[i];
                if (boardValue == 0)
                {
                    continue;
                }

                targetRow = (targetPositions[boardValue]) / gridSize;
                targetCol = (targetPositions[boardValue]) % gridSize;
                distance += Math.Abs(row - targetRow) + Math.Abs(col - targetCol);
            }
            return distance;
        }
    }
}
