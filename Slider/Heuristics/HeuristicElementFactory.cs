using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicElementFactory : IHeuristicElementFactory
    {
        public IHeuristicCalculator CreateHeuristicCalculator(IOptions options, ISolverOptions solverOptions, int gridSize)
        {
            return new HeuristicCalculator(solverOptions, gridSize, this, options);
        }
        public IHeuristicElement CreateManhattanDistance()
        {
            return new ManhattanDistanceCalculator();
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
