using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Slider.Interfaces
{
    public interface IBuildPuzzleViewModel
    {
        ObservableCollection<ITileControlViewModel> Board { get; set; }
        ISelectTileViewModel CreateSelectTileViewModel();
    }
}
