using System;
using System.Collections.Generic;
using System.Text;

namespace PDBGenerator
{
    public record DecodeResultMem
    {
        public Memory<byte> TilePositions;
        public byte BlankPosition;

        public DecodeResultMem(Memory<byte> tilePositions, byte blankPosition)
        {
            TilePositions = tilePositions;
            BlankPosition = blankPosition;
        }
    }
}
