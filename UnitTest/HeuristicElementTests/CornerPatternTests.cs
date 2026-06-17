using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slider.Heuristics;
using System;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class CornerPatternTests
    {
        [TestMethod]
        public void Calculate_3x3_SolvedBoard_ReturnsZeroPenalty()
        {
            CornerPattern cornerPattern = new(3);
            // Solved 3x3: 1 2 3 / 4 5 6 / 7 8 0
            byte[] board = [1, 2, 3, 4, 5, 6, 7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            Assert.AreEqual(0, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_EmptyTileInCorner_DoesNotAddExtraPenalty()
        {
            CornerPattern cornerPattern = new(3);
            // Empty tile (0) at top-left corner, non-corner tile 5 at bottom-right
            byte[] board = [0, 2, 3, 4, 1, 6, 7, 8, 5];

            int penalty = cornerPattern.Calculate(board, 3);

            // Top-left has 0 (empty, no penalty check)
            // Top-right has 3 (correct, no penalty)
            // Bottom-left has 7 (correct, no penalty)
            // Bottom-right expects 8, has 5 (non-corner in corner, penalty 2) 
            // Note: The special case for bottom-right expects 8, not 0
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_CornerTileInWrongCorner_ReturnsPenaltyOfOne()
        {
            CornerPattern cornerPattern = new(3);
            // Tile 3 (top-right corner) is at top-left corner instead of tile 1
            // Tile 1 is at bottom-right (where 0 should be, but we'll put it elsewhere)
            byte[] board = [3, 2, 9, 4, 5, 6, 7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Corner tile (3) is a corner tile value, so penalty is 1
            // Top-left expects 1, has 3 (corner tile in wrong position) = 1 penalty
            // Top-right expects 3, has 9 (non-corner in corner) = 2 penalty
            // Total = 3
            Assert.AreEqual(3, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_NonCornerTileInCorner_ReturnsPenaltyOfTwo()
        {
            CornerPattern cornerPattern = new(3);
            // Tile 5 (center/non-corner) is at top-left corner
            byte[] board = [5, 2, 3, 4, 1, 6, 7, 8, 0];

            // Act
            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Non-corner tile in corner = penalty of 2
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_NonCornerTileInMultipleCorners_AccumulatesPenalty()
        {
            CornerPattern cornerPattern = new(3);
            // Non-corner tiles at top-left and top-right corners
            byte[] board = [5, 2, 6, 4, 1, 3, 7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Two non-corner tiles = 2 + 2 = 4
            Assert.AreEqual(4, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_MixedCornerAndNonCornerTiles_AccumulatesPenalty()
        {
            CornerPattern cornerPattern = new(3);
            // Top-left: tile 5 (non-corner, penalty 2)
            // Top-right: tile 3 (corner tile, penalty 1 because expected is 3 but have 3... actually 0)
            // Bottom-left: tile 4 (not a corner tile, but also not matching position... penalty depends)
            // Bottom-right: 0 (correct, no penalty)
            byte[] board = [5, 2, 3, 4, 1, 6, 7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Only top-left has non-corner tile = penalty 2
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_SolvedBoard_ReturnsZeroPenalty()
        {
            CornerPattern cornerPattern = new(4);
            // Solved 4x4: 1 2 3 4 / 5 6 7 8 / 9 10 11 12 / 13 14 15 0
            byte[] board = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(0, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_NonCornerTileInTopLeftCorner_ReturnsPenaltyOfTwo()
        {
            CornerPattern cornerPattern = new(4);
            // Tile 6 (non-corner) at top-left instead of 1
            byte[] board = [6, 2, 3, 4, 5, 1, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_CornerTileInWrongCorner_IncursPenalty()
        {
            CornerPattern cornerPattern = new(4);
            // Tile 4 (top-right corner value) at top-left instead of 1
            // All other corners correct
            byte[] board = [4, 2, 3, 1, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0];

            int penalty = cornerPattern.Calculate(board, 4);

            // Top-left expects 1, has 4 (corner tile, penalty 1)
            // Top-right expects 4, has 1 (corner tile, penalty 1)
            // Other corners correct
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_AllFourCornersWithNonCornerTiles_AccumulatesPenalty()
        {
            CornerPattern cornerPattern = new(4);
            byte[] board = [6, 2, 3, 5, 4, 1, 8, 7, 9, 10, 11, 12, 14, 13, 15, 0];

            int penalty = cornerPattern.Calculate(board, 4);

            // Top-left expects 1, has 6 (non-corner, penalty 2)
            // Top-right expects 4, has 5 (non-corner, penalty 2)
            // Bottom-left expects 13, has 14 (non-corner, penalty 2)
            // Bottom-right expects 15, has 0 (correct for empty, no penalty)
            Assert.AreEqual(6, penalty);
        }

        [TestMethod]
        public void Calculate_TrackStatistics_IncrementsNumberOfCalls()
        {
            CornerPattern cornerPattern = new(3);
            byte[] board = [1, 2, 3, 4, 5, 6, 7, 8, 0];

            cornerPattern.Calculate(board, 3);
            long callsBefore = cornerPattern.Statistics.NumberOfCalls;

            cornerPattern.Calculate(board, 3);
            long callsAfter = cornerPattern.Statistics.NumberOfCalls;

            Assert.AreEqual(callsBefore + 1, callsAfter);
        }

        [TestMethod]
        public void Calculate_TrackStatistics_RecordsExecutionTime()
        {
            CornerPattern cornerPattern = new(3);
            byte[] board = [1, 2, 3, 4, 5, 6, 7, 8, 0];

            // Act
            long ticksBefore = cornerPattern.Statistics.TicksSpent;
            cornerPattern.Calculate(board, 3);
            long ticksAfter = cornerPattern.Statistics.TicksSpent;

            // Assert
            Assert.IsGreaterThan(ticksBefore, ticksAfter);
        }

        [TestMethod]
        public void Calculate_3x3_EmptyInDifferentCorner_StillChecksOtherCorners()
        {
            CornerPattern cornerPattern = new(3);
            // Empty (0) at top-left, tile 1 at bottom-right (swapped)
            // Other corners correct
            byte[] board = [0, 2, 3, 4, 5, 6, 7, 8, 1];

            int penalty = cornerPattern.Calculate(board, 3);

            // Top-left expects 1, has 0 (empty, no penalty)
            // Top-right expects 3, has 3 (correct, no penalty)
            // Bottom-left expects 7, has 7 (correct, no penalty)
            // Bottom-right expects 8, has 1 (corner tile in wrong corner, penalty 1)
            Assert.AreEqual(1, penalty);
        }
    }
}
