using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class HeuristicCalculator : IHeuristicCalculator
    {
        private readonly ISolverOptions _solverOptions;
        public List<IHeuristicElement> ElementCalculators { get; } = new();
        private List<IHeuristicElement> _additiveElements;
        private List<IHeuristicElement> _singularElements;
        public HeuristicCalculator(Span<int> targetPositions, int gridSize, ISolverOptions solverOptions, IHeuristicElementFactory elementFactory)
        {
            _additiveElements = new();
            _singularElements = new();
            _solverOptions = solverOptions;
            if (_solverOptions.UseManhattanDistance)
            {
                ElementCalculators.Add(elementFactory.CreateManhattanDistance(targetPositions, gridSize));
            }
            if (_solverOptions.UseLinearConflict)
            {
                ElementCalculators.Add(elementFactory.CreateLinearConflict(gridSize));
            }
            if (_solverOptions.UseCornerPattern)
            {
                ElementCalculators.Add(elementFactory.CreateCornerPattern(targetPositions, gridSize));
            }
            if (_solverOptions.UseEdgePattern)
            {
                ElementCalculators.Add(elementFactory.CreateEdgePattern( targetPositions, gridSize, _solverOptions.UseCornerPattern)); // Edge detection should ignore corners, if we are using corner pattern as well
            }
            if (_solverOptions.UseColumnAnchoring)
            {
                ElementCalculators.Add(elementFactory.CreateColumnAnchor(targetPositions, gridSize, _solverOptions.UseCornerPattern)); // Edge detection should ignore corners, if we are using corner pattern as well
            }
            _additiveElements.AddRange(ElementCalculators.Where(p => p.IsAdditive));
            _singularElements.AddRange(ElementCalculators.Where(p => !p.IsAdditive));
        }

        public int GetHeuristic(Span<byte> board, int gridSize)
        {
            int distance = 0;

            foreach (IHeuristicElement heuristicElement in _additiveElements)
            {
                distance += heuristicElement.Calculate(board, gridSize);
            }
            return distance;
        }

        public void UpdateTargetPositionsFromBoard(Span<byte> board)
        {
            foreach (IHeuristicElement element in _additiveElements)
            {
                element.UpdateTargetPositionsFromBoard(board);
            }
            foreach (IHeuristicElement element in _singularElements)
            {
                element.UpdateTargetPositionsFromBoard(board);
            }
        }

    }
}
