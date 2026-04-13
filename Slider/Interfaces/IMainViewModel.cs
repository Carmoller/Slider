using Slider.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IMainViewModel
    {
        public AllowedMove CanMove(ITileControlViewModel tile);
        public void MoveTile(ITileControlViewModel tile);
    }
}
