using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace Slider.Interfaces
{
    public interface IBoardViewModel
    {
        int GridSize { get; set; }
        void SizeChanged(SizeChangedEventArgs e);
        ITileControlViewModel? Selected { get; set; }
    }
}
