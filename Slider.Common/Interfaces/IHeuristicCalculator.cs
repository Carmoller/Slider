using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicCalculator
    {
        public List<IHeuristicElement> ElementCalculators { get; }
        int GetHeuristic(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize);
        int GetHeuristic(byte[] board, int gridSize);
    }
}
