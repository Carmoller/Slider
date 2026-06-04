using Slider.Solver;
using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public interface ISolver
    {
        SolveResult Solve(List<BoardTile> board, SolverOptions options, IHeuristicElementFactory heuristicElementFactory);
        int GetHeuristic(List<BoardTile> board, IHeuristicElementFactory heuristicElementFactory);
    }
}
