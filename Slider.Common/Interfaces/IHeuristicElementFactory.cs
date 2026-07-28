using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicElementFactory
    {
        IHeuristicCalculator CreateHeuristicCalculator(Span<byte> goalBoard, int gridSize, ISolverOptions solverOptions);
        IHeuristicElement CreateManhattanDistance(Span<int> goalPositions, int gridSize);
        IHeuristicElement CreateLinearConflict(int gridSize);
        IHeuristicElement CreateCornerPattern(Span<int> goalPositions, int gridSize);
        IHeuristicElement CreateColumnAnchor(Span<int> goalPositions, int gridSize, bool ignoreCorners);
        IHeuristicElement CreateEdgePattern(Span<int> goalPositions, int gridSize, bool ignoreCorners);
    }
}
