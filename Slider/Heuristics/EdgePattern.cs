using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class EdgePattern : IHeuristicElement
    {
        private readonly int Weight = 1;
        public IHeuristicsStatistics Statistics { get; private set; } = new HeuristicsStatistics();
        public string Name { get { return "EdgePattern"; } }
        public bool IsAdditive { get { return true; } }
        private readonly Stopwatch _stopwatch = new();


        private static bool IsNonEdgeTile(byte tile, byte gridSize)
        {
            // Check if this tile's goal position is in the center (not on any edge)
            // Tile values are 1-based, calculate their goal position
            byte goalRow = (byte)((tile - 1) / gridSize);
            byte goalCol = (byte)((tile - 1) % gridSize);
            // Non-edge tiles have goal positions away from all boundaries
            return goalRow > 0 && goalRow < gridSize - 1 && goalCol > 0 && goalCol < gridSize - 1;
        }

        public int Calculate(Span<byte> board, int gridSize)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int penalty = 0;

            // Penalty for non-edge tiles positioned on any edge (constrained movement)
            // Non-edge tiles have 4-directional freedom in the center, but only 3 on edges

            // Check top edge 
            for (byte col = 1; col < gridSize - 1; col++)
            {
                byte tile = board[col];
                if (tile != 0 && IsNonEdgeTile(tile, (byte)gridSize))
                    penalty += Weight;
            }

            // Check bottom edge (row gridSize-1, columns 1 to gridSize-2)
            for (byte col = (byte)(gridSize * gridSize - gridSize); col < (byte)(gridSize * gridSize); col++)
            {
                byte tile = board[col];
                if (tile != 0 && IsNonEdgeTile(tile, (byte)gridSize))
                    penalty += Weight;
            }

            // Check left edge (column 0, rows 1 to gridSize-2)
            for (byte row = 0; row < gridSize; row++)
            {
                byte tile = board[row * gridSize];
                if (tile != 0 && IsNonEdgeTile(tile, (byte)gridSize))
                    penalty += Weight;
            }

            // Check right edge (column gridSize-1, rows 1 to gridSize-2)
            for (byte row = 0; row < gridSize; row++)
            {
                byte tile = board[row * gridSize + (gridSize - 1)];
                if (tile != 0 && IsNonEdgeTile(tile, (byte)gridSize))
                    penalty += Weight;
            }

            sw.Stop();
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += sw.ElapsedTicks;
            return penalty;
        }

    }
}
