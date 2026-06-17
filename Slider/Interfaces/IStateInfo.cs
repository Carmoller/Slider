using Slider.Common.Interfaces;
using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    internal interface IStateInfo
    {
        int NodeIndex { get; set; }
        int ParentIndex { get; set; }
        byte[] Board { get; set; }
        int BlankPos { get; set; }
        long Hash { get; set; }
        int BestG { get; set; }
        int CurrentG { get; set; }
        int CurrentH { get; set; }
        double CurrentF { get; set; }
        MoveDirection PreviousMove { get; set; }

        bool Equals(object? obj);
        int GetHashCode();
    }
}
