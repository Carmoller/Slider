using Moq;
using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTest
{
    [TestClass]
    public class SolverFactoryTests
    {
        #region FakeSolvers
        private class FakeSolver1 : ISolver
        {
            public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }

            public SolveResult Solve(byte[] board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
        }
        private class FakeSolver2 : ISolver
        {
            public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }

            public SolveResult Solve(byte[] board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
        }
        private class FakeSolver3 : ISolver
        {
            public int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
            public SolveResult Solve(byte[] board, byte[] targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        [TestMethod]
        public void SolverFactory_MustReturnBasedOnGridSize()
        {
            FakeSolver1 fakeSolver1 = new();
            FakeSolver2 fakeSolver2 = new();
            FakeSolver3 fakeSolver3 = new();

            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolverSelector).Returns(
                [new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 0, HighHeuristic = int.MaxValue, Solver = fakeSolver1, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 3, HighGridSize = 5, LowHeuristic = 0, HighHeuristic = int.MaxValue, Solver = fakeSolver2, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 6, HighGridSize = 8, LowHeuristic = 0, HighHeuristic = int.MaxValue, Solver = fakeSolver3, SolverParameters = [] }
            ]);

            SolverFactory testObject = new(optionsMock.Object, stateInfoFactoryMock.Object);

            for (int i = 2; i < 9; i++)
            {
                ISolver solver = testObject.Create(i, 1);
                if (i == 2)
                    Assert.IsInstanceOfType(solver, typeof(FakeSolver1));
                if (i >= 3 && i <= 5)
                    Assert.IsInstanceOfType(solver, typeof(FakeSolver2));
                if (i >= 6 && i <= 8)
                    Assert.IsInstanceOfType(solver, typeof(FakeSolver3));
            }
        }

        [TestMethod]
        public void SolverFactory_MustReturnBasedOnHeuristic()
        {
            FakeSolver1 fakeSolver1 = new();
            FakeSolver2 fakeSolver2 = new();
            FakeSolver3 fakeSolver3 = new();

            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolverSelector).Returns(
                [new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 0, HighHeuristic = 29, Solver = fakeSolver1, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 30, HighHeuristic = 59, Solver = fakeSolver2, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 60, HighHeuristic = int.MaxValue, Solver = fakeSolver3, SolverParameters = [] }
            ]);

            SolverFactory testObject = new(optionsMock.Object, stateInfoFactoryMock.Object);

            for (int i = 0; i < 2; i++)
            {
                ISolver solver = testObject.Create(2, i * 30);
                if (i == 0)
                    Assert.IsInstanceOfType(solver, typeof(FakeSolver1));
                if (i == 1)
                    Assert.IsInstanceOfType(solver, typeof(FakeSolver2));
                if (i == 2)
                    Assert.IsInstanceOfType(solver, typeof(FakeSolver3));
            }
        }

        [TestMethod]
        public void SolverFactory_UnconfiguredGridSize_MustThrow()
        {
            FakeSolver1 fakeSolver1 = new();
            FakeSolver2 fakeSolver2 = new();
            FakeSolver3 fakeSolver3 = new();

            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolverSelector).Returns(
                [new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 0, HighHeuristic = 29, Solver = fakeSolver1, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 30, HighHeuristic = 59, Solver = fakeSolver2, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 60, HighHeuristic = int.MaxValue, Solver = fakeSolver3, SolverParameters = [] }
            ]);

            SolverFactory testObject = new(optionsMock.Object, stateInfoFactoryMock.Object);


            Assert.ThrowsExactly<InvalidOperationException>(() => { ISolver solver = testObject.Create(3, 30); });
        }
        [TestMethod]
        public void SolverFactory_UnconfiguredHeuristic_MustThrow()
        {
            FakeSolver1 fakeSolver1 = new();
            FakeSolver2 fakeSolver2 = new();
            FakeSolver3 fakeSolver3 = new();

            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolverSelector).Returns(
                [new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 0, HighHeuristic = 29, Solver = fakeSolver1, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 30, HighHeuristic = 59, Solver = fakeSolver2, SolverParameters = [] },
                new SolverDescriptor { LowGridSize = 2, HighGridSize = 2, LowHeuristic = 60, HighHeuristic = 90, Solver = fakeSolver3, SolverParameters = [] }
            ]);

            SolverFactory testObject = new(optionsMock.Object, stateInfoFactoryMock.Object);


            Assert.ThrowsExactly<InvalidOperationException>(() => { ISolver solver = testObject.Create(2, 91); });
        }

    }
}
