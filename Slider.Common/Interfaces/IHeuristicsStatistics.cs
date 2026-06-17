using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IHeuristicsStatistics
    {
        long TicksSpent { get; set; }
        long NumberOfCalls { get; set; }
        long TotalTimeSpentMs { get; }
        double AverageTimePerCall { get; }
    }
}
