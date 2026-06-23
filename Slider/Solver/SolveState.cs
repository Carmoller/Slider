using Slider.Common.Interfaces;
using Slider.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Slider.Solver
{
    public class SolveState
    {
        [DebuggerDisplay("{Board.ToCommaSeparatedString()} (G={GCost}, H={HCost}, F={FCost})")]
        public byte[] Board { get; }
        public int GCost { get; }
        public int HCost { get; }
        public int FCost => GCost + HCost;
        public int EmptyPosition { get; }
        public SolveState? Parent { get; }
        public int ParentMoveFromPosition { get; set; } = -1;
        public int ParentMoveToPosition { get; set; } = -1;
        public MoveDirection MoveDirectionFromParent { get; set; }

        public SolveState(byte[] board, int gCost, int hCost, int emptyPosition, SolveState? parent = null)
        {
            Board = board;
            GCost = gCost;
            HCost = hCost;
            EmptyPosition = emptyPosition;
            Parent = parent;
        }

        public bool Equals(SolveState? other)
        {
            if (other == null) return false;
            if (EmptyPosition != other.EmptyPosition) return false;
            return BoardEquals(other.Board);
        }

        public bool BoardEquals(byte[] other)
        {
            return Enumerable.SequenceEqual(other, Board);
        }

        public string DumpBoard()
        {
            int gridSize = (int)(Math.Sqrt(Board.Length));
            StringBuilder sb = new();
            for (int i = 0; i < Board.Length; i++)
            {
                sb.Append(Board[i].ToString().PadLeft(2) + " ");
                if (i % gridSize == 0)
                    sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
