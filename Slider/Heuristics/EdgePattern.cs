using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class EdgePattern : IHeuristicElement
    {
        private int Weight = 1;
        public HeuristicStatistics Statistics { get; private set; } = new();
        public string Name { get { return "EdgePattern"; } }
        private bool IsNonEdgeTile(byte tile, byte gridSize)
        {
            // Check if this tile's goal position is in the center (not on any edge)
            // Tile values are 1-based, calculate their goal position
            byte goalRow = (byte)((tile - 1) / gridSize);
            byte goalCol = (byte)((tile - 1) % gridSize);
            // Non-edge tiles have goal positions away from all boundaries
            return goalRow > 0 && goalRow < gridSize - 1 && goalCol > 0 && goalCol < gridSize - 1;
        }

        public int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int penalty = 0;

            // Penalty for non-edge tiles positioned on any edge (constrained movement)
            // Non-edge tiles have 4-directional freedom in the center, but only 3 on edges

            // Check top edge (row 0, columns 1 to gridSize-2)
            for (byte col = 1; col < gridSize - 1; col++)
            {
                byte tile = board[0, col];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            // Check bottom edge (row gridSize-1, columns 1 to gridSize-2)
            for (byte col = 1; col < gridSize - 1; col++)
            {
                byte tile = board[gridSize - 1, col];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            // Check left edge (column 0, rows 1 to gridSize-2)
            for (byte row = 1; row < gridSize - 1; row++)
            {
                byte tile = board[row, 0];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            // Check right edge (column gridSize-1, rows 1 to gridSize-2)
            for (byte row = 1; row < gridSize - 1; row++)
            {
                byte tile = board[row, gridSize - 1];
                if (tile != 0 && IsNonEdgeTile(tile, gridSize))
                    penalty += Weight;
            }

            sw.Stop();
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += sw.ElapsedTicks;
            return penalty;
        }
    }
}
