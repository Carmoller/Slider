using Slider.Common.Interfaces;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider
{
    public static class Extensions
    {
        public static (int row, int column) ToRowAndColumn(this int index, int gridSize)
        {
            return Math.DivRem(index, gridSize);
        }

        public static string ToPrettyPrintedBoardString(this Span<byte> array)
        {
            StringBuilder sb = new();
            for (int i = 0; i < array.Length; i += (int)Math.Sqrt(array.Length))
            {
                for (int j = 0; j < (int)Math.Sqrt(array.Length); j++)
                {
                    if (array[i + j] == 0)
                    {
                        sb.Append("  ");
                    }
                    else
                    {
                        sb.Append(array[i + j].ToString("D2"));
                    }
                    sb.Append(" ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static byte[] ToByteArray(this List<BoardTile> board)
        {
            int gridSize = (int)Math.Sqrt(board.Count);
            byte[] boardArray = new byte[board.Count];
            foreach (BoardTile tile in board)
            {
                boardArray[tile.Row * gridSize + tile.Column] = tile.Value;
            }
            return boardArray;
        }
        extension(byte b)
        {
            public byte DontCare => 255;
            public byte Locked => 255;
        }
    }
}
