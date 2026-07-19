using Moq;
using Slider;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Printing;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class DyamicWeightedAStarTests
    {
        [TestMethod]
        public void BfsSolver_3x3_Greedy()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
                            6, 3, 8,
                            0, 7, 1,
                            2, 4, 5];
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver solver = new(optionsMock.Object);
            solver.BfsMode = BfsMode.Greedy;

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);

            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_4x4_Greedy()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               10, 13, 00, 07,
               12, 14, 01, 03,
               06, 05, 15, 08,
               09, 04, 11, 02];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver solver = new(optionsMock.Object);
            solver.BfsMode = BfsMode.Greedy;

            SolveResult result = solver.Solve(board, [],
                    new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                    new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);

            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_MustSolveCustomTarget_Greedy()
        {
            // Not really a test, just a way of getting a count of the minimum number of moves to solve a given situation
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               08, 01, 03, 07,
               02, 05, 00, 14,
               15, 13, 11, 12,
               10, 06, 09, 04];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            byte[] targetBoard = [
                02, 08, 00, 03,
                01, 05, 11, 07,
                15, 06, 12, 14,
                10, 09, 13, 04];

            DynamicWeightAStarSolver solver = new(optionsMock.Object);

            SolveResult result = new(); ;
            for (int i = 0; i < 5; i++)
            {
                result = solver.Solve(board, targetBoard,
                    new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                    new HeuristicElementFactory());
            }

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine($"\tMoves: {result.MoveCount}");
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, targetBoard);
        }
        [TestMethod]
        public void BfsSolver_4x4Board_Greedy()
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
            DynamicWeightAStarSolver solver = new(optionsMock.Object);

            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            Console.WriteLine($"States considered: {result.TotalStatesConsidered}");
        }

        [TestMethod]
        public void BfsSolver_4x4Board_Slow_Greedy()
        {
            // This board is immensely slow for the Bidirational A* sovler, so we test it here as well
            Mock<IOptions> optionsMock = new();

            byte[] board = [14, 13, 09, 15,
                            06, 04, 07, 11,
                            08, 05, 00, 10,
                            03, 12, 02, 01];


            Assert.IsTrue(BoardHelper.IsSolvable(board));
            HeuristicElementFactory heuristicsFactory = new();
            HeuristicCalculator calculator = new(Span<int>.Empty, 4, new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicsFactory, optionsMock.Object);
            DynamicWeightAStarSolver solver = new(optionsMock.Object);

            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            Console.WriteLine($"States considered: {result.TotalStatesConsidered}");
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }

        [TestMethod]
        public void BfsSolver_3x3_Standard()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
                            6, 3, 8,
                            0, 7, 1,
                            2, 4, 5];
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver solver = new(optionsMock.Object);
            solver.BfsMode = BfsMode.Standard;

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);

            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_MustSolveCustomTarget_Standard()
        {
            // Not really a test, just a way of getting a count of the minimum number of moves to solve a given situation
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               08, 01, 03, 07,
               02, 05, 00, 14,
               15, 13, 11, 12,
               10, 06, 09, 04];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            byte[] targetBoard = [
                02, 08, 00, 03,
                01, 05, 11, 07,
                15, 06, 12, 14,
                10, 09, 13, 04];

            DynamicWeightAStarSolver solver = new(optionsMock.Object);
            solver.BfsMode = BfsMode.Standard;

            SolveResult result = solver.Solve(board, targetBoard,
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine($"\tMoves: {result.MoveCount}");
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, targetBoard);
        }
    }
}
