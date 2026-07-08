using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public static class ReverseManhattanCalculator
    {
        public static int Calculate(Span<byte> board, byte[] targetPositions, int gridSize)
        {
            int distance = 0;
            for (int i = 0; i < board.Length; i++)
            {
                (int row, int col) = i.ToRowAndColumn(gridSize);

                byte boardValue = board[i];
                if (boardValue == 0)
                {
                    continue;
                }

                (int targetRow, int targetCol) = ((int)(targetPositions[boardValue])).ToRowAndColumn(gridSize);
                distance += Math.Abs(row - targetRow) + Math.Abs(col - targetCol);
            }
            return distance;
        }
    }
}
