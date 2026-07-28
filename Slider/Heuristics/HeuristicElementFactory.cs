using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicElementFactory : IHeuristicElementFactory
    {
        public IHeuristicCalculator CreateHeuristicCalculator(Span<byte> goalBoard, int gridSize, ISolverOptions solverOptions)
        {
            int[] goalPositions = new int[gridSize*gridSize];
            for (int i = 0; i < goalBoard.Length; i++)
            {
                goalPositions[goalBoard[i]] = i;
            }
            return new HeuristicCalculator(goalPositions, gridSize, solverOptions, this);
        }
        public IHeuristicElement CreateManhattanDistance(Span<int> goalPositions, int gridSize)
        {
            return new ManhattanDistanceCalculator(goalPositions, gridSize);
        }

        public IHeuristicElement CreateLinearConflict(int gridSize)
        {
            return new LinearConflict(gridSize);
        }
        public IHeuristicElement CreateCornerPattern(Span<int> goalPositions, int gridSize)
        {
            return new CornerPattern(goalPositions, gridSize);
        }
        public IHeuristicElement CreateColumnAnchor(Span<int> goalPositions, int gridSize, bool ignoreCorners)
        {
            return new ColumnAnchoring(goalPositions, gridSize, true);
        }
        public IHeuristicElement CreateEdgePattern(Span<int> goalPositions, int gridSize, bool ignoreCorners)
        {
            return new EdgePattern(goalPositions, gridSize, ignoreCorners);
        }
    }
}
