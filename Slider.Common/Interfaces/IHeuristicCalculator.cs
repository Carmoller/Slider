using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicCalculator
    {
        public List<IHeuristicElement> ElementCalculators { get; }
        int GetHeuristic(byte[] board, int gridSize);
    }
}
