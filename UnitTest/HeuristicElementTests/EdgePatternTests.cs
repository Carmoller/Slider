using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class EdgePatternTests
    {
        [TestMethod]
        public void EdgePattern_NonEdgeTileAtTopRowMustAddPenalty()
        {
            // set up a board, switching an edge row with a non-edge row on the top edge
            byte[] board = [01, 06, 03, 04,
                            05, 02, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 0];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, false);

            Assert.AreEqual(1, testObject.Calculate(board, gridSize));
        }
        [TestMethod]
        public void EdgePattern_NonEdgeTileAtBottomRowMustAddPenalty()
        {
            // set up a board, switching an edge row with a non-edge row on the bottom edge
            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            09, 14, 11, 12,
                            13, 10, 15, 0];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, false);

            Assert.AreEqual(1, testObject.Calculate(board, gridSize));
        }
        [TestMethod]
        public void EdgePattern_NonEdgeTileAtLeftColumnMustAddPenalty()
        {
            // set up a board, switching an edge row with a non-edge row on the left edge
            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            10, 09, 11, 12,
                            13, 14, 15, 0];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, false);

            Assert.AreEqual(1, testObject.Calculate(board, gridSize));
        }
        [TestMethod]
        public void EdgePattern_NonEdgeTileAtRightColumnMustAddPenalty()
        {
            // set up a board, switching an edge row with a non-edge row on the right edge
            byte[] board = [01, 02, 03, 04,
                            05, 06, 07, 08,
                            09, 10, 12, 11,
                            13, 15, 15, 0];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, false);

            Assert.AreEqual(1, testObject.Calculate(board, gridSize));
        }
        [TestMethod]
        public void EdgePattern_Penalties_Must_Be_Additive()
        {
            // set up a board, switching an edge row with a non-edge row on all edges
            byte[] board = [01, 06, 07, 04,
                            02, 05, 03, 08,
                            10, 14, 12, 11,
                            13, 09, 15, 0];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, false);

            Assert.AreEqual(4, testObject.Calculate(board, gridSize));
        }
        [TestMethod]
        public void EdgePattern_MustIgnoreCornersIfInstructed()
        {
            // Swap non-edge corners into corner tiles, use IgnoreCorners
            byte[] board = [06, 02, 03, 07,
                            05, 06, 04, 08,
                            09, 13, 00, 12,
                            10, 14, 15, 11];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, true);

            Assert.AreEqual(0, testObject.Calculate(board, gridSize));
        }
        [TestMethod]
        public void EdgePattern_MustIncludeCornersIfInstructed()
        {
            // Swap non-edge corners into corner tiles, don't use IgnoreCorners
            byte[] board = [06, 02, 03, 07,
                            05, 06, 04, 08,
                            09, 13, 00, 12,
                            10, 14, 15, 11];

            int gridSize = (int)Math.Sqrt(board.Length);
            EdgePattern testObject = new(gridSize, false);

            Assert.AreEqual(8, testObject.Calculate(board, gridSize));
        }

        [TestMethod]
        public void EdgePatternPerformanceTest()
        {
            // Set up a 12x12 board, iterate through it 1 million times, and measure how long it takes
            int loopCount = 1000000;
            int gridSize = 12;
            byte[] board = new byte[gridSize * gridSize];
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = (byte)(board.Length - i -1);
            }
            EdgePattern testObject = new(gridSize, false);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < loopCount; i++)
            {
                testObject.Calculate(board, gridSize);
            }
            sw.Stop();
            Console.WriteLine($"EdgePattern measurement: {Math.Round((double)loopCount / sw.ElapsedMilliseconds)} calculations / ms");
            Assert.AreEqual(loopCount, testObject.Statistics.NumberOfCalls);
        }
    }
}
