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
        public HeuristicCalculator(ISolverOptions solverOptions, int gridSize, IHeuristicElementFactory elementFactory, IOptions options)
        {
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
        }

        public int GetHeuristic(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            int distance = 0;
            foreach (IHeuristicElement heuristicElement in ElementCalculators)
            {
                distance += heuristicElement.Calculate(board, goalPositions, gridSize);
            }
            return distance;
        }

        public int GetHeuristic(byte[] board, int gridSize)
        {
            int distance = 0;

            foreach (IHeuristicElement heuristicElement in ElementCalculators.Where(p => p.IsAdditive))
            {
                distance += heuristicElement.Calculate(board, gridSize);
            }

            if (_solverOptions.UsePdbs)
            {
                IHeuristicElement heuristicElement = ElementCalculators.First(p => !p.IsAdditive);
                int pdbDistance = heuristicElement.Calculate(board, gridSize);
                distance = Math.Max(distance, pdbDistance);
            }
            return distance;
        }
    }
}
