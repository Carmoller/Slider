using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Interfaces
{
    public class SolveResult
    {
        public SolveResultType Result { get; set; }
        public TimeSpan TimeSpent { get; set; }
        public List<Move> Moves { get; set; }
        public int MoveCount => Moves?.Count ?? 0;
        public long TotalStatesConsidered { get; set; }
        public long ForwardDictonarySize { get; set; }
        public long BackwardDictonarySize { get; set; }
        public long ForwardCollisionCount { get; set; }
        public long BackwardCollisionCount { get; set; }
        public long ForwardHitCount { get; set; }
        public long BackwardHitCount { get; set; }
        public long ForwardMaxListLength { get; set; }
        public long BackwardMaxListLength { get; set; }
        public int IDAStarIterations { get; set; }

        public SolveResult()
        {
            Moves = new();
        }
        public SolveResult(List<Move> moves)
        {
            Moves = moves;
        }
    }
}
