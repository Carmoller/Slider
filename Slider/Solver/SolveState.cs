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
        public byte[,] Board { get; }
        public int GCost { get; }
        public int HCost { get; }
        public int FCost => GCost + HCost;
        public byte EmptyRow { get; }
        public byte EmptyCol { get; }
        public SolveState? Parent { get; }
        public int ParentMoveFromCol { get; set; } = -1;
        public int ParentMoveFromRow { get; set; } = -1;
        public int ParentMoveToCol { get; set; } = -1;
        public int ParentMoveToRow { get; set; } = -1;
        public MoveDirection MoveDirectionFromParent { get; set; }

        public SolveState(byte[,] board, int gCost, int hCost, byte emptyRow, byte emptyCol, SolveState? parent = null)
        {
            Board = board;
            GCost = gCost;
            HCost = hCost;
            EmptyRow = emptyRow;
            EmptyCol = emptyCol;
            Parent = parent;
        }

        public bool Equals(SolveState? other)
        {
            if (other == null) return false;
            if (EmptyRow != other.EmptyRow || EmptyCol != other.EmptyCol) return false;
            return BoardEquals(other.Board);
        }

        public bool BoardEquals(byte[,] otherBoard)
        {
            for (byte row = 0; row < Board.GetLength(0); row++)
            {
                for (byte col = 0; col < Board.GetLength(1); col++)
                {
                    if (Board[row, col] != otherBoard[row, col])
                        return false;
                }
            }
            return true;
        }
        public string DumpBoard()
        {
            StringBuilder sb = new();
            for (byte row = 0; row < Board.GetLength(0); row++)
            {
                for (byte col = 0; col < Board.GetLength(1); col++)
                {
                    sb.Append(Board[row, col].ToString().PadLeft(2) + " ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
