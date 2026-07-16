using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
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
        Right,
        None
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
        Timeout,
        StateLimitExceeded
    }

    public enum SolverType
    {
        BidirectionalAStar,
        WeightedAStar,
        MHAStarSolver,
        PdbSolver,
        BFSSolver
    }

    public enum BfsMode
    {
        Greedy,
        Standard
    }
    public enum BfsPurpose
    {
        SolveBoard,
        SolveTopRow,
    }
}
