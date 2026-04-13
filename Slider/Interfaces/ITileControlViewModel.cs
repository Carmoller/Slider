using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Slider.Interfaces
{
    public interface ITileControlViewModel : INotifyPropertyChanged
    {
        BoardTile BoardTile { get; }
        int TileSize { get; set; }
        bool IsEmpty { get; }
        bool IsHighlighted { get; set; }
        int X { get; set; }
        int Y { get; set; }
        int Row { get; set; }
        int Column { get; set; }
    }
}
