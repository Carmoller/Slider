using Microsoft.Extensions.Options;
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
        private int _animationDelay= 200;
        private string _pdbLocation = @"E:\src\net\Slider";

        public int GridSize { get => _gridSize; set { if (_gridSize != value) { _gridSize = value; OnPropertyChanged(); } } }
        public int AnimationDelay{ get => _animationDelay; set { if (_animationDelay != value) { _animationDelay = value; OnPropertyChanged(); } } }
        public string PdbLocation { get => _pdbLocation; set { if (_pdbLocation != value) { _pdbLocation = value; OnPropertyChanged(); } } }

        public SolverOptions SolverOptions { get; set; } = new SolverOptions { UseLinearConflict = true, UseEdgePattern = true, UseCornerPattern = true};
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
