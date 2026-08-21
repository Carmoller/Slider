using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Slider.Interfaces
{
    public interface ITileControlViewModel : INotifyPropertyChanged
    {
        BoardTile BoardTile { get; }
        int Value { get; set; }
        int TileSize { get; set; }
        bool IsEmpty { get; }
        bool CanSelect { get; set; }
        bool CanMove { get; set; }
        bool IsSelected { get; set; }
        bool IsBorderHighlighted { get; set; }
        bool IsHighlighted { get; set; }
        bool CanGray { get; set; }
        bool IsGray { get; set; }
        int X { get; set; }
        int Y { get; set; }
        int Row { get; set; }
        int Column { get; set; }
        int AnimationDelay { get; set; }

        bool Move(AllowedMove direction);
    }
}
