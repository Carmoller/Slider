using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class HeuristicsStatistics : IHeuristicsStatistics
    {
        public long TicksSpent{ get; set; }
        public long NumberOfCalls { get; set; }
        public long TotalTimeSpentMs => TicksSpent / 10000;
        public double AverageTimePerCall => NumberOfCalls > 0 ? (double)TicksSpent / 10000.0 / NumberOfCalls : 0;
    }
}
