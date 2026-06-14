using Slider.Heuristics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IHeuristicElement
    {
        string Name { get; }
        bool IsAdditive { get; }
        HeuristicStatistics Statistics { get; }
        int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize);
        int Calculate(byte[] board, int gridSize);
    }
}
