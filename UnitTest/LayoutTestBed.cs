using Moq;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    // This class does not contain tests as such. 
    // It is a quick place to check the minimum number of moves required to solve a given setup of a board
    [TestClass]
    public class LayoutTestBed
    {
        [TestMethod]
        public void LayoutTestBed_CornerWithNeigborsSwapped_BlankInCol0()
        {
            Mock<IOptions> optionsMock = new();
            StateInfoFactory stateInfoFactory = new();
            HeuristicElementFactory heuristicElementFactory = new();
            byte[] board = [01, 05, 03, 04,
                            02, 06, 07, 08,
                            00, 10, 11, 12,
                            13, 14, 15, 09];
            DynamicWeightAStarSolver solver = new(optionsMock.Object, stateInfoFactory, (board) =>
            {
                return board[0] == 1 && board[1] == 2 && board[4] == 5;
            });

            SolveResult result = solver.Solve(board, Span<byte>.Empty,
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicElementFactory);

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine($"Number of moves required {result.MoveCount}");
        }
        [TestMethod]
        public void LayoutTestBed_CornerWithNeigborsSwapped_BlankInRow0()
        {
            Mock<IOptions> optionsMock = new();
            StateInfoFactory stateInfoFactory = new();
            HeuristicElementFactory heuristicElementFactory = new();
            byte[] board = [01, 05, 00, 04,
                            02, 06, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 03];
            DynamicWeightAStarSolver solver = new(optionsMock.Object, stateInfoFactory, (board) =>
            {
                return board[0] == 1 && board[1] == 2 && board[4] == 5;
            });

            SolveResult result = solver.Solve(board, Span<byte>.Empty,
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicElementFactory);

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine($"Number of moves required {result.MoveCount}");
        }
        [TestMethod]
        public void LayoutTestBed_CornerWithNeigborsSwapped_BlankInRow1Col1()
        {
            Mock<IOptions> optionsMock = new();
            StateInfoFactory stateInfoFactory = new();
            HeuristicElementFactory heuristicElementFactory = new();
            byte[] board = [01, 05, 03, 04,
                            02, 00, 07, 08,
                            09, 10, 11, 12,
                            13, 14, 15, 06];
            DynamicWeightAStarSolver solver = new(optionsMock.Object, stateInfoFactory, (board) =>
            {
                return board[0] == 1 && board[1] == 2 && board[4] == 5;
            });

            SolveResult result = solver.Solve(board, Span<byte>.Empty,
                new SolverOptions { UseCornerPattern = true, UseEdgePattern = true, UseLinearConflict = true, UseManhattanDistance = true },
                heuristicElementFactory);

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Console.WriteLine($"Number of moves required {result.MoveCount}");
        }

    }
}
