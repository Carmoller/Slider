using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicElementFactory
    {
        IHeuristicCalculator CreateHeuristicCalculator(IOptions options, ISolverOptions solverOptions, int gridSize);
        IHeuristicElement CreateManhattanDistance();
        IHeuristicElement CreateLinearConflict();
        IHeuristicElement CreateCornerPattern(int gridSize);
    }
}
