using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using static Slider.Heuristics.HeuristicInversionDistance;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class InversionDistanceTest
    {
        private byte[] ConstructReverseBoard(int gridSize)
        {
            byte[] board = new byte[gridSize * gridSize];
            for (int i = 1; i < board.Length; i++)
            {
                board[i] = (byte)(board.Length - i);
            }
            return board;
        }
        private byte[] ConstructSolvedBoard(int gridSize)
        {
            byte[] board = new byte[gridSize * gridSize];
            for (int i = 1; i < board.Length; i++)
            {
                board[i-1] = (byte)(i);
            }
            return board;
        }
        [TestMethod]
        [DataRow(3, 28)]
        [DataRow(4, 105)]
        [DataRow(5, 276)]
        [DataRow(6, 595)]
        [DataRow(7, 1128)]
        [DataRow(8, 1953)]
        [DataRow(9, 3160)]
        [DataRow(10, 4851)]
        public void InversionDistance_MaxValues(int gridSize, int maxValue)
        {
            byte[] board = ConstructReverseBoard(gridSize);

            HeuristicInversionDistance testObject = new(Span<int>.Empty, gridSize);

            int h = testObject.Calculate(board, gridSize);
            Assert.AreEqual(maxValue, h);
        }

        [TestMethod]
        [DataRow(3)]
        [DataRow(4)]
        [DataRow(5)]
        [DataRow(6)]
        [DataRow(7)]
        [DataRow(8)]
        [DataRow(9)]
        [DataRow(10)]
        public void InversionDistance_SolvedValues(int gridSize)
        {
            byte[] board = ConstructSolvedBoard(gridSize);

            HeuristicInversionDistance testObject = new(Span<int>.Empty, gridSize);

            int h = testObject.Calculate(board, gridSize);
            Assert.AreEqual(0, h);
        }

        [TestMethod]
        public void InversionDistance_ActualBoard()
        {
            // Tests the value of an actual deadlocked board
            byte[] board = ConstructSolvedBoard(10);
            board[board.Length - 1] = 9;
            board[8] = 0;
            HeuristicInversionDistance testObject = new(Span<int>.Empty, 10);

            int h = testObject.Calculate(board, 10);
        }
        [TestMethod]
        public void InversionDistance_PerformanceTest()
        {
            Stopwatch sw = new();
            int loopCount = 1000000;
            int gridSize = 12;
            byte[] board = ConstructReverseBoard(gridSize);

            HeuristicInversionDistance testObject = new(Span<int>.Empty, gridSize);

            sw.Start();
            for (int i = 0; i < loopCount; i++)
            {
                int distance = testObject.Calculate(board, gridSize);
            }
            sw.Stop();
            Console.WriteLine($"ManhattanDistance measurement: {Math.Round((double)loopCount / sw.ElapsedMilliseconds)} calculations / ms");
        }

    }
}
