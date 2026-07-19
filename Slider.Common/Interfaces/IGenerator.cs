using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface IGenerator
    {
        byte[] Generate(int gridSize);
    }
}
