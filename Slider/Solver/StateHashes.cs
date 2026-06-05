using System;
using System.Collections.Generic;
using System.Text;

namespace Slider.Solver
{
    public class StateHashes
    {
        private long[,]? _zobristTable;

        public static long FastHash(byte[] board)
        {
            unchecked
            {
                long hash = 17L;
                for (int i = 0; i < board.Length; i++)
                {
                    hash = hash * 31L + board[i];
                }
                return hash;
            }
        }

        public void InitializeZobrist(int size)
        {
            long sizeSquared = size * size;
            _zobristTable = new long[sizeSquared, sizeSquared];

            Random rand = new Random(42); // Seeded for consistency
            for (int pos = 0; pos < sizeSquared; pos++)
            {
                for (int tile = 0; tile < sizeSquared; tile++)
                {
                    byte[] buffer = new byte[8];
                    rand.NextBytes(buffer);
                    _zobristTable[pos, tile] = BitConverter.ToInt64(buffer, 0);
                }
            }
        }

        public long MoveZobrist(long currentHash, int blankPos, int oldPos, byte tileValue)
        {
            if (_zobristTable == null)
            {
                throw new InvalidOperationException("Zobrist has not been initialized");
            }
            // To apply a move where Tile X at 'oldPos' moves to 'blankPos':
            long nextHash = currentHash;

            // 1. XOR out the old state of these two slots
            nextHash ^= _zobristTable[blankPos, 0];       // Remove blank from old spot
            nextHash ^= _zobristTable[oldPos, tileValue];  // Remove tile from old spot

            // 2. XOR in their new positions
            nextHash ^= _zobristTable[oldPos, 0];          // Blank is now here
            nextHash ^= _zobristTable[blankPos, tileValue]; // Tile is now here
            return nextHash;
        }
    }
}
