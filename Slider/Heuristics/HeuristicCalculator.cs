using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class HeuristicCalculator : IHeuristicCalculator
    {
        private SolverOptions _solverOptions;
        public List<IHeuristicElement> ElementCalculators { get; } = new();
        public HeuristicCalculator(SolverOptions solverOptions, int gridSize, IHeuristicElementFactory elementFactory)
        {
            _solverOptions = solverOptions;
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
        }

        public int GetHeuristic(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            int distance = ManhattanDistance(board, goalPositions, gridSize);

            foreach (IHeuristicElement heuristicElement in ElementCalculators)
            {
                distance += heuristicElement.Calculate(board, goalPositions, gridSize);
            }
            return distance;
        }

        public int ManhattanDistance(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
        {
            int distance = 0;
            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    int value = board[row, col];
                    if (value == 0) continue;

                    var (goalRow, goalCol) = goalPositions[value];
                    distance += Math.Abs(row - goalRow) + Math.Abs(col - goalCol);
                }
            }
            return distance;
        }

    }
}
