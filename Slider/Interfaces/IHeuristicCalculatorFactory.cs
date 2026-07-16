using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public  interface IHeuristicCalculatorFactory
    {
        IHeuristicCalculator GetHeuristicCalculator(Span<int> targetPositions, int gridSize);
    }
}
