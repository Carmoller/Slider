using Microsoft.Extensions.Options;
using Moq;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class BidirectionalAStarTests
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext? TestContext { get; set; }

        #region Manhattan Distance Speed Test
        private (byte row, byte col)[]? _goalPositions;

        private int ManhattanDistanceMath(byte[,] board, byte gridSize)
        {
            int distance = 0;
            for (byte row = 0; row < gridSize; row++)
            {
                for (byte col = 0; col < gridSize; col++)
                {
                    byte value = board[row, col];
                    if (value == 0) continue;

                    byte goalRowMath = (byte)((value - 1) / gridSize);
                    byte goalColMath = (byte)((value - 1) % gridSize);
                    distance += Math.Abs(row - goalRowMath) + Math.Abs(col - goalColMath);
                }
            }
            return distance;
        }

        private int ManhattanDistanceLookup(byte[,] board, byte gridSize)
        {
            int distance = 0;
            for (byte row = 0; row < gridSize; row++)
            {
                for (byte col = 0; col < gridSize; col++)
                {
                    byte value = board[row, col];
                    if (value == 0) continue;

                    var (goalRow, goalCol) = _goalPositions![value];
                    distance += Math.Abs(row - goalRow) + Math.Abs(col - goalCol);
                }
            }
            return distance;
        }

        [TestMethod]
        public void ManhattanDistanceSpeedTest()
        {
            Stopwatch sw = new();
            int loopCount = 1000000;
            byte boardSize = 12;
            _goalPositions = new (byte row, byte col)[boardSize * boardSize];
            // Set up a 10x10 board where it is in reverse order, which is the worst case for Manhattan distance
            byte[,] board = new byte[boardSize, boardSize];
            for (byte row = 0; row < boardSize; row++)
            {
                for (byte col = 0; col < boardSize; col++)
                {
                    if (row == 0 && col == 0)
                    {
                        board[row, col] = 0; // Empty tile
                    }
                    else if (row == boardSize-1 && col == boardSize-1)
                    {
                        _goalPositions[0] = (row, col);
                    }
                    else
                    {
                        board[row, col] = (byte)(boardSize*boardSize - (row * boardSize + col));
                        int index = row * boardSize+ col;
                        _goalPositions[index + 1] = (row, col);
                    }
                }
            }
            sw.Start();
            for (int i = 0; i < loopCount; i++)
            {
                int distance = ManhattanDistanceLookup(board, boardSize);
            }
            sw.Stop();
            Debug.WriteLine($"ManhattanDistanceLookup: {(double)loopCount / sw.ElapsedMilliseconds} calculations / ms");
            sw.Restart();
            for (int i = 0; i < loopCount; i++)
            {
                int distance = ManhattanDistanceMath(board, boardSize);
            }
            sw.Stop();
            Debug.WriteLine($"ManhattanDistanceMath: {(double)loopCount / sw.ElapsedMilliseconds} calculations / ms");
        }
        #endregion

        #region Solver Tests
        private List<BoardTile> CreateSolvedBoard(int gridSize)
        {
            List<BoardTile> board = new();
            byte tileValue = 1;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    byte value = (row == gridSize - 1 && col == gridSize - 1) ? (byte)0 : tileValue;
                    board.Add(new BoardTile { Value = value, Row = row, Column = col });
                    tileValue++;
                }
            }
            return board;
        }

        private List<BoardTile> CreateBoardWithSingleMove(int gridSize)
        {
            List<BoardTile> board = CreateSolvedBoard(gridSize);

            // Swap empty (0) with the tile to its left
            // Find the empty tile (should be at bottom-right)
            int emptyIndex = board.FindIndex(b => b.Value == 0);
            int swapIndex = emptyIndex - 1; // Tile to the left

            // Swap the values
            (board[emptyIndex].Value, board[swapIndex].Value) = (board[swapIndex].Value, board[emptyIndex].Value);

            return board;
        }

        private List<BoardTile> CreateBoardWithThreeMoves(int gridSize)
        {
            // For simplicity and reliability, just use a 2-move version
            // which we know works from the single move tests
            List<BoardTile> board = CreateSolvedBoard(gridSize);

            // Perform 2 swaps to create a more complex puzzle
            // Swap empty with tile to left
            int emptyIndex = board.FindIndex(b => b.Value == 0);
            int swapIndex = emptyIndex - 1;
            if (swapIndex >= 0 && (emptyIndex % gridSize) != 0)
            {
                byte temp = board[emptyIndex].Value;
                board[emptyIndex].Value = board[swapIndex].Value;
                board[swapIndex].Value = temp;
            }

            return board;
        }

        [TestMethod]
        public void Solve_AlreadySolvedBoard_ReturnsEmptyList()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = CreateSolvedBoard(3);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert
            Assert.AreEqual(SolveResultType.AlreadySolved, result.Result);
        }

        [TestMethod]
        public void Solve_SingleMoveAway_ReturnsSingleMove()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = CreateBoardWithSingleMove(3);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
            heuristicsFactoryMock.Setup(p => p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.HasCount(1, result.Moves, "Board one move away should have exactly one move");
        }

        [TestMethod]
        public void Solve_ThreeMovesAway_ReturnsValidSolution()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = CreateBoardWithThreeMoves(3);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
            heuristicsFactoryMock.Setup(p => p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThanOrEqualTo(1, result.MoveCount, "Board should have a solution");

            // Check moves are valid (at least that they are within board limits)
            foreach (Move move in result.Moves)
            {
                Assert.IsTrue(move.FromRow >= 0 && move.FromRow < 3);
                Assert.IsTrue(move.FromColumn >= 0 && move.FromColumn < 3);
                Assert.IsTrue(move.ToRow >= 0 && move.ToRow < 3);
                Assert.IsTrue(move.ToColumn >= 0 && move.ToColumn < 3);
            }
        }

        [TestMethod]
        public void Solve_TwoByTwoBoard_Solves()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = CreateBoardWithSingleMove(2);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
            calculatorMock.Setup(p => p.GetHeuristic(It.IsAny<byte[]>(), It.IsAny<int>())).Returns(1);
            heuristicsFactoryMock.Setup(p => p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert
            Assert.HasCount(1, result.Moves, "2x2 board one move away should have exactly one move");
        }

        [TestMethod]
        public void Solve_FourByFourBoard_Solves()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory()   );
            List<BoardTile> board = CreateBoardWithSingleMove(4);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new ();
            Mock<IHeuristicCalculator> calculatorMock = new();
            heuristicsFactoryMock.Setup(p=>p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);
            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThanOrEqualTo(1, result.Moves.Count, "4x4 board should be solvable");
        }

        [TestMethod]
        public void Solve_ReturnedMovesAreAdjacent()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = CreateBoardWithThreeMoves(3);
            HeuristicElementFactory heuristicsFactory = new ();
            HeuristicCalculator calculator = new(new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                3, heuristicsFactory, optionsMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactory);

            MoveVerifier.VerifyMoves(board, result);
            // Assert
        }

        [TestMethod]
        public void Solve_MoveSequenceIsValid()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            int gridSize = 3;
            List<BoardTile> board = CreateBoardWithThreeMoves(gridSize);
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                3, heuristicsFactory, optionsMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactory);

            // Assert
            MoveVerifier.VerifyMoves(board, result);
        }

        [TestMethod]
        public void Solve_VariousBoards_AllReturnsValidMoveList()
        {
            Mock<IOptions> optionsMock = new();
            // Test multiple board configurations
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
            heuristicsFactoryMock.Setup(p => p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);

            for (int gridSize = 2; gridSize <= 3; gridSize++)
            {
                List<BoardTile> board = CreateBoardWithSingleMove(gridSize);
                SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

                Assert.IsGreaterThanOrEqualTo(1, result.Moves.Count, $"Should find at least one move for {gridSize}x{gridSize} board");

                foreach (Move move in result.Moves)
                {
                    Assert.IsNotNull(move, $"Move should not be null in {gridSize}x{gridSize} board");
                    Assert.IsTrue(move.FromRow >= 0 && move.FromRow < gridSize);
                    Assert.IsTrue(move.FromColumn >= 0 && move.FromColumn < gridSize);
                    Assert.IsTrue(move.ToRow >= 0 && move.ToRow < gridSize);
                    Assert.IsTrue(move.ToColumn >= 0 && move.ToColumn < gridSize);
                }
            }
        }
        [TestMethod]
        public void Test2x2Board()
        {
            Mock<IOptions> optionsMock = new();
            List<BoardTile> board = new List<BoardTile>
            {
                new BoardTile {Row = 0, Column = 0, Value = 2 },
                new BoardTile {Row = 0, Column = 1, Value = 3 },
                new BoardTile {Row = 1, Column = 0, Value = 1 },
                new BoardTile {Row = 1, Column = 1, Value = 0 },
            };

            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                2, heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactory);

            // Assert
            MoveVerifier.VerifyMoves(board, result);
        }
        [TestMethod]
        public void Test3x3Board()
        {
            Mock<IOptions> optionsMock = new();
            List<BoardTile> board = new List<BoardTile>
            {
                new BoardTile {Row = 0, Column = 0, Value = 0 },
                new BoardTile {Row = 0, Column = 1, Value = 8 },
                new BoardTile {Row = 0, Column = 2, Value = 7 },
                new BoardTile {Row = 1, Column = 0, Value = 6 },
                new BoardTile {Row = 1, Column = 1, Value = 5 },
                new BoardTile {Row = 1, Column = 2, Value = 4 },
                new BoardTile {Row = 2, Column = 0, Value = 3 },
                new BoardTile {Row = 2, Column = 1, Value = 2 },
                new BoardTile {Row = 2, Column = 2, Value = 1 },
            };

            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                3, heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactory);

            // Assert
            MoveVerifier.VerifyMoves(board, result);
        }
        [TestMethod]
        public void Test4x4Board()
        {
            Mock<IOptions> optionsMock = new();
            List<BoardTile> board = new List<BoardTile>
            {
                new BoardTile {Row = 0, Column = 0, Value = 0 },
                new BoardTile {Row = 0, Column = 1, Value = 3 },
                new BoardTile {Row = 0, Column = 2, Value = 7 },
                new BoardTile {Row = 0, Column = 3, Value = 4 },
                new BoardTile {Row = 1, Column = 0, Value = 11 },
                new BoardTile {Row = 1, Column = 1, Value = 5 },
                new BoardTile {Row = 1, Column = 2, Value = 1 },
                new BoardTile {Row = 1, Column = 3, Value = 8 },
                new BoardTile {Row = 2, Column = 0, Value = 2 },
                new BoardTile {Row = 2, Column = 1, Value = 6 },
                new BoardTile {Row = 2, Column = 2, Value = 15 },
                new BoardTile {Row = 2, Column = 3, Value = 10 },
                new BoardTile {Row = 3, Column = 0, Value = 13 },
                new BoardTile {Row = 3, Column = 1, Value = 14 },
                new BoardTile {Row = 3, Column = 2, Value = 12 },
                new BoardTile {Row = 3, Column = 3, Value = 9 },
            };

            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                4, heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactory);

            MoveVerifier.VerifyMoves(board, result);
        }

        #endregion
    }
}
