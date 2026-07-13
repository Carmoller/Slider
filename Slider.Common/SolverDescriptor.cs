using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common
{
    public class SolverDescriptor
    {
        public int LowGridSize { get; set; }
        public int HighGridSize { get; set; }
        public int LowHeuristic { get; set; }
        public int HighHeuristic { get; set; }
        public required ISolver Solver { get; set; }
        public required object[] SolverParameters { get; set; }

    }
}
