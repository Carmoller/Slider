using CommunityToolkit.Mvvm.ComponentModel;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;

namespace Slider.ViewModels
{
    public partial class BuildPuzzleViewModel : ObservableObject, IBuildPuzzleViewModel
    {
        [ObservableProperty]
        public partial int GridSize { get; set; } = 6;
        [ObservableProperty]
        public partial ObservableCollection<ITileControlViewModel> Board { get; set; }
        public BuildPuzzleViewModel()
        {
            Board = [];
            PopulateBoard();
        }

        public ISelectTileViewModel CreateSelectTileViewModel()
        {
            return new SelectTileViewModel(GridSize, [.. Board]);
        }

        private void PopulateBoard()
        {
            if (GridSize == 0)
                return;
            Board.Clear();
            for (int i = 0; i < GridSize * GridSize; i++)
            {
                (int row, int col) = Math.DivRem(i, GridSize);
#warning DEBUG CODE
                byte tileValue = (byte)(i + 1);
                if (i == GridSize * GridSize - 1)
                {
                    tileValue = 0;
                }
                ITileControlViewModel tileControlViewModel = new TileControlViewModel(
                        new BoardTile { Value = tileValue, Row = row, Column = col }, null, null)
                {
                    CanSelect = true,
                    CanGray = false,
                };
                tileControlViewModel.PropertyChanged += TileControlViewModel_PropertyChanged;
                Board.Add(tileControlViewModel);
            }
        }

        private void TileControlViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!(sender is ITileControlViewModel senderVm))
                return;
            if (senderVm.Value == 0)
                return; // We allow more than one blank
            if (e.PropertyName == nameof(TileControlViewModel.Value))
            {
                // Remove duplicate tile values (if any found, set them to 0, which is blank)
                foreach (ITileControlViewModel tileControlViewModel in Board)
                {
                    if (tileControlViewModel == senderVm)
                    {
                        continue;
                    }
                    if (tileControlViewModel.Value == senderVm.Value)
                    {
                        tileControlViewModel.Value = 0; // This will not cause an infinite loop because of the check for Value=0 at the top
                    }
                }
            }
        }

        partial void OnGridSizeChanged(int value)
        {
            PopulateBoard();
        }

    }
}