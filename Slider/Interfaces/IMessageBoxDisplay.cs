using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Slider.Interfaces
{
    public interface IUserAlert
    {
        public MessageBoxResult Alert(string message, string caption);
    }
}
