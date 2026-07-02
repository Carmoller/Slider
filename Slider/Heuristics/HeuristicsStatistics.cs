using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Heuristics
{
    public sealed class HeuristicsStatistics : IHeuristicsStatistics
    {
        public long NumberOfCalls { get; set; }
        public double TotalTimeSpentMs {get;set;}
        public double AverageTimePerCall => NumberOfCalls > 0 ? TotalTimeSpentMs / NumberOfCalls : 0;
    }
}
