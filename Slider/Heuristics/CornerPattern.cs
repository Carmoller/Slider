using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class CornerPattern : IHeuristicElement
    {
        public HeuristicStatistics Statistics { get; private set; }
        public string Name { get { return "CornerPattern"; } }

        private HashSet<byte> _cornerTileValues;
        private (byte row, byte col)[] _corners;
        public CornerPattern(int gridSize)
        {
            Statistics = new();
            // Identify corner tile values
            // For 3×3: corners should have 1, 3, 7, 8
            // For 4×4: corners should have 1, 4, 13, 15
            // Formula: value at position (r, c) = r * gridSize + c + 1 (or 0 if last)
            _cornerTileValues = new()
                 {
                     1,                                      // Top-left (0, 0)
                     (byte)gridSize,                         // Top-right (0, gridSize-1)
                     (byte)((gridSize - 1) * gridSize + 1), // Bottom-left (gridSize-1, 0)
                     (byte)(gridSize * gridSize - 1)         // Bottom-right (gridSize-1, gridSize-1)
                 };

            // Corner positions to check
            _corners = new[]
            {
                     ((byte)0, (byte)0),
                     ((byte)0, (byte)(gridSize - 1)),
                     ((byte)(gridSize - 1), (byte)0),
                     ((byte)(gridSize - 1), (byte)(gridSize - 1))
                 };
        }

        public int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int penalty = 0;

            // Iterate through each corner
            for (int i = 0; i < _corners.Length; i++)
            {
                var (cornerRow, cornerCol) = _corners[i];
                byte tileAtCorner = board[cornerRow, cornerCol];
                byte expectedTile = (byte)(cornerRow * gridSize + cornerCol + 1);

                // Special case: bottom-right should be empty or the last numbered tile
                if (cornerRow == gridSize - 1 && cornerCol == gridSize - 1)
                {
                    expectedTile = (byte)(gridSize * gridSize - 1);
                }

                // Case 1: Non-corner tile in corner = LOCK situation (highest penalty)
                if (tileAtCorner != 0 && tileAtCorner != expectedTile && !_cornerTileValues.Contains(tileAtCorner))
                {
                    // A center/edge tile is blocking a corner - major lock
                    penalty += 2;
                }
                // Case 2: Corner tile in wrong corner (lower penalty)
                else if (tileAtCorner != expectedTile && _cornerTileValues.Contains(tileAtCorner))
                {
                    // Wrong corner tile here - less restrictive but still wrong
                    penalty += 1;
                }
                // Case 3: Correct tile in correct corner (no penalty)
            }
            Statistics.NumberOfCalls++;
            Statistics.TicksSpent += sw.ElapsedTicks;
            return penalty;
        }
    }
}
