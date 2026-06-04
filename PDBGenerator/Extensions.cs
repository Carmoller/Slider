using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    public static class Extensions
    {
        public static string ReadBoundedString(this BinaryReader reader, int maxLength)
        {
            int length = reader.Read7BitEncodedInt();
            if (length > maxLength)
            {
                return string.Empty;
            }
            byte[] stringBytes = reader.ReadBytes(length);

            if (stringBytes.Length < length)
            {
                throw new EndOfStreamException("The stream ended before the full string could be read.");
            }

            return Encoding.UTF8.GetString(stringBytes);
        }
    }
}
