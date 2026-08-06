using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;

namespace Slider.ViewModels
{
    public class TileControlViewModel : ITileControlViewModel, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly IMainViewModel? _mainViewModel;
        private readonly IOptions _options;
        private int _x;
        private int _y;
        private int _tileSize;
        public int Value { get { return BoardTile.Value; } }
        public bool IsEmpty{ get { return BoardTile.IsEmpty; } }
        public bool IsHighlighted { get { return BoardTile.IsHighlighted; } set { if (value != BoardTile.IsHighlighted) { BoardTile.IsHighlighted = value; OnPropertyChanged(); } } }
        public int X { get => _x; set { if (_x != value) { _x = value; OnPropertyChanged(); } } }
        public int Y { get => _y; set { if (_y != value) { _y = value; OnPropertyChanged(); } } }
        public int Row { get { return BoardTile.Row; } set { BoardTile.Row = value; } }
        public int Column { get { return BoardTile.Column; } set { BoardTile.Column = value; } }
        public int TileSize { get => _tileSize; set { if (_tileSize != value) { _tileSize = value; OnPropertyChanged(); } } }
        public int AnimationDelay { get { return _options.AnimationDelay; } }
        public BoardTile BoardTile { get; }

        public TileControlViewModel(BoardTile boardTile, IMainViewModel? mainViewModel, IOptions options)
        {
            _mainViewModel = mainViewModel;
            _options = options;
            BoardTile = boardTile;
        }
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public AllowedMove CanMove()
        {
            if (_mainViewModel == null)
                return AllowedMove.None;
            return _mainViewModel.CanMove(this);
        }

        public void Move()
        {
            if (_mainViewModel == null)
                return;
            _mainViewModel.MoveTile(this);
        }
    }
}
