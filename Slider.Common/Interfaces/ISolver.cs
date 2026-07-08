using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Common.Interfaces
{
    public interface ISolver
    {
        SolveResult Solve(byte[] board, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory);
        SolveResult Solve(List<BoardTile> board, ISolverOptions solverOptions, IHeuristicElementFactory heuristicElementFactory);
        int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory);
    }
}
