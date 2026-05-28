using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    public record DecodeResult
    {
        public byte[] TilePositions;
        public byte BlankPosition;

        public DecodeResult(byte[] tilePositions, byte blankPosition)
        {
            TilePositions = tilePositions;
            BlankPosition = blankPosition;
        }
    }
}
