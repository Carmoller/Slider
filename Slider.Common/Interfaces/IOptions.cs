using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IOptions : INotifyPropertyChanged
    {
        ISolverOptions SolverOptions { get; set; }
        int GridSize { get; set; }
        int AnimationDelay { get; set; }
        TimeSpan SolveTimeout { get; set; }
        List<SolverDescriptor> SolverSelector{ get; }
    }
}
