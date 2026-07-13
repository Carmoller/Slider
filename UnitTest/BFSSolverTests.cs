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
    public class BFSSolverTests
    {
        [TestMethod]
        public void BfsSolver_Simple3x3()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
                            6, 3, 8,
                            0, 7, 1,
                            2, 4, 5];
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            BfsSolver solver = new(optionsMock.Object);
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
        public void BfsSolver_Simple4x4()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               10, 13, 00, 07,
               12, 14, 01, 03,
               06, 05, 15, 08,
               09, 04, 11, 02];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            BfsSolver solver = new(optionsMock.Object);
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
        public void BfsSolver_LockedCorner_BlankUnder1()
        {
            // Not really a test, just a way of getting a count of the minimum number of moves to solve a given situation
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               01, 02, 03, 07,
               00, 08, 10, 04,
               06, 05, 15, 14,
               09, 13, 11, 12];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            BfsSolver solver = new(optionsMock.Object)
            {
                BfsMode = BfsMode.Standard,
                BfsPurpose = BfsPurpose.SolveTopRow
            };

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine("Blank under 1");
            Console.WriteLine("=============");
            int count = 0;
            foreach (Move move in result.Moves)
            {
                count++;
                BoardHelper.DoMove(board, move, count);
                StringBuilder sb = new();
                Console.WriteLine(board.ToPrettyPrintedBoardString());
            }
            Console.WriteLine($"\tMoves: {result.MoveCount}");
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_LockedCorner_BlankUnder2()
        {
            // Not really a test, just a way of getting a count of the minimum number of moves to solve a given situation
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               01, 02, 03, 07,
               08, 00, 10, 04,
               06, 05, 15, 14,
               09, 13, 11, 12];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            BfsSolver solver = new(optionsMock.Object)
            {
                BfsMode = BfsMode.Standard,
                BfsPurpose = BfsPurpose.SolveTopRow
            };

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine("Blank under 2");
            Console.WriteLine("=============");
            int count = 0;
            foreach (Move move in result.Moves)
            {
                count++;
                BoardHelper.DoMove(board, move, count);
                StringBuilder sb = new();
                Console.WriteLine(board.ToPrettyPrintedBoardString());
            }
            Console.WriteLine($"\tMoves: {result.MoveCount}");
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_LockedCorner_BlankUnder3()
        {
            // Not really a test, just a way of getting a count of the minimum number of moves to solve a given situation
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               01, 02, 03, 07,
               08, 10, 00, 04,
               06, 05, 15, 14,
               09, 13, 11, 12];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            BfsSolver solver = new(optionsMock.Object)
            {
                BfsMode = BfsMode.Standard,
                BfsPurpose = BfsPurpose.SolveTopRow
            };

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine("Blank under 3");
            Console.WriteLine("=============");
            int count = 0;
            foreach (Move move in result.Moves)
            {
                count++;
                BoardHelper.DoMove(board, move, count);
                StringBuilder sb = new();
                Console.WriteLine(board.ToPrettyPrintedBoardString());
            }
            Console.WriteLine($"\tMoves: {result.MoveCount}");
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_LockedCorner_BlankUnder4()
        {
            // Not really a test, just a way of getting a count of the minimum number of moves to solve a given situation
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));

            byte[] board = [
               01, 02, 03, 07,
               08, 10, 14, 04,
               05, 06, 15, 00,
               09, 13, 11, 12];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            BfsSolver solver = new(optionsMock.Object)
            {
                BfsMode = BfsMode.Standard,
                BfsPurpose = BfsPurpose.SolveTopRow
            };

            SolveResult result = solver.Solve(board, [],
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true, UseSprintFinish = false },
                new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine("Blank under 4");
            Console.WriteLine("=============");
            int count = 0;
            foreach (Move move in result.Moves)
            {
                count++;
                BoardHelper.DoMove(board, move, count);
                StringBuilder sb = new();
                Console.WriteLine(board.ToPrettyPrintedBoardString());
            }
            Console.WriteLine($"\tMoves: {result.MoveCount}");
            Console.WriteLine($"\tStates visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void BfsSolver_MustSolveCustomTarget()
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

            BfsSolver solver = new(optionsMock.Object);

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
