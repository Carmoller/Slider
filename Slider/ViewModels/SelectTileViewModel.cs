using CommunityToolkit.Mvvm.ComponentModel;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.SliderEventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Input;

namespace Slider.ViewModels
{
    public partial class SelectTileViewModel : ObservableObject, ISelectTileViewModel
    {
        public event EventHandler<SetBoardSelectionEventArgs>? SelectedChanged;

        [ObservableProperty]
        public partial int GridSize { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<ITileControlViewModel> Board { get; set; }
        public int SelectedValue { get; set; }
        private List<ITileControlViewModel> _alreadyPlacedTiles;
        private string _keyPressString = string.Empty;
        private DateTime _latestKeyPress = DateTime.MinValue;
        private int KeyPressMaxIntervalMs = 500;

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

        private void ResetKeyPressString()
        {
            _keyPressString = string.Empty;
            _latestKeyPress = DateTime.MinValue;
        }

        public void KeyDown(KeyEventArgs e)
        {
            string? key = new KeyConverter().ConvertToString(e.Key);
            if ((key == null) ||
                (Keyboard.Modifiers != ModifierKeys.None))
            {
                ResetKeyPressString();
                return;
            }
            if (e.Key == Key.Space)
                key = "0";
            key = key.Replace("NumPad", "");
            if (!char.IsDigit(key[0]))
            {
                ResetKeyPressString();
                return;
            }

            if ((DateTime.UtcNow - _latestKeyPress).TotalMilliseconds > KeyPressMaxIntervalMs)
            {
                ResetKeyPressString();
            }
            _keyPressString += key;
            _latestKeyPress = DateTime.UtcNow;
            ITileControlViewModel? tile = Board.FirstOrDefault(p=>p.Value == Convert.ToByte(_keyPressString));
            if (tile == null)
            {
                ResetKeyPressString();
                return;
            }
            SelectedChanged?.Invoke(this, new SetBoardSelectionEventArgs { Selected = tile });
        }
    }
}
