using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface ISolverOptions
    {
        bool UseManhattanDistance { get; set; }
        bool UseLinearConflict { get; set; }  
        bool UseCornerPattern { get; set; } 
        bool UseEdgePattern { get; set; } 
        bool UseSprintFinish { get; set; }
        bool UseColumnAnchoring { get; set; }

    }
}
