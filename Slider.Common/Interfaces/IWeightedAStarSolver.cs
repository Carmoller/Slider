using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    public interface IWeightedAStarSolver : ISolver
    {
        double InitialW { get; set; }
    }
}
