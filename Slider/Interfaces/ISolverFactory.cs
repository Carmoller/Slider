using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface ISolverFactory
    {
        public ISolver Create(int gridSize, int heuristic);
    }
}
