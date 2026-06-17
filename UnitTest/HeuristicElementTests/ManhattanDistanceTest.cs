using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class ManhattanDistanceTest
    {
        private byte[] goalBoard_4x4 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0];

        [TestMethod]
        public void ManhattanDistance_GoalBoard_MustReturn0()
        {
            ManhattanDistanceCalculator calculator = new();

            int heuristic = calculator.Calculate(goalBoard_4x4, 4);
            Assert.AreEqual(0, heuristic);
        }

        [TestMethod]
        public void ManhattanDistance_GoalBoard_Switch2MustReturn2()
        {
            ManhattanDistanceCalculator calculator = new();

            byte[] board = (byte[])goalBoard_4x4.Clone();

            // Swap tiles 1 and 2
            board[0] = 2;
            board[1] = 1;
            int heuristic = calculator.Calculate(board, 4);
            Assert.AreEqual(2, heuristic);
        }
        [TestMethod]
        public void ManhattanDistance_AllPositions_MustCount()
        {
            // Make a board consisting of all 0s - we'll be moving tile 1 to each position.
            // (0 doesn't count in the manhattan distance)
            byte[] board = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
            ManhattanDistanceCalculator calculator = new();
            int gridSize = (int)Math.Sqrt(board.Length);
            // Now move tile 1 to all positions on the board
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    board[row * gridSize + col] = 1;
                    int h = calculator.Calculate(board, gridSize);
                    Assert.AreEqual(row + col, h);
                    board[row * gridSize + col] = 0;
                }
            }
        }
    }
}
