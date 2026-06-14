using Microsoft.Extensions.Options;
using Moq;
using Slider;
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
    public class SolverWeightedAStarTests
    {

        private void VerifyMoves(List<BoardTile> board, SolveResult result)
        {
            int size = (int)Math.Sqrt(board.Count);
            int count = 0;

            foreach (Move move in result.Moves)
            {
                count++;
                BoardTile? fromTile = board.FirstOrDefault(p => p.Row == move.FromRow && p.Column == move.FromColumn);
                if (fromTile == null)
                {
                    Assert.Fail($"Move #{count}: Moving from (row={move.FromRow}, col={move.FromColumn}, which is outside of the board");
                }
                BoardTile? toTile = board.FirstOrDefault(p => p.Row == move.ToRow && p.Column == move.ToColumn);
                if (toTile == null)
                {
                    Assert.Fail($"Move #{count}: Moving from (row={move.FromRow}, col={move.FromColumn}, which is outside of the board");
                }

                if (toTile.Value != 0)
                {
                    Assert.Fail($"Move #{count}: Moving to (row={move.ToRow}, col={move.ToColumn}, which does not contain the blank");
                }
                int tempRow = toTile.Row;
                int tempCol = toTile.Column;

                toTile.Row= fromTile.Row;
                toTile.Column = fromTile.Column;

                fromTile.Row = tempRow;
                fromTile.Column = tempCol;
            }
            foreach (BoardTile tile in board)
            {
                if (tile.Value == 0) // Must be in bottom right corner
                {
                    Assert.AreEqual(size-1, tile.Row, $"Empty tile should be at ({size}, {size}), but was at ({tile.Row}, {tile.Column}) ");
                    Assert.AreEqual(size-1, tile.Column, $"Empty tile should be at ({size}, {size}), but was at ({tile.Row},{tile.Column}) ");
                }
                else
                    Assert.AreEqual((tile.Row * size + tile.Column) + 1, tile.Value, $"Tile {tile.Value} is not in correct spot");
            }
        }

        [TestMethod]
        public void TestSolve()
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

            SolverOptions options = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true };
            List<byte> puzzle = new() { 14, 12, 13, 11, 5, 4, 2, 0, 9, 6, 8, 7, 10, 15, 3, 1 };
            bool solvable = PuzzleGenerator.IsSolvable(puzzle, 4);
            Assert.IsTrue(solvable);
            WeightedAStarSolver solver = new(new Slider.Options { PdbLocation = @"E:\src\net\Slider" });
            SolveResult result = solver.Solve(board, options, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            VerifyMoves(board, result);
        }

        [TestMethod]
        public void Test_Problematic_State_5x5()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            List<BoardTile> board = new();
            board.Add(new BoardTile { Value = 0, Row = 1, Column = 2 });
            board.Add(new BoardTile { Value = 1, Row = 2, Column = 4 });
            board.Add(new BoardTile { Value = 2, Row = 2, Column = 1 });
            board.Add(new BoardTile { Value = 3, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 4, Row = 4, Column = 0 });
            board.Add(new BoardTile { Value = 5, Row = 0, Column = 4 });
            board.Add(new BoardTile { Value = 6, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 7, Row = 4, Column = 1 });
            board.Add(new BoardTile { Value = 8,  Row = 0, Column = 0 });
            board.Add(new BoardTile { Value = 9, Row = 3, Column = 2 });
            board.Add(new BoardTile { Value = 10, Row = 4, Column = 4 });
            board.Add(new BoardTile { Value = 11, Row = 0, Column = 3 });
            board.Add(new BoardTile { Value = 12, Row = 2, Column = 2 });
            board.Add(new BoardTile { Value = 13, Row = 1, Column = 4 });
            board.Add(new BoardTile { Value = 14, Row = 3, Column = 4 });
            board.Add(new BoardTile { Value = 15, Row = 0, Column = 2 });
            board.Add(new BoardTile { Value = 16, Row = 2, Column = 0 });
            board.Add(new BoardTile { Value = 17, Row = 4, Column = 3 });
            board.Add(new BoardTile { Value = 18, Row = 3, Column = 1 });
            board.Add(new BoardTile { Value = 19, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 20, Row = 4, Column = 2 });
            board.Add(new BoardTile { Value = 21, Row = 3, Column = 0 });
            board.Add(new BoardTile { Value = 22, Row = 1, Column = 3 });
            board.Add(new BoardTile { Value = 23, Row = 2, Column = 3 });
            board.Add(new BoardTile { Value = 24, Row = 3, Column = 3 });

            List<byte> puzzle = new() { 8, 6, 15, 11, 5, 19, 3, 0, 33, 13, 16, 2, 12, 23, 1, 21, 18, 9, 24, 14, 4, 7, 20, 17, 10 };
            bool solvable = PuzzleGenerator.IsSolvable(puzzle, 5);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object);
            SolverOptions options = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = true };
            SolveResult result = solver.Solve(board, options, new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            VerifyMoves(board, result);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }
        [TestMethod]
        public void Test_Problematic_State_5x5_2()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");

            List<BoardTile> board = new();
            board.Add(new BoardTile { Value = 0, Row = 0, Column = 3 });
            board.Add(new BoardTile { Value = 1, Row = 3, Column = 4 });
            board.Add(new BoardTile { Value = 2, Row = 2, Column = 1 });
            board.Add(new BoardTile { Value = 3, Row = 4, Column = 3 });
            board.Add(new BoardTile { Value = 4, Row = 1, Column = 2 });
            board.Add(new BoardTile { Value = 5, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 6, Row = 4, Column = 2 });
            board.Add(new BoardTile { Value = 7, Row = 3, Column = 2 });
            board.Add(new BoardTile { Value = 8, Row = 0, Column = 2 });
            board.Add(new BoardTile { Value = 9, Row = 2, Column = 3 });
            board.Add(new BoardTile { Value = 10, Row = 2, Column = 0 });
            board.Add(new BoardTile { Value = 11, Row = 4, Column = 4 });
            board.Add(new BoardTile { Value = 12, Row = 2, Column = 2 });
            board.Add(new BoardTile { Value = 13, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 14, Row = 1, Column = 3 });
            board.Add(new BoardTile { Value = 15, Row = 2, Column = 4 });
            board.Add(new BoardTile { Value = 16, Row = 0, Column = 4 });
            board.Add(new BoardTile { Value = 17, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 18, Row = 3, Column = 3 });
            board.Add(new BoardTile { Value = 19, Row = 0, Column = 0 });
            board.Add(new BoardTile { Value = 20, Row = 3, Column = 1 });
            board.Add(new BoardTile { Value = 21, Row = 4, Column = 0 });
            board.Add(new BoardTile { Value = 22, Row = 4, Column = 1 });
            board.Add(new BoardTile { Value = 23, Row = 3, Column = 0 });
            board.Add(new BoardTile { Value = 24, Row = 1, Column = 4 });

            List<byte> puzzle = board.OrderBy(p => p.Row).ThenBy(p => p.Column).Select(p => p.Value).ToList();
            bool solvable = PuzzleGenerator.IsSolvable(puzzle, 5);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object);
            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UsePdbs = true };
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());

            Assert.AreEqual(SolveResultType.Solved, result.Result);
            Assert.IsGreaterThan(0, result.Moves.Count);
            VerifyMoves(board, result);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

        [TestMethod]
        public void Test_Problematic_State_4x4()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            List<BoardTile> board = new();
            board.Add(new BoardTile { Value = 0, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 1, Row = 1, Column = 2 });
            board.Add(new BoardTile { Value = 2, Row = 2, Column = 0 });
            board.Add(new BoardTile { Value = 3, Row = 3, Column = 0 });
            board.Add(new BoardTile { Value = 4, Row = 0, Column = 2 });
            board.Add(new BoardTile { Value = 5, Row = 0, Column = 3 });
            board.Add(new BoardTile { Value = 6, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 7, Row = 1, Column = 3 });
            board.Add(new BoardTile { Value = 8, Row = 2, Column = 1 });
            board.Add(new BoardTile { Value = 9, Row = 3, Column = 1 });
            board.Add(new BoardTile { Value = 10, Row = 2, Column = 3 });
            board.Add(new BoardTile { Value = 11, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 12, Row = 0, Column = 0 });
            board.Add(new BoardTile { Value = 13, Row = 2, Column = 2 });
            board.Add(new BoardTile { Value = 14, Row = 3, Column = 3 });
            board.Add(new BoardTile { Value = 15, Row = 3, Column = 2 });

            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true };
            List<byte> puzzle = board.OrderBy(p => p.Row * 4 + p.Column).Select(p => p.Value).ToList();
            bool solvable = PuzzleGenerator.IsSolvable(puzzle, 5);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object);
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            //Assert.IsGreaterThan(0, result.Moves.Count);
            //VerifyMoves(board, result);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

        [TestMethod]
        public void Test_4x4_Sprint_vs_NoSprint()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            optionsMock.Setup(p => p.SolveTimeout).Returns(TimeSpan.FromSeconds(30));
            List<BoardTile> board = new();
            board.Add(new BoardTile { Value = 0, Row = 3, Column = 2 });
            board.Add(new BoardTile { Value = 1, Row = 0, Column = 3 });
            board.Add(new BoardTile { Value = 2, Row = 2, Column = 2 });
            board.Add(new BoardTile { Value = 3, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 4, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 5, Row = 1, Column = 3 });
            board.Add(new BoardTile { Value = 6, Row = 3, Column = 3 });
            board.Add(new BoardTile { Value = 7, Row = 2, Column = 3 });
            board.Add(new BoardTile { Value = 8, Row = 2, Column = 0 });
            board.Add(new BoardTile { Value = 9, Row = 2, Column = 1 });
            board.Add(new BoardTile { Value = 10, Row = 1, Column = 2 });
            board.Add(new BoardTile { Value = 11, Row = 0, Column = 0 });
            board.Add(new BoardTile { Value = 12, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 13, Row = 0, Column = 2 });
            board.Add(new BoardTile { Value = 14, Row = 3, Column = 1 });
            board.Add(new BoardTile { Value = 15, Row = 3, Column = 0 });

            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UseSprintFinish = true };
            List<byte> puzzle = board.OrderBy(p => p.Row * 4 + p.Column).Select(p => p.Value).ToList();
            bool solvable = PuzzleGenerator.IsSolvable(puzzle, 5);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object);
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            int sprintMoves = result.MoveCount;
            Console.Write($"Moves with BFS sprint: {sprintMoves}");

            solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true, UseSprintFinish = false };
            result = solver.Solve(board, solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            int noSprintMoves = result.MoveCount;
            Console.Write($"Moves without BFS sprint: {noSprintMoves}");
            //Assert.IsGreaterThan(0, result.Moves.Count);
            //VerifyMoves(board, result);
            Assert.AreEqual(sprintMoves, noSprintMoves);
        }

        [TestMethod]
        public void Test_Simple_3x3()
        {
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.PdbLocation).Returns("E:\\src\\net\\Slider");
            List<BoardTile> board = new();
            board.Add(new BoardTile { Value = 0, Row = 1, Column = 0 });
            board.Add(new BoardTile { Value = 1, Row = 0, Column = 1 });
            board.Add(new BoardTile { Value = 2, Row = 1, Column = 1 });
            board.Add(new BoardTile { Value = 3, Row = 0, Column = 0 });

            SolverOptions solverOptions = new SolverOptions { UseLinearConflict = true, UseCornerPattern = true, UseEdgePattern = true };
            List<byte> puzzle = board.OrderBy(p => p.Row).ThenBy(p=>p.Column).Select(p => p.Value).ToList();
            bool solvable = PuzzleGenerator.IsSolvable(puzzle, 2);
            Assert.IsTrue(solvable);

            WeightedAStarSolver solver = new(optionsMock.Object);
            SolveResult result = solver.Solve(board, solverOptions, new HeuristicElementFactory());
            Assert.AreEqual(SolveResultType.Solved, result.Result);
            //Assert.IsGreaterThan(0, result.Moves.Count);
            //VerifyMoves(board, result);
            Console.WriteLine($"Iterations: {result.IDAStarIterations}");
            Console.WriteLine($"Moves: {result.MoveCount}");
            Console.WriteLine($"States visited: {result.TotalStatesConsidered}");
        }

    }
}
