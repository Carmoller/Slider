using Microsoft.Extensions.Options;
using Moq;
using Slider;
using Slider.Heuristics;
using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class SolverWeightedAStarTests
    {
        [TestMethod]
        public void TestSolve()
        {
            byte[] board = [14, 12, 13, 11,
                            05, 04, 02, 00,
                            09, 06, 08, 07,
                            10, 15, 03, 01];

            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(60));
            SolverOptions solverOptions = new SolverOptions { UseManhattanDistance = true,  UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true };
            
            bool solvable = BoardHelper.IsSolvable(board);
            Assert.IsTrue(solvable);
            WeightedAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            SolveResult result = solver.Solve(board, [], solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
        }

        [TestMethod]
        public void Test_Problematic_State_5x5()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));

            byte[] board = [
                            08, 06, 15, 11, 05,
                            19, 03, 00, 22, 13,
                            16, 02, 12, 23, 01,
                            21, 18, 09, 24, 14,
                            04, 07, 20, 17, 10];
            bool solvable = PuzzleGenerator.IsSolvable(board, 5);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            solver.InitialW = 2;
            SolverOptions options = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = true };
            SolveResult result = solver.Solve(board, [], options, new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void Test_Problematic_State_5x5_2()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(40));


            byte[] board = [
                19, 05, 08, 00, 16,
                17, 13, 04, 14, 24,
                10, 02, 12, 09, 15,
                23, 20, 07, 18, 01,
                21, 22, 06, 03, 11];

            Assert.IsTrue(BoardHelper.IsSolvable(board));

            WeightedAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = true, UseSprintFinish = true };
            SolveResult result = solver.Solve(board, [], solverOptions, new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

        [TestMethod]
        public void Test_Problematic_State_4x4()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));

            byte[] board = [
                          12, 06, 04, 05,
                          11, 00, 01, 07,
                          02, 08, 13, 10,
                          03, 09, 15, 14];

            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true };
            bool solvable = PuzzleGenerator.IsSolvable(board, 5);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            SolveResult result = solver.Solve(board, [], solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

        [TestMethod]
        public void Test_Simple_3x3()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));

            byte[] board = [
                                03, 01,
                                00, 02];

            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true };
            bool solvable = PuzzleGenerator.IsSolvable(board, 2);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            SolveResult result = solver.Solve(board, [], solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board, Span<byte>.Empty);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

    }
}
