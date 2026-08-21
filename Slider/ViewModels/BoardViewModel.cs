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
        [ObservableProperty]
        public partial int GridSize { get; set; }
        [ObservableProperty]
        public partial bool CanSelect { get; set; }
        [ObservableProperty]
        public partial bool CanGray { get; set; }
        [ObservableProperty]
        public partial bool CanMove { get; set; }
        [ObservableProperty]
        public partial int AnimationDelay { get; set; }
        [ObservableProperty]
        public partial int TileSize { get; set; }
        [ObservableProperty]
        public partial ITileControlViewModel? Selected { get; set; }
        [ObservableProperty]
        public partial ITileControlViewModel? BorderHighlighted { get; set; }
        [ObservableProperty]
        public partial ObservableCollection<ITileControlViewModel> ItemsSource { get; set; }
        private int _availableTilesWidth;
        public BoardViewModel()
        {
            ItemsSource = [];
            TileSize = 20;
        }

        partial void OnItemsSourceChanged(ObservableCollection<ITileControlViewModel> oldValue, ObservableCollection<ITileControlViewModel> newValue)
        {
            oldValue?.CollectionChanged -= ItemsSource_CollectionChanged;
            newValue.CollectionChanged += ItemsSource_CollectionChanged;
        }
        private void ItemsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            CalculateTilesLayout(_availableTilesWidth);
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
                //tileControlViewModel.X = (i % GridSize) * tileControlViewModel.TileSize;
                //tileControlViewModel.Y = (i / GridSize) * tileControlViewModel.TileSize;
                tileControlViewModel.X = tileControlViewModel.Column * tileControlViewModel.TileSize;
                tileControlViewModel.Y = tileControlViewModel.Row * tileControlViewModel.TileSize;
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
        partial void OnGridSizeChanged(int value)
        {
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
        partial void OnCanMoveChanged(bool value)
        {
            foreach (ITileControlViewModel tileControlViewModel in ItemsSource)
            {
                tileControlViewModel.CanMove = value;
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
        partial void OnAnimationDelayChanged(int value)
        {
            foreach (ITileControlViewModel tileControlViewModel in ItemsSource)
            {
                tileControlViewModel.AnimationDelay = value;
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
