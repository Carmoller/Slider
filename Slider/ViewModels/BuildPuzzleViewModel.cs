using CommunityToolkit.Mvvm.ComponentModel;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;

namespace Slider.ViewModels
{
    public partial class BuildPuzzleViewModel : ObservableObject, IBuildPuzzleViewModel
    {
        [ObservableProperty]
        public partial int GridSize { get; set; }
        [ObservableProperty]
        public partial int AvailableTileSize { get; set; }
        [ObservableProperty]
        public partial int BoardTileSize { get; set; }
        public ObservableCollection<ITileControlViewModel> BoardTiles { get; private set; } = [];
        public ObservableCollection<ITileControlViewModel> AvailableTiles { get; private set; } = [];

        private readonly ITileControlViewModelFactory _tileControlViewModelFactory;
        private int _availableTilesWidth = 0;
        private int _boardTilesWidth = 0;
        public BuildPuzzleViewModel(ITileControlViewModelFactory tileControlViewModelFactory)
        {
            _tileControlViewModelFactory = tileControlViewModelFactory;
            GridSize = 5;
        }

        private void CalculateBoardTilesLayout(int width)
        {
            BoardTileSize = width / GridSize;
            for (int i = 0; i < BoardTiles.Count; i++)
            {
                ITileControlViewModel tileControlViewModel = BoardTiles[i];

                tileControlViewModel.TileSize = BoardTileSize;
                tileControlViewModel.X = (i % GridSize) * tileControlViewModel.TileSize;
                tileControlViewModel.Y = (i / GridSize) * tileControlViewModel.TileSize;
            }
        }

        private void CalculateAvailableTilesLayout(int width)
        {
            AvailableTileSize = width / GridSize;
            for (int i=0; i<AvailableTiles.Count; i++)
            {
                ITileControlViewModel tileControlViewModel = AvailableTiles[i];

                tileControlViewModel.TileSize = AvailableTileSize;
                tileControlViewModel.X = (i % GridSize) * tileControlViewModel.TileSize;
                tileControlViewModel.Y = (i / GridSize) * tileControlViewModel.TileSize;
            }
        }

        partial void OnGridSizeChanged(int value)
        {
            BoardTiles.Clear();
            AvailableTiles.Clear();
            for (int i = 0; i < value * value; i++)
            {
                ITileControlViewModel tileControlViewModel = _tileControlViewModelFactory.CreateViewModel(
                    new BoardTile { Value = (byte)(i + 1) });
                AvailableTiles.Add(tileControlViewModel);
                ITileControlViewModel tileControlViewModel2 = _tileControlViewModelFactory.CreateViewModel(
                    new BoardTile { Value = 0 });
                BoardTiles.Add(tileControlViewModel2);
            }
            CalculateAvailableTilesLayout(_availableTilesWidth);
            CalculateBoardTilesLayout(_boardTilesWidth);
        }
        public void AvailableSizeChanged(SizeChangedEventArgs e)
        {
            _availableTilesWidth = (int)(e.NewSize.Width);
            CalculateAvailableTilesLayout(_availableTilesWidth);
        }
        public void BoardSizeChanged(SizeChangedEventArgs e)
        {
            _boardTilesWidth = (int)(e.NewSize.Width);
            CalculateBoardTilesLayout(_availableTilesWidth);
        }

    }
}
