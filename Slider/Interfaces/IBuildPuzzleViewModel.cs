using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Slider.Interfaces
{
    interface IBuildPuzzleViewModel
    {
        void AvailableSizeChanged(SizeChangedEventArgs e);
        void BoardSizeChanged(SizeChangedEventArgs e);
    }
}
