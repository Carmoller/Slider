using Moq;
using Slider.Common.Interfaces;
using Slider.Heuristics;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class RowColSolverTests
    {
        [TestMethod]
        public void RowColSolver_MustSolve4x4()
        {
            Mock<IOptions> optionsMock = new();
            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<ISolverOptions> solverOptionsMock = new();
            solverOptionsMock.Setup(p => p.UseSprintFinish).Returns(true);
            Mock<IHeuristicElementFactory> heuristicElementFactoryMock = new();
            SolverFactory solverFactory = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = BoardHelper.GetBoardFromArray(
                               [00, 14, 13, 04, 
                                11, 02, 15, 06, 
                                08, 12, 05, 07, 
                                09, 01, 10, 03 ]);

            Assert.IsTrue(BoardHelper.IsSolvable(board));
            byte[] goalState = [001, 002, 003, 004,
                                005, 255, 255, 255,
                                009, 255, 255, 255,
                                013, 255, 255, 255];

            RowColSolver testObject = new(optionsMock.Object, stateInfoFactoryMock.Object, solverFactory);

            SolveResult result = testObject.Solve(board, [], solverOptionsMock.Object,new HeuristicElementFactory());
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedRow(board, 0);
            BoardHelper.VerifySolvedColumn(board, 0);
        }
        [TestMethod]
        public void RowColSolver_MustSolve5x5()
        {
            Mock<IOptions> optionsMock = new();
            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<ISolverOptions> solverOptionsMock = new();
            solverOptionsMock.Setup(p => p.UseSprintFinish).Returns(true);
            Mock<IHeuristicElementFactory> heuristicElementFactoryMock = new();
            SolverFactory solverFactory = new(optionsMock.Object, new StateInfoFactory());
            List<BoardTile> board = BoardHelper.GetBoardFromArray(
                               [00, 24, 23, 22, 21,
                                20, 19, 18, 17, 16,
                                15, 14, 13, 12, 11,
                                10, 09, 08, 07, 06,
                                05, 04, 03, 02, 01]);

            Assert.IsTrue(BoardHelper.IsSolvable(board));
            byte[] goalState = [001, 002, 003, 004, 005,
                                006, 255, 255, 255, 255,
                                011, 255, 255, 255, 255,
                                016, 255, 255, 255, 255,
                                021, 255, 255, 255, 255];

            RowColSolver testObject = new(optionsMock.Object, stateInfoFactoryMock.Object, solverFactory);

            SolveResult result = testObject.Solve(board, [], solverOptionsMock.Object, new HeuristicElementFactory());
            BoardHelper.VerifyMoves(board, result);
            BoardHelper.VerifySolvedRow(board, 0);
            BoardHelper.VerifySolvedColumn(board, 0);
        }
    }
}
