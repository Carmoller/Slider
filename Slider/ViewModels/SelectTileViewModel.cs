using CommunityToolkit.Mvvm.ComponentModel;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Slider.ViewModels
{
    public partial class SelectTileViewModel : ObservableObject, ISelectTileViewModel
    {
        [ObservableProperty]
        public partial int GridSize { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<ITileControlViewModel> Board { get; set; }

        public int SelectedValue { get; set; }
        private List<ITileControlViewModel> _alreadyPlacedTiles;
        public SelectTileViewModel(int gridSize, List<ITileControlViewModel> alreadyPlacesTiles)
        {
            Board = [];
            GridSize = gridSize;
            _alreadyPlacedTiles = alreadyPlacesTiles;
            PopulateBoard();
        }

        private void PopulateBoard()
        {
            if (GridSize == 0)
                return;
            Board.Clear();
            for (int i = 0; i < GridSize * GridSize; i++)
            {
                (int row, int col) = Math.DivRem(i, GridSize);
#warning SHOULD BE DRAWN FROM A FACTORY
                byte tileValue = (byte)(i + 1);
                if (i == GridSize * GridSize - 1)
                {
                    tileValue = 0;
                }
                ITileControlViewModel tileControlViewModel = new TileControlViewModel(
                        new BoardTile { Value = tileValue, Row = row, Column = col }, null, null)
                {
                    CanSelect = true,
                    CanGray = true,
                    IsGray = tileValue == 0 ? false : _alreadyPlacedTiles.Exists(p=>p.Value == tileValue)
                };
                Board.Add(tileControlViewModel);
            }
        }

    }
}
