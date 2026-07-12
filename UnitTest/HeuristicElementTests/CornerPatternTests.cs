using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slider.Heuristics;
using System;
using System.Diagnostics;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class CornerPatternTests
    {
        [TestMethod]
        public void Calculate_3x3_SolvedBoard_ReturnsZeroPenalty()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            // Solved 3x3: 1 2 3 / 4 5 6 / 7 8 0
            byte[] board = [1, 2, 3,
                            4, 5, 6,
                            7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            Assert.AreEqual(0, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_CornerTileInWrongCorner_ReturnsPenaltyOfOne()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            // Tile 3 (top-right corner) is at top-left corner instead of tile 1
            // Tile 8 (non-corner) is at top-right corner
            byte[] board = [3, 2, 6,
                            4, 5, 6,
                            7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Corner tile (3) is a corner tile value, so penalty is 1
            // Top-left expects 1, has 3 (corner tile in wrong position) = 1 penalty
            // Top-right expects 3, has 6 (non-corner in corner) = 2 penalty
            // Total = 3
            Assert.AreEqual(3, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_NonCornerTileInCorner_ReturnsPenaltyOfTwo()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            // Tile 5 (center/non-corner) is at top-left corner
            byte[] board = [5, 2, 3,
                            4, 1, 6,
                            7, 8, 0];

            // Act
            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Non-corner tile in corner = penalty of 2
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_NonCornerTileInMultipleCorners_AccumulatesPenalty()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            // Non-corner tiles at top-left and top-right corners
            byte[] board = [5, 2, 6,
                            4, 1, 3,
                            7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Two non-corner tiles = 2 + 2 = 4
            Assert.AreEqual(4, penalty);
        }

        [TestMethod]
        public void Calculate_3x3_MixedCornerAndNonCornerTiles_AccumulatesPenalty()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            // Top-left: tile 5 (non-corner, penalty 2)
            // Top-right: tile 3 (corner tile, penalty 1 because expected is 3 but have 3... actually 0)
            // Bottom-left: tile 4 (not a corner tile, but also not matching position... penalty depends)
            // Bottom-right: 0 (correct, no penalty)
            byte[] board = [5, 2, 3,
                            4, 1, 6,
                            7, 8, 0];

            int penalty = cornerPattern.Calculate(board, 3);

            // Assert - Only top-left has non-corner tile = penalty 2
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_SolvedBoard_ReturnsZeroPenalty()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            // Solved 4x4: 1 2 3 4 / 5 6 7 8 / 9 10 11 12 / 13 14 15 0
            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 0];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(0, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_NonCornerTileInTopLeftCorner_ReturnsPenaltyOfTwo()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            // Tile 6 (non-corner) at top-left instead of 1
            byte[] board = [06, 02, 03, 04,
                            05, 01, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 00];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_CornerTileInWrongCorner_IncursPenalty()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            // Tile 4 (top-right corner value) at top-left instead of 1
            // All other corners correct
            byte[] board = [04, 02, 03, 01,
                            05, 06, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 0];

            int penalty = cornerPattern.Calculate(board, 4);

            // Top-left expects 1, has 4 (corner tile, penalty 1)
            // Top-right expects 4, has 1 (corner tile, penalty 1)
            // Other corners correct
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_AllFourCornersWithNonCornerTiles_AccumulatesPenalty()
        {
            byte[] board = [06, 02, 03, 05,
                            04, 01, 08, 07,
                            09, 10, 11, 12,
                            14, 13, 15, 00];

            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            int penalty = cornerPattern.Calculate(board, 4);

            // Top-left expects 1, has 6 (non-corner, penalty 2)
            // Top-right expects 4, has 5 (non-corner, penalty 2)
            // Bottom-left expects 13, has 14 (non-corner, penalty 2)
            // Bottom-right expects 15, has 0 (correct for empty, no penalty)
            Assert.AreEqual(6, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_CustomTarget_SolvedBoard_ReturnsZeroPenalty()
        {
            byte[] targetBoard = [00, 03, 07, 12,
                                   02, 15, 06, 04,
                                   13, 09, 10, 08,
                                   01, 05, 14, 11];

            int[] targetPositions = BoardHelper.GetBoardPositionsFromBoardValues(targetBoard);

            CornerPattern cornerPattern = new(targetPositions, 4);
            int penalty = cornerPattern.Calculate(targetBoard, 4);

            Assert.AreEqual(0, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_CustomTarget_NonCornerTileInTopLeftCorner_ReturnsPenaltyOfTwo()
        {
            // Tile 15 (non-corner) at top-left instead of blank
            byte[] targetBoard = [00, 03, 07, 12,
                                  02, 15, 06, 04,
                                  13, 09, 10, 08,
                                  01, 05, 14, 11];
            byte[] board = [15, 03, 07, 12,
                            02, 00, 06, 04,
                            13, 09, 10, 08,
                            01, 05, 14, 11];
            int[] targetPositions = BoardHelper.GetBoardPositionsFromBoardValues(targetBoard);

            CornerPattern cornerPattern = new(targetPositions, 4);
            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_CustomTarget_CornerTileInWrongCorner_IncursPenalty()
        {
            // Tile 12 (top-right corner value) at top-left instead of blank
            // All other corners correct

            byte[] targetBoard = [00, 03, 07, 12,
                                  02, 15, 06, 04,
                                  13, 09, 10, 08,
                                  01, 05, 14, 11];
            byte[] board = [12, 03, 07, 00,
                            02, 15, 06, 04,
                            13, 09, 10, 08,
                            01, 05, 14, 11];
            int[] targetPositions = BoardHelper.GetBoardPositionsFromBoardValues(targetBoard);

            CornerPattern cornerPattern = new(targetPositions, 4);
            int penalty = cornerPattern.Calculate(board, 4);

            // Top-left expects blank, has 12 (corner tile, penalty 1)
            // Top-right expects 12, has blank (corner tile, penalty 1)
            // Other corners correct
            Assert.AreEqual(2, penalty);
        }

        [TestMethod]
        public void Calculate_4x4_CustomTarget_AllFourCornersWithNonCornerTiles_AccumulatesPenalty()
        {
            byte[] targetBoard = [00, 03, 07, 12,
                                  02, 15, 06, 04,
                                  13, 09, 10, 08,
                                  01, 05, 14, 11];
            byte[] board = [15, 03, 07, 06,
                            02, 00, 12, 04,
                            13, 01, 11, 08,
                            09, 05, 14, 10];
            int[] targetPositions = BoardHelper.GetBoardPositionsFromBoardValues(targetBoard);

            CornerPattern cornerPattern = new(targetPositions, 4);
            int penalty = cornerPattern.Calculate(board, 4);

            // Top-left expects blank, has 15 (non-corner, penalty 2)
            // Top-right expects 12, has 6 (non-corner, penalty 2)
            // Bottom-left expects 1, has 9 (non-corner, penalty 2)
            // Bottom-right expects 11, has 10 (non-corner, penalty 2)
            Assert.AreEqual(8, penalty);
        }

        [TestMethod]
        public void Calculate_TrackStatistics_IncrementsNumberOfCalls()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            byte[] board = [1, 2, 3,
                            4, 5, 6,
                            7, 8, 0];

            cornerPattern.Calculate(board, 3);
            long callsBefore = cornerPattern.Statistics.NumberOfCalls;

            cornerPattern.Calculate(board, 3);
            long callsAfter = cornerPattern.Statistics.NumberOfCalls;

            Assert.AreEqual(callsBefore + 1, callsAfter);
        }

        [TestMethod]
        public void Calculate_TrackStatistics_RecordsExecutionTime()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            byte[] board = [1, 2, 3,
                            4, 5, 6,
                            7, 8, 0];

            // Act
            double ticksBefore = cornerPattern.Statistics.TotalTimeSpentMs;
            cornerPattern.Calculate(board, 3);
            double ticksAfter = cornerPattern.Statistics.TotalTimeSpentMs;

            // Assert
            Assert.IsGreaterThan(ticksBefore, ticksAfter);
        }

        [TestMethod]
        public void Calculate_3x3_EmptyInDifferentCorner_StillChecksOtherCorners()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 3);
            // Empty (0) at top-left, tile 1 at bottom-right (swapped)
            // Other corners correct
            byte[] board = [0, 2, 3,
                            4, 5, 6,
                            7, 8, 1];

            int penalty = cornerPattern.Calculate(board, 3);

            // Top-left expects 1, has 0 (corner tile on wrong corner, penalty 1)
            // Top-right expects 3, has 3 (correct, no penalty)
            // Bottom-left expects 7, has 7 (correct, no penalty)
            // Bottom-right expects 8, has 1 (corner tile in wrong corner, penalty 1)
            Assert.AreEqual(2, penalty);
        }
        [TestMethod]
        public void CornerPattern_PerformanceTest()
        {
            // Set up a 12x12 board, iterate through it 1 million times, and measure how long it takes
            int loopCount = 1000000;
            int gridSize = 12;
            byte[] board = new byte[gridSize * gridSize];
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = (byte)(board.Length - i - 1);
            }
            CornerPattern testObject = new(Span<int>.Empty, gridSize);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < loopCount; i++)
            {
                testObject.Calculate(board, gridSize);
            }
            sw.Stop();
            Console.WriteLine($"CornerPatter measurement: {Math.Round((double)loopCount / sw.ElapsedMilliseconds)} calculations / ms");
            Assert.AreEqual(loopCount, testObject.Statistics.NumberOfCalls);
        }

        [TestMethod]
        public void Calculate_4x4_HardLock_Upper_Right_ReturnsPenaltyOfSeven()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            byte[] board = [01, 02, 03, 08,
                            05, 06, 07, 04,
                            09, 10, 11, 12,
                            13, 14, 15, 00];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(7, penalty);
        }
        [TestMethod]
        public void Calculate_4x4_HardLock_Upper_Left_ReturnsPenaltyOfSeven()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            byte[] board = [05, 02, 03, 04,
                            01, 06, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 00];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(7, penalty);
        }
        [TestMethod]
        public void Calculate_4x4_HardLock_Bottom_Left_ReturnsPenaltyOfSeven()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            13, 10, 11, 12,
                            09, 14, 15, 00];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(7, penalty);
        }
        [TestMethod]
        public void Calculate_4x4_HardLock_Bottom_Right_DoesNotCountIfBlank()
        {
            CornerPattern cornerPattern = new(Span<int>.Empty, 4);
            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            09, 10, 11, 00,
                            13, 14, 15, 12];

            int penalty = cornerPattern.Calculate(board, 4);

            Assert.AreEqual(2, penalty);
        }
    }
}
