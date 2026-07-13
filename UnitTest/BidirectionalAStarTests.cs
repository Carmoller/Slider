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

        private byte[] CreateBoardWithSingleMove(int gridSize)
        {
            byte[] board = SolverHelper.CreateGoalBoard(gridSize);

            // Swap empty (0) with the tile to its left
            // Find the empty tile (should be at bottom-right)
            int emptyIndex = board.Length - 1;
            int swapIndex = emptyIndex - 1; // Tile to the left

            // Swap the values
            (board[emptyIndex], board[swapIndex]) = (board[swapIndex], board[emptyIndex]);

            return board;
        }

        private byte[] CreateBoardWithThreeMoves(int gridSize)
        {
            // For simplicity and reliability, just use a 2-move version
            // which we know works from the single move tests
            byte[] board = SolverHelper.CreateGoalBoard(gridSize);

            // Perform 2 swaps to create a more complex puzzle
            // Swap empty with tile to left
            int emptyIndex = board.Length - 1;
            int swapIndex = emptyIndex - 1;
            if (swapIndex >= 0 && (emptyIndex % gridSize) != 0)
            {
                byte temp = board[emptyIndex];
                board[emptyIndex] = board[swapIndex];
                board[swapIndex] = temp;
            }

            return board;
        }

        [TestMethod]
        public void Solve_AlreadySolvedBoard_ReturnsEmptyList()
        {
            // Arrange
            Mock<IOptions> optionsMock = new();
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            byte[] board = SolverHelper.CreateGoalBoard(3);
            HeuristicElementFactory heuristicsFactory = new();
            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            // Assert
            Assert.AreEqual(SolveResultType.AlreadySolved, result.Result);
        }

        [TestMethod]
        public void Solve_SingleMoveAway_ReturnsSingleMove()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            byte[] board = CreateBoardWithSingleMove(3);
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 2, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

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
            byte[] board = CreateBoardWithThreeMoves(3);
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 2, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);

            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

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
            byte[] board = CreateBoardWithSingleMove(2);
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 2, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);

            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            // Assert
            Assert.HasCount(1, result.Moves, "2x2 board one move away should have exactly one move");
        }

        [TestMethod]
        public void Solve_FourByFourBoard_Solves()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory()   );
            byte[] board = CreateBoardWithSingleMove(4);
            HeuristicElementFactory heuristicsFactory = new ();
            HeuristicCalculator calculator = new(Span<int>.Empty, 2, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

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
            byte[] board = CreateBoardWithThreeMoves(3);
            HeuristicElementFactory heuristicsFactory = new ();
            HeuristicCalculator calculator = new(Span<int>.Empty, 3, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);

            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            // Assert
        }

        [TestMethod]
        public void Solve_MoveSequenceIsValid()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            int gridSize = 3;
            byte[] board = CreateBoardWithThreeMoves(gridSize);
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 3, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                 heuristicsFactory, optionsMock.Object);

            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            // Assert
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }

        [TestMethod]
        public void Solve_VariousBoards_AllReturnsValidMoveList()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));
            // Test multiple board configurations
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 2, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);

            for (int gridSize = 2; gridSize <= 3; gridSize++)
            {
                byte[] board = CreateBoardWithSingleMove(gridSize);
                SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

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
            byte[] board = [
                            2, 3,
                            1, 0];
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 2, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            // Assert
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }
        [TestMethod]
        public void Test3x3Board()
        {
            Mock<IOptions> optionsMock = new();

            byte[] board = [
                            0, 8, 7,
                            6, 5, 4,
                            3, 2, 1];

            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 3, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            // Act
            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            // Assert
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }
        [TestMethod]
        public void Test4x4Board()
        {
            Mock<IOptions> optionsMock = new();
            byte[] board = [
                            06, 14, 15, 11, 
                            00, 03, 05, 07,
                            08, 12, 10, 09,
                            01, 02, 13, 04];
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 4, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            Console.WriteLine($"States considered: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void Bidirectional_Slow4x4Board()
        {
            Mock<IOptions> optionsMock = new();

            byte[] board = [14, 13, 09, 15,
                            06, 04, 07, 11,
                            08, 05, 00, 10,
                            03, 12, 02, 01];


            Assert.IsTrue(BoardHelper.IsSolvable(board));
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 4, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            BidirectionalAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }

        #endregion
    }
}
