using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Options;
using Slider.Common;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider
{
    public partial class Options : ObservableObject, IOptions
    {
        [ObservableProperty]
        public partial int GridSize { get; set; } = 3;
        [ObservableProperty]
        public partial int AnimationDelay { get; set; } = 200;
        [ObservableProperty]
        public partial TimeSpan SolveTimeout { get; set; } = TimeSpan.MinValue;

        public ISolverOptions SolverOptions { get; set; } = new SolverOptions { UseManhattanDistance = true, UseLinearConflict = true, UseEdgePattern = true, UseCornerPattern = true, UseSprintFinish = true };
        public List<SolverDescriptor> SolverSelector { get; } = new();

        public Options(IStateInfoFactory stateInfoFactory)
        {
            SolveTimeout = TimeSpan.FromSeconds(30);
            SolverSelector = [
                new SolverDescriptor{LowHeuristic = 0, HighHeuristic = 60, Solver = new DynamicWeightAStarSolver(this, stateInfoFactory), SolverParameters=[] },
                new SolverDescriptor{LowHeuristic = 61, HighHeuristic = 80, Solver = new DynamicWeightAStarSolver(this, stateInfoFactory), SolverParameters=[2] },
                new SolverDescriptor{LowHeuristic = 80, HighHeuristic = 100, Solver = new DynamicWeightAStarSolver(this, stateInfoFactory),  SolverParameters=[3] },
                new SolverDescriptor{LowHeuristic = 100, HighHeuristic = int.MaxValue, Solver = new DynamicWeightAStarSolver(this, stateInfoFactory), SolverParameters=[3.5] },
                ];
        }
    }
}
