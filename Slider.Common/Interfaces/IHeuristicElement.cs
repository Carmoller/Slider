using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicElement
    {
        string Name { get; }
        bool IsAdditive { get; }
        IHeuristicsStatistics Statistics { get; }
        int Calculate(byte[,] board, (byte row, byte col)[] goalPositions, byte gridSize);
        int Calculate(byte[] board, int gridSize);
    }
}
