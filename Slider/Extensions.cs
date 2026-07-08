using System.IO;
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
        public static string ToCommaSeparatedString(this byte[] array)
        {
            return string.Join(',', array);
        }

        public static (int row, int column) ToRowAndColumn(this int index, int gridSize)
        {
            return Math.DivRem(index, gridSize);
        }

        extension(byte b)
        {
            public byte DontCare => 255;
            public byte Locked => 255;
        }
    }
}
