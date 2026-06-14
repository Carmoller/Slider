using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    public class SolverOptions
    {
        public bool UseLinearConflict { get; set; } = true;
        public bool UseCornerPattern { get; set; } = true;
        public bool UseEdgePattern { get; set; } = false;
        public bool UsePdbs { get; set; } = false;
        public bool UseSprintFinish { get; set; }

        public override string ToString()
        {
            return $"SolverOptions: UseLinearConflict={UseLinearConflict}, UseCornerPattern={UseCornerPattern}, UseEdgePattern={UseEdgePattern}";
        }
    }
}
