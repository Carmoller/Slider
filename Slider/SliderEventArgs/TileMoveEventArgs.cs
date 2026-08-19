using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.SliderEventArgs
{
    public class TileMoveEventArgs : EventArgs
    {
        public required AllowedMove Direction { get; set; }
    }
}
