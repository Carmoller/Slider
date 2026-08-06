using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface ITileControlViewModelFactory
    {
        ITileControlViewModel CreateViewModel(BoardTile boardTile);
        ITileControlViewModel CreateViewModel(BoardTile boardTile, IMainViewModel mainViewModel);
    }
}
