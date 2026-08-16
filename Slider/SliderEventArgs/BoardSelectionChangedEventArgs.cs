using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Slider.SliderEventArgs
{
    public delegate void BoardSelectionChangedEventHandler(object sender, BoardSelectionChangedEventArgs e);
    public class BoardSelectionChangedEventArgs : RoutedEventArgs
    {
        public required ITileControlViewModel Tile { get; set; }
        public required BoardSelectionMethod SelectionMethod { get; set; }
        public BoardSelectionChangedEventArgs(RoutedEvent routedEvent, object source) : base(routedEvent, source)
        {
        }

    }
}
