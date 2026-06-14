using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IHeuristicElementFactory
    {
        IHeuristicCalculator CreateHeuristicCalculator(IOptions options, SolverOptions solverOptions, int gridSize);
        IHeuristicElement CreateLinearConflict();
        IHeuristicElement CreateCornerPattern(int gridSize);
    }
}
