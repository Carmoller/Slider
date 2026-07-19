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
            public SolveResult Solve(Span<byte> board, Span<byte> targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
        }
        private class FakeSolver2 : ISolver
        {
            public SolveResult Solve(Span<byte> board, Span<byte> targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
        }
        private class FakeSolver3 : ISolver
        {
            public SolveResult Solve(Span<byte> board, Span<byte> targetBoard, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory)
            {
                throw new NotImplementedException();
            }
        }
        #endregion

        [TestMethod]
        public void SolverFactory_MustReturnBasedOnHeuristic()
        {
            FakeSolver1 fakeSolver1 = new();
            FakeSolver2 fakeSolver2 = new();
            FakeSolver3 fakeSolver3 = new();

            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolverSelector).Returns(
                [new SolverDescriptor {LowHeuristic = 0, HighHeuristic = 29, Solver = fakeSolver1, SolverParameters = [] },
                new SolverDescriptor  {LowHeuristic = 30, HighHeuristic = 59, Solver = fakeSolver2, SolverParameters = [] },
                new SolverDescriptor  {LowHeuristic = 60, HighHeuristic = int.MaxValue, Solver = fakeSolver3, SolverParameters = [] }
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
        public void SolverFactory_UnconfiguredHeuristic_MustThrow()
        {
            FakeSolver1 fakeSolver1 = new();
            FakeSolver2 fakeSolver2 = new();
            FakeSolver3 fakeSolver3 = new();

            Mock<IStateInfoFactory> stateInfoFactoryMock = new();
            Mock<IOptions> optionsMock = new();
            optionsMock.Setup(p => p.SolverSelector).Returns(
                [new SolverDescriptor { LowHeuristic = 0, HighHeuristic = 29, Solver = fakeSolver1, SolverParameters = [] },
                new SolverDescriptor  { LowHeuristic = 30, HighHeuristic = 59, Solver = fakeSolver2, SolverParameters = [] },
                new SolverDescriptor  { LowHeuristic = 60, HighHeuristic = 90, Solver = fakeSolver3, SolverParameters = [] }
            ]);

            SolverFactory testObject = new(optionsMock.Object, stateInfoFactoryMock.Object);


            Assert.ThrowsExactly<InvalidOperationException>(() => { ISolver solver = testObject.Create(2, 91); });
        }

    }
}
