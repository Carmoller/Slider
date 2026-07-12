using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Animation;

namespace UnitTest.HeuristicElementTests
{
    [TestClass]
    public class ManhattanDistanceTest
    {
        private byte[] goalBoard_4x4 = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 0];
        private int[] goalPositions_4x4 = [15, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14];

        [TestMethod]
        public void ManhattanDistance_GoalBoard_MustReturn0()
        {
            ManhattanDistanceCalculator calculator = new(goalPositions_4x4, 4);

            int heuristic = calculator.Calculate(goalBoard_4x4, 4);
            Assert.AreEqual(0, heuristic);
        }

        [TestMethod]
        public void ManhattanDistance_GoalBoard_Switch2MustReturn2()
        {
            ManhattanDistanceCalculator calculator = new(goalPositions_4x4, 4);

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
            ManhattanDistanceCalculator calculator = new(goalPositions_4x4, 4);
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

        [TestMethod]
        public void ManhattanDistance_CustomTargetMustReturn_Zero()
        {
            byte[] targetBoard = [00, 03, 07, 12,
                                  02, 15, 06, 04,
                                  13, 09, 10, 08,
                                  01, 05, 14, 11];
            int[] targetPositions = BoardHelper.GetBoardPositionsFromBoardValues(targetBoard);

            byte[] currentBoard = (byte[])targetBoard.Clone();

            ManhattanDistanceCalculator testObject = new(targetPositions, 4);

            Assert.AreEqual(0, testObject.Calculate(currentBoard, 4));
        }
        [TestMethod]
        public void ManhattanDistance_CustomTargetMustUpdateWhenMoved()
        {
            int gridSize = 4;
            byte[] targetBoard = [00, 03, 07, 12,
                                  02, 15, 06, 04,
                                  13, 09, 10, 08,
                                  01, 05, 14, 11];
            int[] targetPositions = new int[targetBoard.Length];
            for (int i = 0; i < targetBoard.Length; i++)
            {
                targetPositions[targetBoard[i]] = i;
            }

            ManhattanDistanceCalculator testObject = new(targetPositions, gridSize);

            // Swap the blank tile with successive tiles in the same row and verify that the distance increases
            for (int i = 1; i < gridSize; i++)
            {
                byte[] currentBoard = (byte[])targetBoard.Clone();
                currentBoard[0] = currentBoard[i];
                currentBoard[i] = 0;
                Assert.AreEqual(i, testObject.Calculate(currentBoard, gridSize));
            }
            // Do the same for the column
            for (int i = 1; i < gridSize; i++)
            {
                byte[] currentBoard = (byte[])targetBoard.Clone();
                currentBoard[0] = currentBoard[i*gridSize];
                currentBoard[i] = 0;
                Assert.AreEqual(i, testObject.Calculate(currentBoard, gridSize));
            }
        }

        [TestMethod]
        public void ManhattanDistance_PerformanceTest()
        {
            Stopwatch sw = new();
            int loopCount = 1000000;
            byte gridSize = 12;
            byte[] board = new byte[gridSize * gridSize];
            // Set up a 12x12 board where it is in reverse order, which is the worst case for Manhattan distance
            for (byte row = 0; row < gridSize; row++)
            {
                for (byte col = 0; col < gridSize; col++)
                {
                    if (row == 0 && col == 0)
                    {
                        board[row * gridSize + col] = 0; // Empty tile
                    }
                    else
                    {
                        board[row *gridSize + col] = (byte)(gridSize*gridSize - (row*gridSize + col));
                    }
                }
            }
            ManhattanDistanceCalculator calculator = new(Span<int>.Empty, gridSize);

            sw.Start();
            for (int i = 0; i < loopCount; i++)
            {
                int distance = calculator.Calculate(board, gridSize);
            }
            sw.Stop();
            Console.WriteLine($"ManhattanDistance measurement: {Math.Round((double)loopCount / sw.ElapsedMilliseconds)} calculations / ms");
        }

    }
}

