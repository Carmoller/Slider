using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicsStatistics
    {
        long NumberOfCalls { get; set; }
        double TotalTimeSpentMs { get; set; }
        double AverageTimePerCall { get; }
    }
}
