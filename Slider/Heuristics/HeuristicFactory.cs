using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicFactory : IHeuristicElementFactory
    {
        public IHeuristicCalculator CreateHeuristicCalculator(SolverOptions solverOptions, int gridSize)
        {
            return new HeuristicCalculator(solverOptions, gridSize, this);
        }

        public IHeuristicElement CreateLinearConflict()
        {
            return new LinearConflict();
        }
        public IHeuristicElement CreateCornerPattern(int gridSize)
        {
            return new CornerPattern(gridSize);
        }
    }
}
