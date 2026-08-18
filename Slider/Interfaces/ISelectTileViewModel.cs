using Slider.SliderEventArgs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;

namespace Slider.Interfaces
{
    public interface ISelectTileViewModel : INotifyPropertyChanged
    {
        event EventHandler<SetBoardSelectionEventArgs> SelectedChanged;
        int SelectedValue { get; set; }
        void KeyDown(KeyEventArgs e);
    }
}
