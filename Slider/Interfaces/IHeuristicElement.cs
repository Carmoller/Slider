using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IHeuristicElement
    {
        string Name { get; }
        HeuristicStatistics Statistics { get; }
        int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize);
    }
}
