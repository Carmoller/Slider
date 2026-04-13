using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public enum AllowedMove
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    public enum MoveDirection
    {
        Up,
        Down,
        Left, 
        Right
    }

    public enum HeuristicType
    {
        ManhattanDistance,
        LinearConflict
    }

    public enum SolveResultType
    {
        AlreadySolved,
        Solved,
        Unsolvable,
        Timeout
    }
}
