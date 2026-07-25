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
        protected int[] TargetPositions { get; private set; }
        protected byte[] TargetValues { get; private set; }
        private (int divisor, int remainder)[] _divremTable;

        public HeuristicElementBase(Span<int> targetPositions, int gridSize)
        {
            Statistics = new HeuristicsStatistics();
            _gridSize = gridSize;
            TargetPositions = new int[gridSize * gridSize];
            TargetValues = new byte[gridSize * gridSize];
            if (targetPositions == Span<int>.Empty)
            {
                for (int i = 1; i < gridSize * gridSize; i++)
                {
                    TargetPositions[i] = i - 1;
                }
                TargetPositions[0] = gridSize * gridSize - 1;
            }
            else
            {
                targetPositions.CopyTo(TargetPositions);
            }

            for (int i = 0; i < TargetPositions.Length; i++)
            {
                TargetValues[TargetPositions[i]] = (byte)i;
            }

            _divremTable = new (int divisor, int remainder)[gridSize * gridSize];
            for (int i = 0; i < gridSize * gridSize; i++)
            {
                _divremTable[i] = Math.DivRem(i, gridSize);
            }
        }

        protected void TargetPositionsUpdateFromBoard(Span<byte> board)
        {
            for (int i = 0; i < board.Length; i++)
            {
                TargetValues[i] = board[i];
                TargetPositions[board[i]] = i;
            }
        }

        protected int GetTargetPosition(int tileValue)
        {
            return TargetPositions[tileValue];
        }
        protected (int row, int col) GetRowAndColumn(int number)
        {
            return _divremTable[number];
        }
        protected int GetValueAtPosition(int index)
        {
            return TargetValues[index];
        }
    }
}
