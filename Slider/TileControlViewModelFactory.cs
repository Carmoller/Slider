using Slider.Interfaces;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider
{
    public class TileControlViewModelFactory(IOptions options) : ITileControlViewModelFactory
    {
        public ITileControlViewModel CreateViewModel(BoardTile boardTile, IMainViewModel mainViewModel)
        {
            return new TileControlViewModel(boardTile, mainViewModel, options);
        }
    }
}
