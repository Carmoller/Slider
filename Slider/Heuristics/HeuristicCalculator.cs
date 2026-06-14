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
        private readonly SolverOptions _solverOptions;
        public List<IHeuristicElement> ElementCalculators { get; } = new();
        public HeuristicCalculator(SolverOptions solverOptions, int gridSize, IHeuristicElementFactory elementFactory, IOptions options)
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
            if (_solverOptions.UsePdbs)
            {
                ElementCalculators.Add(new HeuristicPdb(options));
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

        public int GetHeuristic(byte[] board, int gridSize)
        {
            int distance = ManhattanDistance(board, gridSize);

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

        public static int ManhattanDistance(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize)
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
        public static int ManhattanDistance(byte[] board, int gridSize)
        {
            int distance = 0;
            for (int i = 0; i < board.Length; i++)
            {
                int targetRow;
                int targetCol;
                int row = i / gridSize;
                int col = i % gridSize;

                if (board[i] == 0)
                {
                    continue;
                }

                targetRow = (board[i] - 1) / gridSize;
                targetCol = (board[i] - 1) % gridSize;
                distance += Math.Abs(row - targetRow) + Math.Abs(col - targetCol);
            }
            return distance;
        }
    }
}
