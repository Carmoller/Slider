using Slider.Common.Interfaces;
using Slider.Interfaces;
using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider
{
    public class TileControlViewModelFactory : ITileControlViewModelFactory
    {
        public ITileControlViewModel CreateViewModel(BoardTile boardTile)
        {
            return new TileControlViewModel(boardTile);
        }
    }
}
