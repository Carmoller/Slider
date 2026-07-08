using Moq;
using Slider;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class MHAStarTests
    {
        [TestMethod]
        public void Test_Problematic_State_5x5()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(4000));


            List<BoardTile> board = BoardHelper.GetBoardFromArray(
                               [01, 14, 13, 04, 20,
                                11, 02, 15, 06, 00,
                                08, 24, 05, 07, 10,
                                22, 19, 09, 17, 12,
                                23, 21, 16, 18, 03,]);

            ;
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            MHAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = true, UseSprintFinish = true };
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

        [TestMethod]
        public void Test_Problematic_State_5x5_2()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(4000));


            List<BoardTile> board = BoardHelper.GetBoardFromArray(
                               [19, 05, 08, 00, 16,
                                17, 13, 04, 14, 24,
                                10, 02, 12, 09, 15,
                                23, 20, 07, 18, 01,
                                21, 22, 06, 03, 11,]);

            ;
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            MHAStarSolver solver = new(optionsMock.Object, new StateInfoFactory());
            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = true, UseSprintFinish = true };
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void Test_Problematic_State_6x6()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(4000));

            List<BoardTile> board = BoardHelper.GetBoardFromArray(
                                [07, 35, 14, 03, 12, 04,
                                16, 20, 29, 08, 19, 26,
                                22, 30, 17, 23, 09, 06,
                                01, 25, 33, 27, 15, 32,
                                28, 31, 21, 24, 00, 02,
                                13, 34, 10, 11, 18, 05]);
            Assert.IsTrue(BoardHelper.IsSolvable(board));

            MHAStarSolver solver = new(optionsMock.Object, new StateInfoFactory()) { InitialW = 4 };
            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = false, UseSprintFinish = false };
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());
            ;

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedBoard(board);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

    }
}
