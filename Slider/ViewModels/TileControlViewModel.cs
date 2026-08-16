using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial class TileControlViewModel : ObservableObject, ITileControlViewModel
    {
        private readonly IMainViewModel? _mainViewModel;
        private readonly IOptions? _options;

        public int Value { get { return BoardTile.Value; } set { if (value != BoardTile.Value) { BoardTile.Value = (byte)value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEmpty)); } } }
        public bool IsEmpty{ get { return BoardTile.IsEmpty; } }
        public bool IsHighlighted { get { return BoardTile.IsHighlighted; } set { if (value != BoardTile.IsHighlighted) { BoardTile.IsHighlighted = value; OnPropertyChanged(); } } }
        [ObservableProperty]
        public partial bool CanSelect { get; set; }
        [ObservableProperty]
        public partial bool IsSelected { get; set; }
        [ObservableProperty]
        public partial bool IsBorderHighlighted { get; set; }
        [ObservableProperty] 
        public partial int X { get; set; }
        [ObservableProperty]
        public partial int Y { get; set; }
        public int Row { get { return BoardTile.Row; } set { BoardTile.Row = value; } }
        public int Column { get { return BoardTile.Column; } set { BoardTile.Column = value; } }
        [ObservableProperty]
        public partial bool CanGray { get; set; }
        [ObservableProperty]
        public partial bool IsGray { get; set; }
        [ObservableProperty]
        public partial int TileSize { get; set; }
        public int AnimationDelay { get { return _options == null ? 0 : _options.AnimationDelay; } }
        public BoardTile BoardTile { get; }

        public TileControlViewModel(BoardTile boardTile, IMainViewModel? mainViewModel, IOptions? options)
        {
            _mainViewModel = mainViewModel;
            _options = options;
            BoardTile = boardTile;
        }

        public ITileControlViewModel DeepClone()
        {
            BoardTile boardTile = new();

            TileControlViewModel newVm = new(BoardTile.DeepClone(), _mainViewModel, _options)
            {
                Value = Value,
                CanSelect = CanSelect,
                IsSelected = IsSelected,
                IsBorderHighlighted = IsBorderHighlighted,
                X = X,
                Y = Y,
                Row = Row,
                Column = Column,
                CanGray = CanGray,
                IsGray = IsGray,
                TileSize = TileSize,
            };
            return newVm;
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
