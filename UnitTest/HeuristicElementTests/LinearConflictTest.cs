using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class LinearConflictTest
    {
        private byte[] board = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0];

        [TestMethod]
        public void LinearConflict_SolvedBoard_Returns0()
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            LinearConflict testObject = new(gridSize);

            Assert.AreEqual(0, testObject.Calculate(board, gridSize));
        }

        [TestMethod]
        public void LinearConflict_RowConflict_Adds4()
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            LinearConflict testObject = new(gridSize);

            // Swap two tiles in each row - each should add 4 to the heuristic
            int expectedH = 0;
            for (int i = 0; i < gridSize; i++)
            {
                byte temp = board[i * gridSize];
                board[i * gridSize] = board[i * gridSize + 1];
                board[i*gridSize + 1] = temp;
                expectedH += 4;
                Assert.AreEqual(expectedH, testObject.Calculate(board, gridSize));
            }
        }

        [TestMethod]
        public void LinearConflict_ColumnConflict_Adds4()
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            LinearConflict testObject = new(gridSize);

            // Swap two tiles in each column - each should add 4 to the heuristic
            int expectedH = 0;
            for (int i = 0; i < gridSize; i++)
            {
                byte temp = board[i];
                board[i] = board[i + gridSize];
                board[i + gridSize] = temp;
                expectedH += 4;
                Assert.AreEqual(expectedH, testObject.Calculate(board, gridSize));
            }
        }
        [TestMethod]
        public void LinearConflict_RowAndColumnConflict_Adds4Each()
        {
            int gridSize = (int)Math.Sqrt(board.Length);
            LinearConflict testObject = new(gridSize);

            // Swap two tiles in one row and two in one column - each should add 4 to the heuristic
            byte temp = board[0];
            board[0] = board[gridSize];
            board[gridSize] = temp;

            temp = board[2 * gridSize];
            board[2 * gridSize] = board[2 * gridSize + 1];
            board[2 * gridSize + 1] = temp;

            Assert.AreEqual(8, testObject.Calculate(board, gridSize));
        }

        [TestMethod]
        public void LinearConflict_PerformanceTest()
        {
            // Set up a 12x12 board, iterate through it 1 million times, and measure how long it takes
            int loopCount = 1000000;
            int gridSize = 12;
            byte[] board = new byte[gridSize * gridSize];
            for (int i = 0; i < board.Length; i++)
            {
                board[i] = (byte)(board.Length - i - 1);
            }
            LinearConflict testObject = new(gridSize);

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < loopCount; i++)
            {
                testObject.Calculate(board, gridSize);
            }
            sw.Stop();
            Console.WriteLine($"LinearConflict measurement: {Math.Round((double)loopCount / sw.ElapsedMilliseconds)} calculations / ms");
            Assert.AreEqual(loopCount, testObject.Statistics.NumberOfCalls);
        }
    }
}
