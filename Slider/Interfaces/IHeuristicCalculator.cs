using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IHeuristicCalculator
    {
        public List<IHeuristicElement> ElementCalculators { get; }
        int GetHeuristic(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize);
    }
}
