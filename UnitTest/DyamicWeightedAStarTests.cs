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

            // Set up worst-case board for 3x3 - requires 31 moves to solve
            byte[] board = [
                            8, 6, 7,
                            2, 5, 4,
                            3, 0, 1];
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
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

            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
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

            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

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
                heuristicsFactory);
            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

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
                heuristicsFactory);
            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());

            SolveResult result = solver.Solve(board, [], new SolverOptions(), heuristicsFactory);

            Console.WriteLine($"States considered: {result.TotalStatesConsidered}");
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }
        [TestMethod]
        public void BfsSolver_10x10Board_Deadlock()
        {
            // This board is immensely slow for the Bidirational A* sovler, so we test it here as well
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(new TimeSpan(0, 0, 30));
            // This test attempts to solve a board that choked the Dynamic Weighted A* solver
            byte[] board = [78, 08, 58, 46, 76, 49, 03, 40, 96, 35,
                            72, 23, 21, 47, 02, 27, 45, 38, 91, 95,
                            77, 56, 15, 13, 36, 94, 19, 84, 43, 93,
                            57, 61, 24, 98, 22, 14, 80, 05, 53, 87,
                            11, 55, 10, 20, 60, 26, 52, 63, 75, 82,
                            18, 51, 92, 17, 39, 37, 62, 34, 66, 74,
                            12, 79, 86, 50, 89, 33, 31, 99, 30, 32,
                            97, 67, 54, 70, 01, 71, 25, 85, 09, 73,
                            00, 42, 06, 44, 29, 64, 48, 28, 59, 83,
                            07, 41, 69, 90, 88, 04, 65, 68, 16, 81];
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver testObject = new(optionsMock.Object, new StateInfoFactory());

            SolveResult result = testObject.Solve(board,
                BoardHelper.GetDefaultTargetBoard(board),
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
            Console.WriteLine($"Min h: {result.MinimumH}");
            Console.WriteLine($"Min h time: {result.MinimumHTime}");
            Console.WriteLine();
            Console.WriteLine($"{result.TotalStatesConsidered / result.TimeSpent.TotalMilliseconds} States / ms");
            Console.WriteLine();

            BoardHelper.VerifyMoves(board, result);
            Console.WriteLine(board.ToPrettyPrintedBoardString());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);

        }
        [TestMethod]
        public void BfsSolver_10x10Board_Timeout()
        {
            // This board is immensely slow for the Bidirational A* sovler, so we test it here as well
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(new TimeSpan(0, 0, 30));
            // This test attempts to solve a board that choked the Dynamic Weighted A* solver
            byte[] board = [63, 41, 44, 10, 88, 58, 97, 59, 19, 99,
                            21, 56, 70, 64, 79, 05, 15, 08, 30, 61,
                            31, 01, 38, 86, 74, 68, 39, 47, 29, 49,
                            89, 14, 73, 20, 78, 76, 00, 83, 28, 94,
                            45, 57, 24, 26, 95, 04, 40, 98, 12, 71,
                            62, 34, 42, 81, 25, 13, 33, 09, 77, 93,
                            46, 85, 23, 72, 03, 52, 82, 54, 35, 50,
                            69, 75, 65, 36, 16, 84, 06, 02, 55, 07,
                            43, 80, 48, 90, 51, 32, 92, 60, 17, 11,
                            27, 91, 18, 87, 53, 22, 66, 37, 67, 96];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver testObject = new(optionsMock.Object, new StateInfoFactory());

            SolveResult result = testObject.Solve(board,
                BoardHelper.GetDefaultTargetBoard(board),
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
            Console.WriteLine($"Min h: {result.MinimumH}");
            Console.WriteLine($"Min h time: {result.MinimumHTime}");
            Console.WriteLine();
            Console.WriteLine($"{result.TotalStatesConsidered / result.TimeSpent.TotalMilliseconds} States / ms");
            Console.WriteLine();

            BoardHelper.VerifyMoves(board, result);
            Console.WriteLine(board.ToPrettyPrintedBoardString());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);

        }

        [TestMethod]
        public void BfsSolver_3x3_Standard()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            // Set up worst-case board for 3x3 - requires 31 moves to solve
            byte[] board = [
                            8, 6, 7,
                            2, 5, 4,
                            3, 0, 1]; 
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory())
            {
                BfsMode = BfsMode.Standard
            };

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

            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
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

        [TestMethod]
        public void GodsNumber_3x3()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));
            byte[] board = [
                01, 02, 03,
                04, 05, 06,
                07, 08, 00];
            DynamicWeightAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            solver.BfsMode = BfsMode.Standard;

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
        }

    }
}
