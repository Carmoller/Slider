using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicCalculator
    {
        public List<IHeuristicElement> ElementCalculators { get; }
        int GetHeuristic(Span<byte> board, int gridSize);
        void UpdateTargetPositionsFromBoard(Span<byte> board);
   }
}
