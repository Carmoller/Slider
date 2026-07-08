using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicElementBase
    {
        public IHeuristicsStatistics Statistics { get; }
        protected int _gridSize;
        protected int[] GoalPositions { get; private set; }
        protected byte[] GoalValues { get; private set; }
        private (int divisor, int remainder)[] _divremTable;

        public HeuristicElementBase(Span<int> goalPositions, int gridSize)
        {
            Statistics = new HeuristicsStatistics();
            _gridSize = gridSize;
            GoalPositions = new int[gridSize * gridSize];
            GoalValues = new byte[gridSize * gridSize];
            if (goalPositions == Span<int>.Empty)
            {
                for (int i = 1; i < gridSize * gridSize; i++)
                {
                    GoalPositions[i] = i - 1;
                }
                GoalPositions[0] = gridSize * gridSize - 1;
            }
            else
            {
                goalPositions.CopyTo(GoalPositions);
            }

            for (int i = 0; i < GoalPositions.Length; i++)
            {
                GoalValues[GoalPositions[i]] = (byte)i;
            }

            _divremTable = new (int divisor, int remainder)[gridSize * gridSize];
            for (int i = 0; i < gridSize * gridSize; i++)
            {
                _divremTable[i] = Math.DivRem(i, gridSize);
            }
        }

        protected int GetTargetPosition(int tileValue)
        {
            return GoalPositions[tileValue];
        }
        protected (int row, int col) GetRowAndColumn(int number)
        {
            return _divremTable[number];
        }
        protected int GetValueAtPosition(int index)
        {
            return GoalValues[index];
        }
    }
}
