using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IMainViewModel
    {
        public AllowedMove GetAllowedMoves(ITileControlViewModel tile);
        public void MoveTile(ITileControlViewModel tile);
    }
}
