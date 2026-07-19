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
    public class Options : IOptions, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private int _gridSize = 4;
        private int _animationDelay = 200;
        private string _pdbLocation = @"E:\src\net\Slider";
        private TimeSpan _solveTimeout = TimeSpan.MinValue;
        public int GridSize { get => _gridSize; set { if (_gridSize != value) { _gridSize = value; OnPropertyChanged(); } } }
        public int AnimationDelay { get => _animationDelay; set { if (_animationDelay != value) { _animationDelay = value; OnPropertyChanged(); } } }
        public string PdbLocation { get => _pdbLocation; set { if (_pdbLocation != value) { _pdbLocation = value; OnPropertyChanged(); } } }
        public TimeSpan SolveTimeout { get => _solveTimeout; set { if (_solveTimeout != value) { _solveTimeout = value; OnPropertyChanged(); } } }

        public ISolverOptions SolverOptions { get; set; } = new SolverOptions { UseManhattanDistance = true, UseLinearConflict = true, UseEdgePattern = true, UseCornerPattern = true, UseSprintFinish = true };
        public List<SolverDescriptor> SolverSelector { get; } = new();

        public Options(IStateInfoFactory stateInfoFactory)
        {
            SolveTimeout = TimeSpan.FromSeconds(30);
            SolverSelector = [
                new SolverDescriptor{LowHeuristic = 0, HighHeuristic = 60, Solver = new DynamicWeightAStarSolver(this), SolverParameters=[] },
                new SolverDescriptor{LowHeuristic = 61, HighHeuristic = 80, Solver = new DynamicWeightAStarSolver(this), SolverParameters=[2] },
                new SolverDescriptor{LowHeuristic = 80, HighHeuristic = 100, Solver = new DynamicWeightAStarSolver(this),  SolverParameters=[3] },
                new SolverDescriptor{LowHeuristic = 100, HighHeuristic = int.MaxValue, Solver = new DynamicWeightAStarSolver(this), SolverParameters=[3.5] },
                ];
        }
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
