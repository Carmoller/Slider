using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface IGenerator
    {
        List<int> Generate(int gridSize);
    }
}
