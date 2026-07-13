using Slider.Common.Interfaces;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Slider
{
    public static class Extensions
    {
        public static string ToCommaSeparatedString(this byte[,] array)
        {
            StringBuilder sb = new();
            for (byte row = 0; row < array.GetLength(0); row++)
            {
                for (byte col = 0; col < array.GetLength(1); col++)
                {
                    sb.Append(array[row, col]);
                    if (row != array.GetLength(0) - 1 || col != array.GetLength(1) - 1)
                        sb.Append(',');
                }
            }
            return sb.ToString();
        }
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
        public static string ToPrettyPrintedBoardString(this List<BoardTile> array)
        {
            StringBuilder sb = new();
            for (int i = 0; i < array.Count; i += (int)Math.Sqrt(array.Count))
            {
                for (int j = 0; j < (int)Math.Sqrt(array.Count); j++)
                {
                    if (array[i + j].Value == 0)
                    {
                        sb.Append("  ");
                    }
                    else
                    {
                        sb.Append(array[i + j].Value.ToString("D2"));
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
