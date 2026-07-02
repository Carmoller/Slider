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
        public HeuristicCalculator(ISolverOptions solverOptions, int gridSize, IHeuristicElementFactory elementFactory, IOptions options)
        {
            _additiveElements = new();
            _singularElements = new();
            _solverOptions = solverOptions;
            if (_solverOptions.UseManhattanDistance)
            {
                ElementCalculators.Add(elementFactory.CreateManhattanDistance());
            }
            if (_solverOptions.UseLinearConflict)
            {
                ElementCalculators.Add(elementFactory.CreateLinearConflict());
            }
            if (_solverOptions.UseCornerPattern)
            {
                ElementCalculators.Add(elementFactory.CreateCornerPattern(gridSize));
            }
            if (_solverOptions.UseEdgePattern)
            {
                ElementCalculators.Add(new EdgePattern());
            }
            if (_solverOptions.UsePdbs)
            {
                ElementCalculators.Add(new HeuristicPdb(options));
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

            if (_solverOptions.UsePdbs)
            {
# warning Bit of a hurried approach, to assume we only have one singular element, and that is the PDB. Fix!!
                IHeuristicElement heuristicElement = _singularElements[0];
                int pdbDistance = heuristicElement.Calculate(board, gridSize);
                distance = Math.Max(distance, pdbDistance);
            }
            return distance;
        }
    }
}
