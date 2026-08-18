using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.SliderEventArgs
{
    public class SetBoardSelectionEventArgs : EventArgs
    {
        public required ITileControlViewModel Selected { get; set; }
    }
}
