using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public enum BoardSelectionMethod
    {
        Keyboard,
        Mouse
    }

    public enum SolvableStatus
    {
        Solvable,
        NotSolvable,
        Incomplete,
        DuplicateTiles
    }
}
