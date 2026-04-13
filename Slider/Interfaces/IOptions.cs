using Slider.Solver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Slider.Interfaces
{
    public interface IOptions : INotifyPropertyChanged
    {
        SolverOptions SolverOptions { get; set; }
        int GridSize { get; set; }
        int AnimationDelay { get; set; }
    }
}
