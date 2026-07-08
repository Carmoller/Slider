using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicElementFactory : IHeuristicElementFactory
    {
        public IHeuristicCalculator CreateHeuristicCalculator(Span<int> goalPositions, int gridSize, IOptions options, ISolverOptions solverOptions)
        {
            return new HeuristicCalculator(goalPositions, gridSize, solverOptions, this, options);
        }
        public IHeuristicElement CreateManhattanDistance(Span<int> goalPositions, int gridSize)
        {
            return new ManhattanDistanceCalculator(goalPositions, gridSize);
        }

        public IHeuristicElement CreateLinearConflict(int gridSize)
        {
            return new LinearConflict(gridSize);
        }
        public IHeuristicElement CreateCornerPattern(int gridSize)
        {
            return new CornerPattern(gridSize);
        }
    }
}
