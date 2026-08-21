using Slider.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider.Interfaces
{
    public interface IModel
    {
        event EventHandler? BoardLayoutChanged;
        event EventHandler? BoardSolved;
        List<BoardTile> Board { get; }
        public bool CanUndo { get; }
        public int Heuristic { get; }
        int NumberOfMoves { get; }
        void New();
        void Undo();
        AllowedMove CanMove(BoardTile tile);
        AllowedMove MoveTile(BoardTile tile);
        SolveResult Solve();
        void EditFinished();
    }
}
