using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.SliderEventArgs
{
    public class TilePositionChangedEventArgs : EventArgs
    {
        public int OldRow { get; set; }
        public int OldColumn { get; set; }
        public int NewRow { get; set; }
        public int NewColumn { get; set; }
    }
}
