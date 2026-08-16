using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Slider.ViewModels
{
    public partial class BoardViewModel : ObservableObject, IBoardViewModel
    {
        private int _gridSize;
        public int GridSize { get { return _gridSize; } set { _gridSize = value; OnGridSizeChanged(value); } }
        [ObservableProperty]
        public partial bool CanSelect { get; set; }
        [ObservableProperty]
        public partial bool CanGray { get; set; }
        [ObservableProperty]
        public partial int TileSize { get; set; }
        [ObservableProperty]
        public partial ITileControlViewModel? Selected { get; set; }
        [ObservableProperty]
        public partial ITileControlViewModel? BorderHighlighted { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<ITileControlViewModel> ItemsSource { get; set; }
        //public ObservableCollection<ITileControlViewModel> Tiles { get; private set; } = [];
        private int _availableTilesWidth;
        public BoardViewModel()
        {
            ItemsSource = [];
            TileSize = 20;
        }
        private void SetInitialState()
        {
            if (ItemsSource.Count > 0)
            {
                Selected = ItemsSource[0];
                Selected.IsSelected = true;
            }
            else
            {
                Selected = null; 
            }
        }
        private void CalculateTilesLayout(int width)
        {
            if (GridSize == 0)
                return;
            TileSize = width / GridSize;
            for (int i = 0; i < ItemsSource.Count; i++)
            {
                ITileControlViewModel tileControlViewModel = ItemsSource[i];

                tileControlViewModel.TileSize = TileSize;
                tileControlViewModel.X = (i % GridSize) * tileControlViewModel.TileSize;
                tileControlViewModel.Y = (i / GridSize) * tileControlViewModel.TileSize;
            }
        }
        partial void OnItemsSourceChanged(ObservableCollection<ITileControlViewModel> value)
        {
            SetInitialState();
        }

        public void SizeChanged(SizeChangedEventArgs e)
        {
            _availableTilesWidth = (int)(e.NewSize.Width);
            CalculateTilesLayout((int)e.NewSize.Width);
        }
        void OnGridSizeChanged(int value)
        {
//            Tiles.Clear();
//            for (int i = 0; i < value * value; i++)
//            {
//                (int row, int col) = Math.DivRem(i, value);
//#warning DEBUG CODE
//                byte tileValue = (byte)(i + 1);
//                if (i == value * value - 1)
//                {
//                    tileValue = 0;
//                }
//                ITileControlViewModel tileControlViewModel = new TileControlViewModel(
//                        new BoardTile { Value = tileValue, Row = row, Column = col }, null, null)
//                {
//                    CanSelect = CanSelect,
//                    CanGray = CanGray,
//                };
//                Tiles.Add(tileControlViewModel);
//            }
            CalculateTilesLayout(_availableTilesWidth);
            SetInitialState();
        }
        partial void OnCanSelectChanged(bool value)
        {
            foreach (ITileControlViewModel tileControlViewModel in ItemsSource)
            {
                tileControlViewModel.CanSelect = value;
            }
        }

        partial void OnSelectedChanged(ITileControlViewModel? oldValue, ITileControlViewModel? newValue)
        {
            if (oldValue != null)
            {
                oldValue.IsSelected = false;
            }
            if (newValue != null)
            {
                newValue.IsSelected = true;
            }
        }
        public void SetSelection(ITileControlViewModel vm)
        {
            if (Selected != null)
                Selected.IsSelected = false;
            foreach (ITileControlViewModel tileVm in ItemsSource)
            {
                if (tileVm == vm)
                {
                    tileVm.IsSelected = true;
                    Selected = tileVm;
                    return;
                }
            }
        }
        public void SetBorderHighlight(ITileControlViewModel vm)
        {
            if (BorderHighlighted != null)
                BorderHighlighted.IsBorderHighlighted = false;
            foreach (ITileControlViewModel tileVm in ItemsSource)
            {
                if (tileVm == vm)
                {
                    tileVm.IsBorderHighlighted = true;
                    BorderHighlighted = tileVm;
                    return;
                }
            }
        }
        public void ClearBorderHighlight()
        {
            if (BorderHighlighted == null)
                return;

            BorderHighlighted.IsBorderHighlighted = false;
            BorderHighlighted = null;
        }

        private void MoveSelection(int offset)
        {
            for (int i = 0; i < ItemsSource.Count; i++)
            {
                if (ItemsSource[i] == Selected)
                {
                    Selected.IsSelected = false;
                    int selectedIndex = i + offset;
                    if (selectedIndex < 0)
                        selectedIndex += GridSize * GridSize;
                    if (selectedIndex > (GridSize * GridSize) - 1)
                        selectedIndex -= GridSize * GridSize;
                    Selected = ItemsSource[selectedIndex];
                    Selected.IsSelected = true;
                    break;
                }
            }
        }

        public void MoveNext()
        {
            MoveSelection(1);
        }
        public void MovePrevious()
        {
            MoveSelection(-1);
        }
        public void MoveUp()
        {
            MoveSelection(-GridSize);
        }
        public void MoveDown()
        {
            MoveSelection(GridSize);
        }
    }
}
