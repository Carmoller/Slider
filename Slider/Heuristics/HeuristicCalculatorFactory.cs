using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public class HeuristicCalculatorFactory 
        (IOptions options, IHeuristicElementFactory elementFactory) : IHeuristicCalculatorFactory
    {
        public IHeuristicCalculator GetHeuristicCalculator(Span<int> targetPositions, int gridSize)
        {
            return new HeuristicCalculator(targetPositions, gridSize, options.SolverOptions, elementFactory);
        }
    }
}
