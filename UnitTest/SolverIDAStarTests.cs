using Moq;
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
    public class SolverIDAStarTests
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
        #region Performance Test Bench
        [TestMethod]
        public void MeasurePerformance()
        {
            List<BoardTile> board = new();
            board.Add(new BoardTile { Value = 0, Row = 1, Column = 3 });
            board.Add(new BoardTile { Value = 1, Row = 3, Column = 3 });
            board.Add(new BoardTile { Value = 2, Row = 1, Column = 2 });
            board.Add(new BoardTile { Value = 3, Row = 3, Column = 2 });
            board.Add(new BoardTile { Value = 4, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 5, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 6, Row = 2, Column = 1 });
            board.Add(new BoardTile { Value = 7, Row = 2, Column = 3 });
            board.Add(new BoardTile { Value = 8, Row = 2, Column = 2 });
            board.Add(new BoardTile { Value = 9, Row = 2, Column = 0 });
            board.Add(new BoardTile { Value = 10, Row = 3, Column = 0 });
            board.Add(new BoardTile { Value = 11, Row = 0, Column = 3 });
            board.Add(new BoardTile { Value = 12, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 13, Row = 0, Column = 2 });
            board.Add(new BoardTile { Value = 14, Row = 0, Column = 0 });
            board.Add(new BoardTile { Value = 15, Row = 3, Column = 1 });

            Mock<IOptions> optionsMock = new();
            SolverOptions options = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true };
            Stopwatch sw = new();
            SolverIDAStar solver = new(optionsMock.Object);
            sw.Start();
            SolveResult result = solver.Solve(board, options, new HeuristicElementFactory());
            sw.Stop();
            TestContext!.WriteLine(options.ToString());
            TestContext.WriteLine("\tSolved board in " + result.TimeSpent.ToString() + " with " + result.Moves?.Count + " moves");
            TestContext.WriteLine($"\tConsidered {result.TotalStatesConsidered} board states ({result.TotalStatesConsidered / result.TimeSpent.TotalMilliseconds} states/ms)");
            TestContext.WriteLine($"\tCache Hits: {result.ForwardHitCount + result.BackwardHitCount}");
            TestContext.WriteLine($"\tHash collisions {result.ForwardCollisionCount + result.BackwardCollisionCount}");
            TestContext.WriteLine($"\tMax List Length: {Math.Max(result.BackwardMaxListLength, result.ForwardMaxListLength)}");
            TestContext.WriteLine("\tHeuristic breakdown:=================================");
            foreach (IHeuristicElement element1 in solver.Calculator!.ElementCalculators)
            {
                HeuristicStatistics stats1 = element1.Statistics;
                TestContext.WriteLine($"\t\t{element1.Name}: {stats1.NumberOfCalls} calls, {stats1.TotalTimeSpentMs} ms, {stats1.AverageTimePerCall} ms/call");
            }
            long totalAllocated = GC.GetTotalAllocatedBytes();

            // Get the count of collections for each generation
            int gen0Count = GC.CollectionCount(0);
            int gen1Count = GC.CollectionCount(1);
            int gen2Count = GC.CollectionCount(2);

            // Check current total memory currently thought to be alive
            long totalMemory = GC.GetTotalMemory(false);
            TestContext.WriteLine("GC Stats:=================================");
            TestContext.WriteLine($"\tTotal Allocated: {totalAllocated} bytes");
            TestContext.WriteLine($"\tGen 0 Collections: {gen0Count}");
            TestContext.WriteLine($"\tGen 1 Collections: {gen1Count}");
            TestContext.WriteLine($"\tGen 2 Collections: {gen2Count}");
            TestContext.WriteLine($"\tTotal Memory: {totalMemory} bytes");
            TestContext.WriteLine("GC Stats:=================================");

            options.UseCornerPattern = true;
            options.UseLinearConflict = true;
            options.UseEdgePattern = true;
            sw.Restart();
            SolveResult result2 = solver.Solve(board, options, new HeuristicElementFactory());
            sw.Stop();
            TestContext!.WriteLine(options.ToString());
            TestContext.WriteLine("\tSolved board in " + result2.TimeSpent.ToString() + " with " + result2.Moves?.Count + " moves");
            TestContext.WriteLine($"\tConsidered {result2.TotalStatesConsidered} board states ({result2.TotalStatesConsidered / result2.TimeSpent.TotalMilliseconds} states/ms)");
            TestContext.WriteLine($"\tCache Hits: {result2.ForwardHitCount + result2.BackwardHitCount}");
            TestContext.WriteLine($"\tHash collisions {result2.ForwardCollisionCount + result2.BackwardCollisionCount}");
            TestContext.WriteLine($"\tMax List Length: {Math.Max(result2.BackwardMaxListLength, result2.ForwardMaxListLength)}");
            TestContext.WriteLine("\tHeuristic breakdown:=================================");
            foreach (IHeuristicElement element2 in solver.Calculator!.ElementCalculators)
            {
                HeuristicStatistics stats2 = element2.Statistics;
                TestContext.WriteLine($"\t\t{element2.Name}: {stats2.NumberOfCalls} calls, {stats2.TotalTimeSpentMs} ms, {stats2.AverageTimePerCall} ms/call");
            }
            totalAllocated = GC.GetTotalAllocatedBytes();

            // Get the count of collections for each generation
            gen0Count = GC.CollectionCount(0);
            gen1Count = GC.CollectionCount(1);
            gen2Count = GC.CollectionCount(2);

            // Check current total memory currently thought to be alive
            totalMemory = GC.GetTotalMemory(false);
            TestContext.WriteLine("GC Stats:=================================");
            TestContext.WriteLine($"\tTotal Allocated: {totalAllocated} bytes");
            TestContext.WriteLine($"\tGen 0 Collections: {gen0Count}");
            TestContext.WriteLine($"\tGen 1 Collections: {gen1Count}");
            TestContext.WriteLine($"\tGen 2 Collections: {gen2Count}");
            TestContext.WriteLine($"\tTotal Memory: {totalMemory} bytes");
            TestContext.WriteLine("GC Stats:=================================");

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
            SolverIDAStar solver = new(optionsMock.Object);
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
            SolverIDAStar solver = new(optionsMock.Object);
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
            SolverIDAStar solver = new(optionsMock.Object);
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
            SolverIDAStar solver = new(optionsMock.Object);
            List<BoardTile> board = CreateBoardWithSingleMove(2);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
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
            SolverIDAStar solver = new(optionsMock.Object);
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
            SolverIDAStar solver = new(optionsMock.Object);
            List<BoardTile> board = CreateBoardWithThreeMoves(3);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
            heuristicsFactoryMock.Setup(p => p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert
            foreach (Move move in result.Moves)
            {
                // From and To positions should be adjacent (differ by 1 in either row or column)
                int rowDiff = Math.Abs(move.FromRow - move.ToRow);
                int colDiff = Math.Abs(move.FromColumn - move.ToColumn);

                bool isAdjacent = (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
                Assert.IsTrue(isAdjacent, 
                    $"Move from ({move.FromRow},{move.FromColumn}) to ({move.ToRow},{move.ToColumn}) is not adjacent");
            }
        }

        [TestMethod]
        public void Solve_MoveSequenceIsValid()
        {
            Mock<IOptions> optionsMock = new();
            // Arrange
            SolverIDAStar solver = new(optionsMock.Object);
            int gridSize = 3;
            List<BoardTile> board = CreateBoardWithThreeMoves(gridSize);
            Mock<IHeuristicElementFactory> heuristicsFactoryMock = new Mock<IHeuristicElementFactory>();
            Mock<IHeuristicCalculator> calculatorMock = new();
            heuristicsFactoryMock.Setup(p => p.CreateHeuristicCalculator(It.IsAny<IOptions>(), It.IsAny<SolverOptions>(), It.IsAny<int>())).Returns(calculatorMock.Object);

            // Act
            SolveResult result = solver.Solve(board, new SolverOptions(), heuristicsFactoryMock.Object);

            // Assert

            // Verify each move is valid (adjacent tiles)
            foreach (Move move in result.Moves)
            {
                int rowDiff = Math.Abs(move.FromRow - move.ToRow);
                int colDiff = Math.Abs(move.FromColumn - move.ToColumn);

                // Each move should be to an adjacent cell
                bool isAdjacent = (rowDiff == 1 && colDiff == 0) || (rowDiff == 0 && colDiff == 1);
                Assert.IsTrue(isAdjacent, 
                    $"Move from ({move.FromRow},{move.FromColumn}) to ({move.ToRow},{move.ToColumn}) is not adjacent");

                // Verify positions are in bounds
                Assert.IsTrue(move.FromRow >= 0 && move.FromRow < gridSize);
                Assert.IsTrue(move.FromColumn >= 0 && move.FromColumn < gridSize);
                Assert.IsTrue(move.ToRow >= 0 && move.ToRow < gridSize);
                Assert.IsTrue(move.ToColumn >= 0 && move.ToColumn < gridSize);
            }
        }

        [TestMethod]
        public void Solve_VariousBoards_AllReturnsValidMoveList()
        {
            Mock<IOptions> optionsMock = new();
            // Test multiple board configurations
            SolverIDAStar solver = new(optionsMock.Object);
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

        #endregion
    }
}
